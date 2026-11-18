using System.Linq;
using System;

namespace Company.Crm.Plugins.Shared.Guards
{
    internal static class RegistrationGuard
    {
        internal static bool Passes(Type pluginType, ILocalPluginContext context)
        {
            var registrations = pluginType.GetCustomAttributes(false)
                .OfType<CrmPluginRegistrationAttribute>().ToArray();
            if (registrations.Length == 0) return true;
            var execution = context.PluginExecutionContext;
            return registrations.Any(step =>
                string.Equals(step.Message, execution.MessageName, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrEmpty(step.EntityLogicalName) || step.EntityLogicalName == execution.PrimaryEntityName)
                && (!step.Stage.HasValue || (int)step.Stage.Value == execution.Stage)
                && (!step.Stage.HasValue || execution.Mode ==
                    (step.ExecutionMode == ExecutionModeEnum.Synchronous ? 0 : 1)));
        }
    }
}
