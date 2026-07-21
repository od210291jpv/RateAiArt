namespace RateAiArt.Services
{
    public interface IImagesService
    {
        Task<string> SaveImageToDiskAsync(byte[] imageBytes, string hash, string path, string fileName, HttpContext httpContext);
    }
}