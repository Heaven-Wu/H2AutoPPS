

namespace H2AutoPPS.Models
{
    public class EDIWeight
    {
        //构造方法，并给属性赋值
        public EDIWeight(string print, string GW, string QTY,string loadid,string pallid, string location, string date,string time)
        {
            this.labelName = "EDI_Weight.txt";
            this.print = print;
            this.GW = GW;
            this.QTY = QTY;
            this.LOADID = loadid;
            this.PALLID = pallid;
            this.LOCATION = location;
            this.DATE = date;
            this.TIME = time;
        }

        public EDIWeight()
        {
        }
        public EDIWeight(string print)
        {
            this.labelName = "EDI_Weight.txt";
            this.print = print;
        }
        public string labelName { get; set; }

        public string print { get; set; }

        public string GW { get; set; }

        public string QTY { get; set; }

        public string LOADID { get; set; }

        public string PALLID { get; set; }

        public string LOCATION { get; set; }

        public string DATE { get; set; }

        public string TIME { get; set; }
    }
}