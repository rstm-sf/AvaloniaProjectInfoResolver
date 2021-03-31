using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AvaloniaProjectInfoResolver
{
    public class ProjectInfoResolver : IProjectInfoResolver
    {
        private static readonly string SelfDirectoryPath =
            Path.GetDirectoryName(typeof(ProjectInfoResolver).Assembly.Location)!;

        public async Task<AvaloniaProjectInfoResult> ResolvePreviewProjectInfoAsync(string projectFilePath)
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
            Process.Start(startInfo);

            receiverTask.DisposeLocalCopyOfClientHandle();
            receiverLogger.DisposeLocalCopyOfClientHandle();

            var info = await ResolvePreviewProjectInfoAsync(receiverTask).ConfigureAwait(false);
            var error = await ResolveLoggerProjectInfoAsync(receiverLogger).ConfigureAwait(false);

            return new AvaloniaProjectInfoResult(info, error);
        }

        private static async Task<ProjectInfo?> ResolvePreviewProjectInfoAsync(AnonymousPipeServerStream receiver)
        {
            string line;
            var xmlData = string.Empty;
            using var reader = new StreamReader(receiver);

            while ((line = await reader.ReadLineAsync()) != null)
                xmlData += line;

            return string.IsNullOrEmpty(xmlData)
                ? null
                : (ProjectInfo)DeserializeFromXml(xmlData, typeof(ProjectInfo));
        }

        private static async Task<string> ResolveLoggerProjectInfoAsync(AnonymousPipeServerStream receiver)
        {
            string line;
            var xmlData = string.Empty;
            using var reader = new StreamReader(receiver);

            while ((line = await reader.ReadLineAsync()) != null)
                xmlData += line;

            if (string.IsNullOrEmpty(xmlData))
                return String.Empty;

            var errorArgs = (ProjectInfoErrorArgs)DeserializeFromXml(xmlData, typeof(ProjectInfoErrorArgs));
            return errorArgs.Subcategory == "APIR" ? errorArgs.Message : errorArgs.ToString();
        }

        private static object DeserializeFromXml(string xmlData, Type type)
        {
            var deserializer = new XmlSerializer(type);
            using var stringReader = new StringReader(xmlData);

            return deserializer.Deserialize(stringReader);
        }
    }
}
