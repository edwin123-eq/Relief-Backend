using System;
using System.Data;
using Dapper;
using System.Diagnostics.Metrics;
using ReliefApi.Contracts;
using ReliefApi.Models; // Adjust the namespace as needed
using System.Data.SqlClient;
using ReliefApi;
using System.Data.Common;


public class FAreports : IFAreport  // Assuming this is part of the CompanySettings class
{
    private readonly DapperContext db;
    public FAreports(DapperContext _db) 
    {
        db = _db;
    }

    public async Task< KeyValueSetting> GetKeyValueSettingByKey(string keyName)
    {


        if (!string.IsNullOrWhiteSpace(keyName))
        {
            using (var connection = db.CreateConnection()) // Ensure db.CreateConnection() returns a valid IDbConnection
            {
                string query = "SELECT * FROM SETTINGS WHERE KEYNAME = @KEYNAME";
                return connection.QuerySingleOrDefault<KeyValueSetting>(query, new { KEYNAME = keyName });
            }
        }
     
        return null;
    }



   
    public async Task<SalesDailySummary> SaleDailyProfitSummaryAsync(DateTime fromDt, int excludeTax)
    {
        var query = @"
    SELECT SUM(A.AMOUNT) AS AMOUNT, SUM(A.COST) AS COST, SUM(A.ADDLCOST) AS ADDLCOST
    FROM (
        SELECT 
            CASE 
                WHEN @EXCLTAX = 1 THEN (SALES.SANETAMOUNT - (SALES.SATAX + SALES.SACESSAMT + SALES.SAADDLCESS + SALES.SAFLOODCESSAMT)) 
                ELSE SANETAMOUNT 
            END AS AMOUNT,
            SUM(ABS(SALESDTL.SADTLQTY + SALESDTL.SADTLFREEQTY) * SADTLCOST) AS COST,
            SUM(ABS(SALESDTL.SADTLQTY + SALESDTL.SADTLFREEQTY) * SADTLFTRCOSTAFFECTAMT) AS ADDLCOST
        FROM SALES
        INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID
        INNER JOIN ACMASTER ON SALES.SACUSTID = ACMASTER.ACID
        INNER JOIN SALESDTL ON SALES.VID = SALESDTL.VID
        INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
        LEFT JOIN PRDUNITS ON SALESDTL.SADTLPRDID = PRDUNITS.PUPRDID AND SALESDTL.SADTLUNIT = PRDUNITS.PUUNITID
        WHERE SALES.SADATE = @FROM AND ISNULL(SALES.SACANCELED, 0) = 0
        GROUP BY SALES.VID, SALES.SADOCNO, SALES.SADATE, SALES.SACUSTNAME,
                 SALES.SANETAMOUNT, SADOCNONUM, BHBTYPEID, SALES.SATAX, 
                 SALES.SACESSAMT, SALES.SAADDLCESS, SALES.SAFLOODCESSAMT
    ) A";

        var parameters = new { FROM = fromDt, EXCLTAX = excludeTax };
        using (var connection = db.CreateConnection())
        {
            return await connection.QueryFirstOrDefaultAsync<SalesDailySummary>(query, parameters);
        }
    }


    public void AddBlankObj(ref List<DataMain> lstData, int num = 1)
    {
        for (int i = 0; i < num; i++)
        {
            lstData.Add(new DataMain
            {
                IncName = "",
                IncSubValue = null,
                IncCashValue = null,
                ExpName = "",
                ExpSubValue = null,
                ExpCashValue = null
            });
        }
    }

   

