﻿using Modulight.Modules.Client.RazorComponents;
using Delights.Modules.Hello;
using Delights.Modules.ModuleManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using Modulight.Modules;
using Delights.Modules.Notes;

namespace Delights.Client.Shared
{
    public static class ModuleSetup
    {
        public static Modulight.Modules.IModuleHostBuilder CreateDefaultBuilder(bool isDev)
        {
            var builder = Modulight.Modules.ModuleHostBuilder.CreateDefaultBuilder()
                .UseRazorComponentClientModules()
                .AddModule<UI.UiModule>()
                .AddNotesModule(configureOptions: (o, sp) =>
                {
                    var serverConfiguration = sp.GetRequiredService<IOptions<ServerConfiguration>>().Value;
                    o.GraphQLEndpoint = serverConfiguration.GraphQLEndpoint;
                });

            if (isDev)
            {
                builder.AddHelloModule(configureOptions: (o, sp) =>
                 {
                     var serverConfiguration = sp.GetRequiredService<IOptions<ServerConfiguration>>().Value;
                     o.GraphQLEndpoint = serverConfiguration.GraphQLEndpoint;
                 })
                .AddModuleManagerModule(configureOptions: (o, sp) =>
                {
                    var serverConfiguration = sp.GetRequiredService<IOptions<ServerConfiguration>>().Value;
                    o.GraphQLEndpoint = serverConfiguration.GraphQLEndpoint;
                });
            }


            return builder;
        }
    }

    public static class ServiceExtensions
    {
        public static IServiceCollection AddServerConfiguration(this IServiceCollection services)
        {
            services.AddOptions<ServerConfiguration>().Configure(o =>
            {
                o.GraphQLEndpoint = "https://localhost:5001/graphql";
            });
            return services;
        }
    }
}
