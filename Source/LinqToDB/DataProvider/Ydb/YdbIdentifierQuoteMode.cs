using System;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Defines how to quote identifiers (such as table names, column names, etc.) 
	/// when generating YQL/SQL for YDB.
	/// </summary>
	public enum YdbIdentifierQuoteMode
	{
		/// <summary>
		/// Never quote identifiers.<br/>
		/// Use this mode only if you are certain that all names:
		/// <list type="bullet">
		/// <item>are not reserved keywords;</item>
		/// <item>start with a letter or an underscore;</item>
		/// <item>consist only of Latin letters, digits, or underscores.</item>
		/// </list>
		/// Violating these rules will cause a YDB syntax parser error.
		/// </summary>
		None,

		/// <summary>
		/// Always quote identifiers.<br/>
		/// In the standard YDB mode, backticks are used (e.g., <c>`id`</c>),<br/>
		/// while in ANSI SQL mode (<c>--!ansi_lexer</c>), double quotes are used (e.g., <c>"id"</c>).
		/// </summary>
		Quote,

		/// <summary>
		/// Quote identifiers only when required by YDB rules:
		/// <list type="bullet">
		/// <item>the identifier matches a reserved keyword;</item>
		/// <item>contains spaces, a slash character, or other invalid symbols;</item>
		/// <item>starts with a digit or contains sequences not allowed without quotes.</item>
		/// </list>
		/// </summary>
		Needed,

		/// <summary>
		/// "Smart" mode: quote when necessary (see <see cref="Needed"/>).<br/>
		/// For YDB, this mode behaves identically to <see cref="Needed"/>, 
		/// since case sensitivity always applies and does not affect quoting requirements.
		/// </summary>
		Auto
	}
}
