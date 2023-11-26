using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DotNetAnalyzerPro.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAnalyzerPro
{
    [ApiController]
    [Route("api/solution")]
    public class SolutionController : ControllerBase
    {
        private readonly IDotNetAnalyzerPro _DotNetAnalyzerPro;

        public SolutionController(IDotNetAnalyzerPro DotNetAnalyzerPro)
        {
            _DotNetAnalyzerPro = DotNetAnalyzerPro;
        }

        [HttpGet("GetSolutionInfo")]
        public async Task<ActionResult<SolutionDto>> GetSolutionInfoAsync(string solutionPath)
        {
            if (!VerifyFullPath(ref solutionPath))
            {
                return (ActionResult<SolutionDto>)StatusCode(500);
            }
            try
            {
                SolutionDto solutionDto = MapToSolutionDto(await _DotNetAnalyzerPro.GetSolutionInfoAsync(solutionPath));
                return (ActionResult<SolutionDto>)solutionDto;
            }
            catch (Exception ex2)
            {
                Exception ex = ex2;
                return (ActionResult<SolutionDto>)StatusCode(500, ex.Message);
            }
        }

        private SolutionDto MapToSolutionDto(Solution solution)
        {
            return new SolutionDto
            {
                FilePath = solution.FilePath,
                Projects = solution.Projects.Select((Project p) => new ProjectDto
                {
                    Name = p.Name
                }).ToList()
            };
        }

        [HttpGet("ListProjects")]
        public async Task<ActionResult<List<ProjectDto>>> ListProjectsAsync(string solutionPath)
        {
            if (!VerifyFullPath(ref solutionPath))
            {
                return (ActionResult<List<ProjectDto>>)StatusCode(500);
            }
            try
            {
                List<ProjectDto> projectDtos = (await _DotNetAnalyzerPro.ListProjectsAsync(solutionPath)).Select((Project p) => new ProjectDto
                {
                    Name = p.Name
                }).ToList();
                return (ActionResult<List<ProjectDto>>)Ok(projectDtos);
            }
            catch (Exception ex2)
            {
                Exception ex = ex2;
                return (ActionResult<List<ProjectDto>>)StatusCode(500, ex.Message);
            }
        }

        public async Task<ActionResult<ProjectDto>> GetProjectInfoAsync(string projectPath)
        {
            if (!VerifyFullPath(ref projectPath))
            {
                return (ActionResult<ProjectDto>)StatusCode(500);
            }
            ProjectDto projectDto = MapToProjectDto(await _DotNetAnalyzerPro.GetProjectInfoAsync(projectPath));
            return (ActionResult<ProjectDto>)Ok(projectDto);
        }

        public async Task<ActionResult<IEnumerable<AssemblyDto>>> ListAssembliesAsync(string projectPath)
        {
            if (!VerifyFullPath(ref projectPath))
            {
                return (ActionResult<IEnumerable<AssemblyDto>>)StatusCode(500);
            }
            IEnumerable<AssemblyDto> assemblyDtos = (await _DotNetAnalyzerPro.ListAssembliesAsync(projectPath)).Select((Assembly a) => MapToAssemblyDto(a));
            return (ActionResult<IEnumerable<AssemblyDto>>)Ok(assemblyDtos);
        }

        public async Task<ActionResult<AssemblyDto>> GetAssemblyInfoAsync(string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<AssemblyDto>)StatusCode(500);
            }
            AssemblyDto assemblyDto = MapToAssemblyDto(await _DotNetAnalyzerPro.GetAssemblyInfoAsync(assemblyPath));
            return (ActionResult<AssemblyDto>)Ok(assemblyDto);
        }

        public async Task<ActionResult<IEnumerable<string>>> ListNamespacesAsync(string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<IEnumerable<string>>)StatusCode(500);
            }
            return (ActionResult<IEnumerable<string>>)Ok(await _DotNetAnalyzerPro.ListNamespacesAsync(assemblyPath));
        }

        public async Task<ActionResult<IEnumerable<TypeDto>>> ListClassesAsync(string namespaceName, string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<IEnumerable<TypeDto>>)StatusCode(500);
            }
            IEnumerable<TypeDto> classDtos = (await _DotNetAnalyzerPro.ListClassesAsync(namespaceName, assemblyPath)).Select((Type c) => MapToTypeDto(c));
            return (ActionResult<IEnumerable<TypeDto>>)Ok(classDtos);
        }

        public async Task<ActionResult<TypeDto>> GetClassInfoAsync(string className, string namespaceName, string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<TypeDto>)StatusCode(500);
            }
            TypeDto classDto = MapToTypeDto(await _DotNetAnalyzerPro.GetClassInfoAsync(className, namespaceName, assemblyPath));
            return (ActionResult<TypeDto>)Ok(classDto);
        }

        public async Task<ActionResult<IEnumerable<MethodInfoDto>>> ListMethodsAsync(string className, string namespaceName, string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<IEnumerable<MethodInfoDto>>)StatusCode(500);
            }
            IEnumerable<MethodInfoDto> methodDtos = (await _DotNetAnalyzerPro.ListMethodsAsync(className, namespaceName, assemblyPath)).Select((MethodInfo m) => MapToMethodInfoDto(m));
            return (ActionResult<IEnumerable<MethodInfoDto>>)Ok(methodDtos);
        }

        public async Task<ActionResult<IEnumerable<MethodSyntaxTreeDto>>> GetMethodSyntaxTreeAsync(string methodName, string className, string namespaceName, string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<IEnumerable<MethodSyntaxTreeDto>>)StatusCode(500);
            }
            IEnumerable<MethodSyntaxTreeDto> methodSyntaxTreeDtos = (await _DotNetAnalyzerPro.GetMethodSyntaxTreeAsync(methodName, className, namespaceName, assemblyPath)).Select((MethodDeclarationSyntax m) => MapToMethodDeclarationSyntaxDto(m));
            return (ActionResult<IEnumerable<MethodSyntaxTreeDto>>)Ok(methodSyntaxTreeDtos);
        }

        public async Task<ActionResult<IEnumerable<FieldInfoDto>>> ListFieldsAsync(string className, string namespaceName, string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<IEnumerable<FieldInfoDto>>)StatusCode(500);
            }
            IEnumerable<FieldInfoDto> fieldDtos = (await _DotNetAnalyzerPro.ListFieldsAsync(className, namespaceName, assemblyPath)).Select((FieldInfo f) => MapToFieldInfoDto(f));
            return (ActionResult<IEnumerable<FieldInfoDto>>)Ok(fieldDtos);
        }

        public async Task<ActionResult<FieldInfoDto>> GetFieldInfoAsync(string fieldName, string className, string namespaceName, string assemblyPath)
        {
            if (!VerifyFullPath(ref assemblyPath))
            {
                return (ActionResult<FieldInfoDto>)StatusCode(500);
            }
                
            FieldInfoDto fieldInfoDto = MapToFieldInfoDto(await _DotNetAnalyzerPro.GetFieldInfoAsync(fieldName, className, namespaceName, assemblyPath));
            return (ActionResult<FieldInfoDto>)Ok(fieldInfoDto);
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
                Version = assembly.GetName().Version!.ToString(),
                Culture = assembly.GetName().CultureInfo?.Name,
                PublicKeyToken = BitConverter.ToString(assembly.GetName().GetPublicKeyToken()).Replace("-", string.Empty)
            };
        }

        private bool VerifyFullPath(ref string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                filePath = null;
                return false;
            }
            if (!System.IO.File.Exists(fileName))
            {
                string path = "C:\\Users\\USER\\source";
                string[] files = Directory.GetFiles(path, fileName, SearchOption.AllDirectories);
                filePath = files.FirstOrDefault();
                return filePath != null;
            }
            return true;
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
                Parameters = (from p in methodInfo.GetParameters()
                              select new ParameterDto
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
                ChildNodes = syntaxNode.ChildNodes().Select(new Func<SyntaxNode, SyntaxNodeDto>(MapSyntaxNodeDto)).ToList()
            };
        }

        private SyntaxNodeDto ConvertSyntaxNode(SyntaxNode node)
        {
            return new SyntaxNodeDto
            {
                Kind = node.Kind().ToString(),
                Text = node.ToString(),
                ChildNodes = node.ChildNodes().Select(new Func<SyntaxNode, SyntaxNodeDto>(ConvertSyntaxNode)).ToList()
            };
        }
    }
}
