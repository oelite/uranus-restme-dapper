using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace OElite.Restme.Dapper
{
    public enum RestmeDbQueryType
    {
        Select = 0,
        Insert = 10,
        Update = 20,
        Delete = 30
    }

    public interface IRestmeDbQuery<T>
    {
        RestmeDb DbCentre { get; set; }

        string DefaultOrderByClauseInQuery { get; }
        string CustomSelectTableSource { get; }
        string CustomUpdateTableSource { get; }
        string CustomDeleteTableSource { get; }
        string CustomInsertTableSource { get; }
        string DefaultTableSource { get; }


        Dictionary<string, string> MapSelectColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity;

        Dictionary<string, string> MapUpdateColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity;

        Dictionary<string, string> MapInsertColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity;

        Dictionary<string, string> MapDeleteColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity;

        Dictionary<string, object> PrepareParamValues<A>(RestmeDbQueryType queryType, A data,
            string[] chosenColumnsOnly = null, string[] columnsToExclude = null) where A : IRestmeDbEntity;
    }

    public class RestmeDbQuery<T> : IRestmeDbQuery<T> where T : IRestmeDbEntity
    {
        public RestmeDb DbCentre { get; set; }

        public virtual string DefaultOrderByClauseInQuery { get; private set; }
        public virtual string CustomSelectTableSource { get; private set; }
        public virtual string CustomUpdateTableSource { get; private set; }
        public virtual string CustomDeleteTableSource { get; private set; }
        public virtual string CustomInsertTableSource { get; private set; }
        public virtual string DefaultTableSource { get; private set; }

        private static Dictionary<string, Dictionary<string, RestmeDbColumnAttribute>> _defaultAttributesFromTypes =
            new Dictionary<string, Dictionary<string, RestmeDbColumnAttribute>>();


        public RestmeDbQuery(RestmeDb dbCentre, string customSelectTableSource = null,
            string customInsertTableSource = null, string customUpdateTableSource = null,
            string customDeleteTableSource = null)
        {
            DbCentre = dbCentre;
            var tableAttribute = GetTableAttribute<T>();
            DefaultTableSource = tableAttribute.DbTableName;
            CustomSelectTableSource = customSelectTableSource;
            CustomInsertTableSource = customInsertTableSource;
            CustomUpdateTableSource = customUpdateTableSource;
            CustomDeleteTableSource = customDeleteTableSource;
            DefaultOrderByClauseInQuery = tableAttribute.DefaultOrderByClauseInQuery;
        }


        public Dictionary<string, object> PrepareParamValues<A>(RestmeDbQueryType queryType, A data,
            string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity
        {
            var paramValues = new Dictionary<string, object>();

            List<KeyValuePair<string, string>> props;
            switch (queryType)
            {
                case RestmeDbQueryType.Update:
                    props = MapUpdateColumns<T>(chosenColumnsOnly, columnsToExclude)?.ToList();
                    break;
                case RestmeDbQueryType.Delete:
                    props = MapDeleteColumns<T>(chosenColumnsOnly, columnsToExclude)?.ToList();
                    break;
                case RestmeDbQueryType.Insert:
                    props = MapInsertColumns<T>(chosenColumnsOnly, columnsToExclude)?.ToList();
                    break;
                default:
                    //when executing select query, use type <A> so all properties will be used
                    props = MapSelectColumns<A>(chosenColumnsOnly, columnsToExclude)?.ToList();
                    break;
            }

            if (props?.Count > 0)
            {
                if (chosenColumnsOnly?.Length > 0)
                    props = props.Where(item => chosenColumnsOnly.Contains(item.Key)).ToList();
                else if (columnsToExclude?.Length > 0)
                    props = props.Where(item => !columnsToExclude.Contains(item.Key)).ToList();
                foreach (var prop in props)
                {
                    try
                    {
                        var objValue = data.GetPropertyValue(prop.Key);
                        if (objValue != null && objValue is DateTime)
                        {
                            paramValues.Add(prop.Key,
                                DateTimeUtils.IsValidSqlDateTimeValue(objValue) ? objValue : null);
                        }
                        else if (objValue != null && (objValue is int || objValue is long))
                        {
                            if (IsForeignKey<T>(prop) && NumericUtils.GetLongIntegerValueFromObject(objValue) == 0)
                                paramValues.Add(prop.Key, null);
                            else
                                paramValues.Add(prop.Key, objValue);
                        }
                        else if (objValue != null && objValue.GetType().GetTypeInfo().IsEnum)
                        {
                            paramValues.Add(prop.Key, (int)objValue);
                        }
                        else
                            paramValues.Add(prop.Key, objValue);
                    }
                    catch (Exception ex)
                    {
                        RestmeDb.Logger?.LogError(ex.Message, ex);
                    }
                }
            }

            return paramValues;
        }

        private bool IsForeignKey<A>(KeyValuePair<string, string> prop) where A : IRestmeDbEntity
        {
            var columnAttr = GetTypeColumnAttributes<A>().Where(item => item.Key == prop.Key).Select(c => c.Value)
                .FirstOrDefault();
            return (columnAttr?.ColumnType == RestmeDbColumnType.ForeignKey);
        }


        public RestmeTableAttribute GetTableAttribute<A>() where A : IRestmeDbEntity
        {
            var attribute = typeof(A).GetTypeInfo().GetCustomAttribute(typeof(RestmeTableAttribute));
            if (attribute == null)
                throw new ArgumentException($"{typeof(A)} does not contain valid QueryTableAttribute");
            return (RestmeTableAttribute)attribute;
        }


        public Dictionary<string, RestmeDbColumnAttribute> GetTypeColumnAttributes<A>() where A : IRestmeDbEntity
        {
            var existingRepo = _defaultAttributesFromTypes.Where(item => item.Key == typeof(A).AssemblyQualifiedName)
                .Select(item => item.Value).FirstOrDefault();
            if (existingRepo != null)
                return existingRepo;

            var dic = new Dictionary<string, RestmeDbColumnAttribute>();
            var properties =
                typeof(A).GetProperties(BindingFlags.FlattenHierarchy |
                                        BindingFlags.Instance |
                                        BindingFlags.Public).Where(prop =>
                    prop.IsDefined(typeof(RestmeDbColumnAttribute), true));
            var propertyInfos = properties as PropertyInfo[] ?? properties.ToArray();
            if (!propertyInfos.Any()) return dic;

            foreach (var prop in propertyInfos)
            {
                var attribute = prop.GetCustomAttribute<RestmeDbColumnAttribute>(true);
                if (attribute != null && !dic.ContainsKey(attribute.DbColumnName))
                {
                    dic.Add(attribute.DbColumnName, attribute);
                }
            }

            var keyName = typeof(A).AssemblyQualifiedName;
            if (keyName.IsNotNullOrEmpty())
            {
                lock (_defaultAttributesFromTypes)
                {
                    try
                    {
                        if (_defaultAttributesFromTypes.ContainsKey(keyName))
                        {
                            _defaultAttributesFromTypes[keyName] = dic;
                            RestmeDb.Logger?.LogDebug(
                                $"Adding column attribute repository for an existing identified type - which is unlikely to happen.\n Type: {keyName}");
                        }
                        else
                            _defaultAttributesFromTypes.Add(keyName, dic);
                    }
                    catch (Exception ex)
                    {
                        RestmeDb.Logger?.LogDebug(
                            $"DEBUGGING POTENTIAL ERROR:   Key Name: {keyName}   DIC Count: {dic?.Count}   defaultAttributeFromTypes: {_defaultAttributesFromTypes == null}  -- {_defaultAttributesFromTypes?.Count}");
                        throw ex;
                    }
                }
            }

            return dic;
        }

        private Dictionary<string, string> GenerateDefaultColumnsInQuery<A>(
            string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null, bool? inSelect = null, bool? inInsert = null, bool? inUpdate = null,
            bool? inDelete = null) where A : IRestmeDbEntity
        {
            var defaultTypeAttributes = GetTypeColumnAttributes<A>();
            var tableAttribute = GetTableAttribute<A>();
            var columnAttributes = defaultTypeAttributes?.Where(dic =>
                                       (inSelect == null || dic.Value.InSelect == inSelect) &&
                                       (inInsert == null || dic.Value.InInsert == inInsert) &&
                                       (inUpdate == null || dic.Value.InUpdate == inUpdate) &&
                                       (inDelete == null || dic.Value.InDelete == inDelete) &&
                                       (chosenColumnsOnly == null ||
                                        chosenColumnsOnly.Contains(dic.Key)) &&
                                       (columnsToExclude == null ||
                                        !columnsToExclude.Contains(dic.Key)) &&
                                       (
                                           tableAttribute.ExcludedColumns == null ||
                                           !tableAttribute.ExcludedColumns.Contains(
                                               dic.Key)
                                       ))?.ToDictionary(d => d.Key, d => d.Value.DbColumnName) ??
                                   new Dictionary<string, string>();

            if (chosenColumnsOnly == null || !chosenColumnsOnly.Any()) return columnAttributes;
            var unIdentifiedColumns =
                chosenColumnsOnly.Where(item => defaultTypeAttributes?.ContainsKey(item) != true);
            var identifiedColumns = unIdentifiedColumns as string[] ?? unIdentifiedColumns.ToArray();
            if (!identifiedColumns.Any()) return columnAttributes;
            foreach (var col in identifiedColumns)
            {
                columnAttributes.Add(Guid.NewGuid().ToString(), col);
            }

            return columnAttributes;
        }


        public virtual Dictionary<string, string> MapSelectColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity
            =>
                GenerateDefaultColumnsInQuery<A>(chosenColumnsOnly,
                    columnsToExclude, inSelect: true);

        public virtual Dictionary<string, string> MapUpdateColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity
            =>
                GenerateDefaultColumnsInQuery<A>(chosenColumnsOnly,
                    columnsToExclude, inUpdate: true);

        public virtual Dictionary<string, string> MapInsertColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity
            =>
                GenerateDefaultColumnsInQuery<A>(chosenColumnsOnly,
                    columnsToExclude, inInsert: true);

        public virtual Dictionary<string, string> MapDeleteColumns<A>(string[] chosenColumnsOnly = null,
            string[] columnsToExclude = null) where A : IRestmeDbEntity
            =>
                GenerateDefaultColumnsInQuery<A>(chosenColumnsOnly,
                    columnsToExclude, inDelete: true);
    }
}