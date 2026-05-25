namespace LibraryManagementSystem.ViewModels
{
    public class RevenueRowViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal MembershipRevenue { get; set; }
        public decimal FineRevenue { get; set; }
        public decimal Total => MembershipRevenue + FineRevenue;

        public string MonthName =>
            System.Globalization.CultureInfo.CurrentCulture
                .DateTimeFormat.GetMonthName(Month);
    }
}
