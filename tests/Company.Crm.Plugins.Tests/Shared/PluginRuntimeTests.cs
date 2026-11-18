using Company.Crm.Application.Shared.Extensions;
using System.Collections.Generic;
using System;
using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Shared.Guards;
using Company.Crm.Plugins.Tests.Shared.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Company.Crm.Plugins.Tests.Shared
{
    public class PluginRuntimeTests
    {
        [Fact]
        public void GeneratedContextUsesStepAndInitiatingUsersWithoutImplicitElevation()
        {
            var fixture = new PluginFixture();
            var context = new PluginExecution(fixture.Provider.Object);
            Assert.Same(context.OrgService, context.OrgService);
            var initiating = context.InitiatingUserService;
            fixture.Factory.Verify(x => x.CreateOrganizationService(fixture.UserId), Moq.Times.Once);
            fixture.Factory.Verify(x => x.CreateOrganizationService(fixture.InitiatingUserId), Moq.Times.Once);
            fixture.Factory.VerifyNoOtherCalls();

        }

        [Theory]
        [InlineData(false, "old")]
        [InlineData(true, null)]
        public void EffectiveValueDistinguishesMissingAttributeFromExplicitNull(bool supplied, string expected)
        {
            var target = new Entity("account");
            if (supplied) target["name"] = null;
            Assert.Equal(expected, target.GetEffectiveValue<string>(
                new Entity("account") { ["name"] = "old" }, "name"));
        }

        [Fact]
        public void ParametersAreTypedAndOutputsAreWrittenToOriginalContext()
        {
            var fixture = new PluginFixture();
            var id = Guid.NewGuid();
            fixture.Inputs.Add("id", id);
            var context = new PluginExecution(fixture.Provider.Object);
            Assert.Equal(id, context.GetRequiredInput<Guid>("id"));
            Assert.False(context.TryGetInputParameter<bool>("id", out _));
            Assert.Throws<InvalidPluginExecutionException>(() => context.GetRequiredInput<Guid>("missing"));
            context.SetOutput("success", true);
            Assert.Equal(true, fixture.Outputs["success"]);
        }

        [Fact]
        public void DeleteTargetIsAnEntityReference()
        {
            var fixture = new PluginFixture("Delete");
            var reference = new EntityReference("account", Guid.NewGuid());
            fixture.Inputs.Add("Target", reference);
            Assert.Same(reference, new PluginExecution(fixture.Provider.Object).TargetReference);
        }

        [Fact]
        public void ReusedPluginGetsNewScopeAndDisposesServicesAfterEveryExecution()
        {
            var plugin = new ScopeProbePlugin();
            plugin.Execute(new PluginFixture().Provider.Object);
            plugin.Execute(new PluginFixture().Provider.Object);
            Assert.Equal(2, plugin.Instances.Count);
            Assert.NotSame(plugin.Instances[0], plugin.Instances[1]);
            Assert.All(plugin.Instances, instance => Assert.True(instance.Disposed));
            Assert.Equal(new[] { "unsecure-example", "unsecure-example" }, plugin.Configurations);
        }

        [Fact]
        public void KnownPluginExceptionIsPreservedAndScopeIsDisposed()
        {
            var exception = new InvalidPluginExecutionException("Business validation");
            var plugin = new ScopeProbePlugin { Failure = exception };
            Assert.Same(exception, Assert.Throws<InvalidPluginExecutionException>(() =>
                plugin.Execute(new PluginFixture().Provider.Object)));
            Assert.True(plugin.Instances[0].Disposed);
        }

        [Fact]
        public void UnexpectedExceptionIsTracedAndWrapped()
        {
            var plugin = new ScopeProbePlugin { Failure = new InvalidOperationException("Internal detail") };
            var fixture = new PluginFixture();
            var error = Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(fixture.Provider.Object));
            Assert.Same(plugin.Failure, error.InnerException);
            Assert.True(plugin.Instances[0].Disposed);
            Assert.NotEmpty(fixture.Tracing.Invocations);
        }

        [Fact]
        public void ConventionRegistersServicesRepositoriesAndConfigurationWithoutManualMappings()
        {
            var fixture = new PluginFixture();
            new RegistrationProbePlugin().Execute(fixture.Provider.Object);
            fixture.Organization.VerifyNoOtherCalls();
        }

        private sealed class RegistrationProbePlugin : Company.Crm.Plugins.Shared.PluginBase
        {
            internal RegistrationProbePlugin() : base(typeof(RegistrationProbePlugin)) { }
            protected override void Execute(PluginExecution context)
            {
                Assert.NotNull(context.Get<Company.Crm.Application.Shared.EnvironmentVariables.Service.IEnvironmentVariableReader>());
                Assert.Same(context.OrgService, context.Get<IOrganizationService>());
            }
        }

        private sealed class DisposableProbe : IDisposable
        {
            internal bool Disposed;
            public void Dispose() { Disposed = true; }
        }

        private sealed class ScopeProbePlugin : Company.Crm.Plugins.Shared.PluginBase
        {
            internal readonly List<DisposableProbe> Instances = new List<DisposableProbe>();
            internal readonly List<string> Configurations = new List<string>();
            internal Exception Failure;
            internal ScopeProbePlugin() : base(typeof(ScopeProbePlugin), "unsecure-example", "secure-example") { }

            protected override void ConfigureServices(IServiceCollection services) =>
                services.AddScoped<DisposableProbe>(_ => new DisposableProbe());

            protected override void Execute(PluginExecution context)
            {
                var instance = context.Get<DisposableProbe>();
                Assert.Same(instance, context.Get<DisposableProbe>());
                Instances.Add(instance);
                Configurations.Add(context.UnsecureConfig);
                if (Failure != null) throw Failure;
            }
        }

    }
}
