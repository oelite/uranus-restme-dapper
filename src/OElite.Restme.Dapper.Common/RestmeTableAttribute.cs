using System;

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
        }
    }
}