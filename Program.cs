using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyBackgroundService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<TcpServer>();
                });
    }

    public class TcpServer : BackgroundService
    {
        private TcpListener listener;
        private int port = 8000; // replace with your desired port number
        private string logFilePath = @"C:\MyLogs\MyBackgroundService.log"; // replace with your desired log file path

        public TcpServer()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Listener started ");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Listening on port {port}");
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    ProcessClient(client);
                }
            }
            catch (OperationCanceledException)
            {
                // Do nothing - service is stopping
            }

            listener.Stop();
        }

        private void ProcessClient(TcpClient client)
        {
            string clientAddress = client.Client.RemoteEndPoint.ToString();
            Console.WriteLine($"Client connected: {clientAddress}");
            string statusLine = "HTTP/1.1 200 OK\r\n";
            string corsHeaders = "Access-Control-Allow-Origin: *\r\n" +
                       "Access-Control-Allow-Headers: Content-Type\r\n" +
                       "Access-Control-Allow-Methods: GET, POST, PUT, DELETE\r\n";
            string headers = "Content-Type: application/json\r\n" + corsHeaders;
            var message = new
            {
                en = "Hello World",
                am = "Hello World",
            };
            var json = JsonSerializer.Serialize(message);
            string response = statusLine + headers + "\r\n" + json;
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = Encoding.ASCII.GetBytes(response);
                stream.Write(buffer, 0, buffer.Length);
            }

            Console.WriteLine($"Sent JSON response to {clientAddress}");
            Console.WriteLine($"Client disconnected: {clientAddress}");
        }
    }
}

