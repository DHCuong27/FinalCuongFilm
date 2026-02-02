using FinalCuongFilm.Datalayer;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace FinalCuongFilm.API.Extensions
{
	public static class ServiceExtensions
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
		{
			// DbContext
			services.AddDbContext<CuongFilmDbContext>(options =>
				options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

			// Services
			services.AddScoped<IMovieService, MovieService>();
			services.AddScoped<IActorService, ActorService>();
			services.AddScoped<IGenreService, GenreService>();
			services.AddScoped<ICountryService, CountryService>();
			services.AddScoped<ILanguageService, LanguageService>();
			services.AddScoped<IEpisodeService, EpisodeService>();
			services.AddScoped<IMediaFileService, MediaFileService>();

			return services;
		}

		public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
		{
			services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo
				{
					Title = "CuongFilm API",
					Version = "v1",
					Description = "RESTful API for CuongFilm Movie Management System",
					Contact = new OpenApiContact
					{
						Name = "CuongFilm Team",
						Email = "contact@cuongfilm.com",
						Url = new Uri("https://cuongfilm.com")
					},
					License = new OpenApiLicense
					{
						Name = "MIT License",
						Url = new Uri("https://opensource.org/licenses/MIT")
					}
				});

				// JWT Authentication
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = @"JWT Authorization header using the Bearer scheme. 
                                  Enter 'Bearer' [space] and then your token in the text input below.
                                  Example: 'Bearer 12345abcdef'",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.ApiKey,
					Scheme = "Bearer"
				});

				options.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "Bearer"
							}
						},
						Array.Empty<string>()
					}
				});

				// XML Comments
				var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
				if (File.Exists(xmlPath))
				{
					options.IncludeXmlComments(xmlPath);
				}
			});

			return services;
		}

		public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
		{
			var jwtSettings = configuration.GetSection("Jwt");
			var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtSettings["Issuer"],
					ValidAudience = jwtSettings["Audience"],
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ClockSkew = TimeSpan.Zero
				};
			});

			return services;
		}

		public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
				{
					policy.AllowAnyOrigin()
						  .AllowAnyMethod()
						  .AllowAnyHeader();
				});

				options.AddPolicy("Production", policy =>
				{
					policy.WithOrigins("https://cuongfilm.com", "https://www.cuongfilm.com")
						  .AllowAnyMethod()
						  .AllowAnyHeader()
						  .AllowCredentials();
				});
			});

			return services;
		}
	}
}