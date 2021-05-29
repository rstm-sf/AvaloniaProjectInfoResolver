using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AvaloniaProjectInfoResolver.IntegrationTests
{
    public class ProjectInfoResolverTests
    {
        private const string AvaloniaAppProjPath = "../../../../../AvaloniaProjectInfoResolver.App/AvaloniaProjectInfoResolver.App.csproj";

        [Fact]
        public async Task Should_ResolvePreviewProjectInfoAsync_App_References_Avalonia()
        {
            var projectInfoResolver = new ProjectInfoResolver();

            var result = await projectInfoResolver.ResolvePreviewInfoAsync(AvaloniaAppProjPath);

            Assert.False(result.HasError);
            Assert.Equal(result.Error, string.Empty);

            var info = result.PreviewInfo!;
            Assert.NotNull(info);
            Assert.False(string.IsNullOrEmpty(info.AvaloniaPreviewerNetCoreToolPath));
            Assert.False(string.IsNullOrEmpty(info.AvaloniaPreviewerNetFullToolPath));
            Assert.Equal(string.Empty, info.XamlFileInfo.AvaloniaResource);
            Assert.False(string.IsNullOrEmpty(info.XamlFileInfo.AvaloniaXaml));

            var appExecInfoCollection = info.AppExecInfoCollection;
            Assert.Single(appExecInfoCollection);
            Assert.Equal("netcoreapp3.1", appExecInfoCollection[0].TargetFramework);
            Assert.False(string.IsNullOrEmpty(appExecInfoCollection[0].TargetPath));
            Assert.Equal(".NETCoreApp", appExecInfoCollection[0].TargetFrameworkIdentifier);
            Assert.False(string.IsNullOrEmpty(appExecInfoCollection[0].ProjectDepsFilePath));
            Assert.False(string.IsNullOrEmpty(appExecInfoCollection[0].ProjectRuntimeConfigFilePath));
        }

        [Fact]
        public async Task Should_ResolvePreviewProjectInfoAsync_App_References_Avalonia_Cancellation()
        {
            var projectInfoResolver = new ProjectInfoResolver();
            using var cancellationTokenSource = new CancellationTokenSource(100);

            var result = await projectInfoResolver.ResolvePreviewInfoAsync(
                AvaloniaAppProjPath, cancellationTokenSource.Token);

            Assert.False(result.HasError);
            Assert.Equal(result.Error, string.Empty);
            Assert.Null(result.PreviewInfo);
        }

        [Fact]
        public async Task Should_ResolvePreviewProjectInfoAsync_TaskDebug_Not_References_Avalonia()
        {
            var projectInfoResolver = new ProjectInfoResolver();
            var projPath = "./data/ConsoleApp1/ConsoleApp1.fsproj";
            projPath = new FileInfo(projPath).FullName;

            var result = await projectInfoResolver.ResolvePreviewInfoAsync(projPath);

            Assert.True(result.HasError);
            Assert.Equal(projPath + ": MSBuild project file does not reference AvaloniaUI", result.Error);
            Assert.Null(result.PreviewInfo);
        }
    }
}
