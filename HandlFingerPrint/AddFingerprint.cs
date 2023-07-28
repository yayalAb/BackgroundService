using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using EasyConsole; 
using Futronic.Devices.FS26;
using System.Drawing.Imaging;
using SourceAFIS.Simple;

namespace FpBackgroundService.HandlFingerPrint
{
	public class AddFingerprint
	{
		string FingerPrint;
		public Bitmap[] AddedFingerPrints;
		private AfisEngine _afis;
		bool fingerpint = false;
		string massage = "Fingerprint Detetcted!";
		public async Task<(string?, string)> ScannFIngerPrint(string userId, string Index, bool IsNewUser)
		{
			var device = new DeviceAccessor().AccessFingerprintDevice();
			ManualResetEvent fingerprintDetectedEvent = new ManualResetEvent(false);
			Bitmap bitmapFingerprint = null;
			device.SwitchLedState(true, false);
			device.FingerDetected += (sender, args) =>
			{
				FingerPrint = HandleNewFingerprint(bitmapFingerprint = device.ReadFingerprint());
				fingerprintDetectedEvent.Set();
			};
			device.StartFingerDetection();
			Output.WriteLine("Please place your finger on the device or press enter to cancel");
			if (fingerprintDetectedEvent.WaitOne(10000))
			{	if(!device.IsFingerPresent)	
					{
					Output.WriteLine("waiting .......");	
					}		
			}
			else
			{
				massage = "Connection time out";
				Output.WriteLine("Connection time out");
			}
			// device.SwitchLedState(false, true);
			 device.Dispose();
			 Output.WriteLine("Validating  Fingerprint ..... ");
			if (bitmapFingerprint != null && (bitmapFingerprint is Bitmap))
			{
				await ValidateFingerprint(bitmapFingerprint, userId, Index, IsNewUser);
			}
			return (FingerPrint, massage);
		}
		private string? HandleNewFingerprint(Bitmap bitmap)
		{
			byte[] imageData;
			string base64String = null;
			using (MemoryStream ms = new MemoryStream())
			{
				bitmap.Save(ms, ImageFormat.Png); // save the bitmap to a memory stream as a PNG image
				imageData = ms.ToArray(); // get the bytes from the memory stream
				base64String = Convert.ToBase64String(imageData);
			}
			Output.WriteLine(ConsoleColor.DarkGreen, "Fingerprint registered");
			massage = "Fingerprint registered";
			return base64String;

		}

		public async Task ValidateFingerprint(Bitmap bitmap, string Id, string index, bool isNewuser)
		{

			if (File.Exists("Tempdata/" + Id + index+".bmp"))
			{
				File.Delete("Tempdata/" + Id + index+".bmp");
			}
			if (isNewuser)
			{
				IsNewUser();
			}
			_afis = new AfisEngine();
			var allFingers = new List<Person>();
			var allBitmaps = Directory.GetFiles("Tempdata", "*.bmp", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
			int i = 0;
			if(allBitmaps.FirstOrDefault() !=null)
			{
			 foreach (var bitmapFile in allBitmaps)
			{
				var person = new Person();
				person.Id = i++;
				var fingerprintId = Path.GetFileNameWithoutExtension(bitmapFile);
				var patternFile = $"{fingerprintId}.min";
				Bitmap bitmap1 = new Bitmap(Path.Combine("Tempdata", bitmapFile));
				Fingerprint fp = new Fingerprint();
				fp.AsBitmap = bitmap1;
				person.Fingerprints.Add(fp);
				_afis.Extract(person);
				allFingers.Add(person);
			}
			}
			
			var newFinger = new Person();
			var fingerprint = new Fingerprint();
			fingerprint.AsBitmap = bitmap;
			newFinger.Fingerprints.Add(fingerprint);
			_afis.Extract(newFinger);
			var matches = _afis.Identify(newFinger, allFingers);
			var persons = matches as Person[] ?? matches.ToArray();
			foreach (var person in persons)
			{
				var personId = person.Id;
				massage = $"Duplicate Finger Enrolled with index {personId}!";
				Output.WriteLine(ConsoleColor.DarkRed, $"Duplicate Finger Enrolled with index {personId}!");
			}

			if (!persons.Any())
			{  try
			{
				string fileName = Id + index;
                Console.WriteLine("file name : {0}",fileName);
                string randomFilename = fileName + ".bmp";
				if (File.Exists("Tempdata/" + randomFilename))
				{
					File.Delete("Tempdata/" + randomFilename);
				}
				Console.WriteLine("trying to save file {0}", randomFilename);
				if(!File.Exists("Tempdata/" + randomFilename))
				{
				   bitmap.Save(Path.Combine("Tempdata", randomFilename));
				}
				massage = "Fingerprint Enrolled Sucessfuly";
				Output.WriteLine(ConsoleColor.DarkGreen, "Fingerprint Enrolled Sucessfuly!");	
			}catch(SystemException ex)
			{
				throw new Exception("System Exception !");
			}
			catch(Exception ex)
			{
					throw new Exception("Error on saving");
				}
				
			}
		}

		public void IsNewUser()
		{
			if (!Directory.Exists("Tempdata"))
			{
				Directory.CreateDirectory("Tempdata");
			}  
			try
			{
			string[] files = Directory.GetFiles("Tempdata");
			foreach (string file in files)
			{
				try
				{
					File.Delete(file);
				}
				catch (IOException)
				{
					// The file is being used by another process, wait for a short period of time and try again
					Thread.Sleep(500);
					try
					{
						File.Delete(file);
					}
					catch (IOException ex)
					{
						// Handle the exception if the file is still being used after waiting
						Console.WriteLine($"Failed to delete file: {ex.Message}");

					}
				}
			}
			} catch(Exception ex) 
			{
				throw new Exception("Empty file");
			} 

			

		}
	}
}
