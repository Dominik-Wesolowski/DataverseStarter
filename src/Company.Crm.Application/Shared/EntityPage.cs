using System.Collections.Generic;

namespace Company.Crm.Application.Shared
{
    public sealed class EntityPage<TEntity>
    {
        internal EntityPage(IReadOnlyList<TEntity> records, bool moreRecords, string pagingCookie)
        {
            Records = records;
            MoreRecords = moreRecords;
            PagingCookie = pagingCookie;
        }
        public IReadOnlyList<TEntity> Records { get; }
        public bool MoreRecords { get; }
        public string PagingCookie { get; }
    }
}
