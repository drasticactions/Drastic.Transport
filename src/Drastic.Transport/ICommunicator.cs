// <copyright file="ICommunicator.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Net.Sockets;

namespace Drastic.Transport
{
    public interface ITcpCommunicatorServer : ICommunicator
    {
        event EventHandler ClientConnected;

        int ClientsCount { get; }
    }

    public interface ICommunicator : IDataBridge
    {
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public event EventHandler<ConnectionStateEventArgs> ConnectionStateChanged;

        bool IsRunning { get; }

        Task<bool> SendAsync(Message message);

        Task<bool> SendAsync(NetworkStream? stream, Message message);

        Task<bool> Connect(CancellationToken? token);

        Task Disconnect();
    }

    public class DataReceivedEventArgs : EventArgs
    {
        public DataReceivedEventArgs(Message message)
        {
            this.Message = message;
        }

        public Message Message { get; }
    }

    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionStateEventArgs(bool connected, string clientId)
        {
            this.Connected = connected;
            this.ClientId = clientId;
        }

        public bool Connected { get; }

        public string ClientId { get; }
    }
}
