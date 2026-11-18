// Minimal Account model for template examples and tests; not a complete CRM metadata model.
// Only standard out-of-the-box columns are included.
// npm run model:generate intentionally overwrites this file with the target environment's model.
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Company.Crm.EarlyBound.Generated
{
    [EntityLogicalName(EntityLogicalName)]
    public partial class Account : Entity
    {
        public const string EntityLogicalName = "account";

        public Account() : base(EntityLogicalName) { }
        public Account(Guid id) : this() { Id = id; }

        public static class Fields
        {
            public const string AccountId = "accountid";
            public const string Name = "name";
            public const string AccountNumber = "accountnumber";
            public const string Telephone1 = "telephone1";
            public const string EMailAddress1 = "emailaddress1";
            public const string WebSiteURL = "websiteurl";
            public const string Description = "description";
        }

        [AttributeLogicalName(Fields.AccountId)]
        public Guid? AccountId
        {
            get => GetAttributeValue<Guid?>(Fields.AccountId);
            set
            {
                SetAttributeValue(Fields.AccountId, value);
                base.Id = value ?? Guid.Empty;
            }
        }

        [AttributeLogicalName(Fields.AccountId)]
        public override Guid Id
        {
            get => base.Id;
            set => AccountId = value;
        }

        [AttributeLogicalName(Fields.Name)]
        public string Name { get => GetAttributeValue<string>(Fields.Name); set => SetAttributeValue(Fields.Name, value); }
        [AttributeLogicalName(Fields.AccountNumber)]
        public string AccountNumber { get => GetAttributeValue<string>(Fields.AccountNumber); set => SetAttributeValue(Fields.AccountNumber, value); }
        [AttributeLogicalName(Fields.Telephone1)]
        public string Telephone1 { get => GetAttributeValue<string>(Fields.Telephone1); set => SetAttributeValue(Fields.Telephone1, value); }
        [AttributeLogicalName(Fields.EMailAddress1)]
        public string EMailAddress1 { get => GetAttributeValue<string>(Fields.EMailAddress1); set => SetAttributeValue(Fields.EMailAddress1, value); }
        [AttributeLogicalName(Fields.WebSiteURL)]
        public string WebSiteURL { get => GetAttributeValue<string>(Fields.WebSiteURL); set => SetAttributeValue(Fields.WebSiteURL, value); }
        [AttributeLogicalName(Fields.Description)]
        public string Description { get => GetAttributeValue<string>(Fields.Description); set => SetAttributeValue(Fields.Description, value); }
    }
}
