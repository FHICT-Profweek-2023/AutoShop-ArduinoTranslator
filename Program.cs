using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ArduinoTranslator;

internal static class Program
{
    // Set the host and port to listen on
    private static IPAddress? _host; // Listen on all available interfaces
    private static int        _port; // Choose a port number
                                     
    // API Server url
    private static Uri? _apiServerUri;

    //private static int _count = 0;

    private static void Main() => Start().GetAwaiter().GetResult();

    private static async Task Start()
    {
        _ = IPAddress.TryParse(ConfigurationManager.AppSettings.Get("ArduinoIp"), out _host);
        _ = int.TryParse(ConfigurationManager.AppSettings.Get("ArduinoPort"), out _port);
        _ = Uri.TryCreate(ConfigurationManager.AppSettings.Get("ApiUri"), UriKind.Absolute, out _apiServerUri);

        var wrongIp = new IPAddress(new[] { byte.MinValue, byte.MinValue, byte.MinValue, byte.MinValue });

        if (_host is null || _host.Equals(wrongIp))
        {
            Console.WriteLine("IP address not set!\nSet IP address and restart program.");
            await Task.Delay(-1);
            return;
        }

        if (_port <= 0)
        {
            Console.WriteLine("Port is not valid or not set!\nSet port and restart program.");
            await Task.Delay(-1);
            return;
        }

        if (_apiServerUri is null)
        {
            Console.WriteLine("Api server address is not valid or not set!\nSet address and restart program.");
            await Task.Delay(-1);
            return;
        }

        // Create an IP endpoint
        var endPoint = new IPEndPoint(_host, _port);

        // Create a TCP listener
        var listener = new TcpListener(endPoint);

        // Start listening for incoming connections
        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Details:\n{ex}\n\nSimple Message:\n{ex.Message}");
            await Task.Delay(-1);
            return;
        }

        Console.WriteLine($"Listening on {_host}:{_port}...");

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

        var response = await httpClient.PutAsync(_apiServerUri, content);
        response.EnsureSuccessStatusCode();

        Console.WriteLine($"Put call made with: {content}");
    }

    private static async Task<string> MakeGetApiCall()
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync(_apiServerUri);
        response.EnsureSuccessStatusCode();

        Console.WriteLine($"Get call made with: {await response.Content.ReadAsStringAsync()}");

        return await response.Content.ReadAsStringAsync();
    }
}