namespace H2AutoPPS.Models
{
    public class WebResult
    {
        public WebResult() {
            this.Status = 400;
        }

        public int Status { get; set; }
        public string Msg { get; set; }
        public object Data { get; set; }
    }
}