using Microsoft.AspNetCore.Mvc;
using System;

namespace AspNetCore.Mvc.CookieTempData.Client
{
    public class HomeController : Controller
    {
        private const string Key = "mykey";

        public class State
        {
            public class NestedState
            {
                public long Long { get; set; }
                public string String { get; set; }
            }

            public DateTime Date { get; set; }
            public NestedState Nested { get; set; }
        }

        public IActionResult Index()
        {
            var now = DateTime.UtcNow;
            TempData[Key] = new State
            {
                Date = now,
                Nested = new State.NestedState
                {
                    Long = now.Ticks,
                    String = now.ToString("o"),
                }
            };
            return Content($"<html><body><a href='{Url.Action("Continue")}'>Get value from TempData</a></body></html>", "text/html");
        }

        public IActionResult Continue()
        {
            State value;
            if (TempData.TryGetValue(Key, out value))
            {
                return Content($"TempData has values {value.Date}, {value.Nested.Long}, {value.Nested.String}");
            }

            return Content("Nothing on TempData");
        }
    }
}
