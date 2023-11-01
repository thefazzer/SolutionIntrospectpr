using Microsoft.CodeAnalysis;

namespace SolutionIntrospector
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
}
