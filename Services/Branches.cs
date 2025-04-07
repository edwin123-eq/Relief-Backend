
using Contracts;
using Dapper;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Options;
using Models;
using ReliefApi;
using ReliefApi.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;
using static ReliefApi.Controllers.HomeController;
using static Services.Branches;

namespace Services
{
    public class Branches : IBranches
    {
        private readonly DapperContext dapperContext;
        private object totalSummaryList;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WhatsAppApiSettings _whatsAppApiSettings;


        public Branches(DapperContext dapperContext, IHttpClientFactory httpClientFactory, IOptions<WhatsAppApiSettings> options)
        {
            this.dapperContext = dapperContext;
            _httpClientFactory = httpClientFactory;
            _whatsAppApiSettings = options.Value;
        }
        public async Task AddRefreshToken(ConsoleRefreshTokenModel model)
        {
            var insertQuery = @"INSERT INTO dbo.REFRESHTOKENS( UserId , Token , Expires , Created , Revoked ) VALUES( @userid , @token , @expires , @created , @revoked )";

            var parameters = new
            {
                userid = model.UserId,
                model.Token,
                model.Expires,
                model.Created,
                model.Revoked
            };

            try
            {
                using (var connection = this.dapperContext.CreateConnection())
                {
                    await connection.ExecuteAsync(insertQuery, parameters);
                }
            }
            catch (Exception ex)
            {

                // Handle the exception, log it, or throw a custom exception
                // Example: throw new Exception("Failed to insert refresh token", ex);
            }


        }
        public async Task<Employee> GetByUserName(string UserName)
        {
            var q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<Employee>("SELECT EMPUSRNAME,EMPUSERPASSWORD,EMPID,EMPNAME,EMPUROLEID,EMPBLOCKED FROM EMPLOYEE WHERE EMPUSRNAME=@NAME ", new { NAME = UserName });
            return q;
        }
        public async Task<List<Branch>> List(bool IsAll, long StateId)
        {
            var strTopFilter = IsAll == true ? "" : "AND CNTBLOCKED=0";
            //var q = await this.dapperContext.CreateConnection().QueryAsync<Branch>("SELECT CNTID as Id, CNTNAME as Name, CNTBLOCKED as blocked, CNTADDR1 as Address1, CNTADDR2 as Address2, CNTADDR3 as Address3, CNTPHONE as PhoneNo, CNTMOBILE as MobileNo, CNTEMAIL as Email, CNTSEQ Seq, CNTGSTIN GSTIN, CNTWEB Web, CNTLIC1 Lic1, CNTLIC2 Lic2, CNTLIC3 Lic3, CNTACCID AccID, CNTBANKACCOUNTNAME BanckName, CNTBANKACCOUNTNO BankAccNo, CNTBANKNAME BanckName, CNTBRANCHNAME BranchName, CNTIFSCCODE IFSC, CNTSTOCKSALESVALIDATION StockSaleValidation, CNTSTOCKOTHERVALIDATION StockOtherValidation, CNTPINCODE Pincode, CNTSTATE State, CNTSTATEID StateId, CNTDISTANCE Distance, CNTPACCID PACCID FROM dbo.COUNTER  WHERE (COUNTER.CNTSTATEID=@STATEID OR @STATEID<=0) AND CNTBLOCKED=0" + strTopFilter + " ORDER BY CNTNAME", new { STATEID = StateId });

            var q = await this.dapperContext.CreateConnection().QueryAsync<Branch>("SELECT CNTID as Id, CNTNAME as Name FROM dbo.COUNTER  WHERE (COUNTER.CNTSTATEID=@STATEID OR @STATEID<=0) AND CNTBLOCKED=0" + strTopFilter + " ORDER BY CNTNAME", new { STATEID = StateId });
            return q.ToList();
        }

        public async Task<List<Category>> CategoryList()
        {
            var q = await this.dapperContext.CreateConnection().QueryAsync<Category>("SELECT CATID as Id, CATNAME as CategoryName FROM dbo.CATEGORY ORDER BY CATNAME");
            return q.ToList();
        }

        public async Task<List<Finyear>> FinYearList()
        {
            var q = await this.dapperContext.CreateConnection().QueryAsync<Finyear>("SELECT * FROM FINYEAR");
            return q.ToList();
        }

        public async Task<Finyear> GetFinYear()
        {
            Finyear fin = new();
            fin = (Finyear)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<Finyear>("SELECT * FROM FINYEAR WHERE FINISACTIVE=1");

            return fin;
        }

        public async Task<Finyear> FinYearCalculate(DateTimeOffset Date)
        {
            Date = Date.Date;
            Finyear fin = new();
            fin = (Finyear)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<Finyear>("SELECT * FROM FINYEAR WHERE @D BETWEEN FINYEAR.FINSTART AND  FINYEAR.FINEND", new { D = Date.Date });

            return fin;
        }

        #region CustomerLocation
        public async Task<CustomerLocation> CustomerLocation(long AccountID)
        {
            var q = await this.dapperContext.CreateConnection().QueryAsync<CustomerLocation>(
                "SELECT " +
                "ACMASTER.ACID AS AccountID, " +
                "ACMASTER.ACACNAME AS AccountName, " +  // Add Account Name
                "ACMASTER.ACLONGITUDE AS Longitude, " +
                "ACMASTER.ACLATITUDE AS Latitude " +
                "FROM ACMASTER " +
                "WHERE ACMASTER.ACID = @AccountID ",  // Filter by Account ID
                new { AccountID = AccountID }
            );

            return q.FirstOrDefault();
        }

        public async Task<CustomerLocation> UpdateCustomerLocation(long AccountID, string Longitude, string Latitude)
        {
            using var connection = this.dapperContext.CreateConnection();

            // Update the record in ACMASTER
            var rowsAffected = await connection.ExecuteAsync(
                "UPDATE ACMASTER " +
                "SET ACLONGITUDE = @Longitude, ACLATITUDE = @Latitude " +
                "WHERE ACID = @AccountID",
                new { AccountID, Longitude, Latitude });

            // If rows were updated, fetch the updated details
            if (rowsAffected > 0)
            {
                var updatedRecord = await connection.QueryFirstOrDefaultAsync<CustomerLocation>(
                    "SELECT " +
                    "ACID AS AccountID, " +
                    "ACACNAME AS AccountName, " +
                    "ACLATITUDE AS Latitude, " +
                    "ACLONGITUDE AS Longitude " +
                    "FROM ACMASTER " +
                    "WHERE ACID = @AccountID",
                    new { AccountID });

                return updatedRecord;
            }

            // Return null if no record was updated
            return null;
        }



        #endregion CustomerLocation

        #region Delivery

        public async Task<List<Delivery>> GetDeliveries(long employeeID, int? status, DateTimeOffset date)
        {
            // Strip the time part of the date to calculate the end of the day
            var endOfDay = date.Date.AddDays(1).AddMilliseconds(-1);

            // Query with JOIN and calculated field
            var query = @"
                SELECT 
                    D.DLID,
                    D.DLSALEID,
                    D.DLASSIGNEDBY,
                    D.DLASSIGNEDAT,
                    D.DLEMPID,
                    D.DLSTATUS,
                    D.DLON,
                    D.DLCASHRCVD,
                    D.DLBANKRCVD,
                    D.DLREMARKS,
                    CAST(E.EmpID AS VARCHAR) AS EmployeeIDString, 
                    E.EmpName AS EmployeeName,
                    S.SACUSTID AS CustomerID,
                    S.SACUSTPHONE AS PhoneNumber,
                    S.SACUSTNAME AS CUSTOMERNAME,
                    S.SACUSTADDR1 AS ADDRESS,
                    S.SAQTY AS QUANTITY,
                    (D.DLCASHRCVD + D.DLBANKRCVD) AS AMOUNTRECEIVED,
                    S.SANETAMOUNT AS NetSaleAmount

                FROM 
                    DELIVERY D
                INNER JOIN 
                    SALES S ON D.DLSALEID = S.VID
                INNER JOIN
                    EMPLOYEE E ON D.DLEMPID = E.EmpID -- Join with Employee table
                WHERE 
                    D.DLEMPID = @EmployeeID 
                    AND D.DLASSIGNEDAT <= @EndOfDay";

            // Add condition for status if provided
            if (status.HasValue)
            {
                query += " AND D.DLSTATUS = @Status";
            }

            // Execute the query using Dapper
            var deliveries = await this.dapperContext.CreateConnection().QueryAsync<Delivery>(
                query,
                new { EmployeeID = employeeID, Status = status, EndOfDay = endOfDay } // Parameter binding
            );

            return deliveries.ToList();
        }





        public async Task<List<Delivery>> GetDeliveryReport(long employeeID, int? status, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            // Strip the time part of the start and end dates
            var startOfDay = startDate.Date;
            var endOfDay = endDate.Date.AddDays(1).AddMilliseconds(-1);

            // Query with JOIN and date range filter
            var query = @"
    SELECT 
        D.DLID,
        D.DLSALEID,
        D.DLASSIGNEDBY,
        D.DLASSIGNEDAT,
        D.DLEMPID,
        D.DLSTATUS,
        D.DLON,
        D.DLCASHRCVD,
        D.DLBANKRCVD,
        D.DLREMARKS,
        CAST(E.EmpID AS VARCHAR) AS EmployeeIDString, 
        E.EmpName AS EmployeeName,
        S.SACUSTID AS CustomerID,
        S.SACUSTPHONE AS PhoneNumber,
        S.SACUSTNAME AS CUSTOMERNAME,
        S.SACUSTADDR1 AS ADDRESS,
        S.SADOCNO AS DLSALEDOCNO,
        S.SAQTY AS QUANTITY,
        (D.DLCASHRCVD + D.DLBANKRCVD) AS AMOUNTRECEIVED,
        S.SANETAMOUNT AS NetSaleAmount,
        CASE 
            WHEN D.DLSTATUS = 0 THEN 'Pending'
            WHEN D.DLSTATUS = 1 THEN 'Delivered'
            WHEN D.DLSTATUS = 2 THEN 'Returned'
            WHEN D.DLSTATUS = 3 THEN 'Partially Delivered'
            ELSE 'Unknown'
        END AS DeliveryStatusString
    FROM 
        DELIVERY D
    INNER JOIN 
        SALES S ON D.DLSALEID = S.VID
    INNER JOIN
        EMPLOYEE E ON D.DLEMPID = E.EmpID
    WHERE 
        (@EmployeeID <= 0 OR D.DLEMPID = @EmployeeID)
        AND D.DLASSIGNEDAT >= @StartOfDay 
        AND D.DLASSIGNEDAT <= @EndOfDay";

            // Add condition for status if provided
            if (status.HasValue)
            {
                query += " AND D.DLSTATUS = @Status";
            }

            // Execute the query using Dapper
            var deliveries = await this.dapperContext.CreateConnection().QueryAsync<Delivery>(
                query,
                new { EmployeeID = employeeID, Status = status, StartOfDay = startOfDay, EndOfDay = endOfDay } // Parameter binding
            );

            return deliveries.ToList();
        }


        public async Task<Delivery> UpdateDelivery(Delivery delivery)
        {
            // Step 1: Validate Input
            if (delivery.DLSALEID == 0)
            {
                throw new Exception("DLSALEID cannot be 0. It is an invalid state.");
            }

            // Step 2: Verify if the record exists
            var existingRecord = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<Delivery>(
                "SELECT * FROM DELIVERY WHERE DLSALEID = @DLSALEID AND DLEMPID = @DLEMPID",
                new { delivery.DLSALEID, delivery.DLEMPID });

            if (existingRecord == null)
            {
                throw new Exception("No matching delivery record found with the given Sale ID and Employee ID.");
            }

            // Step 3: Define the Update Query
            var updateQuery = @"
        UPDATE DELIVERY
        SET DLSALEID = @DLSALEID,
            DLASSIGNEDBY = @DLASSIGNEDBY,
            DLASSIGNEDAT = @DLASSIGNEDAT,
            DLEMPID = @DLEMPID,
            DLSTATUS = @DLSTATUS,
            DLON = @DLON,
            DLCASHRCVD = @DLCASHRCVD,
            DLBANKRCVD = @DLBANKRCVD,
            DLREMARKS = @DLREMARKS
        WHERE DLID = @DLID";

            var selectQuery = "SELECT * FROM DELIVERY WHERE DLID = @DLID";

            // Step 4: Execute the Update Query
            using var connection = this.dapperContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(updateQuery, delivery);

            if (rowsAffected == 0)
            {
                throw new Exception("No rows were updated. Verify the DLID and input data.");
            }

            // Step 5: Fetch and Return the Updated Record
            var updatedDelivery = await connection.QueryFirstOrDefaultAsync<Delivery>(selectQuery, new { delivery.DLID });

            return updatedDelivery;
        }

        public async Task<SalesDetails> GetSalesDetails(long DLSALEID, long DLEMPID)
        {
            var query = @"
        SELECT 
            D.DLID AS DeliveryID,
            S.SACUSTID AS CustomerID,
            S.SACUSTNAME AS CustomerName,
            S.SADOCNO AS DocNum,
            S.SADATE AS DocDate,
            S.SACUSTPHONE AS PhoneNumber,
            S.SAQTY AS Quantity,
            S.SADELADD1 AS Address,
            D.DLSTATUS AS Status,
            S.VID AS SaleId,
            D.DLEMPID AS EmpId,
            SUM(D.DLCASHRCVD) AS TotalCashReceived,
            SUM(D.DLBANKRCVD) AS TotalBankReceived,
            SUM(D.DLCASHRCVD + D.DLBANKRCVD) AS AmtReceived
        FROM 
          SALES S
        INNER JOIN 
            DELIVERY D ON S.VID = D.DLSALEID
        WHERE 
            D.DLSALEID = @DLSALEID
            AND D.DLEMPID = @DLEMPID
        GROUP BY 
            D.DLID,
            S.SACUSTID,
            S.SACUSTNAME,
            S.SADOCNO,
            S.SADATE,
            S.SACUSTPHONE,
            S.SAQTY,
            S.SADELADD1,
            D.DLSTATUS,
            S.VID,
            D.DLEMPID";

            var parameters = new { DLSALEID = DLSALEID, DLEMPID = DLEMPID };

            var result = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<SalesDetails>(query, parameters);

            return result;
        }

        public async Task<SummaryDetails> GetSummaryDetails(long EmployeeID, DateTimeOffset Date)
        {
            // Get only the date part of the DateTimeOffset (ignore time)
            var date = Date.Date;

            // Calculate the end of day for the provided date
            DateTimeOffset endOfDay = date.AddDays(1).AddMilliseconds(-1); // End of day (23:59:59.999)

            var query = @"
                    SELECT 
                        E.EMPNAME AS EmployeeName,
                        SUM(CASE WHEN D.DLSTATUS = 1 THEN 1 ELSE 0 END) AS DeliveredCount,
                        SUM(CASE WHEN D.DLSTATUS = 2 THEN 1 ELSE 0 END) AS ReturnedCount,
                        SUM(CASE WHEN D.DLSTATUS = 3 THEN 1 ELSE 0 END) AS PartiallyDeliveredCount,
                        SUM(CASE WHEN D.DLSTATUS = 0 THEN 1 ELSE 0 END) AS PendingCount,
                        SUM(D.DLCASHRCVD) AS TotalCashReceived,
                        SUM(D.DLBANKRCVD) AS TotalBankReceived
                    FROM 
                        DELIVERY D
                    INNER JOIN 
                        Employee E ON D.DLEMPID = E.EMPID
                    WHERE 
                        D.DLEMPID = @EmployeeID
                        AND D.DLASSIGNEDAT <= @EndOfDay  -- Only include records up to the end of the given date
                    GROUP BY 
                        E.EMPNAME";

            // Execute the query using Dapper
            var summary = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<SummaryDetails>(
                query,
                new { EmployeeID, EndOfDay = endOfDay }
            );

            return summary;
        }


        public async Task<ClosingReportDetails> GetClosingReport(long EmployeeID, DateTimeOffset Date)
        {
            // Get the start of the day (00:00:00.000)
            var startOfDay = Date.Date;
            // Get the end of the day (23:59:59.999)
            var endOfDay = startOfDay.AddDays(1).AddMilliseconds(-1);

            var query = @"
                        SELECT 
                            E.EMPNAME AS EmployeeName,
                            COUNT(D.DLSTATUS) AS TotalBills,  -- Total number of bills (all records for the employee on the given day)
                            SUM(CASE WHEN D.DLSTATUS = 1 THEN 1 ELSE 0 END) AS DeliveredCount,  -- Count for status 1 (Delivered)
                            SUM(CASE WHEN D.DLSTATUS = 2 THEN 1 ELSE 0 END) AS ReturnedCount,    -- Count for status 2 (Returned)
                            SUM(CASE WHEN D.DLSTATUS = 3 THEN 1 ELSE 0 END) AS PartiallyDeliveredCount,  -- Count for status 3 (Partially Delivered)
                            SUM(CASE WHEN D.DLSTATUS = 4 THEN 1 ELSE 0 END) AS PendingCount,    -- Count for status 4 (Pending)
                            SUM(D.DLCASHRCVD) AS TotalCashReceived,  -- Total Cash Received on that day
                            SUM(D.DLBANKRCVD) AS TotalBankReceived,  -- Total Bank Received on that day
                            SUM(D.DLCASHRCVD + D.DLBANKRCVD) AS TotalAmount  -- Sum of Cash + Bank Received
                        FROM 
                            DELIVERY D
                        INNER JOIN 
                            Employee E ON D.DLEMPID = E.EMPID
                        WHERE 
                            D.DLEMPID = @EmployeeID
                            AND D.DLASSIGNEDAT >= @StartOfDay  -- Records starting from the beginning of the given day
                            AND D.DLASSIGNEDAT <= @EndOfDay    -- Records ending at the end of the given day
                        GROUP BY 
                            E.EMPNAME";

            // Fetch data from database using Dapper
            var closingReport = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<ClosingReportDetails>(
                query,
                new { EmployeeID, StartOfDay = startOfDay, EndOfDay = endOfDay }
            );

            return closingReport;
        }




        #endregion Delivery
        #region SaleGraph

        public async Task<TotalSummary> SaleGraph(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            TotalSummary q = new();
            var Branchs = Branches;
            if (Branchs != "")
            {
                q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                 " FROM dbo.SALES WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
            }
            else
            {
                q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                " FROM dbo.SALES WHERE SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
            }



            return q;
        }

        #endregion SaleGraph

        #region PurchaseGraph


        public async Task<TotalSummary> PurchaseGraph(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            TotalSummary q = new();
            var Branchs = Branches;

            if (Branchs != "")
            {
                q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(PUNETAMOUNT), 0) AS Amount" +
                " FROM dbo.PURCHASE " +
                " WHERE PURCHASE.PUCOUNTERID IN (" + Branchs + ") AND PURCHASE.PUDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

            }
            else
            {
                q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(PUNETAMOUNT), 0) AS Amount" +
               " FROM dbo.PURCHASE " +
               " WHERE PURCHASE.PUDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });
            }





