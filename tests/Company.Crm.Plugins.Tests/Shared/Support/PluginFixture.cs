using System;
using Company.Crm.Plugins.Shared;
using Microsoft.Xrm.Sdk;
using Moq;

namespace Company.Crm.Plugins.Tests.Shared.Support
{
    internal sealed class PluginFixture
    {
        internal readonly Mock<IPluginExecutionContext> Execution = new Mock<IPluginExecutionContext>();
        internal readonly Mock<IOrganizationService> Organization = new Mock<IOrganizationService>(MockBehavior.Strict);
        internal readonly Mock<IOrganizationServiceFactory> Factory = new Mock<IOrganizationServiceFactory>(MockBehavior.Strict);
        internal readonly Mock<IServiceProvider> Provider = new Mock<IServiceProvider>(MockBehavior.Strict);
        internal readonly Mock<ITracingService> Tracing = new Mock<ITracingService>();
        internal readonly Guid UserId = Guid.NewGuid();
        internal readonly Guid InitiatingUserId = Guid.NewGuid();
        internal readonly ParameterCollection Inputs = new ParameterCollection();
        internal readonly ParameterCollection Outputs = new ParameterCollection();
        internal readonly EntityImageCollection PreImages = new EntityImageCollection();
        internal readonly EntityImageCollection PostImages = new EntityImageCollection();
        internal readonly ParameterCollection SharedVariables = new ParameterCollection();

        internal PluginFixture(string message = "Create", Entity target = null, int stage = 20)
        {
            Execution.SetupGet(x => x.MessageName).Returns(message);
            Execution.SetupGet(x => x.PrimaryEntityName).Returns("account");
            Execution.SetupGet(x => x.Stage).Returns(stage);
            Execution.SetupGet(x => x.Mode).Returns(0);
            Execution.SetupGet(x => x.Depth).Returns(1);
            Execution.SetupGet(x => x.UserId).Returns(UserId);
            Execution.SetupGet(x => x.InitiatingUserId).Returns(InitiatingUserId);
            Execution.SetupGet(x => x.SharedVariables).Returns(SharedVariables);
            Execution.SetupGet(x => x.InputParameters).Returns(Inputs);
            Execution.SetupGet(x => x.OutputParameters).Returns(Outputs);
            Execution.SetupGet(x => x.PreEntityImages).Returns(PreImages);
            Execution.SetupGet(x => x.PostEntityImages).Returns(PostImages);
            if (target != null) Inputs.Add("Target", target);
            Factory.Setup(x => x.CreateOrganizationService(UserId)).Returns(Organization.Object);
            Factory.Setup(x => x.CreateOrganizationService(InitiatingUserId)).Returns(Organization.Object);
            Factory.Setup(x => x.CreateOrganizationService(null)).Returns(Organization.Object);
            Provider.Setup(x => x.GetService(typeof(IExecutionContext))).Returns(Execution.Object);
            Provider.Setup(x => x.GetService(typeof(Microsoft.Xrm.Sdk.PluginTelemetry.ILogger))).Returns((object)null);
            Provider.Setup(x => x.GetService(typeof(IServiceEndpointNotificationService))).Returns((object)null);
            Provider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(Execution.Object);
            Provider.Setup(x => x.GetService(typeof(ITracingService))).Returns(Tracing.Object);
            Provider.Setup(x => x.GetService(typeof(IOrganizationServiceFactory))).Returns(Factory.Object);
        }
    }
}
