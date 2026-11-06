using System.Diagnostics;
using System.IO;
using System.Windows;

namespace StarChart3D
{
    /// <summary>
    /// The main window. Loads the star data when it opens.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>The name of the star data file, copied next to the .exe on build.</summary>
        private const string StarFileName = "stars.csv";

        /// <summary>All the stars loaded from the data file.</summary>
        private readonly StarCollection starCollection = new StarCollection();

        /// <summary>Draws the stars into the 3D view.</summary>
        private readonly StarRenderer starRenderer;

        /// <summary>
        /// Creates the window and asks for the stars to be loaded once it has opened.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            starRenderer = new StarRenderer(StarViewport);
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Runs when the window has opened: loads the stars, shows the count in the title,
        /// draws the stars, and writes the count and the first 3 stars to the Output window.
        /// If loading fails, an error message is shown instead of crashing.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string filePath = System.IO.Path.Combine(AppContext.BaseDirectory, StarFileName);

            try
            {
                starCollection.Load(filePath);
            }
            catch (Exception ex) when (ex is IOException
                                       || ex is InvalidDataException
                                       || ex is UnauthorizedAccessException)
            {
                Title = "Star Chart 3D - no stars loaded";
                MessageBox.Show(this,
                    $"Could not load the star data from:\n{filePath}\n\n{ex.Message}",
                    "Star data error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Filter turned on for testing; an on/off switch comes in the next commit.
            int starsDrawn = starRenderer.Draw(starCollection, onlyProperNames: true);
            Title = $"Star Chart 3D - {starCollection.Count} stars loaded, {starsDrawn} shown";

            Debug.WriteLine($"Loaded {starCollection.Count} stars from {filePath}");
            for (int i = 0; i < 3 && i < starCollection.Count; i++)
            {
                Star star = starCollection.Stars[i];
                string name = star.ProperName.Length > 0 ? star.ProperName : "(no proper name)";
                Debug.WriteLine($"  Star {i + 1}: id {star.Id}, {name}, {star.Distance} parsecs");
            }

            WriteCatalogueNameTest();
            WriteSpectralColourTest();
        }

        /// <summary>
        /// Writes the display names of some known test stars to the Output window, next to the
        /// name each one should have, and counts how many stars have a proper or generated name.
        /// This is a test so the naming can be shown in the demo.
        /// </summary>
        private void WriteCatalogueNameTest()
        {
            (int Id, string Expected)[] testStars =
            {
                (0, "Sol"),
                (8087, "τ Cet"),
                (15419, "κ¹ Cet"),
                (7736, "p Eri A"),
                (103879, "61 Cyg A"),
                (103883, "61 Cyg B")
            };

            Debug.WriteLine("Catalogue name test:");
            foreach ((int id, string expected) in testStars)
            {
                Star? star = starCollection.Stars.FirstOrDefault(s => s.Id == id);
                if (star == null)
                {
                    Debug.WriteLine($"  id {id}: NOT FOUND");
                    continue;
                }

                string actual = CatalogueNames.GetDisplayName(star);
                string result = actual == expected ? "OK" : "WRONG";
                Debug.WriteLine($"  id {id}: {actual} (expected {expected}) {result}");
            }

            int properCount = starCollection.Stars.Count(s => !string.IsNullOrWhiteSpace(s.ProperName));
            int generatedCount = starCollection.Count - properCount;
            Debug.WriteLine($"  {properCount} stars have a proper name, {generatedCount} got a generated name");

            int duplicateNames = starCollection.Stars
                .GroupBy(s => CatalogueNames.GetDisplayName(s))
                .Count(group => group.Count() > 1);
            Debug.WriteLine($"  Names used by more than one star: {duplicateNames}");
        }

        /// <summary>
        /// Writes how many stars fall into each spectral class (and how many are unknown/grey)
        /// to the Output window, plus the class of some known stars, so the colouring can be checked.
        /// </summary>
        private void WriteSpectralColourTest()
        {
            Debug.WriteLine("Spectral colour test:");
            foreach (IGrouping<char?, Star> classGroup in starCollection.Stars
                         .GroupBy(s => SpectralColours.GetSpectralClass(s.SpectralType))
                         .OrderBy(g => "OBAFGKMLT".IndexOf(g.Key ?? '?')))
            {
                string className = classGroup.Key.HasValue ? classGroup.Key.Value.ToString() : "unknown (grey)";
                Debug.WriteLine($"  {className}: {classGroup.Count()} stars");
            }

            foreach (int id in new[] { 0, 32263, 87665, 118441 })
            {
                Star? star = starCollection.Stars.FirstOrDefault(s => s.Id == id);
                if (star != null)
                {
                    Debug.WriteLine($"  id {id} {CatalogueNames.GetDisplayName(star)}: spect '{star.SpectralType}' -> class {SpectralColours.GetSpectralClass(star.SpectralType)}");
                }
            }
        }
    }
}