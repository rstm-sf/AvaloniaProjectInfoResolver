using System.Threading.Tasks;

namespace AvaloniaProjectInfoResolver
{
    public interface IProjectInfoResolver
    {
        Task<AvaloniaProjectInfoResult> ResolvePreviewProjectInfoAsync(string projectFilePath);
    }
}
