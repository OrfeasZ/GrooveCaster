﻿using System;
using System.Collections.Generic;
using System.IO;
using GrooveCaster.Models;
using Nancy;

namespace GrooveCaster.Modules
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
                        Name = "SharpShark",
                        License = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Licenses\\SharpShark.txt"))
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
                    new LibraryLicense
                    {
                        Name = "IronPython",
                        License = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Licenses\\IronPython.txt"))
                    },
                };

                return View["Licenses", new { Libraries = s_Libraries }];
            };
        }
    }
}
