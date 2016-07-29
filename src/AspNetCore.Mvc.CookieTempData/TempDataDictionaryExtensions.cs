using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AspNetCore.Mvc.CookieTempData
{
    /// <summary>
    /// Utility extensions for <see cref="ITempDataDictionary"/>.
    /// </summary>
    public static class TempDataDictionaryExtensions
    {
        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">The expected type for the value</typeparam>
        /// <param name="tempDataDictionary">The target <see cref="ITempDataDictionary"/>.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns>true if the dictionary contains an element with the specified key</returns>
        public static bool TryGetValue<T>(this ITempDataDictionary tempDataDictionary, string key, out T value)
        {
            object valueObj;
            if (tempDataDictionary.TryGetValue(key, out valueObj))
            {
                value = (T)valueObj;
                return true;
            }

            value = default(T);
            return false;
        }
    }
}
