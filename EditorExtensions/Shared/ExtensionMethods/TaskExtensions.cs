using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.Practices.TransientFaultHandling;

namespace MadsKristensen.EditorExtensions
{
    public static class TaskExtensions
    {
        /// <summary>Handles errors on a task to prevent VS from crashing.</summary>
        /// <param name="operation">The description of the asynchronous operation to show in the log on failure.  Should start with a lowercase present-tense infinitive (eg,  "compiling someFile.less").</param>
        /// <returns>A successful task, even if the operation fails.</returns>
        /// <remarks>Call this method when aggregating async operations to report and ignore individual failures.</remarks>
        public static Task HandleErrors(this Task task, string operation)
        {
            // Add the error handler, then return the original task.
            // Don't wait on the continuation; it'll become canceled
            // on success.  http://stackoverflow.com/q/6573720/34397
            task.DoNotWait(operation);
            return task;
        }

        /// <summary>Handles errors on a task to prevent VS from crashing.</summary>
        /// <param name="operation">The description of the asynchronous operation to show in the log on failure.  Should start with a lowercase present-tense infinitive (eg,  "compiling someFile.less").</param>
        /// <remarks>Call this method when you call an async method and don't need to wait for the result.</remarks>
        public static void DoNotWait(this Task task, string operation)
        {
            if (SettingsStore.InTestMode)
                return; // Don't mask crashes in unit tests.
            task.ContinueWith(t =>
            {
                if (t.Exception == null)
                    return;
                Logger.Log("An exception was thrown when " + operation + ": "
                         + string.Join(Environment.NewLine, t.Exception.InnerExceptions));
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>Execute task asynchronously.</summary>
        public static Task<Process> ExecuteAsync(this ProcessStartInfo startInfo)
        {
            var process = Process.Start(startInfo);
            var processTaskCompletionSource = new TaskCompletionSource<Process>();

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                process.WaitForExit();
                processTaskCompletionSource.TrySetResult(process);
            };

            return processTaskCompletionSource.Task;
        }

        /// <summary>
        /// Keep executing task asynchronously until successful execution
        /// or the retry limit, set by the policy, is exausted.
        /// </summary>
        /// <param name="policy">The retry policy for task, can be manufactured by PolicyFactory.</param>
        /// <returns>A successful task, even if the operation fails.</returns>
        public async static Task ExecuteRetryableTaskAsync(this Task task, RetryPolicy policy)
        {
            await policy.ExecuteAsync(() => task);
        }

        /// <summary>
        /// Keep executing task asynchronously until successful execution
        /// or the retry limit, set by the policy, is exausted.
        /// </summary>
        /// <param name="policy">The retry policy for task, can be manufactured by PolicyFactory.</param>
        /// <returns>A successful task, even if the operation fails.</returns>
        public async static Task<T> ExecuteRetryableTaskAsync<T>(this Task<T> task, RetryPolicy policy) where T : class
        {
            return await policy.ExecuteAsync(() => task);
        }
    }
}
