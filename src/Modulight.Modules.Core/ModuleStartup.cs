﻿using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Modulight.Modules
{
    public interface IModuleStartup
    {
        void ConfigureServices(IServiceCollection services);
    }

    public abstract class ModuleStartup : IModuleStartup
    {
        public virtual void ConfigureServices(IServiceCollection services) { }
    }
}