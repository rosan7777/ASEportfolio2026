using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// Flies a spaceship through the stars (Feature 4, inspired by the game Elite).
    /// The ship has its own position, forward and up directions. In flight mode it keeps moving
    /// forward at its current speed, and turns smoothly while a key is held:
    ///   W = speed up, S = slow down (never backwards), X = stop,
    ///   Up / Down arrows = pitch the nose down / up, Left / Right arrows = turn left / right,
    ///   Q / E = roll left / right, C = switch between cockpit view and chase view,
    ///   T = autopilot on/off (flies to the target star; any steering key turns it off).
    /// In the cockpit view the camera is the ship. In the chase view the camera sits behind and
    /// above the ship, and the ship model is shown.
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

        /// <summary>In the chase view, how far behind the ship the camera sits, in parsecs.</summary>
        private const double ChaseDistanceBehind = 0.25;

        /// <summary>In the chase view, how far above the ship the camera sits, in parsecs.</summary>
        private const double ChaseHeightAbove = 0.06;

        /// <summary>In the chase view, how far in front of the ship the camera looks, in parsecs.</summary>
        private const double ChaseLookAhead = 0.3;

        /// <summary>The autopilot stops the ship this close to the target star, in parsecs (about 1 light year).</summary>
        private const double ArrivalDistance = 0.3;

        /// <summary>The slowest the autopilot flies before arriving, in parsecs per second, so it never stalls.</summary>
        private const double AutopilotMinSpeed = 0.05;

        /// <summary>
        /// If the target is more than this many degrees off the nose, the autopilot flies slowly while it
        /// turns, so it doesn't fly a wide circle.
        /// </summary>
        private const double AutopilotSlowTurnAngle = 45.0;

        /// <summary>The autopilot's speed while it is turning towards a target far off the nose, in parsecs per second.</summary>
        private const double AutopilotTurningSpeed = 0.3;

        /// <summary>The 3D view whose camera follows the ship.</summary>
        private readonly HelixViewport3D viewport;

        /// <summary>The ship model, shown only in the chase view.</summary>
        private readonly ShipModel shipModel;

        /// <summary>The ship keys that are held down right now.</summary>
        private readonly HashSet<Key> heldKeys = new HashSet<Key>();

        /// <summary>Measures the time between frames, so movement doesn't depend on the frame rate.</summary>
        private readonly Stopwatch frameTimer = new Stopwatch();

        /// <summary>
        /// The camera's look direction length when flight mode started. It is kept in the cockpit view,
        /// so HelixToolkit's mouse zoom and the camera info still make sense.
        /// </summary>
        private double cockpitLookLength = 1.0;

        /// <summary>
        /// Creates the ship controller for the given 3D view. Flight mode starts off.
        /// </summary>
        /// <param name="viewport">The 3D view whose camera will follow the ship.</param>
        /// <param name="shipModel">The ship model to show in the chase view.</param>
        public ShipController(HelixViewport3D viewport, ShipModel shipModel)
        {
            this.viewport = viewport;
            this.shipModel = shipModel;
        }

        /// <summary>Gets a value showing whether flight mode is on.</summary>
        public bool IsFlightModeOn { get; private set; }

        /// <summary>Gets a value showing whether the chase view (camera behind the ship) is on.</summary>
        public bool IsChaseView { get; private set; }

        /// <summary>Gets the ship's current forward speed, in parsecs per second.</summary>
        public double Speed { get; private set; }

        /// <summary>Gets where the ship is, in parsecs.</summary>
        public Point3D ShipPosition { get; private set; }

        /// <summary>Gets the way the ship points (length 1).</summary>
        public Vector3D ShipForward { get; private set; } = new Vector3D(0, 0, -1);

        /// <summary>Gets the ship's up direction (length 1).</summary>
        public Vector3D ShipUp { get; private set; } = new Vector3D(0, 1, 0);

        /// <summary>Gets the star the ship is targeting, or null if there is no target.</summary>
        public Star? Target { get; private set; }

        /// <summary>Gets a value showing whether the autopilot is flying the ship to the target.</summary>
        public bool IsAutopilotOn { get; private set; }

        /// <summary>Happens when flight mode, the view, the target or the autopilot is changed, or the speed changes.</summary>
        public event EventHandler? StatusChanged;

        /// <summary>Happens when the autopilot has stopped the ship near the target. The sender is this controller.</summary>
        public event EventHandler<Star>? AutopilotArrived;

        /// <summary>
        /// Sets the star the ship targets (or null for none). The autopilot keeps flying if it is on,
        /// now towards the new target.
        /// </summary>
        /// <param name="star">The new target star.</param>
        public void SetTarget(Star? star)
        {
            Target = star;
            if (star == null)
            {
                IsAutopilotOn = false;
            }

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Turns the autopilot on or off. It can only be turned on in flight mode with a target.
        /// </summary>
        public void ToggleAutopilot()
        {
            if (IsAutopilotOn)
            {
                IsAutopilotOn = false;
            }
            else if (IsFlightModeOn && Target != null)
            {
                IsAutopilotOn = true;
                heldKeys.Clear(); // the autopilot is flying now, not the keys
            }

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Turns flight mode on in the cockpit view, starting at speed 0. The ship starts where the
        /// camera is, pointing the way the camera looks.
        /// </summary>
        public void Start()
        {
            if (IsFlightModeOn || viewport.Camera is not ProjectionCamera camera)
            {
                return;
            }

            Vector3D forward = camera.LookDirection;
            cockpitLookLength = forward.Length > 1e-9 ? forward.Length : 1.0;
            forward = forward.Length > 1e-9 ? forward : new Vector3D(0, 0, -1);
            forward.Normalize();

            // Make "up" exactly at right angles to "forward".
            Vector3D right = Vector3D.CrossProduct(forward, camera.UpDirection);
            if (right.Length < 1e-9)
            {
                right = Vector3D.CrossProduct(forward, new Vector3D(1, 0, 0));
            }

            right.Normalize();
            Vector3D up = Vector3D.CrossProduct(right, forward);

            ShipPosition = camera.Position;
            ShipForward = forward;
            ShipUp = up;

            heldKeys.Clear();
            Speed = 0;
            Target = null;
            IsAutopilotOn = false;
            IsChaseView = false;
            shipModel.IsVisible = false;
            frameTimer.Restart();
            CompositionTarget.Rendering += OnRendering;
            IsFlightModeOn = true;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Turns flight mode off: the ship stops, the camera is put back at the ship (cockpit view),
        /// the ship model is hidden, and it stops listening for screen frames.
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
            Target = null;
            IsAutopilotOn = false;
            IsChaseView = false;
            shipModel.IsVisible = false;
            PlaceCamera();
            IsFlightModeOn = false;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Switches between the cockpit view and the chase view. Only works in flight mode.
        /// </summary>
        public void ToggleView()
        {
            if (!IsFlightModeOn)
            {
                return;
            }

            IsChaseView = !IsChaseView;
            shipModel.IsVisible = IsChaseView;
            PlaceCamera();
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when a key is pressed. Remembers ship keys as held; X stops the ship and C switches
        /// the view at once.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <returns>True if the key is a ship key (so it should not go anywhere else).</returns>
        public bool HandleKeyDown(Key key)
        {
            if (!IsFlightModeOn)
            {
                return false;
            }

            if (key == Key.T)
            {
                ToggleAutopilot();
                return true;
            }

            // Any steering or speed key takes control back from the autopilot.
            if (IsAutopilotOn && (key == Key.X || IsShipKey(key)))
            {
                IsAutopilotOn = false;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }

            if (key == Key.X)
            {
                SetSpeed(0);
                return true;
            }

            if (key == Key.C)
            {
                ToggleView();
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

            return heldKeys.Remove(key) || key == Key.X || key == Key.C || key == Key.T;
        }

        /// <summary>
        /// Forgets all held keys, for example when the window loses focus and the key-up would be missed.
        /// </summary>
        public void ReleaseAllKeys()
        {
            heldKeys.Clear();
        }

        /// <summary>Returns true for the keys that steer or speed the ship (X, C and T are handled on their own).</summary>
        private static bool IsShipKey(Key key)
        {
            return key == Key.W || key == Key.S
                || key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right
                || key == Key.Q || key == Key.E;
        }

        /// <summary>
        /// Runs once per screen frame while flight mode is on: changes the speed and turns the ship
        /// for the keys that are held, moves the ship forward, then places the camera and the model.
        /// </summary>
        private void OnRendering(object? sender, EventArgs e)
        {
            double seconds = frameTimer.Elapsed.TotalSeconds;
            frameTimer.Restart();

            if (IsAutopilotOn)
            {
                FlyAutopilot(seconds);
                PlaceCamera();
                return;
            }

            // Speed: W and S change it smoothly while held.
            if (heldKeys.Contains(Key.W))
            {
                SetSpeed(Speed + Acceleration * seconds);
            }

            if (heldKeys.Contains(Key.S))
            {
                SetSpeed(Speed - Acceleration * seconds);
            }

            // The ship's own directions: forward, up, and right.
            Vector3D forward = ShipForward;
            Vector3D up = ShipUp;
            Vector3D right = Vector3D.CrossProduct(forward, up);
            right.Normalize();

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

            // Yaw: turn forward around the up axis. A positive angle turns left.
            double yawAngle = 0;
            if (heldKeys.Contains(Key.Left))
            {
                yawAngle += turn;
            }

            if (heldKeys.Contains(Key.Right))
            {
                yawAngle -= turn;
            }

            // Roll: turn up around the forward axis. A positive angle rolls right.
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

            // Keep both directions length 1 and exactly at right angles, so small rounding errors
            // don't build up over a long flight.
            forward.Normalize();
            right = Vector3D.CrossProduct(forward, up);
            right.Normalize();
            up = Vector3D.CrossProduct(right, forward);

            ShipForward = forward;
            ShipUp = up;
            ShipPosition += forward * (Speed * seconds);

            PlaceCamera();
        }

        /// <summary>
        /// One frame of the autopilot: turns the ship towards the target at the normal turn rate,
        /// changes the speed smoothly (no faster than W and S would), and stops near the target.
        /// The speed is limited so the ship can always brake in time: v = sqrt(2 × acceleration × distance left).
        /// </summary>
        /// <param name="seconds">The time since the last frame.</param>
        private void FlyAutopilot(double seconds)
        {
            if (Target == null)
            {
                IsAutopilotOn = false;
                StatusChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            Star target = Target;
            Vector3D toTarget = new Point3D(target.X, target.Y, target.Z) - ShipPosition;
            double distance = toTarget.Length;

            // Arrived: stop, turn the autopilot off and tell the window. The small extra allows for
            // rounding errors, because the last step stops the ship exactly at ArrivalDistance.
            if (distance <= ArrivalDistance + 1e-6)
            {
                SetSpeed(0);
                IsAutopilotOn = false;
                StatusChanged?.Invoke(this, EventArgs.Empty);
                AutopilotArrived?.Invoke(this, target);
                return;
            }

            // Turn: rotate forward and up around the axis at right angles to both "forward" and
            // "towards the target", by no more than the normal turn rate this frame.
            Vector3D forward = ShipForward;
            Vector3D up = ShipUp;
            double angleOff = Vector3D.AngleBetween(forward, toTarget); // degrees, 0 to 180
            double maxTurn = TurnRateDegrees * seconds;
            if (angleOff > 1e-6)
            {
                Vector3D axis = Vector3D.CrossProduct(forward, toTarget);
                if (axis.Length < 1e-9)
                {
                    axis = up; // the target is straight behind: turn around the up axis
                }

                double turn = Math.Min(angleOff, maxTurn);
                forward = Rotate(forward, axis, turn);
                up = Rotate(up, axis, turn);
            }

            // Keep both directions length 1 and exactly at right angles.
            forward.Normalize();
            Vector3D right = Vector3D.CrossProduct(forward, up);
            right.Normalize();
            up = Vector3D.CrossProduct(right, forward);
            ShipForward = forward;
            ShipUp = up;

            // Speed: as fast as allowed, but able to brake before arriving, and slow while turning a lot.
            double distanceLeft = distance - ArrivalDistance;
            double wantedSpeed = Math.Min(MaxSpeed, Math.Sqrt(2 * Acceleration * distanceLeft));
            wantedSpeed = Math.Max(wantedSpeed, AutopilotMinSpeed);
            double angleAfterTurn = Vector3D.AngleBetween(forward, toTarget);
            if (angleAfterTurn > 90.0)
            {
                wantedSpeed = AutopilotMinSpeed; // target is behind: nearly stop and turn on the spot
            }
            else if (angleAfterTurn > AutopilotSlowTurnAngle)
            {
                wantedSpeed = Math.Min(wantedSpeed, AutopilotTurningSpeed);
            }

            double maxChange = Acceleration * seconds;
            SetSpeed(Speed + Math.Clamp(wantedSpeed - Speed, -maxChange, maxChange));

            // Don't fly past the stopping point in one frame.
            double step = Math.Min(Speed * seconds, distanceLeft);
            ShipPosition += forward * step;
        }

        /// <summary>
        /// Puts the camera where the current view needs it, and moves the ship model to the ship.
        /// Cockpit view: the camera is at the ship, looking forward.
        /// Chase view: the camera is behind and above the ship, looking at a point just in front of it.
        /// </summary>
        private void PlaceCamera()
        {
            if (viewport.Camera is not ProjectionCamera camera)
            {
                return;
            }

            if (IsChaseView)
            {
                Point3D cameraPosition = ShipPosition - ShipForward * ChaseDistanceBehind + ShipUp * ChaseHeightAbove;
                Point3D lookAt = ShipPosition + ShipForward * ChaseLookAhead;
                camera.Position = cameraPosition;
                camera.LookDirection = lookAt - cameraPosition;
                camera.UpDirection = ShipUp;
                shipModel.SetPose(ShipPosition, ShipForward, ShipUp);
            }
            else
            {
                camera.Position = ShipPosition;
                camera.LookDirection = ShipForward * cockpitLookLength;
                camera.UpDirection = ShipUp;
            }
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