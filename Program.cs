using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ArduinoTranslator;

internal static class Program
{
    // API Server url
    private static readonly Uri Uri = new("http://192.168.160.53/api/controls/0");

    // Set the host and port to listen on
//#error Enter IP Adress (comment this line when entered) !!!
#warning Check IP Adress
    private const string Host = "0.0.0.0"; // Listen on all available interfaces
    private const int    Port = 2000;      // Choose a port number

    //private static int _count = 0;

    private static void Main() => Start().GetAwaiter().GetResult();

    private static async Task Start()
    {
        // Create an IP endpoint
        var endPoint = new IPEndPoint(IPAddress.Parse(Host), Port);

        // Create a TCP listener
        var listener = new TcpListener(endPoint);

        // Start listening for incoming connections
        listener.Start();
        Console.WriteLine($"Listening on {Host}:{Port}...");

        // ReSharper disable once AsyncVoidLambda

        while (true)
        {
            // Wait for a connection
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"Accepted connection from {(client.Client.RemoteEndPoint as IPEndPoint)?.Address}");

            var stream = client.GetStream();

            try
            {
                // Receive data from the client (assuming string data)
                var buffer    = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var data      = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // Example of input: "put {\"id\":0,\"next_product\":null,\"current_position\":0,\"halt\":false}" or "get"
                //var data = "put {\"id\":0,\"next_product\":null,\"current_position\":0,\"halt\":false}";

                if (!string.IsNullOrEmpty(data))
                {
                    Console.WriteLine($"Received data: {data}");

                    if (data.ToLower().StartsWith("get"))
                    {
                        // Send a response back to the client (optional)
                        var response      = await MakeGetApiCall();
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                    }

                    if (data.ToLower().StartsWith("put"))
                    {
                        data = data.Remove(0, 3);
                        Console.WriteLine(data);
                        await MakePutApiCall(data);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing data: {e.Message}");
            }
            finally
            {
                // Close the connection
                client.Close();
                Console.WriteLine($"Connection with {(client.Client.RemoteEndPoint as IPEndPoint)?.Address} closed");
            }
        }
    }

    private static async Task MakePutApiCall(string data)
    {
        var content = new StringContent(data, Encoding.UTF8, "application/json");

        using var httpClient = new HttpClient();

        var response = await httpClient.PutAsync(Uri, content);
        response.EnsureSuccessStatusCode();

        Console.WriteLine($"Put call made with: {content}");
    }

    private static async Task<string> MakeGetApiCall()
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync(Uri);
        response.EnsureSuccessStatusCode();

        Console.WriteLine($"Get call made with: {await response.Content.ReadAsStringAsync()}");

        return await response.Content.ReadAsStringAsync();
    }
}