using System.Collections.ObjectModel;
using System.Linq;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    internal class PropertyCollectionNode : INode
    {
        public INode Parent { get; }

        public string Header { get; }

        public ObservableCollection<INode> Children { get; }

        public PropertyCollectionNode(RootNode parent, ProjectInfoByTfm projectInfoByTfm)
        {
            Parent = parent;
            Header = nameof(ProjectInfoByTfm);
            Children = new ObservableCollection<INode>(
                NodesHelper.GetProperties(typeof(ProjectInfoByTfm))
                    .Select(x =>
                        new PropertyNode(this, x.Name, (string)x.GetValue(projectInfoByTfm, null)!)));
        }

        public PropertyCollectionNode(RootNode parent, XamlFileInfo xamlFileInfo)
        {
            Parent = parent;
            Header = nameof(XamlFileInfo);
            Children = new ObservableCollection<INode>(
                NodesHelper.GetProperties(typeof(XamlFileInfo))
                    .Select(x =>
                        new PropertyNode(this, x.Name, (string)x.GetValue(xamlFileInfo, null)!)));
        }
    }
}
