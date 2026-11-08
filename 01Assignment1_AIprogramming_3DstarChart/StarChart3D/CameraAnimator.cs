using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// Smoothly flies the 3D camera to a star. The camera turns to face the star while it moves,
    /// and ends a set distance away from it, looking straight at it. The movement eases in and out
    /// (slow at the start and end, faster in the middle). It uses WPF's own frame timing
    /// (CompositionTarget.Rendering), so it updates once per screen frame.
    /// </summary>
    public class CameraAnimator
    {
        /// <summary>The 3D view whose camera is moved.</summary>
        private readonly HelixViewport3D viewport;

        /// <summary>Measures how long the current animation has been running.</summary>
        private readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>Where the camera was when the animation started.</summary>
        private Point3D startPosition;

        /// <summary>The camera's look direction when the animation started.</summary>
        private Vector3D startLookDirection;

        /// <summary>Where the camera will be when the animation ends.</summary>
        private Point3D endPosition;

        /// <summary>The camera's look direction when the animation ends (points at the star).</summary>
        private Vector3D endLookDirection;

        /// <summary>How long the current animation lasts, in seconds.</summary>
        private double durationSeconds;

        /// <summary>
        /// Creates the animator for the given 3D view.
        /// </summary>
        /// <param name="viewport">The 3D view whose camera will be animated.</param>
        public CameraAnimator(HelixViewport3D viewport)
        {
            this.viewport = viewport;
        }

        /// <summary>Gets a value showing whether the camera is moving right now.</summary>
        public bool IsAnimating { get; private set; }

        /// <summary>Happens when an animation has finished.</summary>
        public event EventHandler? AnimationCompleted;

        /// <summary>
        /// Starts flying the camera to a point, ending viewDistance away from it and looking at it.
        /// If an animation is already running, the new one starts from where the camera is now.
        /// </summary>
        /// <param name="target">The point to fly to (a star's X, Y, Z in parsecs).</param>
        /// <param name="viewDistance">How far from the point the camera stops, in parsecs.</param>
        /// <param name="seconds">How long the flight takes, in seconds.</param>
        public void AnimateTo(Point3D target, double viewDistance, double seconds)
        {
            if (viewport.Camera is not ProjectionCamera camera)
            {
                return;
            }

            // Face the target: the end direction points from where the camera is now to the target.
            Vector3D direction = target - camera.Position;
            if (direction.Length < 1e-9)
            {
                direction = camera.LookDirection; // already at the target: keep facing the same way
            }

            direction.Normalize();

            startPosition = camera.Position;
            startLookDirection = camera.LookDirection;
            endLookDirection = direction * viewDistance;
            endPosition = target - endLookDirection;
            durationSeconds = Math.Max(seconds, 0.01);

            stopwatch.Restart();
            if (!IsAnimating)
            {
                CompositionTarget.Rendering += OnRendering;
                IsAnimating = true;
            }
        }

        /// <summary>
        /// Runs once per screen frame while animating: moves the camera part of the way,
        /// and stops when the time is up.
        /// </summary>
        private void OnRendering(object? sender, EventArgs e)
        {
            if (viewport.Camera is not ProjectionCamera camera)
            {
                Stop();
                return;
            }

            double progress = Math.Min(stopwatch.Elapsed.TotalSeconds / durationSeconds, 1.0);
            double eased = progress * progress * (3 - 2 * progress); // "smoothstep" ease in and out

            camera.Position = startPosition + (endPosition - startPosition) * eased;
            camera.LookDirection = startLookDirection + (endLookDirection - startLookDirection) * eased;

            if (progress >= 1.0)
            {
                Stop();
                AnimationCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Stops the animation and stops listening for screen frames.</summary>
        private void Stop()
        {
            CompositionTarget.Rendering -= OnRendering;
            stopwatch.Stop();
            IsAnimating = false;
        }
    }
}