namespace LinqToDB.LINQPad;

/// <summary>
/// Host operating system checks. On the net472 (LINQPad 5) build they fold to constants, because LINQPad 5
/// runs on Windows only - <see cref="System.OperatingSystem"/>'s helpers are available there too, through
/// the Meziantou.Polyfill entries in Directory.Build.props, so the guard is a simplification.
/// </summary>
internal static class Platform
{
#if NETFRAMEWORK
	public static bool IsWindows => true;
	public static bool IsMacOS   => false;
#else
	public static bool IsWindows => System.OperatingSystem.IsWindows();
	public static bool IsMacOS   => System.OperatingSystem.IsMacOS();
#endif
}
