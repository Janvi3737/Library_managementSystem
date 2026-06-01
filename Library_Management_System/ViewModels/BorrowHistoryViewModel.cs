namespace Library_Management_System.ViewModels
{
    public class BorrowHistoryViewModel
    {
        public int Id { get; set; }

        public string BookTitle { get; set; }

        public string Author { get; set; }

        public DateTime BorrowDate { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public decimal FineAmount { get; set; }

        public string Status { get; set; }
        public int BorrowId { get; set; }
    }
}
