# Microsoft.Identity.Web.Azure

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Identity.Web.Azure.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Microsoft.Identity.Web.Azure/)

This package enables ASP.NET Core web apps and web APIs to use the Azure SDKs with the Microsoft identity platform (formerly Azure AD v2.0).

## Features

- **MicrosoftIdentityTokenCredential** - Provides seamless integration between Microsoft.Identity.Web and Azure SDK's TokenCredential, enabling your application to use Azure services with Microsoft Entra ID (formerly Azure Active Directory) authentication.
- Supports both user delegated and application permission scenarios
- Works with the standard Azure SDK authentication flow

## Installation

```sh
dotnet add package Microsoft.Identity.Web.Azure
## Usage

### Basic setup

1. Register the Azure Token Credential in your service collection:

```cshap
// In your Startup.cs or Program.cs
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

public void ConfigureServices(IServiceCollection services)
{
    // Register Microsoft Identity Web
    services.AddMicrosoftIdentityWebAppAuthentication(Configuration)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();
        
    // Add the Azure Token Credential
    services.AddMicrosoftIdentityAzureTokenCredential();
    
    // Register Azure services
    services.AddAzureClients(builder => 
    {
        // Use the Microsoft Identity credential for all Azure clients
        builder.UseCredential(sp => sp.GetRequiredService<MicrosoftIdentityTokenCredential>());
        
        // Configure Azure Blob Storage client
        builder.AddBlobServiceClient(new Uri("https://your-storage-account.blob.core.windows.net"));
        // Add other Azure clients as needed
    });
}
```

### Using with Azure SDK clients

Once registered, the BlobServiceClient can be injected directly into your controllers or services:
// Direct injection into a controller or Razor Page

```csharp
[Authorize]
public class BlobController : Controller
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly MicrosoftIdentityTokenCredential _tokenCredential;

    public BlobController(
        BlobServiceClient blobServiceClient,
        MicrosoftIdentityTokenCredential tokenCredential) // Optional: inject if you need to modify token behavior
    {
        _blobServiceClient = blobServiceClient;
        _tokenCredential = tokenCredential;
    }

    [HttpGet]
    public async Task<IActionResult> DownloadBlob(string containerName, string blobName)
    {
        try
        {
            // If you want to have get a blob on behalf of the app itself.
            _tokenCredential.Options.RequestAppToken = true;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            // Check if blob exists
            if (!await blobClient.ExistsAsync())
            {
                return NotFound($"Blob '{blobName}' not found in container '{containerName}'");
            }
            
            // Download the blob content
            var response = await blobClient.DownloadContentAsync();
            string content = response.Value.Content.ToString();
            
            return Content(content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error accessing blob: {ex.Message}");
        }
    }
```
    
For Razor Pages, you can similarly inject the client directly:

```csharp
public class BlobModel : PageModel
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobModel(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<IActionResult> OnGetAsync(string containerName, string blobName)
    {
        // Use the blob service client directly in your page handler
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        // ...rest of the implementation
    }
}
```


### Advanced scenarios

#### Using with specific authentication schemes

If your application has multiple authentication schemes, you can specify which one to use:
// Configure the token credential to use a specific authentication scheme
tokenCredential.Options.AcquireTokenOptions.AuthenticationOptionsName = OpenIdConnectDefaults.AuthenticationScheme;

#### Custom configuration

You can customize the token acquisition behavior:

```cs
// Configure additional options
tokenCredential.Options.AcquireTokenOptions.CorrelationId = Guid.NewGuid();
tokenCredential.Options.AcquireTokenOptions.Tenant = "GUID";

## Working with older versions

This package includes two token credentials classes:

- `MicrosoftIdentityTokenCredential` (recommended)
- `TokenAcquirerTokenCredential` (deprecated)

The `TokenAcquirerTokenCredential` is marked as obsolete and is included for backward compatibility. New applications should use `MicrosoftIdentityTokenCredential` instead.

## Integration with Azure SDKs

This package enables integration with [Azure SDKs for .NET](https://learn.microsoft.com/dotnet/azure/sdk/azure-sdk-for-dotnet), including but not limited to:

- Azure Storage (Blobs, Queues, Tables, Files)
- Azure Key Vault (although you might rather want to use the DefaultCrentialLoader for credentials)
- Azure Service Bus
- Azure Cosmos DB
- Azure Event Hubs
- Azure Monitor

See [Azure SDK for .NET packages](https://learn.microsoft.com/dotnet/azure/sdk/packages#libraries-using-azurecore)
for the list of packages.

## Related packages

- [Microsoft.Identity.Web](https://www.nuget.org/packages/Microsoft.Identity.Web/)
- [Microsoft.Identity.Web.UI](https://www.nuget.org/packages/Microsoft.Identity.Web.UI/)
- [Microsoft.Identity.Web.MicrosoftGraph](https://www.nuget.org/packages/Microsoft.Identity.Web.MicrosoftGraph/)

## Learn more

- [Microsoft Identity Web documentation](https://aka.ms/ms-identity-web)
- [Azure SDK documentation](https://docs.microsoft.com/azure/developer/azure-sdk/)
- [Microsoft identity platform documentation](https://docs.microsoft.com/azure/active-directory/develop/)
