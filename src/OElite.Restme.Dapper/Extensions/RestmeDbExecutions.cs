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
        public Task<T> FetchAsync<T>(string standardQuery, object paramValues)
            where T : class
        {
            return Task.FromResult(Connection.QueryFirstOrDefault<T>(standardQuery, paramValues));
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

        public Task<long> ExecuteInsertAsync(string standardQuery, object paramValues)
        {
            return Task.FromResult(Connection.QuerySingleOrDefault<long>(standardQuery, paramValues));
        }

        public Task<int> ExecuteAsync(string standardQuery, object paramValues)
        {
            return Task.FromResult(Connection.Execute(standardQuery, paramValues));
        }
    }
}