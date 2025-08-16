#if NETFRAMEWORK
namespace System.Text;

internal static class StringBuilderExtensions
{
	public static StringBuilder Append(
		this StringBuilder sb,
		IFormatProvider? provider,
		FormattableString formattableString)
	{
		sb.Append(formattableString.ToString(provider));
		return sb;
	}

	public static StringBuilder AppendLine(
		this StringBuilder sb,
		IFormatProvider? provider,
		FormattableString formattableString)
	{
		sb.AppendLine(formattableString.ToString(provider));
		return sb;
	}
}
#endif
