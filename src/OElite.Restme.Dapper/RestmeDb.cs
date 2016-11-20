using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace OElite.Restme.Dapper
{
    public partial class RestmeDb : IDisposable
    {
        private readonly List<IRestmeDbQuery> _dbQueries = new List<IRestmeDbQuery>();

        private readonly string _dbConnectionString = null;

        public RestmeDb()
        {
            if (DefaultConnectionString.IsNullOrEmpty())
                throw new ArgumentException(
                    $"DefaultConnectionString is not present - try instantiate {this.GetType().Name} with a valid connection string first.");
        }

        public RestmeDb(string connectionString)
        {
            _dbConnectionString = connectionString;
        }


        public void Dispose()
        {
        }

        private IDbConnection _currentConnection { get; set; }
        private IDbTransaction _currentTransaction { get; set; }

        private async Task<IDbConnection> GetOpenConnectionAsync(bool establishTransaction = false,
            string connectionString = null)
        {
            connectionString = connectionString ?? _dbConnectionString;
            if (_currentConnection == null)
            {
                var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                if (DefaultConnectionString.IsNullOrEmpty())
                    DefaultConnectionString = connectionString;
                _currentConnection = conn;
            }
            if (establishTransaction)
                await GetDbTransactionAsync();
            return _currentConnection;
        }

        public async Task<IDbTransaction> GetDbTransactionAsync()
        {
            _currentTransaction = _currentTransaction ?? (await GetOpenConnectionAsync()).BeginTransaction();
            return _currentTransaction;
        }


        public T DbQuery<TE, T>() where T : RestmeDbQuery<TE> where TE : IRestmeDbEntity
        {
            var query = _dbQueries.FirstOrDefault(item => item is T);
            if (query != null) return (T) query;

            query = (T) Activator.CreateInstance(typeof(T), new object[] {this});
            _dbQueries.Add(query);
            return (T) query;
        }

        public RestmeDbQuery<T> DbQuery<T>() where T : IRestmeDbEntity
        {
            var query = _dbQueries.FirstOrDefault(item => item is RestmeDbQuery<T>);
            if (query != null) return (RestmeDbQuery<T>) query;

            var genericType = typeof(RestmeDbQuery<>);

            var typeWithGeneric = genericType.MakeGenericType(new[] {typeof(T)});

            query = (RestmeDbQuery<T>) Activator.CreateInstance(typeWithGeneric, new object[] {this});
            _dbQueries.Add(query);
            return (RestmeDbQuery<T>) query;
        }
    }
}