            return q;
        }


        #endregion PurchaseGraph

        #region SaleSummary

        public async Task<TotalSummary> TotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            TotalSummary q = new();
            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                  " FROM dbo.SALES WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "B2B")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                      " FROM SALES " +
                      " INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID " +
                      " WHERE SALES.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN <> '' AND " +
                      " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "B2C")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                      " FROM SALES " +
                      " INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID " +
                      " WHERE SALES.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN = '' AND " +
                      " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(SRNETAMOUNT), 0) AS Amount" +
                      " FROM SALESRETURN " +
                      " INNER JOIN BILLHEADER ON SALESRETURN.VID = BILLHEADER.VID " +
                      " WHERE SALESRETURN.SRCOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SALESRETURN.SRCUSTTIN = '' AND " +
                      " SALESRETURN.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SALESRETURN.SRCANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(STONETAMOUNT), 0) AS Amount" +
                      " FROM STOCKTRANSOUT " +
                      " WHERE STOCKTRANSOUT.STOCOUNTERID IN (" + Branchs + ")  AND " +
                      " STOCKTRANSOUT.STODATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "CUSTOMERCOUNT")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(ACID) AS CustCount " +
                      " FROM ACMASTER " +
                      " WHERE ACTYPE = 1 AND " +
                      " ACCREATEDAT BETWEEN @FROM AND @TO", new { FROM = Startdate, TO = Enddate });
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                  " FROM dbo.SALES WHERE SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "B2B")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                      " FROM SALES " +
                      " INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID " +
                      " WHERE BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN <> '' AND " +
                      " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "B2C")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                      " FROM SALES " +
                      " INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID " +
                      " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN = '' AND " +
                      " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(SRNETAMOUNT), 0) AS Amount" +
                      " FROM SALESRETURN " +
                      " INNER JOIN BILLHEADER ON SALESRETURN.VID = BILLHEADER.VID " +
                      " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SALESRETURN.SRCUSTTIN = '' AND " +
                      " SALESRETURN.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SALESRETURN.SRCANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(STONETAMOUNT), 0) AS Amount" +
                      " FROM STOCKTRANSOUT " +
                      " WHERE " +
                      " STOCKTRANSOUT.STODATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "CUSTOMERCOUNT")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(ACID) AS CustCount " +
                      " FROM ACMASTER " +
                      " WHERE ACTYPE = 1 AND " +
                      " ACCREATEDAT BETWEEN @FROM AND @TO", new { FROM = Startdate, TO = Enddate });
                }
            }


            return q;
        }
        public async Task<TotalSummary> GPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            TotalSummary q = new();


            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {

                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                     @" SELECT SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                        ELSE (SANETAMOUNT) END)  GpAmt,
                        SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                        FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                        WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0",
                     new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });


                    // q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    //  "SELECT (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT)) +" +
                    //  " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL-SD.SADTLTAXAMT ) AS GpdivAmt " +
                    //" FROM SALES SL " +
                    //   " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                    //   " WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 ",
                    //  new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "B2B")
                {
                    // q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    //      "SELECT (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                    //      "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL-SD.SADTLTAXAMT ) AS GpdivAmt " +
                    //" FROM SALES SL " +
                    //    " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                    //    " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                    //   " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                    //   " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 ", new { FROM = Startdate, TO = Enddate });


                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    @" SELECT SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                        ELSE (SANETAMOUNT) END) AS  GpAmt,
                        SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) AS  GpdivAmt
                        FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                        INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID 
                        WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND  BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN <> '' AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0",
                    new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                }
                else if (Type.ToUpper() == "B2C")
                {
                    // q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    //    "SELECT (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                    //    "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) GpdivAmt " +
                    //" FROM SALES SL " +
                    //   " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                    //   " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                    //  " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                    //  " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 ", new { FROM = Startdate, TO = Enddate });


                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                 @" SELECT SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                        ELSE (SANETAMOUNT) END) AS  GpAmt,
                        SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) AS  GpdivAmt
                        FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                        INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID 
                        WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND  BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN = '' AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0",
                 new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });


                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                        "SELECT  (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                     " FROM STOCKTRANSOUT SO " +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                      " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                      " SO.STODATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    // q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    //  "SELECT (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT)) +" +
                    //  " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL-SD.SADTLTAXAMT ) AS GpdivAmt " +
                    //" FROM SALES SL " +
                    //   " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                    //   " WHERE  SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 ",
                    //  new { FROM = Startdate, TO = Enddate });


                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    @" SELECT SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                        ELSE (SANETAMOUNT) END)  GpAmt,
                        SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                        FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                        WHERE SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0",
                    new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });

                }
                else if (Type.ToUpper() == "B2B")
                {
                    // q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    //      "SELECT (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                    //      "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL-SD.SADTLTAXAMT ) AS GpdivAmt " +
                    //" FROM SALES SL " +
                    //    " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                    //    " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                    //   " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                    //   " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 ", new { FROM = Startdate, TO = Enddate });

                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                 @" SELECT SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                            ELSE (SANETAMOUNT) END) AS  GpAmt,
                            SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) AS  GpdivAmt
                            FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                            INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID 
                            WHERE BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN <> '' AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0",
                 new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                }
                else if (Type.ToUpper() == "B2C")
                {
                    // q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                    //    "SELECT (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                    //    "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) GpdivAmt " +
                    //" FROM SALES SL " +
                    //   " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                    //   " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                    //  " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                    //  " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 ", new { FROM = Startdate, TO = Enddate });

                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
               @" SELECT SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                            ELSE (SANETAMOUNT) END) AS  GpAmt,
                            SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) AS  GpdivAmt
                            FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                            INNER JOIN BILLHEADER ON SALES.VID = BILLHEADER.VID 
                            WHERE BILLHEADER.BHFORMID IN(101,103) AND SALES.SACUSTTIN = '' AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0",
               new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                        "SELECT  (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                     " FROM STOCKTRANSOUT SO " +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                      " WHERE" +
                      " SO.STODATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
            }


            return q;
        }

        public async Task<List<SaleValueByCategory>> SaleValueByCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueByCategory> List = new List<SaleValueByCategory>();

            var Branchs = Branches;

            if (Branches != "")
            {
                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>("SELECT C.CATNAME AS Category,C.CATID AS CategoryId, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                //" FROM SALES SL " +
                //  " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                //  " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                //  " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                //" WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                //" GROUP BY C.CATNAME, C.CATID" +
                //" ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                //List = q.ToList();


                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>(@" 
                        SELECT C.CATNAME AS Category,C.CATID AS CategoryId, ISNULL(SUM(S.AMT), 0) AS Amount
                        FROM CATEGORY C 
                            INNER JOIN PRODUCTS PR ON C.CATID = PR.PRDCATID

                            LEFT JOIN ( SELECT PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT
                                        FROM SALESDTL AS PD 
                                            INNER JOIN SALES AS SL ON PD.VID = SL.VID 
                                        WHERE (SL.SADATE BETWEEN @FROM AND @TO) 
                                            AND (SL.SACOUNTERID IN(" + Branchs + @"))
                                            AND SL.SACANCELED = 0
                                        GROUP BY PD.SADTLPRDID ) AS S
                            ON PR.PRDID = S.PID

                        GROUP BY C.CATNAME, C.CATID
                        ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

                List = q.ToList();
            }
            else
            {
                //  var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>("SELECT C.CATNAME AS Category,C.CATID AS CategoryId, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                //" FROM SALES SL " +
                //  " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                //  " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                //  " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                //" WHERE SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                //" GROUP BY C.CATNAME, C.CATID" +
                //" ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                //  List = q.ToList();

                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>(@" 
                        SELECT C.CATNAME AS Category,C.CATID AS CategoryId, ISNULL(SUM(S.AMT), 0) AS Amount
                        FROM CATEGORY C 
                            INNER JOIN PRODUCTS PR ON C.CATID = PR.PRDCATID

                            LEFT JOIN ( SELECT PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT
                                        FROM SALESDTL AS PD 
                                            INNER JOIN SALES AS SL ON PD.VID = SL.VID 
                                        WHERE (SL.SADATE BETWEEN @FROM AND @TO)
                                        AND SL.SACANCELED = 0
                                        GROUP BY PD.SADTLPRDID ) AS S
                            ON PR.PRDID = S.PID

                        GROUP BY C.CATNAME, C.CATID
                        ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

                List = q.ToList();

            }




            return List;

        }


        public async Task<List<SaleValueBySubCategory>> SaleValueBySubCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueBySubCategory> List = new List<SaleValueBySubCategory>();

            var Branchs = Branches;

            //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySubCategory>("SELECT C.SCATNAME AS SubCategory,C.SCATPARENT AS CategoryId, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
            //  " FROM dbo.SALES SL" +
            //    " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
            //    " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
            //    " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
            //  " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
            //  " GROUP BY C.SCATNAME, C.SCATPARENT"+" ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
            //totalSummaryList = q.ToList();

            if (Branchs != "")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySubCategory>(@"SELECT C.SCATNAME AS SubCategory,C.SCATPARENT AS CategoryId, ISNULL(SUM(ISNULL(PD.AMT,0)), 0) AS Amount 
                                                    FROM SUBCATEGORY C INNER JOIN PRODUCTS PR ON C.SCATID = PR.PRDSUBCATID 
                                                    LEFT JOIN 
	                                                    (SELECT  PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT 
	                                                    FROM SALESDTL AS PD INNER JOIN SALES AS SL ON PD.VID = SL.VID 
	                                                    WHERE (SL.SADATE BETWEEN @FROM AND @TO) AND SL.SACOUNTERID IN (" + Branchs + @")
                                                        AND SL.SACANCELED = 0
	                                                    GROUP BY PD.SADTLPRDID ) AS PD
                                                    ON PR.PRDID = PD.PID
                                                    GROUP BY C.SCATNAME, C.SCATPARENT 
                                                    ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });

                List = q.ToList();
            }
            else
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySubCategory>(@"SELECT C.SCATNAME AS SubCategory,C.SCATPARENT AS CategoryId, ISNULL(SUM(ISNULL(PD.AMT,0)), 0) AS Amount 
                                                    FROM SUBCATEGORY C INNER JOIN PRODUCTS PR ON C.SCATID = PR.PRDSUBCATID 
                                                    LEFT JOIN 
	                                                    (SELECT  PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT 
	                                                    FROM SALESDTL AS PD INNER JOIN SALES AS SL ON PD.VID = SL.VID 
	                                                    WHERE (SL.SADATE BETWEEN @FROM AND @TO)
                                                        AND SL.SACANCELED = 0
	                                                    GROUP BY PD.SADTLPRDID ) AS PD
                                                    ON PR.PRDID = PD.PID
                                                    GROUP BY C.SCATNAME, C.SCATPARENT 
                                                    ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });

                List = q.ToList();
            }





            return List;

        }


        public async Task<List<SaleValueBySupplier>> SaleValueBySupplier(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueBySupplier> List = new List<SaleValueBySupplier>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                //     var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT A.ACACNAME Customer,A.ACID AS CustomerId, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                //" FROM dbo.SALES SL " +
                //  " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                //  " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                //  " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                //" WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                //" GROUP BY A.ACACNAME, A.ACID ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                //totalSummaryList = q.ToList();


                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT C.ACACNAME AS Customer,C.ACID AS CustomerId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM ACMASTER C " +
                                                                                              "INNER JOIN PRODUCTS PR ON C.ACID = PR.PRDSUPPID " +
                                                                                             " LEFT JOIN ( SELECT PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT " +
                                                                                              "FROM SALESDTL AS PD INNER JOIN SALES AS SL ON PD.VID = SL.VID " +
                                                                                             " WHERE (SL.SADATE BETWEEN @FROM AND @TO)" +
                                                                                             "AND (SL.SACOUNTERID IN(" + Branchs + ")) AND SL.SACANCELED = 0" +
                                                                                             " GROUP BY PD.SADTLPRDID ) AS S ON PR.PRDID = S.PID" +
                                                                                             " GROUP BY C.ACACNAME, C.ACID ORDER BY C.ACACNAME ", new { FROM = Startdate, TO = Enddate });






                List = q.ToList();
            }
            else
            {
                // var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT A.ACACNAME Customer,A.ACID AS CustomerId, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                //   " FROM dbo.SALES SL " +
                //     " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                //     " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                //     " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                //   " WHERE SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                //   " GROUP BY A.ACACNAME, A.ACID ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                //// totalSummaryList = q.ToList();


                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT C.ACACNAME AS Customer,C.ACID AS CustomerId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM ACMASTER C " +
                                                                                            "INNER JOIN PRODUCTS PR ON C.ACID = PR.PRDSUPPID " +
                                                                                           " LEFT JOIN ( SELECT PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT " +
                                                                                            "FROM SALESDTL AS PD INNER JOIN SALES AS SL ON PD.VID = SL.VID " +
                                                                                           " WHERE (SL.SADATE BETWEEN @FROM AND @TO)" +
                                                                                           " AND SL.SACANCELED = 0" +
                                                                                           " GROUP BY PD.SADTLPRDID ) AS S ON PR.PRDID = S.PID" +
                                                                                           " GROUP BY C.ACACNAME, C.ACID ORDER BY C.ACACNAME ", new { FROM = Startdate, TO = Enddate });



                List = q.ToList();
            }



            return List;

        }


        public async Task<List<SaleValueByMFR>> SaleValueByMFR(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueByMFR> List = new List<SaleValueByMFR>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT M.MNFRNAME MNFRNAME, M.MNFRID MFRId, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                //" FROM dbo.SALES SL " +
                //  " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                //  " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                //  " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                //" WHERE SL.SACOUNTERID IN (" + Branchs + ") AND (SL.SADATE BETWEEN @FROM AND @TO) AND ISNULL(SL.SACANCELED, 0) = 0 " +
                //" GROUP BY  M.MNFRNAME , M.MNFRID ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                ////totalSummaryList = q.ToList();

                //List = q.ToList();




                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT C.MNFRNAME AS MNFRNAME,C.MNFRID AS MFRId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM MANUFACTURER C " +
                                                                                               "INNER JOIN PRODUCTS PR ON C.MNFRID = PR.PRDMFRID " +
                                                                                              " LEFT JOIN ( SELECT PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT " +
                                                                                               "FROM SALESDTL AS PD INNER JOIN SALES AS SL ON PD.VID = SL.VID " +
                                                                                              " WHERE (SL.SADATE BETWEEN @FROM AND @TO)" +
                                                                                              "AND (SL.SACOUNTERID IN(" + Branchs + ")) AND SL.SACANCELED = 0" +
                                                                                              " GROUP BY PD.SADTLPRDID ) AS S ON PR.PRDID = S.PID" +
                                                                                              " GROUP BY C.MNFRNAME, C.MNFRID ORDER BY C.MNFRNAME ", new { FROM = Startdate, TO = Enddate });


                List = q.ToList();
            }
            else
            {
                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT M.MNFRNAME MNFRNAME, M.MNFRID MFRId, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                //" FROM dbo.SALES SL " +
                //  " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                //  " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                //  " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                //" WHERE SL.SADATE BETWEEN @FROM AND @TO AND (ISNULL(SL.SACANCELED, 0) = 0) " +
                //" GROUP BY  M.MNFRNAME , M.MNFRID ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                // totalSummaryList = q.ToList();

                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT C.MNFRNAME AS MNFRNAME,C.MNFRID AS MFRId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM MANUFACTURER C " +
                                                                                            "INNER JOIN PRODUCTS PR ON C.MNFRID = PR.PRDMFRID " +
                                                                                           " LEFT JOIN ( SELECT PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT " +
                                                                                            "FROM SALESDTL AS PD INNER JOIN SALES AS SL ON PD.VID = SL.VID " +
                                                                                           " WHERE (SL.SADATE BETWEEN @FROM AND @TO)" +
                                                                                           " AND SL.SACANCELED = 0" +
                                                                                           " GROUP BY PD.SADTLPRDID ) AS S ON PR.PRDID = S.PID" +
                                                                                           " GROUP BY C.MNFRNAME, C.MNFRID ORDER BY C.MNFRNAME ", new { FROM = Startdate, TO = Enddate });


                List = q.ToList();

            }


            return List;

        }



        public async Task<List<SaleValueByCategory>> PurchaseValueByCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueByCategory> List = new List<SaleValueByCategory>();

            var Branchs = Branches;

            //  var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>("SELECT C.CATNAME AS Category,C.CATID AS CategoryId,  ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
            //" FROM dbo.PURCHASE P" +
            //     " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
            //     " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
            //     " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
            //    " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND (P.PUDOCDATE BETWEEN @FROM AND @TO) " +
            // " GROUP BY C.CATNAME, C.CATID" +
            // " ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

            if (Branches != "")
            {



                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>(@" 
                        SELECT C.CATNAME AS Category,C.CATID AS CategoryId, ISNULL(SUM(S.AMT), 0) AS Amount
                        FROM CATEGORY C 
                            INNER JOIN PRODUCTS PR ON C.CATID = PR.PRDCATID

                            LEFT JOIN ( SELECT PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT
                                        FROM PURCHASEDTL AS PD 
                                            INNER JOIN PURCHASE AS PU ON PD.VID = PU.VID 
                                        WHERE (PU.PUDOCDATE BETWEEN @FROM AND @TO) 
                                            AND (PU.PUCOUNTERID IN(" + Branchs + @"))
                                            AND PU.PUISAPPROVED = 1
                                        GROUP BY PD.DETPRDID ) AS S
                            ON PR.PRDID = S.PID

                        GROUP BY C.CATNAME, C.CATID
                        ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

                List = q.ToList();
            }
            else
            {


                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>(@" 
                //        SELECT C.CATNAME AS Category,C.CATID AS CategoryId, ISNULL(SUM(S.AMT), 0) AS Amount
                //        FROM CATEGORY C 
                //            INNER JOIN PRODUCTS PR ON C.CATID = PR.PRDCATID

                //            LEFT JOIN ( SELECT PD.SADTLPRDID AS PID, SUM(PD.SADTLTOTAL) AS AMT
                //                        FROM SALESDTL AS PD 
                //                            INNER JOIN SALES AS SL ON PD.VID = SL.VID 
                //                        WHERE (SL.SADATE BETWEEN @FROM AND @TO)
                //                        AND PU.PUISAPPROVED = 1
                //                        GROUP BY PD.SADTLPRDID ) AS S
                //            ON PR.PRDID = S.PID

                //        GROUP BY C.CATNAME, C.CATID
                //        ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByCategory>(@" 
                        SELECT C.CATNAME AS Category,C.CATID AS CategoryId, ISNULL(SUM(S.AMT), 0) AS Amount
                        FROM CATEGORY C 
                            INNER JOIN PRODUCTS PR ON C.CATID = PR.PRDCATID

                            LEFT JOIN ( SELECT PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT
                                        FROM PURCHASEDTL AS PD 
                                            INNER JOIN PURCHASE AS PU ON PD.VID = PU.VID 
                                        WHERE (PU.PUDOCDATE BETWEEN @FROM AND @TO) 
                                            AND PU.PUISAPPROVED = 1
                                        GROUP BY PD.DETPRDID ) AS S
                            ON PR.PRDID = S.PID

                        GROUP BY C.CATNAME, C.CATID
                        ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

                List = q.ToList();

            }

            return List;

        }


        public async Task<List<SaleValueBySubCategory>> PurchaseValueBySubCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueBySubCategory> List = new List<SaleValueBySubCategory>();

            var Branchs = Branches;

            //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySubCategory>("SELECT C.SCATNAME AS SubCategory,C.SCATPARENT AS CategoryId, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
            //  " FROM dbo.PURCHASE P" +
            //   " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
            //   " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
            //   " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
            //  " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
            //  " GROUP BY C.SCATNAME, C.SCATPARENT" + " ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });

            if (Branchs != "")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySubCategory>(@"SELECT C.SCATNAME AS SubCategory,C.SCATPARENT AS CategoryId, ISNULL(SUM(ISNULL(PD.AMT,0)), 0) AS Amount 
                                                    FROM SUBCATEGORY C INNER JOIN PRODUCTS PR ON C.SCATID = PR.PRDSUBCATID 
                                                    LEFT JOIN 
	                                                    (SELECT  PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT 
	                                                    FROM PURCHASEDTL AS PD INNER JOIN PURCHASE AS PU ON PD.VID = PU.VID 
	                                                    WHERE (PU.PUDOCDATE BETWEEN @FROM AND @TO) AND PU.PUCOUNTERID IN (" + Branchs + @")
                                                        AND PU.PUISAPPROVED = 1
	                                                    GROUP BY PD.DETPRDID ) AS PD
                                                    ON PR.PRDID = PD.PID
                                                    GROUP BY C.SCATNAME, C.SCATPARENT 
                                                    ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });

                totalSummaryList = q.ToList();

                List = q.ToList();
            }
            else
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySubCategory>(@"SELECT C.SCATNAME AS SubCategory,C.SCATPARENT AS CategoryId, ISNULL(SUM(ISNULL(PD.AMT,0)), 0) AS Amount 
                                                    FROM SUBCATEGORY C INNER JOIN PRODUCTS PR ON C.SCATID = PR.PRDSUBCATID 
                                                    LEFT JOIN 
	                                                    (SELECT  PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT 
	                                                    FROM PURCHASEDTL AS PD INNER JOIN PURCHASE AS PU ON PD.VID = PU.VID 
	                                                    WHERE (PU.PUDOCDATE BETWEEN @FROM AND @TO)
                                                        AND PU.PUISAPPROVED = 1
	                                                    GROUP BY PD.DETPRDID ) AS PD
                                                    ON PR.PRDID = PD.PID
                                                    GROUP BY C.SCATNAME, C.SCATPARENT 
                                                    ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });

                totalSummaryList = q.ToList();

                List = q.ToList();
            }



            return List;

        }


        public async Task<List<SaleValueBySupplier>> PurchaseValueBySupplier(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueBySupplier> List = new List<SaleValueBySupplier>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT A.ACACNAME Customer,A.ACID AS CustomerId, ISNULL(SUM(P.PUNETAMOUNT), 0) AS Amount" +
                // " FROM dbo.PURCHASE P " +
                // " INNER JOIN ACMASTER A ON P.PUSUPPLIER= A.ACID  " +
                // " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
                // " GROUP BY A.ACACNAME, A.ACID ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                //totalSummaryList = q.ToList();



                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT C.ACACNAME AS Customer,C.ACID AS CustomerId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM ACMASTER C " +
                //                                                                             "INNER JOIN PRODUCTS PR ON C.ACID = PR.PRDSUPPID " +
                //                                                                            " LEFT JOIN ( SELECT PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT " +
                //                                                                             "FROM PURCHASEDTL AS PD INNER JOIN PURCHASE AS SL ON PD.VID = SL.VID " +
                //                                                                            " WHERE (SL.PUDOCDATE BETWEEN @FROM AND @TO)" +
                //                                                                            "AND (SL.PUCOUNTERID IN(" + Branchs + ")) AND SL.PUISAPPROVED = 1" +
                //                                                                            " GROUP BY PD.DETPRDID ) AS S ON PR.PRDID = S.PID" +
                //                                                                            " GROUP BY C.ACACNAME, C.ACID ORDER BY C.ACACNAME ", new { FROM = Startdate, TO = Enddate });



                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT ACMASTER.ACACNAME AS Customer," +
                //                                                                                   " ACMASTER.ACID AS CustomerId," +
                //                                                                                    "ISNULL(SUM(PURCHASE.PUNETAMOUNT), 0) AS Amount" +
                //                                                                                    "FROM ACMASTER  LEFT JOIN PURCHASE ON ACMASTER.ACID = PURCHASE.PUSUPPLIER"+
                //                                                                                    " AND PURCHASE.PUDOCDATE BETWEEN @FROM AND @TO" +
                //                                                                                    "AND PURCHASE.PUCOUNTERID IN(" + Branchs + ")"+
                //                                                                                    "GROUP BY ACMASTER.ACACNAME, ACMASTER.ACID", new { FROM = Startdate, TO = Enddate });

                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>(
                //                                                             "SELECT ACMASTER.ACACNAME AS Customer, " +
                //                                                             "ACMASTER.ACID AS CustomerId, " +
                //                                                             "ISNULL(SUM(PURCHASE.PUNETAMOUNT), 0) AS Amount " +
                //                                                             "FROM ACMASTER " +
                //                                                             "LEFT JOIN PURCHASE ON ACMASTER.ACID = PURCHASE.PUSUPPLIER " +
                //                                                             "AND PURCHASE.PUDOCDATE BETWEEN @FROM AND @TO " +
                //                                                             "AND PURCHASE.PUCOUNTERID IN(" + Branchs + ") AND PURCHASE.PUISAPPROVED = 1 " +
                //                                                             "GROUP BY ACMASTER.ACACNAME, ACMASTER.ACID " +
                //                                                             "ORDER BY ACMASTER.ACACNAME", // You can change this to order by any desired column
                //                                                             new { FROM = Startdate, TO = Enddate }
                //                                                         );


                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>(
                                                                           @"SELECT C.ACACNAME AS Customer,C.ACID AS CustomerId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM ACMASTER C
                                                                            INNER JOIN PRODUCTS PR ON C.ACID = PR.PRDSUPPID
                                                                            LEFT JOIN ( SELECT PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT
                                                                            FROM PURCHASEDTL AS PD INNER JOIN PURCHASE AS SL ON PD.VID = SL.VID
                                                                            WHERE (SL.PUDOCDATE BETWEEN @FROM AND @TO)
                                                                            AND (SL.PUCOUNTERID IN(" + Branchs + @")) AND SL.PUISAPPROVED = 1
                                                                            GROUP BY PD.DETPRDID ) AS S ON PR.PRDID = S.PID
                                                                            GROUP BY C.ACACNAME, C.ACID ORDER BY C.ACACNAME ", // You can change this to order by any desired column
                                                                            new { FROM = Startdate, TO = Enddate }
                                                                        );



                List = q.ToList();
            }
            else
            {
                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>("SELECT A.ACACNAME Customer,A.ACID AS CustomerId, ISNULL(SUM(P.PUNETAMOUNT), 0) AS Amount" +
                //    " FROM dbo.PURCHASE P " +
                //    " INNER JOIN ACMASTER A ON P.PUSUPPLIER= A.ACID  " +
                //    " WHERE P.PUDOCDATE BETWEEN @FROM AND @TO " +
                //    " GROUP BY A.ACACNAME, A.ACID ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                //totalSummaryList = q.ToList();


                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>(
                //                                                            "SELECT ACMASTER.ACACNAME AS Customer, " +
                //                                                            "ACMASTER.ACID AS CustomerId, " +
                //                                                            "ISNULL(SUM(PURCHASE.PUNETAMOUNT), 0) AS Amount " +
                //                                                            "FROM ACMASTER " +
                //                                                            "LEFT JOIN PURCHASE ON ACMASTER.ACID = PURCHASE.PUSUPPLIER " +
                //                                                            "AND PURCHASE.PUDOCDATE BETWEEN @FROM AND @TO " +
                //                                                            "AND PURCHASE.PUISAPPROVED = 1 " +
                //                                                            "GROUP BY ACMASTER.ACACNAME, ACMASTER.ACID " +
                //                                                            "ORDER BY ACMASTER.ACACNAME", // You can change this to order by any desired column
                //                                                            new { FROM = Startdate, TO = Enddate }
                //                                                        );

                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueBySupplier>(
                                                                           @"SELECT C.ACACNAME AS Customer,C.ACID AS CustomerId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM ACMASTER C
                                                                            INNER JOIN PRODUCTS PR ON C.ACID = PR.PRDSUPPID
                                                                            LEFT JOIN ( SELECT PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT
                                                                            FROM PURCHASEDTL AS PD INNER JOIN PURCHASE AS SL ON PD.VID = SL.VID
                                                                            WHERE (SL.PUDOCDATE BETWEEN @FROM AND @TO)
                                                                            AND SL.PUISAPPROVED = 1
                                                                            GROUP BY PD.DETPRDID ) AS S ON PR.PRDID = S.PID
                                                                            GROUP BY C.ACACNAME, C.ACID ORDER BY C.ACACNAME ", // You can change this to order by any desired column
                                                                            new { FROM = Startdate, TO = Enddate }
                                                                           );





                List = q.ToList();

            }


            return List;

        }


        public async Task<List<SaleValueByMFR>> PurchaseValueByMFR(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches)
        {
            List<SaleValueByMFR> List = new List<SaleValueByMFR>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                //  var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT M.MNFRNAME MNFRNAME, M.MNFRID MFRId, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                //" FROM dbo.PURCHASE P" +
                // " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                // " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                // " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                //" WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
                //" GROUP BY M.MNFRNAME, M.MNFRID ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                //  totalSummaryList = q.ToList();


                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT C.MNFRNAME AS MNFRNAME,C.MNFRID AS MFRId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM MANUFACTURER C " +
                                                                                             "INNER JOIN PRODUCTS PR ON C.MNFRID = PR.PRDMFRID " +
                                                                                            " LEFT JOIN ( SELECT PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT " +
                                                                                             "FROM PURCHASEDTL AS PD INNER JOIN PURCHASE AS SL ON PD.VID = SL.VID " +
                                                                                            " WHERE (SL.PUDOCDATE BETWEEN @FROM AND @TO)" +
                                                                                            "AND (SL.PUCOUNTERID IN(" + Branchs + ")) AND SL.PUISAPPROVED = 1" +
                                                                                            " GROUP BY PD.DETPRDID ) AS S ON PR.PRDID = S.PID" +
                                                                                            " GROUP BY C.MNFRNAME, C.MNFRID ORDER BY C.MNFRNAME ", new { FROM = Startdate, TO = Enddate });







                List = q.ToList();
            }
            else
            {
                //var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT M.MNFRNAME MNFRNAME, M.MNFRID MFRId, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                //" FROM dbo.PURCHASE P" +
                // " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                // " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                // " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                //" WHERE P.PUDOCDATE BETWEEN @FROM AND @TO " +
                //" GROUP BY M.MNFRNAME, M.MNFRID ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                //totalSummaryList = q.ToList();

                var q = await this.dapperContext.CreateConnection().QueryAsync<SaleValueByMFR>("SELECT C.MNFRNAME AS MNFRNAME,C.MNFRID AS MFRId, ISNULL(SUM(S.AMT), 0) AS Amount  FROM MANUFACTURER C " +
                                                                                           "INNER JOIN PRODUCTS PR ON C.MNFRID = PR.PRDMFRID " +
                                                                                          " LEFT JOIN ( SELECT PD.DETPRDID AS PID, SUM(PD.DETTOTAL) AS AMT " +
                                                                                           "FROM PURCHASEDTL AS PD INNER JOIN PURCHASE AS SL ON PD.VID = SL.VID " +
                                                                                          " WHERE (SL.PUDOCDATE BETWEEN @FROM AND @TO)" +
                                                                                          " AND SL.PUISAPPROVED = 1" +
                                                                                          " GROUP BY PD.DETPRDID ) AS S ON PR.PRDID = S.PID" +
                                                                                          " GROUP BY C.MNFRNAME, C.MNFRID ORDER BY C.MNFRNAME ", new { FROM = Startdate, TO = Enddate });

                List = q.ToList();
            }



            return List;

        }






        public async Task<List<TotalSummary>> CategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();
            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                  " FROM SALES SL " +
                    " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                    " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                    " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                  " WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                  " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT C.CATNAME AS Catgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                      " GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                       " FROM SALES SL  " +
                         " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                        " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                        " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                        " GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                       " FROM SALESRETURN SR " +
                         " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                        " WHERE SR.SRCOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                        " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                        " GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry ,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                       " FROM STOCKTRANSOUT SO " +
                            " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                            " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                            " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                           " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                           " SO.STODATE   BETWEEN @FROM AND @TO GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                  " FROM SALES SL " +
                    " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                    " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                    " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                  " WHERE SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                  " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });

                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT C.CATNAME AS Catgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                      " GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                       " FROM SALES SL  " +
                         " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                        " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                        " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                        " GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                       " FROM SALESRETURN SR " +
                         " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                        " WHERE BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                        " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                        " GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry ,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                       " FROM STOCKTRANSOUT SO " +
                            " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                            " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                            " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                           " WHERE  " +
                           " SO.STODATE   BETWEEN @FROM AND @TO GROUP BY C.CATNAME ORDER BY C.CATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        public async Task<List<GPPercent>> GPPercentCal(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            var Branchs = Branches;
            List<GPPercent> totalSummaryList = new List<GPPercent>();
            if (Branchs != "")
            {
                if (Type == "CATEGORY")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                        @"SELECT 
                            CATEGORY.CATID AS Id, 
                            CATEGORY.CATNAME AS Name,
                            SUM(CASE WHEN @EXCLTAX = 1 THEN (SALES.SANETAMOUNT - (SALES.SATAX + SALES.SACESSAMT + SALES.SAADDLCESS)) 
                                    ELSE (SANETAMOUNT) END) AS GpAmt,
                            SUM(ABS(SALESDTL.SADTLQTY + SALESDTL.SADTLFREEQTY) * SALESDTL.SADTLCOST) AS GpdivAmt
                        FROM 
                            SALES 
                            INNER JOIN SALESDTL ON SALES.VID = SALESDTL.VID 
                            INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                            RIGHT JOIN CATEGORY ON PRODUCTS.PRDCATID = CATEGORY.CATID
                        WHERE 
                            SALES.SACOUNTERID IN (" + Branchs + @") AND 
                            SALES.SADATE BETWEEN @FROM AND @TO AND 
                            ISNULL(SALES.SACANCELED, 0) = 0 
                        GROUP BY 
                            CATEGORY.CATID, CATEGORY.CATNAME",
                        new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }
                else if (Type == "MFR")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                       @"SELECT 
                        MANUFACTURER.MNFRID AS Id, MANUFACTURER.MNFRNAME AS Name,
                        SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                                                ELSE (SANETAMOUNT) END)  GpAmt,
                                                SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                                                FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                                                INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                                                RIGHT JOIN MANUFACTURER ON PRODUCTS.PRDMFRID = MANUFACTURER.MNFRID
                                                WHERE SALES.SACOUNTERID IN (" + Branchs + @") AND SALES.SADATE 
                                               BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0                   
                        GROUP BY MANUFACTURER.MNFRID, MANUFACTURER.MNFRNAME",
                       new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }
                else if (Type == "Supplier")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                       @"SELECT 
                            ACMASTER.ACID AS Id, 
                            ACMASTER.ACACNAME AS Name,
                            SUM(CASE WHEN @EXCLTAX = 1 THEN (SALES.SANETAMOUNT - (SALES.SATAX + SALES.SACESSAMT + SALES.SAADDLCESS)) 
                                    ELSE (SANETAMOUNT) END) AS GpAmt,
                            SUM(ABS(SALESDTL.SADTLQTY + SALESDTL.SADTLFREEQTY) * SALESDTL.SADTLCOST) AS GpdivAmt
                        FROM 
                            SALES 
                            INNER JOIN SALESDTL ON SALES.VID = SALESDTL.VID 
                            INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                            RIGHT JOIN ACMASTER ON PRODUCTS.PRDSUPPID = ACMASTER.ACID
                        WHERE 
                            SALES.SACOUNTERID IN (" + Branchs + @") AND 
                            SALES.SADATE BETWEEN @FROM AND @TO AND 
                            ISNULL(SALES.SACANCELED, 0) = 0 
                        GROUP BY 
                            ACMASTER.ACID, ACMASTER.ACACNAME",
                       new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }
                else if (Type == "SUBCATEGORY")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                       @"SELECT 
                        SUBCATEGORY.SCATID AS Id, SUBCATEGORY.SCATNAME AS Name,
                        SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                                                ELSE (SANETAMOUNT) END)  GpAmt,
                                                SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                                                FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                                                INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                                                RIGHT JOIN SUBCATEGORY ON PRODUCTS.PRDSUBCATID = SUBCATEGORY.SCATID
                                                WHERE SALES.SACOUNTERID IN (" + Branchs + @") AND SALES.SADATE 
                                                BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0                   
                        GROUP BY SUBCATEGORY.SCATID, SUBCATEGORY.SCATNAME",
                       new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }


            }
            else
            {
                if (Type == "CATEGORY")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                                        @"SELECT 
                        CATEGORY.CATID AS Id, CATEGORY.CATNAME AS Name,
                        SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                                                ELSE (SANETAMOUNT) END)  GpAmt,
                                                SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                                                FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                                                INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                                                RIGHT JOIN CATEGORY ON PRODUCTS.PRDCATID = CATEGORY.CATID
                                                WHERE SALES.SADATE
                                                BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0
                    GROUP BY CATEGORY.CATID, CATEGORY.CATNAME", new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }
                else if (Type == "MFR")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                     @"SELECT 
                        MANUFACTURER.MNFRID AS Id, MANUFACTURER.MNFRNAME AS Name,
                        SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                                                ELSE (SANETAMOUNT) END)  GpAmt,
                                                SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                                                FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                                                INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                                                RIGHT JOIN MANUFACTURER ON PRODUCTS.PRDMFRID = MANUFACTURER.MNFRID
                                                WHERE SALES.SADATE 
                                               BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0                   
                        GROUP BY MANUFACTURER.MNFRID, MANUFACTURER.MNFRNAME",
                     new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }
                else if (Type == "Supplier")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                       @"SELECT 
                            ACMASTER.ACID AS Id, 
                            ACMASTER.ACACNAME AS Name,
                            SUM(CASE WHEN @EXCLTAX = 1 THEN (SALES.SANETAMOUNT - (SALES.SATAX + SALES.SACESSAMT + SALES.SAADDLCESS)) 
                                    ELSE (SANETAMOUNT) END) AS GpAmt,
                            SUM(ABS(SALESDTL.SADTLQTY + SALESDTL.SADTLFREEQTY) * SALESDTL.SADTLCOST) AS GpdivAmt
                        FROM 
                            SALES 
                            INNER JOIN SALESDTL ON SALES.VID = SALESDTL.VID 
                            INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                            RIGHT JOIN ACMASTER ON PRODUCTS.PRDSUPPID = ACMASTER.ACID
                        WHERE                             
                            SALES.SADATE BETWEEN @FROM AND @TO AND 
                            ISNULL(SALES.SACANCELED, 0) = 0 
                        GROUP BY 
                            ACMASTER.ACID, ACMASTER.ACACNAME",
                       new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }
                else if (Type == "SUBCATEGORY")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<GPPercent>(
                       @"SELECT 
                        SUBCATEGORY.SCATID AS Id, SUBCATEGORY.SCATNAME AS Name,
                        SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                                                ELSE (SANETAMOUNT) END)  GpAmt,
                                                SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                                                FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                                                INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                                                RIGHT JOIN SUBCATEGORY ON PRODUCTS.PRDSUBCATID = SUBCATEGORY.SCATID
                                                WHERE SALES.SADATE 
                                                BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0                   
                        GROUP BY SUBCATEGORY.SCATID, SUBCATEGORY.SCATNAME",
                       new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();
                }


            }

            return totalSummaryList;
        }


        public async Task<List<TotalSummary>> CategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {


            var Branchs = Branches;
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    // var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                    //  "SELECT C.CATNAME AS Catgry,C.CATID AS CatgryId, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                    //  "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                    //" FROM SALES SL " +
                    //   " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                    //   " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                    //   " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                    //  " WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                    //  " GROUP BY C.CATNAME, C.CATID ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    // totalSummaryList = q.ToList();

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                                            @"SELECT 
                        CATEGORY.CATID AS CatgryId, CATEGORY.CATNAME AS Catgry,
                        SUM(CASE WHEN @EXCLTAX=1 THEN (SALES.SANETAMOUNT -(SALES.SATAX +SALES.SACESSAMT +SALES.SAADDLCESS )) 
                                                ELSE (SANETAMOUNT) END)  GpAmt,
                                                SUM(ABS(SALESDTL.SADTLQTY+SALESDTL.SADTLFREEQTY)  * SADTLCOST) GpdivAmt
                                                FROM SALES INNER JOIN SALESDTL ON SALES.VID=SALESDTL.VID 
                                                INNER JOIN PRODUCTS ON SALESDTL.SADTLPRDID = PRODUCTS.PRDID
                                                RIGHT JOIN CATEGORY ON PRODUCTS.PRDCATID = CATEGORY.CATID
                                                WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SADATE" +
                                                "BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0 " +
                       "GROUP BY CATEGORY.CATID, CATEGORY.CATNAME", new { FROM = Startdate, TO = Enddate, EXCLTAX = 0 });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                    "SELECT C.CATNAME AS Catgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                  " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                      " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                      " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                   "SELECT C.CATNAME AS Catgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt , SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                   " FROM SALES SL " +
                      " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                     " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                     " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                     " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                        "SELECT  C.CATNAME AS Catgry, (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                      " FROM STOCKTRANSOUT SO" +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                       " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                       " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                      " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT C.CATNAME AS Catgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                   " FROM SALES SL " +
                      " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                      " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                     " WHERE SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                     " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                    "SELECT C.CATNAME AS Catgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                  " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                      " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                      " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                   "SELECT C.CATNAME AS Catgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt , SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                   " FROM SALES SL " +
                      " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                     " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                     " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                     " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                        "SELECT  C.CATNAME AS Catgry, (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                      " FROM STOCKTRANSOUT SO" +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                       " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                       " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " WHERE" +
                      " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> SubCategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                  " FROM dbo.SALES SL" +
                    " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                    " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                    " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                  " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                  " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT C.SCATNAME AS SbCatgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                      " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                      " GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                        " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                        " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                      " FROM SALESRETURN SR " +
                        " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                        " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                        " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                        " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " WHERE SR.SRCOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                       " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                       " GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                      " FROM STOCKTRANSOUT SO" +
                          " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                          " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                            "INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                          " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                          " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                  " FROM dbo.SALES SL" +
                    " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                    " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                    " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                  " WHERE SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                  " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT C.SCATNAME AS SbCatgry, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                      " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                      " GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                        " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                        " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                      " FROM SALESRETURN SR " +
                        " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                        " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                        " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                        " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                       " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                       " GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                      " FROM STOCKTRANSOUT SO" +
                          " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                          " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                            "INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                          " WHERE" +
                          " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY C.SCATNAME ORDER BY C.SCATNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> SubCategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT C.SCATNAME AS SbCatgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                 " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                        " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                     " WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT C.SCATNAME AS SbCatgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                " FROM SALES SL " +
                       " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                       " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        "INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                      " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                      " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                       "SELECT C.SCATNAME AS SbCatgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                       "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                 " FROM SALES SL " +
                      " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                      " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                     " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                     " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                     " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                        "SELECT  C.SCATNAME AS SbCatgry, (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt  " +
                       " FROM STOCKTRANSOUT SO " +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                       " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                       " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                      " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                      " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT C.SCATNAME AS SbCatgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                 " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                        " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                     " WHERE SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT C.SCATNAME AS SbCatgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                " FROM SALES SL " +
                       " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                       " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        "INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                      " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                      " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                       "SELECT C.SCATNAME AS SbCatgry, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                       "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                 " FROM SALES SL " +
                      " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                      " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                     " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                     " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0 " +
                     " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                        "SELECT  C.SCATNAME AS SbCatgry, (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt  " +
                       " FROM STOCKTRANSOUT SO " +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                       " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                       " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                      " WHERE" +
                      " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> MFRTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                  " FROM dbo.SALES SL " +
                    " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                    " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                    " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                  " WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                  " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT M.MNFRNAME MFR, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                       " FROM SALES SL " +
                         " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                         " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                       " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                       " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY M.MNFRNAME ORDER BY M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                       " FROM SALESRETURN SR " +
                         " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                         " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                        " WHERE SR.SRCOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                        " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                        " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                       " FROM STOCKTRANSOUT SO " +
                           " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                           " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                           " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                           " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                           " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY  M.MNFRNAME  ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                  " FROM dbo.SALES SL " +
                    " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                    " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                    " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                  " WHERE  SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                  " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT M.MNFRNAME MFR, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                       " FROM SALES SL " +
                         " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                         " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                       " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                       " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY M.MNFRNAME ORDER BY M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                       " FROM SALESRETURN SR " +
                         " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                         " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                        " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                        " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                        " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                       " FROM STOCKTRANSOUT SO " +
                           " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                           " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                           " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                           " WHERE " +
                           " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY  M.MNFRNAME  ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> MFRGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT  M.MNFRNAME MFR, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
               "FROM SALES SL " +
                     "INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                     "INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                      "INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                     "WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                       "SELECT  M.MNFRNAME MFR, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                " FROM SALES SL " +
                       " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                       " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                          "INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                      " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                      " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT  M.MNFRNAME MFR, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt,SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                   " FROM SALES SL " +
                      " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                      " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                     " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                     " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                        "SELECT   M.MNFRNAME MFR, (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                      " FROM STOCKTRANSOUT SO " +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                       " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                       " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                      " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                      " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT  M.MNFRNAME MFR, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
               "FROM SALES SL " +
                     "INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                     "INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                      "INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                     "WHERE  SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                       "SELECT  M.MNFRNAME MFR, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      "SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                " FROM SALES SL " +
                       " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                       " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                          "INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                      " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                      " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                      " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                     "SELECT  M.MNFRNAME MFR, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                     " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt,SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                   " FROM SALES SL " +
                      " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                      " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                     " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                     " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY  M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                        "SELECT   M.MNFRNAME MFR, (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                      " FROM STOCKTRANSOUT SO " +
                       " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                       " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                       " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                      " WHERE " +
                      " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> SupplierTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                    " FROM dbo.SALES SL " +
                      " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                      " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                    " WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                    " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT A.ACACNAME Supplier, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                        " FROM SALES SL " +
                         " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                         " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                        " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                        " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                        " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                       " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                        " FROM SALESRETURN SR " +
                        " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                        " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                        " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                       " WHERE SR.SRCOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                       " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                        " FROM STOCKTRANSOUT SO " +
                            " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                            " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                            " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                          " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                          " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                return totalSummaryList;

            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                    " FROM dbo.SALES SL " +
                      " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                      " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                      " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                    " WHERE  SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                    " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT A.ACACNAME Supplier, ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                        " FROM SALES SL " +
                         " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                         " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                         " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                         " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                        " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                        " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                        " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier,ISNULL(SUM(S.SADTLTOTAL), 0) AS Amount" +
                      " FROM SALES SL " +
                       " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " INNER JOIN SALESDTL  S ON SL.VID =S.VID  " +
                       " INNER JOIN PRODUCTS PR ON S.SADTLPRDID  = PR.PRDID " +
                       " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                       " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 " +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "SALESRETURNB2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier,ISNULL(SUM(S.SRDTLTOTAL), 0) AS Amount" +
                        " FROM SALESRETURN SR " +
                        " INNER JOIN BILLHEADER ON SR.VID = BILLHEADER.VID " +
                        " INNER JOIN SALESRETDTL  S ON SR.VID =S.VID  " +
                        " INNER JOIN PRODUCTS PR ON S.SRDTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                       " WHERE BILLHEADER.BHFORMID IN(101,103) AND SR.SRCUSTTIN = '' AND " +
                       " SR.SRDATE BETWEEN @FROM AND @TO AND ISNULL(SR.SRCANCELED, 0) = 0 " +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier,ISNULL(SUM(S.STODTLTOTAL), 0) AS Amount" +
                        " FROM STOCKTRANSOUT SO " +
                            " INNER JOIN STOCKTRANSOUTDTLS  S ON SO.VID =S.VID  " +
                            " INNER JOIN PRODUCTS PR ON S.STODTLPRDID  = PR.PRDID " +
                            " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                          " WHERE " +
                          " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> SupplierGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                      "SELECT  A.ACACNAME Supplier, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                  "FROM SALES SL " +
                      " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                      " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                     " WHERE SL.SACOUNTERID IN (" + Branchs + ") AND SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                      "SELECT  A.ACACNAME Supplier,  (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                 " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                      "SELECT  A.ACACNAME Supplier,  (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                   " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " WHERE SL.SACOUNTERID IN (" + Branchs + ")  AND  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                         "SELECT  A.ACACNAME Supplier,  (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                        " FROM STOCKTRANSOUT SO" +
                        " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                        " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                       " WHERE SO.STOCOUNTERID IN (" + Branchs + ")  AND " +
                       " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "SALES")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                      "SELECT  A.ACACNAME Supplier, (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                  "FROM SALES SL " +
                      " INNER JOIN SALESDTL SD ON SL.VID = SD.VID " +
                      " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID = PR.PRDID " +
                      " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                     " WHERE  SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                     " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2B")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                      "SELECT  A.ACACNAME Supplier,  (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                 " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " WHERE  BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN <> '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "B2C")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                      "SELECT  A.ACACNAME Supplier,  (SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) -((SUM(abs(SD.SADTLQTY + SD.SADTLFREEQTY) * SD.SADTLCOST) - SUM(SD.SADTLTAXAMT))+" +
                      " SUM(abs(SD.SADTLQTY+SD.SADTLFREEQTY)  *SD.SADTLFTRCOSTAFFECTAMT))) AS GpAmt, SUM(SD.SADTLTOTAL -SD.SADTLTAXAMT) AS GpdivAmt " +
                   " FROM SALES SL " +
                        " INNER JOIN SALESDTL SD   ON SL.VID =SD.VID " +
                        " INNER JOIN PRODUCTS PR ON SD.SADTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                        " INNER JOIN BILLHEADER ON SL.VID = BILLHEADER.VID " +
                       " WHERE BILLHEADER.BHFORMID IN(101,103) AND SL.SACUSTTIN = '' AND " +
                       " SL.SADATE BETWEEN @FROM AND @TO AND ISNULL(SL.SACANCELED, 0) = 0 AND SD.SADTLISTAXABLECHARGE=0" +
                       " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKOUTWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(
                         "SELECT  A.ACACNAME Supplier,  (SUM(SD.STODTLRATE * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT ))- (SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpAmt,(SUM(SD.STODTLPUCOST * SD.STODTLQTY)-SUM(SD.STODTLTAXAMT )) AS GpdivAmt " +
                        " FROM STOCKTRANSOUT SO" +
                        " INNER JOIN STOCKTRANSOUTDTLS  SD ON SO.VID =SD.VID  " +
                        " INNER JOIN PRODUCTS PR ON SD.STODTLPRDID  = PR.PRDID " +
                        " INNER JOIN ACMASTER A ON PR.PRDSUPPID= A.ACID  " +
                       " WHERE" +
                       " SO.STODATE   BETWEEN @FROM AND @TO  GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            return totalSummaryList;
        }

        #endregion SaleSummary


        #region PurchaseSummary

        public async Task<TotalSummary> PurTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            TotalSummary q = new();
            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "PURCHASE")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(PUNETAMOUNT), 0) AS Amount" +
                  " FROM dbo.PURCHASE " +
                  " WHERE PURCHASE.PUCOUNTERID IN (" + Branchs + ") AND PURCHASE.PUISAPPROVED = 1 AND PURCHASE.PUDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "PURCHASERET")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(PRNETAMOUNT), 0) AS Amount" +
                      " FROM PURCHASERET " +
                      " WHERE PURCHASERET.PRCOUNTERID IN (" + Branchs + ")  AND" +
                      " PURCHASERET.PRDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(STINETAMOUNT), 0) AS Amount" +
                      " FROM STOCKTRANSIN " +
                      " WHERE STOCKTRANSIN.STICOUNTERID IN (" + Branchs + ")  AND " +
                      " STOCKTRANSIN.STIDOCDATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
            }
            else
            {
                if (Type.ToUpper() == "PURCHASE")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT  ISNULL(SUM(PUNETAMOUNT), 0) AS Amount" +
                  " FROM dbo.PURCHASE " +
                  " WHERE PURCHASE.PUISAPPROVED = 1 AND PURCHASE.PUDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "PURCHASERET")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(PRNETAMOUNT), 0) AS Amount" +
                      " FROM PURCHASERET " +
                      " WHERE" +
                      " PURCHASERET.PRDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT ISNULL(SUM(STINETAMOUNT), 0) AS Amount" +
                      " FROM STOCKTRANSIN " +
                      " WHERE" +
                      " STOCKTRANSIN.STIDOCDATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
            }


            return q;
        }
        public async Task<TotalSummary> PurGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            TotalSummary q = new();


            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                      "SELECT (SUM(PD.DETRATE * PD.DETQTY)-SUM(PD.DETTAXAMT ))- (SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpAmt,(SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpdivAmt " +
                    " FROM PURCHASE P " +
                      " INNER JOIN PURCHASEDTL PD ON P.VID = PD.VID " +
                      " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });


                }

                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT " +
                         " (SUM(SD.DETRATE * SD.DETQTY)-SUM(SD.DETTAXAMT ))- (SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpAmt,(SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpdivAmt  " +
                        " FROM STOCKTRANSIN S" +
                         " INNER JOIN STOCKTRANSINDTLS SD ON S.VID= SD.VID  " +
                      " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                      " S.STIDOCDATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
            }
            else
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    q = await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(
                      "SELECT (SUM(PD.DETRATE * PD.DETQTY)-SUM(PD.DETTAXAMT ))- (SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpAmt,(SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpdivAmt " +
                    " FROM PURCHASE P " +
                      " INNER JOIN PURCHASEDTL PD ON P.VID = PD.VID " +
                      " WHERE  P.PUDOCDATE BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });


                }

                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT " +
                         " (SUM(SD.DETRATE * SD.DETQTY)-SUM(SD.DETTAXAMT ))- (SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpAmt,(SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpdivAmt  " +
                        " FROM STOCKTRANSIN S" +
                         " INNER JOIN STOCKTRANSINDTLS SD ON S.VID= SD.VID  " +
                      " WHERE" +
                      " S.STIDOCDATE   BETWEEN @FROM AND @TO ", new { FROM = Startdate, TO = Enddate });

                }
            }


            return q;
        }
        public async Task<List<TotalSummary>> PurCategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;
            if (Branchs != "")
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                   " FROM dbo.PURCHASE P" +
                    " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                    " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                    " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                   " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
                   " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "PURCHASERET")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,  ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                      " FROM PURCHASERET P" +
                         " INNER JOIN PURCHASERETDTL  PD ON P.VID =PD.VID  " +
                         " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                       " WHERE P.PRCOUNTERID IN (" + Branchs + ")  AND" +
                       " P.PRDOCDATE BETWEEN @FROM AND @TO " +
                       " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,ISNULL(SUM(S.STINETAMOUNT), 0) AS Amount" +
                       " FROM STOCKTRANSIN S" +
                          " INNER JOIN STOCKTRANSINDTLS  SD ON S.VID =SD.VID  " +
                          " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                          " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                       " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                       " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                       " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                   " FROM dbo.PURCHASE P" +
                    " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                    " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                    " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                   " WHERE  P.PUDOCDATE BETWEEN @FROM AND @TO " +
                   " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "PURCHASERET")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,  ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                      " FROM PURCHASERET P" +
                         " INNER JOIN PURCHASERETDTL  PD ON P.VID =PD.VID  " +
                         " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                       " WHERE" +
                       " P.PRDOCDATE BETWEEN @FROM AND @TO " +
                       " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry,ISNULL(SUM(S.STINETAMOUNT), 0) AS Amount" +
                       " FROM STOCKTRANSIN S" +
                          " INNER JOIN STOCKTRANSINDTLS  SD ON S.VID =SD.VID  " +
                          " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                          " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                       " WHERE" +
                       " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                       " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }

            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> PurCategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry, " +
                     " (SUM(PD.DETRATE * PD.DETQTY)-SUM(PD.DETTAXAMT ))- (SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpAmt,(SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpdivAmt " +
                    " FROM dbo.PURCHASE P " +
                     " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                     " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                     " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                    " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
                    " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry," +
                         " (SUM(SD.DETRATE * SD.DETQTY)-SUM(SD.DETTAXAMT ))- (SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpAmt,(SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpdivAmt  " +
                      " FROM STOCKTRANSIN S " +
                         " INNER JOIN STOCKTRANSINDTLS SD ON S.VID= SD.VID  " +
                         " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                      " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                      " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
            }
            else
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry, " +
                     " (SUM(PD.DETRATE * PD.DETQTY)-SUM(PD.DETTAXAMT ))- (SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpAmt,(SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpdivAmt " +
                    " FROM dbo.PURCHASE P " +
                     " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                     " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                     " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                    " WHERE P.PUDOCDATE BETWEEN @FROM AND @TO " +
                    " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CATNAME AS Catgry," +
                         " (SUM(SD.DETRATE * SD.DETQTY)-SUM(SD.DETTAXAMT ))- (SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpAmt,(SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpdivAmt  " +
                      " FROM STOCKTRANSIN S " +
                         " INNER JOIN STOCKTRANSINDTLS SD ON S.VID= SD.VID  " +
                         " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                         " INNER JOIN CATEGORY C ON PR.PRDCATID = C.CATID " +
                      " WHERE " +
                      " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                      " GROUP BY C.CATNAME ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> PurSubCategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                    " FROM dbo.PURCHASE P" +
                     " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                     " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                     " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                    " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
                    " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "PURCHASERET")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,  ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                       " FROM PURCHASERET P" +
                          " INNER JOIN PURCHASERETDTL  PD ON P.VID =PD.VID  " +
                          " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                          " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                        " WHERE P.PRCOUNTERID IN (" + Branchs + ")  AND" +
                        " P.PRDOCDATE BETWEEN @FROM AND @TO " +
                        " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(SD.DETTOTAL), 0) AS Amount" +
                       " FROM STOCKTRANSIN S" +
                          " INNER JOIN STOCKTRANSINDTLS  SD ON S.VID =SD.VID  " +
                          " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                          " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                       " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                       " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
            }
            else
            {
                if (Type.ToUpper() == "PURCHASE")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                    " FROM dbo.PURCHASE P" +
                     " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                     " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                     " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                    " WHERE  P.PUDOCDATE BETWEEN @FROM AND @TO " +
                    " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "PURCHASERET")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,  ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                       " FROM PURCHASERET P" +
                          " INNER JOIN PURCHASERETDTL  PD ON P.VID =PD.VID  " +
                          " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                          " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                        " WHERE" +
                        " P.PRDOCDATE BETWEEN @FROM AND @TO " +
                        " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "STOCKINWARD")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry,ISNULL(SUM(SD.DETTOTAL), 0) AS Amount" +
                       " FROM STOCKTRANSIN S" +
                          " INNER JOIN STOCKTRANSINDTLS  SD ON S.VID =SD.VID  " +
                          " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                          " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                       " WHERE" +
                       " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                       " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
            }


            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> PurSubCategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;


            if (Type.ToUpper() == "PURCHASE")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry, " +
                     " (SUM(PD.DETRATE * PD.DETQTY)-SUM(PD.DETTAXAMT ))- (SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpAmt,(SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpdivAmt " +
               " FROM dbo.PURCHASE P " +
                " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
               " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
               " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }

            else if (Type.ToUpper() == "STOCKINWARD")
            {

                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.SCATNAME AS SbCatgry," +
                     " (SUM(SD.DETRATE * SD.DETQTY)-SUM(SD.DETTAXAMT ))- (SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpAmt,(SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpdivAmt  " +
                   " FROM STOCKTRANSIN S " +
                      " INNER JOIN STOCKTRANSINDTLS  SD ON S.VID =SD.VID  " +
                      " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                      " INNER JOIN SUBCATEGORY C ON PR.PRDSUBCATID = C.SCATID " +
                   " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                   " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                   " GROUP BY C.SCATNAME ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();

            }
            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> PurMFRTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();


            var Branchs = Branches;


            if (Type.ToUpper() == "PURCHASE")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR, ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
               " FROM dbo.PURCHASE P" +
                " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
               " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
               " GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }
            else if (Type.ToUpper() == "PURCHASERET")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,  ISNULL(SUM(PD.DETTOTAL), 0) AS Amount" +
                   " FROM PURCHASERET P" +
                      " INNER JOIN PURCHASERETDTL  PD ON P.VID =PD.VID  " +
                      " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                      " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                    " WHERE P.PRCOUNTERID IN (" + Branchs + ")  AND" +
                    " P.PRDOCDATE BETWEEN @FROM AND @TO " +
                    " GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }
            else if (Type.ToUpper() == "STOCKINWARD")
            {

                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR,ISNULL(SUM(SD.DETTOTAL), 0) AS Amount" +
                  " FROM STOCKTRANSIN S" +
                     " INNER JOIN STOCKTRANSINDTLS  SD ON S.VID =SD.VID  " +
                     " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                     " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                  " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                  " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                  " GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }
            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> PurMFRGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;
            if (Type.ToUpper() == "PURCHASE")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR," +
                     " (SUM(PD.DETRATE * PD.DETQTY)-SUM(PD.DETTAXAMT ))- (SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpAmt,(SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpdivAmt " +
               " FROM dbo.PURCHASE P " +
                " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                " INNER JOIN PRODUCTS PR ON PD.DETPRDID  = PR.PRDID " +
                " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
               " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
               " GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }

            else if (Type.ToUpper() == "STOCKINWARD")
            {

                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT M.MNFRNAME MFR," +
                      " (SUM(SD.DETRATE * SD.DETQTY)-SUM(SD.DETTAXAMT ))- (SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpAmt,(SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpdivAmt  " +
                     " FROM STOCKTRANSIN S " +
                        " INNER JOIN STOCKTRANSINDTLS SD ON S.VID= SD.VID  " +
                        " INNER JOIN PRODUCTS PR ON SD.DETPRDID  = PR.PRDID " +
                        " INNER JOIN MANUFACTURER  M ON PR.PRDMFRID =M.MNFRID  " +
                     " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                     " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                     " GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();

            }
            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> PurSupplierTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Type.ToUpper() == "PURCHASE")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier, ISNULL(SUM(P.PUNETAMOUNT), 0) AS Amount" +
                " FROM dbo.PURCHASE P " +
                    " INNER JOIN ACMASTER A ON P.PUSUPPLIER= A.ACID  " +
                " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
                " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }
            else if (Type.ToUpper() == "PURCHASERET")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier,  ISNULL(SUM(P.PRNETAMOUNT), 0) AS Amount" +
                   " FROM PURCHASERET P " +
                    " INNER JOIN ACMASTER A ON P.PRSUPPLIER= A.ACID  " +
                    " WHERE P.PRCOUNTERID IN (" + Branchs + ")  AND" +
                    " P.PRDOCDATE BETWEEN @FROM AND @TO " +
                    " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }
            else if (Type.ToUpper() == "STOCKINWARD")
            {

                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier, ISNULL(SUM(S.STINETAMOUNT), 0) AS Amount" +
                   " FROM STOCKTRANSIN S " +
                   " INNER JOIN ACMASTER A ON S.STISUPID= A.ACID  " +
                   " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                   " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                   " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();

            }
            return totalSummaryList;
        }

        public async Task<List<TotalSummary>> PurSupplierGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;


            if (Type.ToUpper() == "PURCHASE")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier, " +
                     " (SUM(PD.DETRATE * PD.DETQTY)-SUM(PD.DETTAXAMT ))- (SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpAmt,(SUM(PD.DETCOST * PD.DETQTY)-SUM(PD.DETTAXAMT )) AS GpdivAmt  " +
               " FROM dbo.PURCHASE P " +
                " INNER JOIN PURCHASEDTL  PD ON P.VID =PD.VID  " +
                " INNER JOIN ACMASTER A ON P.PUSUPPLIER= A.ACID  " +
               " WHERE P.PUCOUNTERID IN (" + Branchs + ") AND P.PUDOCDATE BETWEEN @FROM AND @TO " +
               " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();
            }

            else if (Type.ToUpper() == "STOCKINWARD")
            {

                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT A.ACACNAME Supplier, " +
                      " (SUM(SD.DETRATE * SD.DETQTY)-SUM(SD.DETTAXAMT ))- (SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpAmt,(SUM(SD.DETCOST * SD.DETQTY)-SUM(SD.DETTAXAMT )) AS GpdivAmt  " +
                   " FROM STOCKTRANSIN S " +
                       " INNER JOIN STOCKTRANSINDTLS SD ON S.VID= SD.VID  " +
                       " INNER JOIN ACMASTER A ON S.STISUPID= A.ACID  " +
                   " WHERE S.STICOUNTERID IN (" + Branchs + ")  AND " +
                   " S.STIDOCDATE   BETWEEN @FROM AND @TO " +
                   " GROUP BY A.ACACNAME ORDER BY A.ACACNAME ", new { FROM = Startdate, TO = Enddate });
                totalSummaryList = q.ToList();

            }
            return totalSummaryList;
        }

        #endregion PurchaseSummary


        #region ClosingStock

        public async Task<TotalSummary> ClosingStock(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {

            Stock S = new Stock();
            TotalSummary q = new();
            var Branchs = Branches;
            //"       LEFT JOIN  (SELECT ST.STKPRDID,ST.STKBATCHID, ISNULL(SUM(ST.STKSTOCK),0) AS QTY   " +
            //                                        "                   FROM STOCKDET ST     " +
            //                                        "                   INNER JOIN PRODUCTS ON PRODUCTS.PRDID=ST.STKPRDID   " +
            //                                        "                   INNER JOIN PRDBATCH ON ST.STKBATCHID=PRDBATCH.BCID    " +
            //                                        "                   WHERE ST.STKLSTUPDTIME <=@TO " +
            //                                        "                   AND (ST.STKBRANCHID IN (" + Branchs + "))     " +
            //                                        "                   GROUP BY ST.STKPRDID,ST.STKBATCHID) AS STOCK  " +
            //                                        "       ON PRODUCTS.PRDID=STOCK.STKPRDID  AND PRDBATCH.BCID=STOCK.STKBATCHID   " +

            if (Branchs != "")
            {
                q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(" SELECT CASE WHEN @TYP='MRP'  THEN  ISNULL(SUM(STOCK.QTY*PLMRP),0)  " +
                                                     "             WHEN @TYP='COST' THEN  ISNULL(SUM(STOCK.QTY*PLPUCOST),0)  " +
                                                     "             WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0) )" +
                                                     "             ELSE 0 END AS StkValue" +
                                                     "  FROM PRODUCTS  " +
                                                     "       INNER JOIN PRDBATCH ON PRODUCTS.PRDID =PRDBATCH.BCPRDID    " +
                                                     "       INNER JOIN PRDPRICELIST ON PRODUCTS.PRDID =PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID=PRDBATCH.BCID " +
                                                     "       LEFT JOIN  (SELECT TRDETAILS.DETPRDID,TRDETAILS.DETBCID ,ISNULL(SUM(DETQTY),0) AS QTY   " +
                                                     "                   FROM TRDETAILS      " +
                                                     "                        INNER JOIN PRODUCTS ON PRODUCTS.PRDID=TRDETAILS.DETPRDID   " +
                                                     "                        INNER JOIN PRDBATCH ON TRDETAILS.DETBCID=PRDBATCH.BCID     " +
                                                     "                   WHERE (TRDETAILS.DETDATE <=@TO or TRDETAILS.DETFLAG='OP') " +
                                                     "                   AND (TRDETAILS.DETCOUNTERID IN (" + Branchs + "))     " +
                                                     "                   GROUP BY TRDETAILS.DETPRDID,DETBCID) AS STOCK  " +
                                                     "       ON PRODUCTS.PRDID=STOCK.DETPRDID  AND PRDBATCH.BCID=STOCK.DETBCID   " +
                                                     "       WHERE  PRODUCTS.PRDTYPE=0 ", new { FROM = Startdate, TO = Enddate, TYP = Type });
            }
            else
            {
                //q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(" SELECT CASE WHEN @TYP='MRP'  THEN  ISNULL(SUM(STOCK.QTY*PLMRP),0)  " +
                //                                    "             WHEN @TYP='COST' THEN  ISNULL(SUM(STOCK.QTY*PLPUCOST),0)  " +
                //                                    "             WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0) )" +
                //                                    "             ELSE 0 END AS StkValue" +
                //                                    "  FROM PRODUCTS  " +
                //                                    "       INNER JOIN PRDBATCH ON PRODUCTS.PRDID =PRDBATCH.BCPRDID    " +
                //                                    "       INNER JOIN PRDPRICELIST ON PRODUCTS.PRDID =PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID=PRDBATCH.BCID " +
                //                                    "       LEFT JOIN  (SELECT TRDETAILS.DETPRDID,TRDETAILS.DETBCID ,ISNULL(SUM(DETQTY),0) AS QTY   " +
                //                                    "                   FROM TRDETAILS      " +
                //                                    "                        INNER JOIN PRODUCTS ON PRODUCTS.PRDID=TRDETAILS.DETPRDID   " +
                //                                    "                        INNER JOIN PRDBATCH ON TRDETAILS.DETBCID=PRDBATCH.BCID     " +
                //                                    "                   WHERE (TRDETAILS.DETDATE <=@TO or TRDETAILS.DETFLAG='OP') " +
                //                                    "                      )     " +
                //                                    "                   GROUP BY TRDETAILS.DETPRDID,DETBCID) AS STOCK  " +
                //                                    "       ON PRODUCTS.PRDID=STOCK.DETPRDID  AND PRDBATCH.BCID=STOCK.DETBCID   " +
                //                                    "       WHERE  PRODUCTS.PRDTYPE=0 ", new { FROM = Startdate, TO = Enddate, TYP = Type });

                q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>(@"SELECT 
                        CASE 
                            WHEN @TYP = 'MRP' THEN ISNULL(SUM(STOCK.QTY * PLMRP), 0)
                            WHEN @TYP = 'COST' THEN ISNULL(SUM(STOCK.QTY * PLPUCOST), 0)
                            WHEN @TYP = 'COSTGST' THEN (ISNULL(SUM(STOCK.QTY * PLPURATE), 0) + ISNULL(SUM(STOCK.QTY * PLPUCOSTEXCLTAX), 0))
                            ELSE 0 
                        END AS StkValue
                    FROM 
                        PRODUCTS  
                    INNER JOIN 
                        PRDBATCH ON PRODUCTS.PRDID = PRDBATCH.BCPRDID    
                    INNER JOIN 
                        PRDPRICELIST ON PRODUCTS.PRDID = PRDPRICELIST.PLPRDID AND PRDPRICELIST.PLBCID = PRDBATCH.BCID 
                    LEFT JOIN  
                        (SELECT 
                             TRDETAILS.DETPRDID, 
                             TRDETAILS.DETBCID,
                             ISNULL(SUM(DETQTY), 0) AS QTY   
                         FROM 
                             TRDETAILS      
                         INNER JOIN 
                             PRODUCTS ON PRODUCTS.PRDID = TRDETAILS.DETPRDID   
                         INNER JOIN 
                             PRDBATCH ON TRDETAILS.DETBCID = PRDBATCH.BCID     
                         WHERE 
                             (TRDETAILS.DETDATE <= @TO or TRDETAILS.DETFLAG = 'OP') 
                         GROUP BY 
                             TRDETAILS.DETPRDID, TRDETAILS.DETBCID
                        ) AS STOCK ON PRODUCTS.PRDID = STOCK.DETPRDID AND PRDBATCH.BCID = STOCK.DETBCID   
                    WHERE  
                        PRODUCTS.PRDTYPE = 0
                    ", new { FROM = Startdate, TO = Enddate, TYP = Type });
            }



            //q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT SD.*, P.PRDNAME, HSN.HSNNAME, PRDPRICELIST.PLMRP,PRDPRICELIST.PLPUCOST,CATEGORY.CATNAME, PRDBATCH.BCEXPIRYDATE, PRDBATCH.BCBATCHNO, TAXMASTER.TAXRATE," +
            // "DRUG_SCHEDULE.DSNAME, SUBCATEGORY.SCATNAME, CONTENT.CONNAME, MANUFACTURER.MNFRNAME, BRAND.BRNAME" +
            // "FROM STOCKDET AS SD " +
            // "INNER JOIN PRODUCTS AS P ON P.PRDID = SD.STKPRDID " +
            // "INNER JOIN PRDBATCH ON PRDBATCH.BCID = SD.STKBATCHID " +
            // "LEFT JOIN HSN ON P.PRDHSN = HSN.HSNID " +
            // "INNER JOIN CATEGORY ON P.PRDCATID = CATEGORY.CATID " +
            // "INNER JOIN SUBCATEGORY ON P.PRDSUBCATID = SUBCATEGORY.SCATID " +
            // "LEFT JOIN MANUFACTURER ON P.PRDMFRID = MANUFACTURER.MNFRID " +
            // "INNER JOIN PRDPRICELIST ON PRDBATCH.BCID = PRDPRICELIST.PLBCID " +
            // "LEFT JOIN BRAND ON P.PRDBRANDID = BRAND.BRDID " +
            // "LEFT JOIN CONTENT ON P.PRDCONTENTID = CONTENT.CONID " +
            // "LEFT JOIN DRUG_SCHEDULE ON P.PRDDRUGSCHEDULEID = DRUG_SCHEDULE.DSID " +
            // "INNER JOIN TAXMASTER ON P.PRDTAXID = TAXMASTER.TAXID " +
            // //"WHERE (SD.STKBRANCHID = @CNTID OR @CNTID <= 0) " +
            // //"AND (P.PRDCATID = @CATRID OR @CATRID <= 0) " +
            // //"AND (P.PRDSUBCATID = @SUBCTR OR @SUBCTR <= 0) " +
            // //"AND (P.PRDMFRID = @MFRID OR @MFRID <= 0) " +
            // "ORDER BY P.PRDNAME")/*, new { CATID = CatID, CNTID = CounterId, CATRID = CatID, SUBCTR = SubCatID, MFRID = Manufacture })*/;

            // S = (Stock)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<Stock>(
            //"SELECT" +
            //" SD.*, P.PRDNAME," +
            //" CATEGORY.CATNAME," +
            //" SUBCATEGORY.SCATNAME, CONTENT.CONNAME," +
            //" MANUFACTURER.MNFRNAME, BRAND.BRNAME" +
            //"FROM" +
            //" STOCKDET AS SD " +
            //"INNER JOIN" +
            //" PRODUCTS AS P ON P.PRDID = SD.STKPRDID " +
            //"INNER JOIN" +
            //" CATEGORY ON P.PRDCATID = CATEGORY.CATID " +
            //"INNER JOIN" +
            //" SUBCATEGORY ON P.PRDSUBCATID = SUBCATEGORY.SCATID " +
            //"LEFT JOIN" +
            //" MANUFACTURER ON P.PRDMFRID = MANUFACTURER.MNFRID " +
            //"LEFT JOIN" +
            //" BRAND ON P.PRDBRANDID = BRAND.BRDID " +
            //"LEFT JOIN" +
            //" CONTENT ON P.PRDCONTENTID = CONTENT.CONID " +
            //"ORDER BY P.PRDNAME");




            return q;
        }
        public async Task<List<TotalSummary>> ClosingStockCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT C.CATNAME AS Catgry,C.CATID AS CatgryId, CASE WHEN @TYP='MRP'  THEN  ISNULL(SUM(STOCK.QTY*PLMRP),0)  " +
                                                      "             WHEN @TYP='COST' THEN  ISNULL(SUM(STOCK.QTY*PLPUCOST),0)  " +
                                                      "             WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0) )" +
                                                      "             ELSE 0 END AS StkValue" +
                                                      "  FROM PRODUCTS  " +
                                                      "       INNER JOIN CATEGORY C ON PRODUCTS.PRDCATID = C.CATID " +
                                                      "       INNER JOIN PRDBATCH ON PRODUCTS.PRDID =PRDBATCH.BCPRDID    " +
                                                      "       INNER JOIN PRDPRICELIST ON PRODUCTS.PRDID =PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID=PRDBATCH.BCID " +
                                                      "       LEFT JOIN  (SELECT TRDETAILS.DETPRDID,TRDETAILS.DETBCID ,ISNULL(SUM(DETQTY),0) AS QTY   " +
                                                      "                   FROM TRDETAILS      " +
                                                      "                        INNER JOIN PRODUCTS ON PRODUCTS.PRDID=TRDETAILS.DETPRDID   " +
                                                      "                        INNER JOIN PRDBATCH ON TRDETAILS.DETBCID=PRDBATCH.BCID     " +
                                                      "                   WHERE (TRDETAILS.DETDATE <=@TO or TRDETAILS.DETFLAG='OP') " +
                                                      "                   AND (TRDETAILS.DETCOUNTERID IN (" + Branchs + "))     " +
                                                      "                   GROUP BY TRDETAILS.DETPRDID,DETBCID) AS STOCK  " +
                                                      "       ON PRODUCTS.PRDID=STOCK.DETPRDID  AND PRDBATCH.BCID=STOCK.DETBCID   " +
                                                      "       WHERE  PRODUCTS.PRDTYPE=0 " +
                                                      " GROUP BY C.CATNAME,C.CATID ORDER BY C.CATNAME ", new { FROM = Startdate, TO = Enddate, TYP = Type });

                totalSummaryList = q.ToList();
            }
            else
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(@"SELECT 
                            C.CATNAME AS Catgry,
                            C.CATID AS CatgryId,
                            CASE 
                                WHEN @TYP='MRP' THEN ISNULL(SUM(STOCK.QTY*PLMRP),0)
                                WHEN @TYP='COST' THEN ISNULL(SUM(STOCK.QTY*PLPUCOST),0)
                                WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0))
                                ELSE 0 
                            END AS StkValue
                        FROM 
                            PRODUCTS  
                        INNER JOIN 
                            CATEGORY C ON PRODUCTS.PRDCATID = C.CATID 
                        INNER JOIN 
                            PRDBATCH ON PRODUCTS.PRDID = PRDBATCH.BCPRDID    
                        INNER JOIN 
                            PRDPRICELIST ON PRODUCTS.PRDID = PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID = PRDBATCH.BCID 
                        LEFT JOIN  
                            (SELECT 
                                 TRDETAILS.DETPRDID,
                                 TRDETAILS.DETBCID,
                                 ISNULL(SUM(DETQTY),0) AS QTY   
                             FROM 
                                 TRDETAILS      
                             INNER JOIN 
                                 PRODUCTS ON PRODUCTS.PRDID = TRDETAILS.DETPRDID   
                             INNER JOIN 
                                 PRDBATCH ON TRDETAILS.DETBCID = PRDBATCH.BCID     
                             WHERE 
                                 (TRDETAILS.DETDATE <= @TO or TRDETAILS.DETFLAG='OP') 
                             GROUP BY 
                                 TRDETAILS.DETPRDID,
                                 DETBCID
                            ) AS STOCK ON PRODUCTS.PRDID = STOCK.DETPRDID  AND PRDBATCH.BCID = STOCK.DETBCID   
                        WHERE  
                            PRODUCTS.PRDTYPE = 0 
                        GROUP BY 
                            C.CATNAME,
                            C.CATID 
                        ORDER BY 
                            C.CATNAME
                        ", new { FROM = Startdate, TO = Enddate, TYP = Type });

                totalSummaryList = q.ToList();
            }





            return totalSummaryList;
        }
        public async Task<List<TotalSummary>> ClosingStockSubCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT  C.SCATNAME AS SbCatgry,C.SCATPARENT AS CatgryId,CASE WHEN @TYP='MRP'  THEN  ISNULL(SUM(STOCK.QTY*PLMRP),0) " +
                                                      "             WHEN @TYP='COST' THEN  ISNULL(SUM(STOCK.QTY*PLPUCOST),0)  " +
                                                      "             WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0) )" +
                                                      "             ELSE 0 END AS StkValue " +
                                                      "  FROM PRODUCTS  " +
                                                      "       INNER JOIN SUBCATEGORY C ON PRODUCTS.PRDSUBCATID = C.SCATID " +
                                                      "       INNER JOIN PRDBATCH ON PRODUCTS.PRDID =PRDBATCH.BCPRDID    " +
                                                      "       INNER JOIN PRDPRICELIST ON PRODUCTS.PRDID =PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID=PRDBATCH.BCID " +
                                                      "       LEFT JOIN  (SELECT TRDETAILS.DETPRDID,TRDETAILS.DETBCID ,ISNULL(SUM(DETQTY),0) AS QTY   " +
                                                      "                   FROM TRDETAILS      " +
                                                      "                        INNER JOIN PRODUCTS ON PRODUCTS.PRDID=TRDETAILS.DETPRDID   " +
                                                      "                        INNER JOIN PRDBATCH ON TRDETAILS.DETBCID=PRDBATCH.BCID     " +
                                                      "                   WHERE (TRDETAILS.DETDATE <=@TO or TRDETAILS.DETFLAG='OP') " +
                                                      "                   AND (TRDETAILS.DETCOUNTERID IN (" + Branchs + "))     " +
                                                      "                   GROUP BY TRDETAILS.DETPRDID,DETBCID) AS STOCK  " +
                                                      "       ON PRODUCTS.PRDID=STOCK.DETPRDID  AND PRDBATCH.BCID=STOCK.DETBCID   " +
                                                      "       WHERE  PRODUCTS.PRDTYPE=0 " +
                                                      " GROUP BY C.SCATNAME,C.SCATPARENT ORDER BY C.SCATNAME ", new { FROM = Startdate, TO = Enddate, TYP = Type });
                totalSummaryList = q.ToList();
            }
            else
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(@"SELECT
                                    C.SCATNAME AS SbCatgry,
                                    C.SCATPARENT AS CatgryId,
                                    CASE
                                        WHEN @TYP='MRP' THEN ISNULL(SUM(STOCK.QTY * PLMRP), 0)
                                        WHEN @TYP='COST' THEN ISNULL(SUM(STOCK.QTY * PLPUCOST), 0)
                                        WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY * PLPURATE), 0) + ISNULL(SUM(STOCK.QTY * PLPUCOSTEXCLTAX), 0))
                                        ELSE 0
                                    END AS StkValue
                                FROM
                                    PRODUCTS
                                    INNER JOIN SUBCATEGORY C ON PRODUCTS.PRDSUBCATID = C.SCATID
                                    INNER JOIN PRDBATCH ON PRODUCTS.PRDID = PRDBATCH.BCPRDID
                                    INNER JOIN PRDPRICELIST ON PRODUCTS.PRDID = PRDPRICELIST.PLPRDID AND PRDPRICELIST.PLBCID = PRDBATCH.BCID
                                    LEFT JOIN (
                                        SELECT
                                            TRDETAILS.DETPRDID,
                                            TRDETAILS.DETBCID,
                                            ISNULL(SUM(DETQTY), 0) AS QTY
                                        FROM
                                            TRDETAILS
                                            INNER JOIN PRODUCTS ON PRODUCTS.PRDID = TRDETAILS.DETPRDID
                                            INNER JOIN PRDBATCH ON TRDETAILS.DETBCID = PRDBATCH.BCID
                                        WHERE
                                            (TRDETAILS.DETDATE <= @TO OR TRDETAILS.DETFLAG='OP')
                                        GROUP BY
                                            TRDETAILS.DETPRDID,
                                            DETBCID
                                    ) AS STOCK ON PRODUCTS.PRDID = STOCK.DETPRDID AND PRDBATCH.BCID = STOCK.DETBCID
                                WHERE
                                    PRODUCTS.PRDTYPE = 0
                                GROUP BY
                                    C.SCATNAME,
                                    C.SCATPARENT
                                ORDER BY
                                    C.SCATNAME;
                                ", new { FROM = Startdate, TO = Enddate, TYP = Type });
                totalSummaryList = q.ToList();

            }


            return totalSummaryList;
        }
        public async Task<List<TotalSummary>> ClosingStockMFR(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT M.MNFRNAME MFR, CASE WHEN @TYP='MRP'  THEN  ISNULL(SUM(STOCK.QTY*PLMRP),0)  " +
                                                     "             WHEN @TYP='COST' THEN  ISNULL(SUM(STOCK.QTY*PLPUCOST),0)  " +
                                                     "             WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0) )" +
                                                     "             ELSE 0 END AS StkValue" +
                                                     "  FROM PRODUCTS  " +
                                                     "       INNER JOIN MANUFACTURER  M ON PRODUCTS.PRDMFRID =M.MNFRID  " +
                                                     "       INNER JOIN PRDBATCH ON PRODUCTS.PRDID =PRDBATCH.BCPRDID    " +
                                                     "       INNER JOIN PRDPRICELIST ON PRODUCTS.PRDID =PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID=PRDBATCH.BCID " +
                                                     "       LEFT JOIN  (SELECT TRDETAILS.DETPRDID,TRDETAILS.DETBCID ,ISNULL(SUM(DETQTY),0) AS QTY   " +
                                                     "                   FROM TRDETAILS      " +
                                                     "                        INNER JOIN PRODUCTS ON PRODUCTS.PRDID=TRDETAILS.DETPRDID   " +
                                                     "                        INNER JOIN PRDBATCH ON TRDETAILS.DETBCID=PRDBATCH.BCID     " +
                                                     "                   WHERE (TRDETAILS.DETDATE <=@TO or TRDETAILS.DETFLAG='OP') " +
                                                     "                   AND (TRDETAILS.DETCOUNTERID IN (" + Branchs + "))     " +
                                                     "                   GROUP BY TRDETAILS.DETPRDID,DETBCID) AS STOCK  " +
                                                     "       ON PRODUCTS.PRDID=STOCK.DETPRDID  AND PRDBATCH.BCID=STOCK.DETBCID   " +
                                                     "       WHERE  PRODUCTS.PRDTYPE=0 " +
                                                     " GROUP BY M.MNFRNAME ORDER BY  M.MNFRNAME  ", new { FROM = Startdate, TO = Enddate, TYP = Type });
                totalSummaryList = q.ToList();
            }
            else
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(@"SELECT 
                            M.MNFRNAME AS MFR, 
                            CASE 
                                WHEN @TYP='MRP' THEN ISNULL(SUM(STOCK.QTY*PLMRP),0)
                                WHEN @TYP='COST' THEN ISNULL(SUM(STOCK.QTY*PLPUCOST),0)
                                WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0))
                                ELSE 0 
                            END AS StkValue
                        FROM 
                            PRODUCTS  
                        INNER JOIN 
                            MANUFACTURER M ON PRODUCTS.PRDMFRID = M.MNFRID  
                        INNER JOIN 
                            PRDBATCH ON PRODUCTS.PRDID = PRDBATCH.BCPRDID    
                        INNER JOIN 
                            PRDPRICELIST ON PRODUCTS.PRDID = PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID = PRDBATCH.BCID 
                        LEFT JOIN  
                            (SELECT 
                                 TRDETAILS.DETPRDID,
                                 TRDETAILS.DETBCID,
                                 ISNULL(SUM(DETQTY),0) AS QTY   
                             FROM 
                                 TRDETAILS      
                             INNER JOIN 
                                 PRODUCTS ON PRODUCTS.PRDID = TRDETAILS.DETPRDID   
                             INNER JOIN 
                                 PRDBATCH ON TRDETAILS.DETBCID = PRDBATCH.BCID     
                             WHERE 
                                 (TRDETAILS.DETDATE <= @TO or TRDETAILS.DETFLAG='OP') 
                             GROUP BY 
                                 TRDETAILS.DETPRDID,
                                 DETBCID
                            ) AS STOCK ON PRODUCTS.PRDID = STOCK.DETPRDID  AND PRDBATCH.BCID = STOCK.DETBCID   
                        WHERE  
                            PRODUCTS.PRDTYPE = 0 
                        GROUP BY 
                            M.MNFRNAME
                        ORDER BY  
                            M.MNFRNAME
                        ", new { FROM = Startdate, TO = Enddate, TYP = Type });
                totalSummaryList = q.ToList();
            }



            return totalSummaryList;
        }
        public async Task<List<TotalSummary>> ClosingStockSupplier(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(" SELECT A.ACACNAME Supplier, CASE WHEN @TYP='MRP'  THEN  ISNULL(SUM(STOCK.QTY*PLMRP),0)  " +
                                                  "             WHEN @TYP='COST' THEN  ISNULL(SUM(STOCK.QTY*PLPUCOST),0)  " +
                                                  "             WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0) )" +
                                                  "             ELSE 0 END AS StkValue" +
                                                  "  FROM PRODUCTS  " +
                                                  "       INNER JOIN ACMASTER A ON PRODUCTS.PRDSUPPID= A.ACID  " +
                                                  "       INNER JOIN PRDBATCH ON PRODUCTS.PRDID =PRDBATCH.BCPRDID    " +
                                                  "       INNER JOIN PRDPRICELIST ON PRODUCTS.PRDID =PRDPRICELIST.PLPRDID  AND PRDPRICELIST.PLBCID=PRDBATCH.BCID " +
                                                  "       LEFT JOIN  (SELECT TRDETAILS.DETPRDID,TRDETAILS.DETBCID ,ISNULL(SUM(DETQTY),0) AS QTY   " +
                                                  "                   FROM TRDETAILS      " +
                                                  "                        INNER JOIN PRODUCTS ON PRODUCTS.PRDID=TRDETAILS.DETPRDID   " +
                                                  "                        INNER JOIN PRDBATCH ON TRDETAILS.DETBCID=PRDBATCH.BCID     " +
                                                  "                   WHERE (TRDETAILS.DETDATE <=@TO or TRDETAILS.DETFLAG='OP') " +
                                                  "                   AND (TRDETAILS.DETCOUNTERID IN (" + Branchs + "))     " +
                                                  "                   GROUP BY TRDETAILS.DETPRDID,DETBCID) AS STOCK  " +
                                                  "       ON PRODUCTS.PRDID=STOCK.DETPRDID  AND PRDBATCH.BCID=STOCK.DETBCID   " +
                                                  "       WHERE  PRODUCTS.PRDTYPE=0 " +
                                                  " GROUP BY A.ACACNAME ORDER BY A.ACACNAME  ", new { FROM = Startdate, TO = Enddate, TYP = Type });

                totalSummaryList = q.ToList();
            }
            else
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>(@"SELECT 
                            A.ACACNAME AS Supplier, 
                            CASE 
                                WHEN @TYP='MRP' THEN ISNULL(SUM(STOCK.QTY*PLMRP),0)
                                WHEN @TYP='COST' THEN ISNULL(SUM(STOCK.QTY*PLPUCOST),0)
                                WHEN @TYP='COSTGST' THEN (ISNULL(SUM(STOCK.QTY*PLPURATE),0) + ISNULL(SUM(STOCK.QTY*PLPUCOSTEXCLTAX),0))
                                ELSE 0 
                            END AS StkValue
                        FROM 
                            PRODUCTS  
                        INNER JOIN 
                            ACMASTER A ON PRODUCTS.PRDSUPPID = A.ACID  
                        INNER JOIN 
                            PRDBATCH ON PRODUCTS.PRDID = PRDBATCH.BCPRDID    
                        INNER JOIN 
                            PRDPRICELIST ON PRODUCTS.PRDID = PRDPRICELIST.PLPRDID AND PRDPRICELIST.PLBCID = PRDBATCH.BCID 
                        LEFT JOIN  
                            (SELECT 
                                 TRDETAILS.DETPRDID,
                                 TRDETAILS.DETBCID,
                                 ISNULL(SUM(DETQTY),0) AS QTY   
                             FROM 
                                 TRDETAILS      
                             INNER JOIN 
                                 PRODUCTS ON PRODUCTS.PRDID = TRDETAILS.DETPRDID   
                             INNER JOIN 
                                 PRDBATCH ON TRDETAILS.DETBCID = PRDBATCH.BCID     
                             WHERE 
                                 (TRDETAILS.DETDATE <= @TO OR TRDETAILS.DETFLAG = 'OP') 
                             GROUP BY 
                                 TRDETAILS.DETPRDID,
                                 DETBCID
                            ) AS STOCK ON PRODUCTS.PRDID = STOCK.DETPRDID AND PRDBATCH.BCID = STOCK.DETBCID   
                        WHERE  
                            PRODUCTS.PRDTYPE = 0 
                        GROUP BY 
                            A.ACACNAME 
                        ORDER BY 
                            A.ACACNAME
                        ", new { FROM = Startdate, TO = Enddate, TYP = Type });

                totalSummaryList = q.ToList();
            }


            return totalSummaryList;
        }


        #endregion ClosingStock

        #region BankAndCash

        //Total Value of bank and cash
        public async Task<BankandCash> BankandCash(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            BankandCash q = new();
            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "CASH")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(" SELECT ISNULL(SUM(DB.BAL),0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "  			    WHERE DBCOUNTERID IN (" + Branchs + ")  AND  DBDATE <= @TO GROUP BY  DBACID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "PRECASH")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(" SELECT ISNULL(SUM(DB.BAL),0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "  			    WHERE DBCOUNTERID IN (" + Branchs + ")  AND  DBDATE BETWEEN @FROM AND @TO GROUP BY  DBACID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "BANK")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(" SELECT ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "  			    WHERE DBCOUNTERID IN (" + Branchs + ")  AND  DBDATE <= @TO GROUP BY  DBACID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='BANK'   AND ISNULL(DB.BAL ,0)<>0 ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "DEBTORS")
                {
                    //q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>("  SELECT SUM(DBDEBIT)  AS Balance  " +
                    //        "              FROM ACMASTER  " +
                    //        "              INNER JOIN (SELECT  DAYBOOK.DBACID, " +
                    //        "              CASE WHEN SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT )>0  " +
                    //        "              THEN SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) ELSE 0 END AS DBDEBIT " +
                    //        "              FROM DAYBOOK " +
                    //        "              WHERE DAYBOOK.DBCOUNTERID IN (" + Branchs + ")  AND DAYBOOK.DBDATE <= @TO " +
                    //        "              GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID " +
                    //        "              WHERE ACTYPE<>0 AND DBDEBIT>0 AND DBDEBIT>ACMASTER.ACCREDITLIMIT  ", new { FROM = Startdate, TO = Enddate });


                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(@" SELECT SUM(DBDEBIT)  AS Balance
                                       FROM ACMASTER
                                       INNER JOIN (SELECT  DAYBOOK.DBACID,
                                       SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) AS DBDEBIT 
                                       FROM DAYBOOK 
                                       WHERE DAYBOOK.DBCOUNTERID IN (" + Branchs + @")  AND DAYBOOK.DBDATE <= @TO
                                       GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID
                                       WHERE ACTYPE<>0 AND ACSCHEDULE=28269503", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "CREDITORS")
                {
                    //   q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>("SELECT " +
                    //" CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) < 0  " +
                    //" THEN SUM(abs(BILLWISE.BWDEBIT-billwise.BWCREDIT)) ELSE 0 END AS  Credit, " +
                    //" CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) > 0" +
                    //" THEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) ELSE 0 END AS  Debit, " +
                    //" SUM(ABS(ISNULL(RCVD.BWAMOUNT, 0))) AS   RecievedAmt " +
                    //" FROM ACMASTER" +
                    //" INNER JOIN BILLWISE ON ACMASTER.ACID = BILLWISE.BWACCOUNT" +
                    //" LEFT JOIN TRANTYPE ON BILLWISE.BWVCHTYPE = TRANTYPE.TRFLAG" +
                    //" LEFT JOIN(SELECT BILLWISE.BWBILLID, SUM(BILLWISE.BWDEBIT-billwise.BWCREDIT) AS BWAMOUNT FROM BILLWISE" +
                    //" WHERE(BWISBILL = 0 And BWBILLID > 0)" +
                    // " GROUP BY BILLWISE.BWBILLID ) AS RCVD " +
                    // " ON BILLWISE.VID = RCVD.BWBILLID AND BWCOUNTERID IN (" + Branchs + ") " +
                    //" WHERE BILLWISE.BWISBILL = 1 " +
                    //" AND BILLWISE.BWBILLID > 0 AND ACTYPE = 2  AND(BILLWISE.BWDEBIT - billwise.BWCREDIT) + ISNULL(RCVD.BWAMOUNT, 0) <> 0" +
                    //" AND billwise.BWBILLNO<>'ON ACCOUNT'" +
                    //" AND BILLWISE.BWDATE <= @TO AND BWCOUNTERID IN (" + Branchs + ") AND ACISBILLWISEACC = 1 ", new { FROM = Startdate, TO = Enddate });

                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(@" SELECT SUM(DBDEBIT)  AS Balance
                                       FROM ACMASTER
                                       INNER JOIN (SELECT  DAYBOOK.DBACID,
                                       SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) AS DBDEBIT 
                                       FROM DAYBOOK 
                                       WHERE DAYBOOK.DBCOUNTERID IN (" + Branchs + @")  AND DAYBOOK.DBDATE <= @TO
                                       GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID
                                       WHERE ACTYPE<>0 AND ACSCHEDULE=28269504", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "DEPOSIT")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(@" SELECT ISNULL(SUM(CASHDEPOSIT.CDAMOUNT),0) AS Balance
                     FROM  CASHDEPOSIT
                    WHERE CASHDEPOSIT.CDCOUNTERID IN (" + Branchs + @")  AND   CASHDEPOSIT.CDDATE BETWEEN @FROM AND @TO GROUP BY  CASHDEPOSIT.CDCOUNTERID ", new { FROM = Startdate, TO = Enddate });
                }
            }
            else
            {
                if (Type.ToUpper() == "CASH")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(" SELECT ISNULL(SUM(DB.BAL),0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "  			    WHERE  DBDATE <= @TO GROUP BY  DBACID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "PRECASH")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(" SELECT ISNULL(SUM(DB.BAL),0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "  			    WHERE DBDATE BETWEEN @FROM AND @TO GROUP BY  DBACID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "BANK")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(" SELECT ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "  			    WHERE  DBDATE <= @TO GROUP BY  DBACID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='BANK'   AND ISNULL(DB.BAL ,0)<>0 ", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "DEBTORS")
                {
                    //q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>("  SELECT SUM(DBDEBIT)  AS Balance  " +
                    //        "              FROM ACMASTER  " +
                    //        "              INNER JOIN (SELECT  DAYBOOK.DBACID, " +
                    //        "              CASE WHEN SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT )>0  " +
                    //        "              THEN SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) ELSE 0 END AS DBDEBIT " +
                    //        "              FROM DAYBOOK " +
                    //        "              WHERE DAYBOOK.DBDATE <= @TO " +
                    //        "              GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID " +
                    //        "              WHERE ACTYPE<>0 AND DBDEBIT>0 AND DBDEBIT>ACMASTER.ACCREDITLIMIT  ", new { FROM = Startdate, TO = Enddate });

                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(@" SELECT SUM(DBDEBIT)  AS Balance
                                       FROM ACMASTER
                                       INNER JOIN (SELECT  DAYBOOK.DBACID,
                                       SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) AS DBDEBIT 
                                       FROM DAYBOOK 
                                       WHERE DAYBOOK.DBDATE <= @TO
                                       GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID
                                       WHERE ACTYPE<>0 AND ACSCHEDULE=28269503", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "CREDITORS")
                {
                    //      q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>("SELECT " +
                    //   " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) < 0  " +
                    //   " THEN SUM(abs(BILLWISE.BWDEBIT-billwise.BWCREDIT)) ELSE 0 END AS  Credit, " +
                    //   " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) > 0" +
                    //   " THEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) ELSE 0 END AS  Debit, " +
                    //   " SUM(ABS(ISNULL(RCVD.BWAMOUNT, 0))) AS   RecievedAmt " +
                    //" FROM ACMASTER" +
                    //   " INNER JOIN BILLWISE ON ACMASTER.ACID = BILLWISE.BWACCOUNT" +
                    //   " LEFT JOIN TRANTYPE ON BILLWISE.BWVCHTYPE = TRANTYPE.TRFLAG" +
                    //   " LEFT JOIN(SELECT BILLWISE.BWBILLID, SUM(BILLWISE.BWDEBIT-billwise.BWCREDIT) AS BWAMOUNT FROM BILLWISE" +
                    //   " WHERE(BWISBILL = 0 And BWBILLID > 0)" +
                    //    " GROUP BY BILLWISE.BWBILLID ) AS RCVD " +
                    //    " ON BILLWISE.VID = RCVD.BWBILLID " +
                    //   " WHERE BILLWISE.BWISBILL = 1 " +
                    //   " AND BILLWISE.BWBILLID > 0 AND ACTYPE = 2  AND(BILLWISE.BWDEBIT - billwise.BWCREDIT) + ISNULL(RCVD.BWAMOUNT, 0) <> 0" +
                    //   " AND billwise.BWBILLNO<>'ON ACCOUNT'" +
                    //   " AND BILLWISE.BWDATE <= @TO AND ACISBILLWISEACC = 1 ", new { FROM = Startdate, TO = Enddate });

                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(@" SELECT SUM(DBDEBIT)  AS Balance
                                       FROM ACMASTER
                                       INNER JOIN (SELECT  DAYBOOK.DBACID,
                                       SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) AS DBDEBIT 
                                       FROM DAYBOOK 
                                       WHERE DAYBOOK.DBDATE <= @TO
                                       GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID
                                       WHERE ACTYPE<>0 AND ACSCHEDULE=28269504", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "DEPOSIT")
                {
                    q = (BankandCash)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<BankandCash>(@" SELECT ISNULL(SUM(CASHDEPOSIT.CDAMOUNT),0) AS Balance
                     FROM  CASHDEPOSIT
                    WHERE CASHDEPOSIT.CDDATE BETWEEN @FROM AND @TO GROUP BY  CASHDEPOSIT.CDCOUNTERID ", new { FROM = Startdate, TO = Enddate });
                }

            }


            return q;

        }

        //Value Accordiong to bank and cash branchwi8se
        public async Task<List<BankandCash>> BranchBankandCash(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<BankandCash> totalSummaryList = new List<BankandCash>();
            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "CASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT DB.CNTNAME Branch,DB.CNTID Branchid,ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT C.CNTNAME, C.CNTID,DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "                INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                           "  			    WHERE DBCOUNTERID IN (" + Branchs + ")  AND  DBDATE <= @TO GROUP BY  DBACID,C.CNTNAME,C.CNTID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 GROUP BY  DB.CNTNAME,DB.CNTID ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "PRECASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT DB.CNTNAME Branch,DB.CNTID Branchid,ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT C.CNTNAME, C.CNTID,DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "                INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                           "  			    WHERE DBCOUNTERID IN (" + Branchs + ")  AND  DBDATE BETWEEN @FROM AND @TO GROUP BY  DBACID,C.CNTNAME,C.CNTID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 GROUP BY  DB.CNTNAME,DB.CNTID ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "BANK")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT DB.CNTNAME Branch,DB.CNTID Branchid,ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                        "  FROM ACMASTER " +
                            "  	LEFT JOIN  (SELECT C.CNTNAME, C.CNTID,DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                            "  			    FROM DAYBOOK  " +
                            "                INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                            "  			    WHERE DBCOUNTERID IN (" + Branchs + ")  AND  DBDATE <= @TO  GROUP BY  DBACID,C.CNTNAME,C.CNTID) AS DB  " +
                            "                ON ACMASTER.ACID = DB.DBACID     " +
                            "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                            "    WHERE SCHEDULES.SHNATURE='BANK'   AND ISNULL(DB.BAL ,0)<>0  GROUP BY  DB.CNTNAME,DB.CNTID ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "DEBTORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("  SELECT DB.CNTNAME Branch,DB.CNTID Branchid,SUM(DBDEBIT)  AS Balance  " +
                             "              FROM ACMASTER  " +
                             "              INNER JOIN (SELECT   C.CNTNAME, C.CNTID,DAYBOOK.DBACID, " +
                             "              CASE WHEN SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT )>0  " +
                             "              THEN SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) ELSE 0 END AS DBDEBIT " +
                             "              FROM DAYBOOK " +
                             "              INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                             "              WHERE DAYBOOK.DBCOUNTERID IN (" + Branchs + ")  AND DAYBOOK.DBDATE <= @TO " +
                             "              GROUP BY DAYBOOK.DBACID,C.CNTNAME,C.CNTID )AS DB ON ACMASTER.ACID=DB.DBACID " +
                             "              WHERE ACTYPE<>0 AND DBDEBIT>0 AND DBDEBIT>ACMASTER.ACCREDITLIMIT   GROUP BY  DB.CNTNAME,DB.CNTID  ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "CREDITORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("SELECT COUNTER.CNTNAME Branch,COUNTER.CNTID Branchid, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) < 0  " +
                  " THEN SUM(abs(BILLWISE.BWDEBIT-billwise.BWCREDIT)) ELSE 0 END AS  Credit, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) > 0" +
                  " THEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) ELSE 0 END AS  Debit, " +
                  " SUM(ABS(ISNULL(RCVD.BWAMOUNT, 0))) AS   RecievedAmt " +
               " FROM ACMASTER" +
                  " INNER JOIN BILLWISE ON ACMASTER.ACID = BILLWISE.BWACCOUNT" +
                  " LEFT JOIN TRANTYPE ON BILLWISE.BWVCHTYPE = TRANTYPE.TRFLAG" +
                  " INNER JOIN COUNTER ON BILLWISE.BWCOUNTERID =COUNTER.CNTID  " +
                  " LEFT JOIN(SELECT BILLWISE.BWBILLID, SUM(BILLWISE.BWDEBIT-billwise.BWCREDIT) AS BWAMOUNT FROM BILLWISE" +
                  " WHERE(BWISBILL = 0 And BWBILLID > 0)" +
                   " GROUP BY BILLWISE.BWBILLID ) AS RCVD " +
                   " ON BILLWISE.VID = RCVD.BWBILLID AND BWCOUNTERID IN (" + Branchs + ") " +
                  " WHERE BILLWISE.BWISBILL = 1 " +
                  " AND BILLWISE.BWBILLID > 0 AND ACTYPE = 2  AND(BILLWISE.BWDEBIT - billwise.BWCREDIT) + ISNULL(RCVD.BWAMOUNT, 0) <> 0" +
                  " AND billwise.BWBILLNO<>'ON ACCOUNT'" +
                  " AND BILLWISE.BWDATE <= @TO AND BWCOUNTERID IN (" + Branchs + ") AND ACISBILLWISEACC = 1 " +
                  " GROUP BY COUNTER.CNTNAME,COUNTER.CNTID", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "DEPOSIT")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(@"
                                    SELECT ISNULL(SUM(CD.CDAMOUNT), 0) AS Balance, CD.CDCOUNTERID AS BranchId, C.CNTNAME  AS Branch
                                    FROM CASHDEPOSIT AS CD
                                    INNER JOIN COUNTER C ON CD.CDCOUNTERID = C.CNTID
                                    WHERE CD.CDCOUNTERID IN (" + Branchs + @") AND CD.CDDATE BETWEEN @FROM AND @TO 
                                    GROUP BY CD.CDCOUNTERID, C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "CASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT DB.CNTNAME Branch,DB.CNTID Branchid,ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT C.CNTNAME, C.CNTID,DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "                INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                           "  			    WHERE  DBDATE <= @TO GROUP BY  DBACID,C.CNTNAME,C.CNTID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 GROUP BY  DB.CNTNAME,DB.CNTID ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "PRECASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT DB.CNTNAME Branch,DB.CNTID Branchid,ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                       "  FROM ACMASTER " +
                           "  	LEFT JOIN  (SELECT C.CNTNAME, C.CNTID,DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                           "  			    FROM DAYBOOK  " +
                           "                INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                           "  			    WHERE DBDATE BETWEEN @FROM AND @TO GROUP BY  DBACID,C.CNTNAME,C.CNTID) AS DB  " +
                           "                ON ACMASTER.ACID = DB.DBACID     " +
                           "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                           "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 GROUP BY  DB.CNTNAME,DB.CNTID ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }

                else if (Type.ToUpper() == "BANK")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT DB.CNTNAME Branch,DB.CNTID Branchid,ISNULL(SUM(DB.BAL) ,0) AS Balance  " +
                        "  FROM ACMASTER " +
                            "  	LEFT JOIN  (SELECT C.CNTNAME, C.CNTID,DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                            "  			    FROM DAYBOOK  " +
                            "                INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                            "  			    WHERE  DBDATE <= @TO  GROUP BY  DBACID,C.CNTNAME,C.CNTID) AS DB  " +
                            "                ON ACMASTER.ACID = DB.DBACID     " +
                            "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                            "    WHERE SCHEDULES.SHNATURE='BANK'   AND ISNULL(DB.BAL ,0)<>0  GROUP BY  DB.CNTNAME,DB.CNTID ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "DEBTORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("  SELECT DB.CNTNAME Branch,DB.CNTID Branchid,SUM(DBDEBIT)  AS Balance  " +
                             "              FROM ACMASTER  " +
                             "              INNER JOIN (SELECT   C.CNTNAME, C.CNTID,DAYBOOK.DBACID, " +
                             "              CASE WHEN SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT )>0  " +
                             "              THEN SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) ELSE 0 END AS DBDEBIT " +
                             "              FROM DAYBOOK " +
                             "              INNER JOIN COUNTER  C ON DBCOUNTERID = C.CNTID" +
                             "              WHERE DAYBOOK.DBDATE <= @TO " +
                             "              GROUP BY DAYBOOK.DBACID,C.CNTNAME,C.CNTID )AS DB ON ACMASTER.ACID=DB.DBACID " +
                             "              WHERE ACTYPE<>0 AND DBDEBIT>0 AND DBDEBIT>ACMASTER.ACCREDITLIMIT   GROUP BY  DB.CNTNAME,DB.CNTID  ORDER BY DB.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "CREDITORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("SELECT COUNTER.CNTNAME Branch,COUNTER.CNTID Branchid, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) < 0  " +
                  " THEN SUM(abs(BILLWISE.BWDEBIT-billwise.BWCREDIT)) ELSE 0 END AS  Credit, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) > 0" +
                  " THEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) ELSE 0 END AS  Debit, " +
                  " SUM(ABS(ISNULL(RCVD.BWAMOUNT, 0))) AS   RecievedAmt " +
               " FROM ACMASTER" +
                  " INNER JOIN BILLWISE ON ACMASTER.ACID = BILLWISE.BWACCOUNT" +
                  " LEFT JOIN TRANTYPE ON BILLWISE.BWVCHTYPE = TRANTYPE.TRFLAG" +
                  " INNER JOIN COUNTER ON BILLWISE.BWCOUNTERID =COUNTER.CNTID  " +
                  " LEFT JOIN(SELECT BILLWISE.BWBILLID, SUM(BILLWISE.BWDEBIT-billwise.BWCREDIT) AS BWAMOUNT FROM BILLWISE" +
                  " WHERE(BWISBILL = 0 And BWBILLID > 0)" +
                   " GROUP BY BILLWISE.BWBILLID ) AS RCVD " +
                   " ON BILLWISE.VID = RCVD.BWBILLID  " +
                  " WHERE BILLWISE.BWISBILL = 1 " +
                  " AND BILLWISE.BWBILLID > 0 AND ACTYPE = 2  AND(BILLWISE.BWDEBIT - billwise.BWCREDIT) + ISNULL(RCVD.BWAMOUNT, 0) <> 0" +
                  " AND billwise.BWBILLNO<>'ON ACCOUNT'" +
                  " AND BILLWISE.BWDATE <= @TO AND ACISBILLWISEACC = 1 " +
                  " GROUP BY COUNTER.CNTNAME,COUNTER.CNTID", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
                else if (Type.ToUpper() == "DEPOSIT")
                {


                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(@"
                                    SELECT ISNULL(SUM(CD.CDAMOUNT), 0) AS Balance, CD.CDCOUNTERID AS BranchId, C.CNTNAME  AS Branch
                                    FROM CASHDEPOSIT AS CD
                                    INNER JOIN COUNTER C ON CD.CDCOUNTERID = C.CNTID
                                    WHERE CD.CDDATE BETWEEN @FROM AND @TO 
                                    GROUP BY CD.CDCOUNTERID, C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
            }


            return totalSummaryList;

        }

        //Bank Statement According to Bank wise need to pass bank parameters
        public async Task<List<BankandCash>> BankSummary(DateTimeOffset Startdate, DateTimeOffset Enddate, long Branches, string Type)
        {
            List<BankandCash> totalSummaryList = new List<BankandCash>();

            var Branchs = Branches;

            if (Branchs != 0)
            {
                if (Type.ToUpper() == "CASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT ACMASTER.ACACNAME Bank,ISNULL(DB.BAL ,0) AS Balance  " +
                           "  FROM ACMASTER " +
                               "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                               "  			    FROM DAYBOOK  " +
                               "  			    WHERE (DBCOUNTERID=@COUNTERID OR @COUNTERID<=0)  AND  DBDATE <= @TO  GROUP BY  DBACID) AS DB  " +
                               "                ON ACMASTER.ACID = DB.DBACID     " +
                               "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                               "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate, COUNTERID = Branches });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "PRECASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT ACMASTER.ACACNAME Bank,ISNULL(DB.BAL ,0) AS Balance  " +
                           "  FROM ACMASTER " +
                               "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                               "  			    FROM DAYBOOK  " +
                               "  			    WHERE (DBCOUNTERID=@COUNTERID OR @COUNTERID<=0)  AND  DBDATE BETWEEN @FROM AND @TO  GROUP BY  DBACID) AS DB  " +
                               "                ON ACMASTER.ACID = DB.DBACID     " +
                               "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                               "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate, COUNTERID = Branches });
                    totalSummaryList = q.ToList();

                }
                if (Type.ToUpper() == "BANK")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT ACMASTER.ACACNAME Bank,ISNULL(DB.BAL ,0) AS Balance  " +
                        "  FROM ACMASTER " +
                            "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                            "  			    FROM DAYBOOK  " +
                            "  			    WHERE (DBCOUNTERID=@COUNTERID OR @COUNTERID<=0)  AND  DBDATE <= @TO  GROUP BY  DBACID) AS DB  " +
                            "                ON ACMASTER.ACID = DB.DBACID     " +
                            "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                            "    WHERE SCHEDULES.SHNATURE='BANK'   AND ISNULL(DB.BAL ,0)<>0 ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate, COUNTERID = Branches });
                    totalSummaryList = q.ToList();

                }

                else if (Type.ToUpper() == "DEBTORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("  SELECT ACMASTER.ACACNAME Bank,DBDEBIT AS Balance  " +
                             "              FROM ACMASTER  " +
                             "              INNER JOIN (SELECT DAYBOOK.DBACID, " +
                             "              CASE WHEN SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT )>0  " +
                             "              THEN SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) ELSE 0 END AS DBDEBIT " +
                             "              FROM DAYBOOK " +
                             "              WHERE DAYBOOK.DBCOUNTERID IN (" + Branchs + ")  AND DAYBOOK.DBDATE <= @TO " +
                             "              GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID " +
                             "              WHERE ACTYPE<>0 AND DBDEBIT>0 AND DBDEBIT>ACMASTER.ACCREDITLIMIT   ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "CREDITORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("SELECT ACMASTER.ACACNAME Bank, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) < 0  " +
                  " THEN SUM(abs(BILLWISE.BWDEBIT-billwise.BWCREDIT)) ELSE 0 END AS  Credit, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) > 0" +
                  " THEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) ELSE 0 END AS  Debit, " +
                  " SUM(ABS(ISNULL(RCVD.BWAMOUNT, 0))) AS   RecievedAmt " +
               " FROM ACMASTER" +
                  " INNER JOIN BILLWISE ON ACMASTER.ACID = BILLWISE.BWACCOUNT" +
                  " LEFT JOIN TRANTYPE ON BILLWISE.BWVCHTYPE = TRANTYPE.TRFLAG" +
                  " LEFT JOIN(SELECT BILLWISE.BWBILLID, SUM(BILLWISE.BWDEBIT-billwise.BWCREDIT) AS BWAMOUNT FROM BILLWISE" +
                  " WHERE(BWISBILL = 0 And BWBILLID > 0)" +
                   " GROUP BY BILLWISE.BWBILLID ) AS RCVD " +
                   " ON BILLWISE.VID = RCVD.BWBILLID AND BWCOUNTERID IN (" + Branchs + ") " +
                  " WHERE BILLWISE.BWISBILL = 1 " +
                  " AND BILLWISE.BWBILLID > 0 AND ACTYPE = 2  AND(BILLWISE.BWDEBIT - billwise.BWCREDIT) + ISNULL(RCVD.BWAMOUNT, 0) <> 0" +
                  " AND billwise.BWBILLNO<>'ON ACCOUNT'" +
                  " AND BILLWISE.BWDATE <= @TO AND BWCOUNTERID IN (" + Branchs + ") AND ACISBILLWISEACC = 1 " +
                  " GROUP BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }
            else
            {
                if (Type.ToUpper() == "CASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT ACMASTER.ACACNAME Bank,ISNULL(DB.BAL ,0) AS Balance  " +
                           "  FROM ACMASTER " +
                               "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                               "  			    FROM DAYBOOK  " +
                               "  			    WHERE  DBDATE <= @TO  GROUP BY  DBACID) AS DB  " +
                               "                ON ACMASTER.ACID = DB.DBACID     " +
                               "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                               "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "PRECASH")
                {

                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT ACMASTER.ACACNAME Bank,ISNULL(DB.BAL ,0) AS Balance  " +
                           "  FROM ACMASTER " +
                               "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                               "  			    FROM DAYBOOK  " +
                               "  			    WHERE  DBDATE BETWEEN @FROM AND @TO  GROUP BY  DBACID) AS DB  " +
                               "                ON ACMASTER.ACID = DB.DBACID     " +
                               "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                               "    WHERE SCHEDULES.SHNATURE='CASH'   AND ISNULL(DB.BAL ,0)<>0 ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                if (Type.ToUpper() == "BANK")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>(" SELECT ACMASTER.ACACNAME Bank,ISNULL(DB.BAL ,0) AS Balance  " +
                        "  FROM ACMASTER " +
                            "  	LEFT JOIN  (SELECT DBACID, SUM(DBDEBIT - DBCREDIT) AS BAL  " +
                            "  			    FROM DAYBOOK  " +
                            "  			    WHERE DBDATE <= @TO  GROUP BY  DBACID) AS DB  " +
                            "                ON ACMASTER.ACID = DB.DBACID     " +
                            "    INNER JOIN SCHEDULES ON ACMASTER.ACSCHEDULE=SCHEDULES.SHID  " +
                            "    WHERE SCHEDULES.SHNATURE='BANK'   AND ISNULL(DB.BAL ,0)<>0 ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }

                else if (Type.ToUpper() == "DEBTORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("  SELECT ACMASTER.ACACNAME Bank,DBDEBIT AS Balance  " +
                             "              FROM ACMASTER  " +
                             "              INNER JOIN (SELECT DAYBOOK.DBACID, " +
                             "              CASE WHEN SUM(DAYBOOK.DBDEBIT - DAYBOOK.DBCREDIT )>0  " +
                             "              THEN SUM(DAYBOOK.DBDEBIT -DAYBOOK.DBCREDIT) ELSE 0 END AS DBDEBIT " +
                             "              FROM DAYBOOK " +
                             "              WHERE DAYBOOK.DBDATE <= @TO " +
                             "              GROUP BY DAYBOOK.DBACID )AS DB ON ACMASTER.ACID=DB.DBACID " +
                             "              WHERE ACTYPE<>0 AND DBDEBIT>0 AND DBDEBIT>ACMASTER.ACCREDITLIMIT   ORDER BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "CREDITORS")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("SELECT ACMASTER.ACACNAME Bank, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) < 0  " +
                  " THEN SUM(abs(BILLWISE.BWDEBIT-billwise.BWCREDIT)) ELSE 0 END AS  Credit, " +
                  " CASE WHEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) > 0" +
                  " THEN SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) ELSE 0 END AS  Debit, " +
                  " SUM(ABS(ISNULL(RCVD.BWAMOUNT, 0))) AS   RecievedAmt " +
               " FROM ACMASTER" +
                  " INNER JOIN BILLWISE ON ACMASTER.ACID = BILLWISE.BWACCOUNT" +
                  " LEFT JOIN TRANTYPE ON BILLWISE.BWVCHTYPE = TRANTYPE.TRFLAG" +
                  " LEFT JOIN(SELECT BILLWISE.BWBILLID, SUM(BILLWISE.BWDEBIT-billwise.BWCREDIT) AS BWAMOUNT FROM BILLWISE" +
                  " WHERE(BWISBILL = 0 And BWBILLID > 0)" +
                   " GROUP BY BILLWISE.BWBILLID ) AS RCVD " +
                   " ON BILLWISE.VID = RCVD.BWBILLID" +
                  " WHERE BILLWISE.BWISBILL = 1 " +
                  " AND BILLWISE.BWBILLID > 0 AND ACTYPE = 2  AND(BILLWISE.BWDEBIT - billwise.BWCREDIT) + ISNULL(RCVD.BWAMOUNT, 0) <> 0" +
                  " AND billwise.BWBILLNO<>'ON ACCOUNT'" +
                  " AND BILLWISE.BWDATE <= @TO  AND ACISBILLWISEACC = 1 " +
                  " GROUP BY ACMASTER.ACACNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();
                }
            }


            // var q = await this.dapperContext.CreateConnection().QueryAsync<BankandCash>("SELECT ACMASTER.ACACNAME Bank, " +
            //       " SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT) - ABS(ISNULL(SUM(RCVD.BWAMOUNT), 0)) AS Balance " +
            //       " FROM ACMASTER " +
            //        " INNER JOIN BILLWISE ON ACMASTER.ACID = BILLWISE.BWACCOUNT " +
            //        " LEFT JOIN TRANTYPE ON BILLWISE.BWVCHTYPE = TRANTYPE.TRFLAG " +
            //        " LEFT JOIN(SELECT BILLWISE.BWBILLID, SUM(BILLWISE.BWDEBIT-billwise.BWCREDIT) AS BWAMOUNT " +
            //                " FROM BILLWISE" +
            //                  " WHERE (BWISBILL = 0 And BWBILLID > 0) " +
            //                  " GROUP BY BILLWISE.BWBILLID ) AS RCVD "+
            //      " ON BILLWISE.VID = RCVD.BWBILLID AND BWCOUNTERID IN (" + Branchs + ")"+
            //      " WHERE    BILLWISE.BWISBILL = 1 AND BILLWISE.BWBILLID > 0 AND ACTYPE = 2  "+
            //      " AND (BILLWISE.BWDEBIT - billwise.BWCREDIT) + ISNULL(RCVD.BWAMOUNT, 0) <> 0 "+
            //      " AND billwise.BWBILLNO<>'ON ACCOUNT' AND BILLWISE.BWDATE <= @TO   AND BWCOUNTERID IN(" + Branchs + ")  AND ACISBILLWISEACC = 1"+
            //      " GROUP BY  ACMASTER.ACACNAME "+
            //" UNION ALL "+
            //    " SELECT ACMASTER.ACACNAME Bank, " +
            //    " ISNULL(SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT), 0) Amount "+
            //  " FROM BILLWISE "+
            //    " INNER JOIN ACMASTER ON Billwise.BWACCOUNT = ACMASTER.ACID"+
            //    " WHERE BILLWISE.BWBILLID = 0 AND BILLWISE.BWISBILL = 0 AND BILLWISE.BWDATE <= @TO "+
            //    " AND ACISBILLWISEACC = 1 AND BWCOUNTERID IN(2)  AND ACTYPE = 2"+
            //    " AND billwise.BWBILLNO = 'ON ACCOUNT'"+
            //    " GROUP BY ACMASTER.ACACNAME"+
            //    " HAVING ISNULL(SUM(BILLWISE.BWDEBIT - billwise.BWCREDIT), 0) <> 0", new { FROM = Startdate, TO = Enddate });

            return totalSummaryList;

        }


        #endregion BankAndCash


        #region Homedelivery


        public async Task<TotalSummary> Homedelivery(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            TotalSummary q = new();
            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "BILLED")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(VID) InvCount, ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                  " FROM dbo.SALES WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SAISHOMEDELIVERY = 1 AND " +
                  " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "DELIVERED")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                          " FROM dbo.SALES WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=1 AND " +
                          " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "PENDING")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                         " FROM dbo.SALES WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=0 AND " +
                         " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
            }
            else
            {
                if (Type.ToUpper() == "BILLED")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(VID) InvCount, ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                  " FROM dbo.SALES WHERE  SALES.SAISHOMEDELIVERY = 1 AND " +
                  " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });

                }
                else if (Type.ToUpper() == "DELIVERED")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                          " FROM dbo.SALES WHERE SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=1 AND " +
                          " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
                else if (Type.ToUpper() == "PENDING")
                {
                    q = (TotalSummary)await this.dapperContext.CreateConnection().QueryFirstOrDefaultAsync<TotalSummary>("SELECT COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                         " FROM dbo.SALES WHERE  SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=0 AND " +
                         " SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0", new { FROM = Startdate, TO = Enddate });
                }
            }



            return q;
        }

        public async Task<List<TotalSummary>> BranchHomedelivery(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type)
        {
            List<TotalSummary> totalSummaryList = new List<TotalSummary>();

            var Branchs = Branches;

            if (Branchs != "")
            {
                if (Type.ToUpper() == "BILLED")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CNTNAME Branch,COUNT(VID) InvCount, ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                  " FROM dbo.SALES " +
                  " INNER JOIN COUNTER  C ON SALES.SACOUNTERID = C.CNTID " +
                  " WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SAISHOMEDELIVERY = 1 " +
                  " AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "DELIVERED")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CNTNAME Branch,COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                            " FROM dbo.SALES " +
                            " INNER JOIN COUNTER  C ON SALES.SACOUNTERID = C.CNTID " +
                            " WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=1 " +
                            " AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "PENDING")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CNTNAME Branch,COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                           " FROM dbo.SALES " +
                           " INNER JOIN COUNTER  C ON SALES.SACOUNTERID = C.CNTID " +
                           " WHERE SALES.SACOUNTERID IN (" + Branchs + ") AND SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=0 " +
                           " AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
            }
            else
            {
                if (Type.ToUpper() == "BILLED")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CNTNAME Branch,COUNT(VID) InvCount, ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                  " FROM dbo.SALES " +
                  " INNER JOIN COUNTER  C ON SALES.SACOUNTERID = C.CNTID " +
                  " WHERE SALES.SAISHOMEDELIVERY = 1 " +
                  " AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "DELIVERED")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CNTNAME Branch,COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                            " FROM dbo.SALES " +
                            " INNER JOIN COUNTER  C ON SALES.SACOUNTERID = C.CNTID " +
                            " WHERE SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=1 " +
                            " AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
                else if (Type.ToUpper() == "PENDING")
                {
                    var q = await this.dapperContext.CreateConnection().QueryAsync<TotalSummary>("SELECT C.CNTNAME Branch,COUNT(VID) InvCount,ISNULL(SUM(SANETAMOUNT), 0) AS Amount" +
                           " FROM dbo.SALES " +
                           " INNER JOIN COUNTER  C ON SALES.SACOUNTERID = C.CNTID " +
                           " WHERE SALES.SAISHOMEDELIVERY = 1 AND  SALES.SAISSETTLED=0 " +
                           " AND SALES.SADATE BETWEEN @FROM AND @TO AND ISNULL(SALES.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY C.CNTNAME", new { FROM = Startdate, TO = Enddate });
                    totalSummaryList = q.ToList();

                }
            }



            return totalSummaryList;
        }
        #endregion Homedelivery


        #region TopSale


        public async Task<List<TopSales>> TopSales(DateTimeOffset Startdate, DateTimeOffset Enddate)
        {

            List<TopSales> totalSummaryList = new List<TopSales>();


            var q = await this.dapperContext.CreateConnection().QueryAsync<TopSales>("SELECT TOP 5 C.CNTNAME Branch,  ISNULL(SUM(S.SANETAMOUNT), 0) AS Amount" +
                " FROM dbo.SALES S " +
                " INNER JOIN COUNTER C ON S.SACOUNTERID =C.CNTID " +
              " WHERE  S.SADATE BETWEEN @FROM AND @TO AND ISNULL(S.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY Amount DESC", new { FROM = Startdate, TO = Enddate });

            totalSummaryList = q.ToList();

            return totalSummaryList;
        }
        public async Task<List<TopSalesLst>> SalesbyBranch(DateTimeOffset Startdate, DateTimeOffset Enddate)
        {
            List<TopSalesLst> totalSummaryList = new List<TopSalesLst>();


            var q = await this.dapperContext.CreateConnection().QueryAsync<TopSalesLst>("SELECT  C.CNTNAME Branch,  SUM(S.SANETAMOUNT) Amount, (SELECT SUM(SALES.SANETAMOUNT) FROM SALES ) AS NetAmt" +
                " FROM dbo.SALES S " +
                " INNER JOIN COUNTER C ON S.SACOUNTERID =C.CNTID " +
              " WHERE  S.SADATE BETWEEN @FROM AND @TO AND ISNULL(S.SACANCELED, 0) = 0 GROUP BY C.CNTNAME ORDER BY Amount DESC", new { FROM = Startdate, TO = Enddate });

            totalSummaryList = q.ToList();


            return totalSummaryList;
        }

        #endregion TopSale

        #region Bounce
        public async Task<List<bounce>> Bouncelist(DateTimeOffset Startdate, DateTimeOffset Enddate, string varbranch)
        {
            if (varbranch == "")
            {
                var q = await this.dapperContext.CreateConnection().QueryAsync<bounce>("SELECT BOUNCE.* ,PRODUCTS.PRDNAME" +
    "  			FROM BOUNCE  " +
    "           INNER JOIN PRODUCTS ON PRODUCTS.PRDID=BOUNCE.BCPRDID  " +
    "  			WHERE (BOUNCE.BCDATE BETWEEN @FROM AND @TO)  " +
    //"  			AND (BOUNCE.BCCOUNTERID=@CNTRID OR @CNTRID<=0) "+
    //"  			AND (BOUNCE.BCPRDID=@PRDTID OR @PRDTID<=0)  "+
    "  			ORDER BY PRODUCTS.PRDNAME", new { FROM = Startdate, TO = Enddate });
                return (List<bounce>)q;
            }
            else
            {
                // var q = await this.dapperContext.CreateConnection().QueryAsync<bounce>("SELECT BCDATE, BCPRDID, BCCOUNTERID, BCDIFFQTY, BCUSERID, BCBILLINGCOUNTERID, BCGODOWNID, BCFINYEARID, BCUSERSESSION, BCDAYSESSION FROM dbo.BOUNCE WHERE BOUNCE.BCCOUNTERID IN(" + varbranch + ") AND (BOUNCE.BCDATE BETWEEN @FROM AND @TO) ", new { FROM = Startdate, TO = Enddate });
                var q = await this.dapperContext.CreateConnection().QueryAsync<bounce>("SELECT BOUNCE.* ,PRODUCTS.PRDNAME" +
    "  			FROM BOUNCE  " +
    "           INNER JOIN PRODUCTS ON PRODUCTS.PRDID=BOUNCE.BCPRDID  " +
    "  			WHERE BOUNCE.BCCOUNTERID IN(" + varbranch + " ) AND (BOUNCE.BCDATE BETWEEN @FROM AND @TO)" +
    //"  			AND (BOUNCE.BCCOUNTERID=@CNTRID OR @CNTRID<=0) "+
    //"  			AND (BOUNCE.BCPRDID=@PRDTID OR @PRDTID<=0)  "+
    "  			ORDER BY PRODUCTS.PRDNAME", new { FROM = Startdate, TO = Enddate });
                return (List<bounce>)q;
            }





        }


        #endregion Bounce


        public async Task<ConsoleRefreshTokenModel> GetRefreshTokenByUserId(int userId)
        {
            var query = @"
                SELECT TOP 1 *
                FROM dbo.REFRESHTOKENS
                WHERE UserId = @UserId AND Revoked = 0
                ORDER BY Expires DESC";
            var parameters = new { UserId = userId };
            using (var connection = dapperContext.CreateConnection())
            {
                return await connection.QueryFirstOrDefaultAsync<ConsoleRefreshTokenModel>(query, parameters);
            }
        }

        ////public async Task<bool> ValidateRefreshToken(string refreshToken, int userId)
        ////{
        ////    using var connection = dapperContext.CreateConnection();

        ////    // SQL query to check refresh token validity
        ////    var query = @"
        ////SELECT COUNT(1)
        ////FROM RefreshTokens
        ////WHERE Token = @Token 
        ////  AND UserId = @UserId 
        ////  AND Expires > @CurrentTime 
        ////  AND Revoked = 0";

        ////    // Execute query with parameters
        ////    var isValid = await connection.ExecuteScalarAsync<int>(query, new
        ////    {
        ////        Token = refreshToken,
        ////        UserId = userId,
        ////        CurrentTime = DateTime.Now
        ////    });

        ////    // Return true if the token exists and is valid
        ////    return isValid > 0;
        ////}
        public async Task<Employee> GetByUserId(long userId)
        {
            var query = "SELECT EMPUSRNAME, EMPUSERPASSWORD, EMPID, EMPNAME, EMPUROLEID, EMPBLOCKED FROM EMPLOYEE WHERE EMPID = @UserId";
            var parameters = new { UserId = userId };
            using (var connection = dapperContext.CreateConnection())
            {
                return await connection.QueryFirstOrDefaultAsync<Employee>(query, parameters);
            }
        }

        public async Task<IEnumerable<Employee>> GetAllEmployees()
        {
            var query = "SELECT EMPUSRNAME, EMPUSERPASSWORD, EMPID, EMPNAME, EMPUROLEID, EMPBLOCKED FROM EMPLOYEE";
            using (var connection = dapperContext.CreateConnection())
            {
                return await connection.QueryAsync<Employee>(query);
            }
        }


        public async Task<SmsStatus> SendNewWahtsAppMsg(string MobileNos, string Message)
        {
            var url = $"{_whatsAppApiSettings.ApiBaseUrl}?mobile={MobileNos}&msg={Message}&apikey={_whatsAppApiSettings.ApiKey}";
            var client = _httpClientFactory.CreateClient();

            try
            {
                // Send the GET request to the API
                var resp = await client.GetAsync(url);

                // Read the response content as a string
                var htmlContent = await resp.Content.ReadAsStringAsync();

                // Check if the response contains "200" indicating success
                if (htmlContent.Contains("200"))
                {
                    return new SmsStatus
                    {
                        StatusCode = "200",
                        Status = true,
                        ID = "", // ID is unused in the provided logic
                        Message = "Message sent successfully"
                    };
                }
                else
                {
                    return new SmsStatus
                    {
                        Status = false,
                        StatusCode = resp.StatusCode.ToString(),
                        ID = "",
                        Message = htmlContent
                    };
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and return failure status
                return new SmsStatus
                {
                    Status = false,
                    StatusCode = "500",
                    ID = "",
                    Message = ex.Message
                };
            }
        }
    }


}