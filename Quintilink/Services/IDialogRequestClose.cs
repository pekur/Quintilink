namespace Quintilink.Services
{
    public interface IDialogRequestClose
    {
        event Action<bool>? RequestClose;
    }
}
