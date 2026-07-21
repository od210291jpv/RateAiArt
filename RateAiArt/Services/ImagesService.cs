namespace RateAiArt.Services
{
    public class ImagesService : IImagesService
    {
        public async Task<string> SaveImageToDiskAsync(byte[] imageBytes, string hash, string path, string fileName, HttpContext httpContext)
        {
            throw new NotImplementedException();
        }
    }
}
