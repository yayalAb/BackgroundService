using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Drawing;
using EasyConsole; 
using Futronic.Devices.FS26;
using System.Drawing.Imaging;
using BackgroundServiceResponse;

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
					services.AddHostedService<HttpServer>();
				});
	}

    public class TcpServer : BackgroundService
    {
        private TcpListener listener;
        private int port = 8000; // replace with your desired port number
        private string logFilePath = @"C:\MyLogs\MyBackgroundService.log"; // replace with your desired log file path
        private readonly ILogger<TcpServer> logger;

        public TcpServer(ILogger<TcpServer> logger)
        {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            logger.LogInformation("Listener started");
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    ProcessClient(client);
                }
            }
            catch (OperationCanceledException)
            {
                // Do nothing - service is stopping
            }

            listener.Stop();
            logger.LogInformation("Listener stopped");
        }

        private void ProcessClient(TcpClient client)
        {
            string clientAddress = client.Client.RemoteEndPoint.ToString();
            logger.LogInformation($"Client connected: {clientAddress}");

            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead = 0;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    logger.LogInformation($"Received data from {clientAddress}: {data}");
                }
            }

            logger.LogInformation($"Client disconnected: {clientAddress}");
        }
    }
}

