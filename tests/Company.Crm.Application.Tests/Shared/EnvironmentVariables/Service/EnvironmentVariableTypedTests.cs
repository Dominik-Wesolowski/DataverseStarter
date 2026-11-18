using System;
using Company.Crm.Application.Shared.EnvironmentVariables.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using Xunit;

namespace Company.Crm.Application.Tests.Shared.EnvironmentVariables.Service
{
    public class EnvironmentVariableTypedTests
    {
        private static EnvironmentVariableReader Reader(string value, out Mock<IOrganizationService> service)
        {
            service = new Mock<IOrganizationService>(MockBehavior.Strict);
            var definition = new Entity("environmentvariabledefinition", Guid.NewGuid()) { ["defaultvalue"] = value };
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryBase>(q => ((QueryExpression)q).EntityName == "environmentvariabledefinition")))
                .Returns(new EntityCollection(new[] { definition }));
            service.Setup(s => s.RetrieveMultiple(It.Is<QueryBase>(q => ((QueryExpression)q).EntityName == "environmentvariablevalue")))
                .Returns(new EntityCollection());
            return new EnvironmentVariableReader(service.Object);
        }

        [Fact]
        public void RepeatedReadsUseInstanceCache()
        {
            var reader = Reader("true", out var service);
            Assert.True(reader.GetRequiredBoolean("setting"));
            Assert.Equal("true", reader.GetValue("setting"));
            service.Verify(s => s.RetrieveMultiple(It.IsAny<QueryBase>()), Times.Exactly(2));
            new EnvironmentVariableReader(service.Object).GetValue("setting");
            service.Verify(s => s.RetrieveMultiple(It.IsAny<QueryBase>()), Times.Exactly(4));
        }

        [Fact]
        public void MissingDefinitionsAreCached()
        {
            var service = new Mock<IOrganizationService>(MockBehavior.Strict);
            service.Setup(s => s.RetrieveMultiple(It.IsAny<QueryBase>())).Returns(new EntityCollection());
            var reader = new EnvironmentVariableReader(service.Object);
            Assert.Null(reader.GetBoolean("missing"));
            Assert.Null(reader.GetGuid("missing"));
            service.Verify(s => s.RetrieveMultiple(It.IsAny<QueryBase>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public void RequiredTextRejectsBlank(string value)
        {
            var reader = Reader(value, out _);
            Assert.Throws<InvalidPluginExecutionException>(() => reader.GetRequiredValue("setting"));
        }

        [Fact]
        public void DecimalIsInvariantAndInvalidValuesAreRedacted()
        {
            Assert.Equal(12.5m, Reader("12.5", out _).GetRequiredDecimal("amount"));
            var error = Assert.Throws<InvalidPluginExecutionException>(() => Reader("12,5-private", out _).GetRequiredDecimal("amount"));
            Assert.Contains("amount", error.Message);
            Assert.DoesNotContain("12,5-private", error.Message);
        }

        [Fact]
        public void GuidAndBooleanRejectMalformedValues()
        {
            var id = Guid.NewGuid();
            Assert.Equal(id, Reader(id.ToString(), out _).GetRequiredGuid("id"));
            Assert.Throws<InvalidPluginExecutionException>(() => Reader("bad", out _).GetGuid("id"));
            Assert.Throws<InvalidPluginExecutionException>(() => Reader("", out _).GetBoolean("flag"));
            Assert.False(Reader("false", out _).GetRequiredBoolean("flag"));
        }
    }
}
