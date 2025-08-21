using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SAXTech.DocumentConverter
{
    public class DeleteClientFunction
    {
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private const string CONTAINER_NAME = "fcs-clients";
        private const string CONVERTED_CONTAINER = "fcs-convertedclients";

        public DeleteClientFunction(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
        {
            _logger = loggerFactory.CreateLogger<DeleteClientFunction>();
            _blobServiceClient = blobServiceClient;
        }

        [Function("DeleteClient")]
        public async Task<HttpResponseData> DeleteClient(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "clients/{clientName}")] 
            HttpRequestData req, 
            string clientName)
        {
            _logger.LogInformation($"Delete client request received for: {clientName}");

            try
            {
                // Validate client name
                if (string.IsNullOrWhiteSpace(clientName))
                {
                    var badRequest = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badRequest.WriteAsJsonAsync(new
                    {
                        Success = false,
                        Error = "Client name is required"
                    });
                    return badRequest;
                }

                // Track deletion results
                var deletionResults = new
                {
                    ClientName = clientName,
                    DeletedBlobs = new List<string>(),
                    FailedDeletions = new List<string>(),
                    Errors = new List<string>()
                };

                // Delete from original container
                await DeleteClientBlobsFromContainer(
                    CONTAINER_NAME, 
                    $"FCS-OriginalClients/{clientName}/",
                    deletionResults.DeletedBlobs,
                    deletionResults.FailedDeletions,
                    deletionResults.Errors);

                // Delete from converted container (uses FCS-ConvertedClients prefix)
                await DeleteClientBlobsFromContainer(
                    CONVERTED_CONTAINER, 
                    $"FCS-ConvertedClients/{clientName}/",
                    deletionResults.DeletedBlobs,
                    deletionResults.FailedDeletions,
                    deletionResults.Errors);

                // Also delete any metadata files
                await DeleteClientBlobsFromContainer(
                    CONTAINER_NAME,
                    $"FCS-OriginalClients/{clientName}/.metadata/",
                    deletionResults.DeletedBlobs,
                    deletionResults.FailedDeletions,
                    deletionResults.Errors);

                // Create response
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    Success = true,
                    Message = $"Client '{clientName}' deletion completed",
                    Summary = new
                    {
                        TotalDeleted = deletionResults.DeletedBlobs.Count,
                        TotalFailed = deletionResults.FailedDeletions.Count,
                        HasErrors = deletionResults.Errors.Any()
                    },
                    DeletedFiles = deletionResults.DeletedBlobs,
                    FailedFiles = deletionResults.FailedDeletions,
                    Errors = deletionResults.Errors,
                    Timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting client {clientName}");
                
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new
                {
                    Success = false,
                    Error = $"Failed to delete client: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
                
                return errorResponse;
            }
        }

        private async Task DeleteClientBlobsFromContainer(
            string containerName,
            string prefix,
            List<string> deletedBlobs,
            List<string> failedDeletions,
            List<string> errors)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                
                if (!await containerClient.ExistsAsync())
                {
                    _logger.LogWarning($"Container {containerName} does not exist");
                    return;
                }

                // List all blobs with the prefix
                var blobs = containerClient.GetBlobsAsync(prefix: prefix);
                
                await foreach (var blob in blobs)
                {
                    try
                    {
                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        var deleteResult = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                        
                        if (deleteResult.Value)
                        {
                            deletedBlobs.Add($"{containerName}/{blob.Name}");
                            _logger.LogInformation($"Deleted blob: {containerName}/{blob.Name}");
                        }
                        else
                        {
                            failedDeletions.Add($"{containerName}/{blob.Name}");
                            _logger.LogWarning($"Failed to delete blob: {containerName}/{blob.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failedDeletions.Add($"{containerName}/{blob.Name}");
                        errors.Add($"Error deleting {blob.Name}: {ex.Message}");
                        _logger.LogError(ex, $"Error deleting blob {blob.Name}");
                    }
                }

                // Also try to delete the folder itself (in case it's represented as a directory marker)
                try
                {
                    var folderBlobClient = containerClient.GetBlobClient(prefix.TrimEnd('/'));
                    await folderBlobClient.DeleteIfExistsAsync();
                }
                catch
                {
                    // Folder markers might not exist, ignore errors
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing container {containerName}: {ex.Message}");
                _logger.LogError(ex, $"Error processing container {containerName}");
            }
        }

        [Function("ListClientBlobs")]
        public async Task<HttpResponseData> ListClientBlobs(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "clients/{clientName}/blobs")] 
            HttpRequestData req,
            string clientName)
        {
            _logger.LogInformation($"List blobs request for client: {clientName}");

            try
            {
                var allBlobs = new List<object>();

                // List from original container
                var originalContainer = _blobServiceClient.GetBlobContainerClient(CONTAINER_NAME);
                if (await originalContainer.ExistsAsync())
                {
                    var prefix = $"FCS-OriginalClients/{clientName}/";
                    await foreach (var blob in originalContainer.GetBlobsAsync(prefix: prefix))
                    {
                        allBlobs.Add(new
                        {
                            Container = CONTAINER_NAME,
                            Name = blob.Name,
                            Size = blob.Properties?.ContentLength ?? 0,
                            LastModified = blob.Properties?.LastModified,
                            ContentType = blob.Properties?.ContentType
                        });
                    }
                }

                // List from converted container
                var convertedContainer = _blobServiceClient.GetBlobContainerClient(CONVERTED_CONTAINER);
                if (await convertedContainer.ExistsAsync())
                {
                    var prefix = $"FCS-ConvertedClients/{clientName}/";
                    await foreach (var blob in convertedContainer.GetBlobsAsync(prefix: prefix))
                    {
                        allBlobs.Add(new
                        {
                            Container = CONVERTED_CONTAINER,
                            Name = blob.Name,
                            Size = blob.Properties?.ContentLength ?? 0,
                            LastModified = blob.Properties?.LastModified,
                            ContentType = blob.Properties?.ContentType
                        });
                    }
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    Success = true,
                    ClientName = clientName,
                    TotalBlobs = allBlobs.Count,
                    Blobs = allBlobs,
                    Timestamp = DateTime.UtcNow
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing blobs for client {clientName}");
                
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new
                {
                    Success = false,
                    Error = $"Failed to list blobs: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
                
                return errorResponse;
            }
        }
    }
}
