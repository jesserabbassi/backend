using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

public class HealthCheckDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
    {
        
        var healthCheckPath = "/health";

        var operation = new OpenApiOperation
        {
            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Health Check" } },
        };

       
        operation.Responses.Add("200", new OpenApiResponse
        {
            Description = "(Healthy)"
        });

        
        operation.Responses.Add("503", new OpenApiResponse
        {
            Description = "(Unhealthy)"
        });

        
        var pathItem = new OpenApiPathItem();
        pathItem.AddOperation(OperationType.Get, operation);

        openApiDoc.Paths.Add(healthCheckPath, pathItem);
    }
}
