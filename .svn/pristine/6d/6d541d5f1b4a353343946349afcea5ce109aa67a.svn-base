using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Models
{
    public class Profit
    {
        public string Type { get; set; }
        public decimal Today { get; set; }
        public decimal GpRate { get; set; }
        public decimal MTD { get; set; }
        public decimal YTD { get; set; }
        public decimal LMTD { get; set; }
        public decimal PrevYear { get; set; }

    }
    public class HomeDelivery
    {
       public string Status { get; set; }

        public decimal InvCnt { get; set; }
        public decimal InvAmt { get; set; }

    }
    public class HomeDeliveryBrLst
    {

        public string Status { get; set; }
        public List<HomeDeliveryBranch> HomeDeliveries { get; set; }

    }
    public class HomeDeliveryBranch
    {
        public string Branch { get; set; }
        public decimal InvCnt { get; set; }
        public decimal InvAmt { get; set; }
    }
    public class ClosingStock
    {
        public string Type { get; set; }
        public decimal MRP { get; set; }
        public decimal Cost { get; set; }
        public decimal CostWithGST { get; set; }
    }
    public class TypList
    {
        public string? Type { get; set; }
        public List<CategoryList> categoryLists  { get; set; }
        public List<SubCategoryList> subCategoryLists { get; set; }
        public List<MFRCategoryList> MFRCategoryLists  { get; set; }
        public List<SupplierCategoryList> SupplierCategoryLists  { get; set; }

    }
    public class SaleValueByCategory
    {
        public long CategoryId { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
    }

    public class SaleValueBySubCategory
    {
        public long CategoryId { get; set; }
        public string SubCategory { get; set; }
        public decimal Amount { get; set; }
    }

    public class SaleValueBySupplier
    {
        public long CustomerId { get; set; }
        public string Customer { get; set; }
        public decimal Amount { get; set; }
    }

    public class SaleValueByMFR
    {
        public long MFRId { get; set; }
        public string MNFRNAME { get; set; }
        public decimal Amount { get; set; }
    }

    public class CategoryList
    {
        public string Category { get; set; }
        public long CategoryId { get; set; }
        public decimal Today { get; set; }
        public decimal GpRate { get; set; }
        public decimal MTD { get; set; }
        public decimal YTD { get; set; }
        public decimal LMTD { get; set; }
        public decimal PrevYear { get; set; }

    }
    public class SubCategoryList
    {
        public string SubCategory { get; set; }
        public decimal Today { get; set; }
        public decimal GpRate { get; set; }
        public decimal MTD { get; set; }
        public decimal YTD { get; set; }
        public decimal LMTD { get; set; }
        public decimal PrevYear { get; set; }

    }
    public class MFRCategoryList
    {
        public string MFR { get; set; }
        public long MFRID { get; set; }
        public decimal Today { get; set; }
        public decimal GpRate { get; set; }
        public decimal MTD { get; set; }
        public decimal YTD { get; set; }
        public decimal LMTD { get; set; }
        public decimal PrevYear { get; set; }

    }
    public class SupplierCategoryList
    {
        public string Supplier { get; set; }
        public long SupplierId { get; set; }
        public decimal Today { get; set; }
        public decimal GpRate { get; set; }
        public decimal MTD { get; set; }
        public decimal YTD { get; set; }
        public decimal LMTD { get; set; }
        public decimal PrevYear { get; set; }

    }

    public class ClsTypList
    {
        public string? Type { get; set; }
        public List<ClsCategoryList> ClscategoryLists { get; set; }
        public List<ClsSubCategoryList> ClssubCategoryLists { get; set; }
        public List<ClsMFRCategoryList> ClsMFRCategoryLists { get; set; }
        public List<ClsSupplierCategoryList> ClsSupplierCategoryLists { get; set; }

    }
    public class TopSales
    {
        public string Branch { get; set; }
        public decimal  Amount { get; set; }
       
    }
    public class TopSalesLst
    {
        public string Branch { get; set; }
        public decimal Amount { get; set; }
        public decimal NetAmt { get; set; }

    }

    public class Graph
    {
        public string Month { get; set; }
        public decimal Amount { get; set; }
    }
    public class ClsCategoryList
    {
        public string Category { get; set; }
        public long CategoryId { get; set; }
        public decimal MRP { get; set; }
        public decimal Cost { get; set; }
        public decimal CostWithGST { get; set; }

    }
    public class ClsSubCategoryList
    {
        public long CategoryId { get; set; }
        public string SubCategory { get; set; }
        public decimal MRP { get; set; }
        public decimal Cost { get; set; }
        public decimal CostWithGST { get; set; }

    }
    public class ClsMFRCategoryList
    {
        public string MFR { get; set; }
        public decimal MRP { get; set; }
        public decimal Cost { get; set; }
        public decimal CostWithGST { get; set; }


    }
    public class ClsSupplierCategoryList
    {
        public string Supplier { get; set; }
        public decimal MRP { get; set; }
        public decimal Cost { get; set; }
        public decimal CostWithGST { get; set; }


    }
    public class TotalSummary
    {
        public string MFR { get; set; }
        public string Supplier { get; set; }
        public long  CatgryId { get; set; }
        public string Catgry { get; set; }
        public string SbCatgry { get; set; }
        public decimal Amount { get; set; }
        public decimal GpAmt { get; set; }
        public decimal GpdivAmt { get; set; }
        public long CustCount { get; set; }
        public string Branch { get; set; }
        public long InvCount { get; set; }
        public decimal StkValue { get; set; }

    }

    public class GPPercent
    {
       public long Id { get; set; }
        public string Name { get; set; }
        public decimal GpAmt { get;set; }
        public decimal GpdivAmt { get; set; }
        public decimal Gppercentage { get; set;}

    }

    public class Stock
    {
        public string PRDNAME { get; set; }
        public string CATNAME { get; set; }
        public string SCATNAME { get; set; }
        public string CONNAME { get; set; }
        public string MNFRNAME { get; set; }
        public string BRNAME { get; set; }
       
    }

    public class BankandCash
    {
        public string Branchid { get; set; }
        public string Branch { get; set; }
        public decimal Balance { get; set; }
        public string Bank { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public decimal RecievedAmt { get; set; }

    }
    public class BankLst
    {
        public string Type { get; set; }
         public decimal BankBal { get; set; }
        public decimal SundryCredit { get; set; }
        public decimal SundryDebt { get; set; }

    }
    public class BankBrnchLst
    {
        public string Type { get; set; }
        public List<BranchBankLst> branchBankLsts  { get; set; }
    }

    public class CashBrnchLst
    {
        public string Type { get; set; }
        public List<BranchCashLst> branchCashLsts { get; set; }
    }

    
    public class BranchBankLst
    {
        public string Bank { get; set; }
        public string BranchId { get; set; }
        public string Branch { get; set; }
        public decimal BankBal { get; set; }
        public decimal SundryCredit { get; set; }
        public decimal SundryDebt { get; set; }

    }
    public class BranchCashLst
    {
        public string Bank { get; set; }
        public string Branch { get; set; }
        public decimal NetBalance { get; set; }
        public decimal PrevBal { get; set; }
        public decimal Deposit { get; set; }
    }
    public class CashLst
    {
        public string Type { get; set; }
        public decimal NetBalance { get; set; }
        public decimal PrevBal { get; set; }
        public decimal BankDeposit { get; set; }

    }
    public class Finyear
    {
      
        public long FINID { get; set; }
        public DateTime? FINSTART { get; set; }
        public DateTime? FINEND { get; set; }
        public bool  FINISACTIVE { get; set; }
     

    }

    public class bounce
    {
        public DateTime? BCDATE { get; set; }
        public long BCPRDID { get; set; }
        public long BCCOUNTERID { get; set; }
        public decimal BCDIFFQTY { get; set; }
        public long BCUSERID { get; set; }
        public long BCBILLINGCOUNTERID { get;set; }
        public long BCGODOWNID { get;set; }
        public long BCFINYEARID { get; set; }
        public long BCUSERSESSION { get; set; }
        public long BCDAYSESSION { get; set; }
        public string PRDNAME { get; set; }
    }

    public class BounceModel
    {
        public string product { get; set; }
        public decimal Amount { get; set; }
    }

    public class LMTDDates
    {
        public DateTimeOffset LMTDstart { get; set; }
        public DateTimeOffset LMTDend { get; set; }
    }

    public class CustomerLocation
    {
        public long AccountID { get; set; }
        public string AccountName { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
    }

    public class Delivery
    {
        public long DLID { get; set; }
        public long DLSALEID { get; set; }
        public string? DLSALEDOCNO { get; set; }

        public long DLASSIGNEDBY { get; set; }
        public DateTime DLASSIGNEDAT { get; set; }
        public long DLEMPID { get; set; }
        public long? DLSTATUS { get; set; }

        public string? DeliveryStatusString { get; set; }

        public DateTime DLON { get; set; }
        public decimal DLCASHRCVD { get; set; }
        public decimal DLBANKRCVD { get; set; }
        public string DLREMARKS { get; set; }

        public string? EmployeeIDString { get; set; }
        public string? EmployeeName { get; set; }
        public long CustomerID { get; set; }
        public string PhoneNumber { get; set; }
        public string CUSTOMERNAME { get; set; } // Correct mapping to query: S.SACUSTNAME
        public string ADDRESS { get; set; } // Correct mapping to query: S.SACUSTADDR1
        public long QUANTITY { get; set; } // Correct mapping to query: S.SAQTY

        public decimal AMOUNTRECEIVED { get; set; } // Correct the typo
        public decimal NetSaleAmount { get; set; }

    }


    public class SalesDetails
    {
        public long DeliveryID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string PhoneNumber { get; set; }
        public long Quantity { get; set; }
        public string Address { get; set; }
        public int Status { get; set; }
        public decimal TotalCashReceived { get; set; }
        public decimal TotalBankReceived { get; set; }
        public decimal AmtReceived { get; set; }
        public long SaleId { get; set; }
        public long EmpId { get; set; }
    }

    public class SummaryDetails
    {
        public string EmployeeName { get; set; }
        public long DeliveredCount { get; set; }
        public long ReturnedCount { get; set; }
        public long PartiallyDeliveredCount { get; set; }
        public int PendingCount { get; set; }
        public decimal TotalCashReceived { get; set; }
        public decimal TotalBankReceived { get; set; }



    }

    public class ImageSettings
    {
        public string Company_QR_path { get; set; }
        public string FilePath { get; set; }
        public string PdfPath { get; set; }
    }
    public class ClosingReportDetails
    {
        public string EmployeeName { get; set; }
        public long TotalBills { get; set; }       // Total number of bills (all records for the employee)
        public long DeliveredCount { get; set; }   // Count of status 1 (Delivered)
        public long ReturnedCount { get; set; }     // Count of status 2 (Returned)
        public long PartiallyDeliveredCount { get; set; } // Count of status 3 (Partially Delivered)
        public long PendingCount { get; set; }     // Count of status 4 (Pending)
        public decimal TotalCashReceived { get; set; }
        public decimal TotalBankReceived { get; set; }
        public decimal TotalAmount { get; set; }   // Sum of TotalCashReceived and TotalBankReceived
    }

    public class UpiDetails
    {
        public string UPIcode { get; set; }
        public string AccountName { get; set; }
    }


    public class SmsStatus
    {
        public bool Status { get; set; }
        public string StatusCode { get; set; }
        public string ID { get; set; }
        public string Message { get; set; }
    }

    public class WhatsAppApiSettings
    {
        public string ApiKey { get; set; }
        public string ApiBaseUrl { get; set; }
    }
}
