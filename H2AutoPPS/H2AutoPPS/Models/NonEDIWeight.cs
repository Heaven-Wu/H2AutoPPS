

namespace H2AutoPPS.Models
{
    public class NonEDIWeight
    {
        //构造方法，并给属性赋值
        public NonEDIWeight(string print,string GW,string shpmrk,string date,string location)
        {
            this.labelName = "NONEDIWeight.txt";
            this.print = print;
            this.ACTUALWET = GW;
            this.SHIPMARK = shpmrk;
            this.PRINTTIME = date;
            this.RSLOC = location;
        }

        public NonEDIWeight()
        {
        }

        public NonEDIWeight(string print)
        {
            this.labelName = "NONEDIWeight.txt";
            this.print = print;
        }
        public string labelName { get; set; }

        public string print { get; set; }

        public string ACTUALWET { get; set; }

        public string SHIPMARK { get; set; }

        public string PRINTTIME { get; set; }

        public string RSLOC { get; set; }
    }
}