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
				.OrderByDescending(m => m.CreatedAt)
				.Select(m => new MovieDto
				{
					Id = m.Id,
					Title = m.Title,
					Slug = m.Slug,
					Description = m.Description,
					ReleaseYear = m.ReleaseYear,
					DurationMinutes = m.DurationMinutes,
					ViewCount = m.ViewCount,
					PosterUrl = m.PosterUrl,
					BackdropUrl = m.BackdropUrl,
					TrailerUrl = m.TrailerUrl,
					Type = m.Type,
					Status = m.Status,
					IsActive = m.IsActive,
					LanguageId = m.LanguageId.HasValue ? m.LanguageId.Value : Guid.Empty,
					CountryId = m.CountryId.HasValue ? m.CountryId.Value : Guid.Empty,
					CountryName = m.Country != null ? m.Country.Name : null,
					LanguageName = m.Language != null ? m.Language.Name : null,
					IsVipOnly = m.IsVipOnly,
					SelectedActorIds = m.MovieActors.Select(ma => ma.ActorId).ToList(),
					SelectedGenreIds = m.MovieGenres.Select(mg => mg.GenreId).ToList()
				})
				.ToListAsync();

			return movies;
		}


		public IQueryable<MovieDto> MapToLightweightDto(IQueryable<Movie> query)
		{
			return query.Select(m => new MovieDto
			{
				Id = m.Id,
				Title = m.Title,
				Slug = m.Slug,
				ReleaseYear = m.ReleaseYear,
				ViewCount = m.ViewCount,
				PosterUrl = m.PosterUrl,
				BackdropUrl = m.BackdropUrl,
				Type = m.Type,
				IsVipOnly = m.IsVipOnly,
				CountryName = m.Country != null ? m.Country.Name : null
			});
		}

		public IQueryable<Movie> GetBaseActiveMoviesQuery()
		{
			// BẮT BUỘC dùng AsNoTracking để bỏ qua overhead theo dõi thay đổi của EF Core
			return _context.Movies.AsNoTracking().Where(m => m.IsActive);
		}


		public async Task<MovieDto?> GetByIdAsync(Guid id)
		{
			var movie = await _context.Movies
				.Include(m => m.MovieActors)
					.ThenInclude(ma => ma.Actor)
				.Include(m => m.MovieGenres)
					.ThenInclude(mg => mg.Genre)
				.Include(m => m.Country)
				.Include(m => m.Language)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (movie == null)
				return null;

			return new MovieDto
			{
				Id = movie.Id,
				Title = movie.Title,
				Slug = movie.Slug,
				Type = movie.Type,
				Description = movie.Description,
				DurationMinutes = movie.DurationMinutes,
				ReleaseYear = movie.ReleaseYear,
				Status = movie.Status,
				IsActive = movie.IsActive,
				IsVipOnly = movie.IsVipOnly,
				PosterUrl = movie.PosterUrl,
				BackdropUrl = movie.BackdropUrl,
				GenreName = movie.MovieGenres.FirstOrDefault()?.Genre?.Name,
				GenreNames = movie.MovieGenres
					.Where(mg => mg.Genre != null)
					.Select(mg => mg.Genre!.Name)
					.ToList(),
				CountryName = movie.Country != null ? movie.Country.Name : null,
				LanguageName = movie.Language != null ? movie.Language.Name : null,
				SelectedActorIds = movie.MovieActors.Select(ma => ma.ActorId).ToList(),
				SelectedGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList(),

				Actors = movie.MovieActors.Select(ma => new MovieActorDto
				{
					ActorId = ma.Actor.Id,
					Name = ma.Actor.Name,
					AvatarUrl = ma.Actor.AvartUrl
				}).ToList(),

				ActorName = movie.MovieActors.Select(ma => ma.Actor.Name).ToList()
			};
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
			string normalizedTitle = dto.Title.Trim().ToLower();

			bool isDuplicate = await _context.Movies.AnyAsync(m =>
				m.Title.ToLower() == normalizedTitle &&
				m.ReleaseYear == dto.ReleaseYear
			);

			if (isDuplicate)
			{
				_logger.LogWarning($"[Validation Failed] Attempted to create a duplicate movie: '{dto.Title}' ({dto.ReleaseYear})");
				throw new Exception($"Cannot create! The movie '{dto.Title}' ({dto.ReleaseYear}) already exists in the system.");
			}

			if (await _context.Movies.AnyAsync(m => m.Slug == dto.Slug))
			{
				throw new Exception($"The slug '{dto.Slug}' is already in use. Please modify the title to generate a unique URL.");
			}

			var movie = _mapper.Map<Movie>(dto);
			movie.IsVipOnly = dto.IsVipOnly;
			movie.CreatedAt = DateTime.UtcNow;
			movie.IsActive = true;
			movie.ViewCount = 0;

			_context.Movies.Add(movie);
			await _context.SaveChangesAsync();

			//  SAVE MANY-TO-MANY RELATIONS 
			if (dto.ActorIds != null && dto.ActorIds.Any())
			{
				var movieActors = dto.ActorIds
					.Distinct()
					.Select(actorId => new MovieActor
					{
						MovieId = movie.Id,
						ActorId = actorId
					})
					.ToList();

				_context.MovieActors.AddRange(movieActors);
			}

			if (dto.GenreIds != null && dto.GenreIds.Any())
			{
				var movieGenres = dto.GenreIds
					.Distinct()
					.Select(genreId => new MovieGenre
					{
						MovieId = movie.Id,
						GenreId = genreId
					})
					.ToList();

				_context.MovieGenres.AddRange(movieGenres);
			}

			await _context.SaveChangesAsync();
			_logger.LogInformation($"Successfully created manual movie: {movie.Title} (ID: {movie.Id})");

			return _mapper.Map<MovieDto>(movie);
		}

		public async Task<MovieDto?> UpdateAsync(Guid id, MovieUpdateDto dto)
		{
			var movie = await _context.Movies
				.Include(m => m.MovieActors)
				.Include(m => m.MovieGenres)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (movie == null) return null;

			if (await _context.Movies
				.AnyAsync(m => m.Slug == dto.Slug && m.Id != id))
				throw new Exception("Slug already exists");

			_mapper.Map(dto, movie);
			movie.IsVipOnly = dto.IsVipOnly;
			movie.UpdatedAt = DateTime.UtcNow;

			//  SYNC ACTORS 
			if (dto.ActorIds != null)
			{
				var existingActorIds = movie.MovieActors.Select(ma => ma.ActorId).ToList();

				var toRemove = movie.MovieActors
					.Where(ma => !dto.ActorIds.Contains(ma.ActorId))
					.ToList();
				_context.MovieActors.RemoveRange(toRemove);

				var toAdd = dto.ActorIds
					.Except(existingActorIds)
					.Select(actorId => new MovieActor
					{
						MovieId = movie.Id,
						ActorId = actorId
					});
				_context.MovieActors.AddRange(toAdd);
			}

			//  SYNC GENRES 
			if (dto.GenreIds != null)
			{
				var existingGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList();

				var toRemove = movie.MovieGenres
					.Where(mg => !dto.GenreIds.Contains(mg.GenreId))
					.ToList();
				_context.MovieGenres.RemoveRange(toRemove);

				var toAdd = dto.GenreIds
					.Except(existingGenreIds)
					.Select(genreId => new MovieGenre
					{
						MovieId = movie.Id,
						GenreId = genreId
					});
				_context.MovieGenres.AddRange(toAdd);
			}

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
							m.MovieGenres.Any(g => g.GenreId == genreId))
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

		public async Task<PagedResult<MovieDto>> GetPagedAsync(string searchTerm, int page, int pageSize)
		{
			var query = _context.Movies
				.Include(m => m.Country)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				string keyword = searchTerm.Trim().ToLower();

				query = query.Where(m =>
					m.Title.ToLower().Contains(keyword) ||
					m.Slug.ToLower().Contains(keyword) ||
					(m.Country != null && m.Country.Name.ToLower().Contains(keyword))
				);
			}

			int totalItems = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			page = page < 1 ? 1 : page;
			page = page > totalPages && totalPages > 0 ? totalPages : page;

			var movies = await query
				.OrderByDescending(m => m.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return new PagedResult<MovieDto>
			{
				Items = _mapper.Map<List<MovieDto>>(movies),
				TotalCount = totalItems,
				PageIndex = page,
				PageSize = pageSize
			};
		}

		#endregion
	}
}
