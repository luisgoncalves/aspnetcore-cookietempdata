using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading.Tasks;

namespace AspNetCore.Mvc.CookieTempData
{
    /// <summary>
    /// By default MVC saves temp data via <see cref="IResourceFilter.OnResourceExecuted(ResourceExecutedContext)"/>. This might
    /// be too late to access response cookies because the action result already executed and the response may have already started.
    /// This class antecipates the moment of saving temp data so that response cookies can still be accessed. The drawback is that
    /// we cannot use <see cref="IResultFilter.OnResultExecuted(ResultExecutedContext)"/> to modify temp data, because it has already
    /// been saved.
    /// This class must extend <see cref="Microsoft.AspNetCore.Mvc.ViewFeatures.Internal.SaveTempDataFilter"/> because that filter is
    /// obtained directly from the DI container by MCV. We use explicit interface implementation to be able to intercept the methods.
    /// </summary>
    internal sealed class CustomSaveTempDataFilter : Microsoft.AspNetCore.Mvc.ViewFeatures.Internal.SaveTempDataFilter, IResourceFilter, IResultFilter
    {
        private readonly ITempDataDictionaryFactory _factory;

        public CustomSaveTempDataFilter(ITempDataDictionaryFactory factory) : base(factory)
        {
            _factory = factory;
        }

        void IResultFilter.OnResultExecuting(ResultExecutingContext context)
        {
            var tempDataDictionary = _factory.GetTempData(context.HttpContext);

            if (context.Result is IKeepTempDataResult)
            {
                tempDataDictionary.Keep();
            }

            // Delay as long as we can
            context.HttpContext.Response.OnStarting(() =>
            {
                tempDataDictionary.Save();
                return Task.CompletedTask;
            });
        }

        void IResultFilter.OnResultExecuted(ResultExecutedContext context)
        {
            // MVC's implementation "keeps" temp here. Do NOT call into the base class.
        }

        void IResourceFilter.OnResourceExecuted(ResourceExecutedContext context)
        {
            // MVC's implementation "saves" temp here. Do NOT call into the base class.
        }
    }
}
