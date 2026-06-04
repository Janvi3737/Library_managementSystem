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
}