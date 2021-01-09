﻿using Delights.Modules.Server.GraphQL;
using Delights.Modules.Services;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Delights.Modules.Hello.Server
{
    public static class ModuleExtensions
    {
        public static ModuleCollection AddHelloModule(this ModuleCollection modules, Action<ModuleOption>? configureOptions = null)
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
        public IQueryable<HelloMessage> GetHelloMessages([Service] ModuleService service)
        {
            service.Logger.LogInformation(nameof(GetHelloMessages));
            return service.Messages.AsQueryable();
        }
    }

    public class ModuleMutation : MutationRootObject
    {
    }

    public class ModuleSubscription : SubscriptionRootObject
    {
    }

    public record HelloMessage
    {
        public string Content { get; init; } = "";
    }

    public class ModuleService : Services.IModuleService
    {
        public ModuleService(ILogger<Module> logger) => Logger = logger;

        public ILogger<Module> Logger { get; private set; }

        public List<HelloMessage> Messages { get; } = new List<HelloMessage>() {
            new HelloMessage { Content = "Message 1" },
            new HelloMessage { Content = "Message 2" },
        };
    }
}
