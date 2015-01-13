using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proactima.AzureWorkers
{
    public abstract class BaseWorker
    {
        private CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken Token { get; private set; }

        /// <summary>
        /// Set to false to disable the worker.
        /// Default is true.
        /// </summary>
        public virtual bool Enabled
        {
            get { return true; }
        }

        /// <summary>
        /// The wait time before calling 'Start' when the worker exits the 'Start' method
        /// Expressed in millisecond, default is 1000ms (1 second).
        /// </summary>
        protected virtual int LoopWaitTime
        {
            get { return 1000; }
        }

        /// <summary>
        /// The method to do actual work in a Worker.
        /// If method exits, it will be called again after LoopWaitTime passes.
        /// </summary>
        /// <returns></returns>
        protected abstract Task StartAsync();

        internal async Task ProtectedRun(CancellationTokenSource tokenSource)
        {
            _cancellationTokenSource = tokenSource;
            Token = _cancellationTokenSource.Token;

            while (!Token.IsCancellationRequested)
            {
                try
                {
                    await StartAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    ErrorLogging("An exception was caught in BaseWorker.ProtectedRun", exception);
                }

                if (LoopWaitTime > 0)
                    Token.WaitHandle.WaitOne(LoopWaitTime);
            }
        }

        internal void OnStop()
        {
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();
            OnStopping();
        }

        /// <summary>
        /// Will be called after cancellation token has been signaled, as the role
        /// is shutting down.
        /// </summary>
        protected virtual void OnStopping()
        {
        }

        /// <summary>
        /// Basic error logging
        /// </summary>
        /// <param name="message">Message provided by the worker</param>
        /// <param name="ex">The current exception</param>
        protected virtual void ErrorLogging(string message, Exception ex = null)
        {
        }

        /// <summary>
        /// Basic error logging with a message Id
        /// </summary>
        /// <param name="message">Message provided by the worker</param>
        /// <param name="messageId">Message Id (correlation id)</param>
        /// <param name="ex">The current exception</param>
        protected virtual void ErrorLogging(string message, string messageId = "", Exception ex = null)
        {
        }

        /// <summary>
        /// Informational logging from the worker
        /// </summary>
        /// <param name="message">Message provided by the worker</param>
        /// <param name="messageId">Message Id (correlation id)</param>
        protected virtual void InfoLogging(string message, string messageId = "")
        {
        }

        /// <summary>
        /// Debug logging from the worker
        /// </summary>
        /// <param name="message">Message provided by the worker</param>
        /// <param name="messageId">Message Id (correlation id)</param>
        /// <param name="timerValue">Timings</param>
        protected virtual void DebugLogging(string message, string messageId = "", double timerValue = 0.0)
        {
        }
    }
}