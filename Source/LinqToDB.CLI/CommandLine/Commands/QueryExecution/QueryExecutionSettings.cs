using System;
using System.IO;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Fully resolved query command settings passed to execution logic.
	/// </summary>
	/// <param name="Profile">Selected configuration profile name.</param>
	/// <param name="Provider">linq2db provider name.</param>
	/// <param name="ProviderLocation">
	/// Optional path to external provider assembly. Provider dependencies must be available next to it or through normal application probing.
	/// </param>
	/// <param name="User">Resolved user value used for connection string formatting and optional Windows impersonation.</param>
	/// <param name="Password">Resolved password value used for connection string formatting and optional Windows impersonation.</param>
	/// <param name="ConnectionString">
	/// Final database connection string after command line/profile merge and user/password formatting.
	/// </param>
	/// <param name="CommandTimeout">Optional query command timeout in seconds. Value <c>0</c> disables the option.</param>
	/// <param name="LockTimeout">Optional provider-specific lock wait timeout in seconds. Value <c>0</c> disables the option.</param>
	/// <param name="MaxRows">Maximum number of result rows to read. Value <c>0</c> disables the limit.</param>
	/// <param name="Output">Query output format.</param>
	/// <param name="OutputFile">Optional output file path. When not specified, output is written to stdout.</param>
	/// <param name="Overwrite">Allow replacing existing output file.</param>
	/// <param name="UnsafeSqlPolicy">Unsafe SQL execution policy resolved from configuration profiles.</param>
	/// <param name="AllowUnsafeSql">Command-line confirmation for unsafe SQL execution.</param>
	/// <param name="DiagnosticWriter">Diagnostic writer for non-result execution notices.</param>
	/// <param name="Impersonate">Run the database loop under resolved Windows <see cref="User"/>/<see cref="Password"/> credentials after file resources are opened.</param>
	/// <param name="ImpersonateMode">Windows logon mode used for impersonation.</param>
	/// <param name="Sql">Resolved SQL query text.</param>
	internal sealed record QueryExecutionSettings(
		string             Profile,
		string             Provider,
		string?            ProviderLocation,
		string?            User,
		string?            Password,
		string             ConnectionString,
		int?               CommandTimeout,
		int?               LockTimeout,
		int                MaxRows,
		string             Output,
		string?            OutputFile,
		bool               Overwrite,
		UnsafeSqlPolicy    UnsafeSqlPolicy,
		bool               AllowUnsafeSql,
		TextWriter         DiagnosticWriter,
		bool               Impersonate,
		WindowsImpersonationMode ImpersonateMode,
		string             Sql);
}
