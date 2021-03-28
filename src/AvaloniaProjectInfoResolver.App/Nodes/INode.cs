using System.Collections.ObjectModel;

namespace AvaloniaProjectInfoResolver.App.Nodes
{
    public interface INode
    {
        INode Parent { get; }

        string Header { get; }
        
        ObservableCollection<INode> Children { get; }
    }
}
