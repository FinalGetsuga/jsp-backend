using Amazon.S3;
using Amazon.S3.Model;
using Domain.DTO;
using Domain.S3;
using Microsoft.Extensions.Options;
using Service.Interface;

namespace Service.Implementation;

public class FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public FileStorageService(IAmazonS3 s3Client, IOptions<S3StorageOptions> options)
    {
        _s3Client = s3Client;
        _bucketName = options.Value.BucketName;
    }
    
    public async Task<string> UploadAsync(Stream fileStream, string key, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            AutoCloseStream = false
        };
        
        await _s3Client.PutObjectAsync(request, ct);
        return key;
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET,
            Protocol = Protocol.HTTP
        };
        
        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, key, ct);
    }

    public async Task<List<StorageObjectInfo>> ListAllObjectsAsync(string prefix, CancellationToken ct = default)
    {
        var results = new List<StorageObjectInfo>();
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix,
                ContinuationToken = continuationToken
            };

            var response = await _s3Client.ListObjectsV2Async(request, ct);

            if (response.S3Objects != null)
            {
                results.AddRange(response.S3Objects.Select(o => new StorageObjectInfo
                {
                    Key = o.Key,
                    LastModified = o.LastModified
                }));
            }

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        } while (continuationToken != null);

        return results;
    }
}