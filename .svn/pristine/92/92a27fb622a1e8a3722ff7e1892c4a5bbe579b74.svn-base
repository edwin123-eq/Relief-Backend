using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization; // For DataMember attribute

namespace ReliefApi.Models // Adjust the namespace as needed
{
    [DataContract] // Use DataContract for serialization
    public class FAreport
    {
        [DataMember]
        public decimal Bal { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int Seq { get; set; }

        [DataMember]
        public string CalcFlag { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public decimal Nos { get; set; }

        [DataMember]
        public decimal MakingCharge { get; set; }

        [DataMember]
        public decimal Value { get; set; }

        [DataMember]
        public string TranFlag { get; set; }

        [DataMember]
        public int OrderSeq { get; set; }
    }
    [DataContract]
    public class SalesDailySummary
    {
        [DataMember]
        public decimal Cash { get; set; }

        [DataMember]
        public decimal Credit { get; set; }

        [DataMember]
        public decimal Card { get; set; }

        [DataMember]
        public string CardName { get; set; }

        [DataMember]
        public decimal Total { get; set; }

        [DataMember]
        public decimal Amount { get; set; }

        [DataMember]
        public decimal Cost { get; set; }

        [DataMember]
        public decimal AdditionalCost { get; set; }

        [DataMember]
        public decimal B2B { get; set; }

        [DataMember]
        public decimal SRAmt { get; set; }

        [DataMember]
        public decimal SRCost { get; set; }
    }

    public class StkValueEffPuRate
    {
        public long Id { get; set; }
        public string ProductName { get; set; }
        public string Code { get; set; }
        public DateTime ExpDate { get; set; }
        public string Batch { get; set; }
        public string Mfr { get; set; }
        public string HSN { get; set; }
        public string Category { get; set; }
        public string SubCat { get; set; }
        public decimal Qty { get; set; }
        public decimal Tax { get; set; }
        public decimal MRP { get; set; }
        public decimal Rate { get; set; }
        public decimal Cess { get; set; }
        public decimal Discount { get; set; }
        public string SupplierName { get; set; }
        public string SupCode { get; set; }
        public decimal PLPurate { get; set; }
        public decimal PLMRP { get; set; }
    }
    public static class SalesSettings
    {
        public const string VAN_MANDATORY = "VAN MANDATORY";
        public const string SALES_DISCOUNT_LEVEL = "SALES DISCOUNT LEVEL";
        public const string SALES_SHOW_ITEM_WISE_DISCOUNT = "SALES SHOW ITEM WISE DISCOUNT";
        public const string SALES_SHOW_ITEM_WISE_ADDL_DISCOUNT = "SALES SHOW ITEM WISE ADDL DISCOUNT";
        public const string SALES_SHOW_ITEM_WISE_SPL_DISCOUNT = "SALES SHOW ITEM WISE SPL DISCOUNT";
        public const string SALES_LENGTH_WIDTH_CONTROL = "SALES LENGTH WIDTH CONTROL";
        public const string SALES_SHOW_ITEM_WISE_REMARKS = "SALES SHOW ITEM WISE REMARKS";
        public const string SALES_RATE_EDIT = "SALES RATE EDIT";
        public const string SALES_ITEM_WISE_GODOWN = "SALES ITEM WISE GODOWN";
        public const string SALES_SHOW_MRP = "SALES SHOW MRP";
        public const string SALES_SHOW_FREE_QTY = "SALES SHOW FREE QTY";
        public const string SALES_SHOW_COLOR_RATE = "SALES SHOW COLOR RATE";
        public const string SALES_SHOW_BULK = "SALES SHOW BULK";
        public const string SALES_SHOW_AMOUNT = "SALES SHOW AMOUNT";
        public const string SALES_WARN_MRP = "SALES WARN MRP";
        public const string SALES_WARN_LOSS = "SALES WARN LOSS";
        public const string SALES_LOSS_COMPARISON = "SALES LOSS COMPARISON";
        public const string SALES_ENABLE_COOLIE_CALCULATION = "SALES ENABLE COOLIE CALCULATION";
        public const string SALES_COOLIE_CALCULATED_ON = "SALES COOLIE CALCULATED ON";
        public const string SALES_CREDIT_DAYS_VALIDATION = "SALES CREDIT DAYS VALIDATION";
        public const string SALES_CREDIT_AMOUNT_VALIDATION = "SALES CREDIT AMOUNT VALIDATION";
        public const string SALES_RETAIL_PRODUCT_HELP_ON_ENTER_KEY = "SALES RETAIL PRODUCT HELP ON ENTER KEY";
        public const string SALES_RETAIL_QTY_AS_ONE = "SALES RETAIL QTY AS ONE";
        public const string SALES_RETAIL_BARCODE_MODE_BILLING = "SALES RETAIL BARCODE MODE BILLING";
        public const string SALES_ORDER_AMT_VALIDATION = "SALES ORDER AMT VALIDATION";
        public const string SALES_RETAIL_AMT_LIMIT_CUSTDTL_MANDATORY = "SALES RETAIL AMT LIMIT CUSTDTL MANDATORY";
        public const string SALES_RESET_BILLNOS_DAILY = "SALES RESET BILLNOS DAILY";
        public const string SALES_MRP_EDIT = "SALES MRP EDIT";

        public const string SALES_SHOW_ITEMMASTER_DISC_PER = "SALES_SHOW_ITEMMASTER_DISC_PER";
        public const string SALES_SHOW_ITEMMASTER_DISC_AMT = "SALES_SHOW_ITEMMASTER_DISC_AMT";

        public const string SALES_ENABLE_RETIAL_POS = "SALES ENABLE RETIAL POS";

        public const string REP_CONTROL = "REP CONTROL";
        public const string DEFAULT_REP = "DEFAULT REP";

        public const string MAXIMUM_DOC_SIZE_FOR_UPLOAD = "MAXIMUM DOC SIZE FOR UPLOAD";

        public const string PROMOTOR_CONTROL = "PROMOTOR CONTROL";
        public const string DEFAULT_PROMOTOR = "DEFAULT PROMOTOR";

        public const string SALESMAN_CONTROL = "SALESMAN CONTROL";
        public const string SALESMAN_MANDATORY_CONTROL = "SALESMAN MANDATORY CONTROL";
        public const string DEFAULT_SALESMAN = "DEFAULT SALESMAN";

        public const string SALES_SHOW_NLC = "SALES SHOW NLC";

        public const string REGULAR_PURCHASE_WHATSAPP_TEMPLATE = "REGULAR PURCHASE WHATSAPP TEMPLATE";

        // Sales return
        public const string SALES_RETURN_SHOW_SOLD_ITEMS_ONLY = "SALES RETURN SHOW SOLD ITEMS ONLY";
        public const string SALES_RETURN_SALESMAN_MANDATORY_CONTROL = "SALES RETURN SALESMAN MANDATORY CONTROL";
        public const string PRESCRIPTION_MANDATORY = "PRESCRIPTION MANDATORY";
        public const string ALLOW_NEGATIVE_STOCK = "ALLOW NEGATIVE STOCK";
        public const string SHOW_VALID_STOCK_ITEMS = "SHOW VALID STOCK ITEMS";
        public const string SALES_ALLOW_ZERO_RATED_ITEMS = "SALES ALLOW ZERO RATED ITEMS";
        public const string SALES_PRINT_GROUP_ITEMS_USING_RATES = "SALES PRINT GROUP ITEMS USING RATES";
        public const string SALES_BLOCK_EXPIRED_ITEMS = "SALES_BLOCK_EXPIRED_ITEMS";
        public const string SALES_HIGHLIGHT_NEGATIVE_STOCK_ITEMS = "SALES HIGHLIGHT NEGATIVE STOCK ITEMS";
        public const string SALES_WARN_DUPLICATE_ITEMS = "SALES_WARN_DUPLICATE_ITEMS";
        public const string SALES_WHOLESALE_QTY_AS_ONE = "SALES WHOLESALE QTY AS ONE";
        public const string SALES_BLOCK_LOSS = "SALES BLOCK LOSS";
        public const string SALES_BLOCK_LOSS_DAILY_PRICE_CHANGE_ITEMS = "BLOCK ONLY FOR DAILY PRICE CHANGE ITEMS";
        public const string SALES_ENABLE_ADVANCE_SEARCH = "SALES ENABLE ADVANCE SEARCH";
        public const string SALES_PRODUCT_HELP_ON_ENTER_KEY = "ENABLE ENTER KEY PRODUCT SELECTION SALES";
        public const string ENABLE_FOCUS_ON_DOCDATE_IN_SALES = "ENABLE FOCUS ON DOCDATE IN SALES";
        public const string ENABLE_FOCUS_ON_REP_IN_SALES = "ENABLE FOCUS ON REP IN SALES";
        public const string ENABLE_FOCUS_ON_PROMOTER_IN_SALES = "ENABLE FOCUS ON PROMOTER IN SALES";
        public const string SALES_PROMPT_MULTI_UNIT_SELECTION = "SALES PROMPT MULTI UNIT SELECTION";
        public const string SALES_RETAIL_PRICE_LEVEL = "SALES RETAIL PRICE LEVEL";
        public const string SALES_USE_LOCAL_PRINT_SETTINGS = "SALES USE LOCAL PRINT SETTINGS";
        public const string SALES_QTY_DECIMAL_PLACE_CTRL_AS_PER_UNIT = "SALES QTY DECIMAL PLACE CTRL AS PER UNIT";
        public const string SALES_ADJUST_TOTAL_AMT_TO_NETRATE = "SALES ADJUST TOTAL AMT TO NETRATE";
        public const string ENABLE_DELIVERY_CHALLAN_PRINT_IN_SALES = "ENABLE DELIVERY CHALLAN PRINT IN SALES";
        public const string SALES_ENABLE_EXCESS_RECEIPT_IN_CREDIT_BILLS = "SALES ENABLE EXCESS RECEIPT IN CREDIT BILLS";
        public const string SALES_CHECK_DUPLICATE_INVOICE = "SALES CHECK DUPLICATE INVOICE";
        public const string SALES_CHECK_INVOICE_INSAME_DATE = "SALES CHECK INVOICE INSAME DATE";
        public const string SALES_FLOOD_CESS = "SALES FLOOD CESS";
        public const string SALESRETURN_FLOOD_CESS = "SALESRETURN FLOOD CESS";
        public const string SALES_INCLUDE_FLOOD_CESS_IN_NETRATE_REVERSE = "SALES INCLUDE FLOOD CESS IN NETRATE REVERSE";
        public const string SALES_GODOWN_SLIP_PRINT = "SALES GODOWN SLIP PRINT";
        public const string SALES_GODOWN_SLIP_PRINT_ALL_ITEMS = "SALES GODOWN SLIP PRINT ALL ITEMS";
        public const string SALES_GODOWN_SLIP_PRINT_TEMPLATE = "SALES GODOWN SLIP PRINT TEMPLATE";
        public const string SALES_PRINT_ORDER_OF_ITEMS = "SALES PRINT ORDER OF ITEMS";
        public const string SALES_DISPLAY_REMARKS_IN_PRODUCT_DETAIL_PANEL = "SALES DISPLAY REMARKS IN PRODUCT DETAIL PANEL";
        public const string SALES_SHOW_BULK_STOCK = "SALES SHOW BULK STOCK";
        public const string SALES_BULK_STOCK_BASED_ON = "SALES BULK STOCK BASED ON";
        public const string SALES_SHOW_GODOWN_WISE_STOCK = "SALES SHOW GODOWN WISE STOCK";
        public const string SHOW_STOCK_IN_SALES_INCLUDING_SALES_ORDER = "SHOW STOCK IN SALES INCLUDING SALES ORDER";
        public const string SALES_DAYS_TO_SHOW_PREVIOUS_BILLS = "SALES DAYS TO SHOW PREVIOUS BILLS";
        public const string SALES_EXCLUDE_TAX_FROM_COST_CALCULATION = "SALES EXCLUDE TAX FROM COST CALCULATION";
        public const string SALES_PROMPT_PRINT_MODE = "SALES PROMPT PRINT MODE";
        public const string SALES_SHOW_LAST_3_RATES = "SALES SHOW LAST 3 RATES";
        public const string DISTRIBUTOR_FEATURES = "DISTRIBUTOR FEATURES";
        public const string SALES_ROUTE_IN_DISTRIBUTOR_FEATURES = "SALES ROUTE IN DISTRIBUTOR FEATURES";
        public const string SALES_BLOCK_BILLING_IF_ROUTES_ARE_DIFFERENT = "SALES BLOCK BILLING IF ROUTES ARE DIFFERENT";
        public const string ENABLE_ESTIMATION_FEATURES = "ENABLE ESTIMATION FEATURES";
        public const string SALES_ENABLE_RENT_CALCULATION = "SALES ENABLE RENT CALCULATION";
        public const string SALES_RENT_CALCULATED_ON = "SALES RENT CALCULATED ON";
        public const string SALES_INCREASE_REMARK_COLUMN_WIDTH = "SALES INCREASE REMARK COLUMN WIDTH";
        public const string SALES_POCKET_CASH = "SALES POCKET CASH";
        public const string SALES_POCKET_CASH_CREDIT_LIMIT = "SALES POCKET CASH CREDIT LIMIT";
        public const string SALES_POCKET_CASH_TERM_FOR_USER = "SALES POCKET CASH TERM FOR USER";
        public const string SALES_CASH_RECEIVED_PROMPT = "SALES CASH RECEIVED PROMPT";
        public const string SALES_ITEM_WISE_PRICE_LEVEL = "SALES ITEM WISE PRICE LEVEL";
        public const string SALES_ENABLE_LOYALTY_OFFER = "SALES ENABLE LOYALTY OFFER";
        public const string SALES_RETURN_VAN_MANDATORY = "SALES RETURN VAN MANDATORY";
        public const string ENABLE_FOCUS_ON_CARDNO = "ENABLE FOCUS ON CARDNO";
        public const string SALES_CHANGE_ITEM_PRICE_LEVEL_USING_SEQUENCE = "SALES CHANGE ITEM PRICE LEVEL USING SEQUENCE";

        public const string AUTO_FILL_CASH_IN_RECEIPT = "AUTO FILL CASH IN RECEIPT";

        public const string SALES_ENABLE_DELIVERY = "SALES ENABLE DELIVERY";

        public const string LOAD_CURRENT_RATE_FROM_QTN = "LOAD CURRENT RATE FROM QTN";

        public const string SALES_ENABLE_BRANCH_ADDR_IN_EWAY = "SALES ENABLE BRANCH ADDR IN EWAY";

        public const string SALES_USE_NORMAL_CUSTOMER_CREATION = "SALES USE NORMAL CUSTOMER CREATION";

        public const string RETAIL_SALES_SHOW_BILLING_COUNTER_DATA_IN_SEARCHES = "RETAIL SALES SHOW BILLING COUNTER DATA IN SEARCHES";

        public const string SALES_COMMISION_CALC_ON = "SALES COMMISION CALCULATED ON";
        public const string SALES_FAST_MODE_BILLING = "SALES FAST MODE BILLING";
        public const string SALES_SHOW_OFFER_ITEM_DETAILS = "SALES SHOW OFFER ITEM DETAILS";
        public const string SALES_DELETE_BILLS_PERMANENTLY = "SALES DELETE BILLS PERMANENTLY";

        public const string EXPORT_SALES_PRINT_TEMPLATE = "EXPORT SALES PRINT TEMPLATE";

        public const string SALES_UPLOAD_IMAGES = "SALES UPLOAD IMAGES";
        public const string SALES_IMAGE_PRINT_TEMPLATE = "SALES IMAGE PRINT TEMPLATE";

        public const string SALES_COMMISION_PROCESS_TYPE = "SALES COMMISION PROCESS TYPE";

        public const string SALES_HIGHLIGHT_CASH_CUSTOMER_BILLS = "SALES HIGHLIGHT CASH CUSTOMER BILLS";
        public const string SALES_BULK_LOOSE_CONTROL = "SALES BULK LOOSE CONTROL";
        public const string SALES_CALC_RATE_FROM_MRP = "SALES CALC RATE FROM MRP";

        public const string INFORM_ELIGIBLE_POINT_DISCOUNT_FOR_CUSTOMER = "INFORM ELIGIBLE POINT DISCOUNT FOR CUSTOMER";
        public const string CALCULATE_DECIMAL_IN_POINT = "CALCULATE DECIMAL IN POINT";
        public const string PRINT_CASH_HEAD_NAME = "PRINT CASH HEAD NAME";
        public const string ASK_REASON_FOR_CANCEL = "ASK REASON FOR CANCEL";
        public const string SALES_ENABLE_SALES_RETURN = "SALES ENABLE SALES RETURN";
        public const string SALES_BULK_DISC_TYPE = "SALES BULK DISC TYPE";
        public const string SALES_LIST_CUSTOMERS_AS_PER_GST_TYPE = "SALES LIST CUSTOMERS AS PER GST TYPE";
        public const string SALES_HIGHLIGHT_GST_B2C_BILLS = "SALES HIGHLIGHT GST B2C BILLS";
        public const string SALES_LOAD_PREVIOUS_ITEM_REMARKS_FOR_CUSTOMERS = "SALES LOAD PREVIOUS ITEM REMARKS FOR CUSTOMERS";
        public const string SALES_HOLD_SLIP_PRINT_TEMPLATE = "SALES HOLD SLIP PRINT TEMPLATE";
        public const string SALES_SUMMARIZE_ITEMS_WITH_SAME_DETAILS_ON_SAVE = "SALES SUMMARIZE ITEMS WITH SAME DETAILS ON SAVE";

        public const string SALES_PROMPT_EINVOICE_PRINT_ON_SAVE = "SALES PROMPT EINVOICE PRINT ON SAVE";
        public const string SALES_ENABLE_HIGHLIGHT_LOSS_SALES = "SALES ENABLE HIGHLIGHT LOSS SALES";
        public const string SALES_RETURN_MRP_EDIT = "SALES RETURN MRP EDIT";

        public const string ENABLE_SPL_RATE_CONTROL = "ENABLE SPL RATE CONTROL";

        public const string SALES_ENABLE_AUTO_SUGGEST_PACK_LORRY_MARK = "SALES ENABLE AUTO SUGGEST PACK LORRY MARK";

        public const string SALES_SAVE_AREACALC_IN_REMARKS = "SALES SAVE AREACALC IN REMARKS";
        public const string ENABLE_DISCOUNT_AMT_READ_ONLY_IN_WHOLESALE = "ENABLE DISCOUNT AMT READ ONLY IN WHOLESALE";

        public const string ENABLE_FOCUS_ON_LORRY = "ENABLE FOCUS ON LORRY";

        public const string CREDITNOTE_POSTING_TO_OUTPUTACC = "CREDITNOTE POSTING TO OUTPUTACC";

        public const string SALES_WHOLESALE_BARCODE_MODE_BILLING = "SALES WHOLESALE BARCODE MODE BILLING";
        public const string SALES_ENABLE_PASSCODE = "SALES ENABLE PASSCODE";

        public const string SALES_LOAD_PRICE_FROM_MASTER_RELOAD_FROM_HOLDLIST = "SALES LOAD PRICE FROM MASTER RELOAD FROM HOLDLIST";

        public const string EXCLUDE_ADMIN_FROM_SALESMAN_CHECKER_PICKER = "EXCLUDE ADMIN FROM SALESMAN CHECKER PICKER";

        public const string ENABLE_MASTER_DISCOUNT = "ENABLE MASTER DISCOUNT";
        public const string ENABLE_CUSTOMER_DISCOUNT = "ENABLE CUSTOMER DISCOUNT";
    }

    public enum KeyValueDataType
    {
        Text = 0,
        Bool = 1,
        Number = 2,
        Dates = 3
        // Binary = 4 // Uncomment if needed
    }


    public class KeyValueSetting
    {
        [Column("KEYNAME")]
        [DataMember]
        public string KeyName { get; set; }

        [Column("SECTION")]
        [DataMember]
        public string Section { get; set; }

        [Column("DESCRIPTION")]
        [DataMember]
        public string Description { get; set; }

        [Column("DATATYPE")]
        [DataMember]
        public KeyValueDataType DataType { get; set; }

        [Column("VALUE")]
        [DataMember]
        public object Value { get; set; }
    }

    public class DataMain
    {
        // Uncomment if needed: public inheritance
        // public class DataMain : ReportModel

        public long IncID { get; set; }
        public string IncName { get; set; }
        public decimal? IncSubValue { get; set; }
        public decimal? IncCashValue { get; set; }

        public long ExpID { get; set; }
        public string ExpName { get; set; }
        public decimal? ExpSubValue { get; set; }
        public decimal? ExpCashValue { get; set; }

        public long Id { get; set; }
        public long? SlNo { get; set; }
        public string DocNo { get; set; }
        public DateTime? DocDate { get; set; }
        public string Type { get; set; }
        public string Remarks { get; set; }
        public string Debit { get; set; }
        public string Credit { get; set; }
        public string Balance { get; set; }
        public int DaybookSeq { get; set; }
        public string TranType { get; set; }

        public decimal TotalShortExcess { get; set; }
        public decimal CashBalance { get; set; }
        public decimal StockAdjustmentAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitPercentage { get; set; }
    }

    public class StockJournalRegister
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public DateTime DocDate { get; set; }

        [DataMember]
        public string DocNo { get; set; }

        [DataMember]
        public string TransactionName { get; set; }

        [DataMember]
        public string CounterName { get; set; }

        [DataMember]
        public string User { get; set; }

        [DataMember]
        public string ProductName { get; set; }

        [DataMember]
        public string BatchNo { get; set; }

        [DataMember]
        public DateTime Expiry { get; set; }

        [DataMember]
        public string BatchCode { get; set; }

        [DataMember]
        public decimal Quantity { get; set; }

        [DataMember]
        public string UnitName { get; set; }

        [DataMember]
        public string GodownName { get; set; }

        [DataMember]
        public decimal Rate { get; set; }

        [DataMember]
        public decimal Cost { get; set; }
    }

  

