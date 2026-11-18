using System;
using Company.Crm.Application.Shared.EnvironmentVariables.Service;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Moq;
using Xunit;

namespace Company.Crm.Application.Tests.Shared.EnvironmentVariables.Service
{
    public class EnvironmentVariableReaderTests
    {
        [Theory]
        [InlineData("current", "current")]
        [InlineData("", "")]
        [InlineData(null, "default")]
        public void EnvironmentVariableUsesCurrentValueThenDefault(string current, string expected)
        {
            var definition = new Entity("environmentvariabledefinition", Guid.NewGuid()) { ["defaultvalue"] = "default" };
            var service = new Mock<IOrganizationService>(MockBehavior.Strict);
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryBase>(q =>
                q is QueryExpression && ((QueryExpression)q).EntityName == "environmentvariabledefinition")))
                .Returns(new EntityCollection(new[] { definition }));
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryBase>(q =>
                q is QueryExpression && ((QueryExpression)q).EntityName == "environmentvariablevalue")))
                .Returns(current == null ? new EntityCollection() :
                    new EntityCollection(new[] { new Entity("environmentvariablevalue") { ["value"] = current } }));
            Assert.Equal(expected, new EnvironmentVariableReader(service.Object).GetValue("sample_Setting"));
            service.VerifyAll();
            service.VerifyNoOtherCalls();
        }

        [Fact]
        public void MissingRequiredVariableFailsClearly()
        {
            var service = new Mock<IOrganizationService>(MockBehavior.Strict);
            service.Setup(s => s.RetrieveMultiple(It.IsAny<QueryBase>())).Returns(new EntityCollection());
            Assert.Throws<InvalidPluginExecutionException>(() =>
                new EnvironmentVariableReader(service.Object).GetRequiredValue("sample_Missing"));
        }

        [Fact]
        public void MultipleCurrentValuesAreRejectedInsteadOfChoosingArbitrarily()
        {
            var service = new Mock<IOrganizationService>(MockBehavior.Strict);
            service.SetupSequence(s => s.RetrieveMultiple(It.IsAny<QueryBase>()))
                .Returns(new EntityCollection(new[] { new Entity("environmentvariabledefinition", Guid.NewGuid()) }))
                .Returns(new EntityCollection(new[] { new Entity("environmentvariablevalue"), new Entity("environmentvariablevalue") }));
            Assert.Throws<InvalidPluginExecutionException>(() =>
                new EnvironmentVariableReader(service.Object).GetValue("sample_Ambiguous"));
        }
    }
}