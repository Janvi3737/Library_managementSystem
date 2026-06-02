namespace Library_Management_System.ViewModels
{
    public class MemberAnalyticsViewModel

    {
        public int TotalBorrowed { get; set; }

        public int CurrentBorrowed { get; set; }

        public int TotalReturned { get; set; }

        public int OverdueBooks { get; set; }

        public decimal TotalFine { get; set; }

        public int BooksThisMonth { get; set; }

        public int TotalWishlist { get; set; }

        public string FavoriteCategory { get; set; }

        public string FavoriteAuthor { get; set; }

        public double CompletionRate { get; set; }

        public List<MonthlyAnalyticsViewModel> MonthlyData { get; set; }
            = new();
    }

    public class MonthlyAnalyticsViewModel
    {
        public string Month { get; set; }

        public int Borrowed { get; set; }

        public int Returned { get; set; }

        public decimal Fine { get; set; }
    }
}
