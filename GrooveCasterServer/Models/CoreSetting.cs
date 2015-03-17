﻿using System;
using ServiceStack.DataAnnotations;

namespace GrooveCaster.Models
{
    public class CoreSetting
    {
        [PrimaryKey]
        public String Key { get; set; }

        public String Value { get; set; }
    }
}
