using System.Reflection.Emit;
using LibraryManagementSystem.ClassLibrary.Models;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Member> Members { get; set; }

        public DbSet<Author> Authors { get; set; }

        public DbSet<BorrowRecord> BorrowRecords { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Membership> Memberships { get; set; }

        public DbSet<Reservation> Reservations { get; set; }

        public DbSet<ContactMessage> ContactMessages { get; set; }

        public DbSet<Wishlist> Wishlists { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<MembershipPayment> MembershipPayments { get; set; }

        public DbSet<LibrarySettings> LibrarySettings { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<BookReview> BookReviews { get; set; }

        public DbSet<EventReservation> EventReservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // BOOK → CATEGORY

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // BOOK → AUTHOR

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // BOOK → DEPARTMENT

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Department)
                .WithMany(d => d.Books)
                .HasForeignKey(b => b.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // BORROW RECORD → BOOK

            modelBuilder.Entity<BorrowRecord>()
                .HasOne(br => br.Book)
                .WithMany(b => b.BorrowRecords)
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // BORROW RECORD → MEMBER

            modelBuilder.Entity<BorrowRecord>()
                .HasOne(br => br.Member)
                .WithMany(m => m.BorrowRecords)
                .HasForeignKey(br => br.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // MEMBERSHIP → MEMBER

            modelBuilder.Entity<Membership>()
                .HasOne(m => m.Member)
                .WithMany(mem => mem.Memberships)
                .HasForeignKey(m => m.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // RESERVATION → BOOK

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Book)
                .WithMany(b => b.Reservations)
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            // RESERVATION → USER

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Member)
                .WithMany()
                .HasForeignKey(r => r.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // INDEXES

            modelBuilder.Entity<BorrowRecord>()
                .HasIndex(b => b.BookId);

            modelBuilder.Entity<BorrowRecord>()
                .HasIndex(b => b.MemberId);

            modelBuilder.Entity<BorrowRecord>()
                .HasIndex(b => b.ReturnedOn);

            modelBuilder.Entity<BorrowRecord>()
                .HasIndex(b => b.DueDate);

            // DECIMAL CONFIGURATION

            modelBuilder.Entity<BorrowRecord>()
                .Property(b => b.FineAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<BorrowRecord>()
                .Property(b => b.FinePerDay)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Membership>()
                .Property(m => m.Fee)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MembershipPayment>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}
