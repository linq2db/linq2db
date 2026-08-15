namespace LinqToDB.LINQPad;

/// <summary>
/// Host operating system checks. LINQPad 5 (net472 build) runs on Windows only and has no <see cref="System.OperatingSystem"/> helpers.
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
