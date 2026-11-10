using System.Globalization;
using System.IO;

namespace StarChart3D
{
    /// <summary>
    /// Holds all the stars loaded from stars.csv and reads the file into Star objects.
    /// </summary>
    public class StarCollection
    {
        /// <summary>The number of columns every data line in stars.csv must have.</summary>
        private const int ExpectedColumns = 37;

        // Column numbers in stars.csv (counted from 0).
        private const int ColId = 0;
        private const int ColHip = 1;
        private const int ColHd = 2;
        private const int ColGl = 4;
        private const int ColProper = 6;
        private const int ColDist = 9;
        private const int ColSpect = 15;
        private const int ColX = 17;
        private const int ColY = 18;
        private const int ColZ = 19;
        private const int ColBayer = 27;
        private const int ColFlam = 28;
        private const int ColCon = 29;

        private readonly List<Star> stars = new List<Star>();

        /// <summary>
        /// Gets the loaded stars. Read-only, so other classes cannot add or remove stars.
        /// </summary>
        public IReadOnlyList<Star> Stars => stars;

        /// <summary>
        /// Gets the number of stars loaded.
        /// </summary>
        public int Count => stars.Count;

        /// <summary>
        /// Reads stars.csv and creates one Star for each data line.
        /// The header line is skipped. Any stars loaded before are cleared first.
        /// </summary>
        /// <param name="filePath">The path to stars.csv.</param>
        /// <exception cref="InvalidDataException">
        /// Thrown if a line does not have 37 columns, or a required number (id, dist, x, y, z) is not valid.
        /// </exception>
        public void Load(string filePath)
        {
            stars.Clear();

            string[] lines = File.ReadAllLines(filePath);

            // Start at 1 to skip the header line.
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split(',');
                int lineNumber = i + 1;

                if (parts.Length != ExpectedColumns)
                {
                    throw new InvalidDataException(
                        $"Line {lineNumber} has {parts.Length} columns, expected {ExpectedColumns}.");
                }

                stars.Add(new Star
                {
                    Id = ParseInt(parts[ColId], "id", lineNumber),
                    Hipparcos = Clean(parts[ColHip]),
                    HenryDraper = Clean(parts[ColHd]),
                    Gliese = Clean(parts[ColGl]),
                    ProperName = Clean(parts[ColProper]),
                    Distance = ParseDouble(parts[ColDist], "dist", lineNumber),
                    SpectralType = Clean(parts[ColSpect]),
                    X = ParseDouble(parts[ColX], "x", lineNumber),
                    Y = ParseDouble(parts[ColY], "y", lineNumber),
                    Z = ParseDouble(parts[ColZ], "z", lineNumber),
                    Bayer = Clean(parts[ColBayer]),
                    Flamsteed = Clean(parts[ColFlam]),
                    Constellation = Clean(parts[ColCon])
                });
            }
        }

        /// <summary>
        /// Returns the stars nearest to a given star, closest first. The star itself is left out.
        /// </summary>
        /// <param name="from">The star to measure from.</param>
        /// <param name="count">How many stars to return.</param>
        /// <returns>Up to count stars, sorted by distance from the given star.</returns>
        public List<Star> GetNearestStars(Star from, int count)
        {
            return stars
                .Where(star => star != from)
                .OrderBy(star => DistanceSquared(from, star))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Finds stars whose display name (from CatalogueNames) contains the search text, ignoring
        /// upper and lower case (Feature 4). Names that start with the text come first, then the
        /// rest; within each group the stars closest to the Sun come first.
        /// </summary>
        /// <param name="text">Part of a star name, for example "sir".</param>
        /// <param name="maxResults">The most stars to return.</param>
        /// <returns>Up to maxResults matching stars. Empty if the text is blank.</returns>
        public List<Star> SearchByName(string text, int maxResults)
        {
            string search = text.Trim();
            if (search.Length == 0)
            {
                return new List<Star>();
            }

            return stars
                .Select(star => new { Star = star, Name = CatalogueNames.GetDisplayName(star) })
                .Where(match => match.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(match => match.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(match => match.Star.Distance)
                .Take(maxResults)
                .Select(match => match.Star)
                .ToList();
        }

        /// <summary>
        /// Returns the star nearest to a point in space, for example where the ship is (Feature 4).
        /// </summary>
        /// <param name="x">The point's x, in parsecs.</param>
        /// <param name="y">The point's y, in parsecs.</param>
        /// <param name="z">The point's z, in parsecs.</param>
        /// <returns>The nearest star, or null if no stars are loaded.</returns>
        public Star? GetNearestStar(double x, double y, double z)
        {
            Star? nearest = null;
            double nearestDistanceSquared = double.MaxValue;

            foreach (Star star in stars)
            {
                double dx = star.X - x;
                double dy = star.Y - y;
                double dz = star.Z - z;
                double distanceSquared = dx * dx + dy * dy + dz * dz;

                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearest = star;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Returns the squared distance between two stars, in parsecs squared. The square root is not
        /// needed just to sort by distance, so it is skipped to save time.
        /// </summary>
        private static double DistanceSquared(Star a, Star b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Removes spaces and surrounding quote marks from a field. A blank field becomes an empty string.
        /// </summary>
        private static string Clean(string field)
        {
            return field.Trim().Trim('"');
        }

        /// <summary>
        /// Converts a required number field to a double, using "." as the decimal point.
        /// </summary>
        private static double ParseDouble(string field, string columnName, int lineNumber)
        {
            string value = Clean(field);
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            throw new InvalidDataException($"Line {lineNumber}: '{columnName}' is not a valid number: '{value}'.");
        }

        /// <summary>
        /// Converts a required whole-number field to an int.
        /// </summary>
        private static int ParseInt(string field, string columnName, int lineNumber)
        {
            string value = Clean(field);
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            throw new InvalidDataException($"Line {lineNumber}: '{columnName}' is not a valid whole number: '{value}'.");
        }
    }
}