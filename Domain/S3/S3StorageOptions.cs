namespace Domain.S3;

public class S3StorageOptions
{
    public required string ServiceUrl { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string BucketName { get; set; }
    public bool ForcePathStyle { get; set; } = true;
}