    public class CollectionRpt
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string DocType { get; set; }

        [DataMember]
        public string DocNo { get; set; }

        [DataMember]
        public DateTime DocDate { get; set; }

        [DataMember]
        public string Narration { get; set; }

        [DataMember]
        public string Customer { get; set; }

        [DataMember]
        public decimal BillReceipt { get; set; }

        [DataMember]
        public decimal BillAmount { get; set; }

        [DataMember]
        public long CustomerID { get; set; }

        [DataMember]
        public long IsBillWise { get; set; }

        [DataMember]
        public long VoucherRefID { get; set; }

        [DataMember]
        public string VoucherRefTranType { get; set; }

        [DataMember]
        public decimal VoucherReceipt { get; set; }

        [DataMember]
        public string VoucherRefPayType { get; set; }

        [DataMember]
        public string VoucherChequeNo { get; set; }

        [DataMember]
        public List<BillwiseEntry> BillWiseEntries { get; set; }

        [DataMember]
        public DateTime VoucherChequeDate { get; set; }

        [DataMember]
        public string VoucherTrasnferNo { get; set; }

        [DataMember]
        public string CashCredit { get; set; }

        [DataMember]
        public int Delivered { get; set; }

        [DataMember]
        public DateTime DeliveryDate { get; set; }

