using AspNetCore.Mvc.CookieTempData.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="CookieTempDataProvider"/> class.
        /// </summary>
        /// <param name="optionsAccessor">The configuration options</param>
        /// <param name="serializer">The BSON serializer used to serialize the temporary data</param>
        /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/> used to obtain a <see cref="IDataProtector"/> to protect cookie contents</param>
        public CookieTempDataProvider(IOptions<CookieTempDataOptions> optionsAccessor, IBsonSerializer serializer, IDataProtectionProvider dataProtectionProvider)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (dataProtectionProvider == null)
            {
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            }

            var options = optionsAccessor.Value;

            _cookieName = options.CookieName;
            _serializer = serializer;
            _baseProtector = dataProtectionProvider.CreateProtector(typeof(CookieTempDataProvider).FullName);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        private static CookieOptions CookieOptionsFor(HttpContext context) => new CookieOptions
        {
            HttpOnly = true,
            Path = context.Request.PathBase.HasValue ? context.Request.PathBase.ToString() : "/",
            Secure = context.Request.IsHttps
        };

        private IDataProtector DataProtectorFor(HttpContext context) => context.User.Identity.IsAuthenticated
            ? _baseProtector.CreateProtector(context.User.Identity.Name)
            : _baseProtector;
    }
}
