// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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
        private const string SelectInfoOutputType = nameof(SelectInfoOutputType);
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

        private static readonly string[] TargetGetOutput = {SelectInfoOutputType};

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
            if (!TryResolvePreviewInfoOutputType(ProjectFile, out var outputType))
                return false;

            if (outputType == "Library")
                return false;

            if (!TryResolvePreviewInfoTfms(ProjectFile, out var targetFrameworks))
                return false;

            if (!TryResolvePreviewInfoCommon(out var previewInfo, targetFrameworks))
                return false;

            var appExecInfoCollection = new List<AppExecInfo>(targetFrameworks.Length);
            foreach (var tfm in targetFrameworks)
            {
                if (!TryResolveAppExecInfo(out var appExecInfo, tfm))
                    return false;
                appExecInfoCollection.Add(appExecInfo);
            }
            previewInfo.AppExecInfoCollection = appExecInfoCollection;

            SendMessage(previewInfo);
            return true;
        }

        private bool TryResolvePreviewInfoOutputType(string projectFile, out string outputType)
        {
            var targetOutputs = new Dictionary<string, ITaskItem[]>();

            if (BuildEngine.BuildProjectFile(projectFile, TargetGetOutput, _globalPropertiesCommon, targetOutputs))
            {
                outputType = targetOutputs.ResultFromSingle(SelectInfoOutputType);
                return true;
            }

            outputType = default!;
            return false;
        }

        private bool TryResolvePreviewInfoTfms(string projectFile, out string[] targetFrameworks)
        {
            var targetOutputs = new Dictionary<string, ITaskItem[]>();

            if (BuildEngine.BuildProjectFile(projectFile, TargetGetTfms, _globalPropertiesCommon, targetOutputs))
            {
                targetFrameworks = targetOutputs.ResultFromArray(SelectInfoTargetFrameworks);
                return true;
            }

            targetFrameworks = default!;
            return false;
        }

        private bool TryResolvePreviewInfoCommon(out PreviewInfo previewInfo, string[] targetFrameworks)
        {
            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFrameworks[0]);

            if (!BuildEngine.BuildProjectFile(ProjectFile, TargetNamesCommon, props, targetOutputs))
            {
                previewInfo = default!;
                return false;
            }

            previewInfo = SelectPreviewInfoCommon(targetOutputs);
            if (IsReferencesAvalonia(previewInfo))
            {
                var xamlFileInfoCollection = ResolveReferenceXamlFileInfoCollection(
                    previewInfo.XamlFileInfo.ProjectPath, props, new List<string>());
                previewInfo.XamlFileInfo.ReferenceXamlFileInfoCollection = xamlFileInfoCollection;

                return true;
            }

            LogProjectNotAvalonia();

            previewInfo = default!;
            return false;
        }

        private List<XamlFileInfo> ResolveReferenceXamlFileInfoCollection(
            string parentProjectPath, Dictionary<string, string> props, IReadOnlyList<string> projectReadies)
        {
            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var xamlFileInfoCollection = new List<XamlFileInfo>();
            if (BuildEngine.BuildProjectFile(parentProjectPath, TargetGetProjectReference, props, targetOutputs))
            {
                var currentProjectDirectory = Directory.GetParent(parentProjectPath)!.FullName;

                var projectReferenceCollection = targetOutputs.ResultFromArray(SelectInfoProjectReference);
                if (string.IsNullOrEmpty(projectReferenceCollection[0]))
                    return xamlFileInfoCollection;

                var projectReadiesInner = projectReadies.ToList();
                foreach (var project in projectReferenceCollection)
                {
                    var path = Path.Combine(currentProjectDirectory, project);
                    if (TryResolvePreviewInfoCommonProjectReference(path, projectReadiesInner, out var xamlFileInfo))
                    {
                        xamlFileInfoCollection.Add(new XamlFileInfo
                        {
                            ProjectPath = xamlFileInfo.ProjectPath,
                            AvaloniaResource = xamlFileInfo.AvaloniaResource,
                            AvaloniaXaml = xamlFileInfo.AvaloniaXaml,
                        });
                        xamlFileInfoCollection.AddRange(xamlFileInfo.ReferenceXamlFileInfoCollection);
                        
                        projectReadiesInner.Add(xamlFileInfo.ProjectPath);
                        projectReadiesInner.AddRange(xamlFileInfo.ReferenceXamlFileInfoCollection.Select(x => x.ProjectPath));
                    }
                }
            }

            return xamlFileInfoCollection;
        }

        private bool TryResolvePreviewInfoCommonProjectReference(
            string projectReference, IReadOnlyList<string> readies, out XamlFileInfo xamlFileInfo)
        {
            if (!TryResolvePreviewInfoTfms(projectReference, out var targetFrameworks))
            {
                xamlFileInfo = default!;
                return false;
            }

            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFrameworks[0]);

            if (BuildEngine.BuildProjectFile(projectReference, TargetNamesCommon, props, targetOutputs))
            {
                var projectPath = targetOutputs.ResultFromSingle(SelectInfoProjectPath);
                if (readies.Contains(projectPath, StringComparer.OrdinalIgnoreCase))
                {
                    xamlFileInfo = default!;
                    return false;
                }

                xamlFileInfo = new XamlFileInfo
                {
                    ProjectPath = projectPath,
                    AvaloniaResource = targetOutputs.ResultFromArrayAsSingleSkipNonXaml(SelectInfoAvaloniaResource),
                    AvaloniaXaml = targetOutputs.ResultFromArrayAsSingle(SelectInfoAvaloniaXaml),
                };

                xamlFileInfo.ReferenceXamlFileInfoCollection = ResolveReferenceXamlFileInfoCollection(
                    xamlFileInfo.ProjectPath, props, readies);

                return true;
            }

            xamlFileInfo = default!;
            return false;
        }

        private bool TryResolveAppExecInfo(out AppExecInfo appExecInfo, string targetFramework = "")
        {
            var targetOutputs = new Dictionary<string, ITaskItem[]>();
            var props = GetGlobalProperties(targetFramework);

            if (BuildEngine.BuildProjectFile(ProjectFile, TargetNamesAppExecInfo, props, targetOutputs))
            {
                appExecInfo = SelectAppExecInfo(targetOutputs);
                return true;
            }

            appExecInfo = default!;
            return false;
        }

        private static bool IsReferencesAvalonia(PreviewInfo previewInfo) =>
            !string.IsNullOrEmpty(previewInfo.AvaloniaPreviewerNetCoreToolPath)
            || !string.IsNullOrEmpty(previewInfo.AvaloniaPreviewerNetFullToolPath);

        private void LogProjectNotAvalonia() =>
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
