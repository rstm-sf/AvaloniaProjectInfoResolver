using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

// ReSharper disable once CheckNamespace
namespace AvaloniaProjectInfoResolver
{
    public class ProjectInfoErrorArgs : IXmlSerializable
    {
        public string Subcategory { get; internal set; } = String.Empty;

        public string Code { get; internal set; } = String.Empty;

        public string File { get; internal set; } = String.Empty;

        public int LineNumber { get; internal set; }

        public int ColumnNumber { get; internal set; }

        public string Message { get; internal set; } = String.Empty;

        public override string ToString() =>
            $"{File}({LineNumber}, {ColumnNumber}): [{Code}] {Message}";

        public XmlSchema? GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;

            Subcategory = reader.ReadElementString();
            Code = reader.ReadElementString();
            File = reader.ReadElementString();
            LineNumber = int.Parse(reader.ReadElementString(), NumberStyles.Number, CultureInfo.InvariantCulture);
            ColumnNumber = int.Parse(reader.ReadElementString(), NumberStyles.Number, CultureInfo.InvariantCulture);
            Message = reader.ReadElementString();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString(nameof(Subcategory), Subcategory);
            writer.WriteElementString(nameof(Code), Code);
            writer.WriteElementString(nameof(File), File);
            writer.WriteElementString(nameof(LineNumber), LineNumber.ToString(CultureInfo.InvariantCulture));
            writer.WriteElementString(nameof(ColumnNumber), ColumnNumber.ToString(CultureInfo.InvariantCulture));
            writer.WriteElementString(nameof(Message), Message);
        }
    }
}