        [DataMember]
        public long RepId { get; set; }

        [DataMember]
        public string Area { get; set; }
    }

    
    public class CashSummaryDtls
    {
        [DataMember]
        public decimal Bal { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int Seq { get; set; }

        [DataMember]
        public string CalcFlag { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public decimal Nos { get; set; }

        [DataMember]
        public decimal MakingCharge { get; set; }

        [DataMember]
        public decimal Value { get; set; }

        [DataMember]
        public string TranFlag { get; set; }

        [DataMember]
        public int OrderSeq { get; set; }
    }



    public class BillwiseEntry
    {
        [DataMember]
        public long Voucherid { get; set; }

        [DataMember]
        public long RowId { get; set; }

        [DataMember]
        public int Sequence { get; set; }

        [DataMember]
        public long AccountId { get; set; }

        [DataMember]
        public string AccountName { get; set; }

        [DataMember]
        public string Vouchertype { get; set; }

        [DataMember]
        public DateTime VoucherDate { get; set; }

        [DataMember]
        public long BillId { get; set; }

        [DataMember]
        public bool IsBill { get; set; }

        [DataMember]
        public string BillNo { get; set; }

        [DataMember]
        public decimal Debit { get; set; }

        [DataMember]
        public decimal Credit { get; set; }

        [DataMember]
        public DateTime DueDate { get; set; }

        [DataMember]
        public decimal BalanceAmount { get; set; }

        [DataMember]
        public decimal RcvdAmount { get; set; }

        [DataMember]
        public decimal CurrentReceipt { get; set; }

        [DataMember]
        public string TranType { get; set; }

        [DataMember]
        public string Remark { get; set; }

        [DataMember]
        public long RepId { get; set; }

        [DataMember]
        public long PromotorId { get; set; }

        [DataMember]
        public decimal PDCDebit { get; set; }

        [DataMember]
        public decimal PDCCredit { get; set; }

        [DataMember]
        public string RepName { get; set; }

        [DataMember]
        public string PromoName { get; set; }

        [DataMember]
        public long CounterID { get; set; }

        [DataMember]
        public long DaySessionID { get; set; }

        [DataMember]
        public long UserSessionID { get; set; }

        [DataMember]
        public long CounterSessionID { get; set; }

        [DataMember]
        public long FinYearID { get; set; }

        [DataMember]
        public decimal BillAmt { get; set; }

        [DataMember]
        public string BillTranType { get; set; }

        [DataMember]
        public DateTime BillDate { get; set; }

        [DataMember]
        public string BillWiseDocNo { get; set; }

        [DataMember]
        public bool BillWiseIsBill { get; set; }

        [DataMember]
        public string CreditNoteNo { get; set; }

        [DataMember]
        public string PrCrNo { get; set; }
    }


    public class BankCardAmtDtl
    {
        [DataMember]
        public string CardName { get; set; }

        [DataMember]
        public decimal CardAmt { get; set; }
    }

  
    public class DbPostCode
    {
        public string DBTAG { get; set; }       // Maps to DBPOSTCODES.DBTAG

        public string DBTYPE { get; set; }      // Maps to DBPOSTCODES.DBTYPE

        public string DBDESCRIPTION { get; set; }
        public long DBID { get; set; }          // Maps to DBPOSTCODES.DBID

        public string DBNAME { get; set; }
   
    }


    public static class Config
    {
        public static AppConfig AppSettings { get; set; } = GetConfig();

        private static AppConfig GetConfig()
        {
            string filePath = "config.json"; // Example config file
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<AppConfig>(json);
            }
            return new AppConfig(); // Default if file is missing
        }

        public static string CounterName { get; set; }

    }


