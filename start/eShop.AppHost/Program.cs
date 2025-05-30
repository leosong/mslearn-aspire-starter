using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// https://learn.microsoft.com/en-us/training/modules/azure-storage-dotnet-aspire-app/5-exercise-add-items-azure-storage

var existingStorageName = builder.AddParameter("ergonicedgeiot");
var existingStorageResourceGroup = builder.AddParameter("prd-ergonicedge-iot-weu-rg");
var storage = builder.AddAzureStorage("storage")
    .PublishAsExisting(existingStorageName, existingStorageResourceGroup);

if (!builder.ExecutionContext.IsPublishMode)
{
    storage.RunAsEmulator();
}

var queues = storage.AddQueues("queueConnection");

// Databases

var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var catalogDb = postgres.AddDatabase("CatalogDB");

// DB Manager Apps

builder.AddProject<Catalog_Data_Manager>("catalog-db-mgr")
    .WithReference(catalogDb);


// API Apps

var catalogApi = builder.AddProject<Catalog_API>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(queues);

//var httpEndpoint = catalogApi.Resource.Annotations
//    .OfType<EndpointAnnotation>()
//    .First(e => e.UriScheme == "http");
//httpEndpoint.Name = "http";

/*
// Get all endpoint annotations
var endpointAnnotations = catalogApi.Resource.Annotations
    .OfType<EndpointAnnotation>()
    .ToList();

// Find the HTTP endpoint (should be the first one with UriScheme "http")
var httpEndpoint = endpointAnnotations.FirstOrDefault(e => e.UriScheme == "http");

if (httpEndpoint != null)
{
    // Rename the existing endpoint to "http"
    httpEndpoint.Name = "http";
}
else
{
    // If no HTTP endpoint exists, add one explicitly
    catalogApi.WithHttpEndpoint(name: "http");
}
*/

// Apps

builder.AddProject<WebApp>("webapp")
    .WithReference(catalogApi);

catalogApi.Resource.Annotations.OfType<EndpointAnnotation>()
    .First(e => e.UriScheme == "http").Name = "http";

// 2. Create endpoint reference
var catalogApiEndpoint = catalogApi.GetEndpoint("http");

// 3. Configure environment variables USING REFERENCE
catalogApi.WithEnvironment("CatalogOptions__PicBaseAddress", catalogApiEndpoint);

// Inject assigned URLs for Catalog API
//catalogApi.WithEnvironment("CatalogOptions__PicBaseAddress", () => catalogApi.GetEndpoint("http").Url);

builder.AddProject<Projects.eShop_MessageProcessor>("eshop-messageprocessor")
    .WithReference(queues);

builder.Build().Run();
