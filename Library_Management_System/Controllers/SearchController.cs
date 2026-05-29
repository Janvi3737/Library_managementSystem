using LibraryManagementSystem.ClassLibrary.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class SearchController : Controller
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Route(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return RedirectToAction("Index", "Home");

        query = query.ToLower();

        // 📚 SEARCH BOOKS + AUTHORS
        bool bookMatch = await _context.Books
            .Include(b => b.Author)
            .AnyAsync(b =>
                b.Title.ToLower().Contains(query) ||
                (b.Description != null &&
                 b.Description.ToLower().Contains(query)) ||

                (b.Author != null &&
                 b.Author.Name.ToLower().Contains(query))
            );

        if (bookMatch)
        {
            return RedirectToAction(
                "Index",
                "Catalog",
                new { searchQuery = query }
            );
        }

        // 🎉 SEARCH EVENTS
        bool eventMatch = await _context.Events
            .AnyAsync(e =>
                e.Title.ToLower().Contains(query) ||

                (e.Description != null &&
                 e.Description.ToLower().Contains(query))
            );

        if (eventMatch)
        {
            return RedirectToAction(
                "Index",
                "Events",
                new { searchQuery = query }
            );
        }

        // 🔥 DEFAULT FALLBACK
        return RedirectToAction(
            "Index",
            "Catalog",
            new { searchQuery = query }
        );
    }
}
