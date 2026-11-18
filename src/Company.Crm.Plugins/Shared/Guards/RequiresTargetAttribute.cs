using System;
using Company.Crm.Plugins.Shared;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Plugins.Shared.Guards
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RequiresTargetAttribute : Attribute, IPluginGuard
    {
        private readonly bool _reference;
        public RequiresTargetAttribute(bool reference = false) { _reference = reference; }
        public bool Passes(ILocalPluginContext context) =>
            context.PluginExecutionContext.InputParameters.TryGetValue(PluginConstants.Target, out var target)
            && (_reference ? target is EntityReference : target is Entity);
    }
}
