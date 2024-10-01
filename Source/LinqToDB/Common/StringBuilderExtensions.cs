using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace LinqToDB.Common
{
	public static class StringBuilderExtensions
	{
		private static readonly uint[] _lookup32 = CreateLookup32();

		private static uint[] CreateLookup32()
		{
			var result = new uint[256];
			for (int i = 0; i < 256; i++)
			{
				string s = i.ToString("X2", NumberFormatInfo.InvariantInfo);
				result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
			}

			return result;
		}

		/// <summary>
		/// Appends an array of bytes to a <see cref="StringBuilder"/> in hex (i.e. 255->FF)
		/// format utilizing a static lookup table to minimize allocations.
		/// </summary>
		/// <param name="sb">The <see cref="StringBuilder"/> to append to</param>
		/// <param name="bytes">The byte array to append in hex</param>
		/// <remarks>
		/// The implementation here was chosen based on:
		/// https://stackoverflow.com/a/624379/2937845
		/// Which indicated that  https://stackoverflow.com/a/24343727/2937845's
		/// implementation of ByteArrayToHexViaLookup32 was the fastest method
		/// not involving unsafe
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StringBuilder AppendByteArrayAsHexViaLookup32(this StringBuilder sb, byte[] bytes)
		{
			var lookup32 = _lookup32;

			foreach (var b in bytes)
			{
				var val = lookup32[b];
				sb.Append((char)val);
				sb.Append((char) (val >> 16));
			}

			return sb;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StringBuilder AppendByteAsHexViaLookup32(this StringBuilder sb, byte @byte)
		{
			var val = _lookup32[@byte];
			sb.Append((char)val);
			sb.Append((char)(val >> 16));
			return sb;
		}
	}
}
