

namespace H2AutoPPS.Models
{
    public class NonEDIShpmrk
    {
        public NonEDIShpmrk( string print, string CUST, string SHIPTO, string MADEIN, string PLT, string MODE, string STRQTY, string STRMARK, string STRSHIPMARK, string OVERPACK, string SINO, string PALLID,string PTYPE, string Date)
        {
            this.labelName = "NONEDI_SHIPMARK.txt";
            this.print = print;
            this.CUST = CUST;
            this.SHIPTO = SHIPTO;
            this.MADEIN = MADEIN;
            this.PLT = PLT;
            this.MODE = MODE;
            this.STRQTY = STRQTY;
            this.STRMARK = STRMARK;
            this.STRSHIPMARK = STRSHIPMARK;
            this.OVERPACK = OVERPACK;
            this.SINO = SINO;
            this.PALLID = PALLID;
            this.PTYPE = PTYPE;
            this.DATE = Date;
        }

        public NonEDIShpmrk(string print)
        {
            this.print = print;
            this.labelName = "NONEDI_SHIPMARK.txt";
        }

        public NonEDIShpmrk()
        {
            
        }
        public string labelName { get; set; }

        public string print { get; set; }

        public string CUST { get; set; }

        public string SHIPTO { get; set; }

        public string MADEIN { get; set; }

        public string PLT { get; set; }

        public string MODE { get; set; }

        public string STRQTY { get; set; }

        public string STRMARK { get; set; }

        public string STRSHIPMARK { get; set; }

        public string OVERPACK { get; set; }

        public string SINO { get; set; }

        public string PALLID { get; set; }

        public string PTYPE { get; set; }

        public string DATE { get; set; }
    }
}