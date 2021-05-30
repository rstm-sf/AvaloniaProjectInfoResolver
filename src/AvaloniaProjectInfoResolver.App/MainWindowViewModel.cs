using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
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

        private string _errorMessage;

        private bool _isVisibleOpenProject;

        public string ProjectFilePath
        {
            get => _projectFilePath;
            private set => this.RaiseAndSetIfChanged(ref _projectFilePath, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public bool IsVisibleOpenProject
        {
            get => _isVisibleOpenProject;
            private set => this.RaiseAndSetIfChanged(ref _isVisibleOpenProject, value);
        }

        public bool IsVisibleAvaloniaProjectProps => string.IsNullOrEmpty(ErrorMessage);

        public ObservableCollection<INode> AvaloniaProjectProps { get; }

        public ReactiveCommand<Unit, Unit> OpenProject { get; }

        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public Interaction<Unit, string?> ShowOpenFileDialog { get; }

        public MainWindowViewModel()
        {
            _projectFilePath = string.Empty;
            _errorMessage = string.Empty;
            _isVisibleOpenProject = true;
            AvaloniaProjectProps = new ObservableCollection<INode>();

            OpenProject = ReactiveCommand.CreateFromObservable(
                () => Observable
                    .StartAsync(OpenFileAsync)
                    .TakeUntil(CancelCommand!));
            CancelCommand = ReactiveCommand.Create(() => { }, OpenProject.IsExecuting);

            ShowOpenFileDialog = new Interaction<Unit, string?>();

            this.WhenAnyValue(x => x.ErrorMessage)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsVisibleAvaloniaProjectProps)));
        }

        private async Task OpenFileAsync(CancellationToken cancellationToken)
        {
            var fileName = await ShowOpenFileDialog.Handle(Unit.Default);
            if (fileName is null)
                return;
            await UpdateTreeViewAsync(fileName, cancellationToken);
        }

        private async Task UpdateTreeViewAsync(string fileName, CancellationToken cancellationToken)
        {
            var prevFilePath = ProjectFilePath;
            ProjectFilePath = fileName;
            if (string.IsNullOrEmpty(fileName))
                return;

            IsVisibleOpenProject = false;
            var (avaloniaProjectProps, errors) = await Task.Run(
                async () =>
                {
                    var result = await ProjectInfoResolver.ResolvePreviewInfoAsync(
                        fileName, cancellationToken);
                    var rootNode = cancellationToken.IsCancellationRequested || result.HasError
                        ? null
                        : NodesHelper.SelectRootNode(result.PreviewInfo!);
                    return (rootNode, result.Error);
                },
                cancellationToken);
            IsVisibleOpenProject = true;

            if (cancellationToken.IsCancellationRequested)
            {
                ProjectFilePath = prevFilePath;
                return;
            }

            AvaloniaProjectProps.Clear();
            if (avaloniaProjectProps is not null)
                AvaloniaProjectProps.AddRange(avaloniaProjectProps.Children);
            ErrorMessage = errors;
        }
    }
}
