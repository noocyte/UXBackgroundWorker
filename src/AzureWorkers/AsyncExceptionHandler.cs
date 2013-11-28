using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Proactima.AzureWorkers
{
    public static class AsyncExceptionHandler
    {
        public static async Task LogWith(this Func<Task> action, Func<string, Exception, Task> logger,
            string errorMessage)
        {
            await HandleExceptionWith(action, exception => logger(errorMessage, exception)).ConfigureAwait(false);
        }

        public static async Task HandleExceptionWith(this Func<Task> action, Func<Exception, Task> exceptionHandler)
        {
            ExceptionDispatchInfo capturedException = null;
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);
            }

            if (capturedException != null)
                await exceptionHandler(capturedException.SourceException).ConfigureAwait(false);
        }
    }
}