using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace StarChart3D
{
    /// <summary>
    /// Works out a star's spectral class (O, B, A, F, G, K, M, L, T) from its spect value and
    /// gives the colour to draw it in. The rules come from my domain expert (AI_domainExpert_log.md,
    /// Entry 2). Stars whose class cannot be worked out are drawn grey.
    /// </summary>
    public static class SpectralColours
    {
        /// <summary>
        /// The "chart-style" (saturated) colour for each spectral class, from my domain expert.
        /// </summary>
        private static readonly Dictionary<char, Color> ClassColours = new Dictionary<char, Color>
        {
            ['O'] = Color.FromRgb(0x6B, 0x8C, 0xFF), // blue
            ['B'] = Color.FromRgb(0x9D, 0xB4, 0xFF), // blue-white
            ['A'] = Color.FromRgb(0xE0, 0xE8, 0xFF), // white
            ['F'] = Color.FromRgb(0xFF, 0xF8, 0xE7), // yellow-white
            ['G'] = Color.FromRgb(0xFF, 0xE8, 0x6B), // yellow
            ['K'] = Color.FromRgb(0xFF, 0xAA, 0x55), // orange
            ['M'] = Color.FromRgb(0xFF, 0x6A, 0x4D), // red
            ['L'] = Color.FromRgb(0xB0, 0x30, 0x20), // dark red (brown dwarf)
            ['T'] = Color.FromRgb(0x8A, 0x3A, 0x6A)  // magenta-brown (brown dwarf)
        };

        /// <summary>The grey used for stars whose spectral class is unknown.</summary>
        public static readonly Color UnknownColour = Color.FromRgb(0xB0, 0xB0, 0xB0);

        /// <summary>Finds the temperature number in a white dwarf type, e.g. the 2 in "DA2".</summary>
        private static readonly Regex WhiteDwarfNumber = new Regex(@"^D[A-Z]*(\d+(\.\d+)?)");

        /// <summary>
        /// Finds old prefixes before the class letter: d (dwarf), sd/esd/usd (subdwarfs), c, g.
        /// Only matches when a capital letter follows, so a lone "g" is not removed.
        /// </summary>
        private static readonly Regex OldPrefix = new Regex(@"^(esd|usd|sd|d|c|g)(?=[A-Z])");

        /// <summary>
        /// Returns the colour to draw a star in, based on its spectral class.
        /// </summary>
        /// <param name="star">The star.</param>
        /// <returns>The class colour, or grey if the class is unknown.</returns>
        public static Color GetColour(Star star)
        {
            char? spectralClass = GetSpectralClass(star.SpectralType);
            return spectralClass.HasValue ? ClassColours[spectralClass.Value] : UnknownColour;
        }

        /// <summary>
        /// Returns the spectral class letter for a spect value, for example "G2V" gives 'G',
        /// "dM5.5e" gives 'M', "m" gives 'M', "S" gives 'M' and "DA2" (a white dwarf) gives 'B'.
        /// </summary>
        /// <param name="spectralType">The spect value from stars.csv.</param>
        /// <returns>The class letter, or null if it is blank or not known (for example "pec").</returns>
        public static char? GetSpectralClass(string spectralType)
        {
            string spect = spectralType.Trim();
            if (spect.Length == 0)
            {
                return null;
            }

            if (spect[0] == 'D')
            {
                return GetWhiteDwarfClass(spect);
            }

            spect = OldPrefix.Replace(spect, string.Empty);
            char letter = char.ToUpperInvariant(spect[0]);

            if (letter == 'S')
            {
                return 'M'; // S-type stars are about as cool as M stars
            }

            if (ClassColours.ContainsKey(letter))
            {
                return letter;
            }

            return null;
        }

        /// <summary>
        /// Works out the class of a white dwarf from its temperature number n, using
        /// temperature = 50400 / n (from my domain expert). With no number it counts as A (white).
        /// </summary>
        private static char GetWhiteDwarfClass(string spect)
        {
            Match match = WhiteDwarfNumber.Match(spect);
            if (!match.Success)
            {
                return 'A';
            }

            double number = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            if (number <= 0)
            {
                return 'A';
            }

            return GetClassFromTemperature(50400 / number);
        }

        /// <summary>
        /// Returns the spectral class for a surface temperature in kelvin,
        /// using the temperature ranges from my domain expert.
        /// </summary>
        private static char GetClassFromTemperature(double kelvin)
        {
            if (kelvin >= 30000) return 'O';
            if (kelvin >= 10000) return 'B';
            if (kelvin >= 7500) return 'A';
            if (kelvin >= 6000) return 'F';
            if (kelvin >= 5200) return 'G';
            if (kelvin >= 3700) return 'K';
            return 'M';
        }
    }
}