using AutoMapper;
using AutoMapper.QueryableExtensions;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinalCuongFilm.Service.Services
{
	public class MovieService : IMovieService
	{
		private readonly CuongFilmDbContext _context;
		private readonly IMapper _mapper;
		private readonly ILogger<MovieService> _logger;
		private readonly IAzureBlobService _azureBlobService;

		public MovieService(
			CuongFilmDbContext context,
			IMapper mapper,
			ILogger<MovieService> logger,
			IAzureBlobService azureBlobService)
		{
			_context = context;
			_mapper = mapper;
			_logger = logger;
			_azureBlobService = azureBlobService;
		}

		#region READ METHODS

		public async Task<IEnumerable<MovieDto>> GetAllAsync()
		{
			var movies = await _context.Movies
				.AsNoTracking()
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.CreatedAt)
				.Select(m => new MovieDto
				{
					Id = m.Id,
					Title = m.Title,
					Slug = m.Slug,
					ViewCount = m.ViewCount,
					CountryName = m.Country.Name,
					LanguageName = m.Language.Name
				})
				.ToListAsync();

			return movies;
		}

		public async Task<MovieDto?> GetByIdAsync(Guid id)
		{
			var movie = await _context.Movies
				.AsNoTracking()
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Include(m => m.Movie_Actors)
					.ThenInclude(ma => ma.Actor)
				.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

			return movie == null ? null : _mapper.Map<MovieDto>(movie);
		}

		public async Task<MovieDto?> GetBySlugAsync(string slug)
		{
			var movie = await _context.Movies
				.AsNoTracking()
				.FirstOrDefaultAsync(m => m.Slug == slug && m.IsActive);

			return movie == null ? null : _mapper.Map<MovieDto>(movie);
		}

		public async Task<IEnumerable<MovieDto>> SearchAsync(string keyword)
		{
			return await _context.Movies
				.AsNoTracking()
				.Where(m => m.IsActive &&
					(EF.Functions.Like(m.Title, $"%{keyword}%") ||
					 EF.Functions.Like(m.Description, $"%{keyword}%")))
				.OrderByDescending(m => m.ViewCount)
				.Select(m => new MovieDto
				{
					Id = m.Id,
					Title = m.Title,
					Slug = m.Slug,
					ViewCount = m.ViewCount
				})
				.ToListAsync();
		}

		#endregion

		#region CREATE / UPDATE

		public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
		{
			if (await _context.Movies.AnyAsync(m => m.Slug == dto.Slug))
				throw new Exception("Slug already exists");

			var movie = _mapper.Map<Movie>(dto);
			movie.CreatedAt = DateTime.UtcNow;
			movie.IsActive = true;
			movie.ViewCount = 0;

			_context.Movies.Add(movie);
			await _context.SaveChangesAsync();

			_logger.LogInformation($"Created movie: {movie.Title}");

			return _mapper.Map<MovieDto>(movie);
		}

		public async Task<MovieDto?> UpdateAsync(Guid id, MovieUpdateDto dto)
		{
			var movie = await _context.Movies.FindAsync(id);
			if (movie == null) return null;

			if (await _context.Movies
				.AnyAsync(m => m.Slug == dto.Slug && m.Id != id))
				throw new Exception("Slug already exists");

			_mapper.Map(dto, movie);
			movie.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			_logger.LogInformation($"Updated movie: {movie.Title}");

			return _mapper.Map<MovieDto>(movie);
		}

		#endregion

		#region DELETE

		public async Task<bool> DeleteAsync(Guid id)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			var movie = await _context.Movies
				.Include(m => m.MediaFiles)
				.Include(m => m.Episodes)
					.ThenInclude(e => e.MediaFiles)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (movie == null) return false;

			// Delete blobs first
			var allMedia = movie.MediaFiles
				.Concat(movie.Episodes.SelectMany(e => e.MediaFiles))
				.ToList();

			foreach (var media in allMedia)
			{
				try
				{
					await _azureBlobService.DeleteAsync(media.FileUrl);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, $"Failed to delete blob {media.FileUrl}");
				}
			}

			_context.Movies.Remove(movie);
			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return true;
		}

		#endregion

		#region VIEW COUNT (Race Condition Safe)

		public async Task IncrementViewCountAsync(Guid movieId)
		{
			await _context.Database.ExecuteSqlRawAsync(
				"UPDATE Movies SET ViewCount = ViewCount + 1 WHERE Id = {0}",
				movieId);

			_logger.LogInformation($"View incremented for movie {movieId}");
		}

		#endregion

		#region OTHER

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Movies.AnyAsync(m => m.Id == id);
		}

		public async Task<IEnumerable<MovieDto>> GetLatestAsync(int count = 12)
		{
			return await _context.Movies
				.AsNoTracking()
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.CreatedAt)
				.Take(count)
				.ProjectTo<MovieDto>(_mapper.ConfigurationProvider)
				.ToListAsync();
		}

		public async Task<IEnumerable<MovieDto>> GetPopularAsync(int count = 12)
		{
			return await _context.Movies
				.AsNoTracking()
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ViewCount)
				.Take(count)
				.ProjectTo<MovieDto>(_mapper.ConfigurationProvider)
				.ToListAsync();
		}

		public async Task<IEnumerable<MovieDto>> GetByGenreAsync(Guid genreId)
		{
			return await _context.Movies
				.AsNoTracking()
				.Where(m => m.IsActive &&
							m.Movie_Genres.Any(g => g.GenreId == genreId))
				.OrderByDescending(m => m.ViewCount)
				.ProjectTo<MovieDto>(_mapper.ConfigurationProvider)
				.ToListAsync();
		}

		public async Task<IEnumerable<MovieDto>> GetByCountryAsync(Guid countryId)
		{
			return await _context.Movies
				.AsNoTracking()
				.Where(m => m.IsActive && m.CountryId == countryId)
				.OrderByDescending(m => m.ViewCount)
				.ProjectTo<MovieDto>(_mapper.ConfigurationProvider)
				.ToListAsync();
		}

		#endregion
	}
}