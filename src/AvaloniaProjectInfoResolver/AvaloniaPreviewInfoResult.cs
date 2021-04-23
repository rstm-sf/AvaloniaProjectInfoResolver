namespace AvaloniaProjectInfoResolver
{
    public class AvaloniaPreviewInfoResult
    {
        public PreviewInfo? PreviewInfo { get; }

        public string Error { get; }

        public bool HasError => Error.Length > 0;

        public AvaloniaPreviewInfoResult(PreviewInfo? previewInfo, string error)
        {
            PreviewInfo = previewInfo;
            Error = error;
        }
    }
}
