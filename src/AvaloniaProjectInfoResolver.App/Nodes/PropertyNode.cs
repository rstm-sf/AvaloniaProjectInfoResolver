using System.Collections.ObjectModel;
using System.Linq;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    internal class PropertyNode : INode
    {
        public INode Parent { get; }

        public string Header { get; }

        public ObservableCollection<INode> Children { get; }

        public PropertyNode(INode parent, string header, string value)
        {
            Parent = parent;
            Header = header;
            Children = new ObservableCollection<INode>(
                value.Split(';')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => new ValueNode(this, x)));
        }
    }
}
