using ReliefApi.Contracts;
using ReliefApi.Models;
using Dapper;

namespace ReliefApi.Services
{
    public class Dbcodes : IDbcodes
    {
        private readonly DapperContext _db;
        public Dbcodes(DapperContext db)
        {
            _db = db;
        }

        public async Task<DbPostCode> GetPostCodeByTag(string tag, string type)
        {
            using (var connection = _db.CreateConnection()) // Assumes CreateConnection() returns IDbConnection
            {
                var Obj = new DbPostCode();

                if (type == "A")
                {
                    Obj = connection.QuerySingleOrDefault<Models.DbPostCode>(
                        @"SELECT DBPOSTCODES.*, ISNULL(ACMASTER.ACACNAME,'') AS DBNAME 
                  FROM DbPostCodes 
                  LEFT JOIN ACMASTER ON DBPOSTCODES.DBID = ACMASTER.ACID 
                  WHERE DBPOSTCODES.DBTAG=@TG AND DBTYPE=@TY",
                        new { TG = tag, TY = type }
                    );
                }
                else
                {
                    Obj = connection.QuerySingleOrDefault<Models.DbPostCode>(
                        @"SELECT DBPOSTCODES.*, ISNULL(SCHEDULES.SHNAME,'') AS DBNAME 
                  FROM DbPostCodes 
                  LEFT JOIN SCHEDULES ON DBPOSTCODES.DBID = SCHEDULES.SHID 
                  WHERE DBPOSTCODES.DBTAG=@TG AND DBTYPE=@TY",
                        new { TG = tag, TY = type }
                    );
                }

                if (Obj == null)
                {
                    Obj = new DbPostCode();
                }

                return Obj;
            }
        }
    }
}