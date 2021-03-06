﻿using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Modulight.Modules.Client.RazorComponents.UI
{
    /// <summary>
    /// Specifies <see cref="IRazorComponentClientModule.RootPath"/> for the razor component module.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModuleUIRootPathAttribute : Attribute
    {
        /// <summary>
        /// Specifies <see cref="IRazorComponentClientModule.RootPath"/> for the razor component module.
        /// </summary>
        public ModuleUIRootPathAttribute(string rootPath)
        {
            RootPath = rootPath;
        }

        /// <summary>
        /// Root path.
        /// </summary>
        public string RootPath { get; init; }
    }

    /// <summary>
    /// Specifies UI resources.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ModuleUIResourceAttribute : Attribute
    {
        /// <summary>
        /// Specifies UI resources.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        public ModuleUIResourceAttribute(UIResourceType type, string path)
        {
            Type = type;
            Path = path;
        }

        /// <summary>
        /// Resource type.
        /// </summary>
        public UIResourceType Type { get; init; }

        /// <summary>
        /// Resource relative path.
        /// </summary>
        public string Path { get; init; } = string.Empty;

        /// <summary>
        /// Attributes for the resource.
        /// </summary>
        public string[] Attributes { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Provide JS modules.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IJSModuleProvider<T> : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Get a lazy javascript module at /_content/<paramref name="assemblyName"/>/<paramref name="jsPath"/>.
        /// </summary>
        /// <param name="jsPath">Javascript file path.</param>
        /// <param name="assemblyName">Assembly name, null for the assembly which <typeparamref name="T"/> defined.</param>
        /// <returns></returns>
        Task<IJSObjectReference> GetJSModule(string jsPath, string? assemblyName = null);
    }

    internal class JSModuleProvider<T> : IJSModuleProvider<T>
    {
        Dictionary<string, Lazy<Task<IJSObjectReference>>> JSInvokers { get; } = new Dictionary<string, Lazy<Task<IJSObjectReference>>>();

        public JSModuleProvider(IJSRuntime jsRuntime, ILogger<T> logger)
        {
            JSRuntime = jsRuntime;
            Logger = logger;
        }

        /// <summary>
        /// JS runtime.
        /// </summary>
        protected IJSRuntime JSRuntime { get; }

        /// <summary>
        /// Logger
        /// </summary>
        protected ILogger<T> Logger { get; }

        public Task<IJSObjectReference> GetJSModule(string jsPath, string? assemblyName = null)
        {
            if (assemblyName is null)
                assemblyName = typeof(T).Assembly.GetName().Name ?? "";

            string id = $"{assemblyName}/{jsPath}";

            if (!JSInvokers.ContainsKey(id))
            {
                Logger.LogInformation($"Create JS invoker: {id}.");
                JSInvokers.Add(id, new(() =>
                    JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/{id}").AsTask()));
            }

            return JSInvokers[id].Value;
        }

        #region Dispose

        /// <inheritdoc/>
        protected async ValueTask DisposeAsyncCore()
        {
            foreach (var invoker in JSInvokers)
            {
                if (invoker.Value.IsValueCreated)
                {
                    Logger.LogInformation($"Dispose JS invoker: {invoker.Key}.");
                    var value = await invoker.Value.Value;
                    await value.DisposeAsync();
                }
            }
            JSInvokers.Clear();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        private bool _disposedValue;

        /// <inheritdoc/>
        protected void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var invoker in JSInvokers)
                    {
                        if (invoker.Value.IsValueCreated)
                        {
                            Logger.LogInformation($"Dispose JS invoker: {invoker.Key}.");
                            (invoker.Value.Value as IDisposable)?.Dispose();
                        }
                    }
                    JSInvokers.Clear();
                }
                _disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
