# SAXTech Document Converter Function App

An Azure Function App that converts various document types (PDF, Word, Excel, Images) into structured text using Azure Document Intelligence and other processing libraries.

## Features

- **PDF Processing**: Azure Document Intelligence OCR + Layout Analysis
- **Word Documents**: OpenXML SDK processing for .docx and .doc files
- **Excel Spreadsheets**: ClosedXML processing for .xlsx and .xls files
- **Image Processing**: Azure Document Intelligence OCR for PNG, JPEG, TIFF
- **Construction-Specific Analysis**: Extracts dimensions, costs, quantities, and other construction data
- **Structured Output**: Organized text with headers, metadata, and analysis sections

## API Usage

### Endpoint
```
POST https://saxtech-functionapps2.azurewebsites.net/api/ConvertDocument
```

### Request Body
```json
{
  "blobUrl": "https://your-blob-storage-url/document.pdf",
  "fileName": "construction-plan.pdf",
  "mimeType": "application/pdf",
  "client": "ABC Construction",
  "category": "Blueprints"
}
```

### Response
```json
{
  "success": true,
  "fileName": "construction-plan.pdf",
  "client": "ABC Construction",
  "category": "Blueprints",
  "convertedContent": "=== DOCUMENT ANALYSIS ===\n...",
  "conversionMethod": "Azure Document Intelligence OCR + Layout Analysis",
  "convertedAt": "2025-08-08T22:00:00Z",
  "convertedSize": 12450
}
```

## Supported File Types

| File Type | MIME Type | Processing Method |
|-----------|-----------|-------------------|
| PDF | `application/pdf` | Azure Document Intelligence |
| Word (.docx) | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | OpenXML SDK |
| Word (.doc) | `application/msword` | OpenXML SDK |
| Excel (.xlsx) | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | ClosedXML |
| Excel (.xls) | `application/vnd.ms-excel` | ClosedXML |
| PNG | `image/png` | Azure Document Intelligence OCR |
| JPEG | `image/jpeg` | Azure Document Intelligence OCR |
| TIFF | `image/tiff` | Azure Document Intelligence OCR |

## Deployment

This Function App uses GitHub Actions for continuous deployment. Every push to the main/master branch triggers an automatic build and deployment to Azure.

### Required Secrets

The following secret must be configured in your GitHub repository:

- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`: The publish profile from your Azure Function App

### Environment Variables

The Function App requires these environment variables to be set in Azure:

- `DOCUMENT_INTELLIGENCE_ENDPOINT`: Your Azure Document Intelligence endpoint
- `DOCUMENT_INTELLIGENCE_KEY`: Your Azure Document Intelligence API key
- `AZURE_STORAGE_CONNECTION_STRING`: Connection string for Azure Blob Storage

## Architecture

- **Runtime**: .NET 8.0 Isolated
- **Framework**: Azure Functions v4
- **Dependencies**:
  - Azure.AI.FormRecognizer (Document Intelligence)
  - Azure.Storage.Blobs (Blob Storage)
  - DocumentFormat.OpenXml (Word processing)
  - ClosedXML (Excel processing)

## Project Structure

```
SAXTech-FunctionApps2/
├── .github/workflows/
│   └── deploy-function-app.yml    # GitHub Actions deployment
├── ConvertDocument.cs             # Main function implementation
├── Program.cs                     # Application entry point
├── SAXTech-FunctionApps2.csproj   # Project file
├── host.json                      # Function host configuration
└── local.settings.json            # Local development settings
```
