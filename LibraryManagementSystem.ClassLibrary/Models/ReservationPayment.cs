using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class ReservationPayment
    {
        public int Id { get; set; }

        public int ReservationId { get; set; }

        public decimal BorrowFee { get; set; }

        public decimal DepositAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public DateTime PaidOn { get; set; }
    }
}
