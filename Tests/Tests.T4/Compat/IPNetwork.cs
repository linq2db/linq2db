#if !NET8_0_OR_GREATER

namespace System.Net
{
	// workaround to make project compile
	public readonly struct IPNetwork
	{
	}
}
