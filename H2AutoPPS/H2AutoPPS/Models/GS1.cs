

namespace H2AutoPPS.Models
{
    public class GS1
    {
        public GS1(string print,string modnum, string climat, string ssccNO, string count, string gitnno)
        {
            this.labelName = "GS1.txt";
            this.print = print;
            this.MODNUM = modnum;
            this.CLIMAT = climat;
            this.SSCCNO = ssccNO;
            this.CUNQTY = count;
            this.GTINNO = gitnno;
        }

        public GS1()
        {

        }

        public GS1(string print)
        {
            this.labelName = "GS1.txt";
            this.print = print;
        }
        public string labelName { get; set; }

        public string print { get; set; }

        public string SSCCNO { get; set; }

        public string MODNUM { get; set; }

        public string CLIMAT { get; set; }

        public string CUNQTY { get; set; }

        public string GTINNO { get; set; }

      
    }
}