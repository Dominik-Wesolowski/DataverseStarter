using System;
using PlatformPluginBase = Company.Crm.Plugins.PluginBase;
using System.Linq;
using Company.Crm.Plugins.Shared.DependencyInjection;
using Company.Crm.Plugins.Shared.Guards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Plugins.Shared
{
    public abstract class PluginBase : PlatformPluginBase
    {
        private readonly string _unsecureConfiguration;
        private readonly string _secureConfiguration;

        protected PluginBase(Type pluginType, string unsecureConfiguration = null, string secureConfiguration = null)
            : base(pluginType)
        {
            _unsecureConfiguration = unsecureConfiguration;
            _secureConfiguration = secureConfiguration;
        }

        protected sealed override void ExecuteDataversePlugin(ILocalPluginContext platformContext)
        {
            try
            {
                if (!RegistrationGuard.Passes(GetType(), platformContext))
                {
                    platformContext.Trace("Skipped: execution does not match registration.");
                    return;
                }
                foreach (var guard in GetType().GetCustomAttributes(false).OfType<IPluginGuard>())
                {
                    if (guard.Passes(platformContext)) continue;
                    platformContext.Trace("Skipped by " + guard.GetType().Name);
                    return;
                }
                var context = new PluginExecution(platformContext, _unsecureConfiguration, _secureConfiguration);
                using (var provider = PluginServiceProviderFactory.BuildProvider(context, ConfigureServices))
                using (var scope = provider.CreateScope())
                {
                    context.ServiceProvider = scope.ServiceProvider;
                    Execute(context);
                }
            }
            catch (InvalidPluginExecutionException exception)
            {
                platformContext.Trace(exception.ToString());
                throw;
            }
            catch (System.ServiceModel.FaultException<OrganizationServiceFault>)
            {
                // The original PAC runtime handles OrganizationServiceFault.
                throw;
            }
            catch (Exception exception)
            {
                platformContext.Trace(exception.ToString());
                throw new InvalidPluginExecutionException("The CRM operation failed. See the plugin trace log.", exception);
            }
        }

        protected virtual void ConfigureServices(IServiceCollection services) { }
        protected abstract void Execute(PluginExecution context);

    }
}
