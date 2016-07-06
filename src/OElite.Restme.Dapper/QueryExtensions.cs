using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace OElite.Restme.Dapper
{
    public static class QueryExtensions
    {
        public static OEliteDbQueryString Paginated(this OEliteDbQueryString query, int pageIndex, int pageSize,
            string outerOrderByClause = null)
        {
            var offset = pageIndex * pageSize;
            if (outerOrderByClause.IsNullOrEmpty() && !query.Query.ToLower().Contains(" order by "))
                throw new ArgumentException("invalid order order by clause.");

            var newQuery = string.Empty;
            var orderByIndex = query.Query.IndexOf(" order by ", StringComparison.CurrentCultureIgnoreCase);
            var queryWithoutOrderby = string.Empty;
            if (orderByIndex >= 0)
            {
                queryWithoutOrderby = query.Query.Substring(0, orderByIndex);
                newQuery = $"select count(*) from ({queryWithoutOrderby}) resultSet;";
            }

            if (pageIndex >= 0 && pageSize > 0)
            {
                if (outerOrderByClause.IsNullOrEmpty())
                    query.Query = $"{query.Query} " +
                                  $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                else
                {
                    if (orderByIndex >= 0)
                        query.Query = queryWithoutOrderby;
                    query.Query = $"select * from ({query.Query}) resultSet order by {outerOrderByClause} " +
                                  $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                }
            }
            query.Query = newQuery + query.Query;
            return query;
        }


        public static OEliteDbQueryString Query(this IRestmeDbQuery dbQuery, string whereConditionClause = null,
            string orderByClause = null,
            string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
        {
            var tableSource = dbQuery.CustomSelectTableSource ?? dbQuery.DbTableName;
            if (orderByClause.IsNullOrEmpty())
                orderByClause = dbQuery.DefaultOrderByClauseInQuery;

            dbQuery.MapSelectColumns(choosenPropertiesOnly, propertiesToExclude);
            var selectedColumnsInQuery = dbQuery.DefaultColumnsInQuery.Values;

            var query = $"select {string.Join(", ", selectedColumnsInQuery)} " +
                        $"from {tableSource} " +
                        (whereConditionClause.IsNullOrEmpty() ? "" : $"where ({whereConditionClause}) ") +
                        (orderByClause.IsNullOrEmpty() ? "" : $"order by {orderByClause}");
            return new OEliteDbQueryString(query, dbCentre: dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Insert<T>(this IRestmeDbQuery dbQuery, T data,
            string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null) where T : IRestmeDbEntity
        {
            dbQuery.MapInsertColumns(choosenPropertiesOnly, propertiesToExclude);
            ((RestmeDbQuery<T>)dbQuery).PrepareParamValues(data, choosenPropertiesOnly, propertiesToExclude);

            var query =
                $"insert into {dbQuery.DbTableName}({string.Join(",", dbQuery.ParamValues.Select(c => c.Key))}) " +
                $" values({string.Join(",", dbQuery.ParamValues.Select(c => "@" + c.Key))});" +
                $" SELECT CAST(SCOPE_IDENTITY() as bigint)";

            var paramValues = new ExpandoObject();
            var dic = (IDictionary<string, object>)paramValues;
            dbQuery.ParamValues.ToList().ForEach(item => dic[item.Key] = item.Value);
            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Update<T>(this IRestmeDbQuery dbQuery, T data,
            string whereConditionClause, string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            where T : IRestmeDbEntity
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Update without condition will update all data records of the requested table(s), the action is disabled for data protection.");

            dbQuery.MapUpdateColumns(choosenPropertiesOnly, propertiesToExclude);
            ((RestmeDbQuery<T>)dbQuery).PrepareParamValues(data, choosenPropertiesOnly, propertiesToExclude);

            var query =
                $"update {dbQuery.DbTableName} set {string.Join(", ", dbQuery.ParamValues.Select(c => c.Key + " = @" + c.Key))} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");

            var paramValues = new ExpandoObject();
            var dic = (IDictionary<string, object>)paramValues;
            dbQuery.ParamValues.ToList().ForEach(item => dic[item.Key] = item.Value);
            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Delete<T>(this IRestmeDbQuery dbQuery, T data,
            string whereConditionClause, string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            where T : IRestmeDbEntity
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Delete without condition will remove all data of the requested table(s), the action is disabled for data protection.");
            dbQuery.MapDeleteColumns(choosenPropertiesOnly, propertiesToExclude);
            ((RestmeDbQuery<T>)dbQuery).PrepareParamValues(data, choosenPropertiesOnly, propertiesToExclude);

            var query =
                $"delete from {dbQuery.DbTableName} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");

            var paramValues = new ExpandoObject();
            var dic = (IDictionary<string, object>)paramValues;
            dbQuery.ParamValues.ToList().ForEach(item => dic[item.Key] = item.Value);
            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Delete(this IRestmeDbQuery dbQuery, string whereConditionClause,
            dynamic paramValues)
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Delete without condition will remove all data of the requested table(s), the action is disabled for data protection.");
            var query =
                $"delete from {dbQuery.DbTableName} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");
            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        #region Data Execution

        public static Task<T> FetchAsync<T>(this OEliteDbQueryString query)
            where T : class, IRestmeDbEntity
        {
            return query.DbCentre.FetchAsync<T>(query.Query, query.ParamValues);
        }

        public static Task<TC> FetchAsync<T, TC>(this OEliteDbQueryString query)
            where TC : IRestmeDbEntityCollection<T>, new() where T : IRestmeDbEntity
        {
            return

                    query.DbCentre.FetchAsync<T, TC>(query.Query, query.ParamValues);
        }

        public static Task<long> ExecuteInsertAsync(this OEliteDbQueryString query)
        {
            return query.DbCentre.ExecuteInsertAsync(query.Query, query.ParamValues);
        }

        public static Task<int> ExecuteAsync(this OEliteDbQueryString query)
        {
            return query.DbCentre.ExecuteAsync(query.Query, query.ParamValues);
        }

        #endregion
    }
}