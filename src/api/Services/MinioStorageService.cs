using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.Exceptions;
using Template.Api.Models;
using Template.Api.Data;
using Template.Api.Entities;

namespace Template.Api.Services;

public class MinioStorageService : IStorageService
{
    private readonly Minio.IMinioClient _client;
    private readonly MinioSettings _settings;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<MinioSettings> options, AppDbContext dbContext, ILogger<MinioStorageService> logger)
    {
        _settings = options.Value;
        _dbContext = dbContext;
        _logger = logger;
        _logger.LogInformation("Initializing MinioStorageService with ServerAddress: {ServerAddress}, BucketName: {BucketName}", _settings.ServerAddress, _settings.BucketName);

        // Build Minio client using builder pattern
        _client = new Minio.MinioClient()
            .WithEndpoint(_settings.ServerAddress)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .Build();
    }

    public async Task EnsureBucketExistsAsync()
    {
        try
        {
            _logger.LogDebug("CALLED: EnsureBucketExistsAsync()");
            var beArgs = new Minio.DataModel.Args.BucketExistsArgs()
                .WithBucket(_settings.BucketName);
            var found = await _client.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (!found)
            {
                var mbArgs = new Minio.DataModel.Args.MakeBucketArgs()
                    .WithBucket(_settings.BucketName);
                await _client.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring Minio bucket exists");
            throw;
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        _logger.LogDebug("CALLED: UploadFileAsync(file={File})", file?.FileName);
        if (file is null)
            throw new ArgumentNullException(nameof(file));

        await EnsureBucketExistsAsync();

        var extension = Path.GetExtension(file.FileName) ?? string.Empty;
        var objectName = Guid.NewGuid() + extension;

        using var stream = file.OpenReadStream();
        try
        {
            var putArgs = new Minio.DataModel.Args.PutObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType);

            await _client.PutObjectAsync(putArgs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object {ObjectName} to Minio", objectName);
            throw;
        }

        return objectName;
    }

    public async Task DeleteObjectAsync(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            _logger.LogDebug("CALLED: DeleteObjectAsync(objectName={ObjectName})", objectName);
            throw new ArgumentNullException(nameof(objectName));
        }

        await EnsureBucketExistsAsync();

        try
        {
            var removeArgs = new Minio.DataModel.Args.RemoveObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName);

            await _client.RemoveObjectAsync(removeArgs).ConfigureAwait(false);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            _logger.LogWarning("Attempted to delete non-existent object {ObjectName}", objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object {ObjectName} from Minio", objectName);
            throw;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string objectName, int expirySeconds = 60 * 60)
    {
        _logger.LogDebug("CALLED: GetPresignedUrlAsync(objectName={ObjectName}, expirySeconds={ExpirySeconds})", objectName, expirySeconds);
        try
        {
            var presignedArgs = new Minio.DataModel.Args.PresignedGetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName)
                .WithExpiry(expirySeconds);

            var url = await _client.PresignedGetObjectAsync(presignedArgs).ConfigureAwait(false);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned url for {ObjectName}", objectName);
            throw;
        }
    }
}
