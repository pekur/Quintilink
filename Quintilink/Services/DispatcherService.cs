using System;
using System.Threading.Tasks;
using System.Windows;

namespace Quintilink.Services
{
    public class DispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
        {
   if (Application.Current?.Dispatcher != null)
   {
          if (Application.Current.Dispatcher.CheckAccess())
          {
        action();
    }
     else
   {
     Application.Current.Dispatcher.Invoke(action);
}
    }
    }

        public Task InvokeAsync(Action action)
        {
   if (Application.Current?.Dispatcher != null)
    {
     if (Application.Current.Dispatcher.CheckAccess())
         {
  action();
   return Task.CompletedTask;
    }
     else
      {
      return Application.Current.Dispatcher.InvokeAsync(action).Task;
      }
      }
   
     return Task.CompletedTask;
 }
  }
}
