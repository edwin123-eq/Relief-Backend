using System.Data;
using Microsoft.Data.SqlClient;

namespace ReliefApi
{
    public class DapperContext
    {
        
        private readonly string _connectionString;

        public DapperContext(string connectionString)
        {
            _connectionString = connectionString;
        }
                
        public IDbConnection CreateConnection()  => new SqlConnection(_connectionString);
    }
}
