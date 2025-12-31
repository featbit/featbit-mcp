// using Amazon.S3;
// using Amazon.S3.Model;

// namespace FeatBit.McpServer.Infrastructure;

// /// <summary>
// /// S3-based implementation of IDocumentLoader that loads documents from AWS S3 bucket.
// /// This implementation allows storing documentation in cloud storage with high availability.
// /// </summary>
// /// <remarks>
// /// To use this implementation:
// /// 1. Install NuGet package: dotnet add package AWSSDK.S3
// /// 2. Configure AWS credentials (environment variables, IAM role, or credentials file)
// /// 3. Register in Program.cs: builder.Services.AddSingleton&lt;IDocumentLoader, S3DocumentLoader&gt;();
// /// 4. Configure S3 settings in appsettings.json:
// ///    {
// ///      "S3": {
// ///        "BucketName": "featbit-docs",
// ///        "Region": "us-west-2",
// ///        "Prefix": "documentation/"
// ///      }
// ///    }
// /// </remarks>
// public class S3DocumentLoader : IDocumentLoader
// {
//     private readonly IAmazonS3 _s3Client;
//     private readonly string _bucketName;
//     private readonly string _prefix;
//     private readonly ILogger<S3DocumentLoader> _logger;

//     public S3DocumentLoader(
//         IAmazonS3 s3Client,
//         IConfiguration configuration,
//         ILogger<S3DocumentLoader> logger)
//     {
//         _s3Client = s3Client;
//         _bucketName = configuration["S3:BucketName"] 
//             ?? throw new ArgumentException("S3:BucketName configuration is required");
//         _prefix = configuration["S3:Prefix"] ?? "documentation/";
//         _logger = logger;
//     }

//     public IDocumentLoader.DocumentOption[] LoadAvailableDocuments(string[] documentFiles, string resourceSubPath)
//     {
//         var documents = new List<IDocumentLoader.DocumentOption>();

//         foreach (var fileName in documentFiles)
//         {
//             try
//             {
//                 var description = ExtractDescriptionFromMarkdown(fileName, resourceSubPath);
//                 documents.Add(new IDocumentLoader.DocumentOption(fileName, description));
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogWarning(ex, "Failed to load document metadata for {FileName}", fileName);
//                 documents.Add(new IDocumentLoader.DocumentOption(fileName, fileName));
//             }
//         }

//         return [.. documents];
//     }

//     public string ExtractDescriptionFromMarkdown(string fileName, string resourceSubPath)
//     {
//         try
//         {
//             var content = LoadDocumentContent(fileName, resourceSubPath);
            
//             // Check if file starts with YAML front matter (---)
//             if (!content.TrimStart().StartsWith("---"))
//             {
//                 return fileName;
//             }

//             var lines = content.Split('\n');
//             var inFrontMatter = false;
//             var frontMatterStarted = false;

//             foreach (var line in lines.Take(20))
//             {
//                 var trimmed = line.Trim();
                
//                 if (trimmed == "---" && !frontMatterStarted)
//                 {
//                     inFrontMatter = true;
//                     frontMatterStarted = true;
//                     continue;
//                 }
                
//                 if (trimmed == "---" && frontMatterStarted)
//                 {
//                     break;
//                 }
                
//                 if (inFrontMatter && trimmed.StartsWith("description:", StringComparison.OrdinalIgnoreCase))
//                 {
//                     var description = trimmed.Substring("description:".Length).Trim();
//                     description = description.Trim('"', '\'');
//                     return description;
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogWarning(ex, "Failed to extract description from {FileName}", fileName);
//         }

//         return fileName;
//     }

//     public string LoadDocumentContent(string fileName, string resourceSubPath)
//     {
//         try
//         {
//             // Construct S3 key: prefix/resourceSubPath/fileName
//             // Example: documentation/Sdks/DotNETSdks/NetServerSdkAspNetCore.md
//             var pathParts = resourceSubPath.Split('.');
//             var s3Key = $"{_prefix}{string.Join("/", pathParts)}/{fileName}";

//             _logger.LogDebug("Loading document from S3: s3://{Bucket}/{Key}", _bucketName, s3Key);

//             var request = new GetObjectRequest
//             {
//                 BucketName = _bucketName,
//                 Key = s3Key
//             };

//             // Synchronous call - in production, consider using async version
//             using var response = _s3Client.GetObjectAsync(request).GetAwaiter().GetResult();
//             using var reader = new StreamReader(response.ResponseStream);
//             var content = reader.ReadToEnd();

//             _logger.LogInformation("Successfully loaded document from S3: {Key}", s3Key);
//             return content;
//         }
//         catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
//         {
//             _logger.LogWarning("Document not found in S3: {FileName} in {SubPath}", fileName, resourceSubPath);
//             return $"❌ Document '{fileName}' not found in S3 bucket '{_bucketName}'.";
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Failed to load document from S3: {FileName}", fileName);
//             return $"❌ Error loading '{fileName}' from S3: {ex.Message}";
//         }
//     }
// }
