using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SolutionIntrospector.DTO;
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
        public ActionResult<SolutionDto> GetSolutionInfo(string solutionPath)
        {
            try
            {
                var solutionInfo = _solutionIntrospector.GetSolutionInfo(solutionPath);
                var solutionDto = MapToSolutionDto(solutionInfo); // Assuming a mapper method is implemented.
                return Ok(solutionDto);
            }
            catch (Exception ex)
            {
                // Handle exception, log if necessary, and return an appropriate error response.
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
        public ActionResult<List<ProjectDto>> ListProjects(string solutionPath)
        {
            try
            {
                var projects = _solutionIntrospector.ListProjects(solutionPath);
                var projectDtos = projects.Select(p => new ProjectDto
                {
                    Name = p.Name
                    // Map additional needed project properties...
                }).ToList();
                return Ok(projectDtos);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetProjectInfo")]
        public ActionResult<ProjectDto> GetProjectInfo(string projectPath)
        {
            try
            {
                var projectInfo = _solutionIntrospector.GetProjectInfo(projectPath);
                var projectInfoDto = new ProjectDto
                {
                    FilePath = projectInfo.FilePath,
                    Name = projectInfo.Name
                    // Map additional needed project properties...
                };
                return Ok(projectInfoDto);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("ListAssemblies")]
        public ActionResult<List<AssemblyDto>> ListAssemblies(string projectPath)
        {
            try
            {
                var assemblies = _solutionIntrospector.ListAssemblies(projectPath);
                var assemblyDtos = assemblies.Select(a => new AssemblyDto
                {
                    Name = a.FullName,
                    Version = a.Location
                    // Map additional needed assembly properties...
                }).ToList();
                return Ok(assemblyDtos);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetAssemblyInfo")]
        public ActionResult<AssemblyDto> GetAssemblyInfo(string assemblyPath)
        {
            try
            {
                var assemblyInfo = _solutionIntrospector.GetAssemblyInfo(assemblyPath);
                var detailedAssemblyInfoDto = new AssemblyDto
                {
                    Name = assemblyInfo.FullName
                    // Map additional needed assembly properties...
                };
                return Ok(detailedAssemblyInfoDto);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("ListNamespaces")]
        public ActionResult<List<string>> ListNamespaces(string assemblyPath)
        {
            try
            {
                var namespaces = _solutionIntrospector.ListNamespaces(assemblyPath);
                return Ok(namespaces);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("ListClasses")]
        public ActionResult<List<TypeDto>> ListClasses(string namespaceName, string assemblyPath)
        {
            try
            {
                var classes = _solutionIntrospector.ListClasses(namespaceName, assemblyPath);
                var classDtos = classes.Select(c => new TypeDto
                {
                    Name = c.Name
                    // Map additional needed type information...
                }).ToList();
                return Ok(classDtos);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetClassInfo")]
        public ActionResult<TypeDto> GetClassInfo(string className, string namespaceName, string assemblyPath)
        {
            try
            {
                var classInfo = _solutionIntrospector.GetClassInfo(className, namespaceName, assemblyPath);
                var detailedTypeInfoDto = new TypeDto
                {
                    Name = classInfo.Name
                    // Map additional needed type information...
                };
                return Ok(detailedTypeInfoDto);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("ListMethods")]
        public ActionResult<List<MethodInfoDto>> ListMethods(string className, string namespaceName, string assemblyPath)
        {
            try
            {
                var methods = _solutionIntrospector.ListMethods(className, namespaceName, assemblyPath);
                var methodInfoDtos = methods.Select(m => new MethodInfoDto
                {
                    Name = m.Name,
                    ReturnType = m.ReturnType.ToString(),
                    Parameters = m.GetParameters().Select(p => new ParameterDto
                    {
                        Name = p.Name,
                        Type = p.ParameterType.FullName // or Name, depending on the desired level of detail.
                    }).ToList()
                }).ToList();
                return Ok(methodInfoDtos);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetMethodSyntaxTree")]
        public ActionResult<MethodSyntaxTreeDto> GetMethodSyntaxTree(string methodName, string className, string namespaceName, string assemblyPath)
        {
            try
            {
                // Assuming _solutionIntrospector.GetMethodSyntaxTree returns a SyntaxTree or IEnumerable<SyntaxTree>
                var syntaxTrees = _solutionIntrospector.GetMethodSyntaxTree(methodName, className, namespaceName, assemblyPath);
                var methodSyntaxTreeDto = syntaxTrees.Select(syntaxTree => new MethodSyntaxTreeDto
                {
                    MethodName = methodName,
                    Root = ConvertSyntaxNode(syntaxTree)
                }).FirstOrDefault(); // Assuming you want the first SyntaxTree for the method.

                return Ok(methodSyntaxTreeDto);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
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

        [HttpGet("ListFields")]
        public ActionResult<List<FieldInfoDto>> ListFields(string className, string namespaceName, string assemblyPath)
        {
            try
            {
                var fields = _solutionIntrospector.ListFields(className, namespaceName, assemblyPath);
                var fieldDtos = fields.Select(field => new FieldInfoDto
                {
                    Name = field.Name,
                    FieldType = field.FieldType.ToString(),
                    // Add any additional transformations for other FieldInfo properties if needed.
                }).ToList();

                return Ok(fieldDtos);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        // DTO for field information
        public class FieldInfoDto
        {
            public string Name { get; set; }
            public string FieldType { get; set; }
            // Other relevant properties from FieldInfo to be included as needed.
        }

        [HttpGet("GetFieldInfo")]
        public ActionResult<FieldInfoDto> GetFieldInfo(string fieldName, string className, string namespaceName, string assemblyPath)
        {
            try
            {
                var fieldInfo = _solutionIntrospector.GetFieldInfo(fieldName, className, namespaceName, assemblyPath);
                var fieldInfoDto = new FieldInfoDto
                {
                    Name = fieldInfo.Name,
                    FieldType = fieldInfo.FieldType.ToString(),
                    // Additional FieldInfo properties can be added here as needed.
                };

                return Ok(fieldInfoDto);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response.
                return StatusCode(500, ex.Message);
            }
        }

        // Assuming FieldInfoDto is already defined from previous implementation.

    }


}
