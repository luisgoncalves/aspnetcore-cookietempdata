# AspNetCore.Mvc.CookieTempData

[![Build status](https://ci.appveyor.com/api/projects/status/ja28in082k1vll61/branch/dev?svg=true)](https://ci.appveyor.com/project/luisgoncalves/aspnetcore-cookietempdata/branch/dev)
 [![NuGet](https://img.shields.io/nuget/v/AspNetCore.Mvc.CookieTempData.svg?maxAge=2592000)](https://www.nuget.org/packages/AspNetCore.Mvc.CookieTempData)

Cookie-based *TempData* provider for ASP.NET Core MVC applications. *TempData* values are serialized in BSON and protected via the [data protection API](https://docs.asp.net/en/latest/security/data-protection/index.html).

## Usage

### Installation

This project is available as a NuGet package at [https://www.nuget.org/packages/AspNetCore.Mvc.CookieTempData/](https://www.nuget.org/packages/AspNetCore.Mvc.CookieTempData/).

### Configuration

On the `ConfigureServices` method of your `Startup` class just add a call to `AddCookieTempData`. This will replace MVC's default *TempData* provider - which uses session storage - with the cookie-based provider.

```chsarp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc();
    services.AddCookieTempData();
}
```

**Important note**: you should [configure data protection](https://docs.asp.net/en/latest/security/data-protection/configuration/index.html) to fit your deployment scenarios. The library uses *purpose strings* appropriately but no configuration is done on the data protection system.

### Limitations

By default, MVC saves *TempData* at a stage where it may be too late to access response cookies. To be able to safely use response cookies, the library saves *TempData* a bit earlier. As a side effect, the latest stage of the [filter chain](https://docs.asp.net/en/latest/mvc/controllers/filters.html#how-do-filters-work) on which you can modify *TempData* is `IResultFilter.OnResultExecuting`. Changes made after this point **are not persisted**. 

## Acknowledgements

This project is heavily inspired on Brock Allen's [CookieTempData](https://github.com/brockallen/CookieTempData), which targets previous versions of ASP.NET MVC.