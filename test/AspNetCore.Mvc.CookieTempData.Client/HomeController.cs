using Microsoft.AspNetCore.Mvc;
using System;

namespace AspNetCore.Mvc.CookieTempData.Client
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            TempData["mykey"] = DateTime.UtcNow;
            return Content($"<html><body><a href='{Url.Action("Continue")}'>Click me</a></body></html>", "text/html");
        }

        public IActionResult Continue()
        {
            DateTime value;
            if (TempData.TryGetValue("mykey", out value))
            {
                return Content("TempData has value " + value);
            }
            else
            {
                return Content("Nothing on TempData");
            }
        }
    }
}
