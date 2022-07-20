// <copyright file="ILogger.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Drastic.Transport
{
    public interface ILogger
    {
        void Log(LogMessage message);
    }

    public static class Logger
    {
        public static void Log(this ILogger logger, LogLevel level, string message)
            => logger.Log(new LogMessage(DateTime.Now, level, message));

        public static void Log(this ILogger logger,
            Exception ex,
            LogLevel level = LogLevel.Error,
            [CallerMemberName] string memberName = "(unknown)",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            logger.Log(level, $"Caught exception in {memberName} at {sourceLineNumber}: {ex}\n{ex.StackTrace}");
        }

        public static void LogIfFaulted(this Task task,
            ILogger logger,
            LogLevel level = LogLevel.Error,
            [CallerMemberName] string memberName = "(unknown)",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            task.ContinueWith(t => logger.Log(t.Exception, level, memberName, sourceLineNumber),
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        public static void Log(this ILogger logger,
            Stopwatch sw,
            LogLevel level = LogLevel.Perf,
            [CallerMemberName] string memberName = "(unknown)",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            logger.Log(level, $"Elapsed time in {memberName} at {sourceLineNumber}: {sw.ElapsedMilliseconds}ms");
        }
    }
}
