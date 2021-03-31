using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Xml.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace AvaloniaProjectInfoResolver.MSBuildLogger
{
    // ReSharper disable once UnusedType.Global
    public class ProjectInfoMsBuildLogger : Logger
    {
        private IEventSource? _eventSource;

        private StreamWriter? _streamWriter;

        public override void Initialize(IEventSource eventSource)
        {
            if (string.IsNullOrWhiteSpace(Parameters))
                throw new LoggerException("LoggerParentId was not set.");

            var id = Parameters.Split(';')[0];
            var sender = new AnonymousPipeClientStream(PipeDirection.Out, id);
            _streamWriter = new StreamWriter(sender);

            _eventSource = eventSource;
            _eventSource.ErrorRaised += EventSourceOnErrorRaised;
        }

        public override void Shutdown()
        {
            if (_streamWriter is null)
                return;
            _streamWriter.Dispose();
            _streamWriter = null;

            if (_eventSource is null)
                return;
            _eventSource.ErrorRaised -= EventSourceOnErrorRaised;
            _eventSource = null;
        }

        private void EventSourceOnErrorRaised(object sender, BuildErrorEventArgs e)
        {
            if (_streamWriter is null)
                return;

            var xmlData = SerializeToXml(new ProjectInfoErrorArgs
            {
                Subcategory = e.Subcategory,
                Code = e.Code,
                File = e.File,
                LineNumber = e.LineNumber,
                ColumnNumber = e.ColumnNumber,
                Message = e.Message
            });
            _streamWriter.WriteLine(xmlData);
            _streamWriter.Flush();
        }

        private static string SerializeToXml(ProjectInfoErrorArgs data)
        {
            using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);

            var serializer = new XmlSerializer(data.GetType());
            serializer.Serialize(stringWriter, data);

            return stringWriter.ToString();
        }
    }
}
