using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BackgroundServiceResponse;
using FpBackgroundService.HandlFingerPrint;
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

	public class HttpServer : BackgroundService
	{
		private HttpListener listener;
		private int port = 8000; // replace with your desired port number
		private string logFilePath = @"C:\MyLogs\MyBackgroundService.log"; // replace with your desired log file path

		public HttpServer()
		{
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			listener = new HttpListener();
			listener.Prefixes.Add("http://192.168.1.11:8000/");
			listener.Start();
			Console.WriteLine("Listener started ");
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					Console.WriteLine($"Listening on port {port}");
					HttpListenerContext context = await listener.GetContextAsync();
					ProcessRequest(context);
				}
			}
			catch (OperationCanceledException)
			{
				// Do nothing - service is stopping
			}
			listener.Stop();
		}

		private async void ProcessRequest(HttpListenerContext context)
		{
			AddFingerprint fp = new AddFingerprint();
			string accessToken = "";
			string authorizationHeader = context.Request.Headers["Authorization"];
			if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
			{
				accessToken = authorizationHeader.Substring("Bearer ".Length);
			}

			string clientAddress = context.Request.RemoteEndPoint.ToString();
			context.Response.AddHeader("Access-Control-Allow-Origin", "*");
			context.Response.ContentType = "application/json, text/plain, */*";
			context.Response.AddHeader("Access-Control-Allow-Methods", "GET");
			context.Response.AddHeader("Access-Control-Allow-Headers", "*");
			context.Response.AddHeader("lang", "en");
			context.Response.AddHeader("Authorization", accessToken);
			context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
			var queryParams = context.Request.QueryString;
			string UserId = queryParams.Get("Id");
			string Index = queryParams.Get("Index");
			bool isNewUser=false;
			bool CheckDuplication = false;
			bool idDesposRequest = false;
			
			if(queryParams?.Get("isNewUser")=="1")
			{
				isNewUser = true;
			}
			if(queryParams?.Get("WithCheck")=="1")
			{
				CheckDuplication = true;
			}
			if(queryParams?.Get("IsDespose")=="1")
			{
				idDesposRequest = true;
			}
			var response = new Response
			{
				statusCode = 200,
				success = true,
				message = "Id And Index must not be null",
				// FpImage = fp.ScannFIngerPrint()
			};
			if (context.Request.HttpMethod != "OPTIONS"&&(!string.IsNullOrEmpty(Index)&&(!string.IsNullOrEmpty(Index))))
			{
				(string, string) result = await fp.ScannFIngerPrint(UserId, Index, isNewUser,CheckDuplication,idDesposRequest);
				response = new Response
				{
					statusCode = 200,
					success = true,
					message = result.Item2,
					FpImage = result.Item1
				};
			}
			var json = JsonSerializer.Serialize(response);


			// Write response to output stream
			int retries = 3;
			bool success = false;
			while (!success && retries > 0)
			{
				try
				{
					byte[] buffer = Encoding.UTF8.GetBytes(json);

					await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
					// Close output stream and context
					context.Response.OutputStream.Close();
					context.Response.Close();
					success = true;
				}
				catch (IOException ex)
				{
					Console.WriteLine($"IO Exception occurred: {ex.Message}");
				}
				catch (HttpListenerException ex)
				{
					if (ex.ErrorCode == 64)
					{
						Console.WriteLine("Network name is no longer available");
						retries--;
						if (retries == 0)
						{
							throw new Exception("Network name is no longer available!");
						}
						else
						{
							System.Threading.Thread.Sleep(1000);
						}
					}
					else
					{
						throw;
					}
				}
				finally
				{
					Console.WriteLine($"Client disconnected: {clientAddress}");
				}
			}

		}

	}
}
