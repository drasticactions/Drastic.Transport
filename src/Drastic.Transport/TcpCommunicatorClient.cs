// <copyright file="TcpCommunicatorClient.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Net.Sockets;
using System.Text.Json;

namespace Drastic.Transport
{
    public class TcpCommunicatorClient : TcpCommunicator
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private CancellationTokenSource? readCancellationToken;

        public TcpCommunicatorClient(TcpInitializationData initData, JsonSerializerOptions options, ILogger? logger = default)
            : base(initData, options, logger)
        {
            this.Ip = initData.IpAddress;
            this.Port = initData.Port;

            this.ConnectionStateChanged += this.OnConnectionStateChanged;
        }

        public string Ip { get; }

        public int Port { get; }

        public string? ClientId { get; internal set; } = null;

        public override async Task<bool> Connect(CancellationToken? token = default)
        {
            var cancellationToken = token ?? CancellationToken.None;

            try
            {
                if (string.IsNullOrWhiteSpace(this.Ip))
                {
                    throw new ArgumentException("Ip has not been set", nameof(Ip));
                }

                await this.Disconnect();
                this.IsRunning = true;
                this.client = new TcpClient();
                await this.client.ConnectAsync(Ip, Port);
                this.stream = client.GetStream();
                this.readCancellationToken = new CancellationTokenSource();
                if (cancellationToken.IsCancellationRequested)
                {
                    await Disconnect();
                    return false;
                }

                this.Receive(this.stream, readCancellationToken.Token);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override async Task Disconnect()
        {
            this.readCancellationToken?.Cancel();
            this.IsRunning = false;
            if ((this.client?.Connected ?? false) && stream != null)
            {
                await this.SendAsync(this.stream, new DisconnectMessage { ClientId = this.ClientId });
            }

            this.ClientId = null;
            this.client?.Close();
            this.client?.Dispose();
        }

        public override Task<bool> SendAsync(Message message)
            => SendAsync(this.stream, message);

        private void OnConnectionStateChanged(object sender, ConnectionStateEventArgs e)
        {
            if (e.Connected)
            {
                this.Logger?.Log(LogLevel.Debug, $"ClientID:{e.ClientId} connected");
                this.ClientId = e.ClientId;
            }
            else
            {
                this.Logger?.Log(LogLevel.Debug, $"ClientID:{e.ClientId} disconnected");
            }
        }
    }
}
