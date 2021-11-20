using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaProjectInfoResolver
{
    public interface IProjectInfoResolver
    {
        Task<AvaloniaPreviewInfoResult> ResolvePreviewInfoAsync(
            string projectFilePath, CancellationToken cancellationToken = default);
    }
}
