using System;
using System.ServiceModel;
using Company.Crm.Application.Entities.Accounts.Service;
using Company.Crm.Plugins.Shared;
using Company.Crm.Plugins.Tests.Shared.Support;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using Xunit;

namespace Company.Crm.Plugins.Tests.Entities.Accounts
{
    public class AccountServicePipelineTests
    {
        // Test-only entry point exercises the repository path that normalization does not need.
        private sealed class ReadAccountPlugin : Company.Crm.Plugins.Shared.PluginBase
        {
            internal ReadAccountPlugin() : base(typeof(ReadAccountPlugin)) { }
            protected override void Execute(PluginExecution context)
            {
                var name = context.Get<IAccountService>().GetNormalizedName(context.GetRequiredInput<Guid>("AccountId"));
                context.SetOutput("Name", name);
                context.SetSharedVariable("ReadCompleted", true);
            }
        }

        [Fact]
        public void EntryPointResolvesRealServiceAndRepositoryAndReturnsSdkData()
        {
            var id = Guid.NewGuid();
            var scenario = new PluginScenario().WithInput("AccountId", id);
            scenario.Platform.Organization.Setup(s => s.Retrieve("account", id,
                It.Is<ColumnSet>(columns => !columns.AllColumns && columns.Columns.Count == 1 && columns.Columns.Contains("name"))))
                .Returns(new Entity("account", id) { ["name"] = "  From   Dataverse " });
            var run = scenario.Execute(new ReadAccountPlugin());
            Assert.Null(run.Failure);
            Assert.Equal(new[] { "Retrieve" }, run.SdkCalls);
            Assert.Equal("From Dataverse", scenario.Platform.Outputs["Name"]);
            Assert.Equal(true, scenario.Platform.SharedVariables["ReadCompleted"]);
            scenario.Platform.Organization.Verify(s => s.Retrieve("account", id, It.IsAny<ColumnSet>()), Times.Once);
            scenario.Platform.Organization.VerifyNoOtherCalls();
            scenario.Platform.Factory.Verify(f => f.CreateOrganizationService(null), Times.Never);
        }

        [Fact]
        public void RepositoryFaultReachesRuntimeWithoutPublishingSuccess()
        {
            var scenario = new PluginScenario().WithInput("AccountId", Guid.NewGuid());
            scenario.Platform.Organization.Setup(s => s.Retrieve("account", It.IsAny<Guid>(), It.IsAny<ColumnSet>()))
                .Throws(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "Access denied" }));
            var run = scenario.Execute(new ReadAccountPlugin());
            Assert.IsType<InvalidPluginExecutionException>(run.Failure);
            Assert.Equal(new[] { "Retrieve" }, run.SdkCalls);
            Assert.Empty(scenario.Platform.Outputs);
            Assert.Empty(scenario.Platform.SharedVariables);
            Assert.NotEmpty(run.Traces);
        }

        [Fact]
        public void InvalidInputFailsBeforeRepositoryAccess()
        {
            var scenario = new PluginScenario().WithInput("AccountId", Guid.Empty);
            var run = scenario.Execute(new ReadAccountPlugin());
            Assert.IsType<InvalidPluginExecutionException>(run.Failure);
            Assert.IsType<ArgumentException>(run.Failure.InnerException);
            Assert.Empty(run.SdkCalls);
            Assert.Empty(scenario.Platform.Outputs);
        }
    }
}
