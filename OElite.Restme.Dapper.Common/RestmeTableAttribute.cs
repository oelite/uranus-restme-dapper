using System;
using System.Linq;

namespace OElite
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RestmeTableAttribute : Attribute
    {
        public string DbTableName { get; set; }
        public string DefaultOrderByClauseInQuery { get; set; }
        public string[] ExcludedProperties { get; set; }

        public RestmeTableAttribute(string dbTableName)
        {
            this.DbTableName = dbTableName;
            if (ExcludedProperties?.Length > 0 == false) return;
            var filteredExclusions =
                ExcludedProperties.ToList().Select(item => item.Trim()).Where(item => item.IsNotNullOrEmpty());
            ExcludedProperties = filteredExclusions.ToArray();
        }
    }
}