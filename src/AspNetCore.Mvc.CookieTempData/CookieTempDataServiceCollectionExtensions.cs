using AspNetCore.Mvc.CookieTempData;
using AspNetCore.Mvc.CookieTempData.Serialization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using BuiltInSaveTempDataFilter = Microsoft.AspNetCore.Mvc.ViewFeatures.Internal.SaveTempDataFilter;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up cookie-based temp data services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class CookieTempDataServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for cookie-based temp data to the specified <see cref="IServiceCollection" />. This method
        /// should be invoked after adding MVC services to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">Configuration of <see cref="CookieTempDataOptions"/>.</param>
        public static void AddCookieTempData(this IServiceCollection services, Action<CookieTempDataOptions> setupAction = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            services.AddSingleton<IBsonSerializer, BsonSerializer>();

            /* MVC uses services.TryAddXXX to register theses services, so we can use Replace even if being configured before */

            // We need to replace built in filter for saving temp data. See the notes on CustomSaveTempDataFilter.
            services.Replace(ServiceDescriptor.Singleton<BuiltInSaveTempDataFilter, CustomSaveTempDataFilter>());
            // Replace whatever provider is configured with ours.
            services.Replace(ServiceDescriptor.Singleton<ITempDataProvider, CookieTempDataProvider>());
        }
    }
}
