using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ArduinoTranslator;

internal static class Program
{
    // API Server url
    private static readonly Uri Uri = new ("http://192.168.160.53/controls/0");

    // Set the host and port to listen on
//#error Enter IP Adress (comment this line when entered) !!!
#warning Check IP Adress
    private const string Host = "192.168."; // Listen on all available interfaces
    private const int    Port = 2000;     // Choose a port number

    private static void Main()
    {
        // Create an IP endpoint
        var endPoint = new IPEndPoint(IPAddress.Parse(Host), Port);

        // Create a TCP listener
        var listener = new TcpListener(endPoint);

        // Start listening for incoming connections
        listener.Start();
        Console.WriteLine($"Listening on {Host}:{Port}...");

        // ReSharper disable once AsyncVoidLambda
        new Task(async () =>
        {
            while (true)
            {
                // Wait for a connection
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine(
                    $"Accepted connection from {(client.Client.RemoteEndPoint as IPEndPoint)?.Address}");

                var stream = client.GetStream();

                try
                {
                    // Receive data from the client (assuming string data)
                    //var buffer    = new byte[1024];
                    //var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    var data = "{{id:0, halt:0, next_product:0, current_position:0}}"; //Encoding.UTF8.GetString(buffer, 0, bytesRead);

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
                    Console.WriteLine(
                        $"Connection with {(client.Client.RemoteEndPoint as IPEndPoint)?.Address} closed");
                }
            }
        }).Start();
    }

    private static async Task MakePutApiCall(string data)
    {
        var content = new StringContent(data);

        using var httpClient = new HttpClient();

        var response = await httpClient.PutAsync(Uri, content);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> MakeGetApiCall()
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync(Uri);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}