using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ChatServer
{
    internal class Program
    {
        static Socket server;
        static List<Socket> Clients = new List<Socket>();
        static AsyncLock clientLock = new AsyncLock();

        static async Task Main(string[] args)
        {
            // Assuming database is a static class with necessary methods
            database.DatabaseConnect();

            await CreateServer();

            // Keep the server running
            await Task.Delay(-1);
        }

        static async Task CreateServer()
        {
            var hostName = Dns.GetHostName();
            IPHostEntry localhost = await Dns.GetHostEntryAsync(hostName);

            // Use the first IPv4 address found
            IPAddress localIpAddress = localhost.AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            IPEndPoint ip = new IPEndPoint(localIpAddress, 1337);

            // Call the AcceptClients method which will await the next Client infinitely
            _ = AcceptClients(); // Fire-and-forget
        }

        static async Task AcceptClients()
        {
            while (true)
            {
                try
                {
                    Socket handler = await server.AcceptAsync();
                    using (await clientLock.LockAsync())
                    {
                        Clients.Add(handler);
                    }
                    Console.WriteLine($"Client connected: {handler.RemoteEndPoint}");
                    _ = Communicate(handler); // Fire-and-forget
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in AcceptClients: {ex.Message}");
                }
            }
        }

        static async Task Communicate(Socket handler)
        {
            var buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0)
                    {
                        // Connection closed
                        break;
                    }

                    var messageString = Encoding.UTF8.GetString(buffer, 0, received);
                    Console.WriteLine($"Received from client {handler.RemoteEndPoint}: {messageString}");

                    // Store message in the database
                    getMessage(messageString);

                    // Broadcast the message to other clients
                    await sendMessage(messageString, handler);

                    var response = "Message received!";
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await handler.SendAsync(responseBytes, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in Communicate: {ex.Message}");
                    break;
                }
            }

            // Cleanup after client disconnects
            Console.WriteLine($"Client disconnected: {handler.RemoteEndPoint}");
            try
            {
                handler.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException on Shutdown: {ex.Message}");
            }
            handler.Close();
            using (await clientLock.LockAsync())
            {
                Clients.Remove(handler);
            }
        }

        public static async Task sendMessage(string message, Socket sender)
        {
            var responseBytes = Encoding.UTF8.GetBytes(message);

            List<Socket> clientsToRemove = new List<Socket>();

            using (await clientLock.LockAsync())
            {
                foreach (Socket s in Clients)
                {
                    if (s != sender)
                    {
                        try
                        {
                            Console.WriteLine($"Sending message to client {s.RemoteEndPoint}");
                            await s.SendAsync(responseBytes, SocketFlags.None);
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"SocketException: {ex.Message}");
                            clientsToRemove.Add(s);
                        }
                        catch (ObjectDisposedException ex)
                        {
                            Console.WriteLine($"ObjectDisposedException: {ex.Message}");
                            clientsToRemove.Add(s);
                        }
                    }
                }

                foreach (var client in clientsToRemove)
                {
                    Clients.Remove(client);
                }
            }
        }

        public static void getMessage(string message)
        {
            string[] splittedMessage = message.Split(';');
            string messageString = splittedMessage[1] += splittedMessage[2];
            database.putGRPInDB(splittedMessage[0], messageString);

        }
    }
}
