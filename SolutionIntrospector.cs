using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace SolutionIntrospector
{
    public class SolutionIntrospector : ISolutionIntrospector
    {
        private readonly ConcurrentDictionary<string, Task<Solution>> solutionCache = new ConcurrentDictionary<string, Task<Solution>>();
        private readonly ConcurrentDictionary<string, Task<Project>> projectCache = new ConcurrentDictionary<string, Task<Project>>();
        private readonly ConcurrentDictionary<string, Assembly> assemblyCache = new ConcurrentDictionary<string, Assembly>();
        private static readonly Lazy<MSBuildWorkspace> LazyWorkspace = new Lazy<MSBuildWorkspace>(CreateWorkspace);
        private static readonly object locker = new object();

        public static MSBuildWorkspace WorkspaceInstance => LazyWorkspace.Value;

        public async Task<Solution> GetSolutionInfoAsync(string solutionPath)
        {
            return await solutionCache.GetOrAdd(solutionPath, path => Task.Run(() => OpenSolution(path)));
        }

        public async Task<IEnumerable<Project>> ListProjectsAsync(string solutionPath)
        {
            var solution = await GetSolutionInfoAsync(solutionPath);
            return solution.Projects;
        }

        public async Task<Project> GetProjectInfoAsync(string projectPath)
        {
            await semaphore.WaitAsync();
            try
            {
                if (!WorkspaceInstance.CurrentSolution.Projects.Any(p => p.FilePath == projectPath))
                {
                    return await projectCache.GetOrAdd(projectPath, path => Task.Run(() => OpenProject(projectPath)));
                }
            }
            finally
            {
                semaphore.Release();
            }
            return null;
        }
        public async Task<IEnumerable<Assembly>> ListAssembliesAsync(string projectPath)
        {
            var project = await GetProjectInfoAsync(projectPath);
            var outputFilePath = project.OutputFilePath;
            var assembly = Assembly.LoadFrom(outputFilePath);
            return new List<Assembly> { assembly };
        }

        public async Task<Assembly> GetAssemblyInfoAsync(string assemblyPath)
        {
            return await Task.Run(() => assemblyCache.GetOrAdd(assemblyPath, path => Assembly.LoadFrom(path)));
        }

        public async Task<IEnumerable<string>> ListNamespacesAsync(string assemblyPath)
        {
            var assembly = await GetAssemblyInfoAsync(assemblyPath);
            return await Task.Run(() =>
            {
                var types = assembly.GetTypes();
                var namespaces = new HashSet<string>(types.Select(t => t.Namespace));
                return new List<string>(namespaces);
            });
        }

        public async Task<IEnumerable<Type>> ListClassesAsync(string namespaceName, string assemblyPath)
        {
            var assembly = await GetAssemblyInfoAsync(assemblyPath);
            return await Task.Run(() =>
                assembly.GetTypes().Where(t => t.Namespace == namespaceName).ToList()
            );
        }

        public async Task<Type> GetClassInfoAsync(string className, string namespaceName, string assemblyPath)
        {
            var classes = await ListClassesAsync(namespaceName, assemblyPath);
            return classes.FirstOrDefault(t => t.Name == className);
        }

        public async Task<IEnumerable<MethodInfo>> ListMethodsAsync(string className, string namespaceName, string assemblyPath)
        {
            var type = await GetClassInfoAsync(className, namespaceName, assemblyPath);
            return await Task.Run(() => new List<MethodInfo>(type.GetMethods()));
        }


        public async Task<IEnumerable<FieldInfo>> ListFieldsAsync(string className, string namespaceName, string assemblyPath)
        {
            var type = await GetClassInfoAsync(className, namespaceName, assemblyPath);
            return new List<FieldInfo>(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
        }

        public async Task<FieldInfo> GetFieldInfoAsync(string fieldName, string className, string namespaceName, string assemblyPath)
        {
            var type = await GetClassInfoAsync(className, namespaceName, assemblyPath);
            return await Task.Run(() => type.GetField(fieldName));
        }

        public async Task<IEnumerable<MethodDeclarationSyntax>> GetMethodSyntaxTreeAsync(string methodName, string className, string namespaceName, string assemblyPath)
        {
            var project = await GetProjectInfoAsync(assemblyPath); // Ensure projectPath is used here if different from assemblyPath
            var compilation = await project.GetCompilationAsync();
            var tree = compilation.SyntaxTrees.FirstOrDefault(); // You might want to ensure the correct syntax tree is selected
            var root = tree.GetRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == methodName).ToList();
            return methods;
        }
        private static Solution OpenSolution(string path)
        {
            lock (locker)
            {
                var workspace = WorkspaceInstance;
                if (workspace.CurrentSolution.FilePath == null ||
                    !workspace.CurrentSolution.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return workspace.OpenSolutionAsync(path).Result;
                }

                return workspace.CurrentSolution;
            }
        }

        private static Project OpenProject(string path)
        {
            lock (locker)
            {
                var workspace = WorkspaceInstance;
                var project = workspace.CurrentSolution.Projects.FirstOrDefault(p => p.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (project == null)
                {
                    return workspace.OpenProjectAsync(path).Result;
                }

                return project;
            }
        }

        private static MSBuildWorkspace CreateWorkspace()
        {
            var instance = MSBuildLocator.QueryVisualStudioInstances().First();
            MSBuildLocator.RegisterInstance(instance);
            return MSBuildWorkspace.Create();
        }


        public async Task<string> GetHomePageAsync()
        {
            return await Task.FromResult("SolutionIntrospector API V1");
        }

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    }
}
