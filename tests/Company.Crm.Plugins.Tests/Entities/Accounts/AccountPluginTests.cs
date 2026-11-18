using System;
using Company.Crm.Plugins.Entities.Accounts;
using Company.Crm.Plugins.Tests.Shared.Support;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Company.Crm.Plugins.Tests.Entities.Accounts
{
    public class AccountPluginTests
    {
        [Theory]
        [InlineData("Create", "  Example   Company ", "Example Company")]
        [InlineData("Update", " New   Name ", "New Name")]
        [InlineData("Update", null, null)]
        [InlineData("Create", "", "")]
        public void RealPluginAndServiceModifyOnlyTargetWithoutExtraDatabaseWrite(string message, string name, string expected)
        {
            var id = Guid.NewGuid();
            var target = new Entity("account", id) { ["name"] = name, ["telephone1"] = "123" };
            var image = new Entity("account", id) { ["name"] = "Old Name" };
            var scenario = new PluginScenario(message, target).WithPreImage(image);
            var run = scenario.Execute(Plugin(message));
            Assert.Null(run.Failure);
            Assert.Equal(name, run.Before["name"]);
            Assert.Equal(expected, run.After["name"]);
            Assert.Equal(expected, target["name"]);
            Assert.Equal("123", run.After["telephone1"]);
            Assert.Equal(id, run.After.Id);
            Assert.Equal("Old Name", image["name"]);
            Assert.Empty(run.SdkCalls);
            Assert.NotEmpty(run.Traces);
            scenario.Platform.Organization.VerifyNoOtherCalls();
            scenario.Platform.Factory.Verify(f => f.CreateOrganizationService(null), Moq.Times.Never);
        }

        [Theory]
        [InlineData("Create")]
        [InlineData("Update")]
        public void OmittedNameIsNotCopiedFromImage(string message)
        {
            var target = new Entity("account") { ["telephone1"] = "123" };
            var scenario = new PluginScenario(message, target)
                .WithPreImage(new Entity("account") { ["name"] = "Image Name" });
            var run = scenario.Execute(Plugin(message));
            Assert.Null(run.Failure);
            Assert.False(run.After.Contains("name"));
            Assert.Empty(run.SdkCalls);
        }

        [Theory]
        [InlineData("Update", "account", 20, 0)]
        [InlineData("Create", "contact", 20, 0)]
        [InlineData("Create", "account", 40, 0)]
        [InlineData("Create", "account", 20, 1)]
        public void RegistrationMismatchSkipsBusinessLogic(string message, string entity, int stage, int mode)
        {
            var scenario = new PluginScenario(message, new Entity("account") { ["name"] = " raw " }, stage, entity, mode);
            var run = scenario.Execute(new PreCreateAccountPlugin());
            Assert.Null(run.Failure);
            Assert.Equal(" raw ", run.After["name"]);
            Assert.Empty(run.SdkCalls);
            Assert.Contains(run.Traces, line => line.Contains("does not match registration"));
        }

        [Fact]
        public void RequiredTargetGuardReportsSkip()
        {
            var run = new PluginScenario().Execute(new PreCreateAccountPlugin());
            Assert.Null(run.Failure);
            Assert.Contains(run.Traces, line => line.Contains("Skipped by RequiresTargetAttribute"));
            Assert.Empty(run.SdkCalls);
        }

        [Fact]
        public void MissingImageFailsBeforeBusinessLogic()
        {
            var run = new PluginScenario("Update", new Entity("account") { ["name"] = " raw " })
                .Execute(new PreUpdateAccountPlugin());
            Assert.Contains("PreImage", Assert.IsType<InvalidPluginExecutionException>(run.Failure).Message);
            Assert.Equal(" raw ", run.After["name"]);
            Assert.Empty(run.SdkCalls);
            Assert.Contains(run.Traces, line => line.Contains("_status: Failed"));
        }

        [Fact]
        public void SamePluginInstanceDoesNotRetainTargetBetweenExecutions()
        {
            var plugin = new PreCreateAccountPlugin();
            var first = new PluginScenario(target: new Entity("account") { ["name"] = " First " }).Execute(plugin);
            var second = new PluginScenario(target: new Entity("account") { ["name"] = " Second " }).Execute(plugin);
            Assert.Null(first.Failure);
            Assert.Null(second.Failure);
            Assert.Equal("First", first.After["name"]);
            Assert.Equal("Second", second.After["name"]);
        }

        [Fact]
        public void UnchangedNameIsNotNormalizedAgain()
        {
            var scenario = new PluginScenario("Update", new Entity("account") { ["name"] = " Same  Name " })
                .WithPreImage(new Entity("account") { ["name"] = " Same  Name " });
            var run = scenario.Execute(new PreUpdateAccountPlugin());
            Assert.Null(run.Failure);
            Assert.Equal(" Same  Name ", run.After["name"]);
            Assert.Empty(run.SdkCalls);
        }

        [Fact]
        public void InvalidAttributeTypeIsWrappedAndLeavesTargetUntouched()
        {
            var scenario = new PluginScenario(target: new Entity("account") { ["name"] = 123 });
            var run = scenario.Execute(new PreCreateAccountPlugin());
            Assert.IsType<InvalidPluginExecutionException>(run.Failure);
            Assert.IsType<InvalidCastException>(run.Failure.InnerException);
            Assert.Equal(123, run.After["name"]);
            Assert.Empty(run.SdkCalls);
        }

        private static IPlugin Plugin(string message) => message == "Create"
            ? (IPlugin)new PreCreateAccountPlugin() : new PreUpdateAccountPlugin();
    }
}
