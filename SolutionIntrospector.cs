using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static readonly Lazy<MSBuildWorkspace> LazyWorkspace = new Lazy<MSBuildWorkspace>(() =>
        {
            //AppContext.SetSwitch("Switch.Microsoft.Build.NoInprocNode", true);
            //MSBuildLocator.RegisterMSBuildPath(@"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin");

            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances();
            foreach (var inst in visualStudioInstances)
            {
                Console.WriteLine(inst.Name);
                Console.WriteLine(inst.MSBuildPath);
            }
            var instance = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances().First();
            Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);


            //MSBuildLocator.RegisterDefaults();
            return MSBuildWorkspace.Create();
        });

        public static MSBuildWorkspace WorkspaceInstance => LazyWorkspace.Value;


        // Assuming Assembly loading can be done synchronously, otherwise you would need a similar async approach
        private readonly ConcurrentDictionary<string, Assembly> assemblyCache = new ConcurrentDictionary<string, Assembly>();

        public async Task<Solution> GetSolutionInfoAsync(string solutionPath)
        {
            return await solutionCache.GetOrAdd(solutionPath, async path =>
            {
                var workspace = WorkspaceInstance;
                return await workspace.OpenSolutionAsync(path);
            });
        }

        public async Task<IEnumerable<Project>> ListProjectsAsync(string solutionPath)
        {
            var solution = await GetSolutionInfoAsync(solutionPath);
            return new List<Project>(solution.Projects);
        }

        public async Task<Project> GetProjectInfoAsync(string projectPath)
        {
            return await projectCache.GetOrAdd(projectPath, async path =>
            {
                var workspace = WorkspaceInstance;
                return await workspace.OpenProjectAsync(path);
            });
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
            return new List<FieldInfo>(type.GetFields());
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
    }
}
