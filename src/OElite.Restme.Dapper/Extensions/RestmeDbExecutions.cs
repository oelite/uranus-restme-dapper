using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Dapper;

namespace OElite.Restme.Dapper
{
    public partial class RestmeDb
    {
        public async Task<T> FetchAsync<T>(string standardQuery, object paramValues)
            where T : class
        {
            var result =
                await Connection.QueryFirstOrDefaultAsync<T>(standardQuery, paramValues);
            return (T) result;
        }

        public async Task<TC> FetchAsync<T, TC>(string query, object paramValues)
            where TC : IRestmeDbEntityCollection<T>, new() where T : IRestmeDbEntity
        {
            var resultSet = new TC();
            var results =
                await Connection.QueryMultipleAsync(query, paramValues);
            var totalCount = await results.ReadSingleAsync<int>();
            var result = (await results.ReadAsync<T>()).ToList();
            if (totalCount <= 0) return resultSet;

            resultSet.TotalRecordsCount = Convert.ToInt32(totalCount);
            if (result.Any())
                resultSet.AddRange(result);

            return resultSet;
        }

        public async Task<long> ExecuteInsertAsync(string standardQuery, object paramValues)
        {
            return await Connection.QuerySingleOrDefaultAsync<long>(standardQuery, paramValues);
        }

        public async Task<int> ExecuteAsync(string standardQuery, object paramValues)
        {
            return await Connection.ExecuteAsync(standardQuery, paramValues);
        }
    }
}