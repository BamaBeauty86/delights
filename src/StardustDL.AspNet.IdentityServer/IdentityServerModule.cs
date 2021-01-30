﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Modulight.Modules;
using Modulight.Modules.Server.AspNet;
using StardustDL.AspNet.IdentityServer.Models;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StardustDL.AspNet.IdentityServer
{
    [Module(Description = "Provide Identity Server services.", Url = "https://github.com/StardustDL/delights", Author = "StardustDL")]
    [ModuleService(typeof(IdentityServerService))]
    [ModuleStartup(typeof(Startup))]
    public class IdentityServerModule : AspNetServerModule
    {
        public override void UseMiddleware(IApplicationBuilder builder)
        {
            base.UseMiddleware(builder);

            builder.UseIdentityServer();

            builder.UseAuthentication();
            builder.UseAuthorization();
        }
    }

    public class Startup : ModuleStartup
    {
        public Startup(IOptions<IdentityServerModuleStartupOption> options) => Options = options.Value;

        IdentityServerModuleStartupOption Options { get; }

        public override Task ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<Data.IdentityDbContext>(o =>
            {
                if (Options.ConfigureDbContext is not null)
                    Options.ConfigureDbContext(o);
            });

            services.AddDefaultIdentity<ApplicationUser>(o =>
            {
                if (Options.ConfigureIdentity is not null)
                    Options.ConfigureIdentity(o);
            })
                .AddEntityFrameworkStores<Data.IdentityDbContext>();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // avoid ms-default mapping, it will change sub claim's type name

            services.AddIdentityServer(o =>
            {
                if (Options.ConfigureIdentityServer is not null)
                    Options.ConfigureIdentityServer(o);
            })
                .AddApiAuthorization<ApplicationUser, Data.IdentityDbContext>(o =>
                {
                    if (Options.ConfigureApiAuthorization is not null)
                        Options.ConfigureApiAuthorization(o);
                });

            services.AddAuthentication().AddIdentityServerJwt();

            return base.ConfigureServices(services);
        }
    }
}
