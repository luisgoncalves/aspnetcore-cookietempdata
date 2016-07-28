using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace AspNetCore.Mvc.CookieTempData
{
    /// <summary>
    /// Provides cookie-based data store for the current <see cref="ITempDataDictionary"/>.
    /// </summary>
    public class CookieTempDataProvider : ITempDataProvider
    {
        private readonly string _cookieName;
        private readonly JsonSerializer _serializer;

        public CookieTempDataProvider()
        {
            _cookieName = "tmp";
            _serializer = new JsonSerializer
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            };
        }

        private static CookieOptions CookieOptionsFor(HttpContext context)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Path = context.Request.PathBase,
                Secure = context.Request.IsHttps,
            };
        }

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string cookieValue;
            if (context.Request.Cookies.TryGetValue(_cookieName, out cookieValue))
            {
                if (!string.IsNullOrWhiteSpace(cookieValue))
                {
                    var bytes = Convert.FromBase64String(cookieValue);
                    using (var ms = new MemoryStream(bytes))
                    using (var reader = new BsonReader(ms))
                    {
                        return _serializer.Deserialize<IDictionary<string, object>>(reader);
                    }
                }
                else
                {
                    context.Response.Cookies.Delete(_cookieName, CookieOptionsFor(context));
                    // TODO also do this on deserialization failures (base64, data protection)
                }
            }

            return null;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (values?.Count > 0)
            {
                using (var ms = new MemoryStream())
                {
                    using (var writer = new BsonWriter(ms))
                    {
                        _serializer.Serialize(writer, values);
                    }
                    var cookieValue = Convert.ToBase64String(ms.ToArray());
                    context.Response.Cookies.Append(_cookieName, cookieValue, CookieOptionsFor(context));
                }
            }
            else if (context.Request.Cookies.ContainsKey(_cookieName))
            {
                context.Response.Cookies.Delete(_cookieName, CookieOptionsFor(context));
            }
        }
    }
}
