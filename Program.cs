using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SolutionIntrospector;
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

// Other middleware...
ISolutionIntrospector introspector = app.Services.GetRequiredService<ISolutionIntrospector>();

app.MapGet("/solution/{solutionPath}", async (string solutionPath) =>
    await introspector.GetSolutionInfoAsync(solutionPath));

app.MapGet("/solution/{solutionPath}/projects", async (string solutionPath) =>
    await introspector.ListProjectsAsync(solutionPath));

app.MapGet("/project/{projectPath}", async (string projectPath) =>
    await introspector.GetProjectInfoAsync(projectPath));

app.MapGet("/project/{projectPath}/assemblies", async (string projectPath) =>
    await introspector.ListAssembliesAsync(projectPath));

app.MapGet("/assembly/{assemblyPath}", async (string assemblyPath) =>
    await introspector.GetAssemblyInfoAsync(assemblyPath));

app.MapGet("/assembly/{assemblyPath}/namespaces", async (string assemblyPath) =>
    await introspector.ListNamespacesAsync(assemblyPath));

app.MapGet("/assembly/{assemblyPath}/classes", async (string namespaceName, string assemblyPath) =>
    await introspector.ListClassesAsync(namespaceName, assemblyPath));

app.MapGet("/class/{className}", async (string className, string namespaceName, string assemblyPath) =>
    await introspector.GetClassInfoAsync(className, namespaceName, assemblyPath));

app.MapGet("/class/{className}/methods", async (string className, string namespaceName, string assemblyPath) =>
    await introspector.ListMethodsAsync(className, namespaceName, assemblyPath));

app.MapGet("/method/{methodName}/syntaxtree", async (string methodName, string className, string namespaceName, string assemblyPath) =>
    await introspector.GetMethodSyntaxTreeAsync(methodName, className, namespaceName, assemblyPath));

app.MapGet("/class/{className}/fields", async (string className, string namespaceName, string assemblyPath) =>
    await introspector.ListFieldsAsync(className, namespaceName, assemblyPath));

app.MapGet("/field/{fieldName}", async (string fieldName, string className, string namespaceName, string assemblyPath) =>
    await introspector.GetFieldInfoAsync(fieldName, className, namespaceName, assemblyPath));

app.Run();
