using System;
using Company.Crm.Application.Shared;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using Xunit;

namespace Company.Crm.Application.Tests.Shared
{
    public class ServiceBaseTests
    {
        [Fact]
        public void RetrieveUsesEarlyBoundLogicalNameAndExplicitColumns()
        {
            var sdk = new Mock<IOrganizationService>(MockBehavior.Strict);
            var id = Guid.NewGuid();
            var columns = new ColumnSet("name");
            sdk.Setup(s => s.Retrieve("account", id, columns))
                .Returns(new Entity("account", id) { ["name"] = "Example" });
            var result = new TestService(sdk.Object).Retrieve<TestAccount>(id, columns);
            Assert.Equal(id, result.Id);
            Assert.Equal("Example", result.GetAttributeValue<string>("name"));
            sdk.VerifyAll();
            sdk.VerifyNoOtherCalls();
        }

        [Fact]
        public void RetrievePagePreservesContinuationWithoutChangingTheRequest()
        {
            var sdk = new Mock<IOrganizationService>(MockBehavior.Strict);
            var query = new QueryExpression("account") { ColumnSet = new ColumnSet("name") };
            query.PageInfo = new PagingInfo { Count = 10, PageNumber = 2, PagingCookie = "cookie" };
            var record = new Entity("account", Guid.NewGuid());
            sdk.Setup(s => s.RetrieveMultiple(query)).Returns(new EntityCollection(new[] { record })
            { MoreRecords = true, PagingCookie = "next-cookie" });
            var result = new TestService(sdk.Object).RetrievePage<TestAccount>(query);
            Assert.Single(result.Records);
            Assert.True(result.MoreRecords);
            Assert.Equal("next-cookie", result.PagingCookie);
            Assert.Equal("cookie", query.PageInfo.PagingCookie);
            Assert.Equal(record.Id, result.Records[0].Id);
            Assert.Equal(2, query.PageInfo.PageNumber);
            sdk.VerifyAll();
            sdk.VerifyNoOtherCalls();
        }

        [Fact]
        public void MismatchedQueryIsRejectedBeforeCallingSdk()
        {
            var sdk = new Mock<IOrganizationService>(MockBehavior.Strict);
            Assert.Throws<ArgumentException>(() =>
                new TestService(sdk.Object).RetrievePage<TestAccount>(new QueryExpression("contact")));
            sdk.VerifyNoOtherCalls();
        }

        [Fact]
        public void ExecutePreservesTypedResponse()
        {
            var sdk = new Mock<IOrganizationService>(MockBehavior.Strict);
            var request = new RetrieveRequest();
            var response = new RetrieveResponse();
            sdk.Setup(s => s.Execute(request)).Returns(response);
            Assert.Same(response, new TestService(sdk.Object).Execute<RetrieveResponse>(request));
        }

        [Fact]
        public void ExecuteRejectsUnexpectedResponseType()
        {
            var sdk = new Mock<IOrganizationService>(MockBehavior.Strict);
            var request = new RetrieveRequest();
            sdk.Setup(s => s.Execute(request)).Returns(new OrganizationResponse());
            Assert.Throws<InvalidOperationException>(() =>
                new TestService(sdk.Object).Execute<RetrieveResponse>(request));
        }

        private sealed class TestService : ServiceBase
        {
            internal TestService(IOrganizationService organizationService) : base(organizationService) { }
        }

        [Microsoft.Xrm.Sdk.Client.EntityLogicalName("account")]
        public sealed class TestAccount : Entity
        {
            public TestAccount() : base("account") { }
        }
    }
}