    public async Task<List<CashSummaryDtls>> RptSummaryStatementAsonDate(DateTime ason, long counterId, long cashAccId, string type, long debtorSchId, long credtrSchId)
    {
        var lst = new List<CashSummaryDtls>();
        var lstRcpts = new List<CollectionRpt>();
        decimal intAmt = 0, intTot = 0, intBal = 0, intCash = 0, intCardSales = 0;
        decimal intCashSr = 0, intCreditSr = 0, intCreditSales = 0, intGPaySales = 0, intSwipeCardSales=0;

        if (type.ToUpper() == "IN")
        {
            using (var connection = db.CreateConnection()) // Ensure db.CreateConnection() returns a valid IDbConnection
            {
                intCardSales = await connection.ExecuteScalarAsync<decimal>(
                @"SELECT ISNULL(SUM(BAL),0)
                      FROM (
                          SELECT ISNULL(RCVD.AMT,0) AS BAL
                          FROM SALES
                          LEFT JOIN (
                              SELECT CASHRECEIVED.VID, SUM(CASHRECEIVED.CRAMOUNT) AMT
                              FROM CASHRECEIVED
                              WHERE CASHRECEIVED.CRFLAG='CARD'
                              GROUP BY CASHRECEIVED.VID
                          ) AS RCVD ON SALES.VID=RCVD.VID
                          WHERE SALES.SADATE = @ASON AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                          AND ISNULL(SALES.SACANCELED,0)=0 AND SALES.SACUSTTIN=''
                      ) AS CARDSALES",
                new { ASON = ason, COUNTERID = counterId });

                intGPaySales = await connection.ExecuteScalarAsync<decimal>(
                @"SELECT ISNULL(SUM(BAL),0)
                  FROM (
                      SELECT ISNULL(RCVD.AMT,0) AS BAL
                      FROM SALES
                      LEFT JOIN (
                          SELECT CASHRECEIVED.VID, SUM(CASHRECEIVED.CRAMOUNT) AMT
                          FROM CASHRECEIVED
                          INNER JOIN BANKCARDS ON CASHRECEIVED.CRREFID=BANKCARDS.CDID
                          WHERE CASHRECEIVED.CRFLAG='CARD' AND BANKCARDS.CDNAME LIKE '%GPAY%'
                          GROUP BY CASHRECEIVED.VID
                      ) AS RCVD ON SALES.VID=RCVD.VID
                      WHERE SALES.SADATE = @ASON AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND ISNULL(SALES.SACANCELED,0)=0 AND SALES.SACUSTTIN=''
                  ) AS GPAYSALES",
                new { ASON = ason, COUNTERID = counterId });

                intSwipeCardSales = intCardSales - intGPaySales;

                intCreditSales = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(BAL),0)
                      FROM (
                          SELECT SANETAMOUNT-(ISNULL(RCVD.AMT,0) - ISNULL(EXRCVD.EXCAMT,0)) BAL
                          FROM SALES
                          LEFT JOIN (
                              SELECT SALES.VID, SUM(A.CRAMOUNT - SALES.SANETAMOUNT) EXCAMT
                              FROM SALES
                              INNER JOIN (
                                  SELECT CASHRECEIVED.VID, SUM(CASHRECEIVED.CRAMOUNT) CRAMOUNT
                                  FROM CASHRECEIVED
                                  GROUP BY VID
                              ) A ON SALES.VID = A.VID
                              WHERE (A.CRAMOUNT > SALES.SANETAMOUNT) AND SALES.SACASHCREDIT='CREDIT'
                              GROUP BY SALES.VID
                          ) AS EXRCVD ON SALES.VID=EXRCVD.VID
                          LEFT JOIN (
                              SELECT CASHRECEIVED.VID, SUM(CASHRECEIVED.CRAMOUNT) AMT
                              FROM CASHRECEIVED
                              GROUP BY CASHRECEIVED.VID
                          ) AS RCVD ON SALES.VID=RCVD.VID
                          WHERE SALES.SADATE = @ASON AND ISNULL(SALES.SACANCELED,0)=0 AND SALES.SACASHCREDIT='CREDIT'
                          AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALES.SACUSTTIN=''
                      ) AS CREDITCASHCOLLECTION",
                    new { ASON = ason, COUNTERID = counterId });

                intCashSr = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SRNETAMOUNT),0)
                      FROM SALESRETURN
                      WHERE SALESRETURN.SRDATE = @ASON AND SALESRETURN.SRCASHCREDIT='CASH'
                      AND (SALESRETURN.SRCOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALESRETURN.SRCUSTTIN=''",
                    new { ASON = ason, COUNTERID = counterId });

                intCreditSr = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SRNETAMOUNT),0)
                      FROM SALESRETURN
                      WHERE SALESRETURN.SRDATE = @ASON AND SALESRETURN.SRCASHCREDIT='CREDIT'
                      AND (SALESRETURN.SRCOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALESRETURN.SRCUSTTIN=''",
                    new { ASON = ason, COUNTERID = counterId });

                var obj = new CashSummaryDtls { Type = "Cash Sales" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SANETAMOUNT),0)
                      FROM SALES
                      WHERE SALES.SADATE = @ASON AND ISNULL(SALES.SACANCELED,0)=0
                      AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALES.SACASHCREDIT='CASH'
                      AND SALES.SASALETYPE=1 AND SALES.SACUSTTIN=''",
                    new { ASON = ason, COUNTERID = counterId });

                intCash = obj.Bal - intCardSales;
                obj.Bal = intCash;
                intAmt = obj.Bal;
                intTot += obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "+";
                obj.TranFlag = "SA";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Net Cash Sales (After Return)" };
                obj.Bal = intCash - intCashSr;
                intAmt = obj.Bal;
                intTot += obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "+";
                obj.TranFlag = "SA";
                lst.Add(obj);

                if (intCardSales != 0)
                {
                    var lstBankCard = await connection.QueryAsync<BankCardAmtDtl>(
                        @"SELECT ISNULL(SUM(BAL),0) CARDAMOUNT, ISNULL(CDNAME,'') AS CARDNAME
                          FROM (
                              SELECT ISNULL(RCVD.AMT,0) AS BAL, ISNULL(RCVD.CNAME,'') AS CDNAME
                              FROM Sales
                              LEFT JOIN (
                                  SELECT CASHRECEIVED.VID, BANKCARDS.CDNAME CNAME, SUM(CASHRECEIVED.CRAMOUNT) AMT, CASHRECEIVED.CRREFID
                                  FROM CashReceived
                                  INNER JOIN BANKCARDS ON CASHRECEIVED.CRREFID=BANKCARDS.CDID
                                  WHERE CASHRECEIVED.CRFLAG='CARD' AND CASHRECEIVED.CRACID=BANKCARDS.CDLINKEDACC
                                  GROUP BY CASHRECEIVED.VID, CASHRECEIVED.CRREFID, BANKCARDS.CDNAME
                              ) AS RCVD ON SALES.VID=RCVD.VID
                              WHERE SALES.SADATE = @ASON AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                              AND ISNULL(SALES.SACANCELED,0)=0 AND SALES.SACASHCREDIT='CASH' AND SALES.SACUSTTIN=''
                          ) AS CARDSALES
                          GROUP BY CDNAME",
                        new { ASON = ason, COUNTERID = counterId });

                    if (lstBankCard?.Any() == true)
                    {
                        foreach (var itm in lstBankCard)
                        {
                            if (itm.CardAmt > 0)
                            {
                                obj = new CashSummaryDtls
                                {
                                    Type = itm.CardName,
                                    Bal = itm.CardAmt,
                                    Seq = 4,
                                    CalcFlag = "",
                                    TranFlag = "SA"
                                };
                                lst.Add(obj);
                            }
                        }
                    }
                }

                if (intGPaySales > 0)
                {
                    obj = new CashSummaryDtls
                    {
                        Type = "GPAY ACCOUNT",
                        Bal = intGPaySales,
                        Seq = 4,
                        CalcFlag = "",
                        TranFlag = "SA"
                    };
                    lst.Add(obj);
                }

                // Add Swipping Card explicitly
                if (intSwipeCardSales > 0)
                {
                    obj = new CashSummaryDtls
                    {
                        Type = "SWIPPING CARD",
                        Bal = intSwipeCardSales,
                        Seq = 4,
                        CalcFlag = "",
                        TranFlag = "SA"
                    };
                    lst.Add(obj);
                }

                obj = new CashSummaryDtls { Type = "Credit Sales" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(BAL),0)
                      FROM (
                          SELECT SANETAMOUNT-(ISNULL(RCVD.AMT,0) - ISNULL(EXRCVD.EXCAMT,0)) BAL
                          FROM SALES
                          LEFT JOIN (
                              SELECT SALES.VID, SUM(A.CRAMOUNT - SALES.SANETAMOUNT) EXCAMT
                              FROM SALES
                              INNER JOIN (
                                  SELECT CASHRECEIVED.VID, SUM(CASHRECEIVED.CRAMOUNT) CRAMOUNT
                                  FROM CASHRECEIVED
                                  GROUP BY VID
                              ) A ON SALES.VID = A.VID
                              WHERE (A.CRAMOUNT > SALES.SANETAMOUNT) AND SALES.SACASHCREDIT='CREDIT'
                              GROUP BY SALES.VID
                          ) AS EXRCVD ON SALES.VID=EXRCVD.VID
                          LEFT JOIN (
                              SELECT CASHRECEIVED.VID, SUM(CASHRECEIVED.CRAMOUNT) AMT
                              FROM CASHRECEIVED
                              GROUP BY CASHRECEIVED.VID
                          ) AS RCVD ON SALES.VID=RCVD.VID
                          WHERE SALES.SADATE = @ASON AND ISNULL(SALES.SACANCELED,0)=0 AND SALES.SACASHCREDIT='CREDIT'
                          AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALES.SACUSTTIN=''
                      ) AS CREDITCASHCOLLECTION",
                    new { ASON = ason, COUNTERID = counterId });
                intAmt -= obj.Bal;
                intTot += obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "";
                obj.TranFlag = "SA";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Total Sales (Cash Sales + Card Sales + GPay + Credit - Sales Return)" };
                obj.Bal = intCash + intCardSales + intCreditSales - (intCreditSr + intCashSr);
                intAmt = obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "";
                obj.TranFlag = "SA";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "HD BillAmount" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SANETAMOUNT),0)
                      FROM SALES
                      WHERE SALES.SADATE = @ASON AND ISNULL(SALES.SACANCELED,0)=0
                      AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALES.SAISHOMEDELIVERY=1",
                    new { ASON = ason, COUNTERID = counterId });
                intAmt = obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "";
                obj.TranFlag = "SA";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "HD BillCount" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(COUNT(SACUSTID),0)
                      FROM SALES
                      WHERE SALES.SADATE = @ASON AND ISNULL(SALES.SACANCELED,0)=0
                      AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND SALES.SAISHOMEDELIVERY=1",
                    new { ASON = ason, COUNTERID = counterId });
                intAmt = obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "";
                obj.TranFlag = "SA";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Purchase Return" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(PRNETAMOUNT),0)
                      FROM PURCHASERET
                      WHERE PURCHASERET.PRCORR='CASH' AND PURCHASERET.PRDOCDATE = @ASON
                      AND (PURCHASERET.PRCOUNTERID = @COUNTERID OR @COUNTERID <= 0)",
                    new { ASON = ason, COUNTERID = counterId });
                obj.Seq = 2;
                obj.CalcFlag = "+";
                obj.TranFlag = "PR";
                if (obj.Bal != 0) lst.Add(obj);

                obj = new CashSummaryDtls { Type = "No Of Bills" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(COUNT(VID),0)
                      FROM SALES
                      WHERE SALES.SADATE = @ASON AND ISNULL(SALES.SACANCELED,0)=0
                      AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0)",
                    new { ASON = ason, COUNTERID = counterId });
                intAmt = obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "";
                obj.TranFlag = "SA";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = null, Bal = 0 };
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Bank Withdrawal" };
                obj.Bal = await connection.QuerySingleOrDefaultAsync<decimal>(
                    @"SELECT ISNULL(SUM(AMT),0)
                      FROM (
                          SELECT ISNULL(SUM(DAYBOOK.DBCREDIT),0) AMT
                          FROM VOUCHERS
                          INNER JOIN DAYBOOK ON VOUCHERS.VID=DAYBOOK.VOUCHERID
                          INNER JOIN ACMASTER ON DAYBOOK.DBACID=ACMASTER.ACID
                          INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE = SCHEDULES.SHID
                          WHERE VOUCHERS.VCHTYPE='RC' AND VOUCHERS.VCHACID = @CASHACC
                          AND DBDATE = @ASON AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                          AND SHTYPE<>'E' AND SHNATURE='BANK'
                          UNION
                          SELECT SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT)
                          FROM DAYBOOK
                          WHERE DAYBOOK.DBACID = @CASHACC AND DBDEBIT > 0 AND DBDATE = @ASON
                          AND DBVCHTYPE IN ('PY','CR','JL')
                          AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      ) A",
                    new { ASON = ason, COUNTERID = counterId, CASHACC = cashAccId });
                obj.Seq = 2;
                obj.CalcFlag = "+";
                obj.TranFlag = "BWR";
                lst.Add(obj);

                lstRcpts = (await connection.QueryAsync<CollectionRpt>(
                    @"SELECT ACMASTER.ACACNAME Customer, ISNULL(DAYBOOK.DBCREDIT,0) AMOUNT
                      FROM VOUCHERS
                      INNER JOIN DAYBOOK ON VOUCHERS.VID=DAYBOOK.VOUCHERID
                      INNER JOIN ACMASTER ON DAYBOOK.DBACID=ACMASTER.ACID
                      INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE = SCHEDULES.SHID
                      WHERE VOUCHERS.VCHTYPE='RC' AND VOUCHERS.VCHACID = @CASHACC
                      AND DBDATE = @ASON AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND SHTYPE<>'E' AND SHNATURE='BANK'
                      UNION
                      SELECT DAYBOOK.DBLEDGERREMARKS, ABS(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT)
                      FROM DAYBOOK
                      WHERE DAYBOOK.DBACID = @CASHACC AND DBDEBIT > 0 AND DBDATE = @ASON AND DBVCHTYPE='PY'
                      AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      UNION
                      SELECT ACMASTER.ACACNAME, ABS(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT)
                      FROM DAYBOOK
                      INNER JOIN ACMASTER ON ACMASTER.ACID=DAYBOOK.DBACID
                      WHERE DBACID <> @CASHACC AND DAYBOOK.VOUCHERID IN (
                          SELECT VOUCHERID FROM DAYBOOK
                          WHERE DBDATE = @ASON AND DBDEBIT > 0 AND DAYBOOK.DBACID = @CASHACC
                          AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND DBVCHTYPE IN ('CR','JL')
                      )",
                    new { ASON = ason, COUNTERID = counterId, CASHACC = cashAccId })).ToList();

                if (lstRcpts?.Any() == true)
                {
                    foreach (var objItm in lstRcpts)
                    {
                        obj = new CashSummaryDtls
                        {
                            Type = objItm.Customer,
                            Bal = objItm.BillReceipt,
                            Seq = 2,
                            CalcFlag = "",
                            TranFlag = "BWR"
                        };
                        if (obj.Bal != 0) lst.Add(obj);
                    }
                }

                decimal b2bSales = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SANETAMOUNT),0)
                      FROM SALES
                      WHERE SALES.SADATE = @ASON AND ISNULL(SALES.SACANCELED,0)=0
                      AND (SALES.SACOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND SALES.SASALETYPE=0 AND SALES.SACUSTTIN<>''",
                    new { ASON = ason, COUNTERID = counterId });

                obj = new CashSummaryDtls { Type = "Other Collection(B2B Sales)" };
                obj.Bal = b2bSales;
                obj.Seq = 2;
                obj.CalcFlag = "+";
                obj.TranFlag = "SA";
                lst.Add(obj);

                decimal b2bSR = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SRNETAMOUNT),0)
                      FROM SALESRETURN
                      WHERE SALESRETURN.SRDATE = @ASON
                      AND (SALESRETURN.SRCOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALESRETURN.SRCUSTTIN<>''",
                    new { ASON = ason, COUNTERID = counterId });

                obj = new CashSummaryDtls { Type = "B2B Sales Return" };
                obj.Bal = b2bSR;
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "SR";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Net B2B Sale" };
                obj.Bal = b2bSales - b2bSR;
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "SR";
                lst.Add(obj);
            }
        }
        else
        {
            using (var connection = db.CreateConnection()) // Ensure db.CreateConnection() returns a valid IDbConnection
            {
                var obj = new CashSummaryDtls { Type = "Cash Sales Return" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SRNETAMOUNT),0)
                      FROM SALESRETURN
                      WHERE SALESRETURN.SRDATE = @ASON AND SALESRETURN.SRCASHCREDIT='CASH'
                      AND (SALESRETURN.SRCOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALESRETURN.SRCUSTTIN=''",
                    new { ASON = ason, COUNTERID = counterId });
                intCashSr = obj.Bal;
                intTot -= obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "SR";
                if (obj.Bal != 0) lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Credit Sales Return" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(SRNETAMOUNT),0)
                      FROM SALESRETURN
                      WHERE SALESRETURN.SRDATE = @ASON AND SALESRETURN.SRCASHCREDIT='CREDIT'
                      AND (SALESRETURN.SRCOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND SALESRETURN.SRCUSTTIN=''",
                    new { ASON = ason, COUNTERID = counterId });
                intCreditSr = obj.Bal;
                intTot -= obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "SR";
                if (obj.Bal != 0) lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Total Sales Return" };
                obj.Bal = intCashSr + intCreditSr;
                intTot -= obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "SR";
                if (obj.Bal != 0) lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Total Purchase" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(PUNETAMOUNT),0)
                      FROM PURCHASE
                      WHERE PUDOCDATE = @ASON AND PURCHASE.PUISAPPROVED = 1
                      AND (PURCHASE.PUCOUNTERID = @COUNTERID OR @COUNTERID <= 0)",
                    new { ASON = ason, COUNTERID = counterId });
                intAmt = obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "";
                obj.TranFlag = "PU";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Credit Purchase" };
                obj.Bal = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(PUNETAMOUNT),0)
                      FROM PURCHASE
                      WHERE PUDOCDATE = @ASON AND PURCHASE.PUISAPPROVED = 1
                      AND (PURCHASE.PUCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND PUCORR='CREDIT'",
                    new { ASON = ason, COUNTERID = counterId });
                intAmt -= obj.Bal;
                obj.Seq = 2;
                obj.CalcFlag = "";
                obj.TranFlag = "PU";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Net Cash Purchase" };
                obj.Bal = intAmt;
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "PU";
                lst.Add(obj);

                obj = new CashSummaryDtls { Type = "Payments" };
                obj.Bal = await connection.QuerySingleOrDefaultAsync<decimal>(
                    @"SELECT ISNULL(SUM(ABS(DAYBOOK.DBDEBIT-DAYBOOK.DBCREDIT)),0)
                      FROM VOUCHERS
                      INNER JOIN DAYBOOK ON VOUCHERS.VID=DAYBOOK.VOUCHERID
                      INNER JOIN ACMASTER ON DAYBOOK.DBACID=ACMASTER.ACID
                      INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE = SCHEDULES.SHID
                      WHERE VCHTYPE='PY' AND VOUCHERS.VCHACID = @CASHID AND VCHDATE = @ASON
                      AND (VOUCHERS.VCHCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND ACMASTER.ACSCHEDULE IN (@CRDTSCH, @DBTRSCH)
                      AND DAYBOOK.DBACID <> @CASHID",
                    new { CASHID = cashAccId, ASON = ason, COUNTERID = counterId, CRDTSCH = credtrSchId, DBTRSCH = debtorSchId });
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "PY";
                lst.Add(obj);

                lstRcpts = (await connection.QueryAsync<CollectionRpt>(
                    @"SELECT ACMASTER.ACACNAME Customer, ISNULL(ABS(DAYBOOK.DBDEBIT-DAYBOOK.DBCREDIT),0) AMOUNT
                      FROM VOUCHERS
                      INNER JOIN DAYBOOK ON VOUCHERS.VID=DAYBOOK.VOUCHERID
                      INNER JOIN ACMASTER ON DAYBOOK.DBACID=ACMASTER.ACID
                      INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE = SCHEDULES.SHID
                      WHERE VCHTYPE='PY' AND VOUCHERS.VCHACID = @CASHID AND VCHDATE = @ASON
                      AND (VOUCHERS.VCHCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND ACMASTER.ACSCHEDULE IN (@CRDTSCH, @DBTRSCH)
                      AND DAYBOOK.DBACID <> @CASHID
                      ORDER BY ACACNAME",
                    new { CASHID = cashAccId, ASON = ason, COUNTERID = counterId, CRDTSCH = credtrSchId, DBTRSCH = debtorSchId })).ToList();

                if (lstRcpts?.Any() == true)
                {
                    foreach (var objItm in lstRcpts)
                    {
                        obj = new CashSummaryDtls
                        {
                            Type = objItm.Customer,
                            Bal = objItm.BillReceipt,
                            Seq = 0,
                            CalcFlag = "",
                            TranFlag = "PY"
                        };
                        if (obj.Bal != 0) lst.Add(obj);
                    }
                }

                obj = new CashSummaryDtls { Type = "Bank Deposit" };
                obj.Bal = await connection.QuerySingleOrDefaultAsync<decimal>(
                    @"SELECT ISNULL(SUM(AMT),0)
                      FROM (
                          SELECT ISNULL(SUM(DAYBOOK.DBDEBIT),0) AMT
                          FROM VOUCHERS
                          INNER JOIN DAYBOOK ON VOUCHERS.VID=DAYBOOK.VOUCHERID
                          INNER JOIN ACMASTER ON DAYBOOK.DBACID=ACMASTER.ACID
                          INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE = SCHEDULES.SHID
                          WHERE VOUCHERS.VCHTYPE='PY' AND VOUCHERS.VCHACID = @CASHACC
                          AND DBDATE = @ASON AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                          AND SHTYPE<>'E' AND SHNATURE='BANK'
                          UNION
                          SELECT ABS(SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT))
                          FROM DAYBOOK
                          WHERE DAYBOOK.DBACID = @CASHACC AND DBCREDIT > 0 AND DBDATE = @ASON
                          AND DBVCHTYPE IN ('RC','CR','JL')
                          AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      ) A",
                    new { ASON = ason, COUNTERID = counterId, CASHACC = cashAccId });
                obj.Seq = 2;
                obj.CalcFlag = "-";
                obj.TranFlag = "BWP";
                lst.Add(obj);

                lstRcpts = (await connection.QueryAsync<CollectionRpt>(
                    @"SELECT ACMASTER.ACACNAME Customer, ISNULL(DAYBOOK.DBDEBIT,0) AMOUNT
                      FROM VOUCHERS
                      INNER JOIN DAYBOOK ON VOUCHERS.VID=DAYBOOK.VOUCHERID
                      INNER JOIN ACMASTER ON DAYBOOK.DBACID=ACMASTER.ACID
                      INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE = SCHEDULES.SHID
                      WHERE VOUCHERS.VCHTYPE='PY' AND VOUCHERS.VCHACID = @CASHACC
                      AND DBDATE = @ASON AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      AND SHTYPE<>'E' AND SHNATURE='BANK'
                      UNION
                      SELECT DAYBOOK.DBLEDGERREMARKS, ABS(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT)
                      FROM DAYBOOK
                      WHERE DAYBOOK.DBACID = @CASHACC AND DBCREDIT > 0 AND DBDATE = @ASON AND DBVCHTYPE='RC'
                      AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0)
                      UNION
                      SELECT ACMASTER.ACACNAME, ABS(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT)
                      FROM DAYBOOK
                      INNER JOIN ACMASTER ON ACMASTER.ACID=DAYBOOK.DBACID
                      WHERE DBACID <> @CASHACC AND DAYBOOK.VOUCHERID IN (
                          SELECT VOUCHERID FROM DAYBOOK
                          WHERE DBDATE = @ASON AND DBCREDIT > 0 AND DAYBOOK.DBACID = @CASHACC
                          AND (DAYBOOK.DBCOUNTERID = @COUNTERID OR @COUNTERID <= 0) AND DBVCHTYPE IN ('CR','JL')
                      )",
                    new { ASON = ason, COUNTERID = counterId, CASHACC = cashAccId })).ToList();

                if (lstRcpts?.Any() == true)
                {
                    foreach (var objItm in lstRcpts)
                    {
                        obj = new CashSummaryDtls
                        {
                            Type = objItm.Customer,
                            Bal = objItm.BillReceipt,
                            Seq = 0,
                            CalcFlag = "",
                            TranFlag = "BWP"
                        };
                        if (obj.Bal != 0) lst.Add(obj);
                    }
                }
            }
        }

        return lst;
    }


        public async Task<List<Ledger>> Rpt_LedgerForIncomeAndExpenseHeads(
            DateTime fromDt,
            DateTime toDt,
            long custId,
            long cntrId,
            bool includeAll,
            DateTime finYearStartDt)
        {
            var ldgLst = new List<Ledger>();
            var intBal = 0m; // Decimal literal
            var ldg = new Ledger();

            using (var connection = db.CreateConnection()) // Assumes CreateConnection() returns IDbConnection
            {
                if (includeAll)
                {
                    intBal += await connection.ExecuteScalarAsync<decimal>(
                        @"SELECT ISNULL(SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT), 0) AS BAL
                          FROM DAYBOOK
                          INNER JOIN ACMASTER ON ACMASTER.ACID = DAYBOOK.DBACID
                          WHERE DAYBOOK.DBDATE BETWEEN @FROM AND @TO
                          AND (DAYBOOK.DBCOUNTERID = @CNTRID OR @CNTRID <= 0)
                          AND (DAYBOOK.DBACID = @ID OR ACMASTER.ACPARENTACCID = @ID)",
                        new { CNTRID = cntrId, FROM = finYearStartDt, TO = fromDt.AddDays(-1), ID = custId });

                    ldg.Vchtype = "OP";
                    ldg.Opbal = intBal;

                    ldgLst = (await connection.QueryAsync<Ledger>(
                        @"SELECT DAYBOOK.VOUCHERID, DAYBOOK.DBVCHNO, DAYBOOK.DBDATE,
                                 ISNULL(TRANTYPE.TRNAME, DAYBOOK.DBVCHTYPE) AS DBVCHTYPE, DAYBOOK.DBREMARKS,
                                 DAYBOOK.DBDEBIT, DAYBOOK.DBCREDIT, DAYBOOK.DBSEQ, ACMASTER.ACACNAME, DAYBOOK.DBACID,
                                 CASE ACMASTER.ACPARENTACCID WHEN 0 THEN 1 ELSE 2 END SEQ,
                                 DBLEDGERREMARKS
                          FROM DAYBOOK
                          LEFT JOIN TRANTYPE ON DAYBOOK.DBVCHTYPE = TRANTYPE.TRFLAG
                          INNER JOIN ACMASTER ON ACMASTER.ACID = DAYBOOK.DBACID
                          WHERE (DAYBOOK.DBDATE BETWEEN @FROM AND @TO)
                          AND (DAYBOOK.DBACID = @ID OR ACMASTER.ACPARENTACCID = @ID)
                          AND DBVCHTYPE <> 'OP'
                          AND (DAYBOOK.DBCOUNTERID = @CID OR @CID <= 0)
                          ORDER BY SEQ, DAYBOOK.DBACID, DAYBOOK.DBDATE, DAYBOOK.VOUCHERID, DBSEQ",
                        new { FROM = fromDt, TO = toDt, ID = custId, CID = cntrId })).ToList();
                }
                else
                {
                    intBal += await connection.ExecuteScalarAsync<decimal>(
                        @"SELECT ISNULL(SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT), 0) AS BAL
                          FROM DAYBOOK
                          WHERE DAYBOOK.DBDATE BETWEEN @FROM AND @TO
                          AND (DAYBOOK.DBCOUNTERID = @CNTRID OR @CNTRID <= 0)
                          AND (DAYBOOK.DBACID = @ID)",
                        new { CNTRID = cntrId, FROM = finYearStartDt, TO = fromDt.AddDays(-1), ID = custId });

                    ldg.Vchtype = "OP";
                    ldg.Opbal = intBal;

                    ldgLst = (await connection.QueryAsync<Ledger>(
                        @"SELECT DAYBOOK.VOUCHERID, DAYBOOK.DBVCHNO, DAYBOOK.DBDATE,
                                 ISNULL(TRANTYPE.TRNAME, DAYBOOK.DBVCHTYPE) AS DBVCHTYPE, DAYBOOK.DBREMARKS,
                                 DAYBOOK.DBDEBIT, DAYBOOK.DBCREDIT, DAYBOOK.DBSEQ, ACMASTER.ACACNAME, DAYBOOK.DBACID,
                                 CASE ACMASTER.ACPARENTACCID WHEN 0 THEN 1 ELSE 2 END SEQ,
                                 DBLEDGERREMARKS
                          FROM DAYBOOK
                          LEFT JOIN TRANTYPE ON DAYBOOK.DBVCHTYPE = TRANTYPE.TRFLAG
                          INNER JOIN ACMASTER ON ACMASTER.ACID = DAYBOOK.DBACID
                          WHERE (DAYBOOK.DBDATE BETWEEN @FROM AND @TO)
                          AND DAYBOOK.DBACID = @ID
                          AND (DAYBOOK.DBCOUNTERID = @CID OR @CID <= 0)
                          AND DBVCHTYPE <> 'OP'
                          ORDER BY SEQ, DAYBOOK.DBACID, DAYBOOK.DBDATE, DAYBOOK.VOUCHERID, DBSEQ",
                        new { FROM = fromDt, TO = toDt, ID = custId, CID = cntrId })).ToList();
                }

                ldgLst.Insert(0, ldg);
            }

            return ldgLst;
        }
}


