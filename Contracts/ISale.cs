namespace ReliefApi.Contracts
{
    public interface ISale
    {
        Task<Models.SalesDailySummary> SaleDailyProfitSummary(DateTime fromDt, int excludeTax);

    }
}
