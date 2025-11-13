using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace FableCraft.Server;

internal sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo
            .GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile)
                        || p.ParameterType == typeof(IEnumerable<IFormFile>)
                        || p.ParameterType == typeof(List<IFormFile>)
                        || p.ParameterType == typeof(IFormFile[]))
            .ToList();

        if (!fileParameters.Any())
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = context.MethodInfo.GetParameters()
                            .ToDictionary(
                                p => p.Name,
                                p => p.ParameterType == typeof(IFormFile)
                                    ? new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    }
                                    : new OpenApiSchema
                                    {
                                        Type = "string"
                                    })
                    }
                }
            }
        };
    }
}