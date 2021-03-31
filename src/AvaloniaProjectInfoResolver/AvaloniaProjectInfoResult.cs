namespace AvaloniaProjectInfoResolver
{
    public class AvaloniaProjectInfoResult
    {
        public ProjectInfo? ProjectInfo { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        public string Error { get; }

        public bool HasError => Error.Length > 0;

        public AvaloniaProjectInfoResult(ProjectInfo? projectInfo, string error)
        {
            ProjectInfo = projectInfo;
            Error = error;
        }
    }
}
