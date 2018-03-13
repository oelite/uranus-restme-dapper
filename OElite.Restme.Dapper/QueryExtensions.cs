using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;

namespace OElite.Restme.Dapper
{
    public static class QueryExtensions
    {
        public static OEliteDbQueryString Paginated(this OEliteDbQueryString query, int pageIndex, int pageSize,
            string outerOrderByClause = null)
        {
            var offset = pageIndex * pageSize;
            if (outerOrderByClause.IsNullOrEmpty() && !query.Query.ToLower().Contains(" order by "))
                throw new ArgumentException("invalid order by clause.");

            string newQuery;
            var orderByIndex = query.Query.IndexOf(" order by ", StringComparison.CurrentCultureIgnoreCase);
            var queryWithoutOrderby = string.Empty;
            if (orderByIndex >= 0)
            {
                queryWithoutOrderby = query.Query.Substring(0, orderByIndex);
                newQuery = $"select count(*) from ({queryWithoutOrderby}) resultSet;";
            }
            else
                newQuery = $"select count(*) from ({query.Query}) resultSet;";

            if (pageIndex >= 0 && pageSize > 0)
            {
                query.IsPaginated = true;
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

                query.Query = newQuery + query.Query;
            }
            else
            {
                query.IsPaginated = false;
                if (outerOrderByClause.IsNotNullOrEmpty())
                {
                    if (orderByIndex >= 0)
                        query.Query = queryWithoutOrderby;
                    query.Query = $"select * from ({query.Query}) resultSet order by {outerOrderByClause} ";
                }
            }

            return query;
        }

        public static OEliteDbQueryString Query<T, TA>(this IRestmeDbQuery<TA> dbQuery,
            string whereConditionClause = null,
            string orderByClause = null,
            string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            where T : IRestmeDbEntity where TA : IRestmeDbEntity
        {
            var tableSource = dbQuery.CustomSelectTableSource ?? dbQuery.DefaultTableSource;
            if (orderByClause.IsNullOrEmpty())
                orderByClause = dbQuery.DefaultOrderByClauseInQuery;


            var columnsInQuery = dbQuery.MapSelectColumns<T>(choosenPropertiesOnly, propertiesToExclude);
            var selectedColumnsInQuery = columnsInQuery.Values;

            var query = $"select {string.Join(", ", selectedColumnsInQuery)} " +
                        $"from {tableSource} " +
                        (whereConditionClause.IsNullOrEmpty() ? "" : $"where ({whereConditionClause}) ") +
                        (orderByClause.IsNullOrEmpty() ? "" : $"order by {orderByClause}");


            return new OEliteDbQueryString(query, dbCentre: dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Query<T>(this IRestmeDbQuery<T> dbQuery, string whereConditionClause = null,
            string orderByClause = null,
            string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null) where T : IRestmeDbEntity
        {
            return Query<T, T>(dbQuery, whereConditionClause, orderByClause, choosenPropertiesOnly,
                propertiesToExclude);
        }

        public static OEliteDbQueryString FullQuery<T>(this IRestmeDbQuery<T> dbQuery, string fullQuery)
            where T : IRestmeDbEntity
        {
            return new OEliteDbQueryString(fullQuery, dbCentre: dbQuery.DbCentre ?? new RestmeDb());
        }


        public static OEliteDbQueryString Insert<T, TA>(this IRestmeDbQuery<TA> dbQuery, T data,
            string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null, bool expectIdentityScope = true)
            where T : IRestmeDbEntity where TA : IRestmeDbEntity
        {
            var paramValues = dbQuery.PrepareParamValues(RestmeDbQueryType.Insert, data, choosenPropertiesOnly,
                propertiesToExclude);

            var query =
                $"insert into {dbQuery.CustomInsertTableSource ?? dbQuery.DefaultTableSource}({string.Join(",", paramValues.Select(c => c.Key))}) " +
                $" values({string.Join(",", paramValues.Select(c => "@" + c.Key))});";
            var result = new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());

            if (!expectIdentityScope) return result;

            query = query + $" SELECT CAST(SCOPE_IDENTITY() as bigint)";
            result.Query = query;
            result.IsExpectingIdentityScope = true;

            return result;
        }


        public static OEliteDbQueryString Update<T, TA>(this IRestmeDbQuery<TA> dbQuery, T data,
            string whereConditionClause, string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            where T : IRestmeDbEntity where TA : IRestmeDbEntity
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Update without condition will update all data records of the requested table(s), the action is disabled for data protection.");

            //var columnsInQuery = dbQuery.MapUpdateColumns<T>(choosenPropertiesOnly, propertiesToExclude);
            var paramUpdateValues = dbQuery.PrepareParamValues(RestmeDbQueryType.Update, data, choosenPropertiesOnly,
                propertiesToExclude);
            var paramValues = dbQuery.PrepareParamValues(RestmeDbQueryType.Select, data, choosenPropertiesOnly,
                propertiesToExclude);

            var query =
                $"update {dbQuery.CustomUpdateTableSource ?? dbQuery.DefaultTableSource} set {string.Join(", ", paramUpdateValues.Select(c => c.Key + " = @" + c.Key))} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");

            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Update<T, TA>(this IRestmeDbQuery<TA> dbQuery, T data,
            Dictionary<string, string> updateColumnNamesMatchedWithPropertyNames, string whereConditionClause,
            string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            where TA : IRestmeDbEntity where T : IRestmeDbEntity
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Update without condition will update all data records of the requested table(s), the action is disabled for data protection.");
            if ((updateColumnNamesMatchedWithPropertyNames?.Count).GetValueOrDefault() == 0)
                throw new ArgumentException("Cannot update without update columns.");

            var paramValues = dbQuery.PrepareParamValues(RestmeDbQueryType.Select, data, choosenPropertiesOnly,
                propertiesToExclude);

            var query =
                $"update {dbQuery.CustomUpdateTableSource ?? dbQuery.DefaultTableSource} set {string.Join(", ", updateColumnNamesMatchedWithPropertyNames.Select(c => c.Key + " = @" + c.Value))} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");

            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Update<TA>(this IRestmeDbQuery<TA> dbQuery,
            Dictionary<string, string> updateColumnNamesMatchedWithPropertyNames, string whereConditionClause,
            dynamic paramValues) where TA : IRestmeDbEntity
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Update without condition will update all data records of the requested table(s), the action is disabled for data protection.");
            if ((updateColumnNamesMatchedWithPropertyNames?.Count).GetValueOrDefault() == 0)
                throw new ArgumentException("Cannot update without update columns.");

            var query =
                $"update {dbQuery.CustomUpdateTableSource ?? dbQuery.DefaultTableSource} set {string.Join(", ", updateColumnNamesMatchedWithPropertyNames.Select(c => c.Key + " = @" + c.Value))} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");

            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Delete<T, TA>(this IRestmeDbQuery<TA> dbQuery, T data,
            string whereConditionClause, string[] choosenPropertiesOnly = null, string[] propertiesToExclude = null)
            where T : IRestmeDbEntity where TA : IRestmeDbEntity
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Delete without condition will remove all data of the requested table(s), the action is disabled for data protection.");

            var paramValues = dbQuery.PrepareParamValues(RestmeDbQueryType.Delete, data, choosenPropertiesOnly,
                propertiesToExclude);

            var query =
                $"delete from {dbQuery.CustomDeleteTableSource ?? dbQuery.DefaultTableSource} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");

