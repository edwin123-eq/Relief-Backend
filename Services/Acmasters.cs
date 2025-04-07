using Dapper;
using System;
using System.Data;
using ReliefApi.Contracts;

namespace ReliefApi.Services
{
    public class Acmasters : IACmaster
    {
        private readonly DapperContext db;

        public Acmasters(DapperContext db)
        {
            this.db = db;
        }
        public async Task<decimal> GetAccountBalanceById(long acId, long vid, DateTime tranDate, long counterId)
        {
          
            if (acId != 0)
            {
                using (var connection = db.CreateConnection()) // Ensure db.CreateConnection() returns a valid IDbConnection
                {
                    if (vid == 0)
                    {
                        return await connection.ExecuteScalarAsync<decimal>(
                            @"SELECT ISNULL(DB.BAL, 0) AS BALANCE
                              FROM ACMASTER
                              LEFT JOIN (
                                  SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL
                                  FROM DAYBOOK
                                  WHERE DBACID = @ACID AND VOUCHERID <> @VID
                                  AND (DBDATE <= @TRANDATE OR DAYBOOK.DBVCHTYPE = 'OP')
                                  AND (DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                                  GROUP BY DBACID
                              ) AS DB ON ACMASTER.ACID = DB.DBACID
                              WHERE ACID = @ACID",
                            new { ACID = acId, TRANDATE = tranDate, VID = vid, COUNTERID = counterId });
                    }
                    else
                    {
                        return await connection.ExecuteScalarAsync<decimal>(
                            @"SELECT ISNULL(DB.BAL, 0) AS BALANCE
                              FROM ACMASTER
                              LEFT JOIN (
                                  SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL
                                  FROM DAYBOOK
                                  WHERE DBACID = @ACID AND VOUCHERID <> @VID
                                  AND (DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                                  AND (DBDATE < @TRANDATE OR (DBDATE = @TRANDATE AND VOUCHERID < @VID) OR DAYBOOK.DBVCHTYPE = 'OP')
                                  GROUP BY DBACID
                              ) AS DB ON ACMASTER.ACID = DB.DBACID
                              WHERE ACID = @ACID",
                            new { ACID = acId, TRANDATE = tranDate, VID = vid, COUNTERID = counterId });
                    }
                }
            }

            return 0; // Default return if acId == 0 (implicit in VB.NET, explicit here)
        }
    }
}
