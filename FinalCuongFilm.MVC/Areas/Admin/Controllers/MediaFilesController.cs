using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class MediaFilesController : Controller
    {
        private readonly CuongFilmDbContext _context;

        public MediaFilesController(CuongFilmDbContext context)
        {
            _context = context;
        }

        // GET: Admin/MediaFiles
        public async Task<IActionResult> Index()
        {
            var cuongFilmDbContext = _context.MediaFiles.Include(m => m.Episode).Include(m => m.Movie);
            return View(await cuongFilmDbContext.ToListAsync());
        }

        // GET: Admin/MediaFiles/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mediaFile = await _context.MediaFiles
                .Include(m => m.Episode)
                .Include(m => m.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mediaFile == null)
            {
                return NotFound();
            }

            return View(mediaFile);
        }

        // GET: Admin/MediaFiles/Create
        public IActionResult Create()
        {
            //ViewData["EpisodeId"] = new SelectList(_context.Episodes, "Id", "Title");
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Slug");
            return View();
        }

        // POST: Admin/MediaFiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FileName,Quality,FileFormat,FileSizeInBytes,MovieId,EpisodeId,CreatedAt")] MediaFile mediaFile)
        {
            if (ModelState.IsValid)
            {
                mediaFile.Id = Guid.NewGuid();
                _context.Add(mediaFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            //ViewData["EpisodeId"] = new SelectList(_context.Episodes, "Id", "Title", mediaFile.EpisodeId);
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Slug", mediaFile.MovieId);
            return View(mediaFile);
        }

        // GET: Admin/MediaFiles/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mediaFile = await _context.MediaFiles.FindAsync(id);
            if (mediaFile == null)
            {
                return NotFound();
            }
            //ViewData["EpisodeId"] = new SelectList(_context.Episodes, "Id", "Title", mediaFile.EpisodeId);
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Slug", mediaFile.MovieId);
            return View(mediaFile);
        }

        // POST: Admin/MediaFiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FileName,Quality,FileFormat,FileSizeInBytes,MovieId,EpisodeId,CreatedAt")] MediaFile mediaFile)
        {
            if (id != mediaFile.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mediaFile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MediaFileExists(mediaFile.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            //ViewData["EpisodeId"] = new SelectList(_context.Episodes, "Id", "Title", mediaFile.EpisodeId);
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Slug", mediaFile.MovieId);
            return View(mediaFile);
        }

        // GET: Admin/MediaFiles/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mediaFile = await _context.MediaFiles
                .Include(m => m.Episode)
                .Include(m => m.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mediaFile == null)
            {
                return NotFound();
            }

            return View(mediaFile);
        }

        // POST: Admin/MediaFiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var mediaFile = await _context.MediaFiles.FindAsync(id);
            if (mediaFile != null)
            {
                _context.MediaFiles.Remove(mediaFile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MediaFileExists(Guid id)
        {
            return _context.MediaFiles.Any(e => e.Id == id);
        }
    }
}
