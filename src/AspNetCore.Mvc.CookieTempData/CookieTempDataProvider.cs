using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;
using Microsoft.AspNetCore.DataProtection;

namespace AspNetCore.Mvc.CookieTempData
{
    /// <summary>
    /// Provides cookie-based data store for the current <see cref="ITempDataDictionary"/>.
    /// </summary>
    public class CookieTempDataProvider : ITempDataProvider
    {
        private readonly string _cookieName;
        private readonly JsonSerializer _serializer;
        private readonly IDataProtector _baseProtector;

        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider)
        {
            if (dataProtectionProvider == null)
            {
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            }

            _cookieName = "tmp";
            _serializer = new JsonSerializer
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            };
            _baseProtector = dataProtectionProvider.CreateProtector(typeof(CookieTempDataProvider).FullName, "v1");
        }

        private static CookieOptions CookieOptionsFor(HttpContext context) => new CookieOptions
        {
            HttpOnly = true,
            Path = context.Request.PathBase.HasValue ? context.Request.PathBase.ToString() : "/",
            Secure = context.Request.IsHttps,
        };

        private IDataProtector DataProtectorFor(HttpContext context) => context.User.Identity.IsAuthenticated
            ? _baseProtector.CreateProtector(context.User.Identity.Name)
            : _baseProtector;

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
                    bytes = DataProtectorFor(context).Unprotect(bytes);
                    return Deserialize(bytes);
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
                var bytes = Serialize(values);
                bytes = DataProtectorFor(context).Protect(bytes);
                var cookieValue = Convert.ToBase64String(bytes);
                context.Response.Cookies.Append(_cookieName, cookieValue, CookieOptionsFor(context));
            }
            else if (context.Request.Cookies.ContainsKey(_cookieName))
            {
                context.Response.Cookies.Delete(_cookieName, CookieOptionsFor(context));
            }
        }

        private IDictionary<string, object> Deserialize(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BsonReader(ms))
            {
                return _serializer.Deserialize<IDictionary<string, object>>(reader);
            }
        }

        private byte[] Serialize(IDictionary<string, object> values)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BsonWriter(ms))
                {
                    _serializer.Serialize(writer, values);
                }
                return ms.ToArray();
            }
        }
    }
}
