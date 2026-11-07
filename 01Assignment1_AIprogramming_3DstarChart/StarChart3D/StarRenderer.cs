using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace StarChart3D
{
    /// <summary>
    /// Draws the stars of a StarCollection into a HelixViewport3D.
    /// Every star is a small sphere at its X, Y, Z position (in parsecs, Sun at 0,0,0),
    /// coloured by its spectral class, with its name next to it. Stars of the same colour share
    /// one mesh, and all names are drawn as one group, so 2000 stars stay fast.
    /// It can be told to draw only the stars that have a proper name.
    /// </summary>
    public class StarRenderer
    {
        /// <summary>The radius of each star sphere, in parsecs.</summary>
        private const double StarRadius = 0.1;

        /// <summary>How many slices around each sphere. Low, because the spheres are tiny on screen.</summary>
        private const int SphereThetaDivisions = 8;

        /// <summary>How many slices from top to bottom of each sphere.</summary>
        private const int SpherePhiDivisions = 4;

        /// <summary>How far above the centre of a star its name is placed, in parsecs.</summary>
        private const double LabelOffset = 0.15;

        /// <summary>The font size of the star names.</summary>
        private const double LabelFontSize = 10;

        /// <summary>The 3D object that holds all the star spheres. It is added to the viewport once.</summary>
        private readonly ModelVisual3D starsVisual = new ModelVisual3D();

        /// <summary>
        /// The 3D object that holds all the star names. A billboard text always faces the camera.
        /// </summary>
        private readonly BillboardTextGroupVisual3D labelsVisual = new BillboardTextGroupVisual3D
        {
            Foreground = Brushes.LightGray,
            Background = Brushes.Transparent,
            FontSize = LabelFontSize
        };

        /// <summary>
        /// Creates the renderer and adds its (still empty) star model and labels to the viewport.
        /// </summary>
        /// <param name="viewport">The 3D view to draw the stars in.</param>
        public StarRenderer(HelixViewport3D viewport)
        {
            viewport.Children.Add(starsVisual);
            viewport.Children.Add(labelsVisual);
        }

        /// <summary>
        /// Draws the stars as small spheres in their spectral class colour, each with its display
        /// name next to it. Any stars drawn before are replaced.
        /// </summary>
        /// <param name="stars">The stars to draw.</param>
        /// <param name="onlyProperNames">
        /// If true, only stars with a proper name (such as "Sirius") are drawn; stars that only
        /// have a generated catalogue name are hidden, both sphere and label.
        /// </param>
        /// <returns>How many stars were drawn.</returns>
        public int Draw(StarCollection stars, bool onlyProperNames)
        {
            List<Star> starsToDraw = stars.Stars
                .Where(star => !onlyProperNames || HasProperName(star))
                .ToList();

            DrawSpheres(starsToDraw);
            DrawLabels(starsToDraw);

            return starsToDraw.Count;
        }

        /// <summary>Returns true if the star has a proper name in the data.</summary>
        private static bool HasProperName(Star star)
        {
            return !string.IsNullOrWhiteSpace(star.ProperName);
        }

        /// <summary>
        /// Draws the star spheres, one mesh per spectral class colour.
        /// </summary>
        private void DrawSpheres(IEnumerable<Star> stars)
        {
            Model3DGroup allStars = new Model3DGroup();

            foreach (IGrouping<Color, Star> colourGroup in stars.GroupBy(SpectralColours.GetColour))
            {
                MeshBuilder builder = new MeshBuilder(true, false);
                foreach (Star star in colourGroup)
                {
                    builder.AddSphere(new Point3D(star.X, star.Y, star.Z),
                                      StarRadius, SphereThetaDivisions, SpherePhiDivisions);
                }

                allStars.Children.Add(new GeometryModel3D(builder.ToMesh(true), CreateGlowMaterial(colourGroup.Key)));
            }

            starsVisual.Content = allStars;
        }

        /// <summary>
        /// Puts each star's display name (from CatalogueNames) just above its sphere.
        /// All labels are given to one BillboardTextGroupVisual3D so they are drawn together.
        /// </summary>
        private void DrawLabels(IEnumerable<Star> stars)
        {
            List<BillboardTextItem> labels = new List<BillboardTextItem>();

            foreach (Star star in stars)
            {
                labels.Add(new BillboardTextItem
                {
                    Text = CatalogueNames.GetDisplayName(star),
                    Position = new Point3D(star.X, star.Y + LabelOffset, star.Z),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom
                });
            }

            labelsVisual.Items = labels;
        }

        /// <summary>
        /// Makes a glowing material in the given colour. Stars give off their own light, so an
        /// emissive material is put on top of a black diffuse one, so they look evenly bright.
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