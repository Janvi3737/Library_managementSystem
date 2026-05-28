namespace Library_Management_System.ViewModels
{
    public class MemberDashboardViewModel
    {
        // MEMBER INFO

        public string MemberName { get; set; }

        public string? ProfileImage { get; set; }

        public string MembershipType { get; set; }

        public DateTime MembershipStartDate { get; set; }

        public DateTime MembershipEndDate { get; set; }

        // STATS

        public int CurrentBorrows { get; set; }

        public int Overdue { get; set; }

        public decimal FineDue { get; set; }

        public int WishListCount { get; set; }

        public int ReturnedBooks { get; set; }

        // COLLECTIONS

        public List<RecentActivityViewModel> RecentActivity { get; set; }
            = new();

        public List<MyBookViewModel> MyBooks { get; set; }
            = new();
    }

    public class MyBookViewModel
    {
        public int Id { get; set; }

        public string BookTitle { get; set; }

        public string Author { get; set; }

        public string? CoverImage { get; set; }

        public DateTime IssueDate { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsReturned { get; set; }

        public decimal FineAmount { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string Activity { get; set; }

        public string ActivityType { get; set; }

        public DateTime ActivityDate { get; set; }
    }
}
