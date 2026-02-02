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

		// ===== Core tables =====
		public DbSet<Movie> Movies { get; set; }
		public DbSet<Episode> Episodes { get; set; }
		public DbSet<MediaFile> MediaFiles { get; set; }

		// ===== Metadata =====
		public DbSet<Actor> Actors { get; set; }
		public DbSet<Genre> Genres { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<Country> Countries { get; set; }
		public DbSet<Language> Languages { get; set; }

		// ===== Many-to-many =====
		public DbSet<Movie_Actor> Movie_Actors { get; set; }
		public DbSet<Movie_Genre> Movie_Genres { get; set; }
		public DbSet<Movie_Tag> Movie_Tags { get; set; }

		// ===== User interaction =====
		public DbSet<Review> Reviews { get; set; }
		public DbSet<Favorite> Favorites { get; set; }
		public DbSet<WatchHistory> WatchHistories { get; set; }
		public DbSet<SearchSuggestion> SearchSuggestions { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Movie
			modelBuilder.Entity<Movie>(entity =>
			{
				entity.ToTable("Movies");
				entity.HasKey(x => x.Id);

				entity.Property(x => x.Title)
					.IsRequired()
					.HasMaxLength(MaxLengths.MOVIE_TITLE);

				entity.Property(x => x.Slug)
					.IsRequired()
					.HasMaxLength(MaxLengths.SLUG);

				entity.Property(x => x.Description)
					.HasMaxLength(MaxLengths.DESCRIPTION);

				entity.Property(x => x.PosterUrl)
					.HasMaxLength(MaxLengths.IMAGE_URL);

				entity.Property(x => x.TrailerUrl)
					.HasMaxLength(MaxLengths.VIDEO_URL);

				entity.Property(x => x.ViewCount)
					.HasDefaultValue(0);

				entity.Property(x => x.IsActive)
					.HasDefaultValue(true);

				entity.Property(x => x.CreatedAt)
					.HasDefaultValueSql("GETUTCDATE()");

				entity.HasOne(m => m.Country)
					.WithMany()
					.HasForeignKey(m => m.CountryId)
					.OnDelete(DeleteBehavior.Restrict);

				entity.HasOne(m => m.Language)
					  .WithMany(l => l.Movies)
					  .HasForeignKey(m => m.LanguageId)
					  .OnDelete(DeleteBehavior.Restrict);
			});

			// Actor
			modelBuilder.Entity<Actor>(entity =>
			{
				entity.ToTable("Actors");
				entity.HasKey(x => x.Id);

				entity.Property(x => x.Name)
					.IsRequired()
					.HasMaxLength(MaxLengths.ACTOR_NAME);

				entity.Property(x => x.Slug)
					.IsRequired()
					.HasMaxLength(MaxLengths.SLUG);

				entity.Property(x => x.AvartUrl)
					.HasMaxLength(MaxLengths.IMAGE_URL);

				entity.Property(x => x.DateOfBirth)
					.IsRequired(false);

				entity.Property(x => x.Gender)
					.HasMaxLength(10);
			});

			// Country
			modelBuilder.Entity<Country>(entity =>
			{
				entity.ToTable("Countries");
				entity.HasKey(x => x.Id);

				entity.Property(x => x.Name)
					.IsRequired()
					.HasMaxLength(MaxLengths.COUNTRY_NAME);

				entity.Property(x => x.IsoCode)
					.HasMaxLength(MaxLengths.CODE);

				entity.Property(x => x.Slug)
					.IsRequired()
					.HasMaxLength(MaxLengths.SLUG);
			});

			// ===== Episode Configuration =====
			modelBuilder.Entity<Episode>(entity =>
			{
				entity.ToTable("Episodes");
				entity.HasKey(e => e.Id);

				entity.Property(e => e.Title)
					.IsRequired()
					.HasMaxLength(255);

				entity.Property(e => e.Description)
					.HasMaxLength(2000);

				entity.Property(e => e.ViewCount)
					.HasDefaultValue(0);

				entity.Property(e => e.IsActive)
					.HasDefaultValue(true);

				entity.Property(e => e.CreatedAt)
					.HasDefaultValueSql("GETUTCDATE()");

				// Relationship với Movie
				entity.HasOne(e => e.Movie)
					.WithMany(m => m.Episodes)
					.HasForeignKey(e => e.MovieId)
					.OnDelete(DeleteBehavior.Cascade); 

				// Index cho performance
				entity.HasIndex(e => e.MovieId);
				entity.HasIndex(e => new { e.MovieId, e.EpisodeNumber })
					.IsUnique(); // Đảm bảo không trùng số tập trong 1 phim
			});

			// Favorite
			modelBuilder.Entity<Favorite>()
		  .HasIndex(f => new { f.UserId, f.MovieId })
		  .IsUnique(); // Một user chỉ favorite 1 movie 1 lần

			modelBuilder.Entity<Favorite>()
				.HasOne(f => f.User)
				.WithMany()
				.HasForeignKey(f => f.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Favorite>()
				.HasOne(f => f.Movie)
				.WithMany(m => m.Favorites)
				.HasForeignKey(f => f.MovieId)
				.OnDelete(DeleteBehavior.Cascade);


			// Genre
			modelBuilder.Entity<Genre>(entity =>
			{
				entity.ToTable("Genres");
				entity.HasKey(g => g.Id);

				entity.Property(g => g.Name)
					.IsRequired()
					.HasMaxLength(MaxLengths.NAME);

				entity.Property(g => g.Slug)
					.IsRequired()
					.HasMaxLength(MaxLengths.SLUG);

				entity.Property(g => g.Description)
					.HasMaxLength(MaxLengths.DESCRIPTION);

				// Slug là duy nhất
				entity.HasIndex(g => g.Slug)
					.IsUnique();
			});

			// Language
			modelBuilder.Entity<Language>(entity =>
			{
				entity.ToTable("Languages");
				entity.HasKey(l => l.Id);

				entity.Property(l => l.Name)
					.IsRequired()
					.HasMaxLength(MaxLengths.NAME);

				entity.Property(l => l.Slug)
					.IsRequired()
					.HasMaxLength(MaxLengths.NAME);

				entity.HasIndex(l => l.Slug)
					.IsUnique();
			});

			// MediaFile
			modelBuilder.Entity<MediaFile>(entity =>
			{
				entity.ToTable("MediaFiles");

				// Primary Key
				entity.HasKey(m => m.Id);

				entity.Property(m => m.Id)
					  .HasColumnName("Id")
					  .IsRequired();

				// File info
				entity.Property(m => m.FileName)
					  .IsRequired()
					  .HasMaxLength(MaxLengths.FILE_NAME);

				entity.Property(m => m.FileUrl)
					  .IsRequired()
					  .HasMaxLength(500);

				entity.Property(m => m.FilePath)
					  .HasMaxLength(500);

				entity.Property(m => m.FileType)
					  .IsRequired()
					  .HasMaxLength(20); // video, subtitle

				entity.Property(m => m.Quality)
					  .HasMaxLength(20); // 1080p, 720p...

				entity.Property(m => m.Language)
					  .HasMaxLength(10); // vi, en...

				entity.Property(m => m.FileSizeBytes)
					  .HasColumnName("FileSizeInBytes")
					  .IsRequired(false);

				entity.Property(m => m.UploadedAt)
					  .HasColumnName("CreatedAt")
					  .HasDefaultValueSql("GETUTCDATE()")
					  .IsRequired();

				
				// Relationships
			

				entity.HasOne(m => m.Movie)
					.WithMany(m => m.MediaFiles)
					.HasForeignKey(m => m.MovieId)
					.OnDelete(DeleteBehavior.Restrict); // hoặc NoAction


				entity.HasOne(m => m.Episode)
						 .WithMany(e => e.MediaFiles)
						.HasForeignKey(m => m.EpisodeId)
						.OnDelete(DeleteBehavior.Cascade);

			});


			// Review
			modelBuilder.Entity<Review>(entity =>
			{
				modelBuilder.Entity<Review>()
		   .HasIndex(r => new { r.UserId, r.MovieId }); // Có thể review nhiều lần

				modelBuilder.Entity<Review>()
					.HasOne(r => r.User)
					.WithMany()
					.HasForeignKey(r => r.UserId)
					.OnDelete(DeleteBehavior.Cascade);

				modelBuilder.Entity<Review>()
					.HasOne(r => r.Movie)
					.WithMany(m => m.Reviews)
					.HasForeignKey(r => r.MovieId)
					.OnDelete(DeleteBehavior.Cascade);

				// Indexes for performance
				modelBuilder.Entity<Review>()
					.HasIndex(r => r.Rating);

				modelBuilder.Entity<Review>()
					.HasIndex(r => r.IsApproved);
			});

			// SearchSuggestion
			modelBuilder.Entity<SearchSuggestion>(entity =>
			{
				entity.ToTable("SearchSuggestions");
				entity.HasKey(x => x.Id);

				entity.Property(x => x.SuggestionText)
					.IsRequired()
					.HasMaxLength(MaxLengths.SEARCH_TERM);

				entity.Property(x => x.CreatedAt)
					.HasDefaultValueSql("GETUTCDATE()");

				entity.HasIndex(x => x.SuggestionText);
			});

			// Tag
			modelBuilder.Entity<Tag>(entity =>
			{
				entity.ToTable("Tags");
				entity.HasKey(t => t.Id);

				entity.Property(t => t.Name)
					.IsRequired()
					.HasMaxLength(MaxLengths.NAME);

				entity.Property(t => t.Slug)
					.IsRequired()
					.HasMaxLength(MaxLengths.SLUG);

				entity.HasIndex(t => t.Slug)
					.IsUnique();
			});

			// WatchHistory
			modelBuilder.Entity<WatchHistory>(entity =>
			{
				entity.ToTable("WatchHistories");
				entity.HasKey(x => x.Id);

				entity.Property(x => x.WatchedAt)
					.IsRequired();

			});

			// ===== Many-to-many relationships =====

			// Movie_Actor
			modelBuilder.Entity<Movie_Actor>()
				.HasKey(x => new { x.MovieId, x.ActorId });

			modelBuilder.Entity<Movie_Actor>()
				.HasOne(x => x.Movie)
				.WithMany(m => m.Movie_Actors)
				.HasForeignKey(x => x.MovieId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Movie_Actor>()
				.HasOne(x => x.Actor)
				.WithMany(a => a.Movie_Actors)
				.HasForeignKey(x => x.ActorId)
				.OnDelete(DeleteBehavior.Restrict);

			// Movie_Genre
			modelBuilder.Entity<Movie_Genre>()
				.HasKey(x => new { x.MovieId, x.GenreId });

			modelBuilder.Entity<Movie_Genre>()
				.HasOne(x => x.Movie)
				.WithMany(m => m.Movie_Genres)
				.HasForeignKey(x => x.MovieId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Movie_Genre>()
				.HasOne(x => x.Genre)
				.WithMany(g => g.Movie_Genres)
				.HasForeignKey(x => x.GenreId)
				.OnDelete(DeleteBehavior.Restrict);

			// Movie_Tag
			modelBuilder.Entity<Movie_Tag>()
				.HasKey(x => new { x.MovieId, x.TagId });

			modelBuilder.Entity<Movie_Tag>()
				.HasOne(x => x.Movie)
				.WithMany(m => m.Movie_Tags)
				.HasForeignKey(x => x.MovieId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Movie_Tag>()
				.HasOne(x => x.Tag)
				.WithMany(t => t.Movie_Tags)
				.HasForeignKey(x => x.TagId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}