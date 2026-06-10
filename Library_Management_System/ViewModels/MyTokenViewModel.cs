namespace Library_Management_System.ViewModels
{
    public class MyTokenViewModel
    {
        public int TotalBorrowCount { get; set; }
        public int RemainingBorrows { get; set; }
        public int AvailableTokens { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? PurchaseDate { get; set; }
    }
}
