namespace H2AutoPPS.Models
{
    public class EDIShpmrk
    {
        public EDIShpmrk(string print,string pallet,string sumQty,string date,string Qty,string POE,string SPNO,string loadidNO,string pModel,string GS1,string SSCC,string location)
        {
            this.labelName = "EDI_SHIPMARK.txt";
            this.print = print;
            this.PLT = pallet;
            this.TTL = sumQty;
            this.DATE = date;
            this.STRQTY = Qty;
            this.SHIPTO = POE;
            this.STRSHIPMARK = SPNO;
            this.BLOADID = loadidNO;
            this.PALLID = pModel;
            this.SHPMK1 = GS1;
            this.SHPMK2 = SSCC;
            this.LOCNUM = location;
        }
        public EDIShpmrk()
        {
            
        }

        public EDIShpmrk(string print)
        {
            this.labelName = "EDI_SHIPMARK.txt";
            this.print = print;
        }

        public string labelName { get; set; }

        public string print { get; set; }

        public string PLT { get; set; }

        public string TTL { get; set; }

        public string DATE { get; set; }

        public string STRQTY { get; set; }

        public string SHIPTO { get; set; }

        public string STRSHIPMARK { get; set; }

        public string BLOADID { get; set; }

        public string PALLID { get; set; }

        public string SHPMK1 { get; set; }

        public string SHPMK2 { get; set; }

        public string LOCNUM { get; set; }

    }
}