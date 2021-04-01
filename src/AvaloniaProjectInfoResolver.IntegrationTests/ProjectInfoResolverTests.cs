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
            Assert.Equal(string.Empty, info.AvaloniaResource);
            Assert.False(string.IsNullOrEmpty(info.AvaloniaXaml));
            Assert.Equal(string.Empty, info.TargetFrameworks);

            var infoByTfm = info.ProjectInfoByTfmArray;
            Assert.Single(infoByTfm);
            Assert.Equal("netcoreapp3.1", infoByTfm[0].TargetFramework);
            Assert.False(string.IsNullOrEmpty(infoByTfm[0].TargetPath));
            Assert.Equal(".NETCoreApp", infoByTfm[0].TargetFrameworkIdentifier);
            Assert.False(string.IsNullOrEmpty(infoByTfm[0].ProjectDepsFilePath));
            Assert.False(string.IsNullOrEmpty(infoByTfm[0].ProjectRuntimeConfigFilePath));
        }

        [Fact]
        public async Task Should_ResolvePreviewProjectInfoAsync_TaskDebug_Not_References_Avalonia()
        {
            var projectInfoResolver = new ProjectInfoResolver();
            var projPath = "../../../../../AvaloniaProjectInfoResolver/AvaloniaProjectInfoResolver.csproj";

            var result = await projectInfoResolver.ResolvePreviewProjectInfoAsync(projPath);

            Assert.True(result.HasError);
            Assert.Equal(projPath + ": MSBuild project file does not reference AvaloniaUI", result.Error);
            Assert.Null(result.ProjectInfo);
        }
    }
}
