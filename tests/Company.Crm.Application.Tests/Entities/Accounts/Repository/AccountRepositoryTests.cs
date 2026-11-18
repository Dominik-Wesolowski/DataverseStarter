using System;
using Company.Crm.Application.Entities.Accounts.Repository;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Moq;
using Xunit;

namespace Company.Crm.Application.Tests.Entities.Accounts.Repository
{
    public class AccountRepositoryTests
    {
        [Fact]
        public void GetByIdRequestsOnlyRequiredColumns()
        {
            var id = Guid.NewGuid();
            var result = new Entity("account", id) { ["name"] = "Example" };
            var service = new Mock<IOrganizationService>(MockBehavior.Strict);
            service.Setup(s => s.Retrieve("account", id, It.Is<ColumnSet>(c =>
                !c.AllColumns && c.Columns.Count == 1 && c.Columns.Contains("name")))).Returns(result);
            Assert.Same(result, new AccountRepository(service.Object).GetById(id));
            service.VerifyAll();
            service.VerifyNoOtherCalls();
        }
    }
}