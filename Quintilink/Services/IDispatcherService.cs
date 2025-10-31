using System;

namespace Quintilink.Services
{
    public interface IDispatcherService
    {
        /// <summary>
        /// Executes the specified action synchronously on the UI thread
     /// </summary>
  void Invoke(Action action);

 /// <summary>
 /// Executes the specified action asynchronously on the UI thread
 /// </summary>
    Task InvokeAsync(Action action);
    }
}
