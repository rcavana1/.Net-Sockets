using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sockets {
    class Program {
        public static string data = null;

        static void Main(string[] args) {
            if (args.Length > 0 && args[0] == "server") {
                Console.WriteLine("Starting server...");
                Console.WriteLine("Press Ctrl+C to quit.\n");
                Listen();
            } else {
                Client();
            }
        }

        private static void Client() {
            // 1kb socket buffer
            var bytes = new byte[1024];
            // connect to remote server
            var ipHost = Dns.GetHostEntry(Dns.GetHostName());

            Console.WriteLine("Loopback address: {0}", ipHost.AddressList[0].ToString());
            IPAddress ipAddr;
            bool valid;

            do {
                Console.Write("Enter IP Address to connect to: ");
                var input = Console.ReadLine();

                valid = IPAddress.TryParse(input, out ipAddr);
            } while (!valid);
            var endPoint = new IPEndPoint(ipAddr, 11000);
            // create sender socket
            var sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try {
                sender.Connect(endPoint);
                // get client's name and send it to the server
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());
                Console.Write("Enter your name: ");
                var name = Console.ReadLine();
                // append <EOF> flag to mark the end of the message
                var msg = Encoding.ASCII.GetBytes(string.Concat(name, "<EOF>"));
                var sent = sender.Send(msg);

                Console.WriteLine("\tMessage sent: My name is {0}", name);
                // clear data string and wait for response
                data = null;
                while (true) {
                    var rec = sender.Receive(bytes);

                    data += Encoding.ASCII.GetString(bytes, 0, rec);
                    // <EOF> flag marks the end of the message, so break here and remove it from the final output
                    if (data.IndexOf("<EOF>") > -1) {
                        data = data.Remove(data.IndexOf("<EOF>"));
                        break;
                    }
                }
                Console.WriteLine("\tMessage received: {0}", data);
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            } catch (Exception e) {
                Console.WriteLine("Error! {0}", e.Message);
            }
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }

        private static void Listen() {
            // 1kb socket buffer
            var bytes = new byte[1024];
            // local server endpoint for this socket
            var ipHost = Dns.GetHostEntry(Dns.GetHostName());

            Console.WriteLine("IP Addresses available:");
            for (var i = 0; i < ipHost.AddressList.Length; i++) {
                Console.WriteLine("[{0}] {1}", i, ipHost.AddressList[i]);
            }
            int index;
            bool valid;
            do {
                Console.Write("Which index to bind to? ");
                var input = Console.ReadLine();

                valid = int.TryParse(input, out index);
                if (index > ipHost.AddressList.Length) {
                    valid = false;
                }
            } while (!valid);
            var ipAddr = ipHost.AddressList[index];
            var endPoint = new IPEndPoint(ipAddr, 11000);
            // socket listener
            var listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try {
                // bind to port and wait for client to send a message
                listener.Bind(endPoint);
                listener.Listen(10);
                Console.WriteLine("Listener bound to {0}", listener.LocalEndPoint.ToString());
                while (true) {
                    Console.WriteLine("Waiting for connection...");
                    var handler = listener.Accept();

                    Console.WriteLine("Connection established, awaiting message...");
                    data = null;
                    while (true) {
                        var rec = handler.Receive(bytes);

                        data += Encoding.ASCII.GetString(bytes, 0, rec);
                        // <EOF> flag marks the end of the message, so break here and remove it from the final output
                        if (data.IndexOf("<EOF>") > -1) {
                            data = data.Remove(data.IndexOf("<EOF>"));
                            break;
                        }
                    }
                    Console.WriteLine("\tMessage received: {0}", data);
                    // reply to the client with Hello, [name]
                    var reply = string.Format("Hello, {0}", data);
                    // append <EOF> flag to mark the end of the message
                    var msg = Encoding.ASCII.GetBytes(string.Concat(reply, "<EOF>"));

                    Console.WriteLine("\tMessage sent: {0}", reply);
                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            } catch (Exception e) {
                Console.WriteLine("Error! {0}", e.Message);
            }
        }
    }
}
