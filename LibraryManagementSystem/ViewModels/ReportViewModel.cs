using System;
using System.Collections.Generic;

namespace LibraryManagementSystem.ViewModels
{
    public class ReportViewModel
    {
        // DASHBOARD COUNTS

        public int TotalBooks { get; set; }

        public int TotalUsers { get; set; }

        public int TotalIssuedBooks { get; set; }

        public int TotalOverdueBooks { get; set; }

        public decimal TotalFineCollection { get; set; }

        public int TotalMembers { get; set; }

        public int TotalAuthors { get; set; }

        public int TotalCategories { get; set; }

        public int TotalReservations { get; set; }

        public decimal TotalRevenue { get; set; }


        // Reviews
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }

        public List<ReviewVM> BookReviews { get; set; } = new();
        public List<string> RatingLabels { get; set; } = new()
{
"1 Star", "2 Star", "3 Star", "4 Star", "5 Star"
};

        public List<int> RatingCounts { get; set; } = new();


        // TABLES

        public List<MostBorrowedBookVM> MostBorrowedBooks { get; set; } = new();

        public List<LateReturnVM> LateReturns { get; set; } = new();

        public List<PendingReservationVM> PendingReservations { get; set; } = new();

        public List<IssuedBookVM> IssuedBooks { get; set; } = new();

        public List<ReturnedBookVM> ReturnedBooks { get; set; } = new();

        // CHARTS

        public List<string> BorrowChartLabels { get; set; } = new();

        public List<int> BorrowChartData { get; set; } = new();

        public List<string> CategoryLabels { get; set; } = new();

        public List<int> CategoryData { get; set; } = new();

        public List<string> FineChartLabels { get; set; } = new();

        public List<decimal> FineChartData { get; set; } = new();
    }


    // MOST BORROWED BOOKS

    public class MostBorrowedBookVM
    {
        public string BookName { get; set; } = "";

        public int BorrowCount { get; set; }
    }

    // LATE RETURNS

    public class LateReturnVM
    {
        public string BookName { get; set; } = "";

        public string MemberName { get; set; } = "";

        public DateTime DueDate { get; set; }

        public DateTime ReturnedOn { get; set; }

        public int LateDays { get; set; }

        public decimal FineAmount { get; set; }
    }

    // PENDING RESERVATIONS

    public class PendingReservationVM
    {
        public string BookName { get; set; } = "";

        public string MemberName { get; set; } = "";

        public DateTime ReservedOn { get; set; }
    }

    // ISSUED BOOKS

    public class IssuedBookVM
    {
        public string BookName { get; set; } = "";

        public string MemberName { get; set; } = "";

        public DateTime IssuedOn { get; set; }

        public DateTime DueDate { get; set; }
    }

    // RETURNED BOOKS

    public class ReturnedBookVM
    {
        public string BookName { get; set; } = "";

        public string MemberName { get; set; } = "";

        public DateTime IssuedOn { get; set; }

        public DateTime ReturnedOn { get; set; }

        public decimal FineAmount { get; set; }
    }


    public class ReviewVM
    {
        public string BookName { get; set; } = "";
        public string MemberName { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

}