            return new OEliteDbQueryString(query, paramValues, dbQuery.DbCentre ?? new RestmeDb());
        }

        public static OEliteDbQueryString Delete<T>(this IRestmeDbQuery<T> dbQuery, string whereConditionClause)
            where T : IRestmeDbEntity
        {
            if (whereConditionClause.IsNullOrEmpty())
                throw new ArgumentException(
                    "Delete without condition will remove all data of the requested table(s), the action is disabled for data protection.");

            var query =
                $"delete from {dbQuery.CustomDeleteTableSource ?? dbQuery.DefaultTableSource} " +
                (whereConditionClause.IsNullOrEmpty() ? "" : $" where ({whereConditionClause}) ");

            return new OEliteDbQueryString(query, null, dbQuery.DbCentre ?? new RestmeDb());
        }


        #region Data Execution

        public static Task<T> FetchAsync<T>(this OEliteDbQueryString query, CommandType? dbCommandType = null,
            int commandTimeout = 0)
            where T : class
        {
            try
            {
                return query.DbCentre.FetchAsync<T>(query.Query, query.ParamValues, dbCommandType, commandTimeout);
            }
            catch (Exception ex)
            {
                RestmeDb.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        public static Task<TC> FetchAsync<T, TC>(this OEliteDbQueryString query, CommandType? dbCommandType = null,
            int commandTimeout = 0)
            where TC : IRestmeDbEntityCollection<T>, new() where T : IRestmeDbEntity
        {
            try
            {
                return
                    query.DbCentre.FetchAsync<T, TC>(query.Query, query.ParamValues, query.IsPaginated, dbCommandType,
                        commandTimeout);
            }
            catch (Exception ex)
            {
                RestmeDb.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        public static async Task<long> ExecuteInsertAsync(this OEliteDbQueryString query,
            CommandType? dbCommandType = null, int commandTimeout = 0)
        {
            try
            {
                if (query.IsExpectingIdentityScope)
                    return await query.DbCentre.ExecuteInsertAsync(query.Query, query.ParamValues, dbCommandType,
                        commandTimeout);
                return await query.DbCentre.ExecuteAsync(query.Query, query.ParamValues, dbCommandType, commandTimeout);
            }
            catch (Exception ex)
            {
                RestmeDb.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        public static Task<T> ExecuteInsertAsync<T>(this OEliteDbQueryString query, CommandType? dbCommandType = null,
            int commandTimeout = 0)
        {
            try
            {
                return query.DbCentre.ExecuteInsertAsync<T>(query.Query, query.ParamValues, dbCommandType,
                    commandTimeout);
            }
            catch (Exception ex)
            {
                RestmeDb.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        public static Task<int> ExecuteAsync(this OEliteDbQueryString query, CommandType? dbCommandType = null,
            int commandTimeout = 0)
        {
            try
            {
                return query.DbCentre.ExecuteAsync(query.Query, query.ParamValues, dbCommandType, commandTimeout);
            }
            catch (Exception ex)
            {
                RestmeDb.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        public static Task<T> ExecuteScalarAsync<T>(this OEliteDbQueryString query, CommandType? dbCommandType = null,
            int commandTimeout = 0)
        {
            try
            {
                return query.DbCentre.ExecuteScalarAsync<T>(query.Query, query.ParamValues, dbCommandType,
                    commandTimeout);
            }
            catch (Exception ex)
            {
                RestmeDb.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        public static Task<IList<T>> FetchEnumerableAsync<T>(this OEliteDbQueryString query,
            CommandType? dbCommandType = null, int commandTimeout = 0)
        {
            try
            {
                return query.DbCentre.FetchEnumerableAsync<T>(query.Query, query.ParamValues, query.IsPaginated,
                    dbCommandType, commandTimeout);
            }
            catch (Exception ex)
            {
                RestmeDb.Logger?.LogError(ex.Message, ex);
                throw;
            }
        }

        #endregion
    }
}