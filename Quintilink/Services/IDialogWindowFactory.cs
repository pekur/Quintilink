using System.Windows;

namespace Quintilink.Services
{
    public interface IDialogWindowFactory
    {
        Window? CreateWindowFor(Type viewModelType);
    }
}
