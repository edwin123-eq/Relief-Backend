using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReliefApi.Contracts;
using ReliefApi.Models;
using System.Runtime.InteropServices;

namespace ReliefApi.Controllers
{
    [Guid("9245fe4a-d402-451c-b9ed-9c1a04247482")]
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [EnableCors()]
    public class CashSummaryMailController : ControllerBase
    {
        private readonly IFAreport _FAreport;
        private readonly ISale _sale;
        private readonly IStockReport _stockReport;
        private readonly IACmaster _acmaster;
        private readonly IDbcodes _dbcodes;
        public CashSummaryMailController(IFAreport FAreport, ISale sale, IStockReport stockReport, IACmaster acmaster, IDbcodes dbcodes)
        {
            _FAreport = FAreport;
            _sale = sale;
            _stockReport = stockReport;
            _acmaster = acmaster;
            _dbcodes = dbcodes;
        }


     
        [HttpPost("CashSummaryMail")]
        public async Task<List<DataMain>> CashSummaryMail1(DateTime asOn, int type,long CounterId)
        {
            double intAmt = 0, intCost = 0, intAddlCost = 0, intProfit = 0, intProfitPer = 0;
            Models.SalesDailySummary objPrftSumm = new Models.SalesDailySummary();
            long intDbtrSchID = 0;
            long intCrdtrSchID = 0;
            long cashAccID = 0;
            long shortExcessAccId = 0;

            var blnExcludeCost = await _FAreport.GetKeyValueSettingByKey(SalesSettings.SALES_EXCLUDE_TAX_FROM_COST_CALCULATION);
            int excludeTax = blnExcludeCost?.Value?.ToString() == "1" ? 1 : 0;

            List<Models.CashSummaryDtls> lstInc = new List<Models.CashSummaryDtls>();
            List<Models.CashSummaryDtls> lstExp = new List<Models.CashSummaryDtls>();
            List<Models.StkValueEffPuRate> stk = new List<Models.StkValueEffPuRate>();
            decimal intStock = 0;
            List<Models.StockJournalRegister> stkAdj = new List<Models.StockJournalRegister>();
            decimal intStockAdj = 0;
            decimal cashBalance = 0, totalShortExcess = 0;

            // Fetch account IDs
            intCrdtrSchID = (await _dbcodes.GetPostCodeByTag("SUPSCH", "S")).DBID;
            intDbtrSchID = (await _dbcodes.GetPostCodeByTag("CUSTSCH", "S")).DBID;

            var cashobj = await _dbcodes.GetPostCodeByTag("CASHACC", "A");
            cashAccID = cashobj.DBID;
            shortExcessAccId = (await _dbcodes.GetPostCodeByTag("SHORTEXCES", "A")).DBID;

            if (type == 0)
            {
                objPrftSumm = await _sale.SaleDailyProfitSummary(asOn, excludeTax);
                stk = await _stockReport.RptStockValuationEffctvPuRate(CNV.ToDate(asOn), CNV.ToDate(asOn), 0,
                     CounterId, 0, 0, 0, 0, true);

                cashBalance = await _acmaster.GetAccountBalanceById(cashAccID, 0, CNV.ToDate(asOn),
                     CounterId);

                List<Models.Ledger> ledgerLst = new List<Models.Ledger>();
                if (shortExcessAccId > 0)
                {
                    ledgerLst = await _FAreport.Rpt_LedgerForIncomeAndExpenseHeads(
                        CNV.ToDate(asOn), CNV.ToDate(asOn), shortExcessAccId,
                         CounterId, false, new DateTime(2024, 3, 31));

                    if (ledgerLst != null && ledgerLst.Count > 0)
                    {
                        for (int j = 1; j < ledgerLst.Count; j++)
                        {
                            totalShortExcess += ledgerLst[j].Debit - ledgerLst[j].Credit;
                        }
                    }
                }

                lstInc = await _FAreport.RptSummaryStatementAsonDate(CNV.ToDate(asOn), CounterId,
                    cashAccID, "IN", intDbtrSchID, intCrdtrSchID);
                lstExp = await _FAreport.RptSummaryStatementAsonDate(CNV.ToDate(asOn), CounterId,
                    cashAccID, "OUT", intDbtrSchID, intCrdtrSchID);
            }

            if (objPrftSumm != null)
            {
                intAmt = (double)(objPrftSumm.Amount - objPrftSumm.SRAmt); // Total Sales - Sales Return
                intCost = (double)(objPrftSumm.Cost - objPrftSumm.SRCost); // Total Cost - Return Cost
                intAddlCost = (double)(objPrftSumm.AdditionalCost);
                intProfit = intAmt - intCost;
                intProfitPer = intAmt != 0 ? ((intAmt - (intCost + intAddlCost)) / intAmt) * 100 : 100;

                // Ensure positive profit if that's the intent (temporary fix)
                if (intProfit < 0) intProfit = Math.Abs(intProfit);
                if (intProfitPer < 0) intProfitPer = Math.Abs(intProfitPer);
            }


            if (stk != null)
            {
                foreach (var s in stk)
                {
                    if (s.Qty > 0)
                    {
                        decimal xRate = s.Rate > 0 ? s.Rate : s.PLPurate;
                        intStock += xRate * s.Qty;
                    }
                }
            }

            if (stkAdj != null)
            {
                foreach (var st in stkAdj)
                {
                    intStockAdj += st.Rate * st.Quantity;
                }
            }

            List<DataMain> lstData = new List<DataMain>();
            int intNoOfRowsToAdd = lstInc.Count < lstExp.Count ? lstExp.Count + 1 : lstInc.Count + 1;
            _FAreport.AddBlankObj(ref lstData, intNoOfRowsToAdd);

            int intRightIdx = 0;
            foreach (var x in lstInc)
            {
                DataMain objDm = lstData[intRightIdx];
                objDm.IncName = x.Type;
                objDm.IncCashValue = x.Bal;
                intRightIdx++;
            }

            intRightIdx = 0;
            foreach (var x in lstExp)
            {
                DataMain objDm = lstData[intRightIdx];
                objDm.ExpName = x.Type;
                objDm.ExpCashValue = x.Bal;
                intRightIdx++;
            }

            lstData.Add(new DataMain
            {
                IncName = "Stock",
                IncCashValue = intStock
            });

            lstData.Add(new DataMain
            {
                IncName = "Excess /Short Sales Collection",
                IncCashValue = totalShortExcess,
                ExpName = "CashBalance",
                ExpCashValue = cashBalance
            });

            lstData.Add(new DataMain
            {
                IncName = "Stock Adjustment Amount",
                IncCashValue = intStockAdj
            });

            lstData.Add(new DataMain
            {
                IncName = "Total Cost",
                IncCashValue = (decimal)intCost
            });

            lstData.Add(new DataMain
            {
                IncName = "Total Profit",
                IncCashValue = (decimal)intProfit
            });

            lstData.Add(new DataMain
            {
                IncName = "Profit %",
                IncCashValue = (decimal)intProfitPer
            });

            //// Debug logging
            //Console.WriteLine($"cashAccID: {cashAccID}, cashBalance: {cashBalance}");
            //Console.WriteLine($"intAmt: {intAmt}, intCost: {intCost}, intAddlCost: {intAddlCost}");
            //Console.WriteLine($"intProfit: {intProfit}, intProfitPer: {intProfitPer}");

            return lstData;
        }
        
      


    }
}
