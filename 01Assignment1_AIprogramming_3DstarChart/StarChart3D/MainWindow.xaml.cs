using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

        /// <summary>The most matches the star search shows.</summary>
        private const int SearchResultCount = 8;

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

        /// <summary>Feature 4: works out what the cockpit HUD shows. Created once the stars are loaded.</summary>
        private FlightHud? flightHud;

        /// <summary>True while the HUD is listening for screen frames to update itself.</summary>
        private bool isHudUpdating;

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
            shipController = new ShipController(StarViewport, new ShipModel(StarViewport));
            shipController.StatusChanged += (sender, e) => ShowFlightStatus();
            shipController.StatusChanged += (sender, e) => ShowOrHideHud();
            shipController.AutopilotArrived += ShipController_AutopilotArrived;
            Loaded += MainWindow_Loaded;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            PreviewKeyUp += MainWindow_PreviewKeyUp;
            Deactivated += (sender, e) => shipController.ReleaseAllKeys();

            // Clicking the 3D view takes the keyboard away from the search box, so the ship and
            // camera keys work again.
            StarViewport.PreviewMouseDown += (sender, e) =>
            {
                if (SearchBox.IsKeyboardFocusWithin)
                {
                    MoveKeyboardOffSearchBox();
                }
            };
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
            flightHud = new FlightHud(starCollection);

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
        /// Runs when a star in the "Nearby stars" list is clicked: flies the camera to that star, or in
        /// flight mode makes it the ship's target (see ChooseStar).
        /// While the camera is already flying, the click is ignored and the selection is cleared.
        /// </summary>
        private void NearbyStarsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (NearbyStarsList.SelectedItem is not NearbyStar clicked)
            {
                return; // nothing selected, e.g. after the selection was cleared below
            }

            if (!ChooseStar(clicked.Star))
            {
                NearbyStarsList.SelectedItem = null;
            }
        }

        /// <summary>
        /// Called when a star is chosen in the Nearby stars list or the search.
        /// In flight mode the star becomes the ship's target (press T for the autopilot);
        /// otherwise the camera flies there as before.
        /// </summary>
        /// <param name="star">The chosen star.</param>
        /// <returns>False if nothing happened because the camera is already flying to a star.</returns>
        private bool ChooseStar(Star star)
        {
            if (shipController.IsFlightModeOn)
            {
                shipController.SetTarget(star);
                return true;
            }

            return FlyToStar(star);
        }

        /// <summary>
        /// Runs when the autopilot has stopped the ship near its target: that star becomes the current
        /// star and the "Nearby stars" list is filled for it. The ship stays in flight mode.
        /// </summary>
        private void ShipController_AutopilotArrived(object? sender, Star target)
        {
            currentStar = target;
            ShowNearbyStars(target);
        }

        /// <summary>
        /// Flies the camera to a star. When the flight ends, the star becomes the current star and the
        /// "Nearby stars" list updates (see CameraAnimator_AnimationCompleted). Used by the Nearby stars
        /// list and the star search.
        /// </summary>
        /// <param name="star">The star to fly to.</param>
        /// <returns>False if the camera is already flying, so nothing was started.</returns>
        private bool FlyToStar(Star star)
        {
            if (cameraAnimator.IsAnimating)
            {
                return false;
            }

            // The ship and the flight to a star would both move the camera, so leave flight mode first.
            shipController.Stop();

            destinationStar = star;
            cameraAnimator.AnimateTo(new Point3D(star.X, star.Y, star.Z), StarViewDistance, FlightSeconds);
            return true;
        }

        /// <summary>
        /// Runs whenever the text in the search box changes: shows up to 8 matching stars under the
        /// box, each with the route from the current star, or hides the list if there are none.
        /// </summary>
        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            List<Star> matches = starCollection.SearchByName(SearchBox.Text, SearchResultCount);

            SearchResultsList.ItemsSource = matches
                .Select(star => new NearbyStar(
                    star,
                    CatalogueNames.GetDisplayName(star),
                    currentStar == null ? string.Empty : Navigation.GetRoute(currentStar, star).ToDisplayString()))
                .ToList();

            SearchResultsList.Visibility = matches.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Enter in the search box flies to the first match; Esc clears the box and gives the keyboard
        /// back to the ship and camera keys.
        /// </summary>
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResultsList.Items.Count > 0)
            {
                SearchResultsList.SelectedIndex = 0; // the same as clicking the first match
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ClearSearch();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Runs when a match in the search list is clicked: flies to that star (or makes it the ship's
        /// target in flight mode), then clears the search.
        /// </summary>
        private void SearchResultsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SearchResultsList.SelectedItem is not NearbyStar clicked)
            {
                return;
            }

            if (ChooseStar(clicked.Star))
            {
                ClearSearch();
            }
            else
            {
                SearchResultsList.SelectedItem = null; // the camera is already flying: ignore the click
            }
        }

        /// <summary>
        /// Empties the search box, hides the matches and moves the keyboard back to the window.
        /// </summary>
        private void ClearSearch()
        {
            SearchBox.Text = string.Empty; // this also hides the list, through SearchBox_TextChanged
            MoveKeyboardOffSearchBox();
        }

        /// <summary>
        /// Takes the keyboard away from the search box and gives it to the window, so the ship and
        /// camera keys work again. If the window can't take it, the keyboard focus is just cleared.
        /// </summary>
        private void MoveKeyboardOffSearchBox()
        {
            Keyboard.Focus(this);
            if (SearchBox.IsKeyboardFocusWithin)
            {
                Keyboard.ClearFocus();
            }
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
        /// Runs when a key is pressed anywhere in the window. Keys typed into the search box are left alone.
        /// F turns flight mode on and off (not while the camera is flying to a star).
        /// In flight mode, ship keys go to the ShipController; otherwise camera keys go to the
        /// keyboard controller as before. While the camera is flying to a star, camera keys are
        /// ignored so they don't fight the animation.
        /// </summary>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // While typing in the search box, every key goes into the box, not to the ship or camera.
            if (SearchBox.IsKeyboardFocusWithin)
            {
                return;
            }

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
        /// Shows the HUD and crosshair while flight mode is on, and hides them when it is off.
        /// While shown, the HUD is updated once per screen frame (CompositionTarget.Rendering).
        /// </summary>
        private void ShowOrHideHud()
        {
            bool show = shipController.IsFlightModeOn && flightHud != null;

            HudPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            Crosshair.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            if (show && !isHudUpdating)
            {
                CompositionTarget.Rendering += UpdateHud;
                isHudUpdating = true;
                UpdateHud(this, EventArgs.Empty);
            }
            else if (!show && isHudUpdating)
            {
                CompositionTarget.Rendering -= UpdateHud;
                isHudUpdating = false;
            }
        }

        /// <summary>
        /// Puts the latest HUD text on screen, from where the camera is, where it looks and the ship's speed.
        /// </summary>
        private void UpdateHud(object? sender, EventArgs e)
        {
            if (flightHud == null)
            {
                return;
            }

            // The ship's own position and heading, not the camera's, so the HUD is the same in both views.
            HudReading reading = flightHud.Read(shipController.ShipPosition, shipController.ShipForward, shipController.Speed,
                                                shipController.Target, shipController.IsAutopilotOn);
            HudSpeedText.Text = reading.SpeedText;
            HudHeadingText.Text = reading.HeadingText;
            HudNearestText.Text = reading.NearestStarText;
            HudSunText.Text = reading.SunText;
            HudTargetText.Text = reading.TargetText;
        }

        /// <summary>
        /// Shows whether flight mode is on and the ship's speed in the top-left panel, with the ship
        /// keys as a reminder while flying.
        /// </summary>
        private void ShowFlightStatus()
        {
            if (shipController.IsFlightModeOn)
            {
                string view = shipController.IsChaseView ? "chase" : "cockpit";
                FlightStatusText.Text =
                    $"Flight mode: ON    View: {view}    Speed: {shipController.Speed:0.00} pc/s\n" +
                    "W/S speed, X stop, arrows pitch/turn, Q/E roll, C view, T autopilot, F exit";
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