using System.Threading.Tasks;
using Xunit;

namespace AvaloniaProjectInfoResolver.IntegrationTests
{
    public class ProjectInfoResolverTests
    {
        [Fact]
        public async Task Should_ResolvePreviewProjectInfoAsync_App_References_Avalonia()
        {
            var projectInfoResolver = new ProjectInfoResolver();
            var projPath = "../../../../../AvaloniaProjectInfoResolver.App/AvaloniaProjectInfoResolver.App.csproj";

            var result = await projectInfoResolver.ResolvePreviewProjectInfoAsync(projPath);

            Assert.False(result.HasError);
            Assert.Equal(result.Error, string.Empty);

            var info = result.ProjectInfo!;
            Assert.NotNull(info);
            Assert.False(string.IsNullOrEmpty(info.AvaloniaPreviewerNetCoreToolPath));
            Assert.False(string.IsNullOrEmpty(info.AvaloniaPreviewerNetFullToolPath));
            Assert.Equal(info.AvaloniaResource, string.Empty);
            Assert.False(string.IsNullOrEmpty(info.AvaloniaXaml));
            Assert.Equal(info.TargetFrameworks, string.Empty);

            var infoByTfm = info.ProjectInfoByTfmArray;
            Assert.Equal(infoByTfm.Length, 1);
            Assert.Equal(infoByTfm[0].TargetFramework, "net5.0");
            Assert.False(string.IsNullOrEmpty(infoByTfm[0].TargetPath));
            Assert.Equal(infoByTfm[0].TargetFrameworkIdentifier, ".NETCoreApp");
            Assert.False(string.IsNullOrEmpty(infoByTfm[0].ProjectDepsFilePath));
            Assert.False(string.IsNullOrEmpty(infoByTfm[0].ProjectRuntimeConfigFilePath));
        }

        [Fact]
        public async Task Should_ResolvePreviewProjectInfoAsync_TaskDebug_Not_References_Avalonia()
        {
            var projectInfoResolver = new ProjectInfoResolver();
            var projPath = "../../../../../AvaloniaProjectInfoResolver.PreviewTask.Debug/AvaloniaProjectInfoResolver.PreviewTask.Debug.csproj";

            var result = await projectInfoResolver.ResolvePreviewProjectInfoAsync(projPath);

            Assert.True(result.HasError);
            Assert.Equal(result.Error, projPath + ": MSBuild project file does not reference AvaloniaUI");
            Assert.Null(result.ProjectInfo);
        }
    }
}
