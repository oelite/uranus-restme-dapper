using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OElite.Restme.Dapper
{
    public partial class RestmeDb : IDisposable
    {
        private readonly List<IRestmeDbQuery<IRestmeDbEntity>> _dbQueries = new List<IRestmeDbQuery<IRestmeDbEntity>>();

        private readonly string _dbConnectionString = null;

        public RestmeDb(ILogger logger = null)
        {
            if (logger != null)
                Logger = logger;
            if (DefaultConnectionString.IsNullOrEmpty())
                throw new ArgumentException(
                    $"DefaultConnectionString is not present - try instantiate {this.GetType().Name} with a valid connection string first.");
        }

        public RestmeDb(string connectionString, ILogger logger = null)
        {
            if (logger != null)
                Logger = logger;
            _dbConnectionString = connectionString;
        }


        bool disposed = false;

        public int ExecutionPerformanceThresholdInMs = 500;

        public void Dispose()
        {
            Dispose(true);
            GC.Collect();
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                try
                {
                    _currentTransaction?.Dispose();
                }
                catch
                {
                }

                try
                {
                    _currentConnection?.Close();
                }
                catch
                {
                }

                try
                {
                    _currentConnection?.Dispose();
                }
                catch
                {
                }
            }

            disposed = true;
        }

        private IDbConnection _currentConnection;
        private IDbTransaction _currentTransaction;

        private async Task<IDbConnection> GetOpenConnectionAsync(bool establishTransaction = false,
            string connectionString = null)
        {
            if (_currentConnection == null || _currentConnection.State == ConnectionState.Closed)
            {
                connectionString ??= _dbConnectionString;
                _currentConnection = new SqlConnection(connectionString);
                await ((SqlConnection)_currentConnection).OpenAsync();
            }

            ;

            if (DefaultConnectionString.IsNullOrEmpty())
                DefaultConnectionString = connectionString;

            if (establishTransaction)
                await GetDbTransactionAsync();

            return _currentConnection;
        }

        public async Task<IDbTransaction> GetDbTransactionAsync()
        {
            if (_currentTransaction == null)
                _currentTransaction = await NewDbTransactionAsync();
            return _currentTransaction;
        }

        public async Task<IDbTransaction> NewDbTransactionAsync()
        {
            return (await GetOpenConnectionAsync()).BeginTransaction();
        }


        public T DbQuery<TE, T>() where T : IRestmeDbQuery<TE> where TE : IRestmeDbEntity
        {
            var query = _dbQueries.FirstOrDefault(item => item is T);
            if (query != null) return (T)query;

            var genericType = typeof(T);

            var typeWithGeneric = genericType.MakeGenericType(new[] { typeof(TE) });

            query = (IRestmeDbQuery<IRestmeDbEntity>)Activator.CreateInstance(typeWithGeneric, new object[] { this });
            if (query != null)
                _dbQueries.Add(query);
            return (T)query;
        }

        public IRestmeDbQuery<T> DbQuery<T>(string customSelectTableSource = null,
            string customInsertTableSource = null, string customUpdateTableSource = null,
            string customDeleteTableSource = null) where T : IRestmeDbEntity
        {
            var query = _dbQueries.FirstOrDefault(item => item is IRestmeDbQuery<T>);
            if (query != null) return (IRestmeDbQuery<T>)query;

            return new RestmeDbQuery<T>(this, customSelectTableSource, customInsertTableSource, customUpdateTableSource,
                customDeleteTableSource);
        }
    }
}