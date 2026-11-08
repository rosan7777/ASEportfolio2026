namespace StarChart3D
{
    /// <summary>
    /// One row of the "Nearby stars" list: a star, its display name and the route to it
    /// from the current star as text (distance, bearing and path).
    /// </summary>
    /// <param name="Star">The nearby star.</param>
    /// <param name="Name">The star's display name, from CatalogueNames.</param>
    /// <param name="RouteText">The route from the current star, from Navigation.GetRoute.</param>
    public record NearbyStar(Star Star, string Name, string RouteText);
}