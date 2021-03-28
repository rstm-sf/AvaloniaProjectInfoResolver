using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaProjectInfoResolver.App.Nodes;
using DynamicData;
using ReactiveUI;

namespace AvaloniaProjectInfoResolver.App
{
    public class MainWindowViewModel : ReactiveObject
    {
        private static readonly ProjectInfoResolver ProjectInfoResolver = new();

        private string _projectFilePath;

        public string ProjectFilePath
        {
            get => _projectFilePath;
            private set => this.RaiseAndSetIfChanged(ref _projectFilePath, value);
        }

        public ObservableCollection<INode> Items { get; }

        public ReactiveCommand<Unit, Unit> OpenProject { get; }

        public Interaction<Unit, string?> ShowOpenFileDialog { get; }

        public MainWindowViewModel()
        {
            _projectFilePath = string.Empty;
            Items = new ObservableCollection<INode>();
            OpenProject = ReactiveCommand.CreateFromTask(OpenFileAsync);
            ShowOpenFileDialog = new Interaction<Unit, string?>();
        }

        private async Task OpenFileAsync()
        {
            var fileName = await ShowOpenFileDialog.Handle(Unit.Default);
            if (string.IsNullOrEmpty(fileName))
                return;

            ProjectFilePath = fileName;
            Items.Clear();

            var rootNode = await Task.Run(async () =>
            {
                var result = await ProjectInfoResolver.ResolvePreviewProjectInfoAsync(fileName);
                return result is null ? null : NodesHelper.SelectRootNode(result);
            });

            if (rootNode is not null)
                Items.AddRange(rootNode.Children);
        }
    }
}
