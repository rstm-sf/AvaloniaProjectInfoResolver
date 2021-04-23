// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Xml.Serialization;
using Microsoft.Build.Framework;

namespace AvaloniaProjectInfoResolver.PreviewTask
{
    // ReSharper disable once UnusedType.Global
    public class PreviewInfoResolverTask : ContextAwareTask
    {
        // ReSharper disable InconsistentNaming
        private const string SelectInfoAvaloniaResource = nameof(SelectInfoAvaloniaResource);
        private const string SelectInfoAvaloniaXaml = nameof(SelectInfoAvaloniaXaml);
        private const string SelectInfoAvaloniaPreviewerNetCoreToolPath =
            nameof(SelectInfoAvaloniaPreviewerNetCoreToolPath);
        private const string SelectInfoAvaloniaPreviewerNetFullToolPath =
            nameof(SelectInfoAvaloniaPreviewerNetFullToolPath);
        private const string SelectInfoTargetFrameworks = nameof(SelectInfoTargetFrameworks);
        private const string SelectInfoTargetPath = nameof(SelectInfoTargetPath);
        private const string SelectInfoProjectRuntimeConfigFilePath = nameof(SelectInfoProjectRuntimeConfigFilePath);
        private const string SelectInfoProjectDepsFilePath = nameof(SelectInfoProjectDepsFilePath);
        private const string SelectInfoTargetFramework = nameof(SelectInfoTargetFramework);
        private const string SelectInfoTargetFrameworkIdentifier = nameof(SelectInfoTargetFrameworkIdentifier);
        // ReSharper restore InconsistentNaming

        private static readonly string[] TargetNamesCommon =
        {
            "Restore",
            SelectInfoAvaloniaResource,
            SelectInfoAvaloniaXaml,
            SelectInfoAvaloniaPreviewerNetCoreToolPath,
            SelectInfoAvaloniaPreviewerNetFullToolPath,
        };

        private static readonly string[] TargetNamesByTfm =
        {
            SelectInfoTargetPath,
            SelectInfoProjectRuntimeConfigFilePath,
            SelectInfoProjectDepsFilePath,
            SelectInfoTargetFramework,
            SelectInfoTargetFrameworkIdentifier,
        };

        private static readonly string[] TargetGetTfms = {SelectInfoTargetFrameworks};

        private readonly Dictionary<string, string> _globalPropertiesCommon;

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string? ParentId { get; set; }

        [Required] public string ProjectFile { get; set; } = null!;

        public PreviewInfoResolverTask()
        {
            _globalPropertiesCommon = new Dictionary<string, string>
            {
                {
                    "CustomBeforeMicrosoftCommonProps",
                    // ReSharper disable once VirtualMemberCallInConstructor
                    Path.Combine(ManagedDllDirectory, "AvaloniaProjectInfoResolver.PreviewInner.targets")
                },
                {"DesignTimeBuild", "true"},
                {"BuildingInsideVisualStudio", "false"},
                {"BuildProjectReferences", "false"},
                {"SkipCompilerExecution", "true"},
                {"ProvideCommandLineArgs", "true"},
                {"DisableRarCache", "false"},
                {"AutoGenerateBindingRedirects", "false"},
                {"CopyBuildOutputToOutputDirectory", "false"},
                {"CopyOutputSymbolsToOutputDirectory", "false"},
                {"SkipCopyBuildProduct", "true"},
                {"AddModules", "false"},
                {"UseCommonOutputDirectory", "true"},
                {"GeneratePackageOnBuild", "false"},
                {"ResolveNuGetPackages", "true"}
            };
        }

        protected override bool ExecuteInner()
        {
            var isSuccess = TryResolveProjectInfoTfms(out var targetFrameworks);
            if (!isSuccess)
                return false;

            isSuccess = TryResolveProjectInfoCommon(out var projectInfo, targetFrameworks);
            if (!isSuccess)
                return false;

            if (!IsReferencesAvalonia(projectInfo))
                return false;

            var projectInfoTfmCollection = new List<ProjectInfoByTfm>(targetFrameworks.Length);
            foreach (var tfm in targetFrameworks)
            {
                isSuccess = TryResolveProjectInfoByTfm(out var projectInfoTfm, tfm);
                if (!isSuccess)
                    return false;
                projectInfoTfmCollection.Add(projectInfoTfm);
            }

            projectInfo.ProjectInfoByTfmArray = projectInfoTfmCollection.ToArray();

            SendMessage(projectInfo);
            return true;
        }

