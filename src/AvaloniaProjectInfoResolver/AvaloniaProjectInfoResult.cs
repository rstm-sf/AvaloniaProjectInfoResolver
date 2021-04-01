namespace AvaloniaProjectInfoResolver
{
    public class AvaloniaProjectInfoResult
    {
        public ProjectInfo? ProjectInfo { get; }

        public string Errors { get; }

        public bool HasError => Errors.Length > 0;

        public AvaloniaProjectInfoResult(ProjectInfo? projectInfo, string errors)
        {
            ProjectInfo = projectInfo;
            Errors = errors;
        }
    }
}
