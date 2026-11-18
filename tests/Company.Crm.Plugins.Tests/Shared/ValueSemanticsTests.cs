using System;
using System.Linq;
using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Tests.Shared.Support;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Company.Crm.Plugins.Tests.Shared
{
    public class ValueSemanticsTests
    {
        [Fact]
        public void MultiSelectComparisonIgnoresOrderButDetectsDifferentSelections()
        {
            var target = new Entity("account") { ["choices"] = Choices(2, 1) };
            var fixture = new PluginFixture("Update", target);
            fixture.PreImages.Add("PreImage", new Entity("account") { ["choices"] = Choices(1, 2) });
            var context = new PluginExecution(fixture.Provider.Object);
            Assert.False(context.IsChanged("choices"));
            target["choices"] = Choices(1, 3);
            Assert.True(context.IsChanged("choices"));
        }

        [Fact]
        public void BinaryAndLookupComparisonUsesValuesRatherThanObjectIdentityOrDisplayName()
        {
            var id = Guid.NewGuid();
            var target = new Entity("account") { ["file"] = new byte[] { 1, 2 },
                ["owner"] = new EntityReference("systemuser", id) { Name = "new label" } };
            var fixture = new PluginFixture("Update", target);
            fixture.PreImages.Add("PreImage", new Entity("account") { ["file"] = new byte[] { 1, 2 },
                ["owner"] = new EntityReference("systemuser", id) { Name = "old label" } });
            var context = new PluginExecution(fixture.Provider.Object);
            Assert.False(context.IsChanged("file"));
            Assert.False(context.IsChanged("owner"));
            ((byte[])target["file"])[1] = 3;
            Assert.True(context.IsChanged("file"));
        }

        [Fact]
        public void FullTargetCopiesCommonMutableValues()
        {
            var target = new Entity("account") { ["money"] = new Money(10), ["choice"] = new OptionSetValue(1),
                ["choices"] = Choices(1, 2), ["file"] = new byte[] { 1 },
                ["owner"] = new EntityReference("systemuser", Guid.NewGuid()) { Name = "original" } };
            var context = new PluginExecution(new PluginFixture(target: target).Provider.Object);
            var merged = context.FullTarget;
            ((Money)merged["money"]).Value = 20;
            ((OptionSetValue)merged["choice"]).Value = 2;
            ((OptionSetValueCollection)merged["choices"])[0].Value = 9;
            ((byte[])merged["file"])[0] = 8;
            ((EntityReference)merged["owner"]).Name = "changed";
            Assert.Equal(10m, ((Money)target["money"]).Value);
            Assert.Equal(1, ((OptionSetValue)target["choice"]).Value);
            Assert.Equal(1, ((OptionSetValueCollection)target["choices"])[0].Value);
            Assert.Equal(1, ((byte[])target["file"])[0]);
            Assert.Equal("original", ((EntityReference)target["owner"]).Name);
        }

        [Fact]
        public void PublicContextHidesPlatformAliasesButStillImplementsThePacContract()
        {
            var context = new PluginExecution(new PluginFixture().Provider.Object);
            var platform = (ILocalPluginContext)context;
            Assert.Same(context.Context, platform.PluginExecutionContext);
            Assert.Same(context.OrgService, platform.PluginUserService);
            Assert.Same(context.OrgServiceFactory, platform.OrgSvcFactory);
            Assert.Null(typeof(PluginExecution).GetProperty("PluginExecutionContext"));
            Assert.Null(typeof(PluginExecution).GetProperty("PluginUserService"));
            Assert.Null(typeof(PluginExecution).GetProperty("OrgSvcFactory"));
        }

        [Fact]
        public void PacEntryAndExitLogsUseTheConcretePluginName()
        {
            Assert.Equal(typeof(Company.Crm.Plugins.PluginBase), typeof(Company.Crm.Plugins.Shared.PluginBase).BaseType);
            var fixture = new PluginFixture();
            new TraceProbePlugin().Execute(fixture.Provider.Object);
            var messages = fixture.Tracing.Invocations.Select(call => (string)call.Arguments[0]).ToArray();
            Assert.Contains(messages, message => message.Contains("Entered " + typeof(TraceProbePlugin).FullName + ".Execute()"));
            Assert.Contains(messages, message => message.Contains("Exiting " + typeof(TraceProbePlugin).FullName + ".Execute()"));
            Assert.DoesNotContain(messages, message => message.Contains("Entered " + typeof(Company.Crm.Plugins.Shared.PluginBase).FullName + ".Execute()"));
        }

        private static OptionSetValueCollection Choices(params int[] values) =>
            new OptionSetValueCollection(values.Select(value => new OptionSetValue(value)).ToList());

        private sealed class TraceProbePlugin : Company.Crm.Plugins.Shared.PluginBase
        {
            internal TraceProbePlugin() : base(typeof(TraceProbePlugin)) { }
            protected override void Execute(PluginExecution context) { }
        }
    }
}
