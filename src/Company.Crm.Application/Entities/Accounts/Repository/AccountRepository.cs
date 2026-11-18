using System;
using Company.Crm.EarlyBound.Generated;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Application.Entities.Accounts.Repository
{
    // Concrete SDK adapter. Services own rules; repositories own queries.
    public sealed class AccountRepository : IAccountRepository
    {
        private readonly IOrganizationService _service;
        public AccountRepository(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Entity GetById(Guid id)
        {
            if (id == Guid.Empty) throw new ArgumentException("An account id is required.", nameof(id));
            return _service.Retrieve(Account.EntityLogicalName, id, new ColumnSet(Account.Fields.Name));
        }
    }
}
