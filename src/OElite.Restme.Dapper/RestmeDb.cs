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
            Connection = NewConnection();
        }

        public RestmeDb(string connectionString)
        {
            _dbConnectionString = connectionString;
            Connection = NewConnection();
        }

        public IDbConnection Connection { get; }

        public IDbTransaction NewTransaction()
        {
            return Connection.BeginTransaction();
        }


        public void Dispose()
        {
            DisposeConnection(Connection);
        }


        private IDbConnection NewConnection(string connectionString = null)
        {
            connectionString = connectionString ?? _dbConnectionString;
            var conn = new SqlConnection(connectionString);
            conn.Open();
            if (DefaultConnectionString.IsNullOrEmpty())
                DefaultConnectionString = connectionString;
            return conn;
        }


        private static void DisposeConnection(IDbConnection dbConn)
        {
            try
            {
                if (dbConn != null && dbConn.State != ConnectionState.Closed)
                    dbConn.Dispose();
            }
            catch
            {
                //ignore exceptions
            }
        }

        public T DbQuery<TE, T>() where T : RestmeDbQuery<TE> where TE : IRestmeDbEntity
        {
            var query = _dbQueries.FirstOrDefault(item => item is T);
            if (query != null) return (T) query;

            query = (T) Activator.CreateInstance(typeof(T), this);
            _dbQueries.Add(query);
            return (T) query;
        }
    }
}