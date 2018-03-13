using System.Collections.Generic;

namespace OElite
{
    public interface IRestmeDbEntityCollection<T> : IEnumerable<T> where T : IRestmeDbEntity
    {
        int TotalRecordsCount { get; set; }
        void AddRange(IEnumerable<T> items);
    }
}