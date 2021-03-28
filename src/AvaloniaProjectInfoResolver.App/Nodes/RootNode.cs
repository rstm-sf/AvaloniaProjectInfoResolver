using System.Collections.ObjectModel;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    public class RootNode : INode
    {
        public INode Parent => null!;

        public string Header => nameof(RootNode);

        public ObservableCollection<INode> Children { get; } = new();
    }
}
