namespace StarChart3D
{
    /// <summary>
    /// The route from one star to another: distance in light years, bearing and path in degrees.
    /// Bearing and path are null when they cannot be worked out (same star, or straight up/down).
    /// </summary>
    /// <param name="DistanceLightYears">The straight-line distance in light years.</param>
    /// <param name="BearingDegrees">The angle in the equatorial (x-y) plane, from +x towards +y, 0 to 360.</param>
    /// <param name="PathDegrees">The up/down angle from the equatorial plane, -90 to +90.</param>
    public record Route(double DistanceLightYears, double? BearingDegrees, double? PathDegrees)
    {
        /// <summary>
        /// Returns the route as text, for example "4.32 ly, bearing 219.9°, path -60.8°".
        /// Angles that cannot be worked out are shown as "—".
        /// </summary>
        public string ToDisplayString()
        {
            string distance = DistanceLightYears < 0.01 ? "< 0.01 ly" : $"{DistanceLightYears:0.00} ly";
            string bearing = BearingDegrees.HasValue ? $"{BearingDegrees.Value:0.0}°" : "—";
            string path = PathDegrees.HasValue ? $"{PathDegrees.Value:0.0}°" : "—";
            return $"{distance}, bearing {bearing}, path {path}";
        }
    }

    /// <summary>
    /// Works out routes between stars, using the formulas from my domain expert
    /// (AI_domainExpert_log.md, Entry 3). HYG x, y, z are equatorial: +x points to RA 0h,
    /// +y to RA 6h and +z to the north celestial pole, all in parsecs with the Sun at 0,0,0.
    /// </summary>
    public static class Navigation
    {
        /// <summary>How many light years there are in one parsec.</summary>
        public const double LightYearsPerParsec = 3.26156;

        /// <summary>Distances smaller than this (in parsecs) are treated as zero.</summary>
        private const double Tiny = 1e-9;

        /// <summary>
        /// Works out the distance, bearing and path from one star to another.
        /// The maths uses the original HYG x, y, z, not any swapped display coordinates.
        /// </summary>
        /// <param name="from">The star you are at.</param>
        /// <param name="to">The star you want to go to.</param>
        /// <returns>The route. Bearing and path are null if they cannot be worked out.</returns>
        public static Route GetRoute(Star from, Star to)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double dz = to.Z - from.Z;

            double horizontal = Math.Sqrt(dx * dx + dy * dy);
            double distanceParsecs = Math.Sqrt(horizontal * horizontal + dz * dz);

            if (distanceParsecs < Tiny)
            {
                return new Route(0, null, null); // same place: both angles are undefined
            }

            double? bearing = null;
            if (horizontal > Tiny)
            {
                bearing = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                if (bearing < 0)
                {
                    bearing += 360.0; // keep it in 0 to 360
                }
            }

            double path = Math.Atan2(dz, horizontal) * 180.0 / Math.PI; // -90 to +90

            return new Route(distanceParsecs * LightYearsPerParsec, bearing, path);
        }
    }
}