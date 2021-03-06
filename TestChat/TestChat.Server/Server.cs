﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestChat.Server
{
    class Server
    {
        private TcpListener _tcpListener;
        private List<Client> _clients = new List<Client>();
        private string[] _lastMessages = new string[5] {"", "", "", "", ""};

        public void AddConnection(Client client)
        {
            _clients.Add(client);
            Logger.Log(String.Format("{0} connected", client.Id));
        }

        public void RemoveConnection(string id)
        {
            Client client = _clients.FirstOrDefault(x => x.Id == id);
            if (client != null)
            {
                _clients.Remove(client);
                Logger.Log(String.Format("Connection closed with {0}", id));
            }
            Logger.Log(String.Format("Id {0} not found", id));
        }

        private void _PopLast(string message)
        {
            for (int i = 0; i < 4; i++)
            {
                _lastMessages[i] = _lastMessages[i + 1];
            }
            _lastMessages[4] = message;
        }

        public void Listen()
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, 8888);
                _tcpListener.Start();
                Logger.Log("Server started, waiting for connections");

                while (true)
                {
                    TcpClient tcpClient = _tcpListener.AcceptTcpClient();

                    Client client = new Client(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(client.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }
        }

        public void BroadcastMessage(string message, string id)
        {
            _PopLast(message);
            byte[] data = Encoding.Unicode.GetBytes(message);

            for (int i = 0; i < _clients.Count; i++)
            {
                _clients[i].Stream.Write(data, 0, data.Length); 
            }

            Logger.Log(message);
        }

        public void SendLast(string id)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var message in _lastMessages)
            {
                if (message != "")
                    builder.AppendLine(message);
            }

            Client client = _clients.FirstOrDefault(x => x.Id == id);
            byte[] data = Encoding.Unicode.GetBytes(builder.ToString());

            if (client != null)
            {
                client.Stream.Write(data, 0, data.Length);
                Logger.Log(String.Format("Last messages sent to client {0}", id));
            }
            else
                Logger.Log(String.Format("Id {0} not found", id));
        }

        public void Disconnect()
        {
            _tcpListener.Stop();

            for (int i = 0; i < _clients.Count; i++)
            {
                _clients[i].Close();
            }
            Environment.Exit(0);
        }
    }
}
