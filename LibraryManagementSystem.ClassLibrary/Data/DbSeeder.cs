using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.ClassLibrary.Data
{
    /// <summary>
    /// Idempotent seed data. Runs after the schema is created.
    /// Each section checks `if (!table.Any())` so re-running on an existing
    /// database is a safe no-op — only the empty tables get seeded.
    ///
    /// Purpose: lets a fresh `dotnet run` immediately show data on the
    /// user-facing pages (Catalog, Events) without needing the user to
    /// register an admin account and enter data first. Once they DO add
    /// admin data, it will simply append to this seeded baseline.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Library settings — singleton row that the admin Settings page edits.
            if (!await db.LibrarySettings.AnyAsync())
            {
                db.LibrarySettings.Add(new LibrarySettings());
                await db.SaveChangesAsync();
            }

            // Categories — seed first, others reference them
            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Fiction" },
                    new Category { Name = "Non-Fiction" },
                    new Category { Name = "Self-Help" },
                    new Category { Name = "Technology" },
                    new Category { Name = "Biography" }
                );
                await db.SaveChangesAsync();
            }

            // Authors
            if (!await db.Authors.AnyAsync())
            {
                db.Authors.AddRange(
                    new Author { Name = "James Clear",      Bio = "Author of Atomic Habits." },
                    new Author { Name = "Robert Kiyosaki",  Bio = "Author of Rich Dad Poor Dad." },
                    new Author { Name = "Cal Newport",      Bio = "Author of Deep Work." },
                    new Author { Name = "Yuval Noah Harari", Bio = "Author of Sapiens." },
                    new Author { Name = "Paulo Coelho",     Bio = "Author of The Alchemist." }
                );
                await db.SaveChangesAsync();
            }

            // Departments (optional foreign key on Book)
            if (!await db.Departments.AnyAsync())
            {
                db.Departments.AddRange(
                    new Department { Name = "General",    Description = "Main floor — general circulation" },
                    new Department { Name = "Reference",  Description = "Reference materials (not borrowable)" },
                    new Department { Name = "Children",   Description = "Kids' books and picture books" }
                );
                await db.SaveChangesAsync();
            }

            // Books — only seed if no books exist
            if (!await db.Books.AnyAsync())
            {
                // Look up the FK ids we just inserted
                var fiction       = await db.Categories.FirstAsync(c => c.Name == "Fiction");
                var nonFiction    = await db.Categories.FirstAsync(c => c.Name == "Non-Fiction");
                var selfHelp      = await db.Categories.FirstAsync(c => c.Name == "Self-Help");

                var clear         = await db.Authors.FirstAsync(a => a.Name == "James Clear");
                var kiyosaki      = await db.Authors.FirstAsync(a => a.Name == "Robert Kiyosaki");
                var newport       = await db.Authors.FirstAsync(a => a.Name == "Cal Newport");
                var harari        = await db.Authors.FirstAsync(a => a.Name == "Yuval Noah Harari");
                var coelho        = await db.Authors.FirstAsync(a => a.Name == "Paulo Coelho");

                var general       = await db.Departments.FirstOrDefaultAsync(d => d.Name == "General");
                var generalId     = general?.Id;

                db.Books.AddRange(
                    new Book
                    {
                        Title = "Atomic Habits",
                        ISBN = "9780735211292",
                        AuthorId = clear.Id,
                        CategoryId = selfHelp.Id,
                        DepartmentId = generalId,
                        TotalCopies = 5,
                        AvailableCopies = 5,
                        TotalPages = 320,
                        IsFeatured = true,
                        Description = "An easy & proven way to build good habits & break bad ones.",
                        CoverImageUrl = "/images/books/AtomicHabits.jpeg"
                    },
                    new Book
                    {
                        Title = "Rich Dad Poor Dad",
                        ISBN = "9781612680194",
                        AuthorId = kiyosaki.Id,
                        CategoryId = selfHelp.Id,
                        DepartmentId = generalId,
                        TotalCopies = 3,
                        AvailableCopies = 3,
                        TotalPages = 336,
                        IsFeatured = true,
                        Description = "What the rich teach their kids about money that the poor and middle class do not.",
                        CoverImageUrl = "/images/books/RichDad.jpeg"
                    },
                    new Book
                    {
                        Title = "Deep Work",
                        ISBN = "9781455586691",
                        AuthorId = newport.Id,
                        CategoryId = selfHelp.Id,
                        DepartmentId = generalId,
                        TotalCopies = 4,
                        AvailableCopies = 4,
                        TotalPages = 304,
                        Description = "Rules for focused success in a distracted world.",
                        CoverImageUrl = "/images/books/deepwork.jpeg"
                    },
                    new Book
                    {
                        Title = "Sapiens",
                        ISBN = "9780062316097",
                        AuthorId = harari.Id,
                        CategoryId = nonFiction.Id,
                        DepartmentId = generalId,
                        TotalCopies = 4,
                        AvailableCopies = 4,
                        TotalPages = 464,
                        IsFeatured = true,
                        Description = "A brief history of humankind.",
                        CoverImageUrl = "/images/books/sapiens.jpeg"
                    },
                    new Book
                    {
                        Title = "The Alchemist",
                        ISBN = "9780062315007",
                        AuthorId = coelho.Id,
                        CategoryId = fiction.Id,
                        DepartmentId = generalId,
                        TotalCopies = 6,
                        AvailableCopies = 6,
                        TotalPages = 197,
                        Description = "A fable about following your dream.",
                        CoverImageUrl = "/images/books/TheAlchemist.jpeg"
                    }
                );
                await db.SaveChangesAsync();
            }

            // Events — seed both a future-dated event (visible on /Events)
            // and a past-dated event (visible on /Events/Past) so both pages
            // have data immediately after first run.
            if (!await db.Events.AnyAsync())
            {
                var now = DateTime.Now;
                db.Events.AddRange(
                    new Event
                    {
                        Title       = "Author Meet — James Clear",
                        Description = "Live Q&A and book signing with the author of Atomic Habits.",
                        Date        = now.AddDays(14),
                        Location    = "Main Hall",
                        ImagePath   = "/images/events/authormeet.jpg"
                    },
                    new Event
                    {
                        Title       = "Annual Book Fair",
                        Description = "Three days of new releases, discounts, and reader meetups.",
                        Date        = now.AddDays(30),
                        Location    = "Library Atrium",
                        ImagePath   = "/images/events/bookfair.jpg"
                    },
                    new Event
                    {
                        Title       = "Coding Workshop for Beginners",
                        Description = "Hands-on intro to programming using free public-domain resources.",
                        Date        = now.AddDays(7),
                        Location    = "Tech Lab",
                        ImagePath   = "/images/events/codingworkshop.jpg"
                    },
                    // A past event so the /Events/Past page also has content
                    new Event
                    {
                        Title       = "Children's Reading Hour (recap)",
                        Description = "Weekly story-time for kids aged 4–8. Past sessions archived.",
                        Date        = now.AddDays(-20),
                        Location    = "Children's Section",
                        ImagePath   = "/images/events/authormeet.jpg"
                    }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
