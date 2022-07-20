using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Drastic.Transport
{
    public abstract class TcpCommunicator : ICommunicator
    {
        private JsonSerializerOptions options;

        public event EventHandler<DataReceivedEventArgs>? DataReceived;

        public event EventHandler<ConnectionStateEventArgs>? ConnectionStateChanged;

        protected ILogger? Logger;

        private readonly byte[] readBuffer = new byte[1024];

        private readonly Queue<byte[]> runtimeHostMessages = new();

        public TcpCommunicator(TcpInitializationData initData, JsonSerializerOptions options, ILogger? logger = default)
        {
            this.options = options;
            this.Logger = logger;
        }

        public bool IsRunning { get; internal set; }

        public ManualResetEvent ReadyEvent { get; } = new ManualResetEvent(false);

        public ManualResetEvent FirstMessageEvent => ReadyEvent;

        public byte[] ReadMessage()
        {
            byte[] message;
            lock (runtimeHostMessages)
            {
                // TODO Dispose/Shutdown
                while (true)
                {
                    if (runtimeHostMessages.Count > 0)
                    {
                        message = runtimeHostMessages.Dequeue();
                        break;
                    }
                    Monitor.Wait(runtimeHostMessages);
                }
            }

            return message;
        }

        public void WriteMessage(byte[] buffer)
        {
            this.SendAsync(new RuntimeHostMessage() { Message = buffer });
        }

        public void Close()
        {
            DataReceived -= DataReceived;
            this.Disconnect();
        }

        public virtual Task<bool> Connect(CancellationToken? token = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task Disconnect()
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> SendAsync(Message message)
        {
            throw new NotImplementedException();
        }

        public async virtual Task<bool> SendAsync(NetworkStream? stream, Message message)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                var writer = new Utf8JsonWriter(stream);
                JsonSerializer.Serialize(writer, message, this.options);
                return true;
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex);
                return false;
            }
        }

        protected void Receive(NetworkStream stream, CancellationToken cancellationToken)
        {
            this.Logger?.Log(LogLevel.Debug, "Start receiving updates");
            Task.Run(
                () =>
                {
                        this.ReceiveLoop(stream, cancellationToken);
            }, cancellationToken);
        }

        private void ReceiveLoop(NetworkStream? stream, CancellationToken cancellationToken)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (stream.DataAvailable)
                    {
                        stream.BeginRead(readBuffer, 0, readBuffer.Length, ReadComplete, stream);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex);
            }
        }

        private void ReadComplete(IAsyncResult iar)
        {
            var stream = (NetworkStream)iar.AsyncState;
            int bytesAvailable = stream.EndRead(iar);
            var buffer = new ReadOnlySpan<byte>(readBuffer);
            var reader = new Utf8JsonReader(buffer);

            if (bytesAvailable > 0)
            {
                var message = JsonSerializer.Deserialize<Message>(ref reader, this.options);
                if (message != null)
                {
                    switch (message)
                    {
                        case ConnectMessage connect:
                            this.ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs (true, connect.ClientId));
                            break;
                        case DisconnectMessage disconnect:
                            this.ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs(true, disconnect.ClientId));
                            break;
                        default:
                            this.DataReceived?.Invoke(this, new DataReceivedEventArgs(message));
                            break;
                    }
                }
                else
                {
                    this.Logger?.Log(LogLevel.Error, "Failed to deserialize incoming message");
                }
            }

            Array.Clear(this.readBuffer, 0, this.readBuffer.Length);
        }
    }
}
