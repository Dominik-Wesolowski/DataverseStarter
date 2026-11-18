using System;
using Company.Crm.Plugins.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Plugins.Shared.DependencyInjection
{
    internal static class PluginServiceProviderFactory
    {
        internal static ServiceProvider BuildProvider(PluginExecution context,
            Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILocalPluginContext>(context);
            services.AddSingleton<PluginExecution>(context);
            services.AddSingleton(context.Context);
            services.AddSingleton(context.TracingService);
            services.AddScoped<IOrganizationService>(_ => context.OrgService);

            services.AddApplicationServices();
            configure(services);

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
        }
    }
}
