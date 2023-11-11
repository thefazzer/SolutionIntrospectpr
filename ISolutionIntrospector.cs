using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace SolutionIntrospector
{
    public interface ISolutionIntrospector
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
    }
}
