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
                switch (property.Name)
                {
                    case nameof(ProjectInfo.AppExecInfoCollection):
                    {
                        var node = new PropertyCollectionNode(rootNode, projectInfo.AppExecInfoCollection);
                        rootNode.Children.Add(node);
                        break;
                    }
                    case nameof(ProjectInfo.XamlFileInfo):
                    {
                        var node = new PropertyCollectionNode(rootNode, projectInfo.XamlFileInfo);
                        rootNode.Children.Add(node);
                        break;
                    }
                    default:
                    {
                        var node = new PropertyNode(rootNode, property.Name, (string)property.GetValue(projectInfo, null)!);
                        rootNode.Children.Add(node);
                        break;
                    }
                }
            }

            return rootNode;
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type) => type.GetProperties();
    }
}
