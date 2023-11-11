using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SolutionIntrospector.DTO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace SolutionIntrospector
{
    [ApiController]
    [Route("api/solution")]
    public class SolutionController : ControllerBase
    {
        private readonly ISolutionIntrospector _solutionIntrospector;

        public SolutionController(ISolutionIntrospector solutionIntrospector)
        {
            _solutionIntrospector = solutionIntrospector;
        }

        [HttpGet("GetSolutionInfo")]
        public async Task<ActionResult<SolutionDto>> GetSolutionInfoAsync(string solutionPath)
        {
            try
            {
                var solutionInfo = await _solutionIntrospector.GetSolutionInfoAsync(solutionPath);
                var solutionDto = MapToSolutionDto(solutionInfo);
                return solutionDto;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private SolutionDto MapToSolutionDto(Solution solution)
        {
            // Mapping logic from Solution to SolutionDto
            var solutionDto = new SolutionDto
            {
                FilePath = solution.FilePath,
                Projects = solution.Projects.Select(p => new ProjectDto
                {
                    Name = p.Name
                    // Fill in other properties...
                }).ToList()
            };
            return solutionDto;
        }

        [HttpGet("ListProjects")]
        public async Task<ActionResult<List<ProjectDto>>> ListProjectsAsync(string solutionPath)
        {
            try
            {
                var projects = await _solutionIntrospector.ListProjectsAsync(solutionPath);
                var projectDtos = projects.Select(p => new ProjectDto
                {
                    Name = p.Name
                    // Map additional properties...
                }).ToList();
                return Ok(projectDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        public async Task<ActionResult<ProjectDto>> GetProjectInfoAsync(string projectPath)
        {
            var projectInfo = await _solutionIntrospector.GetProjectInfoAsync(projectPath);
            var projectDto = MapToProjectDto(projectInfo);
            return Ok(projectDto);
        }

        public async Task<ActionResult<IEnumerable<AssemblyDto>>> ListAssembliesAsync(string projectPath)
        {
            var assemblies = await _solutionIntrospector.ListAssembliesAsync(projectPath);
            var assemblyDtos = assemblies.Select(a => MapToAssemblyDto(a));
            return Ok(assemblyDtos);
        }

        public async Task<ActionResult<AssemblyDto>> GetAssemblyInfoAsync(string assemblyPath)
        {
            var assemblyInfo = await _solutionIntrospector.GetAssemblyInfoAsync(assemblyPath);
            var assemblyDto = MapToAssemblyDto(assemblyInfo);
            return Ok(assemblyDto);
        }

        public async Task<ActionResult<IEnumerable<string>>> ListNamespacesAsync(string assemblyPath)
        {
            var namespaces = await _solutionIntrospector.ListNamespacesAsync(assemblyPath);
            return Ok(namespaces);
        }

        public async Task<ActionResult<IEnumerable<TypeDto>>> ListClassesAsync(string namespaceName, string assemblyPath)
        {
            var classes = await _solutionIntrospector.ListClassesAsync(namespaceName, assemblyPath);
            var classDtos = classes.Select(c => MapToTypeDto(c));
            return Ok(classDtos);
        }

        public async Task<ActionResult<TypeDto>> GetClassInfoAsync(string className, string namespaceName, string assemblyPath)
        {
            var classInfo = await _solutionIntrospector.GetClassInfoAsync(className, namespaceName, assemblyPath);
            var classDto = MapToTypeDto(classInfo);
            return Ok(classDto);
        }

        public async Task<ActionResult<IEnumerable<MethodInfoDto>>> ListMethodsAsync(string className, string namespaceName, string assemblyPath)
        {
            var methods = await _solutionIntrospector.ListMethodsAsync(className, namespaceName, assemblyPath);
            var methodDtos = methods.Select(m => MapToMethodInfoDto(m));
            return Ok(methodDtos);
        }

        public async Task<ActionResult<IEnumerable<MethodSyntaxTreeDto>>> GetMethodSyntaxTreeAsync(string methodName, string className, string namespaceName, string assemblyPath)
        {
            var methodSyntaxTrees = await _solutionIntrospector.GetMethodSyntaxTreeAsync(methodName, className, namespaceName, assemblyPath);
            var methodSyntaxTreeDtos = methodSyntaxTrees.Select(m => MapToMethodDeclarationSyntaxDto(m));
            return Ok(methodSyntaxTreeDtos);
        }

        public async Task<ActionResult<IEnumerable<FieldInfoDto>>> ListFieldsAsync(string className, string namespaceName, string assemblyPath)
        {
            var fields = await _solutionIntrospector.ListFieldsAsync(className, namespaceName, assemblyPath);
            var fieldDtos = fields.Select(f => MapToFieldInfoDto(f));
            return Ok(fieldDtos);
        }

        public async Task<ActionResult<FieldInfoDto>> GetFieldInfoAsync(string fieldName, string className, string namespaceName, string assemblyPath)
        {
            var fieldInfo = await _solutionIntrospector.GetFieldInfoAsync(fieldName, className, namespaceName, assemblyPath);
            var fieldInfoDto = MapToFieldInfoDto(fieldInfo);
            return Ok(fieldInfoDto);
        }
        private ProjectDto MapToProjectDto(Project project)
        {
            return new ProjectDto
            {
                Name = project.Name,
                FilePath = project.FilePath
            };
        }

        private AssemblyDto MapToAssemblyDto(Assembly assembly)
        {
            return new AssemblyDto
            {
                Name = assembly.GetName().Name,
                Version = assembly.GetName().Version.ToString(),
                Culture = assembly.GetName().CultureInfo?.Name,
                PublicKeyToken = BitConverter.ToString(assembly.GetName().GetPublicKeyToken()).Replace("-", string.Empty)
            };
        }

        private TypeDto MapToTypeDto(Type type)
        {
            return new TypeDto
            {
                Name = type.Name,
                Namespace = type.Namespace,
                AssemblyName = type.Assembly.GetName().Name
            };
        }

        private MethodInfoDto MapToMethodInfoDto(MethodInfo methodInfo)
        {
            return new MethodInfoDto
            {
                Name = methodInfo.Name,
                ReturnType = methodInfo.ReturnType.FullName,
                Parameters = methodInfo.GetParameters().Select(p => new ParameterDto
                {
                    Name = p.Name,
                    Type = p.ParameterType.FullName
                }).ToList()
            };
        }

        private FieldInfoDto MapToFieldInfoDto(FieldInfo fieldInfo)
        {
            return new FieldInfoDto
            {
                Name = fieldInfo.Name,
                Type = fieldInfo.FieldType.FullName
            };
        }

        private MethodSyntaxTreeDto MapToMethodDeclarationSyntaxDto(MethodDeclarationSyntax methodSyntax)
        {
            return new MethodSyntaxTreeDto
            {
                MethodName = methodSyntax.Identifier.ValueText,
                Root = MapSyntaxNodeDto(methodSyntax)
            };
        }

        private SyntaxNodeDto MapSyntaxNodeDto(SyntaxNode syntaxNode)
        {
            return new SyntaxNodeDto
            {
                Kind = syntaxNode.Kind().ToString(),
                Text = syntaxNode.ToString(),
                ChildNodes = syntaxNode.ChildNodes().Select(MapSyntaxNodeDto).ToList()
            };
        }
        private SyntaxNodeDto ConvertSyntaxNode(SyntaxNode node)
        {
            return new SyntaxNodeDto
            {
                Kind = node.Kind().ToString(),
                Text = node.ToString(),
                ChildNodes = node.ChildNodes().Select(ConvertSyntaxNode).ToList()
            };
        }

     

    }


}
