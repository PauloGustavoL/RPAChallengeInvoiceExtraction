namespace RPAChallengeInvoiceExtraction.Models
{
    public class CSVModels
    {
        public string numberID { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
        public string DueData { get; set; } = string.Empty; //talvez seja Datetime
        public string InvoiceNo { get; set; } = string.Empty;
        public string Invoicedate { get; set; } = string.Empty;    
        public string CompanyName { get; set; } = string.Empty;
        public double TotalDue { get; set; } = 0;

    }
}
