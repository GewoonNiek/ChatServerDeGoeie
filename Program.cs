using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static Socket server;
    static List<Socket> Clients = new List<Socket>();

    static async Task Main(string[] args)
    {
        await CreateServer();

        // Keep the server running
        await Task.Delay(-1);
    }

    static async Task CreateServer()
    {
        var hostName = Dns.GetHostName();
        IPHostEntry localhost = await Dns.GetHostEntryAsync(hostName);
        IPAddress localIpAddress = localhost.AddressList[0];

        IPEndPoint ip = new IPEndPoint(localIpAddress, 1337);

        server = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        server.Bind(ip);
        server.Listen();
        Console.WriteLine("Server started listening on port: 1337");

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
                Clients.Add(handler);
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
                Console.WriteLine("Client sent: " + messageString);

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
        handler.Shutdown(SocketShutdown.Both);
        handler.Close();
        Clients.Remove(handler);
    }
}
