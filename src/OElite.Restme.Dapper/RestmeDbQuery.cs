using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Dynamic;
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


        //Dictionary<string, object> ParamValues { get; }
        ExpandoObject ParamValues { get; }
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


        public ExpandoObject ParamValues { get; } = new ExpandoObject();


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
            var paramValues = (IDictionary<string, object>) ParamValues;
            paramValues.Clear();
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
                    paramValues.Add(prop.Key, DateTimeUtils.IsValidSqlDateTimeValue(objValue) ? objValue : null);
                }
                else if (objValue != null && (objValue is int || objValue is long))
                {
                    if (IsForeignKey(prop) && NumericUtils.GetLongIntegerValueFromObject(objValue) == 0)
                        paramValues.Add(prop.Key, null);
                    else
                        paramValues.Add(prop.Key, objValue);
                }
                else if (objValue != null && objValue.GetType().GetTypeInfo().IsEnum)
                {
                    paramValues.Add(prop.Key, (int) objValue);
                }
                else
                    paramValues.Add(prop.Key, objValue);
            }
        }

        private bool IsForeignKey(KeyValuePair<string, string> prop)
        {
            var columnAttr = ColumnAttributes.Where(item => item.Key == prop.Key).Select(c => c.Value).FirstOrDefault();
            return (columnAttr?.ColumnType == RestmeDbColumnType.ForeignKey);
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

            if (choosenPropertiesOnly == null || !choosenPropertiesOnly.Any()) return columnAttributes;
            var unIdentifiedColumns = choosenPropertiesOnly.Where(item => !ColumnAttributes.ContainsKey(item));
            var identifiedColumns = unIdentifiedColumns as string[] ?? unIdentifiedColumns.ToArray();
            if (!identifiedColumns.Any()) return columnAttributes;
            foreach (var col in identifiedColumns)
            {
                columnAttributes.Add(Guid.NewGuid().ToString(), col);
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