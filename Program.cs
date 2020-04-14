using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Sockets {
    class Program {
        private static SocketHelper socket;

        static void Main(string[] args) {
            try {
                if (args.Length > 0 && args[0] == "server") {
                    Console.WriteLine("Starting server...");
                    Console.WriteLine("Press Ctrl+C to quit.\n");
                    socket = new SocketHelper(true);
                    BindIPPrompt();
                    Console.WriteLine("Waiting for connection...");
                    socket.WaitForConnection();
                    Console.WriteLine("Connection established! Press Ctrl+C to quit.");
                    while (socket.IsConnected) {
                        Console.WriteLine("Waiting for message...");
                        socket.Receive();
                        Console.WriteLine("\t{0} says: {1}", socket.RemoteEndpoint, socket.Message);
                        Console.Write("Type a return message: ");
                        socket.Message = Console.ReadLine();
                        Console.WriteLine("\t{0} (you) says: {1}", socket.LocalEndpoint, socket.Message);
                        socket.Send();
                    }
                } else {
                    socket = new SocketHelper(false);
                    ConnectToIPPrompt();
                    Console.WriteLine("Connection established! Press Ctrl+C to quit.");
                    while (socket.IsConnected) {
                        Console.Write("Type a return message: ");
                        socket.Message = Console.ReadLine();
                        Console.WriteLine("\t{0} (you) says: {1}", socket.LocalEndpoint, socket.Message);
                        socket.Send();
                        Console.WriteLine("Waiting for message...");
                        socket.Receive();
                        Console.WriteLine("\t{0} says: {1}", socket.RemoteEndpoint, socket.Message);
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
            }
        }

        private static void BindIPPrompt() {
            var ips = socket.GetIPAddresses();

            Console.Clear();
            Console.WriteLine("IP Addresses available:");
            for (var i = 0; i < ips.Count; i++) {
                Console.WriteLine("\t[{0}]: {1}", i, ips[i]);
            }
            int index;
            bool valid;

            do {
                Console.Write("Bind to which IP? Enter index: ");
                var input = Console.ReadLine();

                valid = int.TryParse(input, out index);
                if (index >= ips.Count) {
                    valid = false;
                }
            } while (!valid);
            try {
                socket.BindToAddress(ips[index]);
            } catch (ArgumentException e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to try again or Ctrl+C to quit...");
                Console.ReadLine();
                // recursively try again
                BindIPPrompt();
            }
        }

        private static void ConnectToIPPrompt() {
            Console.Clear();
            Console.Write("Enter IP Address to connect to: ");
            var input = Console.ReadLine();

            try {
                socket.EstablishConnection(input);
            } catch (ArgumentException e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to try again or Ctrl+C to quit...");
                Console.ReadLine();
                // recursively try again
                ConnectToIPPrompt();
            }
        }
    }
}
