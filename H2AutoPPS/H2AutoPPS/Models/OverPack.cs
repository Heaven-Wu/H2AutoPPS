

namespace H2AutoPPS.Models
{
    public class OverPack
    {
        public OverPack( string print)
        {
            this.labelName = "OverPack.txt";
            this.print = print;
        }
        public OverPack()
        {

        }
        public string labelName { get; set; }

        public string print { get; set; }
    }
}