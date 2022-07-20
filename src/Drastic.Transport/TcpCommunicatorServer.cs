// <copyright file="TcpCommunicatorServer.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Drastic.Transport
{
    public class TcpCommunicatorServer : TcpCommunicator, ITcpCommunicatorServer
    {
        private int serverPort;
        private TcpListener? listener;
        protected ConcurrentDictionary<Guid, Tuple<TcpClient, CancellationTokenSource>> clients = new();

        public TcpCommunicatorServer(TcpInitializationData initData, JsonSerializerOptions options, ILogger? logger = default)
            : base(initData, options, logger)
        {
            this.serverPort = initData.Port;
            this.ConnectionStateChanged += OnConnectionStateChanged;
        }

        public event EventHandler? ClientConnected;

        public int ClientsCount => this.clients.Count;

        public override Task Disconnect()
        {
            foreach (var client in clients)
            {
                client.Value.Item1.Close();
                client.Value.Item2.Cancel();
            }

            this.clients.Clear();
            this.listener?.Stop();
            this.IsRunning = false;
            return Task.CompletedTask;
        }

        public override async Task<bool> SendAsync(Message message)
        {
            // Sends message to all clients. Maybe it should be able to send a message to a specific client?
            foreach (var client in this.clients)
            {

                bool isConnected = client.Value.Item1.Connected;
                if (isConnected)
                {
                    this.Logger?.Log(LogLevel.Debug, $"Sending to:{client.Key}");
                    try
                    {
                        var stream = client.Value.Item1.GetStream();
                        await this.SendAsync(stream, message);
                    }
                    catch
                    {
                        isConnected = false;
                    }
                }

                if (!isConnected)
                {
                    this.Logger?.Log(LogLevel.Error, $"Failed to send to:{client.Key}");
                    client.Value.Item1.Close();
                    this.clients.TryRemove(client.Key, out Tuple<TcpClient, CancellationTokenSource> removedClient);
                    removedClient?.Item2.Cancel();
                }
            }

            // Improve return if errors
            return true;
        }

        public override Task<bool> Connect(CancellationToken? token = default)
        {
            var taskCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task.Factory.StartNew(() => Run(taskCompletion, token), TaskCreationOptions.LongRunning);
            return taskCompletion.Task;
        }

        private async Task Run(TaskCompletionSource<bool> tcs, CancellationToken? token)
        {
            // TODO Token is ignored for the moment, but maybe it could be used to quit the process?

            try
            {
                await this.Disconnect();
                this.listener = new TcpListener(IPAddress.Any, this.serverPort);
                this.listener.Start();

                this.Logger?.Log(LogLevel.Debug, $"Tcp server listening at port {this.serverPort}");
                tcs.SetResult(true);

            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return;
            }

            this.IsRunning = true;

            // Loop
            for (; ; )
            {
                var client = await this.listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var cts = new CancellationTokenSource();
                this.Receive(stream, cts.Token);
                var guid = Guid.NewGuid();
                this.clients[guid] = new Tuple<TcpClient, CancellationTokenSource>(client, cts);

                await Task.Run(async () => {
                    await Task.Delay(100);
                    await this.SendAsync(stream, new ConnectMessage { ClientId = guid.ToString() });
                });
                Logger?.Log(LogLevel.Debug, $"New client connection: {guid}");
                this.ClientConnected?.Invoke(this, null);
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateEventArgs e)
        {
            if (!e.Connected)
            {
                this.Logger?.Log(LogLevel.Debug, $"ClientID:{e.ClientId} disconnected");
            }
        }
    }
}
