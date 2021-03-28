using System;
using System.Collections.Generic;
using System.Reflection;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    internal static class NodesHelper
    {
        public static RootNode SelectRootNode(ProjectInfo projectInfo)
        {
            var rootNode = new RootNode();
            foreach (var property in GetProperties(typeof(ProjectInfo)))
            {
                if (property.Name == nameof(ProjectInfo.ProjectInfoByTfmArray))
                {
                    foreach (var info in projectInfo.ProjectInfoByTfmArray)
                    {
                        var node = new PropertyCollectionNode(rootNode, info);
                        rootNode.Children.Add(node);
                    }
                }
                else
                {
                    var node = new PropertyNode(rootNode, property.Name, (string)property.GetValue(projectInfo, null)!);
                    rootNode.Children.Add(node);
                }
            }

            return rootNode;
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type) => type.GetProperties();
    }
}
