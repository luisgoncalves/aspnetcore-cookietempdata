# AspNetCore.Mvc.CookieTempData

[![AppVeyor build status](https://ci.appveyor.com/api/projects/status/ja28in082k1vll61/branch/dev?svg=true)](https://ci.appveyor.com/project/luisgoncalves/aspnetcore-cookietempdata/branch/dev)
[![Travis CI build status](https://travis-ci.org/luisgoncalves/aspnetcore-cookietempdata.svg?branch=dev)](https://travis-ci.org/luisgoncalves/aspnetcore-cookietempdata)
[![NuGet](https://img.shields.io/nuget/v/AspNetCore.Mvc.CookieTempData.svg?maxAge=3600)](https://www.nuget.org/packages/AspNetCore.Mvc.CookieTempData)

Cookie-based *TempData* provider for ASP.NET Core MVC applications. *TempData* values are serialized in BSON and protected via the [data protection API](https://docs.asp.net/en/latest/security/data-protection/index.html).

**NOTE**: [ASP.NET Core 1.1.0](https://github.com/aspnet/Mvc/releases/tag/rel%2F1.1.0-preview1) will include a built-in cookie-based *TempData* provider.

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

Additional configurations can be done using the *options* pattern:

```csharp
services.AddCookieTempData(o => 
{
    o.CookieName = "t";
});
```

### Limitations

By default, MVC saves *TempData* at a stage where it may be too late to access response cookies. To be able to safely use response cookies, the library saves *TempData* a bit earlier. As a side effect, the latest stage of the [filter chain](https://docs.asp.net/en/latest/mvc/controllers/filters.html#how-do-filters-work) on which you can modify *TempData* is `IResultFilter.OnResultExecuting`. Changes made after this point **are not persisted**. This shouldn't be a problem for most application, as *TempData* is usually used within action methods. 

## Acknowledgements

This project is heavily inspired on Brock Allen's [CookieTempData](https://github.com/brockallen/CookieTempData), which targets previous versions of ASP.NET MVC.
