using System;
using System.Threading;
using System.Threading.Tasks;
using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Tests.Shared.Support;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Xunit;

namespace Company.Crm.Plugins.Tests.Shared
{
    public class GenericBasePluginTests
    {
        [Fact]
        public void TypedTargetIsAvailableAndAttributeWritesReachOriginalTarget()
        {
            var original = new Entity("account", Guid.NewGuid()) { ["name"] = "before" };
            new TypedProbePlugin().Execute(new PluginFixture(target: original).Provider.Object);
            Assert.Equal("after", original["name"]);
        }

        [Fact]
        public void MismatchedEntityIsRejectedBeforeFeatureExecution()
        {
            var fixture = new PluginFixture(target: new Entity("contact"));
            var error = Assert.Throws<InvalidPluginExecutionException>(() =>
                new TypedProbePlugin().Execute(fixture.Provider.Object));
            Assert.Contains("account", error.Message);
            Assert.Contains("contact", error.Message);
        }

        [Fact]
        public void MissingTargetFailsClearly()
        {
            var error = Assert.Throws<InvalidPluginExecutionException>(() =>
                new TypedProbePlugin().Execute(new PluginFixture().Provider.Object));
            Assert.Contains("Target", error.Message);
        }

        [Fact]
        public void TypedImagesUseTheRequestedEarlyBoundType()
        {
            var fixture = new PluginFixture();
            fixture.PreImages.Add("PreImage", new Entity("account") { ["name"] = "before" });
            var context = new PluginExecution(fixture.Provider.Object).ToEntity<TestAccount>();
            Assert.Equal("before", context.PreImage.Name);
            Assert.Null(context.PostImage);
        }

        [Fact]
        public async Task ConcurrentExecutionsOfOnePluginDoNotShareTarget()
        {
            using (var barrier = new Barrier(2))
            {
                var plugin = new ParallelProbePlugin(barrier);
                var first = new Entity("account", Guid.NewGuid()) { ["name"] = "first" };
                var second = new Entity("account", Guid.NewGuid()) { ["name"] = "second" };
                await Task.WhenAll(
                    Task.Run(() => plugin.Execute(new PluginFixture(target: first).Provider.Object)),
                    Task.Run(() => plugin.Execute(new PluginFixture(target: second).Provider.Object)));
                Assert.Equal("first processed", first["name"]);
                Assert.Equal("second processed", second["name"]);
            }
        }

        [EntityLogicalName("account")]
        public sealed class TestAccount : Entity
        {
            public TestAccount() : base("account") { }
            public string Name
            {
                get => GetAttributeValue<string>("name");
                set => this["name"] = value;
            }
        }

        private sealed class TypedProbePlugin : PluginBase<TestAccount>
        {
            internal TypedProbePlugin() : base(typeof(TypedProbePlugin)) { }
            protected override void Execute(PluginContext context)
            {
                var target = context.TargetEntity;
                Assert.Equal(((Entity)context.Target).Id, target.Id);
                target.Name = "after";
            }
        }

        private sealed class ParallelProbePlugin : PluginBase<TestAccount>
        {
            private readonly Barrier _barrier;
            internal ParallelProbePlugin(Barrier barrier) : base(typeof(ParallelProbePlugin)) { _barrier = barrier; }
            protected override void Execute(PluginContext context)
            {
                var target = context.TargetEntity;
                var expected = target.Name;
                if (!_barrier.SignalAndWait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("Parallel test did not reach both executions.");
                Assert.Equal(((Entity)context.Target).Id, target.Id);
                target.Name = expected + " processed";
            }
        }
    }
}
