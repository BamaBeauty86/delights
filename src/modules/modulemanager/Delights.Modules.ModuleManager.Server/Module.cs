using Delights.Modules.Server.GraphQL;
using Delights.Modules.Services;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Delights.Modules.ModuleManager.Server
{
    public static class ModuleExtensions
    {
        public static ModuleCollection AddModuleManagerModule(this ModuleCollection modules, Action<ModuleOption>? configureOptions = null)
        {
            modules.AddModule<Module, ModuleOption>(configureOptions);
            return modules;
        }
    }

    public class Module : GraphQLServerModule<ModuleService, ModuleOption, ModuleQuery, ModuleMutation, ModuleSubscription>
    {
        public Module() : base()
        {
            Metadata = Metadata with
            {
                Name = SharedMetadata.Raw.Name,
                DisplayName = SharedMetadata.Raw.DisplayName,
                Description = SharedMetadata.Raw.Description,
                Url = SharedMetadata.Raw.Url,
                Author = SharedMetadata.Raw.Author,
            };
        }
    }

    public class ModuleQuery : QueryRootObject
    {
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ModuleManifest> GetModuleManagerModules([Service] ModuleCollection collection)
        {
            return collection.Modules.Select(m => m.Metadata).AsQueryable();
        }
    }

    public class ModuleMutation : MutationRootObject
    {
    }

    public class ModuleSubscription : SubscriptionRootObject
    {
    }

    public class ModuleService : Services.IModuleService
    {
        public ModuleService(ILogger<Module> logger) => Logger = logger;

        public ILogger<Module> Logger { get; private set; }
    }
}
