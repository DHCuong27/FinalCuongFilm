namespace FinalCuongFilm.Service.Interfaces
{
	public interface IVideoConversionService
	{
		Task<string> ConvertToHlsAsync(string sourceFileUrl, string slug, int episodeNumber, CancellationToken cancellationToken);

		Task ProcessVideoBackgroundJobAsync(Guid mediaFileId, string mp4Url, string slug, int episodeNumber, CancellationToken cancellationToken);
	}
}