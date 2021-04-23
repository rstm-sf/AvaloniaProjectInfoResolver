using System;
using System.Collections.Generic;
using System.Reflection;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    internal static class NodesHelper
    {
        public static RootNode SelectRootNode(PreviewInfo previewInfo)
        {
            var rootNode = new RootNode();
            foreach (var property in GetProperties(typeof(PreviewInfo)))
            {
                switch (property.Name)
                {
                    case nameof(PreviewInfo.AppExecInfoCollection):
                    {
                        var node = new PropertyCollectionNode(rootNode, previewInfo.AppExecInfoCollection);
                        rootNode.Children.Add(node);
                        break;
                    }
                    case nameof(PreviewInfo.XamlFileInfo):
                    {
                        var node = new PropertyCollectionNode(rootNode, previewInfo.XamlFileInfo);
                        rootNode.Children.Add(node);
                        break;
                    }
                    default:
                    {
                        var node = new PropertyNode(rootNode, property.Name, (string)property.GetValue(previewInfo, null)!);
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
