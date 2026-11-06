using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// Draws the stars of a StarCollection into a HelixViewport3D.
    /// Every star is a small sphere at its X, Y, Z position (in parsecs, Sun at 0,0,0).
    /// All spheres are put into one mesh, so 2000 stars are drawn as a single 3D model.
    /// </summary>
    public class StarRenderer
    {
        /// <summary>The radius of each star sphere, in parsecs.</summary>
        private const double StarRadius = 0.1;

        /// <summary>How many slices around each sphere. Low, because the spheres are tiny on screen.</summary>
        private const int SphereThetaDivisions = 8;

        /// <summary>How many slices from top to bottom of each sphere.</summary>
        private const int SpherePhiDivisions = 4;

        /// <summary>The 3D object that holds all the star spheres. It is added to the viewport once.</summary>
        private readonly ModelVisual3D starsVisual = new ModelVisual3D();

        /// <summary>
        /// Creates the renderer and adds its (still empty) star model to the viewport.
        /// </summary>
        /// <param name="viewport">The 3D view to draw the stars in.</param>
        public StarRenderer(HelixViewport3D viewport)
        {
            viewport.Children.Add(starsVisual);
        }

        /// <summary>
        /// Draws every star in the collection as a small white sphere.
        /// Any stars drawn before are replaced.
        /// </summary>
        /// <param name="stars">The stars to draw.</param>
        public void Draw(StarCollection stars)
        {
            MeshBuilder builder = new MeshBuilder(true, false);

            foreach (Star star in stars.Stars)
            {
                builder.AddSphere(new Point3D(star.X, star.Y, star.Z),
                                  StarRadius, SphereThetaDivisions, SpherePhiDivisions);
            }

            // Stars give off their own light, so they are drawn with an emissive (glowing) material
            // on top of a black diffuse one. This makes them look evenly bright from every side.
            MaterialGroup material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(Brushes.Black));
            material.Children.Add(new EmissiveMaterial(Brushes.White));

            starsVisual.Content = new GeometryModel3D(builder.ToMesh(true), material);
        }
    }
}