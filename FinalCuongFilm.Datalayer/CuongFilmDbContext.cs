using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.Constants;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.DataLayer
{
	public class CuongFilmDbContext : DbContext
	{
		public CuongFilmDbContext(DbContextOptions<CuongFilmDbContext> options)
			: base(options)
		{
		}

		#region DbSets
		// Core tables 
		public DbSet<Movie> Movies { get; set; }
		public DbSet<Episode> Episodes { get; set; }
		public DbSet<MediaFile> MediaFiles { get; set; }

		// Metadata 
		public DbSet<Actor> Actors { get; set; }
		public DbSet<Genre> Genres { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<Country> Countries { get; set; }
		public DbSet<Language> Languages { get; set; }

		// Many-to-many (PascalCase format)
		public DbSet<MovieActor> MovieActors { get; set; }
		public DbSet<MovieGenre> MovieGenres { get; set; }
		public DbSet<MovieTag> MovieTags { get; set; }

		// User interaction 
		public DbSet<Review> Reviews { get; set; }
		public DbSet<Favorite> Favorites { get; set; }
		public DbSet<WatchHistory> WatchHistories { get; set; }
		public DbSet<SearchSuggestion> SearchSuggestions { get; set; }
		#endregion

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// 1. MOVIE CONFIGURATION
			modelBuilder.Entity<Movie>(entity =>
			{
				entity.ToTable("Movies");
				entity.HasKey(x => x.Id);

				entity.Property(x => x.Title).IsRequired().HasMaxLength(MaxLengths.MOVIE_TITLE);
				entity.Property(x => x.Slug).IsRequired().HasMaxLength(MaxLengths.SLUG);
				entity.Property(x => x.Description).HasMaxLength(MaxLengths.DESCRIPTION);
				entity.Property(x => x.PosterUrl).HasMaxLength(MaxLengths.IMAGE_URL);
				entity.Property(x => x.TrailerUrl).HasMaxLength(MaxLengths.VIDEO_URL);

			
				entity.Property(x => x.TmdbId).IsRequired(false);

				entity.Property(x => x.ViewCount).HasDefaultValue(0);
				entity.Property(x => x.IsActive).HasDefaultValue(true);
				entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

				entity.HasOne(m => m.Country).WithMany().HasForeignKey(m => m.CountryId).OnDelete(DeleteBehavior.Restrict);
				entity.HasOne(m => m.Language).WithMany(l => l.Movies).HasForeignKey(m => m.LanguageId).OnDelete(DeleteBehavior.Restrict);

				entity.HasMany(m => m.Episodes).WithOne(e => e.Movie).HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
				entity.HasMany(m => m.MediaFiles).WithOne(mf => mf.Movie).HasForeignKey(mf => mf.MovieId).OnDelete(DeleteBehavior.ClientSetNull);
				entity.HasMany(m => m.Favorites).WithOne(f => f.Movie).HasForeignKey(f => f.MovieId).OnDelete(DeleteBehavior.Cascade);
				entity.HasMany(m => m.Reviews).WithOne(r => r.Movie).HasForeignKey(r => r.MovieId).OnDelete(DeleteBehavior.Cascade);

				entity.HasIndex(x => x.Slug).IsUnique();
				entity.HasIndex(x => x.IsActive);
				entity.HasIndex(x => x.CreatedAt);
				entity.HasIndex(x => x.ViewCount);

				
				entity.HasIndex(x => x.TmdbId).IsUnique().HasFilter("[TmdbId] IS NOT NULL");
			});

			// 2. EPISODE CONFIGURATION
			modelBuilder.Entity<Episode>(entity =>
			{
				entity.ToTable("Episodes");
				entity.HasKey(e => e.Id);

				entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
				entity.Property(e => e.Description).HasMaxLength(2000);
				entity.Property(e => e.EpisodeNumber).IsRequired();
				entity.Property(e => e.ViewCount).HasDefaultValue(0);
				entity.Property(e => e.IsActive).HasDefaultValue(true);
				entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

				entity.HasMany(e => e.MediaFiles).WithOne(mf => mf.Episode).HasForeignKey(mf => mf.EpisodeId).OnDelete(DeleteBehavior.ClientSetNull);

				entity.HasIndex(e => e.MovieId);
				entity.HasIndex(e => new { e.MovieId, e.EpisodeNumber }).IsUnique();
				entity.HasIndex(e => e.IsActive);
			});

			// 3. MEDIA FILE CONFIGURATION
			modelBuilder.Entity<MediaFile>(entity =>
			{
				entity.ToTable("MediaFiles");
				entity.HasKey(m => m.Id);

				entity.Property(m => m.FileName).IsRequired().HasMaxLength(MaxLengths.FILE_NAME);
				entity.Property(m => m.FileUrl).IsRequired().HasMaxLength(500);
				entity.Property(m => m.FilePath).HasMaxLength(500);
				entity.Property(m => m.FileType).IsRequired().HasMaxLength(20);
				entity.Property(m => m.Quality).HasMaxLength(20);
				entity.Property(m => m.Language).HasMaxLength(10);
				entity.Property(m => m.FileSizeBytes).HasColumnName("FileSizeInBytes").IsRequired(false);
				entity.Property(m => m.UploadedAt).HasColumnName("CreatedAt").HasDefaultValueSql("GETUTCDATE()").IsRequired();

				entity.HasIndex(m => m.MovieId);
				entity.HasIndex(m => m.EpisodeId);
				entity.HasIndex(m => m.FileType);
			});

			// 4. ACTOR CONFIGURATION
			modelBuilder.Entity<Actor>(entity =>
			{
				entity.ToTable("Actors");
				entity.HasKey(x => x.Id);

				entity.Property(x => x.Name).IsRequired().HasMaxLength(MaxLengths.ACTOR_NAME);
				entity.Property(x => x.Slug).IsRequired().HasMaxLength(MaxLengths.SLUG);
				entity.Property(x => x.AvartUrl).HasMaxLength(MaxLengths.IMAGE_URL);
				entity.Property(x => x.DateOfBirth).IsRequired(false);
				entity.Property(x => x.Gender).HasMaxLength(10);
				entity.Property(x => x.TmdbId).IsRequired(false);

				entity.HasIndex(x => x.Slug).IsUnique();
				entity.HasIndex(x => x.TmdbId).IsUnique().HasFilter("[TmdbId] IS NOT NULL");
			});

			// 5. MANY-TO-MANY RELATIONSHIPS (PascalCase format)
			modelBuilder.Entity<MovieActor>(entity =>
			{
				entity.ToTable("MovieActors");

				entity.HasKey(x => new { x.MovieId, x.ActorId });

				entity.HasOne(x => x.Movie)
					.WithMany(m => m.MovieActors)
					.HasForeignKey(x => x.MovieId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(x => x.Actor)
					.WithMany(a => a.MovieActors)
					.HasForeignKey(x => x.ActorId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasIndex(x => x.MovieId);
				entity.HasIndex(x => x.ActorId);
			});

			modelBuilder.Entity<MovieGenre>(entity =>
			{
				entity.ToTable("MovieGenres");

				entity.HasKey(x => new { x.MovieId, x.GenreId });

				entity.HasOne(x => x.Movie)
					.WithMany(m => m.MovieGenres)
					.HasForeignKey(x => x.MovieId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(x => x.Genre)
					.WithMany(g => g.MovieGenres)
					.HasForeignKey(x => x.GenreId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasIndex(x => x.MovieId);
				entity.HasIndex(x => x.GenreId);
			});

			modelBuilder.Entity<MovieTag>(entity =>
			{
				entity.ToTable("MovieTags");

				entity.HasKey(x => new { x.MovieId, x.TagId });

				entity.HasOne(x => x.Movie)
					.WithMany(m => m.MovieTags)
					.HasForeignKey(x => x.MovieId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(x => x.Tag)
					.WithMany(t => t.MovieTags)
					.HasForeignKey(x => x.TagId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasIndex(x => x.MovieId);
				entity.HasIndex(x => x.TagId);
			});

			// 6. USER INTERACTIONS & METADATA
			ConfigureInteractionsAndMetadata(modelBuilder);
		}

		private void ConfigureInteractionsAndMetadata(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Favorite>(entity =>
			{
				entity.ToTable("Favorites");
				entity.HasKey(f => f.Id);
				entity.Property(f => f.UserId).IsRequired().HasMaxLength(450);
				entity.Property(f => f.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
				entity.HasIndex(f => new { f.UserId, f.MovieId }).IsUnique();
				entity.HasIndex(f => f.UserId);
				entity.HasIndex(f => f.MovieId);
			});

			modelBuilder.Entity<Review>(entity =>
			{
				entity.ToTable("Reviews");
				entity.HasKey(r => r.Id);
				entity.Property(r => r.UserId).IsRequired().HasMaxLength(450);
				entity.Property(r => r.Rating).IsRequired();
				entity.Property(r => r.Comment).HasMaxLength(1000);
				entity.Property(r => r.IsApproved).HasDefaultValue(false);
				entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
				entity.HasIndex(r => new { r.UserId, r.MovieId });
				entity.HasIndex(r => r.Rating);
				entity.HasIndex(r => r.IsApproved);
				entity.HasIndex(r => r.CreatedAt);
			});

			modelBuilder.Entity<WatchHistory>(entity =>
			{
				entity.ToTable("WatchHistories");
				entity.HasKey(x => x.Id);
				entity.Property(x => x.LastWatchedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
				entity.HasIndex(x => x.MovieId);
				entity.HasIndex(x => x.LastWatchedAt);
			});

			modelBuilder.Entity<SearchSuggestion>(entity =>
			{
				entity.ToTable("SearchSuggestions");
				entity.HasKey(x => x.Id);
				entity.Property(x => x.SuggestionText).IsRequired().HasMaxLength(MaxLengths.SEARCH_TERM);
				entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
				entity.HasIndex(x => x.SuggestionText);
				entity.HasIndex(x => x.CreatedAt);
			});

			modelBuilder.Entity<Country>(entity =>
			{
				entity.ToTable("Countries");
				entity.HasKey(x => x.Id);
				entity.Property(x => x.Name).IsRequired().HasMaxLength(MaxLengths.COUNTRY_NAME);
				entity.Property(x => x.IsoCode).HasMaxLength(MaxLengths.CODE);
				entity.Property(x => x.Slug).IsRequired().HasMaxLength(MaxLengths.SLUG);
				entity.HasIndex(x => x.Slug).IsUnique();
				entity.HasIndex(x => x.IsoCode);
			});

			modelBuilder.Entity<Genre>(entity =>
			{
				entity.ToTable("Genres");
				entity.HasKey(g => g.Id);
				entity.Property(g => g.Name).IsRequired().HasMaxLength(MaxLengths.NAME);
				entity.Property(g => g.Slug).IsRequired().HasMaxLength(MaxLengths.SLUG);
				entity.Property(g => g.Description).HasMaxLength(MaxLengths.DESCRIPTION);
				entity.HasIndex(g => g.Slug).IsUnique();
			});

			modelBuilder.Entity<Language>(entity =>
			{
				entity.ToTable("Languages");
				entity.HasKey(l => l.Id);
				entity.Property(l => l.Name).IsRequired().HasMaxLength(MaxLengths.NAME);
				entity.Property(l => l.Slug).IsRequired().HasMaxLength(MaxLengths.NAME);
				entity.HasIndex(l => l.Slug).IsUnique();
			});

			modelBuilder.Entity<Tag>(entity =>
			{
				entity.ToTable("Tags");
				entity.HasKey(t => t.Id);
				entity.Property(t => t.Name).IsRequired().HasMaxLength(MaxLengths.NAME);
				entity.Property(t => t.Slug).IsRequired().HasMaxLength(MaxLengths.SLUG);
				entity.HasIndex(t => t.Slug).IsUnique();
			});
		}
	}
}