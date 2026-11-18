using System;
using Company.Crm.EarlyBound.Generated;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace Company.Crm.Application.Tests.Entities.Accounts.Model
{
    public class AccountModelTests
    {
        [Fact]
        public void EarlyBoundPropertiesUseStandardAttributeNamesAndPreserveExplicitNull()
        {
            var entity = new Entity("account") { ["name"] = "Example", ["telephone1"] = "123" };
            var account = entity.ToEntity<Account>();
            Assert.Equal("Example", account.Name);
            Assert.Equal("123", account.Telephone1);
            account.Name = null;
            Assert.True(entity.Contains("name"));
            Assert.Null(entity["name"]);
        }

        [Fact]
        public void PrimaryIdPropertyAndSdkIdRemainConsistent()
        {
            var id = Guid.NewGuid();
            var account = new Account { AccountId = id };
            Assert.Equal(id, account.Id);
            var anotherId = Guid.NewGuid();
            account.Id = anotherId;
            Assert.Equal(anotherId, account.AccountId);
            account.AccountId = null;
            Assert.Equal(Guid.Empty, account.Id);
        }
    }
}
