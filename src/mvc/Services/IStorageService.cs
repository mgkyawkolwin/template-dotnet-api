using Microsoft.AspNetCore.Http;

namespace Template.Api.Services;

public interface IStorageService
{
    Task EnsureBucketExistsAsync();
    Task<string> UploadFileAsync(IFormFile file);
    Task DeleteObjectAsync(string objectName);
    Task<string> GetPresignedUrlAsync(string objectName, int expirySeconds = 60 * 60);
}
