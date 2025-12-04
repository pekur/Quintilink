using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Quintilink.Models;
using Quintilink.Services;

namespace Quintilink.ViewModels
{
    public partial class HexComparisonViewModel : ObservableObject
    {
        private readonly IHexComparisonService _comparisonService;

        [ObservableProperty]
        private MessageDefinition? message1;

        [ObservableProperty]
        private MessageDefinition? message2;

        [ObservableProperty]
        private HexComparisonResult? comparisonResult;

        [ObservableProperty]
        private string message1Hex = string.Empty;

        [ObservableProperty]
        private string message2Hex = string.Empty;

        [ObservableProperty]
        private string message1Ascii = string.Empty;

        [ObservableProperty]
        private string message2Ascii = string.Empty;

        [ObservableProperty]
        private int totalBytes;

        [ObservableProperty]
        private int differentBytes;

        [ObservableProperty]
        private double similarityPercentage;

        public ObservableCollection<ByteDifference> Differences { get; } = new();
        public ObservableCollection<MessageDefinition> AvailableMessages { get; } = new();

        public HexComparisonViewModel()
        {
            _comparisonService = new HexComparisonService();
        }

        partial void OnMessage1Changed(MessageDefinition? value)
        {
            if (value != null)
            {
                Message1Hex = value.DisplayHex;
                Message1Ascii = value.DisplayAscii;
                CompareMessages();
            }
        }

        partial void OnMessage2Changed(MessageDefinition? value)
        {
            if (value != null)
            {
                Message2Hex = value.DisplayHex;
                Message2Ascii = value.DisplayAscii;
                CompareMessages();
            }
        }

        [RelayCommand]
        private void CompareMessages()
        {
            if (Message1 == null || Message2 == null)
                return;

            ComparisonResult = _comparisonService.Compare(Message1, Message2);

            TotalBytes = ComparisonResult.TotalBytes;
            DifferentBytes = ComparisonResult.DifferentBytes;
            SimilarityPercentage = ComparisonResult.SimilarityPercentage;

            Differences.Clear();
            foreach (var diff in ComparisonResult.Differences)
            {
                Differences.Add(diff);
            }
        }

        public void LoadMessages(ObservableCollection<MessageDefinition> messages)
        {
            AvailableMessages.Clear();
            foreach (var msg in messages)
            {
                AvailableMessages.Add(msg);
            }
        }
    }
}
