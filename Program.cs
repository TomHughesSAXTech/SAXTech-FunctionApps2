// Program.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using Azure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Add Azure Document Intelligence client
        services.AddSingleton(provider =>
        {
            var endpoint = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT") 
                ?? throw new InvalidOperationException("DOCUMENT_INTELLIGENCE_ENDPOINT is not configured");
            var key = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_KEY") 
                ?? throw new InvalidOperationException("DOCUMENT_INTELLIGENCE_KEY is not configured");
            return new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
        });

        // Add Blob Storage client
        services.AddSingleton(provider =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") 
                ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is not configured");
            return new BlobServiceClient(connectionString);
        });
    })
    .Build();

host.Run();
