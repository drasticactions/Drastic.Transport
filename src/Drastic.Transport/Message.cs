// <copyright file="Message.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace Drastic.Transport
{
    [Serializable]
    public abstract class Message
    {
        public Message()
        {
            this.Type = nameof(Message);
        }

        public string Type { get; set; }
    }

    public class ConnectMessage : Message
    {
        [JsonConstructor]
        public ConnectMessage() : base()
        {
            Type = nameof(ConnectMessage);
            ClientId = string.Empty;
        }

        public string ClientId { get; set; }
    }

    [Serializable]
    public class DisconnectMessage : Message
    {
        public DisconnectMessage() : base()
        {
            Type = nameof(DisconnectMessage);
            ClientId = string.Empty;
        }

        public string ClientId { get; set; }
    }

    [Serializable]
    public class RuntimeHostMessage : Message
    {
        public RuntimeHostMessage()
        {
            this.Type = nameof(RuntimeHostMessage);
        }

        public byte[] Message { get; set; }
    }

    [Serializable]
    public class LogMessageMessage : Message
    {
        public LogMessageMessage()
        {
            this.Type = nameof(LogMessageMessage);
        }

        public LogMessage Message { get; set; }
    }

    [Serializable]
    public sealed class LogMessage
    {
        public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.f";

        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public string Message { get; private set; }

        public LogMessage(DateTime timestamp, LogLevel level, string message)
        {
            if (level <= LogLevel.All || level >= LogLevel.None)
                throw new ArgumentException("Invalid log level", nameof(level));
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            Timestamp = timestamp;
            Level = level;
            Message = message;
        }

        public LogMessage WithMessage(string message)
        {
            var result = (LogMessage)MemberwiseClone();
            result.Message = message;
            return result;
        }

        public override string ToString()
            => $"[Test] ({Timestamp.ToString(TimestampFormat)}): {Level.ToString().ToUpperInvariant()}: {Message}";

        /// <summary>
        /// Return message appropriate for the output pad/pane, which is more visible to the end user (but still technical).
        /// Here, we want to have somewhat concise output, minimzing horizontal scrolling.
        /// </summary>
        public string ToOutputPaneString()
        {
            // In the output pane, only show time, not date, to make it more concise.
            // Use the locale specific long time format (e.g. "1:45:30 PM" for en-US)
            String timestamp = Timestamp.ToString("T");

            return $"[{timestamp}]  {Message}";
        }
    }

    public enum LogLevel
    {
        /// <summary>
        /// Used for filtering only; All messages are logged
        /// </summary>
        All, // must be first

        /// <summary>
        /// Informational messages used for debugging or to trace code execution
        /// </summary>
        Debug,

        /// <summary>
        /// Informational messages containing performance metrics
        /// </summary>
        Perf,

        /// <summary>
        /// Informational messages that might be of interest to the user
        /// </summary>
        Info,

        /// <summary>
        /// Warnings
        /// </summary>
        Warn,

        /// <summary>
        /// Errors that are handled gracefully
        /// </summary>
        Error,

        /// <summary>
        /// Errors that are not handled gracefully
        /// </summary>
        Fail,

        /// <summary>
        /// Used for filtering only; No messages are logged
        /// </summary>
        None, // must be last
    }
}
