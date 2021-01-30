﻿using Microsoft.Extensions.DependencyInjection;
using System;
using Modulight.Modules.Hosting;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Modulight.Modules.Server.AspNet
{
    public sealed class AspNetServerModulePlugin : ModuleHostBuilderPlugin
    {
        /// <inheritdoc/>
        public override Task AfterBuild(IReadOnlyDictionary<Type, ModuleManifest> modules, IServiceCollection services)
        {
            services.AddSingleton<IAspNetServerModuleHost>(sp => new AspNetServerModuleHost(sp, modules));
            return base.AfterBuild(modules, services);
        }
    }
}
