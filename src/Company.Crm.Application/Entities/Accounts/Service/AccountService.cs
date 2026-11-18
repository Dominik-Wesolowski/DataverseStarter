using Company.Crm.Application.Shared;
using Company.Crm.Application.Entities.Accounts.Repository;
using System.Text.RegularExpressions;
using System;
using Company.Crm.EarlyBound.Generated;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Application.Entities.Accounts.Service
{
    public sealed class AccountService : ServiceBase, IAccountService
    {
        private readonly IAccountRepository _accounts;

        public AccountService(IOrganizationService organizationService, IAccountRepository accounts)
            : base(organizationService)
        {
            _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        }

        public static string NormalizeName(string name) =>
            name == null ? null : Regex.Replace(name.Trim(), @"\s+", " ");

        public void NormalizeAccountName(Entity target, Entity preImage = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target.LogicalName != Account.EntityLogicalName)
                throw new ArgumentException("Expected an account target.", nameof(target));
            if (!target.Contains(Account.Fields.Name)) return;
            var name = target.GetAttributeValue<string>(Account.Fields.Name);
            if (preImage != null && preImage.Contains(Account.Fields.Name) &&
                name == preImage.GetAttributeValue<string>(Account.Fields.Name)) return;
            // Keep the original Target and explicit nulls. No additional CRM Update.
            target[Account.Fields.Name] = NormalizeName(name);
        }

        public string GetNormalizedName(Guid accountId)
        {
            var account = _accounts.GetById(accountId);
            return NormalizeName(account.GetAttributeValue<string>(Account.Fields.Name));
        }
    }
}
