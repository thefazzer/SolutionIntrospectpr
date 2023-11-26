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

namespace DotNetAnalyzerPro
{
    public class DotNetAnalyzerPro : IDotNetAnalyzerPro
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
                Project project = null;
                // Normalize the file path for comparison
                var normalizedPath = Path.GetFullPath(projectPath);

                // Check inside the semaphore block to ensure the project hasn't been added by another thread
                var existingProject = WorkspaceInstance.CurrentSolution.Projects
                    .FirstOrDefault(p => Path.GetFullPath(p.FilePath) == normalizedPath);

                if (existingProject == null)
                {
                    // Try to get or add the project atomically to prevent race conditions
                    project = await projectCache.GetOrAdd(normalizedPath, async path =>
                    {
                        // Open the project and add it to the workspace
                        var loadedProject = await WorkspaceInstance.OpenProjectAsync(path);
                        return loadedProject;
                    });
                }
                else
                {
                    // If the project is already added, return it directly from the workspace
                    project = existingProject;
                }

                return project;
            }
            finally
            {
                semaphore.Release();
            }
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
            await semaphore.WaitAsync();
            try
            {
                Assembly assembly = null;
                // Normalize the file path for comparison
                var normalizedPath = Path.GetFullPath(assemblyPath);

                // Check inside the semaphore block to ensure the assembly hasn't been added by another thread
                if (!assemblyCache.TryGetValue(normalizedPath, out assembly))
                {
                    // Load and add the assembly atomically to prevent race conditions
                    assembly = await Task.Run(() => Assembly.LoadFrom(normalizedPath));
                    assemblyCache.TryAdd(normalizedPath, assembly);
                }

                return assembly;
            }
            finally
            {
                semaphore.Release();
            }
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
                assembly.GetTypes()
                .Where(t => t.Namespace == namespaceName && t.Assembly.Location == assembly.Location)
                .ToList());
        }


        public async Task<Type> GetClassInfoAsync(string className, string namespaceName, string assemblyPath)
        {
            var classes = await ListClassesAsync(namespaceName, assemblyPath);
            return classes.FirstOrDefault(t => t.Name == className);
        }

        public async Task<IEnumerable<MethodInfo>> ListMethodsAsync(string className, string namespaceName, string assemblyPath)
        {
            var type = await GetClassInfoAsync(className, namespaceName, assemblyPath);
            return type != null
                ? await Task.Run(() => new List<MethodInfo>(type.GetMethods()))
                : Enumerable.Empty<MethodInfo>();
        }

        public async Task<IEnumerable<FieldInfo>> ListFieldsAsync(string className, string namespaceName, string assemblyPath)
        {
            var type = await GetClassInfoAsync(className, namespaceName, assemblyPath);
            return type != null
                ? new List<FieldInfo>(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                : Enumerable.Empty<FieldInfo>();
        }

        public async Task<FieldInfo> GetFieldInfoAsync(string fieldName, string className, string namespaceName, string assemblyPath)
        {
            var type = await GetClassInfoAsync(className, namespaceName, assemblyPath);
            return type != null
                ? await Task.Run(() => type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                : null;
        }

        public async Task<IEnumerable<MethodDeclarationSyntax>> GetMethodSyntaxTreeAsync(string methodName, string className, string namespaceName, string projectPath)
        {
            var project = await GetProjectInfoAsync(projectPath);
            var compilation = await project.GetCompilationAsync();
            var trees = compilation.SyntaxTrees;

            List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();

            foreach (var tree in trees)
            {
                var root = await tree.GetRootAsync();
                var model = compilation.GetSemanticModel(tree);

                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => c.Identifier.Text == className);

                foreach (var classDeclaration in classes)
                {
                    var declaredSymbol = model.GetDeclaredSymbol(classDeclaration);

                    if (declaredSymbol != null && declaredSymbol.ContainingNamespace.Name == namespaceName)
                    {
                        var methodsInClass = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == methodName);
                        methods.AddRange(methodsInClass);
                    }
                }
            }

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

        private static Microsoft.CodeAnalysis.Project OpenProject(string path)
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
            return await Task.FromResult("DotNetAnalyzerPro API V1");
        }

        public async Task<IEnumerable<string>> ListSourceFilesAsync(string projectPath)
        {
            // Ensure the project file exists
            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("Project file not found.", projectPath);
            }

            // Load the project
            var project = OpenProject(projectPath);
            // Get all source files, this assumes they are included in the project
            var filePaths = project.Documents
                                   .Select(doc => doc.FilePath)
                                   .Where(path => !string.IsNullOrWhiteSpace(path))
                                   .ToList();

            return filePaths;
        }

        public async Task<IEnumerable<string>> GetAllSourceFilesAsync(Project project)
        {
            // This will hold the file paths for all source documents in the project
            var filePaths = project.Documents
                                   .Select(doc => doc.FilePath)
                                   .Where(path => !string.IsNullOrWhiteSpace(path))
                                   .ToList();

            return filePaths;
        }

        public async Task<string> GetFileContentAsync(string filePath)
        {
            // Ensure the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }

            // Read the file content
            var content = await File.ReadAllTextAsync(filePath);
            return content;
        }

        public async Task<IEnumerable<Microsoft.Build.Evaluation.ProjectItem>> ListProjectReferencesAsync(string projectPath)
        {
            // Ensure the project file exists
            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("Project file not found.", projectPath);
            }

            // Load the project
            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
            var project = projectCollection.LoadProject(projectPath);
            var projectReferences = project.GetItems("ProjectReference");
             //   .Select(pr => new ProjectReference(pr.EvaluatedInclude));
                

            return projectReferences;
        }

        public async Task<IEnumerable<Microsoft.Build.Evaluation.ProjectItem>> ListPackageReferencesAsync(string projectPath)
        {
            // Ensure the project file exists
            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException("Project file not found.", projectPath);
            }

            // Load the project
            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
            var msbuildProject = projectCollection.LoadProject(projectPath);
            var packageReferences = msbuildProject.Items
                .Where(i => i.ItemType == "PackageReference")
                .ToList();

            // Make sure to dispose of the project collection to release all MSBuild-related resources
            projectCollection.Dispose();

            return packageReferences;
        }

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    }
}
