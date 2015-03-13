using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GrooveCasterServer.Models;
using Nancy;

namespace GrooveCasterServer.Modules
{
    public class LicenseModule : NancyModule
    {
        public LicenseModule()
        {
            Get["/licenses"] = p_Parameters =>
            {
                var s_Libraries = new List<LibraryLicense>()
                {
                    new LibraryLicense
                    {
                        Name = "Json.NET",
                        License = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Licenses\\Json.txt"))
                    },
                    new LibraryLicense
                    {
                        Name = "Nancy",
                        License = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Licenses\\Nancy.txt"))
                    },
                    new LibraryLicense
                    {
                        Name = "ServiceStack",
                        License = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Licenses\\ServiceStack.txt"))
                    },
                    new LibraryLicense
                    {
                        Name = "Razor",
                        License = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Licenses\\Razor.txt"))
                    },
                };

                return View["Licenses", new { Libraries = s_Libraries }];
            };
        }
    }
}
