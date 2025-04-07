using ReliefApi.Models;

namespace ReliefApi.Contracts
{
    public interface IStockReport
    {

        Task<List<StkValueEffPuRate>> RptStockValuationEffctvPuRate(
            DateTime fromDt,
            DateTime toDt,
            long suppId,
            long counterId,
            long mfr,
            long dvsn,
            long prdId,
            long godown,
            bool showValidClsngStk);


    }
}
