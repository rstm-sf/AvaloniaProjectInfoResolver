using System.IO;
using System.IO.Pipes;
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

            var error = e.Subcategory == "APIR"
                ? $"{e.File}: {e.Message}"
                : $"{e.File}({e.LineNumber}, {e.ColumnNumber}): [{e.Code}] {e.Message}";
            _streamWriter.WriteLine(error);
            _streamWriter.Flush();
        }
    }
}
