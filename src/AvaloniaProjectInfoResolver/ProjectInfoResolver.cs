using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AvaloniaProjectInfoResolver
{
    public class ProjectInfoResolver
    {
        private static readonly string SelfDirectoryPath =
            Path.GetDirectoryName(typeof(ProjectInfoResolver).Assembly.Location)!;

        public async Task<ProjectInfo?> ResolvePreviewProjectInfoAsync(string projectFilePath)
        {
            var receiver = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

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
                    " -t:RunAvaloniaPreviewProjectInfoResolverTask",
                    " -p:AvaloniaPreviewParentId=",
                    receiver.GetClientHandleAsString(),
                    " -p:AvaloniaProjectFilePath=",
                    projectFilePath)
            };
            Process.Start(startInfo);

            receiver.DisposeLocalCopyOfClientHandle();

            var result = await ResolvePreviewProjectInfoAsync(receiver).ConfigureAwait(false);
            return result;
        }

        private static async Task<ProjectInfo?> ResolvePreviewProjectInfoAsync(AnonymousPipeServerStream receiver)
        {
            string line;
            var xmlData = string.Empty;
            using var reader = new StreamReader(receiver);

            while ((line = await reader.ReadLineAsync()) != null)
                xmlData += line;

            return string.IsNullOrEmpty(xmlData) ? null : DeserializeFromXml(xmlData);
        }

        private static ProjectInfo DeserializeFromXml(string xmlData)
        {
            var deserializer = new XmlSerializer(typeof(ProjectInfo));
            using var stringReader = new StringReader(xmlData);

            return (ProjectInfo)deserializer.Deserialize(stringReader);
        }
    }
}
