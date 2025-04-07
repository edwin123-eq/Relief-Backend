using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Sale
    {
        public int Id { get; set; }
        public long BillTypeId { get; set; }
        public DateTimeOffset Date { get; set; }
        public string? DocNo { get; set; }
        public long DocNoNum { get; set; }
        public long GodownId { get; set; }
        public long QtnId { get; set; }
        public long OrdId { get; set; }
        public string? CashCredit { get; set; }
        public long CustId { get; set; }
        public string? CustName { get; set; }
        public string? CustAdd1 { get; set; }
        public string? CustAdd2 { get; set; }
        public string? CustAdd3 { get; set; }
        //public long DocNoNum { get; set; }
        //public long DocNoNum { get; set; }
        //public long DocNoNum { get; set; }
        //public long DocNoNum { get; set; }

    }
}
