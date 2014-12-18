using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aroc
{
    sealed class PipeServer
    {
        private readonly NamedPipeServerStream _pipe = new NamedPipeServerStream("Echo", PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        public PipeServer()
        {
            _pipe.BeginWaitForConnection(ClientConnected, null);
        }

        private void ClientConnected(IAsyncResult result)
        {
            new PipeServer();
            _pipe.EndWaitForConnection(result);
            byte[] data = new byte[1000];
            _pipe.BeginRead(data, 0, data.Length, GotRequest, data);
        }

        private void GotRequest(IAsyncResult result)
        {
            int bytesRead = _pipe.EndRead(result);
            byte[] data = (byte[])result.AsyncState;
            // 处理数据
            _pipe.BeginWrite(data, 0, data.Length, WriteDone, null);
        }

        private void WriteDone(IAsyncResult result)
        {
            _pipe.EndWrite(result);
            _pipe.Close();
        }
    }

    sealed class PipeClient
    {
        private readonly NamedPipeClientStream _pipe;

        public PipeClient(string serverName, string message)
        {
            _pipe = new NamedPipeClientStream(serverName, "Echo", PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            _pipe.Connect();
            _pipe.ReadMode = PipeTransmissionMode.Message;

            //处理消息
            byte[] output = Encoding.UTF8.GetBytes(message);
            _pipe.BeginWrite(output, 0, output.Length, WriteDone, null);
        }

        private void WriteDone(IAsyncResult result)
        {
            _pipe.EndWrite(result);

            byte[] data = new byte[100];
            _pipe.BeginRead(data, 0, data.Length, GotResponse, data);
        }

        private void GotResponse(IAsyncResult result)
        {
            int byteRead = _pipe.EndRead(result);

            byte[] data = (byte[])result.AsyncState;
            _pipe.Close();
        }
    }
}
