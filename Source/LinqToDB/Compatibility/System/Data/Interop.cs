using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal partial class Interop
{
	internal static unsafe void GetRandomBytes(byte* buffer, int length)
	{
		//if (!LocalAppContextSwitches.UseNonRandomizedHashSeed)
		{
			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				byte[] tmp = new byte[length];
				rng.GetBytes(tmp);
				Marshal.Copy(tmp, 0, (IntPtr)buffer, length);
			}
		}
	}
}
