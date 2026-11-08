using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// Draws "space dust": a few hundred small, faint grey points in a box around the camera, to give
    /// the illusion of slow motion through space. The dust always drifts slowly towards the camera
    /// (as if we are moving forward), and when the camera moves - for example while flying to a star -
    /// the dust stays still in space, so it streams past in the direction of travel. A point that leaves
    /// the box comes back in on the opposite side, so the dust never runs out.
    /// It uses WPF's own frame timing (CompositionTarget.Rendering), like CameraAnimator.
    /// </summary>
    public class DustField
    {
        /// <summary>How many dust points there are.</summary>
        private const int DustCount = 400;

        /// <summary>Half the width of the dust box around the camera, in parsecs.</summary>
        private const double BoxHalfSize = 1.5;

        /// <summary>How fast the dust drifts towards the camera when the camera is still, in parsecs per second.</summary>
        private const double DriftSpeed = 0.15;

        /// <summary>The size of each dust point on screen, in pixels. Stars are drawn bigger than this.</summary>
        private const double DustPointSize = 2.5;

        /// <summary>The colour of the dust: a mid grey, bright enough to see but fainter than the star colours.</summary>
        private static readonly Color DustColour = Color.FromRgb(140, 140, 140);

        /// <summary>The 3D view the dust is drawn in.</summary>
        private readonly HelixViewport3D viewport;

        /// <summary>The visual that draws the points. Points are drawn a fixed number of pixels wide.</summary>
        private readonly PointsVisual3D dustVisual = new PointsVisual3D
        {
            Color = DustColour,
            Size = DustPointSize
        };

        /// <summary>
        /// Where each dust point is, measured from the camera's position (in parsecs).
        /// Every value stays between -BoxHalfSize and +BoxHalfSize.
        /// </summary>
        private readonly Vector3D[] offsets = new Vector3D[DustCount];

        /// <summary>Measures the time between frames, so the drift speed doesn't depend on the frame rate.</summary>
        private readonly Stopwatch frameTimer = new Stopwatch();

        /// <summary>Where the camera was on the last frame, to see how far it has moved.</summary>
        private Point3D lastCameraPosition;

        /// <summary>True once lastCameraPosition has been set on the first frame.</summary>
        private bool hasLastCameraPosition;

        /// <summary>
        /// Creates the dust, spreads the points randomly through the box and adds them to the viewport.
        /// The dust does not move until Start is called.
        /// </summary>
        /// <param name="viewport">The 3D view to draw the dust in.</param>
        public DustField(HelixViewport3D viewport)
        {
            this.viewport = viewport;

            // A fixed seed gives the same dust every time the app runs.
            Random random = new Random(1);
            for (int i = 0; i < DustCount; i++)
            {
                offsets[i] = new Vector3D(
                    RandomInBox(random),
                    RandomInBox(random),
                    RandomInBox(random));
            }

            viewport.Children.Add(dustVisual);
        }

        /// <summary>Gets a value showing whether the dust is moving.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Starts moving the dust once per screen frame.</summary>
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            hasLastCameraPosition = false;
            frameTimer.Restart();
            CompositionTarget.Rendering += OnRendering;
            IsRunning = true;
        }

        /// <summary>Stops moving the dust. The points stay where they are.</summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            CompositionTarget.Rendering -= OnRendering;
            frameTimer.Stop();
            IsRunning = false;
        }

        /// <summary>
        /// Runs once per screen frame: moves the dust by the slow drift plus however far the camera
        /// moved, wraps points that left the box, and draws them around the camera.
        /// </summary>
        private void OnRendering(object? sender, EventArgs e)
        {
            if (viewport.Camera is not ProjectionCamera camera)
            {
                return;
            }

            double seconds = frameTimer.Elapsed.TotalSeconds;
            frameTimer.Restart();

            // How far the camera moved since the last frame. Moving the dust back by this amount keeps it
            // still in space, so it streams past while the camera flies.
            Vector3D cameraMove = hasLastCameraPosition ? camera.Position - lastCameraPosition : new Vector3D();
            lastCameraPosition = camera.Position;
            hasLastCameraPosition = true;

            // The slow drift: the dust comes towards the camera along the way it is looking.
            Vector3D lookDirection = camera.LookDirection;
            Vector3D drift = new Vector3D();
            if (lookDirection.Length > 1e-9)
            {
                lookDirection.Normalize();
                drift = lookDirection * (DriftSpeed * seconds);
            }

            Vector3D totalMove = cameraMove + drift;
            Point3DCollection points = new Point3DCollection(DustCount);
            for (int i = 0; i < DustCount; i++)
            {
                Vector3D offset = offsets[i] - totalMove;
                offset = new Vector3D(Wrap(offset.X), Wrap(offset.Y), Wrap(offset.Z));
                offsets[i] = offset;
                points.Add(camera.Position + offset);
            }

            // A new collection each frame tells PointsVisual3D to redraw the points.
            dustVisual.Points = points;
        }

        /// <summary>Returns a random number between -BoxHalfSize and +BoxHalfSize.</summary>
        private static double RandomInBox(Random random)
        {
            return (random.NextDouble() * 2 - 1) * BoxHalfSize;
        }

        /// <summary>
        /// Brings a value that has left the box back in on the opposite side, so it stays between
        /// -BoxHalfSize and +BoxHalfSize. This works even for a big jump, such as during a fast flight.
        /// </summary>
        private static double Wrap(double value)
        {
            double size = BoxHalfSize * 2;
            double shifted = (value + BoxHalfSize) % size;
            if (shifted < 0)
            {
                shifted += size;
            }

            return shifted - BoxHalfSize;
        }
    }
}