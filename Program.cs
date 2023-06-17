using System.Net;
using System.Text;
using System.Text.Json;
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
			listener.Prefixes.Add("http://localhost:8000/");
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

		private void ProcessRequest(HttpListenerContext context)
		{
			AddFingerprint fp = new AddFingerprint();
			
			string clientAddress = context.Request.RemoteEndPoint.ToString();
			Console.WriteLine($"Client connected: {clientAddress}");

			// Set CORS headers
			context.Response.AddHeader("Access-Control-Allow-Origin", "*");

			// Set content type to JSON
			context.Response.ContentType = "application/json";

			// Create and serialize response message
			 var response = new Response
			{
				status = 200,
				sucess = true,
				massage = "Fingerprint Detected!",
				FpImage = fp.ScannFIngerPrint()
			};
			var json = JsonSerializer.Serialize(response);


			// Write response to output stream
			byte[] buffer = Encoding.UTF8.GetBytes(json);
			context.Response.OutputStream.Write(buffer, 0, buffer.Length);

			// Close output stream and context
			context.Response.OutputStream.Close();
			context.Response.Close();

			Console.WriteLine($"Sent JSON response to {clientAddress}");
			Console.WriteLine($"Client disconnected: {clientAddress}");
		}
   
	
	public class AddFingerprint 
		{
		string FingerPrint;
		bool fingerpint = false;
		public string ? ScannFIngerPrint() 
			{
				var device = new DeviceAccessor().AccessFingerprintDevice();
				ManualResetEvent fingerprintDetectedEvent = new ManualResetEvent(false);
						
			device.FingerDetected += (sender, args) => {
					FingerPrint = HandleNewFingerprint(device.ReadFingerprint());
					 fingerprintDetectedEvent.Set();
				
			};
				device.StartFingerDetection();
				Output.WriteLine("Please place your finger on the device or press enter to cancel");
		   if (fingerprintDetectedEvent.WaitOne(10000)) {
					 Output.WriteLine("Please place your finger on the device...nbnbnv");
			   } 
		   else {
			 Output.WriteLine("Please place your finger on the device... time out");
			 }
				device.Dispose();
		 return FingerPrint;
							}

			private string ? HandleNewFingerprint(Bitmap bitmap)
			{
				byte[] imageData;
				string base64String=null;
				 using (MemoryStream ms = new MemoryStream())
				{
					bitmap.Save(ms, ImageFormat.Png); // save the bitmap to a memory stream as a PNG image
					imageData = ms.ToArray(); // get the bytes from the memory stream
				    base64String = Convert.ToBase64String(imageData);
				}
				 Output.WriteLine(ConsoleColor.DarkGreen, "Fingerprint registered");
			   return base64String;
		   
			}
			
		}
	
	
	
	}
}