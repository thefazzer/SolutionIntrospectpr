// DTOs.cs

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace DotNetAnalyzerPro.DTO
{
    public class SolutionDto
    {
        public string FilePath { get; set; }
        public List<ProjectDto> Projects { get; set; }

    }

    public class ProjectDto
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public List<string> TargetFrameworks { get; set; }
    }

    public class AssemblyDto
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Culture { get; set; }
        public string PublicKeyToken { get; set; }
    }

    public class NamespaceDto
    {
        public string Name { get; set; }
    }

    public class TypeDto
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string AssemblyName { get; set; }
    }

    public class MethodInfoDto
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<ParameterDto> Parameters { get; set; }
    }

    public class ParameterDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class FieldInfoDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    public class SyntaxNodeDto
    {
        public string Kind { get; set; }
        public string Text { get; set; }
        public List<SyntaxNodeDto> ChildNodes { get; set; }
    }

    public class MethodSyntaxTreeDto
    {
        public string MethodName { get; set; }
        public SyntaxNodeDto Root { get; set; }
    }
}
