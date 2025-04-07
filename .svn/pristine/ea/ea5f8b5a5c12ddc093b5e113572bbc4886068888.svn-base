using ReliefApi.Models;
using Dapper;
using System.Data;
using ReliefApi.Contracts;

namespace ReliefApi.Services
{
    public class Sales:ISale
    {
        private readonly DapperContext db;

        public Sales(DapperContext dapperContext)
        {
            db = dapperContext;
        }

        public async Task<Models.SalesDailySummary> SaleDailyProfitSummary(DateTime fromDt, int excludeTax)
        {
            using (var connection = db.CreateConnection()) // Ensure db.CreateConnection() returns a valid IDbConnection
            {
                try
                {
                    var obj = new SalesDailySummary();

                    obj = await connection.QuerySingleOrDefaultAsync<SalesDailySummary>(
                        @"SELECT SUM(A.AMOUNT) AMOUNT, SUM(A.COST) COST, SUM(A.ADDLCOST) ADDLCOST
                      FROM (
                          SELECT CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT-(SALES.SATAX + SALES.SACESSAMT + SALES.SAADDLCESS + SALES.SAFLOODCESSAMT)) 
                                 ELSE SANETAMOUNT END AMOUNT,
                                 SUM(ABS(SALESDTL.SADTLQTY + SALESDTL.SADTLFREEQTY) * SADTLCOST) COST,
                                 SUM(ABS(SALESDTL.SADTLQTY + SALESDTL.SADTLFREEQTY) * SADTLFTRCOSTAFFECTAMT) ADDLCOST
                          FROM SALES
                          INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID
                          INNER JOIN ACMASTER ON SALES.SACUSTID = ACMASTER.ACID
                          INNER JOIN SALESDTL ON SALES.VID = SALESDTL.VID
                          INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                          LEFT JOIN PRDUNITS ON SALESDTL.SADTLPRDID = PRDUNITS.PUPRDID AND SALESDTL.SADTLUNIT = PRDUNITS.PUUNITID
                          WHERE SALES.SADATE = @FROM AND ISNULL(SALES.SACANCELED, 0) = 0 AND SALES.SACUSTTIN = ''
                          GROUP BY SALES.VID, SALES.SADOCNO, SALES.SADATE, SALES.SACUSTNAME,
                                   SALES.SANETAMOUNT, SADOCNONUM, BHBTYPEID, SALES.SATAX, SALES.SACESSAMT, SALES.SAADDLCESS, SALES.SAFLOODCESSAMT
                      ) A",
                        new { FROM = fromDt, EXCLTAX = excludeTax }) ?? new SalesDailySummary();

                    var objSR = new SalesDailySummary();

                    objSR = await connection.QuerySingleOrDefaultAsync<SalesDailySummary>(
                        @"SELECT SUM(SALESRETURN.SRNETAMOUNT) SRAMT, 
                             SUM(SALESRETDTL.SRDTLCOST * (SALESRETDTL.SRDTLQTY + SALESRETDTL.SRDTLFREEQTY)) SRCOST 
                      FROM SALESRETDTL
                      INNER JOIN SALESRETURN ON SALESRETURN.VID = SALESRETDTL.VID 
                      WHERE SRCANCELED = 0 AND SRDATE = @FROM AND SALESRETURN.SRCUSTTIN = ''",
                        new { FROM = fromDt, EXCLTAX = excludeTax }) ?? new SalesDailySummary();

                    obj.SRAmt = objSR.SRAmt;
                    obj.SRCost = objSR.SRCost;

                    return obj;
                }
                catch (Exception ex)
                {
                    // Log the exception if you have a logging mechanism
                    // e.g., Logger.LogError(ex, "Error in SaleDailyProfitSummary");
                    throw; // Re-throw or handle as needed
                }
            }
        }
    }
}

