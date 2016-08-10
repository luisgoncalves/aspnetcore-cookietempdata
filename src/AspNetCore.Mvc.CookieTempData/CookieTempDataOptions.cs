namespace AspNetCore.Mvc.CookieTempData
{
    /// <summary>
    /// Configuration for cookie-based TempData.
    /// </summary>
    public class CookieTempDataOptions
    {
        /// <summary>
        /// Gets or sets the name of the cookie used for storage. The default value is "tmp".
        /// </summary>
        public string CookieName { get; set; } = "tmp";
    }
}
