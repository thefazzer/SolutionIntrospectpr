using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using Microsoft.CodeAnalysis.MSBuild;

namespace SolutionIntrospector
{
    public class SolutionIntrospector : ISolutionIntrospector
    {
        private readonly ConcurrentDictionary<string, Solution> solutionCache = new ConcurrentDictionary<string, Solution>();
        private readonly ConcurrentDictionary<string, Project> projectCache = new ConcurrentDictionary<string, Project>();
        private readonly ConcurrentDictionary<string, Assembly> assemblyCache = new ConcurrentDictionary<string, Assembly>();

        public Solution GetSolutionInfo(string solutionPath)
        {
            return solutionCache.GetOrAdd(solutionPath, path =>
            {
                var workspace = MSBuildWorkspace.Create();
                return workspace.OpenSolutionAsync(path).Result;
            });
        }

        public List<Project> ListProjects(string solutionPath)
        {
            var solution = GetSolutionInfo(solutionPath);
            return new List<Project>(solution.Projects);
        }

        public Project GetProjectInfo(string projectPath)
        {
            return projectCache.GetOrAdd(projectPath, path =>
            {
                var workspace = MSBuildWorkspace.Create();
                return workspace.OpenProjectAsync(path).Result;
            });
        }

        public List<Assembly> ListAssemblies(string projectPath)
        {
            var project = GetProjectInfo(projectPath);
            var outputFilePath = project.OutputFilePath;
            var assembly = Assembly.LoadFrom(outputFilePath);
            return new List<Assembly> { assembly };
        }

        public Assembly GetAssemblyInfo(string assemblyPath)
        {
            return assemblyCache.GetOrAdd(assemblyPath, path => Assembly.LoadFrom(path));
        }

        public List<string> ListNamespaces(string assemblyPath)
        {
            var assembly = GetAssemblyInfo(assemblyPath);
            var types = assembly.GetTypes();
            var namespaces = new HashSet<string>(types.Select(t => t.Namespace));
            return new List<string>(namespaces);
        }

        public List<Type> ListClasses(string namespaceName, string assemblyPath)
        {
            var assembly = GetAssemblyInfo(assemblyPath);
            var types = assembly.GetTypes();
            return types.Where(t => t.Namespace == namespaceName).ToList();
        }

        public Type GetClassInfo(string className, string namespaceName, string assemblyPath)
        {
            var classes = ListClasses(namespaceName, assemblyPath);
            return classes.FirstOrDefault(t => t.Name == className);
        }

        public List<MethodInfo> ListMethods(string className, string namespaceName, string assemblyPath)
        {
            var type = GetClassInfo(className, namespaceName, assemblyPath);
            return new List<MethodInfo>(type.GetMethods());
        }

        public List<MethodDeclarationSyntax> GetMethodSyntaxTree(string methodName, string className, string namespaceName, string assemblyPath)
        {
            var project = GetProjectInfo(assemblyPath); // Assuming project file path and assembly path are the same for simplicity
            var compilation = project.GetCompilationAsync().Result;
            var tree = compilation.SyntaxTrees.FirstOrDefault();
            var root = tree.GetRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ToString() == methodName).ToList();
            return methods;
        }

        public List<FieldInfo> ListFields(string className, string namespaceName, string assemblyPath)
        {
            var type = GetClassInfo(className, namespaceName, assemblyPath);
            return new List<FieldInfo>(type.GetFields());
        }

        public FieldInfo GetFieldInfo(string fieldName, string className, string namespaceName, string assemblyPath)
        {
            var type = GetClassInfo(className, namespaceName, assemblyPath);
            return type.GetField(fieldName);
        }
    }
}
