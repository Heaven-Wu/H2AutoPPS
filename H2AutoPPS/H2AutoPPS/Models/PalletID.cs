

namespace H2AutoPPS.Models
{
    public class PalletID
    {
        public PalletID( string print,string palletID,string QPN,string CPN,string Location, string QTY,string date)
        {
            this.labelName = "PalletID.txt";
            this.print = print;
            this.PALLET_ID = palletID;
            this.QPN = QPN;
            this.CPN = CPN;
            this.LOCATION = Location;
            this.QTY = QTY;
            this.DATE = date;
        }
        public PalletID()
        {

        }

        public PalletID(string print)
        {
            this.labelName = "PalletID.txt";
            this.print = print;
        }
        public string labelName { get; set; }

        public string print { get; set; }

        public string PALLET_ID { get; set; }

        public string QPN { get; set; }

        public string CPN { get; set; }

        public string LOCATION { get; set; }

        public string QTY { get; set; }

        public string DATE { get; set; }
    }
}