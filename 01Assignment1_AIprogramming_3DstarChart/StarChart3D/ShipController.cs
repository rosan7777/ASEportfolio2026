using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// Flies the 3D camera like a spaceship (Feature 4, inspired by the game Elite).
    /// In flight mode the ship keeps moving forward at its current speed, and turns smoothly while a
    /// key is held:
    ///   W = speed up, S = slow down (never backwards), X = stop,
    ///   Up / Down arrows = pitch the nose down / up, Left / Right arrows = turn left / right,
    ///   Q / E = roll left / right.
    /// It uses WPF's own frame timing (CompositionTarget.Rendering), like CameraAnimator, and only
    /// listens for frames while flight mode is on.
    /// </summary>
    public class ShipController
    {
        /// <summary>The fastest the ship can go, in parsecs per second.</summary>
        public const double MaxSpeed = 5.0;

        /// <summary>How quickly W and S change the speed, in parsecs per second, per second.</summary>
        private const double Acceleration = 1.0;

        /// <summary>How fast the ship pitches and turns while an arrow key is held, in degrees per second.</summary>
        private const double TurnRateDegrees = 45.0;

        /// <summary>How fast the ship rolls while Q or E is held, in degrees per second.</summary>
        private const double RollRateDegrees = 60.0;

        /// <summary>The 3D view whose camera is the ship.</summary>
        private readonly HelixViewport3D viewport;

        /// <summary>The ship keys that are held down right now.</summary>
        private readonly HashSet<Key> heldKeys = new HashSet<Key>();

        /// <summary>Measures the time between frames, so movement doesn't depend on the frame rate.</summary>
        private readonly Stopwatch frameTimer = new Stopwatch();

        /// <summary>
        /// Creates the ship controller for the given 3D view. Flight mode starts off.
        /// </summary>
        /// <param name="viewport">The 3D view whose camera will be flown.</param>
        public ShipController(HelixViewport3D viewport)
        {
            this.viewport = viewport;
        }

        /// <summary>Gets a value showing whether flight mode is on.</summary>
        public bool IsFlightModeOn { get; private set; }

        /// <summary>Gets the ship's current forward speed, in parsecs per second.</summary>
        public double Speed { get; private set; }

        /// <summary>Happens when flight mode is turned on or off, or the speed changes.</summary>
        public event EventHandler? StatusChanged;

        /// <summary>
        /// Turns flight mode on, starting at speed 0, and starts listening for screen frames.
        /// </summary>
        public void Start()
        {
            if (IsFlightModeOn)
            {
                return;
            }

            heldKeys.Clear();
            Speed = 0;
            frameTimer.Restart();
            CompositionTarget.Rendering += OnRendering;
            IsFlightModeOn = true;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Turns flight mode off: the ship stops and stops listening for screen frames.
        /// </summary>
        public void Stop()
        {
            if (!IsFlightModeOn)
            {
                return;
            }

            CompositionTarget.Rendering -= OnRendering;
            frameTimer.Stop();
            heldKeys.Clear();
            Speed = 0;
            IsFlightModeOn = false;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when a key is pressed. Remembers ship keys as held, and X stops the ship at once.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <returns>True if the key is a ship key (so it should not go anywhere else).</returns>
        public bool HandleKeyDown(Key key)
        {
            if (!IsFlightModeOn)
            {
                return false;
            }

            if (key == Key.X)
            {
                SetSpeed(0);
                return true;
            }

            if (IsShipKey(key))
            {
                heldKeys.Add(key);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when a key is let go. A ship key stops having an effect.
        /// </summary>
        /// <param name="key">The key that was let go.</param>
        /// <returns>True if the key is a ship key.</returns>
        public bool HandleKeyUp(Key key)
        {
            if (!IsFlightModeOn)
            {
                return false;
            }

            return heldKeys.Remove(key) || key == Key.X;
        }

        /// <summary>
        /// Forgets all held keys, for example when the window loses focus and the key-up would be missed.
        /// </summary>
        public void ReleaseAllKeys()
        {
            heldKeys.Clear();
        }

        /// <summary>Returns true for the keys that steer or speed the ship (X is handled on its own).</summary>
        private static bool IsShipKey(Key key)
        {
            return key == Key.W || key == Key.S
                || key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right
                || key == Key.Q || key == Key.E;
        }

        /// <summary>
        /// Runs once per screen frame while flight mode is on: changes the speed and turns the ship
        /// for the keys that are held, then moves the camera forward.
        /// </summary>
        private void OnRendering(object? sender, EventArgs e)
        {
            if (viewport.Camera is not ProjectionCamera camera)
            {
                return;
            }

            double seconds = frameTimer.Elapsed.TotalSeconds;
            frameTimer.Restart();

            // Speed: W and S change it smoothly while held.
            if (heldKeys.Contains(Key.W))
            {
                SetSpeed(Speed + Acceleration * seconds);
            }

            if (heldKeys.Contains(Key.S))
            {
                SetSpeed(Speed - Acceleration * seconds);
            }

            // The ship's own directions: forward (where the camera looks), up, and right.
            double lookLength = camera.LookDirection.Length;
            Vector3D forward = camera.LookDirection;
            forward.Normalize();
            Vector3D right = Vector3D.CrossProduct(forward, camera.UpDirection);
            if (right.Length < 1e-9)
            {
                return; // looking straight along the up direction: can't work out "right" this frame
            }

            right.Normalize();
            Vector3D up = Vector3D.CrossProduct(right, forward);

            double turn = TurnRateDegrees * seconds;
            double roll = RollRateDegrees * seconds;

            // Pitch: turn forward and up around the right axis. A positive angle lifts the nose.
            double pitchAngle = 0;
            if (heldKeys.Contains(Key.Up))
            {
                pitchAngle -= turn; // nose down
            }

            if (heldKeys.Contains(Key.Down))
            {
                pitchAngle += turn; // nose up
            }

            // Yaw: turn forward and right around the up axis. A positive angle turns left.
            double yawAngle = 0;
            if (heldKeys.Contains(Key.Left))
            {
                yawAngle += turn;
            }

            if (heldKeys.Contains(Key.Right))
            {
                yawAngle -= turn;
            }

            // Roll: turn up and right around the forward axis. A positive angle rolls right.
            double rollAngle = 0;
            if (heldKeys.Contains(Key.Q))
            {
                rollAngle -= roll;
            }

            if (heldKeys.Contains(Key.E))
            {
                rollAngle += roll;
            }

            forward = Rotate(forward, right, pitchAngle);
            up = Rotate(up, right, pitchAngle);
            forward = Rotate(forward, up, yawAngle);
            up = Rotate(up, forward, rollAngle);

            forward.Normalize();
            up.Normalize();

            // Keep the look direction's length, so HelixToolkit's mouse zoom and the camera info still make sense.
            camera.LookDirection = forward * lookLength;
            camera.UpDirection = up;
            camera.Position += forward * (Speed * seconds);
        }

        /// <summary>
        /// Sets the speed, kept between 0 (stopped, never backwards) and MaxSpeed, and reports a change.
        /// </summary>
        private void SetSpeed(double newSpeed)
        {
            double clamped = Math.Clamp(newSpeed, 0, MaxSpeed);
            if (clamped != Speed)
            {
                Speed = clamped;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Rotates a vector around an axis by an angle in degrees (counter-clockwise when looking
        /// back along the axis, the right-hand rule).
        /// </summary>
        private static Vector3D Rotate(Vector3D vector, Vector3D axis, double angleDegrees)
        {
            if (angleDegrees == 0)
            {
                return vector;
            }

            Matrix3D matrix = Matrix3D.Identity;
            matrix.Rotate(new Quaternion(axis, angleDegrees));
            return matrix.Transform(vector);
        }
    }
}