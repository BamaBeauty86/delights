﻿using System;
using Modulight.Modules;
using Modulight.Modules.Hosting;

namespace Delights.Modules.ModuleManager.Server
{
    public static class ModuleExtensions
    {
        public static IModuleHostBuilder AddModuleManagerModule(this IModuleHostBuilder modules)
        {
            modules.AddModule<ModuleManagerServerModule>();
            return modules;
        }
    }
}
