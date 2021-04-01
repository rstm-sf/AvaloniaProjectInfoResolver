namespace AvaloniaProjectInfoResolver
{
    public class AvaloniaProjectInfoResult
    {
        public ProjectInfo? ProjectInfo { get; }

        public string Error { get; }

        public bool HasError => Error.Length > 0;

        public AvaloniaProjectInfoResult(ProjectInfo? projectInfo, string error)
        {
            ProjectInfo = projectInfo;
            Error = error;
        }
    }
}
