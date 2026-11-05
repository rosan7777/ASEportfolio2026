namespace StarChart3D
{
    /// <summary>
    /// Holds the data for one star read from stars.csv (HYG database v4.4).
    /// This class only stores data; reading the file is done by StarCollection.
    /// </summary>
    public class Star
    {
        /// <summary>
        /// Gets or sets the HYG database ID of the star (column "id"). Sol is 0.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the star's proper (common) name, such as "Sirius" (column "proper").
        /// Empty if the star has no proper name.
        /// </summary>
        public string ProperName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the distance from the Sun in parsecs (column "dist").
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Gets or sets the X coordinate in parsecs, with the Sun at (0,0,0) (column "x").
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate in parsecs, with the Sun at (0,0,0) (column "y").
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the Z coordinate in parsecs, with the Sun at (0,0,0) (column "z").
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// Gets or sets the spectral type, such as "G2V" (column "spect").
        /// Empty if not known.
        /// </summary>
        public string SpectralType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Bayer designation, such as "Tau" (column "bayer"). Empty if none.
        /// </summary>
        public string Bayer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Flamsteed number (column "flam"). Empty if none.
        /// </summary>
        public string Flamsteed { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the three-letter constellation abbreviation, such as "Cet" (column "con").
        /// Empty if none.
        /// </summary>
        public string Constellation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Gliese catalogue name, such as "Gl 71" (column "gl"). Empty if none.
        /// </summary>
        public string Gliese { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Hipparcos catalogue number (column "hip"). Empty if none.
        /// </summary>
        public string Hipparcos { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Henry Draper catalogue number (column "hd"). Empty if none.
        /// </summary>
        public string HenryDraper { get; set; } = string.Empty;
    }
}