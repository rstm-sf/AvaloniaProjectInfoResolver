using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AvaloniaProjectInfoResolver
{
    public class ProjectInfoResolver : IProjectInfoResolver
    {
        private static readonly string SelfDirectoryPath =
            Path.GetDirectoryName(typeof(ProjectInfoResolver).Assembly.Location)!;

        public async Task<AvaloniaProjectInfoResult> ResolvePreviewProjectInfoAsync(
            string projectFilePath, CancellationToken cancellationToken = default)
        {
            var receiverTask = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            var receiverLogger = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                Arguments = string.Concat(
                    "msbuild ",
                    SelfDirectoryPath,
                    "/AvaloniaProjectInfoResolver.Core/AvaloniaPreviewInfoResolver.proj -noLogo",
#if !DEBUG
                    " -noConsoleLogger",
#endif
                    " /logger:ProjectInfoMsBuildLogger,",
                    SelfDirectoryPath,
                    "/AvaloniaProjectInfoResolver.Core/AvaloniaProjectInfoResolver.MSBuildLogger.dll;",
                    receiverLogger.GetClientHandleAsString(),
                    " -p:AvaloniaPreviewParentId=",
                    receiverTask.GetClientHandleAsString(),
                    " -p:AvaloniaProjectFilePath=",
                    projectFilePath)
            };

            var proc = Process.Start(startInfo);
            receiverTask.DisposeLocalCopyOfClientHandle();
            receiverLogger.DisposeLocalCopyOfClientHandle();

            if (cancellationToken.IsCancellationRequested)
            {
                proc?.Kill();
                return new AvaloniaProjectInfoResult(null, string.Empty);
            }

            cancellationToken.Register(() => proc?.Kill());

            var info = await ResolvePreviewProjectInfoAsync(receiverTask).ConfigureAwait(false);
            var error = await ResolveLoggerProjectInfoAsync(receiverLogger).ConfigureAwait(false);

            return new AvaloniaProjectInfoResult(
                cancellationToken.IsCancellationRequested ? null : info,
                error);
        }

        private static async Task<ProjectInfo?> ResolvePreviewProjectInfoAsync(AnonymousPipeServerStream receiver)
        {
            string line;
            var sb = new StringBuilder();
            using var reader = new StreamReader(receiver);

            while ((line = await reader.ReadLineAsync()) != null)
                sb.Append(line);

            var xmlData = sb.ToString();
            return string.IsNullOrEmpty(xmlData)
                ? null
                : (ProjectInfo)DeserializeFromXml(xmlData, typeof(ProjectInfo));
        }

        private static async Task<string> ResolveLoggerProjectInfoAsync(AnonymousPipeServerStream receiver)
        {
            using var reader = new StreamReader(receiver);
            var error = await reader.ReadLineAsync();
            return error ?? string.Empty;
        }

        private static object DeserializeFromXml(string xmlData, Type type)
        {
            var deserializer = new XmlSerializer(type);
            using var stringReader = new StringReader(xmlData);

            return deserializer.Deserialize(stringReader);
        }
    }
}
