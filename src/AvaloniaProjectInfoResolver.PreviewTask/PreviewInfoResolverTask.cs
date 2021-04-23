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
        private const string SelectInfoProjectReference = nameof(SelectInfoProjectReference);
        private const string SelectInfoProjectPath = nameof(SelectInfoProjectPath);
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
            SelectInfoProjectPath,
            SelectInfoAvaloniaResource,
            SelectInfoAvaloniaXaml,
            SelectInfoAvaloniaPreviewerNetCoreToolPath,
            SelectInfoAvaloniaPreviewerNetFullToolPath,
        };

        private static readonly string[] TargetNamesAppExecInfo =
        {
            SelectInfoTargetPath,
            SelectInfoProjectRuntimeConfigFilePath,
            SelectInfoProjectDepsFilePath,
            SelectInfoTargetFramework,
            SelectInfoTargetFrameworkIdentifier,
        };

        private static readonly string[] TargetGetProjectReference = {SelectInfoProjectReference};

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
            var isSuccess = TryResolvePreviewInfoTfms(out var targetFrameworks);
            if (!isSuccess)
                return false;

            isSuccess = TryResolvePreviewInfoCommon(out var previewInfo, targetFrameworks);
            if (!isSuccess)
                return false;

            var appExecInfoCollection = new List<AppExecInfo>(targetFrameworks.Length);
            foreach (var tfm in targetFrameworks)
            {
                isSuccess = TryResolveAppExecInfo(out var appExecInfo, tfm);
                if (!isSuccess)
                    return false;
                appExecInfoCollection.Add(appExecInfo);
            }
            previewInfo.AppExecInfoCollection = appExecInfoCollection;

            SendMessage(previewInfo);
            return true;
        }

        private bool IsReferencesAvalonia(PreviewInfo previewInfo)
        {
            var isPreviewContains = !string.IsNullOrEmpty(previewInfo.AvaloniaPreviewerNetCoreToolPath)
                                 || !string.IsNullOrEmpty(previewInfo.AvaloniaPreviewerNetFullToolPath);
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

        private bool TryResolvePreviewInfoTfms(out string[] targetFrameworks)
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

        private bool TryResolvePreviewInfoCommon(out PreviewInfo previewInfo, string[] targetFrameworks)
        {
            previewInfo = default!;
            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFrameworks[0]);

            var isSuccess = BuildEngine.BuildProjectFile(ProjectFile, TargetNamesCommon, props, targetOutputs);
            if (!isSuccess)
                return false;

            previewInfo = SelectPreviewInfoCommon(targetOutputs);
            if (!IsReferencesAvalonia(previewInfo))
            {
                previewInfo = default!;
                return false;
            }

            isSuccess = BuildEngine.BuildProjectFile(ProjectFile, TargetGetProjectReference, props, targetOutputs);
            if (isSuccess)
            {
                var currentProjectDirectory = Directory.GetParent(previewInfo.XamlFileInfo.ProjectPath).FullName;

                var projectReferenceCollection = targetOutputs.ResultFromArray(SelectInfoProjectReference);
                var xamlFileInfoCollection = new List<XamlFileInfo>(projectReferenceCollection.Length);
                foreach (var project in projectReferenceCollection)
                {
                    var path = Path.Combine(currentProjectDirectory, project);
                    isSuccess = TryResolvePreviewInfoCommonProjectReference(path, out var xamlFileInfo);
                    if (isSuccess)
                        xamlFileInfoCollection.Add(xamlFileInfo);
                }

                previewInfo.XamlFileInfo.ReferenceXamlFileInfoCollection = xamlFileInfoCollection;
            }

            return true;
        }

        private bool TryResolvePreviewInfoCommonProjectReference(string projectReference, out XamlFileInfo xamlFileInfo)
        {
            xamlFileInfo = default!;

            var isSuccess = TryResolvePreviewInfoTfms(out var targetFrameworks);
            if (!isSuccess)
                return false;

            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFrameworks[0]);

            isSuccess = BuildEngine.BuildProjectFile(projectReference, TargetNamesCommon, props, targetOutputs);
            if (!isSuccess)
                return false;

            xamlFileInfo = new XamlFileInfo
            {
                ProjectPath = targetOutputs.ResultFromSingle(SelectInfoProjectPath),
                AvaloniaResource = targetOutputs.ResultFromArrayAsSingleSkipNonXaml(SelectInfoAvaloniaResource),
                AvaloniaXaml = targetOutputs.ResultFromArrayAsSingle(SelectInfoAvaloniaXaml),
            };

            return true;
        }

        private bool TryResolveAppExecInfo(out AppExecInfo appExecInfo, string targetFramework = "")
        {
            appExecInfo = default!;
            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFramework);

            var isSuccess = BuildEngine.BuildProjectFile(ProjectFile, TargetNamesAppExecInfo, props, targetOutputs);
            if (!isSuccess)
                return false;

            appExecInfo = SelectAppExecInfo(targetOutputs);

            return true;
        }

        private Dictionary<string, string> GetGlobalProperties(string targetFramework) =>
            string.IsNullOrEmpty(targetFramework)
                ? _globalPropertiesCommon
                : new Dictionary<string, string>(_globalPropertiesCommon) {{"TargetFramework", targetFramework}};

        private void SendMessage(PreviewInfo previewInfo)
        {
            var message = SerializeToXml(previewInfo);
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
        private static PreviewInfo SelectPreviewInfoCommon(Dictionary<string, ITaskItem[]> targetOutputs) =>
            new()
            {
                AvaloniaPreviewerNetCoreToolPath = targetOutputs
                    .ResultFromSingle(SelectInfoAvaloniaPreviewerNetCoreToolPath),
                AvaloniaPreviewerNetFullToolPath = targetOutputs
                    .ResultFromSingle(SelectInfoAvaloniaPreviewerNetFullToolPath),
                XamlFileInfo =
                {
                    ProjectPath = targetOutputs.ResultFromSingle(SelectInfoProjectPath),
                    AvaloniaResource = targetOutputs.ResultFromArrayAsSingleSkipNonXaml(SelectInfoAvaloniaResource),
                    AvaloniaXaml = targetOutputs.ResultFromArrayAsSingle(SelectInfoAvaloniaXaml),
                }
            };

        // ReSharper disable once InconsistentNaming
        private static AppExecInfo SelectAppExecInfo(Dictionary<string, ITaskItem[]> targetOutputs) =>
            new()
            {
                TargetFramework = targetOutputs.ResultFromSingle(SelectInfoTargetFramework),
                TargetFrameworkIdentifier = targetOutputs.ResultFromSingle(SelectInfoTargetFrameworkIdentifier),
                TargetPath = targetOutputs.ResultFromSingle(SelectInfoTargetPath),
                ProjectDepsFilePath = targetOutputs.ResultFromSingle(SelectInfoProjectDepsFilePath),
                ProjectRuntimeConfigFilePath = targetOutputs.ResultFromSingle(SelectInfoProjectRuntimeConfigFilePath),
            };

        private static string SerializeToXml(PreviewInfo data)
        {
            using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);

            var serializer = new XmlSerializer(data.GetType());
            serializer.Serialize(stringWriter, data);

            return stringWriter.ToString();
        }
    }
}
