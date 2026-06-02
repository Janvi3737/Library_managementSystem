using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Controllers
{
    public class EventsController : Controller
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // UPCOMING EVENTS
        public async Task<IActionResult> Index(string? searchQuery, int page = 1)
        {
            int pageSize = 6;

            var eventsQuery = _context.Events
                .Where(e => e.Date >= DateTime.Today)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(searchQuery) ||

                    (e.Description != null &&
                     e.Description.Contains(searchQuery)));
            }

            int totalEvents = await eventsQuery.CountAsync();

            // Ascending so the next-upcoming event appears first, not the
            // farthest-future one. Was OrderByDescending which pushed the
            // most-relevant event to the bottom of page 1.
            var upcomingEvents = await eventsQuery
                .OrderBy(e => e.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalEvents / pageSize);

            ViewBag.SearchQuery = searchQuery;

            return View(upcomingEvents);
        }
        // EVENT DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // PAST EVENTS
        public async Task<IActionResult> Past(int page = 1)
        {
            int pageSize = 6;

            var query = _context.Events
                .Where(e => e.Date < DateTime.Today)
                .OrderByDescending(e => e.Date);

            int totalEvents = await query.CountAsync();

            var pastEvents = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;

            ViewBag.TotalPages = (int)Math.Ceiling(
                totalEvents / (double)pageSize
            );

            return View(pastEvents);
        }
    }
}
