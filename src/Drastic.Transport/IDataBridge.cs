namespace Drastic.Transport
{
    public interface IDataBridge
    {
        ManualResetEvent ReadyEvent { get; }

        ManualResetEvent FirstMessageEvent { get; }

        byte[] ReadMessage();

        void WriteMessage(byte[] buffer);

        void Close();
    }
}
