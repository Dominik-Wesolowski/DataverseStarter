using System;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Plugins.Shared
{
    public abstract class PluginBase<TEntity> : PluginBase where TEntity : Entity
    {
        protected PluginBase(Type pluginType, string unsecureConfiguration = null, string secureConfiguration = null)
            : base(pluginType, unsecureConfiguration, secureConfiguration) { }

        protected sealed override void Execute(PluginExecution context)
        {
            var typed = new PluginContext(context);
            typed.Require(typed.TargetEntity != null, "Required Entity Target is missing or has an invalid type.");
            Execute(typed);
        }
        protected abstract void Execute(PluginContext context);

        // Inherited by concrete plugins with TEntity already bound by PluginBase<TEntity>.
        protected sealed class PluginContext : PluginExecution<TEntity>
        {
            internal PluginContext(PluginExecution source)
                : base(source) { }
        }
    }
}
