using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SolutionIntrospector;
using SolutionIntrospector.DTO;
using Swashbuckle.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container.
builder.Services.AddSingleton<ISolutionIntrospector, SolutionIntrospector.SolutionIntrospector>(); // Registers SolutionIntrospector with DI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SolutionIntrospector API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCorsPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SolutionIntrospector API V1"));
}

app.UseHttpsRedirection();

app.UseCors("OpenCorsPolicy");

app.MapGet("/", async () => "SolutionIntrospector API V1");

// Other middleware...
ISolutionIntrospector introspector = app.Services.GetRequiredService<ISolutionIntrospector>();
string s = @"C:\Users\USER\source\repos\SolutionIntrospectpr\SolutionIntrospector.sln";
app.MapGet("/{*s}", async (string s) =>
{
    s = System.Net.WebUtility.UrlDecode(s);
    Microsoft.CodeAnalysis.Solution solutionInfoResult = await introspector.GetSolutionInfoAsync(s);
    return solutionInfoResult.Projects.Count();
});

//await introspector.GetHomePageAsync());
SolutionController controller = new SolutionController(introspector);

app.MapGet("/solution/{solutionPath}", async (string solutionPath) =>
    await controller.GetSolutionInfoAsync(solutionPath));

app.MapGet("/solution/{solutionPath}/projects", async (string solutionPath) =>
{
    var actionResult = await controller.ListProjectsAsync(solutionPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as IEnumerable<ProjectDto>;
    return dtos;
});

app.MapGet("/project/{projectPath}", async (string projectPath) =>
{
    var actionResult = await controller.GetProjectInfoAsync(projectPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as ProjectDto;
    return dtos;
});

app.MapGet("/project/{projectPath}/assemblies", async (string projectPath) =>
{
    var actionResult = await controller.ListAssembliesAsync(projectPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as IEnumerable<AssemblyDto>;
    return dtos;
});

app.MapGet("/assembly/{assemblyPath}", async (string assemblyPath) =>
{
    var actionResult = await controller.GetAssemblyInfoAsync(assemblyPath);
    var dto = (actionResult.Result as OkObjectResult)?.Value as AssemblyDto;
    return dto;
});

app.MapGet("/assembly/{assemblyPath}/namespaces", async (string assemblyPath) =>
{
    var actionResult = await controller.ListNamespacesAsync(assemblyPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as IEnumerable<string>;
    return dtos;
});

app.MapGet("/assembly/{assemblyPath}/classes", async (string namespaceName, string assemblyPath) =>
{
    var actionResult = await controller.ListClassesAsync(namespaceName, assemblyPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as IEnumerable<TypeDto>;
    return dtos;
});

app.MapGet("/class", async (string className, string namespaceName, string assemblyPath) =>
{
    var actionResult = await controller.GetClassInfoAsync(className, namespaceName, assemblyPath);
    var dto = (actionResult.Result as OkObjectResult)?.Value as TypeDto;
    return dto;
});

app.MapGet("/class/methods", async (string className, string namespaceName, string assemblyPath) =>
{
    var actionResult = await controller.ListMethodsAsync(className, namespaceName, assemblyPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as IEnumerable<MethodInfoDto>;
    return dtos;
});

app.MapGet("/method/syntaxtree", async (string methodName, string className, string namespaceName, string assemblyPath) =>
{
    var actionResult = await controller.GetMethodSyntaxTreeAsync(methodName, className, namespaceName, assemblyPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as IEnumerable<MethodSyntaxTreeDto>;
    return dtos;
});

app.MapGet("/class/fields", async (string className, string namespaceName, string assemblyPath) =>
{
    var actionResult = await controller.ListFieldsAsync(className, namespaceName, assemblyPath);
    var dtos = (actionResult.Result as OkObjectResult)?.Value as IEnumerable<FieldInfoDto>;
    return dtos;
});

app.MapGet("/field/{fieldName}", async (string fieldName, string className, string namespaceName, string assemblyPath) =>
{
    var actionResult = await controller.GetFieldInfoAsync(fieldName, className, namespaceName, assemblyPath);
    var dto = (actionResult.Result as OkObjectResult)?.Value as FieldInfoDto;
    return dto;
});


app.Run();
