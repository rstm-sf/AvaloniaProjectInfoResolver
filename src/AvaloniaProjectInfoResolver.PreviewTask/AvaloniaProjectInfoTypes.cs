using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

// ReSharper disable once CheckNamespace
namespace AvaloniaProjectInfoResolver
{
    public class ProjectInfo : IXmlSerializable
    {
        private static readonly XmlSerializer SerializerForXamlFileInfo
            = new(typeof(XamlFileInfo), new XmlRootAttribute(nameof(XamlFileInfo)));

        private static readonly XmlSerializer SerializerForTfm = new(typeof(ProjectInfoByTfm));

        public string AvaloniaPreviewerNetCoreToolPath { get; internal set; } = string.Empty;

        public string AvaloniaPreviewerNetFullToolPath { get; internal set; } = string.Empty;

        public ProjectInfoByTfm[] ProjectInfoByTfmArray { get; internal set; } = new ProjectInfoByTfm[0];

        public XamlFileInfo XamlFileInfo { get; internal set; } = new();

        public XmlSchema? GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;

            AvaloniaPreviewerNetCoreToolPath = reader.ReadElementString();
            AvaloniaPreviewerNetFullToolPath = reader.ReadElementString();

            reader.ReadStartElement(nameof(ProjectInfoByTfmArray));

            var result = new List<ProjectInfoByTfm>();
            while (SerializerForTfm.CanDeserialize(reader))
            {
                var info = (ProjectInfoByTfm)SerializerForTfm.Deserialize(reader);
                result.Add(info);
            }

            ProjectInfoByTfmArray = result.ToArray();

            reader.ReadEndElement();
            reader.Read();

            XamlFileInfo = (XamlFileInfo)SerializerForXamlFileInfo.Deserialize(reader);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString(nameof(AvaloniaPreviewerNetCoreToolPath), AvaloniaPreviewerNetCoreToolPath);
            writer.WriteElementString(nameof(AvaloniaPreviewerNetFullToolPath), AvaloniaPreviewerNetFullToolPath);

            writer.WriteStartElement(nameof(ProjectInfoByTfmArray));
            foreach (var projectInfoByTfm in ProjectInfoByTfmArray)
                SerializerForTfm.Serialize(writer, projectInfoByTfm);
            writer.WriteEndElement();

            SerializerForXamlFileInfo.Serialize(writer, XamlFileInfo);
        }
    }

    public class XamlFileInfo : IXmlSerializable
    {
        public string AvaloniaResource { get; internal set; } = string.Empty;

        public string AvaloniaXaml { get; internal set; } = string.Empty;

        public XmlSchema? GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;

            AvaloniaResource = reader.ReadElementString();
            AvaloniaXaml = reader.ReadElementString();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString(nameof(AvaloniaResource), AvaloniaResource);
            writer.WriteElementString(nameof(AvaloniaXaml), AvaloniaXaml);
        }
    }

    public class ProjectInfoByTfm : IXmlSerializable
    {
        public string ProjectDepsFilePath { get; internal set; } = string.Empty;

        public string ProjectRuntimeConfigFilePath { get; internal set; } = string.Empty;

        public string TargetFramework { get; internal set; } = string.Empty;

        public string TargetFrameworkIdentifier { get; internal set; } = string.Empty;

        public string TargetPath { get; internal set; } = string.Empty;

        public XmlSchema? GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;

            ProjectDepsFilePath = reader.ReadElementString();
            ProjectRuntimeConfigFilePath = reader.ReadElementString();
            TargetFramework = reader.ReadElementString();
            TargetFrameworkIdentifier = reader.ReadElementString();
            TargetPath = reader.ReadElementString();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString(nameof(ProjectDepsFilePath), ProjectDepsFilePath);
            writer.WriteElementString(nameof(ProjectRuntimeConfigFilePath), ProjectRuntimeConfigFilePath);
            writer.WriteElementString(nameof(TargetFramework), TargetFramework);
            writer.WriteElementString(nameof(TargetFrameworkIdentifier), TargetFrameworkIdentifier);
            writer.WriteElementString(nameof(TargetPath), TargetPath);
        }
    }
}
