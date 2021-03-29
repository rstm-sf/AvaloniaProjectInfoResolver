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

        /// <remarks>
        /// F.ex., AvaloniaResource includes AvaloniaXaml in case of 'GenerateAvaloniaResources' target.
        /// For more info, see https://github.com/AvaloniaUI/Avalonia/blob/f33e0de004846be854836e095a3721dc80d9fd94/packages/Avalonia/AvaloniaBuildTasks.targets#L48-L55
        /// </remarks>
        public static string ResultFromArrayAsSingleSkipNonXaml(
            this Dictionary<string, ITaskItem[]> targetOutputs, string target)
        {
            var output = targetOutputs[target];
            return output.Length > 0
                ? string.Join(";", output.Where(x => x.GetMetadata("Extension") == ".xaml").Select(x => x.ItemSpec))
                : string.Empty;
        }
    }
}
