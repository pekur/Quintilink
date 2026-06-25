using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Quintilink.Models;
using Quintilink.Services;

namespace Quintilink.ViewModels
{
    public partial class MqttPublishEditorViewModel : ObservableObject, IDialogRequestClose
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string topic = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PayloadByteCount))]
        private string payload = string.Empty;

        [ObservableProperty]
        private int qos;

        [ObservableProperty]
        private bool retain;

        public ObservableCollection<int> QosLevels { get; } = new() { 0, 1, 2 };

        public bool IsValid => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Topic);

        public int PayloadByteCount => System.Text.Encoding.UTF8.GetByteCount(Payload ?? string.Empty);

        public event Action<bool>? RequestClose;

        public MqttPublishEditorViewModel() { }

        public void Load(MqttPublishDefinition def)
        {
            Name = def.Name;
            Topic = def.Topic;
            Payload = def.Payload;
            Qos = def.Qos;
            Retain = def.Retain;
        }

        public MqttPublishDefinition ToDefinition() => new()
        {
            Name = Name.Trim(),
            Topic = Topic.Trim(),
            Payload = Payload ?? string.Empty,
            Qos = Qos,
            Retain = Retain
        };

        [RelayCommand(CanExecute = nameof(IsValid))]
        private void Save() => RequestClose?.Invoke(true);

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);
    }
}
