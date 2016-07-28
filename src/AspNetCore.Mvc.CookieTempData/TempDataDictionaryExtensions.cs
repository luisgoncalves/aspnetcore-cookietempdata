using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AspNetCore.Mvc.CookieTempData
{
    public static class TempDataDictionaryExtensions
    {
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
