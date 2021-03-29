using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace AvaloniaProjectInfoResolver.PreviewTask
{
    internal static class MsBuildFrameworkExtensions
    {
        public static string ResultFromSingle(this Dictionary<string, ITaskItem[]> targetOutputs, string target)
        {
            var output = targetOutputs[target];
            if (output.Length > 1)
                throw new Exception("Output array length is greater than one");
            return output.Length == 1 ? output.Single().ItemSpec : string.Empty;
        }

        public static string[] ResultFromArray(this Dictionary<string, ITaskItem[]> targetOutputs, string target)
        {
            var output = targetOutputs[target];
            return output.Length > 0
                ? output.Select(x => x.ItemSpec).ToArray()
                : new[] {string.Empty};
        }

        public static string ResultFromArrayAsSingle(this Dictionary<string, ITaskItem[]> targetOutputs, string target)
        {
            var output = targetOutputs[target];
            return output.Length > 0
                ? string.Join(";", output.Select(x => x.ItemSpec))
                : string.Empty;
        }

        public static string ResultFromArrayAsSingleSkipNonXaml(
            this Dictionary<string, ITaskItem[]> targetOutputs, string target)
        {
            var output = targetOutputs[target];
            return output.Length > 0
                ? string.Join(";", output.Where(IsXamlFile).Select(x => x.ItemSpec))
                : string.Empty;
        }

        private static bool IsXamlFile(ITaskItem item)
        {
            var extension = item.GetMetadata("Extension");
            return extension == ".xaml" || extension == ".axaml" || extension == ".paml";
        }
    }
}
