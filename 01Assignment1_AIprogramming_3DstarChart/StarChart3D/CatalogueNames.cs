using System.Text;

namespace StarChart3D
{
    /// <summary>
    /// Works out the name to show for a star. Stars with a proper name keep it; other stars get a
    /// catalogue name, using the order of preference from my domain expert (see AI_domainExpert_log.md):
    /// proper name, Bayer, Flamsteed, Gliese, HD, HIP, then the HYG id.
    /// Bayer and Flamsteed names get the component letter (A, B, C) from the Gliese name, so the
    /// stars of a binary system have different names, for example "61 Cyg A" and "61 Cyg B".
    /// </summary>
    public static class CatalogueNames
    {
        /// <summary>
        /// Maps the 3-letter Greek abbreviations used in the bayer column to Greek symbols.
        /// </summary>
        private static readonly Dictionary<string, string> GreekLetters = new Dictionary<string, string>
        {
            ["Alp"] = "α",
            ["Bet"] = "β",
            ["Gam"] = "γ",
            ["Del"] = "δ",
            ["Eps"] = "ε",
            ["Zet"] = "ζ",
            ["Eta"] = "η",
            ["The"] = "θ",
            ["Iot"] = "ι",
            ["Kap"] = "κ",
            ["Lam"] = "λ",
            ["Mu"] = "μ",
            ["Nu"] = "ν",
            ["Xi"] = "ξ",
            ["Omi"] = "ο",
            ["Pi"] = "π",
            ["Rho"] = "ρ",
            ["Sig"] = "σ",
            ["Tau"] = "τ",
            ["Ups"] = "υ",
            ["Phi"] = "φ",
            ["Chi"] = "χ",
            ["Psi"] = "ψ",
            ["Ome"] = "ω"
        };

        /// <summary>The superscript digits 0 to 9, in order, used for numbers like the 1 in κ¹.</summary>
        private const string SuperscriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

        /// <summary>
        /// Returns the name to show for a star: its proper name if it has one,
        /// otherwise the best catalogue name available.
        /// </summary>
        /// <param name="star">The star to name.</param>
        /// <returns>For example "Sirius", "τ Cet", "61 Cyg A", "Gl 71", "HD 48915", "HIP 32349" or "HYG 5".</returns>
        public static string GetDisplayName(Star star)
        {
            if (HasValue(star.ProperName))
            {
                return star.ProperName;
            }

            if (HasValue(star.Bayer))
            {
                return $"{FormatBayer(star.Bayer)} {star.Constellation}".Trim() + GetComponentLetter(star);
            }

            if (HasValue(star.Flamsteed))
            {
                return $"{star.Flamsteed} {star.Constellation}".Trim() + GetComponentLetter(star);
            }

            if (HasValue(star.Gliese))
            {
                return star.Gliese;
            }

            if (HasValue(star.HenryDraper))
            {
                return $"HD {star.HenryDraper}";
            }

            if (HasValue(star.Hipparcos))
            {
                return $"HIP {star.Hipparcos}";
            }

            return $"HYG {star.Id}";
        }

        /// <summary>
        /// Returns the component letter of a star in a multiple system, taken from the end of its
        /// Gliese name, with a space in front: "Gl 820B" gives " B". Returns an empty string if
        /// the Gliese name has no letter after its number (for example "Gl 71").
        /// </summary>
        private static string GetComponentLetter(Star star)
        {
            string gliese = star.Gliese.Trim();
            if (gliese.Length >= 2
                && char.IsAsciiLetterUpper(gliese[^1])
                && char.IsAsciiDigit(gliese[^2]))
            {
                return " " + gliese[^1];
            }

            return string.Empty;
        }

        /// <summary>
        /// Turns a bayer value into its symbol form: "Tau" becomes "τ" and "Kap-1" becomes "κ¹".
        /// A value that is not a Greek abbreviation (such as "p") is returned unchanged.
        /// </summary>
        private static string FormatBayer(string bayer)
        {
            string[] parts = bayer.Split('-', 2);
            string letter = GreekLetters.TryGetValue(parts[0], out string? symbol) ? symbol : parts[0];

            if (parts.Length == 2)
            {
                letter += ToSuperscript(parts[1]);
            }

            return letter;
        }

        /// <summary>
        /// Changes the digits in a number to superscript digits, for example "1" to "¹".
        /// Any character that is not a digit is kept as it is, so it cannot crash.
        /// </summary>
        private static string ToSuperscript(string number)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in number)
            {
                result.Append(c >= '0' && c <= '9' ? SuperscriptDigits[c - '0'] : c);
            }

            return result.ToString();
        }

        /// <summary>Returns true if the text is not empty or just spaces.</summary>
        private static bool HasValue(string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }
    }
}