    public class AppConfig
        {
            [Category("Server Settings")]
            public string ServerAddress { get; set; } = "";

            [Category("Server Settings")]
            public string LicenseNo { get; set; } = "";

            [Category("Server Settings")]
            public string CashDrawerPort { get; set; } = "";

            [Category("Server Settings")]
            public string Command { get; set; } = "";

            [Category("Local Settings")]
            public bool OfflineMode { get; set; } = false;

            [Browsable(false)]
            public long CounterId { get; set; } = 0;

            [Browsable(false)]
            public string PrimaryPrinter { get; set; } = "";

            [Browsable(false)]
            public string SecondaryPrinter { get; set; } = "";

            [Browsable(false)]
            public string PoleDisplayPort { get; set; } = "";

            [Browsable(false)]
            public string PoleDisplayTxt1 { get; set; } = "";

            [Browsable(false)]
            public string PoleDisplayTxt2 { get; set; } = "";

            [Browsable(false)]
            public long PoleDisplayCharPerRow { get; set; } = 0;

            [Browsable(false)]
            public long PoleDisplayNoofRows { get; set; } = 0;

            [Browsable(false)]
            public string CompanyName { get; set; } = "";

            [Browsable(false)]
            public string Caption { get; set; } = "";

            [Browsable(false)]
            public string Address1 { get; set; } = "";

