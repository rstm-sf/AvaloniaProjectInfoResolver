using System.Collections.ObjectModel;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    internal class ValueNode : INode
    {
        public INode Parent { get; }

        public string Header { get; }

        public ObservableCollection<INode> Children { get; } = null!;

        public ValueNode(PropertyNode parentNode, string value)
        {
            Parent = parentNode;
            Header = value;
        }
    }
}
