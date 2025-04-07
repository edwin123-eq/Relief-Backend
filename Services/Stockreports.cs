using ReliefApi.Models;
using Dapper;
using ReliefApi.Contracts;
namespace ReliefApi.Services
{
    public class Stockreports:IStockReport
    {
        private readonly DapperContext db;
        public Stockreports(DapperContext dapperContext)
        {
            db = dapperContext;
        }
        public async Task<List<StkValueEffPuRate>> RptStockValuationEffctvPuRate(
         DateTime fromDt,
         DateTime toDt,
         long suppId,
         long counterId,
         long mfr,
         long dvsn,
         long prdId,
         long godown,
         bool showValidClsngStk)
        {
            using (var connection = db.CreateConnection()) // Ensure db.CreateConnection() returns a valid IDbConnection
            {
                return (await connection.QueryAsync<StkValueEffPuRate>(
                    @"SELECT 
                PRODUCTS.PRDID AS Id,
                PRODUCTS.PRDNAME AS ProductName,
                PRDBATCH.BCCODE AS Code,
                PRDBATCH.BCEXPIRYDATE AS ExpDate,
                PRDBATCH.BCBATCHNO AS Batch,
                MFR.MNFRNAME AS Mfr,
                HSN.HSNCODE AS HSN,
                CATNAME AS Category,
                SCATNAME AS SubCat,
                (ISNULL(OPSTOCKO.QTY, 0) + ISNULL(INSTOCK.QTY, 0) + ISNULL(OUTSTOCK.QTY, 0)) AS Qty,
                A.DETTAXRATE AS Tax,
                A.DETMRP AS MRP,
                A.DETRATE AS Rate,
                A.DETCESSPERC AS Cess,
                A.DEDISCPERC AS Discount,
                CASE WHEN ISNULL(A.PUSUPPLIER, 0) = 0 THEN B.ACACNAME ELSE A.PUSUPNAME END AS SupplierName,
                A.ACCODE AS SupCode,
                PRDPRICELIST.PLPURATE AS PLPurate,
                PRDPRICELIST.PLMRP AS PLMRP
            FROM PRODUCTS
            INNER JOIN PRDBATCH ON PRDBATCH.BCPRDID = PRODUCTS.PRDID
            INNER JOIN PRDPRICELIST ON PRDPRICELIST.PLBCID = PRDBATCH.BCID AND PRDPRICELIST.PLSEQ = 0
            INNER JOIN CATEGORY ON PRODUCTS.PRDCATID = CATEGORY.CATID
            LEFT JOIN (SELECT MANUFACTURER.MNFRID, MANUFACTURER.MNFRNAME FROM MANUFACTURER) MFR ON PRODUCTS.PRDMFRID = MFR.MNFRID
            LEFT JOIN SUBCATEGORY ON PRODUCTS.PRDSUBCATID = SUBCATEGORY.SCATID
            LEFT JOIN HSN ON PRODUCTS.PRDHSN = HSN.HSNID
            LEFT JOIN (
                SELECT TRDETAILS.DETBCID, SUM(TRDETAILS.DETQTY) AS QTY
                FROM TRDETAILS
                WHERE (DETFLAG = 'OP' OR TRDETAILS.DETDATE < @FROM)
                AND (TRDETAILS.DETCOUNTERID = @CNTID OR @CNTID <= 0)
                AND (TRDETAILS.DETGOID = @GDWN OR @GDWN <= 0)
                GROUP BY TRDETAILS.DETBCID
            ) AS OPSTOCKO ON PRDBATCH.BCID = OPSTOCKO.DETBCID
            LEFT JOIN (
                SELECT TRDETAILS.DETBCID, SUM(TRDETAILS.DETQTY) AS QTY
                FROM TRDETAILS
                WHERE (DETDATE BETWEEN @FROM AND @TO)
                AND (TRDETAILS.DETCOUNTERID = @CNTID OR @CNTID <= 0)
                AND TRDETAILS.DETQTY > 0
                AND (TRDETAILS.DETGOID = @GDWN OR @GDWN <= 0)
                AND DETFLAG <> 'OP'
                GROUP BY TRDETAILS.DETBCID
            ) AS INSTOCK ON PRDBATCH.BCID = INSTOCK.DETBCID
            LEFT JOIN (
                SELECT TRDETAILS.DETBCID, SUM(TRDETAILS.DETQTY) AS QTY
                FROM TRDETAILS
                WHERE (DETDATE BETWEEN @FROM AND @TO)
                AND (TRDETAILS.DETCOUNTERID = @CNTID OR @CNTID <= 0)
                AND TRDETAILS.DETQTY < 0
                AND (TRDETAILS.DETGOID = @GDWN OR @GDWN <= 0)
                AND DETFLAG <> 'OP'
                GROUP BY TRDETAILS.DETBCID
            ) AS OUTSTOCK ON PRDBATCH.BCID = OUTSTOCK.DETBCID
            OUTER APPLY (
                SELECT TOP 1 PURCHASEDTL.DETMRP, PURCHASE.PUSUPPLIER,
                            PURCHASE.PUSUPNAME, ACM.ACCODE, PURCHASEDTL.DEDISCPERC,
                            PURCHASEDTL.DETCESSPERC, PURCHASEDTL.DETRATE, PURCHASEDTL.DETTAXRATE
                FROM PURCHASE
                INNER JOIN PURCHASEDTL ON PURCHASE.VID = PURCHASEDTL.VID
                LEFT JOIN (SELECT ACID, ACACNAME, ACCODE FROM ACMASTER) ACM ON PURCHASE.PUSUPPLIER = ACM.ACID
                WHERE PURCHASEDTL.DETBATCHID = PRDBATCH.BCID
                AND (PURCHASE.PUSUPPLIER = @SUPP OR @SUPP <= 0)
                ORDER BY PURCHASE.PUDOCDATE DESC, PURCHASE.VID DESC
            ) AS A
            OUTER APPLY (
                SELECT TOP 1 ISNULL(ACM.ACACNAME, '') AS ACACNAME
                FROM PRODUCTSSUPPLIER
                LEFT JOIN (SELECT ACID, ACACNAME FROM ACMASTER) ACM ON PRODUCTSSUPPLIER.PRDSUPPLIER = ACM.ACID
                WHERE (PRODUCTSSUPPLIER.PRDSUPPLIER = @SUPP OR @SUPP <= 0)
                AND (PRODUCTSSUPPLIER.PRDID = PRDBATCH.BCPRDID)
                ORDER BY PRODUCTSSUPPLIER.PRDSUPPLIERSEQ
            ) AS B
            WHERE (PRODUCTS.PRDSUPPID = @SUPP OR @SUPP <= 0)
            AND (PRODUCTS.PRDID = @PRDID OR @PRDID <= 0)
            AND PRODUCTS.PRDTYPE = 0
            AND (PRODUCTS.PRDDIVISIONID = @DIV OR @DIV <= 0)
            AND (@SHOWVALIDCLSSTKONLY = 0
                OR (@SHOWVALIDCLSSTKONLY = 1 AND (
                    (ISNULL(OPSTOCKO.QTY, 0) + ISNULL(INSTOCK.QTY, 0) + ISNULL(OUTSTOCK.QTY, 0)) >= 0
                    OR ISNULL(INSTOCK.QTY, 0) > 0
                    OR ISNULL(OUTSTOCK.QTY, 0) > 0)))
            ORDER BY PRDNAME, BCID",
                    new
                    {
                        FROM = fromDt,
                        TO = toDt,
                        SUPP = suppId,
                        MFR = mfr,
                        DIV = dvsn,
                        PRDID = prdId,
                        CNTID = counterId,
                        GDWN = godown,
                        SHOWVALIDCLSSTKONLY = showValidClsngStk
                    },
                    commandTimeout: 360)).ToList();
            }
        }
    }
}
  

