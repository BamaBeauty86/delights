﻿using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Modulight.Modules.Client.RazorComponents.UI;
using Modulight.Modules.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Modulight.Modules.Client.RazorComponents
{
    /// <summary>
    /// Specifies the contract for razor component module hosts.
    /// </summary>
    public interface IRazorComponentClientModuleHost : IModuleHost
    {
        /// <summary>
        /// Get all registered modules.
        /// </summary>
        new IReadOnlyList<IRazorComponentClientModule> Modules { get; }

        /// <summary>
        /// Load related assemblies for a given route.
        /// </summary>
        /// <param name="path">Route path.</param>
        /// <param name="recurse">Load dependent assemblies recursely.</param>
        /// <param name="throwOnError">Throw exceptions when error occurs instead of logs.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<Assembly>> GetAssembliesForRouting(string path, bool recurse = false, bool throwOnError = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate modules.
        /// Check if route roots conflict or assembly loading fails.
        /// </summary>
        /// <returns></returns>
        Task Validate();

        /// <summary>
        /// Load all <see cref="UIResource"/> defined in modules into DOM.
        /// </summary>
        /// <returns></returns>
        Task LoadResources();
    }

    internal class RazorComponentClientModuleHost : DefaultModuleHost, IRazorComponentClientModuleHost
    {
        public RazorComponentClientModuleHost(IServiceProvider services, IReadOnlyDictionary<Type, ModuleManifest> moduleTypes) : base(services,
            new Dictionary<Type, ModuleManifest>(moduleTypes.Where(x => x.Key.IsModule<IRazorComponentClientModule>())))
        {
            Modules = base.Modules.Select(x => (IRazorComponentClientModule)x).ToArray();
            Logger = Services.GetRequiredService<ILogger<RazorComponentClientModuleHost>>();
        }

        public ILogger<RazorComponentClientModuleHost> Logger { get; }

        public new IReadOnlyList<IRazorComponentClientModule> Modules { get; }

        public async Task LoadResources()
        {
            using var scope = Services.CreateScope();
            var provider = scope.ServiceProvider;
            await using var ui = new ModuleUILoader(provider.GetRequiredService<IJSRuntime>(), provider.GetRequiredService<ILogger<ModuleUI>>());
            foreach (var (lmodule, manifest) in LoadedModules)
            {
                var module = (IRazorComponentClientModule)lmodule;
                var cui = module.GetUI(provider);
                if (cui is not null)
                {
                    foreach (var resource in cui.Resources)
                    {
                        try
                        {
                            switch (resource.Type)
                            {
                                case UIResourceType.Script:
                                    await ui.LoadScript(resource.Path);
                                    break;
                                case UIResourceType.StyleSheet:
                                    await ui.LoadStyleSheet(resource.Path);
                                    break;
                            }
                        }
                        catch (JSException ex)
                        {
                            Logger.LogError(ex, $"Failed to load resource {resource.Path} in module {manifest.Name}");
                        }
                    }
                }
            }
        }

        public async Task Validate()
        {
            using var scope = Services.CreateScope();
            var provider = scope.ServiceProvider;
            HashSet<string> rootPaths = new HashSet<string>();
            foreach (var (lmodule, manifest) in LoadedModules)
            {
                var module = (IRazorComponentClientModule)lmodule;
                var cui = module.GetUI(provider);

                if (cui is not null)
                {
                    if (cui.RootPath is not "")
                    {
                        if (rootPaths.Contains(cui.RootPath))
                        {
                            throw new Exception($"Same RootPath in modules: {cui.RootPath} @ {manifest.Name}");
                        }
                        rootPaths.Add(cui.RootPath);
                    }

                    await GetAssembliesForRouting($"/{cui.RootPath}", throwOnError: true);
                }
            }
        }

        public async Task<List<Assembly>> GetAssembliesForRouting(string path, bool recurse = false, bool throwOnError = false, CancellationToken cancellationToken = default)
        {
            using var scope = Services.CreateScope();
            var provider = scope.ServiceProvider;
            var lazyLoader = provider.GetRequiredService<LazyAssemblyLoader>();

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<Assembly> results = new List<Assembly>();

            Queue<string> toLoad = new Queue<string>();

            foreach (var (lmodule, manifest) in LoadedModules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var module = (IRazorComponentClientModule)lmodule;

                toLoad.Enqueue(manifest.EntryAssembly);

                var ui = module.GetUI(provider);

                if (ui is not null)
                {
                    if (ui.Contains(path))
                    {
                        foreach (var name in manifest.Assemblies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            toLoad.Enqueue(name);
                        }
                    }
                }
            }

            while (toLoad.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = toLoad.Dequeue();

                Assembly? assembly;

                assembly = loadedAssemblies.FirstOrDefault(x => x.GetName().Name == current);

                if (assembly is null)
                {
                    try
                    {
                        // Logger.LogInformation($"Loading assembly {current}");
                        if (Environment.OSVersion.Platform == PlatformID.Other)
                        {
                            assembly = (await lazyLoader.LoadAssembliesAsync(new[] { current + ".dll" })).FirstOrDefault();
                        }
                        else
                        {
                            assembly = Assembly.Load(current);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (throwOnError)
                        {
                            throw;
                        }
                        else
                        {
                            Logger.LogWarning($"Failed to load assembly {current}: {ex}");
                        }
                    }
                }

                if (assembly is null)
                {
                    if (throwOnError)
                    {
                        throw new NullReferenceException($"Failed to load assembly {current}.");
                    }
                    Logger.LogError($"Failed to load assembly {current}.");
                    continue;
                }

                results.Add(assembly);

                if (recurse)
                {
                    foreach (var refe in assembly.GetReferencedAssemblies())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (refe.Name is not null)
                            toLoad.Enqueue(refe.Name);
                    }
                }
            }

            return results;
        }
    }
}