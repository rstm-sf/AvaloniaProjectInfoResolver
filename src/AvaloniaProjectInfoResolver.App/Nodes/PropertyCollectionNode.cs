using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    internal class PropertyCollectionNode : INode
    {
        public INode Parent { get; }

        public string Header { get; }

        public ObservableCollection<INode> Children { get; }

        public PropertyCollectionNode(RootNode parent, IReadOnlyList<AppExecInfo> appExecInfoCollection)
        {
            Parent = parent;
            Header = nameof(ProjectInfo.AppExecInfoCollection);
            Children = new ObservableCollection<INode>();
            foreach (var info in appExecInfoCollection)
            {
                var node = new PropertyCollectionNode(this, info);
                Children.Add(node);
            }
        }

        public PropertyCollectionNode(RootNode parent, XamlFileInfo xamlFileInfo)
        {
            Parent = parent;
            Header = nameof(ProjectInfo.XamlFileInfo);
            Children = new ObservableCollection<INode>(
                NodesHelper.GetProperties(typeof(XamlFileInfo))
                    .Select(x =>
                        new PropertyNode(this, x.Name, (string)x.GetValue(xamlFileInfo, null)!)));
        }

        private PropertyCollectionNode(PropertyCollectionNode parent, AppExecInfo appExecInfo)
        {
            Parent = parent;
            Header = nameof(AppExecInfo);
            Children = new ObservableCollection<INode>(
                NodesHelper.GetProperties(typeof(AppExecInfo))
                    .Select(x =>
                        new PropertyNode(this, x.Name, (string)x.GetValue(appExecInfo, null)!)));
        }
    }
}
