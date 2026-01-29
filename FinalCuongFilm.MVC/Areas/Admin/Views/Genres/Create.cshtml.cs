using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;

namespace FinalCuongFilm.MVC.Areas.Admin.Views
{
    public class CreateModel : PageModel
    {
        private readonly FinalCuongFilm.DataLayer.CuongFilmDbContext _context;

        public CreateModel(FinalCuongFilm.DataLayer.CuongFilmDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Slug");
            return Page();
        }

        [BindProperty]
        public Episode Episode { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Episodes.Add(Episode);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
