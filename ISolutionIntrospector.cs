using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace SolutionIntrospector
{
    public interface ISolutionIntrospector
    {
        Solution GetSolutionInfo(string solutionPath);
        List<Project> ListProjects(string solutionPath);
        Project GetProjectInfo(string projectPath);
        List<Assembly> ListAssemblies(string projectPath);
        Assembly GetAssemblyInfo(string assemblyPath);
        List<string> ListNamespaces(string assemblyPath);
        List<Type> ListClasses(string namespaceName, string assemblyPath);
        Type GetClassInfo(string className, string namespaceName, string assemblyPath);
        List<MethodInfo> ListMethods(string className, string namespaceName, string assemblyPath);
        List<MethodDeclarationSyntax> GetMethodSyntaxTree(string methodName, string className, string namespaceName, string assemblyPath);
        List<FieldInfo> ListFields(string className, string namespaceName, string assemblyPath);
        FieldInfo GetFieldInfo(string fieldName, string className, string namespaceName, string assemblyPath);
    }
}
