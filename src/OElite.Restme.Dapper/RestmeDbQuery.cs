using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OElite.Restme.Dapper
{
    public interface IRestmeDbQuery
    {
        RestmeDb DbCentre { get; set; }

        Dictionary<string, string> DefaultColumnsInQuery { get; set; }

        string DefaultOrderByClauseInQuery { get; }
        string CustomSelectTableSource { get; }
        string DbTableName { get; }


        Dictionary<string, object> ParamValues { get; }
        void MapSelectColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null);
        void MapUpdateColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null);
        void MapInsertColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null);
        void MapDeleteColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null);
    }

    public class RestmeDbQuery<T> : IRestmeDbQuery where T : IRestmeDbEntity
    {
        public RestmeDb DbCentre { get; set; }

        public virtual string CustomSelectTableSource { get; }
        public Dictionary<string, string> DefaultColumnsInQuery { get; set; } = new Dictionary<string, string>();

        public virtual string DefaultOrderByClauseInQuery { get; }
        public string DbTableName { get; }


        public Dictionary<string, object> ParamValues { get; } = new Dictionary<string, object>();


        public RestmeDbQuery(RestmeDb dbCentre)
        {
            DbCentre = dbCentre;
            DbTableName = TableAttribute.DbTableName;
            CustomSelectTableSource = DbTableName;
            DefaultOrderByClauseInQuery = TableAttribute.DefaultOrderByClauseInQuery;
        }


        public virtual void PrepareParamValues(T data,
            string[] choosenPropertiesOnly = null,
            string[] propertiesToExclude = null)
        {
            ParamValues.Clear();
            var props = DefaultColumnsInQuery.ToList();
            if (choosenPropertiesOnly?.Length > 0)
                props = DefaultColumnsInQuery.Where(item => choosenPropertiesOnly.Contains(item.Key)).ToList();
            else if (propertiesToExclude?.Length > 0)
                props = DefaultColumnsInQuery.Where(item => !propertiesToExclude.Contains(item.Key)).ToList();
            foreach (var prop in props)
            {
                var objValue = data.GetPropertyValue(prop.Key);
                if (objValue != null && objValue is DateTime)
                {
                    if (DateTimeUtils.IsValidSqlDateTimeValue(objValue))
                        ParamValues.Add(prop.Key, objValue);
                    else
                        ParamValues.Add(prop.Key, null);
                }
                else if (objValue != null && objValue.GetType().GetTypeInfo().IsEnum)
                {
                    ParamValues.Add(prop.Key, (int) objValue);
                }
                else
                    ParamValues.Add(prop.Key, objValue);
            }
        }

        public RestmeTableAttribute TableAttribute
        {
            get
            {
                var attribute = typeof(T).GetTypeInfo().GetCustomAttribute(typeof(RestmeTableAttribute));
                if (attribute == null)
                    throw new ArgumentException($"{typeof(T)} does not contain valid QueryTableAttribute");
                return (RestmeTableAttribute) attribute;
            }
        }

        public Dictionary<string, RestmeDbColumnAttribute> ColumnAttributes
        {
            get
            {
                var dic = new Dictionary<string, RestmeDbColumnAttribute>();
                var properties =
                    typeof(T).GetProperties().Where(prop => prop.IsDefined(typeof(RestmeDbColumnAttribute), true));
                var propertyInfos = properties as PropertyInfo[] ?? properties.ToArray();
                if (!propertyInfos.Any()) return dic;

                foreach (var prop in propertyInfos)
                {
                    var attribute = prop.GetCustomAttribute<RestmeDbColumnAttribute>(true);
                    if (attribute != null)
                    {
                        dic.Add(prop.Name, attribute);
                    }
                }

                return dic;
            }
        }


        private Dictionary<string, string> GenerateDefaultColumnsInQuery(
            string[] choosenPropertiesOnly = null,
            string[] propertiesToExclude = null, bool? inSelect = null, bool? inInsert = null, bool? inUpdate = null,
            bool? inDelete = null)
        {
            var columnAttributes = ColumnAttributes.Where(dic => (inSelect == null || dic.Value.InSelect == inSelect) &&
                                                                 (inInsert == null || dic.Value.InInsert == inInsert) &&
                                                                 (inUpdate == null || dic.Value.InUpdate == inUpdate) &&
                                                                 (inDelete == null || dic.Value.InDelete == inDelete) &&
                                                                 (choosenPropertiesOnly == null ||
                                                                  choosenPropertiesOnly.Contains(dic.Key)) &&
                                                                 (propertiesToExclude == null ||
                                                                  !propertiesToExclude.Contains(dic.Key)) &&
                                                                 (
                                                                     TableAttribute.ExcludedProperties == null ||
                                                                     !TableAttribute.ExcludedProperties.Contains(
                                                                         dic.Key)
                                                                 )).ToDictionary(d => d.Key, d => d.Value.DbColumnName) ??
                                   new Dictionary<string, string>();

            if (choosenPropertiesOnly != null && choosenPropertiesOnly.Any())
            {
                var unIdentifiedColumns = choosenPropertiesOnly.Where(item => !ColumnAttributes.ContainsKey(item));
                if (unIdentifiedColumns != null && unIdentifiedColumns.Any())
                {
                    foreach (var col in unIdentifiedColumns)
                    {
                        columnAttributes.Add(Guid.NewGuid().ToString(), col);
                    }
                }
            }
            return columnAttributes;
        }


        public virtual void MapSelectColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            =>
            DefaultColumnsInQuery =
                GenerateDefaultColumnsInQuery(choosenPropertiesOnly,
                    propertiesToExclude, inSelect: true);

        public virtual void MapUpdateColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            => DefaultColumnsInQuery =
                GenerateDefaultColumnsInQuery(choosenPropertiesOnly,
                    propertiesToExclude, inUpdate: true);

        public virtual void MapInsertColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            => DefaultColumnsInQuery =
                GenerateDefaultColumnsInQuery(choosenPropertiesOnly,
                    propertiesToExclude, inInsert: true);

        public virtual void MapDeleteColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            => DefaultColumnsInQuery =
                GenerateDefaultColumnsInQuery(choosenPropertiesOnly,
                    propertiesToExclude, inDelete: true);


        public void Map(string propertyName, string dbColumnAlias)
        {
            if (propertyName.IsNullOrEmpty() || dbColumnAlias.IsNullOrEmpty() || DefaultColumnsInQuery == null)
                return;

            if (DefaultColumnsInQuery.ContainsKey(propertyName))
                DefaultColumnsInQuery[propertyName] = dbColumnAlias;
            else
            {
                DefaultColumnsInQuery.Add(propertyName, dbColumnAlias);
            }
        }
    }
}