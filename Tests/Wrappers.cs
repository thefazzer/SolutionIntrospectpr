using Microsoft.CodeAnalysis;

namespace DotNetAnalyzerPro
{
    public class SolutionWrapper
    {
        public Solution ActualSolution { get; set; }

        public SolutionWrapper() { }

        public SolutionWrapper(Solution solution)
        {
            ActualSolution = solution;
        }

        public IEnumerable<Project> Projects => ActualSolution.Projects;

        // Add other methods and properties to expose from Solution
    }

    public class ProjectWrapper
    {
        public Project ActualProject{ get; set; }

        public ProjectWrapper() { }

        public ProjectWrapper(Project project)
        {
            ActualProject = project;
        }

        // Add other methods and properties to expose from Solution
    }
}
