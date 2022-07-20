// <copyright file="LocalHostTests.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Text.Json;

namespace Drastic.Transport.Tests;

[TestClass]
public class LocalHostTests
{
    TcpCommunicatorServer server;
    ILogger? logger;
    string ipAddress = "127.0.0.1";
    int port = 8889;
    TcpInitializationData initData;
    JsonSerializerOptions jsonOptions;

    public LocalHostTests()
    {
        // TODO: Mock ILogger.
        this.logger = null;
        this.jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new MessageJsonConverter());
        this.initData = new TcpInitializationData(this.ipAddress, this.port);
    }

    [TestInitialize]
    public void Setup()
    {
        this.server = new TcpCommunicatorServer(initData, jsonOptions, this.logger);

        // Force the server to start when creating the test.
        var result = this.server.Connect().Result;

        if (!result)
        {
            throw new Exception("Failed to start the server.");
        }
    }

    [TestCleanup]
    public void TearDown()
    {
        this.server.Disconnect();
        this.server.Close();
    }

    [TestMethod]
    public async Task SingleClientConnection()
    {
        Assert.IsTrue(this.server.IsRunning);

        var client = await this.CreateClient();

        await client.Disconnect();

        Assert.IsFalse(client.IsRunning);
    }

    [TestMethod]
    public async Task SendMessageToServer()
    {
        this.server.DataReceived += Server_DataReceived;

        var client = await CreateClient();

        await client.SendAsync(new LogMessageMessage() { Message = new LogMessage(DateTime.Now, LogLevel.Info, "Hello!") });
        await Task.Delay(1000);

        this.server.DataReceived -= Server_DataReceived;

        void Server_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Assert.IsNotNull(e?.Message);
        }
    }

    private async Task<TcpCommunicatorClient> CreateClient()
    {
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new MessageJsonConverter());
        var initData = new TcpInitializationData(this.ipAddress, this.port);
        var client = new TcpCommunicatorClient(initData, jsonOptions, this.logger);
        var result = await client.Connect();

        // Just running async for Connect does not mean the server is "fully" connected yet.
        // The "true" connect is when the client as a ClientId.
        await Task.Delay(1000);

        Assert.IsTrue(result);

        Assert.IsTrue(this.server.ClientsCount > 0);
        Assert.IsNotNull(client.ClientId);

        return client;
    }
}