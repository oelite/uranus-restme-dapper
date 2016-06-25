using System;
using System.Runtime.CompilerServices;

namespace OElite
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RestmeDbColumnAttribute : Attribute
    {
        public bool InSelect { get; set; }
        public bool InUpdate { get; set; }
        public bool InDelete { get; set; }
        public bool InInsert { get; set; }
        public string DbColumnName { get; }

        public RestmeDbColumnAttribute(RestmeDbColumnType type, [CallerMemberName] string dbColumnName = null)
        {
            if (dbColumnName.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(dbColumnName));

            this.DbColumnName = dbColumnName;
            SetPropertiesByColumnType(type);
        }

        public RestmeDbColumnAttribute(string dbColumnName, RestmeDbColumnType type)
        {
            this.DbColumnName = dbColumnName;
            SetPropertiesByColumnType(type);
        }

        private void SetPropertiesByColumnType(RestmeDbColumnType type)
        {
            switch (type)
            {
                case RestmeDbColumnType.NormalColumn:
                    this.InSelect = true;
                    this.InUpdate = true;
                    this.InInsert = true;
                    this.InDelete = true;
                    break;
                case RestmeDbColumnType.PrimaryKey:
                    this.InSelect = true;
                    this.InUpdate = true;
                    this.InInsert = false;
                    this.InDelete = true;
                    break;
                case RestmeDbColumnType.ViewColumn:
                    this.InSelect = true;
                    this.InUpdate = true;
                    this.InInsert = false;
                    this.InDelete = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}