using System.Windows.Input;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// Moves the 3D camera with the keyboard:
    ///   + / -        zoom in / out towards the point the camera looks at
    ///   arrow keys   pan up, down, left, right
    ///   W A S D      rotate (orbit) around the point the camera looks at
    ///   Home         reset the camera to where it started
    /// Zoom and pan steps are a fraction of the camera's distance to that point,
    /// so the keys work the same whether the camera is near or far.
    /// </summary>
    public class KeyboardCameraController
    {
        /// <summary>How much of the distance to the target one zoom key press moves (10%).</summary>
        private const double ZoomFraction = 0.1;

        /// <summary>How much of the distance to the target one pan key press moves (5%).</summary>
        private const double PanFraction = 0.05;

        /// <summary>How many degrees one rotate key press turns the camera.</summary>
        private const double RotateDegrees = 5;

        /// <summary>The closest the camera may zoom to its target, in parsecs.</summary>
        private const double MinimumDistance = 0.2;

        /// <summary>The furthest the camera may zoom away from its target, in parsecs.</summary>
        private const double MaximumDistance = 500;

        /// <summary>The 3D view whose camera is moved.</summary>
        private readonly HelixViewport3D viewport;

        /// <summary>The camera position when the controller was created, used by Home.</summary>
        private readonly Point3D startPosition;

        /// <summary>The camera look direction when the controller was created, used by Home.</summary>
        private readonly Vector3D startLookDirection;

        /// <summary>The camera up direction when the controller was created, used by Home.</summary>
        private readonly Vector3D startUpDirection;

        /// <summary>
        /// Creates the controller and remembers where the camera starts, so Home can go back there.
        /// </summary>
        /// <param name="viewport">The 3D view whose camera the keys will move.</param>
        public KeyboardCameraController(HelixViewport3D viewport)
        {
            this.viewport = viewport;

            ProjectionCamera camera = (ProjectionCamera)viewport.Camera;
            startPosition = camera.Position;
            startLookDirection = camera.LookDirection;
            startUpDirection = camera.UpDirection;
        }

        /// <summary>
        /// Moves the camera if the key is one of the camera keys.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <returns>True if the key was used to move the camera, otherwise false.</returns>
        public bool HandleKey(Key key)
        {
            if (viewport.Camera is not ProjectionCamera camera)
            {
                return false;
            }

            switch (key)
            {
                case Key.Add:
                case Key.OemPlus:
                    Zoom(camera, ZoomFraction);
                    return true;

                case Key.Subtract:
                case Key.OemMinus:
                    Zoom(camera, -ZoomFraction);
                    return true;

                case Key.Left:
                    Pan(camera, -1, 0);
                    return true;

                case Key.Right:
                    Pan(camera, 1, 0);
                    return true;

                case Key.Up:
                    Pan(camera, 0, 1);
                    return true;

                case Key.Down:
                    Pan(camera, 0, -1);
                    return true;

                case Key.A:
                    Rotate(camera, camera.UpDirection, RotateDegrees);
                    return true;

                case Key.D:
                    Rotate(camera, camera.UpDirection, -RotateDegrees);
                    return true;

                case Key.W:
                    Rotate(camera, GetRightDirection(camera), RotateDegrees);
                    return true;

                case Key.S:
                    Rotate(camera, GetRightDirection(camera), -RotateDegrees);
                    return true;

                case Key.Home:
                    camera.Position = startPosition;
                    camera.LookDirection = startLookDirection;
                    camera.UpDirection = startUpDirection;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Moves the camera towards (positive fraction) or away from (negative fraction) the point it
        /// looks at. The point it looks at stays the same, so only the distance changes.
        /// </summary>
        private static void Zoom(ProjectionCamera camera, double fraction)
        {
            double newDistance = camera.LookDirection.Length * (1 - fraction);
            if (newDistance < MinimumDistance || newDistance > MaximumDistance)
            {
                return;
            }

            Vector3D move = camera.LookDirection * fraction;
            camera.Position += move;
            camera.LookDirection -= move;
        }

        /// <summary>
        /// Slides the camera sideways (right = +1) and up (up = +1) on the screen without turning it.
        /// The point it looks at slides with it.
        /// </summary>
        private static void Pan(ProjectionCamera camera, int right, int up)
        {
            Vector3D rightDirection = GetRightDirection(camera);
            Vector3D upDirection = Vector3D.CrossProduct(rightDirection, camera.LookDirection);
            upDirection.Normalize();

            double step = camera.LookDirection.Length * PanFraction;
            camera.Position += (rightDirection * right + upDirection * up) * step;
        }

        /// <summary>
        /// Turns the camera around the point it looks at, about the given axis.
        /// </summary>
        private static void Rotate(ProjectionCamera camera, Vector3D axis, double degrees)
        {
            Point3D target = camera.Position + camera.LookDirection;
            RotateTransform3D rotation = new RotateTransform3D(new AxisAngleRotation3D(axis, degrees), target);

            camera.Position = rotation.Transform(camera.Position);
            camera.LookDirection = rotation.Transform(camera.LookDirection);
            camera.UpDirection = rotation.Transform(camera.UpDirection);
        }

        /// <summary>Returns the direction pointing to the right of the screen, length 1.</summary>
        private static Vector3D GetRightDirection(ProjectionCamera camera)
        {
            Vector3D rightDirection = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection);
            rightDirection.Normalize();
            return rightDirection;
        }
    }
}