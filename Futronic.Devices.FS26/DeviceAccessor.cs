using System;

namespace Futronic.Devices.FS26
{
	public class DeviceAccessor
	{
		public FingerprintDevice AccessFingerprintDevice()
		{
			Console.WriteLine("Connecting Device");
			var handle = LibScanApi.ftrScanOpenDevice();

			if (handle != IntPtr.Zero)
			{
				Console.WriteLine("Connected");
				return new FingerprintDevice(handle);
			}

			throw new Exception("Cannot open device, please  check you USB ");
		}
	}
}