using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// A simple spaceship 3D model for the chase view (Feature 4), built from HelixToolkit shapes:
    /// a body with a pointed nose, two wings, a tail fin, a canopy and a glowing engine.
    /// The model is built once around (0,0,0) pointing along +X with +Y as "up"; each frame
    /// SetPose moves and turns it to where the ship is. It is about 0.09 parsecs long, smaller
    /// than one star sphere (0.2 parsecs wide), so it fits between the stars.
    /// </summary>
    public class ShipModel
    {
        /// <summary>The colour of the ship's body, wings and fin.</summary>
        private static readonly Color HullColour = Color.FromRgb(170, 180, 200);

        /// <summary>The colour of the cockpit canopy.</summary>
        private static readonly Color CanopyColour = Color.FromRgb(90, 160, 255);

        /// <summary>The colour of the engine glow.</summary>
        private static readonly Color EngineColour = Color.FromRgb(255, 140, 40);

        /// <summary>The 3D object added to the viewport. Its Content is null while the ship is hidden.</summary>
        private readonly ModelVisual3D visual = new ModelVisual3D();

        /// <summary>The ship's shapes, built once.</summary>
        private readonly Model3DGroup model;

        /// <summary>Moves and turns the model to the ship's position and direction.</summary>
        private readonly MatrixTransform3D poseTransform = new MatrixTransform3D();

        /// <summary>
        /// Builds the ship model and adds it to the viewport, hidden.
        /// </summary>
        /// <param name="viewport">The 3D view to show the ship in.</param>
        public ShipModel(HelixViewport3D viewport)
        {
            model = BuildModel();
            model.Transform = poseTransform;
            viewport.Children.Add(visual);
        }

        /// <summary>Gets or sets a value showing whether the ship can be seen.</summary>
        public bool IsVisible
        {
            get => visual.Content != null;
            set => visual.Content = value ? model : null;
        }

        /// <summary>
        /// Moves the model to the ship's position and turns it to point along forward, with its top
        /// towards up.
        /// </summary>
        /// <param name="position">Where the ship is, in parsecs.</param>
        /// <param name="forward">The way the ship points (length 1).</param>
        /// <param name="up">The ship's up direction (length 1, at right angles to forward).</param>
        public void SetPose(Point3D position, Vector3D forward, Vector3D up)
        {
            Vector3D right = Vector3D.CrossProduct(forward, up);

            // Each row says where one of the model's own axes ends up: +X -> forward, +Y -> up,
            // +Z -> right; the last row moves it to the ship's position.
            poseTransform.Matrix = new Matrix3D(
                forward.X, forward.Y, forward.Z, 0,
                up.X, up.Y, up.Z, 0,
                right.X, right.Y, right.Z, 0,
                position.X, position.Y, position.Z, 1);
        }

        /// <summary>
        /// Builds the ship's shapes around (0,0,0), pointing along +X, with +Y up and wings along Z.
        /// Sizes are in parsecs.
        /// </summary>
        private static Model3DGroup BuildModel()
        {
            MeshBuilder hull = new MeshBuilder();
            hull.AddCylinder(new Point3D(-0.040, 0, 0), new Point3D(0.020, 0, 0), 0.016, 16); // body
            hull.AddCone(new Point3D(0.020, 0, 0), new Vector3D(1, 0, 0), 0.008, 0, 0.025, false, false, 16); // nose
            hull.AddBox(new Point3D(-0.012, 0, 0), 0.030, 0.003, 0.070); // wings, across the body
            hull.AddBox(new Point3D(-0.032, 0.010, 0), 0.016, 0.018, 0.002); // tail fin, on top

            MeshBuilder canopy = new MeshBuilder();
            canopy.AddSphere(new Point3D(0.010, 0.007, 0), 0.005, 12, 8);

            MeshBuilder engine = new MeshBuilder();
            engine.AddSphere(new Point3D(-0.042, 0, 0), 0.007, 12, 8);

            Model3DGroup group = new Model3DGroup();
            group.Children.Add(new GeometryModel3D(hull.ToMesh(true), MaterialHelper.CreateMaterial(HullColour)));
            group.Children.Add(new GeometryModel3D(canopy.ToMesh(true), MaterialHelper.CreateMaterial(CanopyColour)));
            group.Children.Add(new GeometryModel3D(engine.ToMesh(true), CreateGlowMaterial(EngineColour)));
            return group;
        }

        /// <summary>
        /// A material that glows in its own colour, so the engine is bright even without light on it.
        /// </summary>
        private static Material CreateGlowMaterial(Color colour)
        {
            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(Brushes.Black));
            material.Children.Add(new EmissiveMaterial(new SolidColorBrush(colour)));
            return material;
        }
    }
}