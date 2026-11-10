using System.Windows.Media.Media3D;

namespace StarChart3D
{
    /// <summary>
    /// What the cockpit HUD shows at one moment, already turned into text lines.
    /// </summary>
    /// <param name="SpeedText">The speed in parsecs and light years per second.</param>
    /// <param name="HeadingText">The bearing and path of the direction the ship is pointing.</param>
    /// <param name="NearestStarText">The nearest star to the ship and how far away it is.</param>
    /// <param name="SunText">How far the ship is from the Sun.</param>
    /// <param name="TargetText">The target star, its distance, how far off the nose it is, and the autopilot.</param>
    public record HudReading(string SpeedText, string HeadingText, string NearestStarText, string SunText, string TargetText);

    /// <summary>
    /// Works out what the cockpit HUD shows in flight mode (Feature 4), from the camera and the ship's
    /// speed. It doesn't draw anything itself; MainWindow puts the text on screen.
    /// Heading, distances and the Sun distance all come from Navigation.GetRoute, so the angles are
    /// exactly the same as in the "Nearby stars" list (bearing in the x-y plane, path up/down from it).
    /// </summary>
    public class FlightHud
    {
        /// <summary>
        /// How far ahead of the ship the "pointing at" point is placed to work out the heading, in parsecs.
        /// Any length works, because only the direction matters.
        /// </summary>
        private const double HeadingProbeLength = 1.0;

        /// <summary>All the stars, used to find the nearest one to the ship.</summary>
        private readonly StarCollection stars;

        /// <summary>The Sun, or null if it is not in the data.</summary>
        private readonly Star? sun;

        /// <summary>
        /// Creates the HUD calculator for the loaded stars. Call it after the stars are loaded.
        /// </summary>
        /// <param name="stars">The loaded stars.</param>
        public FlightHud(StarCollection stars)
        {
            this.stars = stars;
            sun = stars.Stars.FirstOrDefault(s => s.Id == 0);
        }

        /// <summary>
        /// Works out the HUD text for where the ship (camera) is now, which way it points, and its speed.
        /// </summary>
        /// <param name="shipPosition">The camera position, in parsecs.</param>
        /// <param name="lookDirection">The direction the camera is looking.</param>
        /// <param name="speedParsecsPerSecond">The ship's speed from ShipController.</param>
        /// <param name="target">The ship's target star, or null if there is none.</param>
        /// <param name="isAutopilotOn">True if the autopilot is flying to the target.</param>
        /// <returns>The text for each HUD line.</returns>
        public HudReading Read(Point3D shipPosition, Vector3D lookDirection, double speedParsecsPerSecond,
                               Star? target, bool isAutopilotOn)
        {
            // The ship as a pretend star, so Navigation.GetRoute can be reused for every number.
            Star ship = MakePoint(shipPosition);

            double speedLightYears = speedParsecsPerSecond * Navigation.LightYearsPerParsec;
            string speedText = $"Speed: {speedParsecsPerSecond:0.00} pc/s ({speedLightYears:0.00} ly/s)";

            string headingText;
            if (lookDirection.Length < 1e-9)
            {
                headingText = "Heading: bearing —, path —";
            }
            else
            {
                Vector3D direction = lookDirection;
                direction.Normalize();
                Star ahead = MakePoint(shipPosition + direction * HeadingProbeLength);
                Route heading = Navigation.GetRoute(ship, ahead);
                headingText = $"Heading: bearing {FormatAngle(heading.BearingDegrees)}, path {FormatAngle(heading.PathDegrees)}";
            }

            string nearestText;
            Star? nearest = stars.GetNearestStar(shipPosition.X, shipPosition.Y, shipPosition.Z);
            if (nearest == null)
            {
                nearestText = "Nearest star: none";
            }
            else
            {
                Route toNearest = Navigation.GetRoute(ship, nearest);
                nearestText = $"Nearest star: {CatalogueNames.GetDisplayName(nearest)}, {FormatDistance(toNearest.DistanceLightYears)}";
            }

            string sunText = sun == null
                ? "Distance from Sol: unknown"
                : $"Distance from Sol: {FormatDistance(Navigation.GetRoute(ship, sun).DistanceLightYears)}";

            string targetText;
            if (target == null)
            {
                targetText = "Target: none (choose a star in a list)";
            }
            else
            {
                Route toTarget = Navigation.GetRoute(ship, target);

                // How far off the nose: the angle between where the ship points and the direction to the target.
                Vector3D direction = new Point3D(target.X, target.Y, target.Z) - shipPosition;
                string offNose = direction.Length < 1e-9 || lookDirection.Length < 1e-9
                    ? "—"
                    : $"{Vector3D.AngleBetween(lookDirection, direction):0.0}°";

                string autopilot = isAutopilotOn ? "AUTOPILOT ON" : "T = autopilot";
                targetText = $"Target: {CatalogueNames.GetDisplayName(target)}, {FormatDistance(toTarget.DistanceLightYears)}, {offNose} off nose  [{autopilot}]";
            }

            return new HudReading(speedText, headingText, nearestText, sunText, targetText);
        }

        /// <summary>Makes a pretend star at a point, only so its X, Y, Z can be passed to Navigation.</summary>
        private static Star MakePoint(Point3D point)
        {
            return new Star { X = point.X, Y = point.Y, Z = point.Z };
        }

        /// <summary>Shows an angle with one decimal place, or "—" if it can't be worked out.</summary>
        private static string FormatAngle(double? degrees)
        {
            return degrees.HasValue ? $"{degrees.Value:0.0}°" : "—";
        }

        /// <summary>Shows a distance in light years with two decimal places, like the Nearby stars list.</summary>
        private static string FormatDistance(double lightYears)
        {
            return lightYears < 0.01 ? "< 0.01 ly" : $"{lightYears:0.00} ly";
        }
    }
}