            [Browsable(false)]
            public string Address2 { get; set; } = "";

            [Browsable(false)]
            public string Address3 { get; set; } = "";

            [Browsable(false)]
            public string Phone { get; set; } = "";

            [Browsable(false)]
            public string GSTIN { get; set; } = "";

            [Browsable(false)]
            public string Pincode { get; set; } = "";

            [Browsable(false)]
            public string BranchInvoiceApiUserName { get; set; } = "";

            [Browsable(false)]
            public string BranchInvoiceApiPassword { get; set; } = "";

            [Category("Company Settings")]
            public List<CompanySetting> SettingsList { get; set; } = new List<CompanySetting>();
        }


        public class CompanySetting
        {
            public string CompanyDBName { get; set; } = "";
            public long BillingCounterId { get; set; } = 0;
            public long GodownId { get; set; } = 0;
            public long GodownIdForBulk { get; set; } = 0;
            public long CounterId { get; set; } = 0;
            public long StoreId { get; set; } = 0;
            public string PrimaryPrinter { get; set; } = "";
            public string SecondaryPrinter { get; set; } = "";
            public string PoleDisplayPort { get; set; } = "";
            public long PoleDisplayCharPerRow { get; set; } = 0;
            public long PoleDisplayNoofRows { get; set; } = 0;
            public string PoleDisplayTxt1 { get; set; } = "";
            public string PoleDisplayTxt2 { get; set; } = "";
            public List<BillPrintSetting> BillPrintSettingList { get; set; } = new List<BillPrintSetting>();
            public bool RememberMe { get; set; } = false;
            public string UserName { get; set; } = "";
            public string Password { get; set; } = "";
            public string GeneralKeyboard { get; set; } = "";
            public string MalayalamKeyboard { get; set; } = "";
            public bool CashReceiptPromot { get; set; } = false;
            public bool BarcodeBillingFIFO { get; set; } = false;
            public bool RetailAutoHelp { get; set; } = false;
        }

        public class BillPrintSetting
        {
            public long CounterId { get; set; } = 0;
            public long SalesBillTypeID { get; set; } = 0;
            public long SalesBillPrintMode { get; set; } = 0;
            public string PrinterName { get; set; } = "";
        }

    public class Ledger
    {
        public long ID { get; set; }

        public DateTime DocDate { get; set; }

        public string DocNo { get; set; }

        public long Acid { get; set; }

        public string Vchtype { get; set; }

        public string Remarks { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public decimal Balance { get; set; }

        public decimal Opbal { get; set; }

        public int Mode { get; set; }

        public int Seq { get; set; }

        public string AccName { get; set; }

        public int DaybookSeq { get; set; }

        [Column("DBLEDGERREMARKS")]
        [DataMember]
        public string LedgerRemarks { get; set; }
    }
}


