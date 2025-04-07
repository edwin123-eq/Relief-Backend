using Models;
using ReliefApi.Models;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IBranches
    {
        Task AddRefreshToken(ConsoleRefreshTokenModel model);
        Task<Employee> GetByUserName(string UserName);
        Task<List<Branch>> List(bool IsAll, long StateId);
        // Category List
        Task<List<Category>> CategoryList();
        Task<List<Finyear>> FinYearList();
        Task<Finyear> GetFinYear();
        Task<Finyear> FinYearCalculate(DateTimeOffset Date);
        Task<TotalSummary> SaleGraph(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<TotalSummary> PurchaseGraph(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<TotalSummary> TotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<SaleValueByCategory>> SaleValueByCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<List<SaleValueBySubCategory>> SaleValueBySubCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<List<SaleValueBySupplier>> SaleValueBySupplier(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<List<SaleValueByMFR>> SaleValueByMFR(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);

        Task<List<SaleValueByCategory>> PurchaseValueByCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<List<SaleValueBySubCategory>> PurchaseValueBySubCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<List<SaleValueBySupplier>> PurchaseValueBySupplier(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);
        Task<List<SaleValueByMFR>> PurchaseValueByMFR(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches);

        Task<TotalSummary> GPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<GPPercent>> GPPercentCal(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task <List<TotalSummary>> CategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> CategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> SubCategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> SubCategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> MFRTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> MFRGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> SupplierTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> SupplierGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);


        /// <Purchase>      
        Task<TotalSummary> PurTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<TotalSummary> PurGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> PurCategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> PurCategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> PurSubCategoryTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> PurSubCategoryGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> PurMFRTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> PurMFRGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> PurSupplierTotalAmount(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> PurSupplierGPPercent(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        /// <Closingstock>      
        Task<TotalSummary> ClosingStock(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> ClosingStockCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> ClosingStockSubCategory(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> ClosingStockMFR(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        Task<List<TotalSummary>> ClosingStockSupplier(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);

        //<BANK AND CASH>
        Task<BankandCash> BankandCash(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<BankandCash>> BranchBankandCash(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<BankandCash>> BankSummary(DateTimeOffset Startdate, DateTimeOffset Enddate, long Branches, string Type);

        Task<TotalSummary> Homedelivery(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TotalSummary>> BranchHomedelivery(DateTimeOffset Startdate, DateTimeOffset Enddate, string Branches, string Type);
        Task<List<TopSales>> TopSales(DateTimeOffset Startdate, DateTimeOffset Enddate);
        Task<List<TopSalesLst>> SalesbyBranch(DateTimeOffset Startdate, DateTimeOffset Enddate);

        //BOUNCE
        Task<List<bounce>> Bouncelist(DateTimeOffset Startdate, DateTimeOffset Enddate, string varbranch);

        //CUSTOMER LOCATION
        Task<CustomerLocation> CustomerLocation(long AccountID);
        Task<CustomerLocation> UpdateCustomerLocation(long AccountID, string Longitude, string Latitude);

        //DELIVERY
        Task<List<Delivery>> GetDeliveries(long employeeId, int? status, DateTimeOffset date);
        Task<Delivery> UpdateDelivery(Delivery delivery);

        Task<SalesDetails> GetSalesDetails(long DLSALEID, long DLEMPID);

        Task<SummaryDetails> GetSummaryDetails(long EmployeeID, DateTimeOffset Date);

        Task<ClosingReportDetails> GetClosingReport(long EmployeeID, DateTimeOffset Date);
        Task<ConsoleRefreshTokenModel> GetRefreshTokenByUserId(int userId);
         //Task<bool> ValidateRefreshToken(string refreshToken, int userId);
        Task<List<Delivery>> GetDeliveryReport(long employeeID, int? status, DateTimeOffset startDate, DateTimeOffset endDate);
        Task<Employee> GetByUserId(long userId);
        Task<SmsStatus> SendNewWahtsAppMsg(string MobileNos, string Message);
        Task<IEnumerable<Employee>> GetAllEmployees();

    }

}