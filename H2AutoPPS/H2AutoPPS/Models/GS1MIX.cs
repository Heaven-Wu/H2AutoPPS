

namespace H2AutoPPS.Models
{
    public class GS1MIX
    {
        public GS1MIX(string print, string ssccNO)
        {
            this.labelName = "GS1MIX.txt";
            this.print = print;
            this.SSCCNO = ssccNO;
        }

        public GS1MIX()
        {

        }

        public GS1MIX(string print)
        {
            this.labelName = "GS1MIX.txt";
            this.print = print;
        }
        public string labelName { get; set; }

        public string print { get; set; }

        public string SSCCNO { get; set; }

    }
}