using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace StarChart3D
{
    /// <summary>
    /// The main window. Loads the star data when it opens and draws it in 3D.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>The name of the star data file, copied next to the .exe on build.</summary>
        private const string StarFileName = "stars.csv";

        /// <summary>How far from a star the camera stops after flying to it, in parsecs.</summary>
        private const double StarViewDistance = 2.0;

        /// <summary>How long a flight to a star takes, in seconds.</summary>
        private const double FlightSeconds = 3.0;

        /// <summary>How many stars the "Nearby stars" list shows.</summary>
        private const int NearbyStarCount = 10;

        /// <summary>All the stars loaded from the data file.</summary>
        private readonly StarCollection starCollection = new StarCollection();

        /// <summary>Draws the stars into the 3D view.</summary>
        private readonly StarRenderer starRenderer;

        /// <summary>Moves the 3D camera with the keyboard.</summary>
        private readonly KeyboardCameraController keyboardCameraController;

        /// <summary>Flies the 3D camera smoothly to a star.</summary>
        private readonly CameraAnimator cameraAnimator;

        /// <summary>Draws the moving space dust that gives the feeling of travelling.</summary>
        private readonly DustField dustField;

        /// <summary>Feature 4: flies the camera like a spaceship when flight mode is on.</summary>
        private readonly ShipController shipController;

        /// <summary>The star the user is currently at. Starts as the Sun.</summary>
        private Star? currentStar;

        /// <summary>
        /// The star the camera is flying to after a click in the list. It becomes the current star
        /// when the flight has finished. Null when no flight to a clicked star is happening.
        /// </summary>
        private Star? destinationStar;

        /// <summary>
        /// Creates the window and asks for the stars to be loaded once it has opened.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            starRenderer = new StarRenderer(StarViewport);
            keyboardCameraController = new KeyboardCameraController(StarViewport);
            cameraAnimator = new CameraAnimator(StarViewport);
            cameraAnimator.AnimationCompleted += CameraAnimator_AnimationCompleted;
            dustField = new DustField(StarViewport);
            shipController = new ShipController(StarViewport);
            shipController.StatusChanged += (sender, e) => ShowFlightStatus();
            Loaded += MainWindow_Loaded;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            PreviewKeyUp += MainWindow_PreviewKeyUp;
            Deactivated += (sender, e) => shipController.ReleaseAllKeys();
            ShowFlightStatus();
        }

        /// <summary>
        /// Runs when the window has opened: loads the stars, draws them, flies the camera to the
        /// Sun, lists the nearby stars, and writes the tests to the Output window.
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

            RedrawStars();

            // Feature 3: start the dust drifting, to give the illusion of slow motion through space.
            dustField.Start();

            // Feature 3: animate from the overview towards the initial star system, the Sun,
            // and list the stars near it.
            Star? sun = starCollection.Stars.FirstOrDefault(s => s.Id == 0);
            if (sun != null)
            {
                currentStar = sun;
                cameraAnimator.AnimateTo(new Point3D(sun.X, sun.Y, sun.Z), StarViewDistance, FlightSeconds);
                ShowNearbyStars(sun);
            }

            Debug.WriteLine($"Loaded {starCollection.Count} stars from {filePath}");
            for (int i = 0; i < 3 && i < starCollection.Count; i++)
            {
                Star star = starCollection.Stars[i];
                string name = star.ProperName.Length > 0 ? star.ProperName : "(no proper name)";
                Debug.WriteLine($"  Star {i + 1}: id {star.Id}, {name}, {star.Distance} parsecs");
            }

            WriteCatalogueNameTest();
            WriteSpectralColourTest();
            WriteRouteTest();
        }

        /// <summary>
        /// Draws the stars again, using the filter checkbox to decide whether only stars with a
        /// proper name are shown, and shows how many stars are drawn in the title.
        /// </summary>
        private void RedrawStars()
        {
            bool onlyProperNames = ProperNameFilterCheckBox.IsChecked == true;
            int starsDrawn = starRenderer.Draw(starCollection, onlyProperNames);
            Title = $"Star Chart 3D - {starCollection.Count} stars loaded, {starsDrawn} shown";
        }

        /// <summary>
        /// Shows the current star above the list, and fills the "Nearby stars" list with the
        /// stars nearest to it and the route to each (distance, bearing and path).
        /// </summary>
        /// <param name="current">The star the user is at.</param>
        private void ShowNearbyStars(Star current)
        {
            CurrentStarText.Text = $"Current star: {CatalogueNames.GetDisplayName(current)}";

            NearbyStarsList.ItemsSource = starCollection
                .GetNearestStars(current, NearbyStarCount)
                .Select(star => new NearbyStar(
                    star,
                    CatalogueNames.GetDisplayName(star),
                    Navigation.GetRoute(current, star).ToDisplayString()))
                .ToList();
        }

        /// <summary>
        /// Runs when a star in the "Nearby stars" list is clicked: flies the camera to that star.
        /// The list is only updated when the flight has finished (see CameraAnimator_AnimationCompleted).
        /// While the camera is already flying, the click is ignored and the selection is cleared.
        /// </summary>
        private void NearbyStarsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (NearbyStarsList.SelectedItem is not NearbyStar clicked)
            {
                return; // nothing selected, e.g. after the selection was cleared below
            }

            if (cameraAnimator.IsAnimating)
            {
                NearbyStarsList.SelectedItem = null;
                return;
            }

            // The ship and the flight to a star would both move the camera, so leave flight mode first.
            shipController.Stop();

            destinationStar = clicked.Star;
            Star star = clicked.Star;
            cameraAnimator.AnimateTo(new Point3D(star.X, star.Y, star.Z), StarViewDistance, FlightSeconds);
        }

        /// <summary>
        /// Runs when the camera has finished flying. If it was flying to a star clicked in the list,
        /// that star becomes the current star and the "Nearby stars" list is filled for it.
        /// </summary>
        private void CameraAnimator_AnimationCompleted(object? sender, EventArgs e)
        {
            if (destinationStar == null)
            {
                return; // the first flight to the Sun: the list was already filled for it
            }

            currentStar = destinationStar;
            destinationStar = null;
            ShowNearbyStars(currentStar);
        }

        /// <summary>
        /// Runs when a key is pressed anywhere in the window.
        /// F turns flight mode on and off (not while the camera is flying to a star).
        /// In flight mode, ship keys go to the ShipController; otherwise camera keys go to the
        /// keyboard controller as before. While the camera is flying to a star, camera keys are
        /// ignored so they don't fight the animation.
        /// </summary>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F)
            {
                ToggleFlightMode();
                e.Handled = true;
                return;
            }

            if (shipController.IsFlightModeOn)
            {
                if (shipController.HandleKeyDown(e.Key))
                {
                    e.Handled = true;
                }

                return;
            }

            if (!cameraAnimator.IsAnimating && keyboardCameraController.HandleKey(e.Key))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Runs when a key is let go anywhere in the window, so the ship knows a key is no longer held.
        /// </summary>
        private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (shipController.HandleKeyUp(e.Key))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Turns flight mode on or off. It can't be turned on while the camera is flying to a star.
        /// </summary>
        private void ToggleFlightMode()
        {
            if (shipController.IsFlightModeOn)
            {
                shipController.Stop();
            }
            else if (!cameraAnimator.IsAnimating)
            {
                shipController.Start();
            }
        }

        /// <summary>
        /// Shows whether flight mode is on and the ship's speed in the top-left panel, with the ship
        /// keys as a reminder while flying.
        /// </summary>
        private void ShowFlightStatus()
        {
            if (shipController.IsFlightModeOn)
            {
                FlightStatusText.Text =
                    $"Flight mode: ON    Speed: {shipController.Speed:0.00} pc/s\n" +
                    "W/S speed, X stop, arrows pitch/turn, Q/E roll, F exit";
            }
            else
            {
                FlightStatusText.Text = "Flight mode: OFF (press F to fly)";
            }
        }

        /// <summary>
        /// Runs when the "Only show stars with a proper name" checkbox is ticked or unticked.
        /// </summary>
        private void ProperNameFilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            RedrawStars();
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

        /// <summary>
        /// Writes some routes from the Sun to the Output window, so the distance, bearing and path
        /// can be checked against the domain expert's example (Sun to Rigil Kentaurus).
        /// </summary>
        private void WriteRouteTest()
        {
            Debug.WriteLine("Route test:");
            Star? sun = starCollection.Stars.FirstOrDefault(s => s.Id == 0);
            if (sun == null)
            {
                Debug.WriteLine("  Sun not found");
                return;
            }

            foreach (int id in new[] { 71456, 70666, 32263, 87665, 71453 })
            {
                Star? target = starCollection.Stars.FirstOrDefault(s => s.Id == id);
                if (target != null)
                {
                    Debug.WriteLine($"  Sol -> {CatalogueNames.GetDisplayName(target)}: {Navigation.GetRoute(sun, target).ToDisplayString()}");
                }
            }

            Debug.WriteLine($"  Sol -> Sol: {Navigation.GetRoute(sun, sun).ToDisplayString()}");
        }
    }
}