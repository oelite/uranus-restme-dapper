using System;
using System.Collections;
using System.Collections.Generic;
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

    public abstract class RestmeDbQuery<T> : IRestmeDbQuery where T : IRestmeDbEntity
    {
        public RestmeDb DbCentre { get; set; }

        public virtual string CustomSelectTableSource { get; }

        public Dictionary<string, string> DefaultColumnsInQuery { get; set; } = new Dictionary<string, string>();

        public virtual string DefaultOrderByClauseInQuery { get; }
        public string DbTableName { get; }


        public Dictionary<string, object> ParamValues { get; } = new Dictionary<string, object>();


        protected RestmeDbQuery(RestmeDb dbCentre)
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
                ParamValues.Add(prop.Key, data.GetPropertyValue(prop.Key));
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

        public virtual void MapSelectColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            => DefaultColumnsInQuery = ColumnAttributes.Where(dic => dic.Value.InSelect &&
                                                                     (choosenPropertiesOnly == null ||
                                                                      choosenPropertiesOnly.Contains(dic.Key)) &&
                                                                     (propertiesToExclude == null ||
                                                                      !propertiesToExclude.Contains(dic.Key)))
                .ToDictionary(d => d.Key, d => d.Value.DbColumnName);

        public virtual void MapUpdateColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            => DefaultColumnsInQuery = ColumnAttributes.Where(dic => dic.Value.InSelect &&
                                                                     (choosenPropertiesOnly == null ||
                                                                      choosenPropertiesOnly.Contains(dic.Key)) &&
                                                                     (propertiesToExclude == null ||
                                                                      !propertiesToExclude.Contains(dic.Key)))
                .ToDictionary(d => d.Key, d => d.Value.DbColumnName);

        public virtual void MapInsertColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            => DefaultColumnsInQuery = ColumnAttributes.Where(dic => dic.Value.InSelect &&
                                                                     (choosenPropertiesOnly == null ||
                                                                      choosenPropertiesOnly.Contains(dic.Key)) &&
                                                                     (propertiesToExclude == null ||
                                                                      !propertiesToExclude.Contains(dic.Key)))
                .ToDictionary(d => d.Key, d => d.Value.DbColumnName);

        public virtual void MapDeleteColumns(string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            => DefaultColumnsInQuery = ColumnAttributes.Where(dic => dic.Value.InSelect &&
                                                                     (choosenPropertiesOnly == null ||
                                                                      choosenPropertiesOnly.Contains(dic.Key)) &&
                                                                     (propertiesToExclude == null ||
                                                                      !propertiesToExclude.Contains(dic.Key)))
                .ToDictionary(d => d.Key, d => d.Value.DbColumnName);


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