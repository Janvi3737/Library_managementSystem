using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class UserToken
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int AvailableTokens { get; set; }

        public int TotalBorrowCount { get; set; }

        public decimal DepositAmount { get; set; }

        public DateTime PurchaseDate { get; set; }

        public bool IsApproved { get; set; }

        public ApplicationUser User { get; set; }
    }
}
