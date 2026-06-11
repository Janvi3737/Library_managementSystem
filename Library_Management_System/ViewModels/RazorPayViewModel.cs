using LibraryManagementSystem.ClassLibrary.Models;

namespace Library_Management_System.ViewModels
{
    public class RazorPayViewModel
    {
        public Book Book { get; set; }

        public int Quantity { get; set; }

        public decimal BorrowFee { get; set; }

        public decimal SecurityDeposit { get; set; }

        public decimal TotalAmount { get; set; }

        public string RazorpayKey { get; set; }

        public string RazorpayOrderId { get; set; }
    }
}
