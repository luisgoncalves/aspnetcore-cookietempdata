using AspNetCore.Mvc.CookieTempData.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AspNetCore.Mvc.CookieTempData
{
    /// <summary>
    /// Provides cookie-based data store for the current <see cref="ITempDataDictionary"/>.
    /// </summary>
    public class CookieTempDataProvider : ITempDataProvider
    {
        private readonly string _cookieName;
        private readonly IBsonSerializer _serializer;
        private readonly IDataProtector _baseProtector;

        public CookieTempDataProvider(IBsonSerializer serializer, IDataProtectionProvider dataProtectionProvider)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (dataProtectionProvider == null)
            {
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            }

            _cookieName = "tmp";
            _serializer = serializer;
            _baseProtector = dataProtectionProvider.CreateProtector(typeof(CookieTempDataProvider).FullName);
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
                    try
                    {
                        var bytes = Convert.FromBase64String(cookieValue);
                        bytes = DataProtectorFor(context).Unprotect(bytes);
                        return _serializer.Deserialize<IDictionary<string, object>>(bytes);
                    }
                    catch (FormatException)
                    {
                        // Invalid base 64 string. Fall through.
                    }
                    catch (CryptographicException)
                    {
                        // Invalid protected payload. Fall through.
                    }
                }

                context.Response.Cookies.Delete(_cookieName, CookieOptionsFor(context));
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
                var bytes = _serializer.Serialize(values);
                bytes = DataProtectorFor(context).Protect(bytes);
                var cookieValue = Convert.ToBase64String(bytes);
                context.Response.Cookies.Append(_cookieName, cookieValue, CookieOptionsFor(context));
            }
            else if (context.Request.Cookies.ContainsKey(_cookieName))
            {
                context.Response.Cookies.Delete(_cookieName, CookieOptionsFor(context));
            }
        }
    }
}
