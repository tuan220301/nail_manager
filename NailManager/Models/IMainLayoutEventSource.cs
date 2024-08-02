namespace NailManager.Models;

public interface IMainLayoutEventSource
{
    event EventHandler<string> ToastRequested;
    event EventHandler<bool> LoadingRequested;
}