using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using NuGet.Packaging;

namespace DotNetAnalyzerPro
{
    public interface IDotNetAnalyzerPro
    {
        Task<string> GetHomePageAsync();
        Task<Solution> GetSolutionInfoAsync(string solutionPath);
        Task<IEnumerable<Project>> ListProjectsAsync(string solutionPath);
        Task<Project> GetProjectInfoAsync(string projectPath);
        Task<IEnumerable<Assembly>> ListAssembliesAsync(string projectPath);
        Task<Assembly> GetAssemblyInfoAsync(string assemblyPath);
        Task<IEnumerable<string>> ListNamespacesAsync(string assemblyPath);
        Task<IEnumerable<Type>> ListClassesAsync(string namespaceName, string assemblyPath);
        Task<Type> GetClassInfoAsync(string className, string namespaceName, string assemblyPath);
        Task<IEnumerable<MethodInfo>> ListMethodsAsync(string className, string namespaceName, string assemblyPath);
        Task<IEnumerable<MethodDeclarationSyntax>> GetMethodSyntaxTreeAsync(string methodName, string className, string namespaceName, string assemblyPath);
        Task<IEnumerable<FieldInfo>> ListFieldsAsync(string className, string namespaceName, string assemblyPath);
        Task<FieldInfo> GetFieldInfoAsync(string fieldName, string className, string namespaceName, string assemblyPath);
        Task<IEnumerable<string>> ListSourceFilesAsync(string projectPath);

        // Method to get the content of a file by its path.
        Task<string> GetFileContentAsync(string filePath);

        // Method to list all project references (projects referenced by the given project).
        Task<IEnumerable<Microsoft.Build.Evaluation.ProjectItem>> ListProjectReferencesAsync(string projectPath);

        // Method to list all package references (NuGet packages used by the given project).
        Task<IEnumerable<Microsoft.Build.Evaluation.ProjectItem>> ListPackageReferencesAsync(string projectPath);

    }
}
