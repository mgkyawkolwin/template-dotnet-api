namespace Template.Api.Models;

public sealed record MinioSettings
{
    public required string ObjectAccessUrl { get; init; }
    public required string ServerAddress { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string BucketName { get; init; }
}
