using System;
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
        private static readonly IProjectInfoResolver ProjectInfoResolver = new ProjectInfoResolver();

        private string _projectFilePath;

        private string _errors;

        public string ProjectFilePath
        {
            get => _projectFilePath;
            private set => this.RaiseAndSetIfChanged(ref _projectFilePath, value);
        }

        public string Errors
        {
            get => _errors;
            private set => this.RaiseAndSetIfChanged(ref _errors, value);
        }

        public bool IsVisibleAvaloniaProjectProps => string.IsNullOrEmpty(Errors);

        public ObservableCollection<INode> AvaloniaProjectProps { get; }

        public ReactiveCommand<Unit, Unit> OpenProject { get; }

        public Interaction<Unit, string?> ShowOpenFileDialog { get; }

        public MainWindowViewModel()
        {
            _projectFilePath = string.Empty;
            _errors = string.Empty;
            AvaloniaProjectProps = new ObservableCollection<INode>();
            OpenProject = ReactiveCommand.CreateFromTask(OpenFileAsync);
            ShowOpenFileDialog = new Interaction<Unit, string?>();

            this.WhenAnyValue(x => x.ProjectFilePath)
                .Subscribe(async x => await UpdateTreeViewAsync(x));

            this.WhenAnyValue(x => x.Errors)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsVisibleAvaloniaProjectProps)));
        }

        private async Task OpenFileAsync()
        {
            var fileName = await ShowOpenFileDialog.Handle(Unit.Default);
            if (fileName is null)
                return;
            ProjectFilePath = fileName;
        }

        private async Task UpdateTreeViewAsync(string fileName)
        {
            AvaloniaProjectProps.Clear();

            if (string.IsNullOrEmpty(fileName))
                return;

            var (avaloniaProjectProps, errors) = await Task.Run(async () =>
            {
                var result = await ProjectInfoResolver.ResolvePreviewProjectInfoAsync(fileName);
                var rootNode = result.HasError ? null : NodesHelper.SelectRootNode(result.ProjectInfo!);
                return (rootNode, result.Errors);
            });

            if (avaloniaProjectProps is not null)
                AvaloniaProjectProps.AddRange(avaloniaProjectProps.Children);

            Errors = errors;
        }
    }
}
