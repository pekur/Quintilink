using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Quintilink.ViewModels;
using Quintilink.Views;

namespace Quintilink.Services
{
    public class DialogWindowFactory : IDialogWindowFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DialogWindowFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Window? CreateWindowFor(Type viewModelType)
        {
            var windowType = viewModelType switch
            {
                var type when type == typeof(MessageEditorViewModel) => typeof(MessageEditorWindow),
                var type when type == typeof(ResponseEditorViewModel) => typeof(ResponseEditorWindow),
                var type when type == typeof(SearchDialogViewModel) => typeof(SearchDialog),
                _ => null
            };

            return windowType == null
                ? null
                : (Window)_serviceProvider.GetRequiredService(windowType);
        }
    }
}
