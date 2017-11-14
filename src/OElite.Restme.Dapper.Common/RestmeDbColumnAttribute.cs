﻿using System;
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
        public RestmeDbColumnType ColumnType = RestmeDbColumnType.NormalColumn;

        public RestmeDbColumnAttribute(RestmeDbColumnType type = RestmeDbColumnType.NormalColumn,
            [CallerMemberName] string dbColumnName = null)
        {
            if (dbColumnName.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(dbColumnName));

            DbColumnName = dbColumnName;
            SetPropertiesByColumnType(type);
        }

        public RestmeDbColumnAttribute(string dbColumnName, RestmeDbColumnType type)
        {
            DbColumnName = dbColumnName;
            SetPropertiesByColumnType(type);
        }

        private void SetPropertiesByColumnType(RestmeDbColumnType type)
        {
            ColumnType = type;
            switch (type)
            {
                case RestmeDbColumnType.NormalColumn:
                    InSelect = true;
                    InUpdate = true;
                    InInsert = true;
                    InDelete = true;
                    break;
                case RestmeDbColumnType.ForeignKey:
                    InSelect = true;
                    InUpdate = true;
                    InInsert = true;
                    InDelete = true;
                    break;
                case RestmeDbColumnType.AutoGeneratedPrimaryKey:
                    InSelect = true;
                    InUpdate = false;
                    InInsert = false;
                    InDelete = true;
                    break;
                case RestmeDbColumnType.ViewColumn:
                    InSelect = true;
                    InUpdate = false;
                    InInsert = false;
                    InDelete = true;
                    break;
                case RestmeDbColumnType.NormalPrimaryKey:
                    InSelect = true;
                    InUpdate = true;
                    InInsert = true;
                    InDelete = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}