﻿using System;
using System.Net.Sockets;
using AwesomeSockets.Domain.Sockets;
using AwesomeSockets.Sockets;
using NUnit.Framework;
using Buffer = AwesomeSockets.Buffers.Buffer;
using System.Threading;

namespace AwesomeSockets.Tests.AcceptanceTests
{
    [TestFixture]
    class TcpSyncTests
    {
        private bool isServerThreadFinished = false;
        private bool isClientThreadFinished = false;

        [Test]
        public void TcpSynchronousAcceptanceTest()
        {
            Thread serverThread = new Thread(() => {
                ServerThread(x => {
                    ServerThreadCallback(x);
                });
            });

            Thread clientThread = new Thread(() => {
                ClientThread(x => {
                    ClientThreadCallback(x);
                });
            });

            clientThread.Start();
            serverThread.Start();

            do
            {
                Thread.Sleep(1000);
            } while (!isServerThreadFinished && !isClientThreadFinished);

            //Politely ask both threads to stop before main thread dies and hard-aborts them
            serverThread.Abort();
            clientThread.Abort();

            //Sleep to allow the threads a chance to die
            Thread.Sleep(1000);
        }

        #region Shared Methods
        private void SendTestMessage(ISocket other, Buffer sendBuffer)
        {
            Buffer.ClearBuffer(sendBuffer);
            Buffer.Add(sendBuffer, 10);
            Buffer.Add(sendBuffer, 20.0F);
            Buffer.Add(sendBuffer, 40.0);
            Buffer.Add(sendBuffer, 'A');
            Buffer.Add(sendBuffer, "The quick brown fox jumped over the lazy dog");
            Buffer.Add(sendBuffer, (byte)255);
            Buffer.FinalizeBuffer(sendBuffer);

            var bytesSent = AweSock.SendMessage(other, sendBuffer);
            Console.WriteLine("Sent payload. {0} bytes written.", bytesSent);
        }

        private void ServerThreadCallback(bool isSuccess)
        {
            isServerThreadFinished = true;
        }

        private void ClientThreadCallback(bool isSuccess)
        {
            isClientThreadFinished = true;
        }
        #endregion

        #region Server Methods
        private void ServerThread(Action<bool> callback)
        {
            var sendBuffer = Buffer.New();
            var recvBuffer = Buffer.New();
            var listenSocket = AweSock.TcpListen(14804);
            var client = AweSock.TcpAccept(listenSocket);

            SendTestMessage(client, sendBuffer);
            callback(ReceiveResponseFromClient(client, recvBuffer));
        }       

        private bool ReceiveResponseFromClient(ISocket client, Buffer recvBuffer)
        {
            bool SERVER_EXIT_FLAG = false;
            do
            {
                var bytesReceived = AweSock.ReceiveMessage(client, recvBuffer);
                if (bytesReceived.Item1 > 0)
                {
                    if (!ValidateResponse(recvBuffer))
                        return false;
                    SERVER_EXIT_FLAG = true;
                }
                else if (bytesReceived.Item1 == 0)
                    return false;

                Thread.Sleep(1000);
            } while (!SERVER_EXIT_FLAG);
            return true;
        }

        private bool ValidateResponse(Buffer receiveBuffer)
        {
            return ((Buffer.Get<int>(receiveBuffer) == 10) && (Buffer.Get<float>(receiveBuffer) == 20.0F) && (Buffer.Get<double>(receiveBuffer) == 40.0) && (Buffer.Get<char>(receiveBuffer) == 'A') &&
                (Buffer.Get<string>(receiveBuffer) == "The quick brown fox jumped over the lazy dog") && (Buffer.Get<byte>(receiveBuffer) == ((byte)255)));
        }
        #endregion

        #region Client Methods
        public void ClientThread(Action<bool> callback)
        {
            Buffer sendBuffer = Buffer.New();
            Buffer recvBuffer = Buffer.New();

            ISocket server = AweSock.TcpConnect("127.0.0.1", 14804);

            ReceiveMessageFromServer(server, recvBuffer);
            SendTestMessage(server, sendBuffer);
            callback(true);
        }

        private bool ReceiveMessageFromServer(ISocket server, Buffer recvBuffer)
        {
            bool CLIENT_EXIT_FLAG = false;
            do
            {
                var bytesReceived = AweSock.ReceiveMessage(server, recvBuffer);
                if (bytesReceived.Item1 > 0)
                    CLIENT_EXIT_FLAG = true;
                else if (bytesReceived.Item1 == 0)
                    return false;

                Thread.Sleep(1000);
            } while (!CLIENT_EXIT_FLAG);
            return true;
        }
        #endregion
    }
}
