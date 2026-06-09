using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class TokenPayment
    {
        public int Id { get; set; }

        public int UserTokenId { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; }

        public string TransactionId { get; set; }

        public string PaymentStatus { get; set; }

        public string ScreenshotPath { get; set; }

        public DateTime PaymentDate { get; set; }

        public UserToken UserToken { get; set; }
    }
}
