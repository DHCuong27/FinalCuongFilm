namespace FinalCuongFilm.Common.Constants
{
	
	public static class MaxLengths
	{
		
		// Generic
		
		public const int NAME = 100;
		public const int TITLE = 200;
		public const int SLUG = 200;
		public const int CODE = 20;

		// Text content

		public const int SUMMARY = 1000;
		public const int DESCRIPTION = 4000;
		public const int NOTE = 2000;
		public const int CONTENT = 8000;
		public const int COMMENT = 3000;

       // Identity standard
		public const int EMAIL_ADDRESS = 256;
		public const int PHONE_NUMBER = 20;
		public const int PASSWORD_HASH = 500;

		
		// Media 

		public const int FILE_NAME = 255;
		public const int FILE_PATH = 500;
		public const int FILE_FORMAT = 20;
		public const int QUALITY = 20;            // 720p, 1080p, 4K
		public const int SERVER_NAME = 50;        // CDN, VIP, Backup

		
		// URLs
	
		public const int URL = 500;
		public const int IMAGE_URL = 500;
		public const int VIDEO_URL = 500;
		public const int TRAILER_URL = 500;

		
		// Movie domain
		
		public const int MOVIE_TITLE = 255;
		public const int ACTOR_NAME = 150;
		public const int CHARACTER_NAME = 150;
		public const int GENRE_NAME = 100;
		public const int TAG_NAME = 100;
		public const int COUNTRY_NAME = 100;
		public const int LANGUAGE_NAME = 100;

		
		// SEO
		
		public const int META_TITLE = 150;
		public const int META_DESCRIPTION = 300;
		public const int META_KEYWORDS = 300;

		
		// Search
		public const int SEARCH_TERM = 255;

		
		// Logging / System
	
		public const int IP_ADDRESS = 45;   // IPv6 compatible
		public const int USER_AGENT = 300;
	}
}
