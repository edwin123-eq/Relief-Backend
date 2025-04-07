using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Data;
//using System.Drawing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net;
using System.Text;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Models;
using Contracts;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Collections;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace ReliefApi.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    //[EnableCors("AllowOrigin")]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Guid("9245fe4a-d402-451c-b9ed-9c1a04247482")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [EnableCors()]
    public class DashBoardController : ControllerBase
    {
        private readonly IBranches _Branches;
        private readonly IConfiguration _configuration;
        private readonly UpiDetails _upiDetails;

        // Constructor Injection for IConfiguration
        public DashBoardController(IBranches branches, IConfiguration configuration, IOptions<UpiDetails> upiDetails)
        {
            _Branches = branches;
            _configuration = configuration;
            _upiDetails = upiDetails.Value;
        }




        //[HttpGet(Name = "GetSettings")]
        //public List<TestModel> Get(long count)
        //{
        //    List<TestModel> models = new List<TestModel>();

        //    var strTopFilter = count > 0 ? "TOP " + count.ToString() : "";

        //    var query = @"SELECT " + strTopFilter +  @"  PRDID, PRDNAME, PRODUCTS.PRDCODE, PRODUCTS.PRDMFRID, PRODUCTS.PRDBRANDID, UNIT.UNITNAME , PRODUCTS.PRDTAXID,
        //    TRDETAILS.DETDATE, TRDETAILS.DETDOCNO, TRDETAILS.DETQTY, TRDETAILS.DETRATE, TRDETAILS.DETQTY* TRDETAILS.DETRATE AS AMOUNT
        //    FROM PRODUCTS INNER JOIN UNIT ON PRODUCTS.PRDUNIT = UNIT.UNITID
        //                  LEFT JOIN TRDETAILS ON PRODUCTS.PRDID = TRDETAILS.DETPRDID
        //                  ORDER BY PRDID"
        //    ;

        //    models= this._dappercontext.CreateConnection().Query< TestModel>(query).ToList();

        //    return models;
        //}

        [HttpGet("GETLMTD")]
        public async Task<LMTDDates> GETLMTD(DateTimeOffset Date)
        {
            var Today = Date;
            var lastMonth = Today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(Today.Year, lastMonth.Month, 1);
            var LMTDEnd = new DateTime(Today.Year, lastMonth.Month, lastMonth.Day);

            LMTDDates ListOfDate = new LMTDDates();
            ListOfDate.LMTDstart = LMTDStart;
            ListOfDate.LMTDend = LMTDEnd.AddDays(1).AddMilliseconds(-1);

            return ListOfDate;
        }


        [HttpGet("getBranchList")]
        public async Task<List<Branch>> getBranchList(bool All, long StateId)
        {
            List<Branch> branchList = new List<Branch>();
            branchList = await this._Branches.List(All, StateId);
            branchList = branchList.OrderBy(y => y.Id).ToList();

            return branchList;
        }

        [HttpGet("Bouncelist")]
        public async Task<List<BounceModel>> Bouncelist(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            var today = Date;
            string varbranch = string.Join(",", Branches);

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Fifteendaysback = TodayStart.AddDays(-15);
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);
            List<bounce> BounceList = await _Branches.Bouncelist(Fifteendaysback, Todayend, varbranch);
            List<BounceModel> BounceModelList = new List<BounceModel>();
            var categoryMap = new Dictionary<string, BounceModel>();

            foreach (var item in BounceList)
            {
                var categoryList = new BounceModel
                {
                    product = item.PRDNAME,
                    Amount = item.BCDIFFQTY,
                };
                categoryMap[item.PRDNAME] = categoryList;
            }

            BounceModelList = categoryMap.Values.ToList();

            return BounceModelList;
        }


        [HttpGet("getCategoryList")]
        public async Task<List<Category>> getCategoryList()
        {
            List<Category> CategoryList = new List<Category>();
            CategoryList = await this._Branches.CategoryList();


            return CategoryList;
        }

        [HttpGet("getFinyearList")]
        public async Task<Finyear> getFinyearList()
        {
            Finyear MaxAndMin = new Finyear();

            List<Finyear> FinyearList = new List<Finyear>();
            FinyearList = await this._Branches.FinYearList();

            if (FinyearList != null)
            {
                MaxAndMin.FINSTART = FinyearList.Min(y => y.FINSTART);
                MaxAndMin.FINEND = FinyearList.Max(y => y.FINEND);
            }



            return MaxAndMin;
        }

        [HttpGet("SalesGraph")]
        public async Task<List<Graph>> SalesGraph(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            Finyear FinYear = await this._Branches.GetFinYear();

            string varbranch = string.Join(",", Branches);

            var TwelvemonthBeforetoday = Date.AddMonths(-11);

            DateTimeOffset TodayStart = Date.Date;
            DateTimeOffset Todayend = Date.Date.AddDays(1).AddMilliseconds(-1);



            bool DontCheck = false;

            List<Graph> GraphList = new List<Graph>();

            var MonthStart = new DateTime(TwelvemonthBeforetoday.Year, TwelvemonthBeforetoday.Month, 1);
            var YearStart = FinYear.FINSTART.Value;
            var FirstmonthStart = MonthStart;
            var FirstmonthEnd = MonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary FirstValue = await this._Branches.SaleGraph(FirstmonthStart, FirstmonthEnd, varbranch);
            Graph FirstGraph = new Graph();
            string Firstmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[MonthStart.Month - 1].ToUpper();
            FirstGraph.Month = Firstmonth;
            if (FirstValue != null)
            {
                FirstGraph.Amount = FirstValue.Amount;
            }

            GraphList.Add(FirstGraph);


            var SecondmonthStart = MonthStart.AddMonths(1);
            var SecondmonthEnd = SecondmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary SeondValue = await this._Branches.SaleGraph(SecondmonthStart, SecondmonthEnd, varbranch);
            Graph SecondGraph = new Graph();
            string Secondmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[SecondmonthStart.Month - 1].ToUpper();
            SecondGraph.Month = Secondmonth;
            if (SeondValue != null)
            {
                SecondGraph.Amount = SeondValue.Amount;
            }

            GraphList.Add(SecondGraph);


            var ThirdmonthStart = MonthStart.AddMonths(2);
            var ThirdmonthEnd = ThirdmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary ThirdValue = await this._Branches.SaleGraph(ThirdmonthStart, ThirdmonthEnd, varbranch);
            Graph ThirdGraph = new Graph();
            string Thirdmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[ThirdmonthStart.Month - 1].ToUpper();
            ThirdGraph.Month = Thirdmonth;
            if (ThirdValue != null)
            {
                ThirdGraph.Amount = ThirdValue.Amount;
            }

            GraphList.Add(ThirdGraph);

            var ForthmonthStart = MonthStart.AddMonths(3);
            var ForthmonthEnd = ForthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary ForthValue = await this._Branches.SaleGraph(ForthmonthStart, ForthmonthEnd, varbranch);
            Graph ForthGraph = new Graph();
            string Forthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[ForthmonthStart.Month - 1].ToUpper();
            ForthGraph.Month = Forthmonth;
            if (ForthValue != null)
            {
                ForthGraph.Amount = ForthValue.Amount;
            }

            GraphList.Add(ForthGraph);

            var FifthmonthStart = MonthStart.AddMonths(4);
            var FifthmonthEnd = FifthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary FifthValue = await this._Branches.SaleGraph(FifthmonthStart, FifthmonthEnd, varbranch);
            Graph FifthGraph = new Graph();
            string Fifthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[FifthmonthStart.Month - 1].ToUpper();
            FifthGraph.Month = Fifthmonth;
            if (FifthValue != null)
            {
                FifthGraph.Amount = FifthValue.Amount;
            }

            GraphList.Add(FifthGraph);


            var SixthmonthStart = MonthStart.AddMonths(5);
            var SixthmonthEnd = SixthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary SixthValue = await this._Branches.SaleGraph(SixthmonthStart, SixthmonthEnd, varbranch);
            Graph SixthGraph = new Graph();
            string Sixthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[SixthmonthStart.Month - 1].ToUpper();
            SixthGraph.Month = Sixthmonth;
            if (SixthValue != null)
            {
                SixthGraph.Amount = SixthValue.Amount;
            }

            GraphList.Add(SixthGraph);


            var SeventhmonthStart = MonthStart.AddMonths(6);
            var SeventhmonthEnd = SeventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary SeventhValue = await this._Branches.SaleGraph(SeventhmonthStart, SeventhmonthEnd, varbranch);
            Graph SeventhGraph = new Graph();
            string Seventhmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[SeventhmonthStart.Month - 1].ToUpper();
            SeventhGraph.Month = Seventhmonth;
            if (SeventhValue != null)
            {
                SeventhGraph.Amount = SeventhValue.Amount;
            }

            GraphList.Add(SeventhGraph);


            var EighthmonthStart = MonthStart.AddMonths(7);
            var EighthmonthEnd = EighthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary EighthValue = await this._Branches.SaleGraph(EighthmonthStart, EighthmonthEnd, varbranch);
            Graph EighthGraph = new Graph();
            string Eighthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[EighthmonthStart.Month - 1].ToUpper();
            EighthGraph.Month = Eighthmonth;
            if (EighthValue != null)
            {
                EighthGraph.Amount = EighthValue.Amount;
            }

            GraphList.Add(EighthGraph);


            var NinethmonthStart = MonthStart.AddMonths(8);
            var NinethmonthEnd = NinethmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary NinethValue = await this._Branches.SaleGraph(NinethmonthStart, NinethmonthEnd, varbranch);
            Graph NinethGraph = new Graph();
            string Ninethmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[NinethmonthStart.Month - 1].ToUpper();
            NinethGraph.Month = Ninethmonth;
            if (NinethValue != null)
            {
                NinethGraph.Amount = NinethValue.Amount;
            }

            GraphList.Add(NinethGraph);


            var TenthmonthStart = MonthStart.AddMonths(9);
            var TenthmonthEnd = TenthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary TenthValue = await this._Branches.SaleGraph(TenthmonthStart, TenthmonthEnd, varbranch);
            Graph TenthGraph = new Graph();
            string Tenthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[TenthmonthStart.Month - 1].ToUpper();
            TenthGraph.Month = Tenthmonth;
            if (TenthValue != null)
            {
                TenthGraph.Amount = TenthValue.Amount;
            }

            GraphList.Add(TenthGraph);


            var EleventhmonthStart = MonthStart.AddMonths(10);
            var EleventhmonthEnd = EleventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary EleventhValue = await this._Branches.SaleGraph(EleventhmonthStart, EleventhmonthEnd, varbranch);
            Graph EleventhGraph = new Graph();
            string Eleventhmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[EleventhmonthStart.Month - 1].ToUpper();
            EleventhGraph.Month = Eleventhmonth;
            if (EleventhValue != null)
            {
                EleventhGraph.Amount = EleventhValue.Amount;
            }

            GraphList.Add(EleventhGraph);


            var TwelvethmonthStart = MonthStart.AddMonths(11);
            var TwelvethmonthEnd = Todayend;

            TotalSummary TwelvethValue = await this._Branches.SaleGraph(TwelvethmonthStart, TwelvethmonthEnd, varbranch);
            Graph TwelvethGraph = new Graph();
            string Twelvethmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[TwelvethmonthStart.Month - 1].ToUpper();
            TwelvethGraph.Month = Twelvethmonth;
            if (TwelvethValue != null)
            {
                TwelvethGraph.Amount = TwelvethValue.Amount;
            }

            GraphList.Add(TwelvethGraph);




            return GraphList;


            //if(DontCheck!=true)
            //{
            //    if (FirstmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary FirstValue = await this._Branches.SaleGraph(FirstmonthStart, FirstmonthEnd, varbranch);
            //        Graph FirstGraph = new Graph();
            //        FirstGraph.Month = "APR";
            //        if (FirstValue != null)
            //        {
            //            FirstGraph.Amount = FirstValue.Amount;
            //        }

            //        GraphList.Add(FirstGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}




            //var SecondmonthStart = YearStart.AddMonths(1);
            //var SecondmonthEnd = SecondmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (SecondmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary SecondValue = await this._Branches.SaleGraph(SecondmonthStart, SecondmonthEnd, varbranch);
            //        Graph SecondGraph = new Graph();
            //        SecondGraph.Month = "MAY";
            //        if (SecondValue != null)
            //        {
            //            SecondGraph.Amount = SecondValue.Amount;
            //        }

            //        GraphList.Add(SecondGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}





            //var ThirdmonthStart = SecondmonthStart.AddMonths(1);
            //var ThirdmonthEnd = ThirdmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (ThirdmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary ThirdValue = await this._Branches.SaleGraph(ThirdmonthStart, ThirdmonthEnd, varbranch);
            //        Graph ThirdGraph = new Graph();
            //        ThirdGraph.Month = "JUN";
            //        if (ThirdValue != null)
            //        {
            //            ThirdGraph.Amount = ThirdValue.Amount;
            //        }

            //        GraphList.Add(ThirdGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var ForthmonthStart = ThirdmonthStart.AddMonths(1);
            //var ForthmonthEnd = ForthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (ForthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary ForthValue = await this._Branches.SaleGraph(ForthmonthStart, ForthmonthEnd, varbranch);
            //        Graph ForthGraph = new Graph();
            //        ForthGraph.Month = "JUL";
            //        if (ForthValue != null)
            //        {
            //            ForthGraph.Amount = ForthValue.Amount;
            //        }

            //        GraphList.Add(ForthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}




            //var FifthmonthStart = ForthmonthStart.AddMonths(1);
            //var FifthmonthEnd = FifthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (FifthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary FifthValue = await this._Branches.SaleGraph(FifthmonthStart, FifthmonthEnd, varbranch);
            //        Graph FifthGraph = new Graph();
            //        FifthGraph.Month = "AUG";
            //        if (FifthValue != null)
            //        {
            //            FifthGraph.Amount = FifthValue.Amount;
            //        }

            //        GraphList.Add(FifthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }

            //}




            //var SixthmonthStart = FifthmonthStart.AddMonths(1);
            //var SixthmonthEnd = SixthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (SixthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary SixthValue = await this._Branches.SaleGraph(SixthmonthStart, SixthmonthEnd, varbranch);
            //        Graph SixthGraph = new Graph();
            //        SixthGraph.Month = "SEP";
            //        if (SixthValue != null)
            //        {
            //            SixthGraph.Amount = SixthValue.Amount;
            //        }

            //        GraphList.Add(SixthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}





            //var SeventhmonthStart = SixthmonthStart.AddMonths(1);
            //var SeventhmonthEnd = SeventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (SeventhmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary SeventhValue = await this._Branches.SaleGraph(SeventhmonthStart, SeventhmonthEnd, varbranch);
            //        Graph SeventhGraph = new Graph();
            //        SeventhGraph.Month = "OCT";
            //        if (SeventhValue != null)
            //        {
            //            SeventhGraph.Amount = SeventhValue.Amount;
            //        }

            //        GraphList.Add(SeventhGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var EighthmonthStart = SeventhmonthStart.AddMonths(1);
            //var EighthmonthEnd = EighthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (EighthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary EighthValue = await this._Branches.SaleGraph(EighthmonthStart, EighthmonthEnd, varbranch);
            //        Graph EighthGraph = new Graph();
            //        EighthGraph.Month = "NOV";
            //        if (EighthValue != null)
            //        {
            //            EighthGraph.Amount = EighthValue.Amount;
            //        }

            //        GraphList.Add(EighthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }

            //}





            //var NinethmonthStart = EighthmonthStart.AddMonths(1);
            //var NinethmonthEnd = NinethmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (NinethmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary NinethValue = await this._Branches.SaleGraph(NinethmonthStart, NinethmonthEnd, varbranch);
            //        Graph NinethGraph = new Graph();
            //        NinethGraph.Month = "DEC";
            //        if (NinethValue != null)
            //        {
            //            NinethGraph.Amount = NinethValue.Amount;
            //        }

            //        GraphList.Add(NinethGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var TenthmonthStart = NinethmonthStart.AddMonths(1);
            //var TenthmonthEnd = TenthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (TenthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary TenthValue = await this._Branches.SaleGraph(TenthmonthStart, TenthmonthEnd, varbranch);
            //        Graph TenthGraph = new Graph();
            //        TenthGraph.Month = "JAN";
            //        if (TenthValue != null)
            //        {
            //            TenthGraph.Amount = TenthValue.Amount;
            //        }

            //        GraphList.Add(TenthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var EleventhmonthStart = TenthmonthStart.AddMonths(1);
            //var EleventhmonthEnd = EleventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (EleventhmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary EleventhValue = await this._Branches.SaleGraph(EleventhmonthStart, EleventhmonthEnd, varbranch);
            //        Graph EleventhGraph = new Graph();
            //        EleventhGraph.Month = "FEB";
            //        if (EleventhValue != null)
            //        {
            //            EleventhGraph.Amount = EleventhValue.Amount;
            //        }

            //        GraphList.Add(EleventhGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}







            //var TwelvethmonthStart = EleventhmonthStart.AddMonths(1);
            //var TwelvethmonthEnd = TwelvethmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (TwelvethmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary TwelvethValue = await this._Branches.SaleGraph(TwelvethmonthStart, TwelvethmonthEnd, varbranch);
            //        Graph TwelvethGraph = new Graph();
            //        TwelvethGraph.Month = "MAR";
            //        if (TwelvethValue != null)
            //        {
            //            TwelvethGraph.Amount = TwelvethValue.Amount;
            //        }

            //        GraphList.Add(TwelvethGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}








        }


        [HttpGet("PurchaseGraph")]
        public async Task<List<Graph>> PurchaseGraph(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {



            Finyear FinYear = await this._Branches.GetFinYear();

            string varbranch = string.Join(",", Branches);

            var TwelvemonthBeforetoday = Date.AddMonths(-11);

            DateTimeOffset TodayStart = Date.Date;
            DateTimeOffset Todayend = Date.Date.AddDays(1).AddMilliseconds(-1);



            bool DontCheck = false;

            List<Graph> GraphList = new List<Graph>();

            var MonthStart = new DateTime(TwelvemonthBeforetoday.Year, TwelvemonthBeforetoday.Month, 1);
            var YearStart = FinYear.FINSTART.Value;
            var FirstmonthStart = MonthStart;
            var FirstmonthEnd = MonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary FirstValue = await this._Branches.PurchaseGraph(FirstmonthStart, FirstmonthEnd, varbranch);
            Graph FirstGraph = new Graph();
            string Firstmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[MonthStart.Month - 1].ToUpper();
            FirstGraph.Month = Firstmonth;
            if (FirstValue != null)
            {
                FirstGraph.Amount = FirstValue.Amount;
            }

            GraphList.Add(FirstGraph);


            var SecondmonthStart = MonthStart.AddMonths(1);
            var SecondmonthEnd = SecondmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary SeondValue = await this._Branches.PurchaseGraph(SecondmonthStart, SecondmonthEnd, varbranch);
            Graph SecondGraph = new Graph();
            string Secondmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[SecondmonthStart.Month - 1].ToUpper();
            SecondGraph.Month = Secondmonth;
            if (SeondValue != null)
            {
                SecondGraph.Amount = SeondValue.Amount;
            }

            GraphList.Add(SecondGraph);


            var ThirdmonthStart = MonthStart.AddMonths(2);
            var ThirdmonthEnd = ThirdmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary ThirdValue = await this._Branches.PurchaseGraph(ThirdmonthStart, ThirdmonthEnd, varbranch);
            Graph ThirdGraph = new Graph();
            string Thirdmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[ThirdmonthStart.Month - 1].ToUpper();
            ThirdGraph.Month = Thirdmonth;
            if (ThirdValue != null)
            {
                ThirdGraph.Amount = ThirdValue.Amount;
            }

            GraphList.Add(ThirdGraph);

            var ForthmonthStart = MonthStart.AddMonths(3);
            var ForthmonthEnd = ForthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary ForthValue = await this._Branches.PurchaseGraph(ForthmonthStart, ForthmonthEnd, varbranch);
            Graph ForthGraph = new Graph();
            string Forthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[ForthmonthStart.Month - 1].ToUpper();
            ForthGraph.Month = Forthmonth;
            if (ForthValue != null)
            {
                ForthGraph.Amount = ForthValue.Amount;
            }

            GraphList.Add(ForthGraph);

            var FifthmonthStart = MonthStart.AddMonths(4);
            var FifthmonthEnd = FifthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary FifthValue = await this._Branches.PurchaseGraph(FifthmonthStart, FifthmonthEnd, varbranch);
            Graph FifthGraph = new Graph();
            string Fifthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[FifthmonthStart.Month - 1].ToUpper();
            FifthGraph.Month = Fifthmonth;
            if (FifthValue != null)
            {
                FifthGraph.Amount = FifthValue.Amount;
            }

            GraphList.Add(FifthGraph);


            var SixthmonthStart = MonthStart.AddMonths(5);
            var SixthmonthEnd = SixthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary SixthValue = await this._Branches.PurchaseGraph(SixthmonthStart, SixthmonthEnd, varbranch);
            Graph SixthGraph = new Graph();
            string Sixthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[SixthmonthStart.Month - 1].ToUpper();
            SixthGraph.Month = Sixthmonth;
            if (SixthValue != null)
            {
                SixthGraph.Amount = SixthValue.Amount;
            }

            GraphList.Add(SixthGraph);


            var SeventhmonthStart = MonthStart.AddMonths(6);
            var SeventhmonthEnd = SeventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary SeventhValue = await this._Branches.PurchaseGraph(SeventhmonthStart, SeventhmonthEnd, varbranch);
            Graph SeventhGraph = new Graph();
            string Seventhmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[SeventhmonthStart.Month - 1].ToUpper();
            SeventhGraph.Month = Seventhmonth;
            if (SeventhValue != null)
            {
                SeventhGraph.Amount = SeventhValue.Amount;
            }

            GraphList.Add(SeventhGraph);


            var EighthmonthStart = MonthStart.AddMonths(7);
            var EighthmonthEnd = EighthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary EighthValue = await this._Branches.PurchaseGraph(EighthmonthStart, EighthmonthEnd, varbranch);
            Graph EighthGraph = new Graph();
            string Eighthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[EighthmonthStart.Month - 1].ToUpper();
            EighthGraph.Month = Eighthmonth;
            if (EighthValue != null)
            {
                EighthGraph.Amount = EighthValue.Amount;
            }

            GraphList.Add(EighthGraph);


            var NinethmonthStart = MonthStart.AddMonths(8);
            var NinethmonthEnd = NinethmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary NinethValue = await this._Branches.PurchaseGraph(NinethmonthStart, NinethmonthEnd, varbranch);
            Graph NinethGraph = new Graph();
            string Ninethmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[NinethmonthStart.Month - 1].ToUpper();
            NinethGraph.Month = Ninethmonth;
            if (NinethValue != null)
            {
                NinethGraph.Amount = NinethValue.Amount;
            }

            GraphList.Add(NinethGraph);


            var TenthmonthStart = MonthStart.AddMonths(9);
            var TenthmonthEnd = TenthmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary TenthValue = await this._Branches.PurchaseGraph(TenthmonthStart, TenthmonthEnd, varbranch);
            Graph TenthGraph = new Graph();
            string Tenthmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[TenthmonthStart.Month - 1].ToUpper();
            TenthGraph.Month = Tenthmonth;
            if (TenthValue != null)
            {
                TenthGraph.Amount = TenthValue.Amount;
            }

            GraphList.Add(TenthGraph);


            var EleventhmonthStart = MonthStart.AddMonths(10);
            var EleventhmonthEnd = EleventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            TotalSummary EleventhValue = await this._Branches.PurchaseGraph(EleventhmonthStart, EleventhmonthEnd, varbranch);
            Graph EleventhGraph = new Graph();
            string Eleventhmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[EleventhmonthStart.Month - 1].ToUpper();
            EleventhGraph.Month = Eleventhmonth;
            if (EleventhValue != null)
            {
                EleventhGraph.Amount = EleventhValue.Amount;
            }

            GraphList.Add(EleventhGraph);


            var TwelvethmonthStart = MonthStart.AddMonths(11);
            var TwelvethmonthEnd = Todayend;

            TotalSummary TwelvethValue = await this._Branches.PurchaseGraph(TwelvethmonthStart, TwelvethmonthEnd, varbranch);
            Graph TwelvethGraph = new Graph();
            string Twelvethmonth = System.Globalization.DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[TwelvethmonthStart.Month - 1].ToUpper();
            TwelvethGraph.Month = Twelvethmonth;
            if (TwelvethValue != null)
            {
                TwelvethGraph.Amount = TwelvethValue.Amount;
            }

            GraphList.Add(TwelvethGraph);




            return GraphList;


            //Finyear FinYear = await this._Branches.GetFinYear();

            //string varbranch = string.Join(",", Branches);

            //var today = Date;

            //DateTimeOffset TodayStart = today.Date;
            //DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);



            //bool DontCheck = false;

            //List<Graph> GraphList = new List<Graph>();

            //var MonthStart = new DateTime(today.Year, today.Month, 1);
            //var YearStart = FinYear.FINSTART.Value;
            //var FirstmonthStart = YearStart;
            //var FirstmonthEnd = YearStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (FirstmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary FirstValue = await this._Branches.PurchaseGraph(FirstmonthStart, FirstmonthEnd, varbranch);
            //        Graph FirstGraph = new Graph();
            //        FirstGraph.Month = "APR";
            //        if (FirstValue != null)
            //        {
            //            FirstGraph.Amount = FirstValue.Amount;
            //        }

            //        GraphList.Add(FirstGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}




            //var SecondmonthStart = YearStart.AddMonths(1);
            //var SecondmonthEnd = SecondmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (SecondmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary SecondValue = await this._Branches.PurchaseGraph(SecondmonthStart, SecondmonthEnd, varbranch);
            //        Graph SecondGraph = new Graph();
            //        SecondGraph.Month = "MAY";
            //        if (SecondValue != null)
            //        {
            //            SecondGraph.Amount = SecondValue.Amount;
            //        }

            //        GraphList.Add(SecondGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}





            //var ThirdmonthStart = SecondmonthStart.AddMonths(1);
            //var ThirdmonthEnd = ThirdmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (ThirdmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary ThirdValue = await this._Branches.PurchaseGraph(ThirdmonthStart, ThirdmonthEnd, varbranch);
            //        Graph ThirdGraph = new Graph();
            //        ThirdGraph.Month = "JUN";
            //        if (ThirdValue != null)
            //        {
            //            ThirdGraph.Amount = ThirdValue.Amount;
            //        }

            //        GraphList.Add(ThirdGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var ForthmonthStart = ThirdmonthStart.AddMonths(1);
            //var ForthmonthEnd = ForthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (ForthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary ForthValue = await this._Branches.PurchaseGraph(ForthmonthStart, ForthmonthEnd, varbranch);
            //        Graph ForthGraph = new Graph();
            //        ForthGraph.Month = "JUL";
            //        if (ForthValue != null)
            //        {
            //            ForthGraph.Amount = ForthValue.Amount;
            //        }

            //        GraphList.Add(ForthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}




            //var FifthmonthStart = ForthmonthStart.AddMonths(1);
            //var FifthmonthEnd = FifthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (FifthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary FifthValue = await this._Branches.PurchaseGraph(FifthmonthStart, FifthmonthEnd, varbranch);
            //        Graph FifthGraph = new Graph();
            //        FifthGraph.Month = "AUG";
            //        if (FifthValue != null)
            //        {
            //            FifthGraph.Amount = FifthValue.Amount;
            //        }

            //        GraphList.Add(FifthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }

            //}




            //var SixthmonthStart = FifthmonthStart.AddMonths(1);
            //var SixthmonthEnd = SixthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (SixthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary SixthValue = await this._Branches.PurchaseGraph(SixthmonthStart, SixthmonthEnd, varbranch);
            //        Graph SixthGraph = new Graph();
            //        SixthGraph.Month = "SEP";
            //        if (SixthValue != null)
            //        {
            //            SixthGraph.Amount = SixthValue.Amount;
            //        }

            //        GraphList.Add(SixthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}





            //var SeventhmonthStart = SixthmonthStart.AddMonths(1);
            //var SeventhmonthEnd = SeventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (SeventhmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary SeventhValue = await this._Branches.PurchaseGraph(SeventhmonthStart, SeventhmonthEnd, varbranch);
            //        Graph SeventhGraph = new Graph();
            //        SeventhGraph.Month = "OCT";
            //        if (SeventhValue != null)
            //        {
            //            SeventhGraph.Amount = SeventhValue.Amount;
            //        }

            //        GraphList.Add(SeventhGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var EighthmonthStart = SeventhmonthStart.AddMonths(1);
            //var EighthmonthEnd = EighthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (EighthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary EighthValue = await this._Branches.PurchaseGraph(EighthmonthStart, EighthmonthEnd, varbranch);
            //        Graph EighthGraph = new Graph();
            //        EighthGraph.Month = "NOV";
            //        if (EighthValue != null)
            //        {
            //            EighthGraph.Amount = EighthValue.Amount;
            //        }

            //        GraphList.Add(EighthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }

            //}





            //var NinethmonthStart = EighthmonthStart.AddMonths(1);
            //var NinethmonthEnd = NinethmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (NinethmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary NinethValue = await this._Branches.PurchaseGraph(NinethmonthStart, NinethmonthEnd, varbranch);
            //        Graph NinethGraph = new Graph();
            //        NinethGraph.Month = "DEC";
            //        if (NinethValue != null)
            //        {
            //            NinethGraph.Amount = NinethValue.Amount;
            //        }

            //        GraphList.Add(NinethGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var TenthmonthStart = NinethmonthStart.AddMonths(1);
            //var TenthmonthEnd = TenthmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (TenthmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary TenthValue = await this._Branches.PurchaseGraph(TenthmonthStart, TenthmonthEnd, varbranch);
            //        Graph TenthGraph = new Graph();
            //        TenthGraph.Month = "JAN";
            //        if (TenthValue != null)
            //        {
            //            TenthGraph.Amount = TenthValue.Amount;
            //        }

            //        GraphList.Add(TenthGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //var EleventhmonthStart = TenthmonthStart.AddMonths(1);
            //var EleventhmonthEnd = EleventhmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (EleventhmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary EleventhValue = await this._Branches.PurchaseGraph(EleventhmonthStart, EleventhmonthEnd, varbranch);
            //        Graph EleventhGraph = new Graph();
            //        EleventhGraph.Month = "FEB";
            //        if (EleventhValue != null)
            //        {
            //            EleventhGraph.Amount = EleventhValue.Amount;
            //        }

            //        GraphList.Add(EleventhGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}







            //var TwelvethmonthStart = EleventhmonthStart.AddMonths(1);
            //var TwelvethmonthEnd = TwelvethmonthStart.AddMonths(1).AddMilliseconds(-1);

            //if (DontCheck != true)
            //{
            //    if (TwelvethmonthStart.Date <= Date.Date)
            //    {
            //        TotalSummary TwelvethValue = await this._Branches.PurchaseGraph(TwelvethmonthStart, TwelvethmonthEnd, varbranch);
            //        Graph TwelvethGraph = new Graph();
            //        TwelvethGraph.Month = "MAR";
            //        if (TwelvethValue != null)
            //        {
            //            TwelvethGraph.Amount = TwelvethValue.Amount;
            //        }

            //        GraphList.Add(TwelvethGraph);
            //    }
            //    else
            //    {
            //        DontCheck = true;
            //    }
            //}






            //return GraphList;

        }



        [HttpGet("GetSalesProfit")]
        public async Task<List<Profit>> GetSalesProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            // Finyear  FinYear = await this._Branches.GetFinYear();

            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);

            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            LMTDDates Dates = await GETLMTD(Date);


            var LMTDStart = new DateTime(today.Year, lastMonth.Month, 1);
            var LMTDEND = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);


            TotalSummary SaleTodayAmount = await this._Branches.TotalAmount(TodayStart, Todayend, varbranch, "SALES");
            TotalSummary SaleTodayGP = await this._Branches.GPPercent(TodayStart, Todayend, varbranch, "SALES");

            TotalSummary SaleMTDamount = await this._Branches.TotalAmount(MonthStart, Todayend, varbranch, "SALES");


            TotalSummary SaleYTDamount = await this._Branches.TotalAmount(YearStart, Todayend, varbranch, "SALES");


            TotalSummary SaleLMTDamount = await this._Branches.TotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALES");


            TotalSummary SalePreyearamount = await this._Branches.TotalAmount(PreyearStart, PreyearEnd, varbranch, "SALES");

            Profit Saleprofits = new Profit();

            Saleprofits.Type = "SALES";
            Saleprofits.Today = SaleTodayAmount.Amount;
            Saleprofits.GpRate = SaleTodayGP.GpdivAmt > 0 ? ((SaleTodayGP.GpAmt - SaleTodayGP.GpdivAmt) / SaleTodayGP.GpdivAmt) * 100 : 0;
            Saleprofits.MTD = SaleMTDamount.Amount;
            Saleprofits.YTD = SaleYTDamount.Amount;
            Saleprofits.LMTD = SaleLMTDamount.Amount;
            Saleprofits.PrevYear = SalePreyearamount.Amount;



            //B2C

            TotalSummary B2CTodayAmount = await this._Branches.TotalAmount(TodayStart, Todayend, varbranch, "B2C");
            TotalSummary B2CTodayGP = await this._Branches.GPPercent(TodayStart, Todayend, varbranch, "B2C");

            TotalSummary B2CMTDamount = await this._Branches.TotalAmount(MonthStart, Todayend, varbranch, "B2C");


            TotalSummary B2CYTDamount = await this._Branches.TotalAmount(YearStart, Todayend, varbranch, "B2C");


            TotalSummary B2CLMTDamount = await this._Branches.TotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2C");


            TotalSummary B2CPreyearamount = await this._Branches.TotalAmount(PreyearStart, PreyearEnd, varbranch, "B2C");

            Profit B2Cprofits = new Profit();

            B2Cprofits.Type = "B2C";
            B2Cprofits.Today = B2CTodayAmount.Amount;
            B2Cprofits.GpRate = B2CTodayGP.GpdivAmt > 0 ? ((B2CTodayGP.GpAmt - B2CTodayGP.GpdivAmt) / B2CTodayGP.GpdivAmt) * 100 : 0;
            B2Cprofits.MTD = B2CMTDamount.Amount;
            B2Cprofits.YTD = B2CYTDamount.Amount;
            B2Cprofits.LMTD = B2CLMTDamount.Amount;
            B2Cprofits.PrevYear = B2CPreyearamount.Amount;




            //B2B

            TotalSummary B2BTodayAmount = await this._Branches.TotalAmount(TodayStart, Todayend, varbranch, "B2B");

            TotalSummary B2BTodayGP = await this._Branches.GPPercent(TodayStart, Todayend, varbranch, "B2B");

            TotalSummary B2BMTDamount = await this._Branches.TotalAmount(MonthStart, Todayend, varbranch, "B2B");


            TotalSummary B2BYTDamount = await this._Branches.TotalAmount(YearStart, Todayend, varbranch, "B2B");


            TotalSummary B2BLMTDamount = await this._Branches.TotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2B");


            TotalSummary B2BPreyearamount = await this._Branches.TotalAmount(PreyearStart, PreyearEnd, varbranch, "B2B");

            Profit B2Bprofits = new Profit();

            B2Bprofits.Type = "B2B";
            B2Bprofits.Today = B2BTodayAmount.Amount;
            B2Bprofits.GpRate = B2BTodayGP.GpdivAmt > 0 ? ((B2BTodayGP.GpAmt - B2BTodayGP.GpdivAmt) / B2BTodayGP.GpdivAmt) * 100 : 0;
            B2Bprofits.MTD = B2BMTDamount.Amount;
            B2Bprofits.YTD = B2BYTDamount.Amount;
            B2Bprofits.LMTD = B2BLMTDamount.Amount;
            B2Bprofits.PrevYear = B2BPreyearamount.Amount;



            //SALESRETURNB2C

            TotalSummary SALESRETURNB2CTodayAmount = await this._Branches.TotalAmount(TodayStart, Todayend, varbranch, "SALESRETURNB2C");


            TotalSummary SALESRETURNB2CMTDamount = await this._Branches.TotalAmount(MonthStart, Todayend, varbranch, "SALESRETURNB2C");


            TotalSummary SALESRETURNB2CYTDamount = await this._Branches.TotalAmount(YearStart, Todayend, varbranch, "SALESRETURNB2C");


            TotalSummary SALESRETURNB2CLMTDamount = await this._Branches.TotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALESRETURNB2C");


            TotalSummary SALESRETURNB2CPreyearamount = await this._Branches.TotalAmount(PreyearStart, PreyearEnd, varbranch, "SALESRETURNB2C");

            Profit SALESRETURNB2Cprofits = new Profit();

            SALESRETURNB2Cprofits.Type = "SALES RETURN B2C";
            SALESRETURNB2Cprofits.Today = SALESRETURNB2CTodayAmount.Amount;
            SALESRETURNB2Cprofits.MTD = SALESRETURNB2CMTDamount.Amount;
            SALESRETURNB2Cprofits.YTD = SALESRETURNB2CYTDamount.Amount;
            SALESRETURNB2Cprofits.LMTD = SALESRETURNB2CLMTDamount.Amount;
            SALESRETURNB2Cprofits.PrevYear = SALESRETURNB2CPreyearamount.Amount;

            //STOCKOUTWARD

            TotalSummary STOCKOUTWARDTodayAmount = await this._Branches.TotalAmount(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            TotalSummary STOCKOUTWARDTodayGP = await this._Branches.GPPercent(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            TotalSummary STOCKOUTWARDMTDamount = await this._Branches.TotalAmount(MonthStart, Todayend, varbranch, "STOCKOUTWARD");


            TotalSummary STOCKOUTWARDYTDamount = await this._Branches.TotalAmount(YearStart, Todayend, varbranch, "STOCKOUTWARD");


            TotalSummary STOCKOUTWARDLMTDamount = await this._Branches.TotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKOUTWARD");


            TotalSummary STOCKOUTWARDPreyearamount = await this._Branches.TotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKOUTWARD");

            Profit STOCKOUTWARDprofits = new Profit();

            STOCKOUTWARDprofits.Type = "STOCK OUTWARD";
            STOCKOUTWARDprofits.Today = STOCKOUTWARDTodayAmount.Amount;
            STOCKOUTWARDprofits.GpRate = STOCKOUTWARDTodayGP.GpdivAmt > 0 ? (STOCKOUTWARDTodayGP.GpAmt / STOCKOUTWARDTodayGP.GpdivAmt) * 100 : 0;
            STOCKOUTWARDprofits.MTD = STOCKOUTWARDMTDamount.Amount;
            STOCKOUTWARDprofits.YTD = STOCKOUTWARDYTDamount.Amount;
            STOCKOUTWARDprofits.LMTD = STOCKOUTWARDLMTDamount.Amount;
            STOCKOUTWARDprofits.PrevYear = STOCKOUTWARDPreyearamount.Amount;


            //CustomerCount
            TotalSummary CustomerCount = await this._Branches.TotalAmount(TodayStart, Todayend, varbranch, "CUSTOMERCOUNT");

            Profit CusomterCount = new Profit();
            CusomterCount.Type = "CUSTOMER COUNT";
            CusomterCount.Today = CustomerCount.CustCount;

            List<Profit> ProfitList = new List<Profit>();

            ProfitList.Add(Saleprofits);
            ProfitList.Add(B2Cprofits);
            ProfitList.Add(B2Bprofits);
            ProfitList.Add(SALESRETURNB2Cprofits);
            ProfitList.Add(STOCKOUTWARDprofits);
            ProfitList.Add(CusomterCount);



            return ProfitList;
        }

        [HttpGet("GetValByCategory")]
        public async Task<List<CategoryList>> GetValByCategory(DateTimeOffset Date, [FromQuery] List<long> Branches, string Type)
        {
            List<SaleValueByCategory> SaleValueByCategoryTodayList = new List<SaleValueByCategory>();
            List<SaleValueByCategory> SaleValueByCategoryMTDList = new List<SaleValueByCategory>();
            List<SaleValueByCategory> SaleValueByCategoryYTDList = new List<SaleValueByCategory>();
            List<SaleValueByCategory> SaleValueByCategoryLMTDList = new List<SaleValueByCategory>();
            List<SaleValueByCategory> SaleValueByCategoryPrevYearList = new List<SaleValueByCategory>();
            List<GPPercent> SaleTodayGPByCategory = new List<GPPercent>();

            List<CategoryList> SaleValueByCategoryMainList = new List<CategoryList>();
            // List<Category> CategoryList = new List<Category>();
            // CategoryList = await this._Branches.CategoryList();
            //  Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            var today = Date;
            string varbranch = string.Join(",", Branches);
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);
            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            if (Type == "SALE")
            {
                SaleValueByCategoryTodayList = await _Branches.SaleValueByCategory(TodayStart, Todayend, varbranch);
                SaleValueByCategoryMTDList = await _Branches.SaleValueByCategory(MonthStart, Todayend, varbranch);
                SaleValueByCategoryYTDList = await _Branches.SaleValueByCategory(YearStart, Todayend, varbranch);
                SaleValueByCategoryLMTDList = await _Branches.SaleValueByCategory(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueByCategoryPrevYearList = await _Branches.SaleValueByCategory(PreyearStart, PreyearEnd, varbranch);
                SaleTodayGPByCategory = await _Branches.GPPercentCal(TodayStart, Todayend, varbranch, "CATEGORY");

                if (SaleTodayGPByCategory != null)
                {
                    foreach (var itm in SaleTodayGPByCategory)
                    {
                        itm.Gppercentage = itm.GpdivAmt > 0 ? ((itm.GpAmt - itm.GpdivAmt) / itm.GpdivAmt) * 100 : 0;
                    }

                }


            }
            else if (Type == "PURCHASE")
            {
                SaleValueByCategoryTodayList = await _Branches.PurchaseValueByCategory(TodayStart, Todayend, varbranch);
                SaleValueByCategoryMTDList = await _Branches.PurchaseValueByCategory(MonthStart, Todayend, varbranch);
                SaleValueByCategoryYTDList = await _Branches.PurchaseValueByCategory(YearStart, Todayend, varbranch);
                SaleValueByCategoryLMTDList = await _Branches.PurchaseValueByCategory(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueByCategoryPrevYearList = await _Branches.PurchaseValueByCategory(PreyearStart, PreyearEnd, varbranch);
            }



            if (Type == "SALE" || Type == "PURCHASE")
            {
                var categoryMap = new Dictionary<long, CategoryList>();

                foreach (var item in SaleValueByCategoryPrevYearList)
                {
                    var categoryList = new CategoryList
                    {
                        Category = item.Category,
                        PrevYear = item.Amount,
                        CategoryId = item.CategoryId,
                    };
                    categoryMap[item.CategoryId] = categoryList;
                }

                foreach (var item in SaleValueByCategoryYTDList)
                {
                    if (categoryMap.TryGetValue(item.CategoryId, out var categoryList))
                    {
                        categoryList.YTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueByCategoryLMTDList)
                {
                    if (categoryMap.TryGetValue(item.CategoryId, out var categoryList))
                    {
                        categoryList.LMTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueByCategoryMTDList)
                {
                    if (categoryMap.TryGetValue(item.CategoryId, out var categoryList))
                    {
                        categoryList.MTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueByCategoryTodayList)
                {
                    if (categoryMap.TryGetValue(item.CategoryId, out var categoryList))
                    {
                        categoryList.Today = item.Amount;
                    }
                }

                if (SaleTodayGPByCategory != null)
                {
                    foreach (var item in SaleTodayGPByCategory)
                    {
                        if (categoryMap.TryGetValue(item.Id, out var categoryList))
                        {
                            categoryList.GpRate = item.Gppercentage;
                        }
                    }
                }




                SaleValueByCategoryMainList = categoryMap.Values.ToList();
            }

            // Order the list by checking if any field has a non-zero amount
            var orderedList = SaleValueByCategoryMainList.OrderByDescending(model =>
            {
                // Check if any decimal property in the model has a non-zero value
                return typeof(CategoryList).GetProperties()
                    .Where(prop => prop.PropertyType == typeof(decimal))
                    .Any(prop => (decimal)prop.GetValue(model) != 0);
            }).ToList();


            return SaleValueByCategoryMainList;
        }



        [HttpGet("GetValBySubCategory")]
        public async Task<List<CategoryList>> GetValBySubCategory(DateTimeOffset Date, [FromQuery] List<long> Branches, long CatId, string Type)
        {
            List<SaleValueBySubCategory> SaleValueBySubCategoryTodayList = new List<SaleValueBySubCategory>();
            List<SaleValueBySubCategory> SaleValueBySubCategoryMTDList = new List<SaleValueBySubCategory>();
            List<SaleValueBySubCategory> SaleValueBySubCategoryYTDList = new List<SaleValueBySubCategory>();
            List<SaleValueBySubCategory> SaleValueBySubCategoryLMTDList = new List<SaleValueBySubCategory>();
            List<SaleValueBySubCategory> SaleValueBySubCategoryPrevYearList = new List<SaleValueBySubCategory>();
            List<GPPercent> SaleTodayGPBySubCategory = new List<GPPercent>();
            List<CategoryList> SaleValueBysubCategoryMainList = new List<CategoryList>();
            // List<Category> CategoryList = new List<Category>();
            // CategoryList = await this._Branches.CategoryList();
            //  Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            var today = Date;
            string varbranch = string.Join(",", Branches);
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);
            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            if (Type == "SALE")
            {
                SaleValueBySubCategoryTodayList = await _Branches.SaleValueBySubCategory(TodayStart, Todayend, varbranch);
                SaleValueBySubCategoryMTDList = await _Branches.SaleValueBySubCategory(MonthStart, Todayend, varbranch);
                SaleValueBySubCategoryYTDList = await _Branches.SaleValueBySubCategory(YearStart, Todayend, varbranch);
                SaleValueBySubCategoryLMTDList = await _Branches.SaleValueBySubCategory(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueBySubCategoryPrevYearList = await _Branches.SaleValueBySubCategory(PreyearStart, PreyearEnd, varbranch);
                SaleTodayGPBySubCategory = await _Branches.GPPercentCal(TodayStart, Todayend, varbranch, "SUBCATEGORY");

                if (SaleTodayGPBySubCategory != null)
                {
                    foreach (var itm in SaleTodayGPBySubCategory)
                    {
                        itm.Gppercentage = itm.GpdivAmt > 0 ? ((itm.GpAmt - itm.GpdivAmt) / itm.GpdivAmt) * 100 : 0;
                    }

                }
            }
            else if (Type == "PURCHASE")
            {
                SaleValueBySubCategoryTodayList = await _Branches.PurchaseValueBySubCategory(TodayStart, Todayend, varbranch);
                SaleValueBySubCategoryMTDList = await _Branches.PurchaseValueBySubCategory(MonthStart, Todayend, varbranch);
                SaleValueBySubCategoryYTDList = await _Branches.PurchaseValueBySubCategory(YearStart, Todayend, varbranch);
                SaleValueBySubCategoryLMTDList = await _Branches.PurchaseValueBySubCategory(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueBySubCategoryPrevYearList = await _Branches.PurchaseValueBySubCategory(PreyearStart, PreyearEnd, varbranch);
            }




            if (Type == "SALE" || Type == "PURCHASE")
            {
                var categoryMap = new Dictionary<string, CategoryList>();

                //// Function to check if CategoryId exists in categoryMap and return the CategoryList
                //CategoryList GetOrCreateCategoryList(long categoryId)
                //{
                //    if (!categoryMap.TryGetValue(categoryId, out var categoryList))
                //    {
                //        categoryList = new CategoryList { CategoryId = categoryId };
                //        categoryMap[categoryId] = categoryList;
                //    }
                //    return categoryList;
                //}


                foreach (var item in SaleValueBySubCategoryPrevYearList)
                {
                    var categoryList = new CategoryList
                    {
                        Category = item.SubCategory,
                        PrevYear = item.Amount,
                        CategoryId = item.CategoryId,
                    };
                    categoryMap[item.SubCategory] = categoryList;

                    //var categoryList = GetOrCreateCategoryList(item.CategoryId);
                    //categoryList.Category = item.SubCategory;
                    //categoryList.PrevYear = item.Amount;
                    //categoryMap[item.CategoryId] = categoryList;
                }

                foreach (var item in SaleValueBySubCategoryYTDList)
                {
                    if (categoryMap.TryGetValue(item.SubCategory, out var categoryList))
                    {
                        categoryList.YTD = item.Amount;
                    }
                    //var categoryList = GetOrCreateCategoryList(item.CategoryId);
                    //categoryList.YTD = item.Amount;

                }

                foreach (var item in SaleValueBySubCategoryLMTDList)
                {
                    if (categoryMap.TryGetValue(item.SubCategory, out var categoryList))
                    {
                        categoryList.LMTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueBySubCategoryMTDList)
                {
                    if (categoryMap.TryGetValue(item.SubCategory, out var categoryList))
                    {
                        categoryList.MTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueBySubCategoryTodayList)
                {
                    if (categoryMap.TryGetValue(item.SubCategory, out var categoryList))
                    {
                        categoryList.Today = item.Amount;
                    }
                }

                if (SaleTodayGPBySubCategory != null)
                {
                    foreach (var item in SaleTodayGPBySubCategory)
                    {
                        if (categoryMap.TryGetValue(item.Name, out var categoryList))
                        {
                            categoryList.GpRate = item.Gppercentage;
                        }
                    }
                }


                SaleValueBysubCategoryMainList = categoryMap.Values.ToList();

                if (CatId != 0 && CatId != null)
                {
                    SaleValueBysubCategoryMainList = SaleValueBysubCategoryMainList.FindAll(y => y.CategoryId == CatId);
                }


            }

            // Order the list by checking if any field has a non-zero amount
            var orderedList = SaleValueBysubCategoryMainList.OrderByDescending(model =>
            {
                // Check if any decimal property in the model has a non-zero value
                return typeof(CategoryList).GetProperties()
                    .Where(prop => prop.PropertyType == typeof(decimal))
                    .Any(prop => (decimal)prop.GetValue(model) != 0);
            }).ToList();




            return orderedList;
        }


        [HttpGet("GetValBySupplier")]
        public async Task<List<SupplierCategoryList>> GetValBySupplier(DateTimeOffset Date, [FromQuery] List<long> Branches, string Type)
        {
            List<SaleValueBySupplier> SaleValueBySupplierTodayList = new List<SaleValueBySupplier>();
            List<SaleValueBySupplier> SaleValueBySupplierMTDList = new List<SaleValueBySupplier>();
            List<SaleValueBySupplier> SaleValueBySupplierYTDList = new List<SaleValueBySupplier>();
            List<SaleValueBySupplier> SaleValueBySupplierLMTDList = new List<SaleValueBySupplier>();
            List<SaleValueBySupplier> SaleValueBySupplierPrevYearList = new List<SaleValueBySupplier>();
            List<TotalSummary> SaleTodayGPBySupplier = new List<TotalSummary>();
            List<SupplierCategoryList> SaleValueBySupplierMainList = new List<SupplierCategoryList>();
            List<GPPercent> SaleGPByMFR = new List<GPPercent>();
            // List<Category> CategoryList = new List<Category>();
            // CategoryList = await this._Branches.CategoryList();
            //  Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            var today = Date;
            string varbranch = string.Join(",", Branches);
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);
            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            if (Type == "SALE")
            {
                SaleValueBySupplierTodayList = await _Branches.SaleValueBySupplier(TodayStart, Todayend, varbranch);
                SaleValueBySupplierMTDList = await _Branches.SaleValueBySupplier(MonthStart, Todayend, varbranch);
                SaleValueBySupplierYTDList = await _Branches.SaleValueBySupplier(YearStart, Todayend, varbranch);
                SaleValueBySupplierLMTDList = await _Branches.SaleValueBySupplier(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueBySupplierPrevYearList = await _Branches.SaleValueBySupplier(PreyearStart, PreyearEnd, varbranch);
                // SaleTodayGPBySupplier = await _Branches.SupplierGPPercent(TodayStart, Todayend, varbranch, "SALES");
                SaleGPByMFR = await _Branches.GPPercentCal(TodayStart, Todayend, varbranch, "Supplier");
                if (SaleGPByMFR != null)
                {
                    foreach (var itm in SaleGPByMFR)
                    {
                        itm.Gppercentage = itm.GpdivAmt > 0 ? ((itm.GpAmt - itm.GpdivAmt) / itm.GpdivAmt) * 100 : 0;
                    }

                }
            }
            else if (Type == "PURCHASE")
            {
                SaleValueBySupplierTodayList = await _Branches.PurchaseValueBySupplier(TodayStart, Todayend, varbranch);
                SaleValueBySupplierMTDList = await _Branches.PurchaseValueBySupplier(MonthStart, Todayend, varbranch);
                SaleValueBySupplierYTDList = await _Branches.PurchaseValueBySupplier(YearStart, Todayend, varbranch);
                SaleValueBySupplierLMTDList = await _Branches.PurchaseValueBySupplier(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueBySupplierPrevYearList = await _Branches.PurchaseValueBySupplier(PreyearStart, PreyearEnd, varbranch);
            }





            if (Type == "SALE" || Type == "PURCHASE")
            {
                var categoryMap = new Dictionary<long, SupplierCategoryList>();

                foreach (var item in SaleValueBySupplierPrevYearList)
                {
                    var categoryList = new SupplierCategoryList
                    {
                        Supplier = item.Customer,
                        PrevYear = item.Amount,
                        SupplierId = item.CustomerId,
                    };
                    categoryMap[item.CustomerId] = categoryList;
                }

                foreach (var item in SaleValueBySupplierYTDList)
                {
                    if (categoryMap.TryGetValue(item.CustomerId, out var categoryList))
                    {
                        categoryList.YTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueBySupplierLMTDList)
                {
                    if (categoryMap.TryGetValue(item.CustomerId, out var categoryList))
                    {
                        categoryList.LMTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueBySupplierMTDList)
                {
                    if (categoryMap.TryGetValue(item.CustomerId, out var categoryList))
                    {
                        categoryList.MTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueBySupplierTodayList)
                {
                    if (categoryMap.TryGetValue(item.CustomerId, out var categoryList))
                    {
                        categoryList.Today = item.Amount;
                    }
                }


                if (SaleGPByMFR != null)
                {
                    foreach (var item in SaleGPByMFR)
                    {
                        if (categoryMap.TryGetValue(item.Id, out var categoryList))
                        {
                            categoryList.GpRate = item.Gppercentage;
                        }
                    }
                }



                SaleValueBySupplierMainList = categoryMap.Values.ToList();
            }


            // Order the list by checking if any field has a non-zero amount
            var orderedList = SaleValueBySupplierMainList.OrderByDescending(model =>
            {
                // Check if any decimal property in the model has a non-zero value
                return typeof(SupplierCategoryList).GetProperties()
                    .Where(prop => prop.PropertyType == typeof(decimal))
                    .Any(prop => (decimal)prop.GetValue(model) != 0);
            }).ToList();



            return orderedList;
        }


        [HttpGet("GetValByMFR")]
        public async Task<List<MFRCategoryList>> GetValByMFR(DateTimeOffset Date, [FromQuery] List<long> Branches, string Type)
        {
            List<SaleValueByMFR> SaleValueByMFRTodayList = new List<SaleValueByMFR>();
            List<SaleValueByMFR> SaleValueByMFRMTDList = new List<SaleValueByMFR>();
            List<SaleValueByMFR> SaleValueByMFRYTDList = new List<SaleValueByMFR>();
            List<SaleValueByMFR> SaleValueByMFRLMTDList = new List<SaleValueByMFR>();
            List<SaleValueByMFR> SaleValueByMFRPrevYearList = new List<SaleValueByMFR>();
            List<TotalSummary> SaleTodayGPByMFR = new List<TotalSummary>();
            List<MFRCategoryList> SaleValueByMFRMainList = new List<MFRCategoryList>();
            List<GPPercent> SaleGPByMFR = new List<GPPercent>();
            // List<Category> CategoryList = new List<Category>();
            // CategoryList = await this._Branches.CategoryList();
            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            var today = Date;
            string varbranch = string.Join(",", Branches);
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);
            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            if (Type == "SALE")
            {
                SaleValueByMFRTodayList = await _Branches.SaleValueByMFR(TodayStart, Todayend, varbranch);
                SaleValueByMFRMTDList = await _Branches.SaleValueByMFR(MonthStart, Todayend, varbranch);
                SaleValueByMFRYTDList = await _Branches.SaleValueByMFR(YearStart, Todayend, varbranch);
                SaleValueByMFRLMTDList = await _Branches.SaleValueByMFR(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueByMFRPrevYearList = await _Branches.SaleValueByMFR(PreyearStart, PreyearEnd, varbranch);
                // SaleTodayGPByMFR = await _Branches.MFRGPPercent(TodayStart, Todayend, varbranch, "SALES");
                SaleGPByMFR = await _Branches.GPPercentCal(TodayStart, Todayend, varbranch, "MFR");


                if (SaleGPByMFR != null)
                {
                    foreach (var itm in SaleGPByMFR)
                    {
                        itm.Gppercentage = itm.GpdivAmt > 0 ? ((itm.GpAmt - itm.GpdivAmt) / itm.GpdivAmt) * 100 : 0;
                    }

                }
            }
            else if (Type == "PURCHASE")
            {
                SaleValueByMFRTodayList = await _Branches.PurchaseValueByMFR(TodayStart, Todayend, varbranch);
                SaleValueByMFRMTDList = await _Branches.PurchaseValueByMFR(MonthStart, Todayend, varbranch);
                SaleValueByMFRYTDList = await _Branches.PurchaseValueByMFR(YearStart, Todayend, varbranch);
                SaleValueByMFRLMTDList = await _Branches.PurchaseValueByMFR(Dates.LMTDstart, Dates.LMTDend, varbranch);
                SaleValueByMFRPrevYearList = await _Branches.PurchaseValueByMFR(PreyearStart, PreyearEnd, varbranch);
            }



            if (Type == "SALE" || Type == "PURCHASE")
            {
                var categoryMap = new Dictionary<long, MFRCategoryList>();

                foreach (var item in SaleValueByMFRPrevYearList)
                {
                    var categoryList = new MFRCategoryList
                    {
                        MFR = item.MNFRNAME,
                        PrevYear = item.Amount,
                        MFRID = item.MFRId,
                    };
                    categoryMap[item.MFRId] = categoryList;
                }

                foreach (var item in SaleValueByMFRYTDList)
                {
                    if (categoryMap.TryGetValue(item.MFRId, out var categoryList))
                    {
                        categoryList.YTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueByMFRLMTDList)
                {
                    if (categoryMap.TryGetValue(item.MFRId, out var categoryList))
                    {
                        categoryList.LMTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueByMFRMTDList)
                {
                    if (categoryMap.TryGetValue(item.MFRId, out var categoryList))
                    {
                        categoryList.MTD = item.Amount;
                    }
                }

                foreach (var item in SaleValueByMFRTodayList)
                {
                    if (categoryMap.TryGetValue(item.MFRId, out var categoryList))
                    {
                        categoryList.Today = item.Amount;
                    }
                }

                if (SaleGPByMFR != null)
                {
                    foreach (var item in SaleGPByMFR)
                    {
                        if (categoryMap.TryGetValue(item.Id, out var categoryList))
                        {
                            categoryList.GpRate = item.Gppercentage;
                        }
                    }
                }


                SaleValueByMFRMainList = categoryMap.Values.ToList();
            }



            // Order the list by checking if any field has a non-zero amount
            var orderedList = SaleValueByMFRMainList.OrderByDescending(model =>
            {
                // Check if any decimal property in the model has a non-zero value
                return typeof(MFRCategoryList).GetProperties()
                    .Where(prop => prop.PropertyType == typeof(decimal))
                    .Any(prop => (decimal)prop.GetValue(model) != 0);
            }).ToList();


            return orderedList;
        }


        [HttpGet("GetCategorySalesProfit")]
        public async Task<List<TypList>> GetCatrgyProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {


            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);

            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);


            List<TotalSummary> SaleTodayAmount = await this._Branches.CategoryTotalAmount(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleTodayGP = await this._Branches.CategoryGPPercent(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleMTDamount = await this._Branches.CategoryTotalAmount(MonthStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleYTDamount = await this._Branches.CategoryTotalAmount(YearStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleLMTDamount = await this._Branches.CategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALES");
            List<TotalSummary> SalePreyearamount = await this._Branches.CategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALES");

            TypList Saleprofits = new TypList();

            // Create a dictionary to map categories to CategoryList objects
            var categoryMap = new Dictionary<string, CategoryList>();
            Saleprofits.Type = "SALES";
            foreach (var item in SalePreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                categoryMap[item.Catgry] = categoryList;
            }

            foreach (var item in SaleYTDamount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SaleLMTDamount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SaleMTDamount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SaleTodayAmount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in SaleTodayGP)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            Saleprofits.categoryLists = categoryMap.Values.ToList();

            //B2C

            List<TotalSummary> B2CTodayAmount = await this._Branches.CategoryTotalAmount(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CTodayGP = await this._Branches.CategoryGPPercent(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CMTDamount = await this._Branches.CategoryTotalAmount(MonthStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CYTDamount = await this._Branches.CategoryTotalAmount(YearStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CLMTDamount = await this._Branches.CategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2C");
            List<TotalSummary> B2CPreyearamount = await this._Branches.CategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2C");

            TypList B2Cprofits = new TypList();
            var B2CcategoryMap = new Dictionary<string, CategoryList>();
            B2Cprofits.Type = "B2C";
            foreach (var item in B2CPreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                B2CcategoryMap[item.Catgry] = categoryList;
            }

            foreach (var item in B2CYTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2CLMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2CMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2CTodayAmount)
            {
                if (B2CcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in B2CTodayGP)
            {
                if (B2CcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Cprofits.categoryLists = B2CcategoryMap.Values.ToList();


            //B2B

            List<TotalSummary> B2BTodayAmount = await this._Branches.CategoryTotalAmount(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BTodayGP = await this._Branches.CategoryGPPercent(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BMTDamount = await this._Branches.CategoryTotalAmount(MonthStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BYTDamount = await this._Branches.CategoryTotalAmount(YearStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BLMTDamount = await this._Branches.CategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2B");
            List<TotalSummary> B2BPreyearamount = await this._Branches.CategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2B");

            TypList B2Bprofits = new TypList();

            var B2BcategoryMap = new Dictionary<string, CategoryList>();
            B2Bprofits.Type = "B2B";
            foreach (var item in B2CPreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                B2BcategoryMap[item.Catgry] = categoryList;
            }

            foreach (var item in B2BYTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2BLMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2BMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2BTodayAmount)
            {
                if (B2BcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }
            foreach (var item in B2BTodayGP)
            {
                if (B2BcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Bprofits.categoryLists = B2BcategoryMap.Values.ToList();

            //SALESRETURNB2C

            List<TotalSummary> SALESRETURNB2CTodayAmount = await this._Branches.CategoryTotalAmount(TodayStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CMTDamount = await this._Branches.CategoryTotalAmount(MonthStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CYTDamount = await this._Branches.CategoryTotalAmount(YearStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CLMTDamount = await this._Branches.CategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CPreyearamount = await this._Branches.CategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALESRETURNB2C");

            TypList SALESRETURNB2Cprofits = new TypList();
            var SALESRETURNB2CMap = new Dictionary<string, CategoryList>();
            SALESRETURNB2Cprofits.Type = "SALE SRETURN B2C";
            foreach (var item in SALESRETURNB2CPreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                SALESRETURNB2CMap[item.Catgry] = categoryList;
            }

            foreach (var item in SALESRETURNB2CYTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CLMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CTodayAmount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            SALESRETURNB2Cprofits.categoryLists = SALESRETURNB2CMap.Values.ToList();

            //STOCKOUTWARD

            List<TotalSummary> STOCKOUTWARDTodayAmount = await this._Branches.CategoryTotalAmount(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDTodayGP = await this._Branches.CategoryGPPercent(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDMTDamount = await this._Branches.CategoryTotalAmount(MonthStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDYTDamount = await this._Branches.CategoryTotalAmount(YearStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDLMTDamount = await this._Branches.CategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDPreyearamount = await this._Branches.CategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKOUTWARD");


            TypList STOCKOUTWARDprofits = new TypList();

            var STOCKOUTWARDMap = new Dictionary<string, CategoryList>();
            STOCKOUTWARDprofits.Type = "STOCK OUTWARD";
            foreach (var item in STOCKOUTWARDPreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                STOCKOUTWARDMap[item.Catgry] = categoryList;
            }

            foreach (var item in STOCKOUTWARDYTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDLMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDTodayAmount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }
            foreach (var item in STOCKOUTWARDTodayGP)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }
            STOCKOUTWARDprofits.categoryLists = STOCKOUTWARDMap.Values.ToList();

            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Saleprofits);
            ProfitList.Add(B2Cprofits);
            ProfitList.Add(B2Bprofits);
            ProfitList.Add(SALESRETURNB2Cprofits);
            ProfitList.Add(STOCKOUTWARDprofits);



            return ProfitList;
        }

        [HttpGet("GetSubCategorySalesProfit")]
        public async Task<List<TypList>> GetSubCatrgyProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            List<TotalSummary> SaleTodayAmount = await this._Branches.SubCategoryTotalAmount(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleTodayGP = await this._Branches.SubCategoryGPPercent(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleMTDamount = await this._Branches.SubCategoryTotalAmount(MonthStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleYTDamount = await this._Branches.SubCategoryTotalAmount(YearStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleLMTDamount = await this._Branches.SubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALES");
            List<TotalSummary> SalePreyearamount = await this._Branches.SubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALES");

            TypList Saleprofits = new TypList();


            var categoryMap = new Dictionary<string, SubCategoryList>();
            Saleprofits.Type = "SALES";
            foreach (var item in SalePreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                categoryMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in SaleYTDamount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SaleLMTDamount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SaleMTDamount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SaleTodayAmount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in SaleTodayGP)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            Saleprofits.subCategoryLists = categoryMap.Values.ToList();



            //B2C

            List<TotalSummary> B2CTodayAmount = await this._Branches.SubCategoryTotalAmount(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CTodayGP = await this._Branches.SubCategoryGPPercent(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CMTDamount = await this._Branches.SubCategoryTotalAmount(MonthStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CYTDamount = await this._Branches.SubCategoryTotalAmount(YearStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CLMTDamount = await this._Branches.SubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2C");
            List<TotalSummary> B2CPreyearamount = await this._Branches.SubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2C");

            TypList B2Cprofits = new TypList();
            var B2CcategoryMap = new Dictionary<string, SubCategoryList>();
            B2Cprofits.Type = "B2C";
            foreach (var item in B2CPreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                B2CcategoryMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in B2CYTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2CLMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2CMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2CTodayAmount)
            {
                if (B2CcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in B2CTodayGP)
            {
                if (B2CcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Cprofits.subCategoryLists = B2CcategoryMap.Values.ToList();

            //B2B

            List<TotalSummary> B2BTodayAmount = await this._Branches.SubCategoryTotalAmount(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BTodayGP = await this._Branches.SubCategoryGPPercent(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BMTDamount = await this._Branches.SubCategoryTotalAmount(MonthStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BYTDamount = await this._Branches.SubCategoryTotalAmount(YearStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BLMTDamount = await this._Branches.SubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2B");
            List<TotalSummary> B2BPreyearamount = await this._Branches.SubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2B");

            TypList B2Bprofits = new TypList();
            var B2BcategoryMap = new Dictionary<string, SubCategoryList>();
            B2Bprofits.Type = "B2B";
            foreach (var item in B2CPreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                B2BcategoryMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in B2BYTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2BLMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2BMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2BTodayAmount)
            {
                if (B2BcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }
            foreach (var item in B2BTodayGP)
            {
                if (B2BcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Bprofits.subCategoryLists = B2BcategoryMap.Values.ToList();



            //SALESRETURNB2C

            List<TotalSummary> SALESRETURNB2CTodayAmount = await this._Branches.SubCategoryTotalAmount(TodayStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CMTDamount = await this._Branches.SubCategoryTotalAmount(MonthStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CYTDamount = await this._Branches.SubCategoryTotalAmount(YearStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CLMTDamount = await this._Branches.SubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CPreyearamount = await this._Branches.SubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALESRETURNB2C");

            TypList SALESRETURNB2Cprofits = new TypList();
            var SALESRETURNB2CMap = new Dictionary<string, SubCategoryList>();
            SALESRETURNB2Cprofits.Type = "SALES RETURN B2C";
            foreach (var item in SALESRETURNB2CPreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                SALESRETURNB2CMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in SALESRETURNB2CYTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CLMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CTodayAmount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            SALESRETURNB2Cprofits.subCategoryLists = SALESRETURNB2CMap.Values.ToList();


            //STOCKOUTWARD

            List<TotalSummary> STOCKOUTWARDTodayAmount = await this._Branches.SubCategoryTotalAmount(TodayStart, Todayend, varbranch, "STOCKOUTWARD");
            List<TotalSummary> STOCKOUTWARDTodayGP = await this._Branches.SubCategoryGPPercent(TodayStart, Todayend, varbranch, "STOCKOUTWARD");
            List<TotalSummary> STOCKOUTWARDMTDamount = await this._Branches.SubCategoryTotalAmount(MonthStart, Todayend, varbranch, "STOCKOUTWARD");
            List<TotalSummary> STOCKOUTWARDYTDamount = await this._Branches.SubCategoryTotalAmount(YearStart, Todayend, varbranch, "STOCKOUTWARD");
            List<TotalSummary> STOCKOUTWARDLMTDamount = await this._Branches.SubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKOUTWARD");
            List<TotalSummary> STOCKOUTWARDPreyearamount = await this._Branches.SubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKOUTWARD");


            TypList STOCKOUTWARDprofits = new TypList();
            var STOCKOUTWARDMap = new Dictionary<string, SubCategoryList>();
            STOCKOUTWARDprofits.Type = "STOCK OUTWARD";
            foreach (var item in STOCKOUTWARDPreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                STOCKOUTWARDMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in STOCKOUTWARDYTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDLMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDTodayAmount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }
            foreach (var item in STOCKOUTWARDTodayGP)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }
            STOCKOUTWARDprofits.subCategoryLists = STOCKOUTWARDMap.Values.ToList();


            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Saleprofits);
            ProfitList.Add(B2Cprofits);
            ProfitList.Add(B2Bprofits);
            ProfitList.Add(SALESRETURNB2Cprofits);
            ProfitList.Add(STOCKOUTWARDprofits);



            return ProfitList;
        }

        [HttpGet("GetMFRSalesProfit")]
        public async Task<List<TypList>> GetMFRSalesProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {


            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            List<TotalSummary> SaleTodayAmount = await this._Branches.MFRTotalAmount(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleTodayGP = await this._Branches.MFRGPPercent(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleMTDamount = await this._Branches.MFRTotalAmount(MonthStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleYTDamount = await this._Branches.MFRTotalAmount(YearStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleLMTDamount = await this._Branches.MFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALES");
            List<TotalSummary> SalePreyearamount = await this._Branches.MFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALES");

            TypList Saleprofits = new TypList();

            var categoryMap = new Dictionary<string, MFRCategoryList>();
            Saleprofits.Type = "SALES";
            foreach (var item in SalePreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                categoryMap[item.MFR] = categoryList;
            }

            foreach (var item in SaleYTDamount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SaleLMTDamount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SaleMTDamount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SaleTodayAmount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in SaleTodayGP)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            Saleprofits.MFRCategoryLists = categoryMap.Values.ToList();


            //B2C

            List<TotalSummary> B2CTodayAmount = await this._Branches.MFRTotalAmount(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CTodayGP = await this._Branches.MFRGPPercent(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CMTDamount = await this._Branches.MFRTotalAmount(MonthStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CYTDamount = await this._Branches.MFRTotalAmount(YearStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CLMTDamount = await this._Branches.MFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2C");
            List<TotalSummary> B2CPreyearamount = await this._Branches.MFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2C");

            TypList B2Cprofits = new TypList();
            var B2CcategoryMap = new Dictionary<string, MFRCategoryList>();
            B2Cprofits.Type = "B2C";
            foreach (var item in B2CPreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                B2CcategoryMap[item.MFR] = categoryList;
            }

            foreach (var item in B2CYTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2CLMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2CMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2CTodayAmount)
            {
                if (B2CcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in B2CTodayGP)
            {
                if (B2CcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Cprofits.MFRCategoryLists = B2CcategoryMap.Values.ToList();


            //B2B

            List<TotalSummary> B2BTodayAmount = await this._Branches.MFRTotalAmount(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BTodayGP = await this._Branches.MFRGPPercent(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BMTDamount = await this._Branches.MFRTotalAmount(MonthStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BYTDamount = await this._Branches.MFRTotalAmount(YearStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BLMTDamount = await this._Branches.MFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2B");
            List<TotalSummary> B2BPreyearamount = await this._Branches.MFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2B");

            TypList B2Bprofits = new TypList();
            var B2BcategoryMap = new Dictionary<string, MFRCategoryList>();
            B2Bprofits.Type = "B2B";
            foreach (var item in B2CPreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                B2BcategoryMap[item.MFR] = categoryList;
            }

            foreach (var item in B2BYTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2BLMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2BMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2BTodayAmount)
            {
                if (B2BcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }
            foreach (var item in B2BTodayGP)
            {
                if (B2BcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Bprofits.MFRCategoryLists = B2BcategoryMap.Values.ToList();




            //SALESRETURNB2C

            List<TotalSummary> SALESRETURNB2CTodayAmount = await this._Branches.MFRTotalAmount(TodayStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CMTDamount = await this._Branches.MFRTotalAmount(MonthStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CYTDamount = await this._Branches.MFRTotalAmount(YearStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CLMTDamount = await this._Branches.MFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CPreyearamount = await this._Branches.MFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALESRETURNB2C");

            TypList SALESRETURNB2Cprofits = new TypList();
            var SALESRETURNB2CMap = new Dictionary<string, MFRCategoryList>();
            SALESRETURNB2Cprofits.Type = "SALES RETURN B2C";
            foreach (var item in SALESRETURNB2CPreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                SALESRETURNB2CMap[item.MFR] = categoryList;
            }

            foreach (var item in SALESRETURNB2CYTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CLMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CTodayAmount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            SALESRETURNB2Cprofits.MFRCategoryLists = SALESRETURNB2CMap.Values.ToList();


            //STOCKOUTWARD

            List<TotalSummary> STOCKOUTWARDTodayAmount = await this._Branches.MFRTotalAmount(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDTodayGP = await this._Branches.MFRGPPercent(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDMTDamount = await this._Branches.MFRTotalAmount(MonthStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDYTDamount = await this._Branches.MFRTotalAmount(YearStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDLMTDamount = await this._Branches.MFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDPreyearamount = await this._Branches.MFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKOUTWARD");


            TypList STOCKOUTWARDprofits = new TypList();
            var STOCKOUTWARDMap = new Dictionary<string, MFRCategoryList>();
            STOCKOUTWARDprofits.Type = "STOCK OUTWARD";
            foreach (var item in STOCKOUTWARDPreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                STOCKOUTWARDMap[item.MFR] = categoryList;
            }

            foreach (var item in STOCKOUTWARDYTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDLMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDTodayAmount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }
            foreach (var item in STOCKOUTWARDTodayGP)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }
            STOCKOUTWARDprofits.MFRCategoryLists = STOCKOUTWARDMap.Values.ToList();
            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Saleprofits);
            ProfitList.Add(B2Cprofits);
            ProfitList.Add(B2Bprofits);
            ProfitList.Add(SALESRETURNB2Cprofits);
            ProfitList.Add(STOCKOUTWARDprofits);



            return ProfitList;
        }

        [HttpGet("GetSupplierSalesProfit")]
        public async Task<List<TypList>> GetSupplierSalesProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {

            //  Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            List<TotalSummary> SaleTodayAmount = await this._Branches.SupplierTotalAmount(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleTodayGP = await this._Branches.SupplierGPPercent(TodayStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleMTDamount = await this._Branches.SupplierTotalAmount(MonthStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleYTDamount = await this._Branches.SupplierTotalAmount(YearStart, Todayend, varbranch, "SALES");
            List<TotalSummary> SaleLMTDamount = await this._Branches.SupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALES");
            List<TotalSummary> SalePreyearamount = await this._Branches.SupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALES");

            TypList Saleprofits = new TypList();

            var categoryMap = new Dictionary<string, SupplierCategoryList>();
            Saleprofits.Type = "SALES";
            foreach (var item in SalePreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                categoryMap[item.Supplier] = categoryList;
            }

            foreach (var item in SaleYTDamount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SaleLMTDamount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SaleMTDamount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SaleTodayAmount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in SaleTodayGP)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            Saleprofits.SupplierCategoryLists = categoryMap.Values.ToList();



            //B2C

            List<TotalSummary> B2CTodayAmount = await this._Branches.SupplierTotalAmount(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CTodayGP = await this._Branches.SupplierGPPercent(TodayStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CMTDamount = await this._Branches.SupplierTotalAmount(MonthStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CYTDamount = await this._Branches.SupplierTotalAmount(YearStart, Todayend, varbranch, "B2C");
            List<TotalSummary> B2CLMTDamount = await this._Branches.SupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2C");
            List<TotalSummary> B2CPreyearamount = await this._Branches.SupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2C");

            TypList B2Cprofits = new TypList();
            var B2CcategoryMap = new Dictionary<string, SupplierCategoryList>();
            B2Cprofits.Type = "B2C";
            foreach (var item in B2CPreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                B2CcategoryMap[item.Supplier] = categoryList;
            }

            foreach (var item in B2CYTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2CLMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2CMTDamount)
            {
                if (B2CcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2CTodayAmount)
            {
                if (B2CcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in B2CTodayGP)
            {
                if (B2CcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Cprofits.SupplierCategoryLists = B2CcategoryMap.Values.ToList();
            //B2B

            List<TotalSummary> B2BTodayAmount = await this._Branches.SupplierTotalAmount(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BTodayGP = await this._Branches.SupplierGPPercent(TodayStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BMTDamount = await this._Branches.SupplierTotalAmount(MonthStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BYTDamount = await this._Branches.SupplierTotalAmount(YearStart, Todayend, varbranch, "B2B");
            List<TotalSummary> B2BLMTDamount = await this._Branches.SupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "B2B");
            List<TotalSummary> B2BPreyearamount = await this._Branches.SupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "B2B");

            TypList B2Bprofits = new TypList();
            var B2BcategoryMap = new Dictionary<string, SupplierCategoryList>();
            B2Cprofits.Type = "B2C";
            foreach (var item in B2BPreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                B2BcategoryMap[item.Supplier] = categoryList;
            }

            foreach (var item in B2BYTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in B2BLMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in B2BMTDamount)
            {
                if (B2BcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in B2BTodayAmount)
            {
                if (B2BcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in B2BTodayGP)
            {
                if (B2BcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            B2Bprofits.SupplierCategoryLists = B2BcategoryMap.Values.ToList();



            //SALESRETURNB2C

            List<TotalSummary> SALESRETURNB2CTodayAmount = await this._Branches.SupplierTotalAmount(TodayStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CMTDamount = await this._Branches.SupplierTotalAmount(MonthStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CYTDamount = await this._Branches.SupplierTotalAmount(YearStart, Todayend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CLMTDamount = await this._Branches.SupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "SALESRETURNB2C");
            List<TotalSummary> SALESRETURNB2CPreyearamount = await this._Branches.SupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "SALESRETURNB2C");

            TypList SALESRETURNB2Cprofits = new TypList();
            var SALESRETURNB2CMap = new Dictionary<string, SupplierCategoryList>();
            SALESRETURNB2Cprofits.Type = "SALES RETURN B2C";
            foreach (var item in SALESRETURNB2CPreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                SALESRETURNB2CMap[item.Supplier] = categoryList;
            }

            foreach (var item in SALESRETURNB2CYTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CLMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CMTDamount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in SALESRETURNB2CTodayAmount)
            {
                if (SALESRETURNB2CMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            SALESRETURNB2Cprofits.SupplierCategoryLists = SALESRETURNB2CMap.Values.ToList();


            //STOCKOUTWARD

            List<TotalSummary> STOCKOUTWARDTodayAmount = await this._Branches.SupplierTotalAmount(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDTodayGP = await this._Branches.SupplierGPPercent(TodayStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDMTDamount = await this._Branches.SupplierTotalAmount(MonthStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDYTDamount = await this._Branches.SupplierTotalAmount(YearStart, Todayend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDLMTDamount = await this._Branches.SupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKOUTWARD");

            List<TotalSummary> STOCKOUTWARDPreyearamount = await this._Branches.SupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKOUTWARD");


            TypList STOCKOUTWARDprofits = new TypList();
            var STOCKOUTWARDMap = new Dictionary<string, SupplierCategoryList>();
            STOCKOUTWARDprofits.Type = "STOCK OUTWARD";
            foreach (var item in STOCKOUTWARDPreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                STOCKOUTWARDMap[item.Supplier] = categoryList;
            }

            foreach (var item in STOCKOUTWARDYTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDLMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDMTDamount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in STOCKOUTWARDTodayAmount)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }
            foreach (var item in STOCKOUTWARDTodayGP)
            {
                if (STOCKOUTWARDMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }
            STOCKOUTWARDprofits.SupplierCategoryLists = STOCKOUTWARDMap.Values.ToList();

            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Saleprofits);
            ProfitList.Add(B2Cprofits);
            ProfitList.Add(B2Bprofits);
            ProfitList.Add(SALESRETURNB2Cprofits);
            ProfitList.Add(STOCKOUTWARDprofits);



            return ProfitList;
        }


        //PURCHASE

        [HttpGet("GetPurchaseProfit")]
        public async Task<List<Profit>> GetPurchaseProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            //Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, lastMonth.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);


            TotalSummary PurTodayAmount = await this._Branches.PurTotalAmount(TodayStart, Todayend, varbranch, "PURCHASE");
            TotalSummary PurTodayGP = await this._Branches.PurGPPercent(TodayStart, Todayend, varbranch, "PURCHASE");
            TotalSummary PurMTDamount = await this._Branches.PurTotalAmount(MonthStart, Todayend, varbranch, "PURCHASE");
            TotalSummary PurYTDamount = await this._Branches.PurTotalAmount(YearStart, Todayend, varbranch, "PURCHASE");
            TotalSummary PurLMTDamount = await this._Branches.PurTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASE");
            TotalSummary PurPreyearamount = await this._Branches.PurTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASE");

            Profit Purprofits = new Profit();

            Purprofits.Type = "PURCHASE";
            Purprofits.Today = PurTodayAmount.Amount;
            Purprofits.GpRate = PurTodayGP.GpdivAmt > 0 ? (PurTodayGP.GpAmt / PurTodayGP.GpdivAmt) * 100 : 0;
            Purprofits.MTD = PurMTDamount.Amount;
            Purprofits.YTD = PurYTDamount.Amount;
            Purprofits.LMTD = PurLMTDamount.Amount;
            Purprofits.PrevYear = PurPreyearamount.Amount;

            //PURCHASERET


            TotalSummary PurRetTodayAmount = await this._Branches.PurTotalAmount(TodayStart, Todayend, varbranch, "PURCHASERET");
            TotalSummary PurRetTodayGP = await this._Branches.PurGPPercent(TodayStart, Todayend, varbranch, "PURCHASERET");
            TotalSummary PurRetMTDamount = await this._Branches.PurTotalAmount(MonthStart, Todayend, varbranch, "PURCHASERET");
            TotalSummary PurRetYTDamount = await this._Branches.PurTotalAmount(YearStart, Todayend, varbranch, "PURCHASERET");
            TotalSummary PurRetLMTDamount = await this._Branches.PurTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASERET");
            TotalSummary PurRetPreyearamount = await this._Branches.PurTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASERET");

            Profit PurRetprofits = new Profit();

            PurRetprofits.Type = "PURCHASE RETURN";
            PurRetprofits.Today = PurRetTodayAmount.Amount;
            PurRetprofits.GpRate = PurRetTodayGP.GpdivAmt > 0 ? (PurRetTodayGP.GpAmt / PurRetTodayGP.GpdivAmt) * 100 : 0;
            PurRetprofits.MTD = PurRetMTDamount.Amount;
            PurRetprofits.YTD = PurRetYTDamount.Amount;
            PurRetprofits.LMTD = PurRetLMTDamount.Amount;
            PurRetprofits.PrevYear = PurRetPreyearamount.Amount;

            //STOCKINWARD

            TotalSummary STOCKINWARDTodayAmount = await this._Branches.PurTotalAmount(TodayStart, Todayend, varbranch, "STOCKINWARD");
            TotalSummary STOCKINWARDTodayGP = await this._Branches.PurGPPercent(TodayStart, Todayend, varbranch, "STOCKINWARD");
            TotalSummary STOCKINWARDMTDamount = await this._Branches.PurTotalAmount(MonthStart, Todayend, varbranch, "STOCKINWARD");
            TotalSummary STOCKINWARDYTDamount = await this._Branches.PurTotalAmount(YearStart, Todayend, varbranch, "STOCKINWARD");
            TotalSummary STOCKINWARDLMTDamount = await this._Branches.PurTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKINWARD");
            TotalSummary STOCKINWARDPreyearamount = await this._Branches.PurTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKINWARD");

            Profit STOCKINWARDprofits = new Profit();

            STOCKINWARDprofits.Type = "STOCK INWARD";
            STOCKINWARDprofits.Today = STOCKINWARDTodayAmount.Amount;
            STOCKINWARDprofits.GpRate = STOCKINWARDTodayGP.GpdivAmt > 0 ? (STOCKINWARDTodayGP.GpAmt / STOCKINWARDTodayGP.GpdivAmt) * 100 : 0;
            STOCKINWARDprofits.MTD = STOCKINWARDMTDamount.Amount;
            STOCKINWARDprofits.YTD = STOCKINWARDYTDamount.Amount;
            STOCKINWARDprofits.LMTD = STOCKINWARDLMTDamount.Amount;
            STOCKINWARDprofits.PrevYear = STOCKINWARDPreyearamount.Amount;




            List<Profit> PurProfitList = new List<Profit>();

            PurProfitList.Add(Purprofits);
            PurProfitList.Add(PurRetprofits);
            PurProfitList.Add(STOCKINWARDprofits);

            return PurProfitList;
        }

        [HttpGet("GetCategoryPurchaseProfit")]
        public async Task<List<TypList>> GetCategoryPurchaseProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            //    Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, today.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            List<TotalSummary> PurTodayAmount = await this._Branches.PurCategoryTotalAmount(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurTodayGP = await this._Branches.PurCategoryGPPercent(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurMTDamount = await this._Branches.PurCategoryTotalAmount(MonthStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurYTDamount = await this._Branches.PurCategoryTotalAmount(YearStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurLMTDamount = await this._Branches.PurCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASE");
            List<TotalSummary> PurPreyearamount = await this._Branches.PurCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASE");

            TypList Purprofits = new TypList();
            var categoryMap = new Dictionary<string, CategoryList>();
            Purprofits.Type = "PURCHASE";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                categoryMap[item.Catgry] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (categoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            Purprofits.categoryLists = categoryMap.Values.ToList();


            //PURCHASERET

            List<TotalSummary> PurRetTodayAmount = await this._Branches.PurCategoryTotalAmount(TodayStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetMTDamount = await this._Branches.PurCategoryTotalAmount(MonthStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetYTDamount = await this._Branches.PurCategoryTotalAmount(YearStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetLMTDamount = await this._Branches.PurCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetPreyearamount = await this._Branches.PurCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASERET");

            TypList PurRetprofits = new TypList();

            var PurCategryLst = new Dictionary<string, CategoryList>();
            PurRetprofits.Type = "PURCHASE RETURN";
            foreach (var item in PurRetPreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                PurCategryLst[item.Catgry] = categoryList;
            }

            foreach (var item in PurRetYTDamount)
            {
                if (PurCategryLst.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurRetLMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurRetMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurRetTodayAmount)
            {
                if (PurCategryLst.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }


            PurRetprofits.categoryLists = PurCategryLst.Values.ToList();
            //STOCKOUTWARD

            List<TotalSummary> STOCKINWARDTodayAmount = await this._Branches.PurCategoryTotalAmount(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDTodayGP = await this._Branches.PurCategoryGPPercent(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDMTDamount = await this._Branches.PurCategoryTotalAmount(MonthStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDYTDamount = await this._Branches.PurCategoryTotalAmount(YearStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDLMTDamount = await this._Branches.PurCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDPreyearamount = await this._Branches.PurCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKINWARD");


            TypList STOCKINWARDprofits = new TypList();
            var StkcategoryMap = new Dictionary<string, CategoryList>();
            STOCKINWARDprofits.Type = "STOCK INWARD";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new CategoryList
                {
                    Category = item.Catgry,
                    PrevYear = item.Amount
                };
                StkcategoryMap[item.Catgry] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (StkcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (StkcategoryMap.TryGetValue(item.Catgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            STOCKINWARDprofits.categoryLists = StkcategoryMap.Values.ToList();

            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Purprofits);
            ProfitList.Add(PurRetprofits);
            ProfitList.Add(STOCKINWARDprofits);
            return ProfitList;
        }

        [HttpGet("GetSubCategoryPurchaseProfit")]
        public async Task<List<TypList>> GetSubCategoryPurchaseProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, today.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            List<TotalSummary> PurTodayAmount = await this._Branches.PurSubCategoryTotalAmount(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurTodayGP = await this._Branches.PurSubCategoryGPPercent(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurMTDamount = await this._Branches.PurSubCategoryTotalAmount(MonthStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurYTDamount = await this._Branches.PurSubCategoryTotalAmount(YearStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurLMTDamount = await this._Branches.PurSubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASE");
            List<TotalSummary> PurPreyearamount = await this._Branches.PurSubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASE");

            TypList Purprofits = new TypList();

            var categoryMap = new Dictionary<string, SubCategoryList>();
            Purprofits.Type = "PURCHASE";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                categoryMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (categoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            Purprofits.subCategoryLists = categoryMap.Values.ToList();


            //PURCHASERET

            List<TotalSummary> PurRetTodayAmount = await this._Branches.PurSubCategoryTotalAmount(TodayStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetMTDamount = await this._Branches.PurSubCategoryTotalAmount(MonthStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetYTDamount = await this._Branches.PurSubCategoryTotalAmount(YearStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetLMTDamount = await this._Branches.PurSubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetPreyearamount = await this._Branches.PurSubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASERET");

            TypList PurRetprofits = new TypList();
            var PurCategryLst = new Dictionary<string, SubCategoryList>();
            PurRetprofits.Type = "PURCHASE RETURN";
            foreach (var item in PurRetPreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                PurCategryLst[item.SbCatgry] = categoryList;
            }

            foreach (var item in PurRetYTDamount)
            {
                if (PurCategryLst.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurRetLMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurRetMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurRetTodayAmount)
            {
                if (PurCategryLst.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }


            PurRetprofits.subCategoryLists = PurCategryLst.Values.ToList();
            //STOCKOUTWARD

            List<TotalSummary> STOCKINWARDTodayAmount = await this._Branches.PurSubCategoryTotalAmount(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDTodayGP = await this._Branches.PurSubCategoryGPPercent(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDMTDamount = await this._Branches.PurSubCategoryTotalAmount(MonthStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDYTDamount = await this._Branches.PurSubCategoryTotalAmount(YearStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDLMTDamount = await this._Branches.PurSubCategoryTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDPreyearamount = await this._Branches.PurSubCategoryTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKINWARD");


            TypList STOCKINWARDprofits = new TypList();
            var StkcategoryMap = new Dictionary<string, SubCategoryList>();
            STOCKINWARDprofits.Type = "STOCK INWARD";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new SubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    PrevYear = item.Amount
                };
                StkcategoryMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (StkcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (StkcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            STOCKINWARDprofits.subCategoryLists = StkcategoryMap.Values.ToList();
            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Purprofits);
            ProfitList.Add(PurRetprofits);
            ProfitList.Add(STOCKINWARDprofits);
            return ProfitList;
        }

        [HttpGet("GetMFRPurchaseProfit")]
        public async Task<List<TypList>> GetMFRPurchaseProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            //Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);
            var today = Date;
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, today.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            List<TotalSummary> PurTodayAmount = await this._Branches.PurMFRTotalAmount(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurTodayGP = await this._Branches.PurMFRGPPercent(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurMTDamount = await this._Branches.PurMFRTotalAmount(MonthStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurYTDamount = await this._Branches.PurMFRTotalAmount(YearStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurLMTDamount = await this._Branches.PurMFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASE");
            List<TotalSummary> PurPreyearamount = await this._Branches.PurMFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASE");

            TypList Purprofits = new TypList();

            var categoryMap = new Dictionary<string, MFRCategoryList>();
            Purprofits.Type = "PURCHASE";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                categoryMap[item.MFR] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (categoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            Purprofits.MFRCategoryLists = categoryMap.Values.ToList();


            //PURCHASERET

            List<TotalSummary> PurRetTodayAmount = await this._Branches.PurMFRTotalAmount(TodayStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetMTDamount = await this._Branches.PurMFRTotalAmount(MonthStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetYTDamount = await this._Branches.PurMFRTotalAmount(YearStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetLMTDamount = await this._Branches.PurMFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetPreyearamount = await this._Branches.PurMFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASERET");

            TypList PurRetprofits = new TypList();
            var PurCategryLst = new Dictionary<string, MFRCategoryList>();
            PurRetprofits.Type = "PURCHASE RETURN";
            foreach (var item in PurRetPreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                PurCategryLst[item.MFR] = categoryList;
            }

            foreach (var item in PurRetYTDamount)
            {
                if (PurCategryLst.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurRetLMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurRetMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurRetTodayAmount)
            {
                if (PurCategryLst.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }


            PurRetprofits.MFRCategoryLists = PurCategryLst.Values.ToList();

            //STOCKOUTWARD

            List<TotalSummary> STOCKINWARDTodayAmount = await this._Branches.PurMFRTotalAmount(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDTodayGP = await this._Branches.PurMFRGPPercent(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDMTDamount = await this._Branches.PurMFRTotalAmount(MonthStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDYTDamount = await this._Branches.PurMFRTotalAmount(YearStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDLMTDamount = await this._Branches.PurMFRTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDPreyearamount = await this._Branches.PurMFRTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKINWARD");


            TypList STOCKINWARDprofits = new TypList();
            var StkcategoryMap = new Dictionary<string, MFRCategoryList>();
            STOCKINWARDprofits.Type = "STOCK INWARD";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new MFRCategoryList
                {
                    MFR = item.MFR,
                    PrevYear = item.Amount
                };
                StkcategoryMap[item.MFR] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (StkcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (StkcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }

            STOCKINWARDprofits.MFRCategoryLists = StkcategoryMap.Values.ToList();

            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Purprofits);
            ProfitList.Add(PurRetprofits);
            ProfitList.Add(STOCKINWARDprofits);
            return ProfitList;
        }

        [HttpGet("GetSupplierPurchaseProfit")]
        public async Task<List<TypList>> GetSupplierPurchaseProfit(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            var MonthStart = new DateTime(today.Year, today.Month, 1);
            var YearStart = CurrentYear.FINSTART.Value;
            var lastMonth = today.AddMonths(-1).Date;
            var LMTDStart = new DateTime(today.Year, lastMonth.Month, today.Day);
            var lastYear = today.AddYears(-1).Date;
            var PreyearStart = new DateTime(lastYear.Year, CurrentYear.FINSTART.Value.Month, CurrentYear.FINSTART.Value.Day);
            var PreyearEnd = new DateTime(lastYear.AddYears(1).Year, CurrentYear.FINEND.Value.Month, CurrentYear.FINEND.Value.Day);
            LMTDDates Dates = await GETLMTD(Date);

            List<TotalSummary> PurTodayAmount = await this._Branches.PurSupplierTotalAmount(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurTodayGP = await this._Branches.PurSupplierGPPercent(TodayStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurMTDamount = await this._Branches.PurSupplierTotalAmount(MonthStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurYTDamount = await this._Branches.PurSupplierTotalAmount(YearStart, Todayend, varbranch, "PURCHASE");
            List<TotalSummary> PurLMTDamount = await this._Branches.PurSupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASE");
            List<TotalSummary> PurPreyearamount = await this._Branches.PurSupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASE");

            TypList Purprofits = new TypList();

            var categoryMap = new Dictionary<string, SupplierCategoryList>();
            Purprofits.Type = "PURCHASE";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                categoryMap[item.Supplier] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (categoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }
            Purprofits.SupplierCategoryLists = categoryMap.Values.ToList();





            //PURCHASERET

            List<TotalSummary> PurRetTodayAmount = await this._Branches.PurSupplierTotalAmount(TodayStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetMTDamount = await this._Branches.PurSupplierTotalAmount(MonthStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetYTDamount = await this._Branches.PurSupplierTotalAmount(YearStart, Todayend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetLMTDamount = await this._Branches.PurSupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "PURCHASERET");
            List<TotalSummary> PurRetPreyearamount = await this._Branches.PurSupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "PURCHASERET");

            TypList PurRetprofits = new TypList();
            var PurCategryLst = new Dictionary<string, SupplierCategoryList>();
            PurRetprofits.Type = "PURCHASE RETURN";
            foreach (var item in PurRetPreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                PurCategryLst[item.Supplier] = categoryList;
            }

            foreach (var item in PurRetYTDamount)
            {
                if (PurCategryLst.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurRetLMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurRetMTDamount)
            {
                if (PurCategryLst.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurRetTodayAmount)
            {
                if (PurCategryLst.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }


            PurRetprofits.SupplierCategoryLists = PurCategryLst.Values.ToList();
            //STOCKOUTWARD

            List<TotalSummary> STOCKINWARDTodayAmount = await this._Branches.PurSupplierTotalAmount(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDTodayGP = await this._Branches.PurSupplierGPPercent(TodayStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDMTDamount = await this._Branches.PurSupplierTotalAmount(MonthStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDYTDamount = await this._Branches.PurSupplierTotalAmount(YearStart, Todayend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDLMTDamount = await this._Branches.PurSupplierTotalAmount(Dates.LMTDstart, Dates.LMTDend, varbranch, "STOCKINWARD");
            List<TotalSummary> STOCKINWARDPreyearamount = await this._Branches.PurSupplierTotalAmount(PreyearStart, PreyearEnd, varbranch, "STOCKINWARD");


            TypList STOCKINWARDprofits = new TypList();
            var StkcategoryMap = new Dictionary<string, SupplierCategoryList>();
            STOCKINWARDprofits.Type = "STOCK INWARD";
            foreach (var item in PurPreyearamount)
            {
                var categoryList = new SupplierCategoryList
                {
                    Supplier = item.Supplier,
                    PrevYear = item.Amount
                };
                StkcategoryMap[item.Supplier] = categoryList;
            }

            foreach (var item in PurYTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.YTD = item.Amount;
                }
            }

            foreach (var item in PurLMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.LMTD = item.Amount;
                }
            }

            foreach (var item in PurMTDamount)
            {
                if (StkcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MTD = item.Amount;
                }
            }

            foreach (var item in PurTodayAmount)
            {
                if (StkcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Today = item.Amount;
                }
            }

            foreach (var item in PurTodayGP)
            {
                if (StkcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.GpRate = item.GpdivAmt > 0 ? (item.GpAmt / item.GpdivAmt) * 100 : 0;
                }
            }
            STOCKINWARDprofits.SupplierCategoryLists = StkcategoryMap.Values.ToList();


            List<TypList> ProfitList = new List<TypList>();

            ProfitList.Add(Purprofits);
            ProfitList.Add(PurRetprofits);
            ProfitList.Add(STOCKINWARDprofits);
            return ProfitList;
        }

        //CLOSING STOCK

        [HttpGet("GetClosingStock")]
        public async Task<List<ClosingStock>> GetClosingStock(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {


            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);


            TotalSummary ClosingMRP = await this._Branches.ClosingStock(TodayStart, Todayend, varbranch, "MRP");
            TotalSummary ClosingCost = await this._Branches.ClosingStock(TodayStart, Todayend, varbranch, "COST");
            TotalSummary ClosingGST = await this._Branches.ClosingStock(TodayStart, Todayend, varbranch, "COSTGST");


            ClosingStock ClsStk = new ClosingStock();

            ClsStk.Type = "CLOSINGSTOCK";
            ClsStk.MRP = ClosingMRP.StkValue;
            ClsStk.Cost = ClosingCost.StkValue;
            ClsStk.CostWithGST = ClosingGST.StkValue;



            List<ClosingStock> ClosingstkList = new List<ClosingStock>();

            ClosingstkList.Add(ClsStk);


            return ClosingstkList;
        }

        [HttpGet("GetCategoryClosingStock")]
        public async Task<List<ClsTypList>> GetCategoryClosingStock(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {


            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);


            List<TotalSummary> ClosingMRP = await this._Branches.ClosingStockCategory(TodayStart, Todayend, varbranch, "MRP");
            List<TotalSummary> ClosingCost = await this._Branches.ClosingStockCategory(TodayStart, Todayend, varbranch, "COST");
            List<TotalSummary> ClosingGST = await this._Branches.ClosingStockCategory(TodayStart, Todayend, varbranch, "COSTGST");

            ClsTypList ClsingCat = new ClsTypList();

            var StkcategoryMap = new Dictionary<long, ClsCategoryList>();
            ClsingCat.Type = "CLOSINGSTOCK";
            foreach (var item in ClosingGST)
            {
                var categoryList = new ClsCategoryList
                {
                    Category = item.Catgry,
                    CostWithGST = item.StkValue,
                    CategoryId = item.CatgryId
                };
                StkcategoryMap[item.CatgryId] = categoryList;
            }

            foreach (var item in ClosingCost)
            {
                if (StkcategoryMap.TryGetValue(item.CatgryId, out var categoryList))
                {
                    categoryList.Cost = item.StkValue;
                }
            }

            foreach (var item in ClosingMRP)
            {
                if (StkcategoryMap.TryGetValue(item.CatgryId, out var categoryList))
                {
                    categoryList.MRP = item.StkValue;
                }
            }


            ClsingCat.ClscategoryLists = StkcategoryMap.Values.ToList();



            List<ClsTypList> ClsngLst = new List<ClsTypList>();

            ClsngLst.Add(ClsingCat);

            return ClsngLst;
        }

        [HttpGet("GetSubCategoryClosingStock")]
        public async Task<List<ClsTypList>> GetSubCategoryClosingStock(DateTimeOffset Date, [FromQuery] List<long> Branches, long CatId)
        {

            string varbranch = string.Join(",", Branches);

            var today = Date;
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            List<TotalSummary> ClosingMRP = await this._Branches.ClosingStockSubCategory(TodayStart, Todayend, varbranch, "MRP");
            List<TotalSummary> ClosingCost = await this._Branches.ClosingStockSubCategory(TodayStart, Todayend, varbranch, "COST");
            List<TotalSummary> ClosingGST = await this._Branches.ClosingStockSubCategory(TodayStart, Todayend, varbranch, "COSTGST");

            ClsTypList ClsingCat = new ClsTypList();
            var StkcategoryMap = new Dictionary<string, ClsSubCategoryList>();
            ClsingCat.Type = "CLOSINGSTOCK";
            foreach (var item in ClosingGST)
            {
                var categoryList = new ClsSubCategoryList
                {
                    SubCategory = item.SbCatgry,
                    CostWithGST = item.StkValue,
                    CategoryId = item.CatgryId

                };
                StkcategoryMap[item.SbCatgry] = categoryList;
            }

            foreach (var item in ClosingCost)
            {
                if (StkcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.Cost = item.StkValue;
                }
            }

            foreach (var item in ClosingMRP)
            {
                if (StkcategoryMap.TryGetValue(item.SbCatgry, out var categoryList))
                {
                    categoryList.MRP = item.StkValue;
                }
            }



            ClsingCat.ClssubCategoryLists = StkcategoryMap.Values.ToList();

            if (CatId != 0 && CatId != null)
            {
                ClsingCat.ClssubCategoryLists = ClsingCat.ClssubCategoryLists.Where(y => y.CategoryId == CatId).ToList();
            }

            List<ClsTypList> ClsngLst = new List<ClsTypList>();

            ClsngLst.Add(ClsingCat);


            return ClsngLst;
        }

        [HttpGet("GetMFRClosingStock")]
        public async Task<List<ClsTypList>> GetMFRClosingStock(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {

            string varbranch = string.Join(",", Branches);

            var today = Date;
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            List<TotalSummary> ClosingMRP = await this._Branches.ClosingStockMFR(TodayStart, Todayend, varbranch, "MRP");
            List<TotalSummary> ClosingCost = await this._Branches.ClosingStockMFR(TodayStart, Todayend, varbranch, "COST");
            List<TotalSummary> ClosingGST = await this._Branches.ClosingStockMFR(TodayStart, Todayend, varbranch, "COSTGST");

            ClsTypList ClsingCat = new ClsTypList();
            var StkcategoryMap = new Dictionary<string, ClsMFRCategoryList>();
            ClsingCat.Type = "CLOSINGSTOCK";
            foreach (var item in ClosingGST)
            {
                var categoryList = new ClsMFRCategoryList
                {
                    MFR = item.MFR,
                    CostWithGST = item.StkValue
                };
                StkcategoryMap[item.MFR] = categoryList;
            }

            foreach (var item in ClosingCost)
            {
                if (StkcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.Cost = item.StkValue;
                }
            }

            foreach (var item in ClosingMRP)
            {
                if (StkcategoryMap.TryGetValue(item.MFR, out var categoryList))
                {
                    categoryList.MRP = item.StkValue;
                }
            }


            ClsingCat.ClsMFRCategoryLists = StkcategoryMap.Values.ToList();


            List<ClsTypList> ClsngLst = new List<ClsTypList>();

            ClsngLst.Add(ClsingCat);

            return ClsngLst;
        }

        [HttpGet("GetSupplierClosingStock")]
        public async Task<List<ClsTypList>> GetSupplierClosingStock(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {

            string varbranch = string.Join(",", Branches);
            var today = Date;
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            List<TotalSummary> ClosingMRP = await this._Branches.ClosingStockSupplier(TodayStart, Todayend, varbranch, "MRP");
            List<TotalSummary> ClosingCost = await this._Branches.ClosingStockSupplier(TodayStart, Todayend, varbranch, "COST");
            List<TotalSummary> ClosingGST = await this._Branches.ClosingStockSupplier(TodayStart, Todayend, varbranch, "COSTGST");

            ClsTypList ClsingCat = new ClsTypList();
            var StkcategoryMap = new Dictionary<string, ClsSupplierCategoryList>();
            ClsingCat.Type = "CLOSINGSTOCK";
            foreach (var item in ClosingGST)
            {
                var categoryList = new ClsSupplierCategoryList
                {
                    Supplier = item.Supplier,
                    CostWithGST = item.StkValue
                };
                StkcategoryMap[item.Supplier] = categoryList;
            }

            foreach (var item in ClosingCost)
            {
                if (StkcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.Cost = item.StkValue;
                }
            }

            foreach (var item in ClosingMRP)
            {
                if (StkcategoryMap.TryGetValue(item.Supplier, out var categoryList))
                {
                    categoryList.MRP = item.StkValue;
                }
            }


            ClsingCat.ClsSupplierCategoryLists = StkcategoryMap.Values.ToList();

            List<ClsTypList> ClsngLst = new List<ClsTypList>();

            ClsngLst.Add(ClsingCat);

            return ClsngLst;
        }

        [HttpGet("GetTopSales")]
        public async Task<List<TopSales>> GetTopSales(DateTimeOffset Date)
        {
            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            var YearStart = CurrentYear.FINSTART.Value;
            var today = Date;
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            return await this._Branches.TopSales(YearStart, Todayend);
        }
        [HttpGet("GetSalesbyBranch")]
        public async Task<List<TopSales>> GetSalesbyBranch(DateTimeOffset Date)
        {
            //  Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            var YearStart = CurrentYear.FINSTART.Value;
            var today = Date;
            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);
            List<TopSalesLst> Topsalesbrnch = await this._Branches.SalesbyBranch(YearStart, Todayend);


            List<TopSales> Salestop = new List<TopSales>();

            foreach (var item in Topsalesbrnch)
            {
                var SalesCatgrylst = new TopSales();
                SalesCatgrylst.Branch = item.Branch;
                SalesCatgrylst.Amount = item.NetAmt > 0 ? (item.Amount / item.NetAmt) * 100 : 0;
                Salestop.Add(SalesCatgrylst);
            }

            return Salestop;
        }


        [HttpGet("GetHomeDelivery")]
        public async Task<List<HomeDelivery>> GetHomeDelivery(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            //Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            TotalSummary HomeDelBilled = await this._Branches.Homedelivery(TodayStart, Todayend, varbranch, "BILLED");
            TotalSummary HomeDelDelivrd = await this._Branches.Homedelivery(TodayStart, Todayend, varbranch, "DELIVERED");
            TotalSummary HomePending = await this._Branches.Homedelivery(TodayStart, Todayend, varbranch, "PENDING");


            HomeDelivery HomeDelBilld = new HomeDelivery();

            HomeDelBilld.Status = "BILLED";
            HomeDelBilld.InvCnt = HomeDelBilled.InvCount;
            HomeDelBilld.InvAmt = HomeDelBilled.Amount;

            HomeDelivery HomeDel = new HomeDelivery();

            HomeDel.Status = "DELIVERED";
            HomeDel.InvCnt = HomeDelDelivrd.InvCount;
            HomeDel.InvAmt = HomeDelDelivrd.Amount;

            HomeDelivery HomeDelPen = new HomeDelivery();

            HomeDelPen.Status = "PENDING";
            HomeDelPen.InvCnt = HomePending.InvCount;
            HomeDelPen.InvAmt = HomePending.Amount;

            List<HomeDelivery> homeDeliveries = new List<HomeDelivery>();

            homeDeliveries.Add(HomeDelBilld);
            homeDeliveries.Add(HomeDel);
            homeDeliveries.Add(HomeDelPen);

            return homeDeliveries;
        }

        [HttpGet("GetHomeDeliveryBranch")]
        public async Task<List<HomeDeliveryBrLst>> GetHomeDeliveryBranch(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);


            List<TotalSummary> HomeDelBill = await this._Branches.BranchHomedelivery(TodayStart, Todayend, varbranch, "BILLED");
            List<TotalSummary> Homedel = await this._Branches.BranchHomedelivery(TodayStart, Todayend, varbranch, "DELIVERED");
            List<TotalSummary> HomedelPen = await this._Branches.BranchHomedelivery(TodayStart, Todayend, varbranch, "PENDING");

            HomeDeliveryBrLst HomedelBilllst = new HomeDeliveryBrLst();

            // Create a dictionary to map categories to CategoryList objects
            var branchMap = new Dictionary<string, HomeDeliveryBranch>();
            HomedelBilllst.Status = "BILLED";
            foreach (var item in HomeDelBill)
            {
                var categoryList = new HomeDeliveryBranch
                {
                    Branch = item.Branch,
                    InvCnt = item.InvCount,
                    InvAmt = item.Amount
                };
                branchMap[item.Branch] = categoryList;
            }
            HomedelBilllst.HomeDeliveries = branchMap.Values.ToList();


            HomeDeliveryBrLst Homedelst = new HomeDeliveryBrLst();
            var branchMapdel = new Dictionary<string, HomeDeliveryBranch>();
            Homedelst.Status = "DELIVERED";
            foreach (var item in Homedel)
            {
                var categoryList = new HomeDeliveryBranch
                {
                    Branch = item.Branch,
                    InvCnt = item.InvCount,
                    InvAmt = item.Amount
                };
                branchMapdel[item.Branch] = categoryList;
            }
            Homedelst.HomeDeliveries = branchMapdel.Values.ToList();

            HomeDeliveryBrLst HomePenlst = new HomeDeliveryBrLst();

            var branchMapPen = new Dictionary<string, HomeDeliveryBranch>();

            HomePenlst.Status = "PENDING";
            foreach (var item in HomedelPen)
            {
                var categoryList = new HomeDeliveryBranch
                {
                    Branch = item.Branch,
                    InvCnt = item.InvCount,
                    InvAmt = item.Amount
                };
                branchMapPen[item.Branch] = categoryList;
            }
            HomePenlst.HomeDeliveries = branchMapPen.Values.ToList();


            List<HomeDeliveryBrLst> HomedelList = new List<HomeDeliveryBrLst>();

            HomedelList.Add(HomedelBilllst);
            HomedelList.Add(Homedelst);
            HomedelList.Add(HomePenlst);



            return HomedelList;
        }

        [HttpGet("GetBank")]
        public async Task<List<BankLst>> GetBank(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            // Finyear FinYear = await this._Branches.GetFinYear();
            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            BankandCash Bankbal = await this._Branches.BankandCash(TodayStart, Todayend, varbranch, "BANK");
            BankandCash SunDebit = await this._Branches.BankandCash(TodayStart, Todayend, varbranch, "DEBTORS");
            BankandCash SunCredit = await this._Branches.BankandCash(TodayStart, Todayend, varbranch, "CREDITORS");

            BankLst Banklst = new BankLst();

            Banklst.Type = "BANK";
            Banklst.BankBal = Bankbal.Balance;
            Banklst.SundryDebt = SunDebit.Balance;
            Banklst.SundryCredit = SunCredit.Balance;// SunCredit.Credit -SunCredit.RecievedAmt ;


            List<BankLst> Banklt = new List<BankLst>();
            Banklt.Add(Banklst);
            return Banklt;
        }

        [HttpGet("GetCash")]
        public async Task<List<CashLst>> GetCash(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {

            string varbranch = string.Join(",", Branches);

            var today = Date;
            var PrevDate = today.AddDays(-1).Date;
            var PrevDateEnd = PrevDate.AddDays(1).AddMilliseconds(-1);

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);

            BankandCash Cash = await this._Branches.BankandCash(TodayStart, Todayend, varbranch, "CASH");
            BankandCash PreviousCash = await this._Branches.BankandCash(PrevDate, PrevDateEnd, varbranch, "PRECASH");
            BankandCash Deposit = await this._Branches.BankandCash(TodayStart, Todayend, varbranch, "DEPOSIT");

            CashLst Banklst = new CashLst();

            Banklst.Type = "CASH";
            Banklst.NetBalance = Cash.Balance;
            Banklst.PrevBal = PreviousCash.Balance;

            if (Deposit != null)
            {
                Banklst.BankDeposit = Deposit.Balance;

            }


            List<CashLst> Banklt = new List<CashLst>();
            Banklt.Add(Banklst);
            return Banklt;
        }

        [HttpGet("GetBankBranch")]
        public async Task<List<BankBrnchLst>> GetBankBranch(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            //  Finyear FinYear = await this._Branches.GetFinYear();

            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);

            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);


            List<BankandCash> Bankbal = await this._Branches.BranchBankandCash(TodayStart, Todayend, varbranch, "BANK");
            // List<BankandCash> SunDebit = await this._Branches.BranchBankandCash(TodayStart, Todayend, varbranch, "DEBTORS");
            //  List<BankandCash> SunCredit = await this._Branches.BranchBankandCash(TodayStart, Todayend, varbranch, "CREDITORS");

            BankBrnchLst Purprofits = new BankBrnchLst();
            var categoryMap = new Dictionary<string, BranchBankLst>();
            Purprofits.Type = "BANK";
            foreach (var item in Bankbal)
            {
                var categoryList = new BranchBankLst
                {
                    Branch = item.Branch,
                    BankBal = item.Balance,
                    BranchId = item.Branchid
                };
                categoryMap[item.Branch] = categoryList;
            }

            //foreach (var item in SunDebit)
            //{
            //    if (categoryMap.TryGetValue(item.Branch, out var categoryList))
            //    {
            //        categoryList.SundryDebt  = item.Balance;
            //    }
            //    else
            //    {
            //        if (categoryList == null)
            //        {
            //            categoryList = new BranchBankLst();
            //        }
            //        categoryList.Branch = item.Branch;
            //        categoryList.BranchId = item.Branchid;
            //        categoryList.SundryDebt = item.Balance;
            //        categoryMap[item.Branch] = categoryList;
            //    }
            //}

            //foreach (var item in SunCredit)
            //{
            //    if (categoryMap.TryGetValue(item.Branch, out var categoryList))
            //    {
            //        categoryList.SundryCredit = item.Credit - item.RecievedAmt;
            //    }
            //   else
            //   {
            //            if (categoryList == null)
            //            {
            //                categoryList = new BranchBankLst();
            //            }
            //            categoryList.Branch = item.Branch;
            //            categoryList.BranchId = item.Branchid;
            //            categoryList.SundryCredit = item.Credit - item.RecievedAmt;
            //            categoryMap[item.Branch] = categoryList;
            //    }

            //}




            Purprofits.branchBankLsts = categoryMap.Values.ToList();



            List<BankBrnchLst> ProfitList = new List<BankBrnchLst>();

            ProfitList.Add(Purprofits);

            return ProfitList;
        }

        [HttpGet("GetCashBranch")]
        public async Task<List<CashBrnchLst>> GetCashBranch(DateTimeOffset Date, [FromQuery] List<long> Branches)
        {
            // Finyear FinYear = await this._Branches.GetFinYear();

            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);

            string varbranch = string.Join(",", Branches);

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);


            List<BankandCash> Netamt = await this._Branches.BranchBankandCash(TodayStart, Todayend, varbranch, "CASH");
            List<BankandCash> Prevamt = await this._Branches.BranchBankandCash(TodayStart, Todayend, varbranch, "PRECASH");
            List<BankandCash> Deposits = await this._Branches.BranchBankandCash(TodayStart, Todayend, varbranch, "DEPOSIT");

            CashBrnchLst Purprofits = new CashBrnchLst();
            var categoryMap = new Dictionary<string, BranchCashLst>();
            Purprofits.Type = "CASH";
            foreach (var item in Netamt)
            {
                var categoryList = new BranchCashLst
                {
                    Branch = item.Branch,
                    NetBalance = item.Balance
                };
                categoryMap[item.Branch] = categoryList;
            }

            foreach (var item in Prevamt)
            {
                if (categoryMap.TryGetValue(item.Branch, out var categoryList))
                {
                    categoryList.PrevBal = item.Balance;
                }
            }

            foreach (var item in Deposits)
            {
                if (categoryMap.TryGetValue(item.Branch, out var categoryList))
                {
                    categoryList.Deposit = item.Balance;
                }
            }
            Purprofits.branchCashLsts = categoryMap.Values.ToList();

            List<CashBrnchLst> ProfitList = new List<CashBrnchLst>();

            ProfitList.Add(Purprofits);

            return ProfitList;
        }


        [HttpGet("GetBranchBankSummary")]
        public async Task<List<BankBrnchLst>> GetBranchBankSummary(DateTimeOffset Date, long Branches)
        {
            //  Finyear FinYear = await this._Branches.GetFinYear();

            Finyear CurrentYear = await this._Branches.FinYearCalculate(Date);
            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);


            List<BankandCash> Bankbal = await this._Branches.BankSummary(TodayStart, Todayend, Branches, "BANK");
            List<BankandCash> SunDebit = await this._Branches.BankSummary(TodayStart, Todayend, Branches, "DEBTORS");
            List<BankandCash> SunCredit = await this._Branches.BankSummary(TodayStart, Todayend, Branches, "CREDITORS");

            BankBrnchLst Purprofits = new BankBrnchLst();
            var categoryMap = new Dictionary<string, BranchBankLst>();
            Purprofits.Type = "BANK";
            foreach (var item in Bankbal)
            {
                var categoryList = new BranchBankLst
                {
                    Bank = item.Bank,
                    BankBal = item.Balance
                };
                categoryMap[item.Bank] = categoryList;
            }

            //foreach (var item in SunDebit)
            //{
            //    if (categoryMap.TryGetValue(item.Bank, out var categoryList))
            //    {
            //        categoryList.SundryDebt = item.Balance;
            //    }
            //    else
            //    {
            //        if (categoryList == null)
            //        {
            //            categoryList = new BranchBankLst();
            //        }
            //        categoryList.Bank = item.Bank;
            //        categoryList.SundryDebt = item.Balance;
            //        categoryMap[item.Bank] = categoryList;
            //    }
            //}

            //foreach (var item in SunCredit)
            //{
            //    if (categoryMap.TryGetValue(item.Bank, out var categoryList))
            //    {
            //        categoryList.SundryCredit = item.Credit - item.RecievedAmt;
            //    }
            //    else
            //    {
            //        if (categoryList == null)
            //        {
            //             categoryList = new BranchBankLst();
            //        }
            //        categoryList.Bank = item.Bank;
            //        categoryList.SundryCredit = item.Credit - item.RecievedAmt;
            //        categoryMap[item.Bank] = categoryList;
            //    }
            //}




            Purprofits.branchBankLsts = categoryMap.Values.ToList();



            List<BankBrnchLst> ProfitList = new List<BankBrnchLst>();

            ProfitList.Add(Purprofits);

            return ProfitList;
        }

        [HttpGet("GetCashBankSummary")]
        public async Task<List<CashBrnchLst>> GetCashBankSummary(DateTimeOffset Date, long Branches)
        {

            var today = Date;

            DateTimeOffset TodayStart = today.Date;
            DateTimeOffset Todayend = today.Date.AddDays(1).AddMilliseconds(-1);


            List<BankandCash> Netamt = await this._Branches.BankSummary(TodayStart, Todayend, Branches, "CASH");
            List<BankandCash> Prevamt = await this._Branches.BankSummary(TodayStart, Todayend, Branches, "PRECASH");

            CashBrnchLst Purprofits = new CashBrnchLst();
            var categoryMap = new Dictionary<string, BranchCashLst>();
            Purprofits.Type = "CASH";
            foreach (var item in Netamt)
            {
                var categoryList = new BranchCashLst
                {
                    Bank = item.Bank,
                    NetBalance = item.Balance
                };
                categoryMap[item.Bank] = categoryList;
            }

            foreach (var item in Prevamt)
            {
                if (categoryMap.TryGetValue(item.Bank, out var categoryList))
                {
                    categoryList.PrevBal = item.Balance;
                }
            }
            Purprofits.branchCashLsts = categoryMap.Values.ToList();

            List<CashBrnchLst> ProfitList = new List<CashBrnchLst>();

            ProfitList.Add(Purprofits);

            return ProfitList;
        }

        [HttpGet("GetCustomerLocation")]
        public async Task<CustomerLocation> GetCustomerLocation(long AccountID)
        {
            return await this._Branches.CustomerLocation(AccountID);
        }

        [HttpPost("UpdateCustomerLocation")]
        public async Task<IActionResult> UpdateCustomerLocation(long AccountID, string Longitude, string Latitude)
        {
            var result = await this._Branches.UpdateCustomerLocation(AccountID, Longitude, Latitude);

            if (result != null)
            {
                return Ok(result); // Return the updated account details
            }
            else
            {
                return NotFound(new { Message = "Account ID not found or update failed" });
            }
        }

        [HttpGet("GetListOfDeliveries")]
        public async Task<IActionResult> GetListOfDeliveries(long employeeID, int? status, DateTimeOffset date)
        {
            var result = await _Branches.GetDeliveries(employeeID, status, date);

            if (result != null && result.Any())
                return Ok(result);
            else
                return NotFound(new { Message = "No deliveries found" });
        }




        [HttpPost("UpdateDelivery")]
        public async Task<IActionResult> UpdateDelivery([FromBody] Delivery delivery)
        {
            if (delivery == null || delivery.DLID <= 0)
            {
                return BadRequest(new { Message = "Invalid delivery data." });
            }

            try
            {
                var updatedDelivery = await _Branches.UpdateDelivery(delivery);

                if (updatedDelivery == null)
                {
                    return NotFound(new { Message = "Delivery not found or update failed." });
                }

                return Ok(updatedDelivery);  // Return the updated delivery
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });  // Handle errors from validation checks
            }
        }


        [HttpGet("GetSalesDetails")]
        public async Task<IActionResult> GetSalesDetails(long DLSALEID, long DLEMPID)
        {
            // Call the service to get the sales details
            var salesDetails = await _Branches.GetSalesDetails(DLSALEID, DLEMPID);

            // Return the sales details as a response
            return Ok(salesDetails);
        }

        [HttpGet("GetSummaryDetails")]
        public async Task<IActionResult> GetSummaryDetails(long employeeID, DateTimeOffset date)
        {
            var result = await _Branches.GetSummaryDetails(employeeID, date);

            if (result != null)
                return Ok(result);
            else
                return NotFound(new { Message = "No summary details found" });
        }


        [HttpGet("GetClosingReport")]
        public async Task<IActionResult> GetClosingReport(long employeeID, DateTimeOffset date)
        {
            var result = await _Branches.GetClosingReport(employeeID, date);

            if (result != null)
                return Ok(result);
            else
                return NotFound(new { Message = "No closing report found" });
        }

        //[HttpPost("upload-payment-qr")]
        //public IActionResult UploadPaymentQR(IFormFile image)
        //{
        //    // Get the configured Company QR path from ImageSettings
        //    string companyQRPath = _configuration["ImageSettings:image_path"];

        //    if (string.IsNullOrEmpty(companyQRPath))
        //    {
        //        return BadRequest("Company QR path is not configured.");
        //    }

        //    // Ensure the directory exists
        //    string absolutePath = Path.GetFullPath(companyQRPath);
        //    if (!Directory.Exists(absolutePath))
        //    {
        //        Directory.CreateDirectory(absolutePath);
        //    }

        //    // Save the uploaded QR image with a defined name
        //    string fileName = "PaymentQR.png";  // You can change this name or generate dynamically
        //    string filePath = Path.Combine(absolutePath, fileName);

        //    using (var fileStream = new FileStream(filePath, FileMode.Create))
        //    {
        //        image.CopyTo(fileStream);
        //    }

        //    return Ok("QR code uploaded successfully.");
        //}

        // Optionally, you can add a method to retrieve the uploaded QR code image
        //[HttpGet("get-payment-qr")]
        //public IActionResult GetPaymentQR()
        //{
        //    var companyQRPath = _configuration["ImageSettings:Company_QR_path"];
        //    if (string.IsNullOrEmpty(companyQRPath))
        //    {
        //        return BadRequest("Company QR path is not configured.");
        //    }

        //    string filePath = Path.Combine(companyQRPath, "PaymentQR.png");

        //    if (!System.IO.File.Exists(filePath))
        //    {
        //        return NotFound("QR code not found.");
        //    }

        //    var fileBytes = System.IO.File.ReadAllBytes(filePath);
        //    return File(fileBytes, "image/png");
        //}

        [HttpGet("get-payment-qr")]
        public IActionResult GetPaymentQR()
        {
            var companyQRPath = _configuration["ImageSettings:image_path"];

            if (string.IsNullOrEmpty(companyQRPath))
            {
                return BadRequest("Company QR path is not configured.");

            }
            var filePath = Path.Combine(companyQRPath, "PaymentQR.png");




            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("QR code not found !!");
            }

            // Read the file bytes and return as a file response
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "image/png");
        }

        [HttpGet("GetUPIdetails")]
        public IActionResult GetUPIdetails()
        {
            return Ok(_upiDetails);
        }

        [HttpGet("GetDeliveryReport")]
        public async Task<List<Delivery>> GetDeliveryReport(long employeeID, int? status, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            List<Delivery> result = new List<Delivery>();
            result = await _Branches.GetDeliveryReport(employeeID, status, startDate, endDate);

            return result;

            //if (result != null && result.Any())
            //    return Ok(result);
            //else
            //    return NotFound(new { Message = "No deliveries found" });
        }

        [HttpGet("GetAllEmployees")]
        public async Task<IActionResult>GetAllEmployees()
        {
            var employees = await _Branches.GetAllEmployees();
            return Ok(employees);

        }

    }
}
