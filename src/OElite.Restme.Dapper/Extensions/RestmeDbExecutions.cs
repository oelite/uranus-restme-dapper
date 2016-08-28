using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace OElite.Restme.Dapper
{
    public partial class RestmeDb
    {
        public async Task<T> FetchAsync<T>(string standardQuery, object paramValues, CommandType? dbCommandType = null)
            where T : class
        {
            var result =
                await
                    (await GetOpenConnectionAsync()).QueryFirstOrDefaultAsync<T>(standardQuery, paramValues,
                        _currentTransaction, commandType: dbCommandType);
            return result;
        }

        public async Task<TC> FetchAsync<T, TC>(string query, object paramValues, bool paginatedQuery = false,
            CommandType? dbCommandType = null)
            where TC : IRestmeDbEntityCollection<T>, new() where T : IRestmeDbEntity
        {
            var resultSet = new TC();
            if (paginatedQuery)
            {
                var results =
                    await
                        (await GetOpenConnectionAsync()).QueryMultipleAsync(query, paramValues, _currentTransaction,
                            commandType: dbCommandType);
                var totalCount = await results.ReadSingleAsync<int>();
                var result = (await results.ReadAsync<T>()).ToList();
                if (totalCount <= 0) return resultSet;

                resultSet.TotalRecordsCount = Convert.ToInt32(totalCount);
                if (result.Any())
                    resultSet.AddRange(result);
            }
            else
            {
                var results =
                    await
                        (await GetOpenConnectionAsync()).QueryAsync<T>(query, paramValues, _currentTransaction,
                            commandType: dbCommandType);
                var enumerable = results as IList<T> ?? results.ToList();
                if (enumerable.Any())
                    resultSet.AddRange(enumerable);
            }
            return resultSet;
        }

        public async Task<long> ExecuteInsertAsync(string standardQuery, object paramValues,
            CommandType? dbCommandType = null)
        {
            return
                await
                    (await GetOpenConnectionAsync()).QuerySingleOrDefaultAsync<long>(standardQuery, paramValues,
                        _currentTransaction, commandType: dbCommandType);
        }

        public async Task<T> ExecuteInsertAsync<T>(string standardQuery, object paramValues,
            CommandType? dbCommandType = null)
        {
            return
                await
                    (await GetOpenConnectionAsync()).QuerySingleOrDefaultAsync<T>(standardQuery, paramValues,
                        _currentTransaction, commandType: dbCommandType);
        }

        public async Task<int> ExecuteAsync(string standardQuery, object paramValues, CommandType? dbCommandType = null)
        {
            return
                await
                    (await GetOpenConnectionAsync()).ExecuteAsync(standardQuery, paramValues, _currentTransaction, commandType: dbCommandType);
        }
    }
}