        private bool IsReferencesAvalonia(ProjectInfo projectInfo)
        {
            var isPreviewContains = !string.IsNullOrEmpty(projectInfo.AvaloniaPreviewerNetCoreToolPath)
                                 || !string.IsNullOrEmpty(projectInfo.AvaloniaPreviewerNetFullToolPath);
            if (isPreviewContains)
                return true;

            BuildEngine.LogErrorEvent(new BuildErrorEventArgs(
                "APIR",
                string.Empty,
                ProjectFile,
                0,
                0,
                0,
                0,
                "MSBuild project file does not reference AvaloniaUI",
                string.Empty,
                string.Empty));

            return false;
        }

        private bool TryResolveProjectInfoTfms(out string[] targetFrameworks)
        {
            targetFrameworks = default!;
            var targetOutputs = new Dictionary<string, ITaskItem[]>();

            var isSuccess = BuildEngine.BuildProjectFile(
                ProjectFile, TargetGetTfms, _globalPropertiesCommon, targetOutputs);
            if (!isSuccess)
                return false;

            targetFrameworks = targetOutputs.ResultFromArray(SelectInfoTargetFrameworks);

            return true;
        }

        private bool TryResolveProjectInfoCommon(out ProjectInfo projectInfo, string[] targetFrameworks)
        {
            projectInfo = default!;
            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFrameworks[0]);

            var isSuccess = BuildEngine.BuildProjectFile(ProjectFile, TargetNamesCommon, props, targetOutputs);
            if (!isSuccess)
                return false;

            projectInfo = SelectProjectInfoCommon(targetOutputs);

            return true;
        }

        private bool TryResolveProjectInfoByTfm(out ProjectInfoByTfm projectInfoByTfm, string targetFramework = "")
        {
            projectInfoByTfm = default!;
            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFramework);

            var isSuccess = BuildEngine.BuildProjectFile(ProjectFile, TargetNamesByTfm, props, targetOutputs);
            if (!isSuccess)
                return false;

            projectInfoByTfm = SelectProjectInfoByTfm(targetOutputs);

            return true;
        }

        private Dictionary<string, string> GetGlobalProperties(string targetFramework) =>
            string.IsNullOrEmpty(targetFramework)
                ? _globalPropertiesCommon
                : new Dictionary<string, string>(_globalPropertiesCommon) {{"TargetFramework", targetFramework}};

        private void SendMessage(ProjectInfo projectInfo)
        {
            var message = SerializeToXml(projectInfo);
            if (string.IsNullOrWhiteSpace(ParentId))
            {
                Log.LogMessage(MessageImportance.High, message);
            }
            else
            {
                var sender = new AnonymousPipeClientStream(PipeDirection.Out, ParentId!);
                using var writer = new StreamWriter(sender);
                writer.WriteLine(message);
                writer.Flush();
            }
        }

        // ReSharper disable once InconsistentNaming
        private static ProjectInfo SelectProjectInfoCommon(Dictionary<string, ITaskItem[]> targetOutputs) =>
            new()
            {
                AvaloniaPreviewerNetCoreToolPath = targetOutputs
                    .ResultFromSingle(SelectInfoAvaloniaPreviewerNetCoreToolPath),
                AvaloniaPreviewerNetFullToolPath = targetOutputs
                    .ResultFromSingle(SelectInfoAvaloniaPreviewerNetFullToolPath),
                XamlFileInfo =
                {
                    AvaloniaResource = targetOutputs.ResultFromArrayAsSingleSkipNonXaml(SelectInfoAvaloniaResource),
                    AvaloniaXaml = targetOutputs.ResultFromArrayAsSingle(SelectInfoAvaloniaXaml),
                }
            };

        // ReSharper disable once InconsistentNaming
        private static ProjectInfoByTfm SelectProjectInfoByTfm(Dictionary<string, ITaskItem[]> targetOutputs) =>
            new()
            {
                TargetFramework = targetOutputs.ResultFromSingle(SelectInfoTargetFramework),
                TargetFrameworkIdentifier = targetOutputs.ResultFromSingle(SelectInfoTargetFrameworkIdentifier),
                TargetPath = targetOutputs.ResultFromSingle(SelectInfoTargetPath),
                ProjectDepsFilePath = targetOutputs.ResultFromSingle(SelectInfoProjectDepsFilePath),
                ProjectRuntimeConfigFilePath = targetOutputs.ResultFromSingle(SelectInfoProjectRuntimeConfigFilePath),
            };

        private static string SerializeToXml(ProjectInfo data)
        {
            using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);

            var serializer = new XmlSerializer(data.GetType());
            serializer.Serialize(stringWriter, data);

            return stringWriter.ToString();
        }
    }
}
