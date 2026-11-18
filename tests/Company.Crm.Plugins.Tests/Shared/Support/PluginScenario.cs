using System;
using System.Linq;
using System.Collections.Generic;
using Company.Crm.Application.Shared.Extensions;
using Microsoft.Xrm.Sdk;
using Moq;

namespace Company.Crm.Plugins.Tests.Shared.Support
{
    // Runs the real IPlugin entry point and composition. Only the platform boundary is mocked.
    internal sealed class PluginScenario
    {
        internal PluginFixture Platform { get; }
        internal Entity Target { get; }
        internal PluginScenario(string message = "Create", Entity target = null, int stage = 20,
            string entityName = "account", int mode = 0)
        {
            Target = target;
            Platform = new PluginFixture(message, target, stage);
            Platform.Execution.SetupGet(c => c.PrimaryEntityName).Returns(entityName);
            Platform.Execution.SetupGet(c => c.Mode).Returns(mode);
        }

        internal PluginScenario WithPreImage(Entity image, string alias = "PreImage")
        {
            Platform.PreImages.Add(alias, image);
            return this;
        }

        internal PluginScenario WithInput(string name, object value)
        {
            Platform.Inputs[name] = value;
            return this;
        }

        internal PluginRun Execute(IPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            var before = Snapshot(Target);
            var sdkStart = Platform.Organization.Invocations.Count;
            var traceStart = Platform.Tracing.Invocations.Count;
            Exception failure = null;
            try { plugin.Execute(Platform.Provider.Object); }
            catch (Exception exception) { failure = exception; }
            var calls = Platform.Organization.Invocations.Skip(sdkStart)
                .Select(call => call.Method.Name).ToArray();
            var traces = Platform.Tracing.Invocations.Skip(traceStart).Select(call =>
            {
                var format = (string)call.Arguments[0];
                var args = (object[])call.Arguments[1];
                return args.Length == 0 ? format : string.Format(format, args);
            }).ToArray();
            return new PluginRun(before, Snapshot(Target), failure, calls, traces);
        }

        private static Entity Snapshot(Entity source)
        {
            if (source == null) return null;
            var copy = new Entity(source.LogicalName, source.Id);
            foreach (var field in source.Attributes) copy[field.Key] = DataverseValue.Copy(field.Value);
            return copy;
        }
    }

    internal sealed class PluginRun
    {
        internal Entity Before { get; }
        internal Entity After { get; }
        internal Exception Failure { get; }
        internal IReadOnlyList<string> SdkCalls { get; }
        internal IReadOnlyList<string> Traces { get; }
        internal PluginRun(Entity before, Entity after, Exception failure, string[] calls, string[] traces)
        {
            Before = before; After = after; Failure = failure; SdkCalls = calls; Traces = traces;
        }
    }
}
