using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sockets {
    /// <summary>
    /// The Socket Helper class for sending and receiving packets in the Tic Tac Toe Program
    /// <para>Author: Drew Cavanaugh, 2020</para>
    /// </summary>
    public class SocketHelper {
        public const ushort PORT = 11000;
        public const byte BUFFER = 32;
        public const string END_FLAG = "<END>";

        public bool IsHost { get; private set; }
        public bool IsConnected {
            get {
                return sender == null ? false : sender.Connected;
            }
        }
        public string RemoteEndpoint {
            get {
                return sender == null ? string.Empty : sender.RemoteEndPoint.ToString();
            }
        }
        public string LocalEndpoint {
            get {
                return sender == null ? string.Empty : sender.LocalEndPoint.ToString();
            }
        }
        public string Message { get; set; }

        private EndPoint endPoint;
        private Socket sender, listener;
        private byte[] buffer;

        /// <summary>
        /// Initializes a new SocketHelper class for Tic-Tac-Toe.
        /// </summary>
        /// <param name="isHost">Whether this instance is acting as the host (true) or client (false).</param>
        public SocketHelper(bool isHost) {
            IsHost = isHost;
            Message = string.Empty;
            buffer = new byte[BUFFER];
        }

        /// <summary>
        /// Returns a string List of all IP Addresses this SocketHelper can bind to.
        /// </summary>
        public List<string> GetIPAddresses() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addrList = new List<string>();

            foreach (var ip in host.AddressList) {
                addrList.Add(ip.ToString());
            }
            return addrList;
        }

        /// <summary>
        /// Host only: binds the application to the specified IP address and port.
        /// </summary>
        /// <param name="ip">The IP Address to bind to.</param>
        /// <exception cref="InvalidOperationException">If the specified IP is not available to be binded to.</exception>
        /// <exception cref="SocketException">If it is unable to bind to the IP and port.</exception>
        public void BindToAddress(string ip) {
            if (!IsHost) {
                // method is only for hosts
                throw new InvalidOperationException("This method should only be called by the host!");
            }
            if (IsConnected) {
                // can't bind to new address while we're connected
                throw new InvalidOperationException(string.Format("The socket is already connected to {0}!", sender.RemoteEndPoint.ToString()));
            }
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var valid = IPAddress.TryParse(ip, out var addr);

            if (!valid) {
                throw new ArgumentException(string.Format("{0} is not a valid IPv4 Address.", ip));
            }
            if (!host.AddressList.Any(a => a.Equals(addr))) {
                throw new InvalidOperationException(string.Format("Cannot bind to {0} because it is not reserved for this machine.", ip));
            }
            endPoint = new IPEndPoint(addr, PORT);
            listener = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(endPoint);
            listener.Listen(1);
        }

        /// <summary>
        /// HOST ONLY: Waits for a client to connect.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the socket connection is already established.</exception>
        public void WaitForConnection() {
            if (!IsHost) {
                // method is only for hosts
                throw new InvalidOperationException("This method should only be called by the host!");
            }
            if (IsConnected) {
                // can't wait for new connection while we're connected
                throw new InvalidOperationException(string.Format("The socket is already connected to {0}!", sender.RemoteEndPoint.ToString()));
            }
            while (!IsConnected) {
                // waiting for connection
                sender = listener.Accept();
            }
        }

        /// <summary>
        /// CLIENT ONLY: Connects to a host IP.
        /// </summary>
        /// <param name="ip">The IP Address of the Host.</param>
        /// <exception cref="InvalidOperationException">If the socket connection is already established.</exception>
        /// <exception cref="ArgumentException">If the IP parameter is not a valid IPv4 Address.</exception>
        public void EstablishConnection(string ip) {
            if (IsHost) {
                // method is only for clients
                throw new InvalidOperationException("This method should only be called by the client!");
            }
            if (IsConnected) {
                // can't establish new connection while we're connected
                throw new InvalidOperationException(string.Format("The socket is already connected to {0}", sender.RemoteEndPoint.ToString()));
            }
            var valid = IPAddress.TryParse(ip, out var addr);
            
            if (!valid) {
                throw new ArgumentException(string.Format("{0} is not a valid IPv4 Address.", ip));
            }
            endPoint = new IPEndPoint(addr, PORT);
            sender = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(endPoint);
        }

        /// <summary>
        /// Sends the data in Message field to the remote endpoint.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the socket connection is not established.</exception>
        /// <exception cref="NullReferenceException">If the Message field is null, empty, or white spaces.</exception>
        public void Send() {
            if (!IsConnected) {
                // cannot send message if there is no connection
                throw new InvalidOperationException(string.Format("The socket is not connected!"));
            }
            if (string.IsNullOrWhiteSpace(Message)) {
                throw new NullReferenceException(string.Format("Cannot send a null or empty Message!"));
            }
            var raw = Encoding.ASCII.GetBytes(string.Concat(Message, END_FLAG));

            sender.Send(raw);
        }

        /// <summary>
        /// Waits for the remote endpoint to receive a message and stores it in the Message field.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the socket connection is not established.</exception>
        public void Receive() {
            if (!IsConnected) {
                // cannot receive message if there is no connection
                throw new InvalidOperationException(string.Format("The socket is not connected!"));
            }
            Message = string.Empty;
            while (true) {
                var received = sender.Receive(buffer);

                Message = string.Concat(Message, Encoding.ASCII.GetString(buffer, 0, received));
                if (Message.EndsWith(END_FLAG)) {
                    Message = Message.Remove(Message.IndexOf(END_FLAG));
                    break;
                }
            }
        }

        /// <summary>
        /// Closes all socket connections and resets the class.
        /// </summary>
        public void CloseConnection() {
            if (IsConnected) {
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
            if (IsHost) {
                listener.Close();
            }
            Message = string.Empty;
        }
    }
}
