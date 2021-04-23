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
            Header = nameof(PreviewInfo.AppExecInfoCollection);
            Children = new ObservableCollection<INode>();
            foreach (var info in appExecInfoCollection)
            {
                var node = new PropertyCollectionNode(this, info);
                Children.Add(node);
            }
        }

        public PropertyCollectionNode(INode parent, XamlFileInfo xamlFileInfo)
        {
            Parent = parent;
            Header = nameof(PreviewInfo.XamlFileInfo);
            Children = new ObservableCollection<INode>(
                NodesHelper.GetProperties(typeof(XamlFileInfo))
                    .Where(x => x.Name != nameof(XamlFileInfo.ReferenceXamlFileInfoCollection))
                    .Select(x =>
                        new PropertyNode(this, x.Name, (string)x.GetValue(xamlFileInfo, null)!)));
            Children.Add(new PropertyCollectionNode(this, xamlFileInfo.ReferenceXamlFileInfoCollection));
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

        private PropertyCollectionNode(
            PropertyCollectionNode parent, IReadOnlyList<XamlFileInfo> referenceXamlFileInfoCollection)
        {
            Parent = parent;
            Header = nameof(XamlFileInfo.ReferenceXamlFileInfoCollection);
            Children = new ObservableCollection<INode>(
                referenceXamlFileInfoCollection.Select(x => new PropertyCollectionNode(this, x)));
        }
    }
}
