using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

public class DigitalOceanSpaceUploaderService
{
    private readonly AmazonS3Client _s3Client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DigitalOceanSpaceUploaderService> _logger;

    private const string S3_BUCKET_NAME = "fatiha";
    private const string S3_HOST_ENDPOINT = "https://fatiha.sfo3.digitaloceanspaces.com";

    public DigitalOceanSpaceUploaderService(IConfiguration configuration, ILogger<DigitalOceanSpaceUploaderService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var accessKey = "DO00W7TTXZHR8JPJPL73";
        var secretKey = "NjbeMSP6gEBrbNbT7uoiz6AM1hsBdLmkoWMM2StId6w";


        var credentials = new BasicAWSCredentials(accessKey, secretKey);

        _s3Client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = S3_HOST_ENDPOINT,
            ForcePathStyle = true,   // Important! This should be true for DigitalOcean.
            SignatureVersion = "v4" // DigitalOcean supports Signature V4.
        });
    }

    public string ExtractFileNameFromUrl(string url)
    {
        Uri uri = new Uri(url);
        return Path.GetFileName(uri.AbsolutePath);
    }


    public async Task DeleteFileAsync(string fileUrl)
    {

        var fileName = ExtractFileNameFromUrl(fileUrl);

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = "fatiha",
            Key = fileName
        };

        try
        {
            var response = await _s3Client.DeleteObjectAsync(deleteRequest);

            _logger.LogInformation($"Response HTTP Status Code: {response.HttpStatusCode}");

            if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogError($"Failed to delete from DigitalOcean Spaces with response: {response.HttpStatusCode}");
                throw new Exception($"Failed to delete from DigitalOcean Spaces with response: {response.HttpStatusCode}");
            }
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError($"Amazon S3 Exception: {e.Message}");
            _logger.LogError($"HTTP Status Code: {e.StatusCode}");
            _logger.LogError($"S3 Error Type: {e.ErrorCode}");
            _logger.LogError($"Response Content: {e.ResponseBody}");
            _logger.LogError($"Request ID: {e.RequestId}");
            throw;
        }


        catch (Exception e)
        {
            _logger.LogError($"Unknown encountered on server. Message:'{e.Message}'");
            throw;
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        _logger.LogInformation($"Uploading {fileName} to {S3_BUCKET_NAME}");

        var putRequest = new PutObjectRequest
        {
            BucketName = S3_BUCKET_NAME,
            ContentType = contentType,
            Key = fileName,
            CannedACL = S3CannedACL.PublicRead  // Set the canned ACL for public read access

        };

        try
        {
            putRequest.InputStream = fileStream;

            var response = await _s3Client.PutObjectAsync(putRequest);

            _logger.LogInformation($"Response HTTP Status Code: {response.HttpStatusCode}");

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError($"Failed to upload to DigitalOcean Spaces with response: {response.HttpStatusCode}");
                throw new Exception($"Failed to upload to DigitalOcean Spaces with response: {response.HttpStatusCode}");
            }
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError($"Amazon S3 Exception: {e.Message}");
            _logger.LogError($"HTTP Status Code: {e.StatusCode}");
            _logger.LogError($"S3 Error Type: {e.ErrorCode}");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError($"Unknown encountered on server. Message:'{e.Message}'");
            throw;
        }

        return $"https://{S3_BUCKET_NAME}.sfo3.digitaloceanspaces.com/{fileName}";
    }




}

