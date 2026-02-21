using AutoMapper;
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

		public async Task<IEnumerable<MovieDto>> GetAllAsync()
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.CreatedAt)
				.ToListAsync();

			return _mapper.Map<IEnumerable<MovieDto>>(movies);
		}

		public async Task<MovieDto?> GetByIdAsync(Guid id)
		{
			var movie = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Include(m => m.Movie_Actors)
					.ThenInclude(ma => ma.Actor)
				.Include(m => m.Movie_Tags)
					.ThenInclude(mt => mt.Tag)
				.FirstOrDefaultAsync(m => m.Id == id);

			return _mapper.Map<MovieDto>(movie);
		}

		public async Task<MovieDto?> GetBySlugAsync(string slug)
		{
			var movie = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.FirstOrDefaultAsync(m => m.Slug == slug && m.IsActive);

			return _mapper.Map<MovieDto>(movie);
		}

		public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
		{
			var movie = _mapper.Map<Movie>(dto);
			movie.CreatedAt = DateTime.UtcNow;
			movie.IsActive = true;

			_context.Movies.Add(movie);
			await _context.SaveChangesAsync();

			_logger.LogInformation($" Created movie: {movie.Title} (ID: {movie.Id})");

			return _mapper.Map<MovieDto>(movie);
		}

		public async Task<MovieDto?> UpdateAsync(Guid id, MovieUpdateDto dto)
		{
			var movie = await _context.Movies.FindAsync(id);

			if (movie == null)
			{
				_logger.LogWarning($"Movie with ID {id} not found");
				return null;
			}

			_mapper.Map(dto, movie);
			movie.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			_logger.LogInformation($" Updated movie: {movie.Title} (ID: {movie.Id})");

			return _mapper.Map<MovieDto>(movie);
		}

		//  DELETE WITH MANUAL MediaFiles DELETION
		public async Task<bool> DeleteAsync(Guid id)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				_logger.LogInformation($"🗑️ Starting delete movie with ID: {id}");

				var movie = await _context.Movies
					.Include(m => m.MediaFiles)
					.Include(m => m.Episodes)
						.ThenInclude(e => e.MediaFiles)
					.Include(m => m.Favorites)
					.Include(m => m.Reviews)
					.Include(m => m.Movie_Actors)
					.Include(m => m.Movie_Genres)
					.Include(m => m.Movie_Tags)
					.FirstOrDefaultAsync(m => m.Id == id);

				if (movie == null)
				{
					_logger.LogWarning($"Movie with ID {id} not found");
					await transaction.RollbackAsync();
					return false;
				}

				_logger.LogInformation($"Found movie: {movie.Title}");

				// 1. Delete Episode MediaFiles
				if (movie.Episodes != null && movie.Episodes.Any())
				{
					_logger.LogInformation($"Processing {movie.Episodes.Count} episodes");

					foreach (var episode in movie.Episodes)
					{
						if (episode.MediaFiles != null && episode.MediaFiles.Any())
						{
							_logger.LogInformation($"   Deleting {episode.MediaFiles.Count} media files for Episode {episode.EpisodeNumber}");

							foreach (var mediaFile in episode.MediaFiles.ToList())
							{
								try
								{
									await _azureBlobService.DeleteAsync(mediaFile.FileUrl);
									_logger.LogInformation($"      Deleted blob: {mediaFile.FileName}");
								}
								catch (Exception ex)
								{
									_logger.LogWarning(ex, $"      Failed to delete blob: {mediaFile.FileUrl}");
								}
							}

							_context.MediaFiles.RemoveRange(episode.MediaFiles);
						}
					}
				}

				// 2. Delete Movie MediaFiles
				if (movie.MediaFiles != null && movie.MediaFiles.Any())
				{
					_logger.LogInformation($"   Deleting {movie.MediaFiles.Count} movie media files");

					foreach (var mediaFile in movie.MediaFiles.ToList())
					{
						try
						{
							await _azureBlobService.DeleteAsync(mediaFile.FileUrl);
							_logger.LogInformation($"      Deleted blob: {mediaFile.FileName}");
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, $"      Failed to delete blob: {mediaFile.FileUrl}");
						}
					}

					_context.MediaFiles.RemoveRange(movie.MediaFiles);
				}

				// 3. Delete Many-to-many relationships
				if (movie.Movie_Actors != null && movie.Movie_Actors.Any())
				{
					_logger.LogInformation($"   Removing {movie.Movie_Actors.Count} actor relationships");
					_context.Movie_Actors.RemoveRange(movie.Movie_Actors);
				}

				if (movie.Movie_Genres != null && movie.Movie_Genres.Any())
				{
					_logger.LogInformation($"   Removing {movie.Movie_Genres.Count} genre relationships");
					_context.Movie_Genres.RemoveRange(movie.Movie_Genres);
				}

				if (movie.Movie_Tags != null && movie.Movie_Tags.Any())
				{
					_logger.LogInformation($"   Removing {movie.Movie_Tags.Count} tag relationships");
					_context.Movie_Tags.RemoveRange(movie.Movie_Tags);
				}

				// 4. Delete Movie (CASCADE will auto-delete Episodes, Favorites, Reviews)
				_context.Movies.Remove(movie);

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				_logger.LogInformation($" Successfully deleted movie: {movie.Title}");
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"❌ Error deleting movie: {id}");
				await transaction.RollbackAsync();
				throw;
			}
		}

		public async Task<IEnumerable<MovieDto>> SearchAsync(string keyword)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Where(m => m.IsActive && (
					m.Title.Contains(keyword) ||
					m.Description.Contains(keyword)
				))
				.OrderByDescending(m => m.ViewCount)
				.ToListAsync();

			return _mapper.Map<IEnumerable<MovieDto>>(movies);
		}

		public async Task<IEnumerable<MovieDto>> GetByGenreAsync(Guid genreId)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
				.Where(m => m.IsActive && m.Movie_Genres.Any(mg => mg.GenreId == genreId))
				.OrderByDescending(m => m.ViewCount)
				.ToListAsync();

			return _mapper.Map<IEnumerable<MovieDto>>(movies);
		}

		public async Task<IEnumerable<MovieDto>> GetByCountryAsync(Guid countryId)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Where(m => m.IsActive && m.CountryId == countryId)
				.OrderByDescending(m => m.ViewCount)
				.ToListAsync();

			return _mapper.Map<IEnumerable<MovieDto>>(movies);
		}

		public async Task IncrementViewCountAsync(Guid id)
		{
			var movie = await _context.Movies.FindAsync(id);

			if (movie != null)
			{
				movie.ViewCount++;
				await _context.SaveChangesAsync();
			}
		}

		public async Task<IEnumerable<MovieDto>> GetPopularMoviesAsync(int count = 10)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ViewCount)
				.Take(count)
				.ToListAsync();

			return _mapper.Map<IEnumerable<MovieDto>>(movies);
		}

		public async Task<IEnumerable<MovieDto>> GetLatestMoviesAsync(int count = 10)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.CreatedAt)
				.Take(count)
				.ToListAsync();

			return _mapper.Map<IEnumerable<MovieDto>>(movies);
		}

		public Task<bool> UpdateAsync(MovieUpdateDto dto)
		{
			throw new NotImplementedException();
		}

		public Task<bool> ExistsAsync(Guid id)
		{
			throw new NotImplementedException();
		}

		Task<bool> IMovieService.IncrementViewCountAsync(Guid id)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MovieDto>> GetLatestAsync(int count = 12)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MovieDto>> GetPopularAsync(int count = 12)
		{
			throw new NotImplementedException();
		}
	}
}