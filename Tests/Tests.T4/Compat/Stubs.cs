#if !NET8_0_OR_GREATER
// workaround to make project compile

namespace System.Net
{
	public readonly struct IPNetwork;
}

namespace System
{
	public readonly struct DateOnly;
	public readonly struct TimeOnly;
}

#endif
