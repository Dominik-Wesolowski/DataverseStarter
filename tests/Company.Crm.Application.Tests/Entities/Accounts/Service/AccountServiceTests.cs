using Company.Crm.Application.Entities.Accounts.Repository;
using System;
using Company.Crm.Application.Entities.Accounts.Service;
using Microsoft.Xrm.Sdk;
using Moq;
using Xunit;

namespace Company.Crm.Application.Tests.Entities.Accounts.Service
{
    public class AccountServiceTests
    {
        [Theory]
        [InlineData("  Example   Company  ", "Example Company")]
        [InlineData("Example\t\r\nCompany", "Example Company")]
        [InlineData("   ", "")]
        [InlineData(null, null)]
        public void NormalizesWhitespace(string input, string expected) =>
            Assert.Equal(expected, AccountService.NormalizeName(input));

        [Fact]
        public void MissingNameDoesNotIntroduceAnAttributeOrReadRepository()
        {
            var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
            var target = new Entity("account");
            new AccountService(Mock.Of<IOrganizationService>(), repository.Object).NormalizeAccountName(target);
            Assert.False(target.Contains("name"));
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public void ExplicitNullIsPreservedEvenWhenPreImageContainsName()
        {
            var target = new Entity("account") { ["name"] = null };
            new AccountService(Mock.Of<IOrganizationService>(), Mock.Of<IAccountRepository>()).NormalizeAccountName(target,
                new Entity("account") { ["name"] = "Old Name" });
            Assert.True(target.Contains("name"));
            Assert.Null(target["name"]);
        }

        [Fact]
        public void LoadsExistingAccountThroughRepository()
        {
            var id = Guid.NewGuid();
            var repository = new Mock<IAccountRepository>(MockBehavior.Strict);
            repository.Setup(x => x.GetById(id)).Returns(new Entity("account", id) { ["name"] = " Example   Company " });
            Assert.Equal("Example Company", new AccountService(Mock.Of<IOrganizationService>(), repository.Object).GetNormalizedName(id));
            repository.VerifyAll();
            repository.VerifyNoOtherCalls();
        }
    }
}
