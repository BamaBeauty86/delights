﻿using Delights.Modules.Client.UI;
using Delights.Modules.Options;
using Delights.Modules.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Delights.Modules.Client
{
    public static class ClientModuleExtensions
    {
        public static ModuleCollection AddClientModules(this ModuleCollection modules)
        {
            modules.AddModule<Core.Module>();
            return modules;
        }
    }

    public interface IClientModule : IModule
    {
        void RegisterUI(IServiceCollection services);

        void RegisterUIService(IServiceCollection services);

        ModuleUI GetUI(IServiceProvider provider);

        IModuleService GetUIService(IServiceProvider provider);
    }

    public abstract class ClientModule<TUIService, TOption, TUI> : Module<TUIService, TOption>, IClientModule where TUI : ModuleUI where TUIService : class, IModuleService where TOption : ModuleOption
    {
        protected ClientModule(ModuleManifest? manifest = null) : base(manifest)
        {
        }

        public virtual void RegisterUI(IServiceCollection services)
        {
            services.AddScoped<TUI>();
        }

        public virtual void RegisterUIService(IServiceCollection services)
        {
            services.AddScoped<TUIService>();
        }

        public override void RegisterService(IServiceCollection services)
        {
            base.RegisterService(services);
            RegisterUI(services);
        }

        public TUI GetUI(IServiceProvider provider) => provider.GetRequiredService<TUI>();

        public TUIService GetUIService(IServiceProvider provider) => base.GetService(provider);

        ModuleUI IClientModule.GetUI(IServiceProvider provider) => GetUI(provider);

        IModuleService IClientModule.GetUIService(IServiceProvider provider) => GetUIService(provider);
    }
}
