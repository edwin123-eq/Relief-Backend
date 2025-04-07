using ReliefApi.Models;

namespace ReliefApi.Contracts
{
    public interface IFAreport
    {
        Task<KeyValueSetting> GetKeyValueSettingByKey(string keyName);
        Task<SalesDailySummary> SaleDailyProfitSummaryAsync(DateTime fromDt, int excludeTax);


        void AddBlankObj(ref List<DataMain> lstData, int num = 1);


        Task<List<CashSummaryDtls>> RptSummaryStatementAsonDate(DateTime ason, long counterId, long cashAccId, string type, long debtorSchId, long credtrSchId);

        Task<List<Ledger>> Rpt_LedgerForIncomeAndExpenseHeads(
          DateTime fromDt,
          DateTime toDt,
          long custId,
          long cntrId,
          bool includeAll,
          DateTime finYearStartDt);
    }
}
