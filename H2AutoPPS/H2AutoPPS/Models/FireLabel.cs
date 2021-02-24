

namespace H2AutoPPS.Models
{
    public class FireLabel
    {
        public FireLabel(string print)
        {
            this.labelName = "FireLabel.txt";
            this.print = print;
        }
        public FireLabel()
        {
        }
       
        public string labelName { get; set; }

        public string print { get; set; }
    }
}