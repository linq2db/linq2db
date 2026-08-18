using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

using LINQPad.Extensibility.DataContext;

namespace LinqToDB.LINQPad;

// the one place allowed to use MessageBox, see BannedSymbols.txt
#pragma warning disable RS0030 // Do not use banned APIs
internal static class Notification
{
	/// <summary>
	/// Log file (in LINQPad's log folder) all driver errors are written to.
	/// </summary>
	public const string LogFileName = "linq2db.LINQPad.log";

	// On macOS LINQPad renders the connection dialog through Avalonia XPF, and XPF is available only for the
	// duration of the ShowConnectionDialog call - touching a WPF type anywhere else fails to load
	// PresentationFramework, which then masks the error being reported. Outside the dialog we log only.
#if NETFRAMEWORK
	// LINQPad 5 is Windows-only, WPF is always available there
	public static void BeginConnectionDialog() { }
	public static void EndConnectionDialog  () { }

	private static bool CanShowMessageBox => true;
#else
	[ThreadStatic]
	private static bool _connectionDialogScope;

	public static void BeginConnectionDialog() => _connectionDialogScope = true;
	public static void EndConnectionDialog  () => _connectionDialogScope = false;

	private static bool CanShowMessageBox => _connectionDialogScope;
#endif

	public static void Error(Exception ex, string context, string title = "Error")
	{
		Log(ex, context);

		if (CanShowMessageBox)
			ShowError(Format(ex, context), title);
	}

	public static void Error(string message, string title = "Error")
	{
		Log(message, title);

		if (CanShowMessageBox)
			ShowError(message, title);
	}

	public static void Error(Window owner, string message, string title = "Error")
	{
		Log(message, title);

		MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	public static void Error(Window owner, Exception ex, string title = "Error")
	{
		Log(ex, title);

		MessageBox.Show(owner, FormatMessages(ex), title, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	/// <summary>
	/// Returns messages of the exception and all its inner exceptions: the message of a wrapper such as
	/// <see cref="System.Reflection.TargetInvocationException"/> says nothing about the actual failure.
	/// </summary>
	public static string FormatMessages(Exception ex)
	{
		var messages = new StringBuilder();

		for (var currEx = ex; currEx != null; currEx = currEx.InnerException)
		{
			if (messages.Length > 0)
				messages.AppendLine().AppendLine();

			messages.Append(currEx.Message);
		}

		return messages.ToString();
	}

	public static void Warning(Window owner, string message, string title = "Warning")
	{
		Log(message, title);

		MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
	}

	public static void Info(Window owner, string message, string title = "Information")
	{
		MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
	}

	public static bool YesNo(Window owner, string message, string title = "Information", MessageBoxImage icon = MessageBoxImage.Question)
	{
		return MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes;
	}

	// separate non-inlined method: WPF assemblies are resolved when a method referencing them is JIT-compiled,
	// so the MessageBox call must never share a method body with code reachable outside the connection dialog
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ShowError(string message, string title)
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	private static string Format(Exception ex, string context)
	{
		var error = new StringBuilder();

		error.AppendLine(context);

		for (var currEx = ex; currEx != null; currEx = currEx.InnerException)
		{
			error.AppendLine(currEx.Message);
			error.AppendLine(currEx.StackTrace);
		}

		return error.ToString();
	}

	// logging must never replace the error it reports
	private static void Log(Exception ex, string context)
	{
		try
		{
			DataContextDriver.WriteToLog(ex, LogFileName, context);
		}
		catch
		{
		}
	}

	private static void Log(string message, string title)
	{
		try
		{
			DataContextDriver.WriteToLog($"{title}: {message}", LogFileName);
		}
		catch
		{
		}
	}
}
#pragma warning restore RS0030 // Do not use banned APIs
