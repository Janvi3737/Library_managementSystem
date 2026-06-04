// namespace Library_Management_System.ViewModels

//         public string BookTitle { get; set; }

//         public string Author { get; set; }

//         public DateTime BorrowDate { get; set; }

//         public DateTime DueDate { get; set; }

//         public DateTime? ReturnDate { get; set; }

//         public decimal FineAmount { get; set; }

//         public int DaysLate { get; set; }

// public decimal FinePerDay { get; set; }

// public bool FinePaid { get; set; }

//         public string Status { get; set; }


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

        public string Status { get; set; }

        // FINE MODULE

        public int DaysLate { get; set; }

        public decimal FinePerDay { get; set; }

        public decimal FineAmount { get; set; }

        public bool FinePaid { get; set; }
    }
}
