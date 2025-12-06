using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	[PublicAPI]
	public static class SqlFn
	{
		#region Configuration

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DBTS-transact-sql">@@DBTS (Transact-SQL)</see></b></para>
		/// <para>This function returns the value of the current timestamp data type for the current database. The current database will have a guaranteed unique timestamp value.</para>
		/// </summary>
		/// <returns>varbinary</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@DBTS", ServerSideOnly=true)]
		public static byte[] DbTS => throw new ServerSideOnlyException(nameof(DbTS));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LANGID-transact-sql">@@LANGID (Transact-SQL)</see></b></para>
		/// <para>Returns the local language identifier (ID) of the language that is currently being used.</para>
		/// </summary>
		/// <returns>smallint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@LANGID", ServerSideOnly=true)]
		public static short LangID => throw new ServerSideOnlyException(nameof(LangID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LANGUAGE-transact-sql">@@LANGUAGE (Transact-SQL)</see></b></para>
		/// <para>Returns the name of the language currently being used.</para>
		/// </summary>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@LANGUAGE", ServerSideOnly=true)]
		public static string Language => throw new ServerSideOnlyException(nameof(Language));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LOCK-TIMEOUT-transact-sql">@@LOCK_TIMEOUT (Transact-SQL)</see></b></para>
		/// <para>Returns the current lock time-out setting in milliseconds for the current session.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@LOCK_TIMEOUT", ServerSideOnly=true)]
		public static int LockTimeout => throw new ServerSideOnlyException(nameof(LockTimeout));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/MAX-CONNECTIONS-transact-sql">@@MAX_CONNECTIONS (Transact-SQL)</see></b></para>
		/// <para>Returns the maximum number of simultaneous user connections allowed on an instance of SQL Server.
		/// The number returned is not necessarily the number currently configured.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@MAX_CONNECTIONS", ServerSideOnly=true)]
		public static int MaxConnections => throw new ServerSideOnlyException(nameof(MaxConnections));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/MAX-PRECISION-transact-sql">@@MAX_PRECISION (Transact-SQL)</see></b></para>
		/// <para>Returns the precision level used by <b>decimal</b> and <b>numeric</b> data types as currently set in the server.</para>
		/// </summary>
		/// <returns>tinyint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[CLSCompliant(false)]
		[Sql.Expression(ProviderName.SqlServer, "@@MAX_PRECISION", ServerSideOnly=true)]
		public static byte MaxPrecision => throw new ServerSideOnlyException(nameof(MaxPrecision));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/NESTLEVEL-transact-sql">@@NESTLEVEL (Transact-SQL)</see></b></para>
		/// <para>Returns the nesting level of the current stored procedure execution (initially 0) on the local server.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@NESTLEVEL", ServerSideOnly=true)]
		[CLSCompliant(false)]
		public static int NestLevel => throw new ServerSideOnlyException(nameof(NestLevel));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OPTIONS-transact-sql">@@OPTIONS (Transact-SQL)</see></b></para>
		/// <para>Returns information about the current SET options.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@OPTIONS", ServerSideOnly=true)]
		public static int Options => throw new ServerSideOnlyException(nameof(Options));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/REMSERVER-transact-sql">@@REMSERVER (Transact-SQL)</see></b></para>
		/// <para>Returns the name of the remote SQL Server database server as it appears in the login record.</para>
		/// </summary>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@REMSERVER", ServerSideOnly=true)]
		public static string? RemServer => throw new ServerSideOnlyException(nameof(RemServer));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SERVERNAME-transact-sql">@@SERVERNAME (Transact-SQL)</see></b></para>
		/// <para>Returns the name of the local server that is running SQL Server.</para>
		/// </summary>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@SERVERNAME", ServerSideOnly=true)]
		public static string ServerName => throw new ServerSideOnlyException(nameof(ServerName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SERVICENAME-transact-sql">@@SERVICENAME (Transact-SQL)</see></b></para>
		/// <para>Returns the name of the registry key under which SQL Server is running. @@SERVICENAME returns 'MSSQLSERVER'
		/// if the current instance is the default instance; this function returns the instance name if the current instance is a named instance.</para>
		/// </summary>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@SERVICENAME", ServerSideOnly=true)]
		public static string ServiceName => throw new ServerSideOnlyException(nameof(ServiceName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SPID-transact-sql">@@SPID (Transact-SQL)</see></b></para>
		/// <para>Returns the session ID of the current user process.</para>
		/// </summary>
		/// <returns>smallint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@SPID", ServerSideOnly=true)]
		public static short SpID => throw new ServerSideOnlyException(nameof(SpID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TEXTSIZE-transact-sql">@@TEXTSIZE (Transact-SQL)</see></b></para>
		/// <para>Returns the current value of the <see href="https://docs.microsoft.com/en-us/sql/t-sql/statements/set-textsize-transact-sql">TEXTSIZE</see> option.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@TEXTSIZE", ServerSideOnly=true)]
		public static int TextSize => throw new ServerSideOnlyException(nameof(TextSize));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/version-transact-sql-configuration-functions">@@VERSION (Transact-SQL)</see></b></para>
		/// <para>Returns system and build information for the current installation of SQL Server.</para>
		/// </summary>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@VERSION", ServerSideOnly=true)]
		public static string Version => throw new ServerSideOnlyException(nameof(Version));

		#endregion

		#region Conversion

		sealed class DataTypeBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var dataType = builder.GetObjectValue("data_type");
				builder.AddFragment("data_type", dataType is SqlType ? string.Format(CultureInfo.InvariantCulture, "{0}", dataType) : ((Func<SqlType>)dataType)().ToString());
			}
		}

		#region Cast

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "CAST({expression} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Cast<T>([ExprParameter] object? expression, [SqlQueryDependent] SqlType<T> data_type)
			=> throw new ServerSideOnlyException(nameof(Cast));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "CAST({expression} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Cast<T>([ExprParameter] object? expression, [SqlQueryDependent] Func<SqlType<T>> data_type)
			=> throw new ServerSideOnlyException(nameof(Cast));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "CAST({0} as {1})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Cast<T>(object? expression)
			=> throw new ServerSideOnlyException(nameof(Cast));

		#endregion

		#region Convert

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "CONVERT({data_type}, {expression})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Convert<T>([SqlQueryDependent] SqlType<T> data_type, [ExprParameter] object? expression)
			=> throw new ServerSideOnlyException(nameof(Convert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "CONVERT({data_type}, {expression})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Convert<T>([SqlQueryDependent] Func<SqlType<T>> data_type, [ExprParameter] object? expression)
			=> throw new ServerSideOnlyException(nameof(Convert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "CONVERT({1}, {0})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Convert<T>([ExprParameter] object? expression)
			=> throw new ServerSideOnlyException(nameof(Convert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "CONVERT({data_type}, {expression}, {style})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Convert<T>([SqlQueryDependent] SqlType<T> data_type, [ExprParameter] object? expression, [SqlQueryDependent, ExprParameter] int style)
			=> throw new ServerSideOnlyException(nameof(Convert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "CONVERT({data_type}, {expression}, {style})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Convert<T>([SqlQueryDependent] Func<SqlType<T>> data_type, [ExprParameter] object? expression, [SqlQueryDependent, ExprParameter] int style)
			=> throw new ServerSideOnlyException(nameof(Convert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CAST-and-CONVERT-transact-sql">CAST and CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "CONVERT({2}, {0}, {1})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T Convert<T>([ExprParameter] object? expression, [SqlQueryDependent, ExprParameter] int style)
			=> throw new ServerSideOnlyException(nameof(Convert));

		#endregion

		#region Parse

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PARSE-transact-sql">PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "PARSE({string_value} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		public static T Parse<T>([ExprParameter] string string_value, [SqlQueryDependent] SqlType<T> data_type)
			=> throw new ServerSideOnlyException(nameof(Parse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PARSE-transact-sql">PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "PARSE({string_value} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		public static T Parse<T>([ExprParameter] string string_value, [SqlQueryDependent] Func<SqlType<T>> data_type)
			=> throw new ServerSideOnlyException(nameof(Parse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PARSE-transact-sql">PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "PARSE({0} as {1})", ServerSideOnly=true)]
		public static T Parse<T>([ExprParameter] string string_value)
			=> throw new ServerSideOnlyException(nameof(Parse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PARSE-transact-sql">PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <param name="culture">Optional string that identifies the culture in which string_value is formatted.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "PARSE({string_value} as {data_type} USING {culture})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		public static T Parse<T>([ExprParameter] string string_value, [SqlQueryDependent] SqlType<T> data_type, [ExprParameter, SqlQueryDependent] string culture)
			=> throw new ServerSideOnlyException(nameof(Parse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PARSE-transact-sql">PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <param name="culture">Optional string that identifies the culture in which string_value is formatted.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "PARSE({string_value} as {data_type} USING {culture})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		public static T Parse<T>([ExprParameter] string string_value, [SqlQueryDependent] Func<SqlType<T>> data_type, [ExprParameter, SqlQueryDependent] string culture)
			=> throw new ServerSideOnlyException(nameof(Parse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PARSE-transact-sql">PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="culture">Optional string that identifies the culture in which string_value is formatted.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "PARSE({0} as {2} USING {1})", ServerSideOnly=true)]
		public static T Parse<T>([ExprParameter] string string_value, [ExprParameter, SqlQueryDependent] string culture)
			=> throw new ServerSideOnlyException(nameof(Parse));

		#endregion

		#region TryCast

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CAST-transact-sql">TRY_CAST (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_CAST({expression} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryCast<T>([ExprParameter] object? expression, [SqlQueryDependent] SqlType<T> data_type)
			=> throw new ServerSideOnlyException(nameof(TryCast));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CAST-transact-sql">TRY_CAST (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_CAST({expression} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryCast<T>([ExprParameter] object? expression, [SqlQueryDependent] Func<SqlType<T>> data_type)
			=> throw new ServerSideOnlyException(nameof(TryCast));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CAST-transact-sql">TRY_CAST (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "TRY_CAST({0} as {1})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryCast<T>([ExprParameter] object? expression)
			=> throw new ServerSideOnlyException(nameof(TryCast));

		#endregion

		#region TryConvert

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CONVERT-transact-sql">TRY_CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_CONVERT({data_type}, {expression})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryConvert<T>([SqlQueryDependent] SqlType<T> data_type, [ExprParameter] object? expression)
			=> throw new ServerSideOnlyException(nameof(TryConvert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CONVERT-transact-sql">TRY_CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_CONVERT({data_type}, {expression})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryConvert<T>([SqlQueryDependent] Func<SqlType<T>> data_type, [ExprParameter] object? expression)
			=> throw new ServerSideOnlyException(nameof(TryConvert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CONVERT-transact-sql">TRY_CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "TRY_CONVERT({1}, {0})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryConvert<T>([ExprParameter] object? expression)
			=> throw new ServerSideOnlyException(nameof(TryConvert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CONVERT-transact-sql">TRY_CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_CONVERT({data_type}, {expression}, {style})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryConvert<T>([SqlQueryDependent] SqlType<T> data_type, [ExprParameter] object? expression, [SqlQueryDependent, ExprParameter] int style)
			=> throw new ServerSideOnlyException(nameof(TryConvert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CONVERT-transact-sql">TRY_CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <param name="data_type">The target data type. This includes <b>xml</b>, <b>bigint</b>, and <b>sql_variant</b>. Alias data types cannot be used.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_CONVERT({data_type}, {expression}, {style})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryConvert<T>([SqlQueryDependent] Func<SqlType<T>> data_type, [ExprParameter] object? expression, [SqlQueryDependent, ExprParameter] int style)
			=> throw new ServerSideOnlyException(nameof(TryConvert));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-CONVERT-transact-sql">TRY_CONVERT (Transact-SQL)</see></b></para>
		/// <para>These functions convert an expression of one data type to another.</para>
		/// </summary>
		/// <param name="expression">Any valid <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>.</param>
		/// <returns>Returns expression, translated to data_type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "TRY_CONVERT({2}, {0}, {1})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static T TryConvert<T>([ExprParameter] object? expression, [SqlQueryDependent, ExprParameter] int style)
			=> throw new ServerSideOnlyException(nameof(TryConvert));

		#endregion

		#region TryParse

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-PARSE-transact-sql">TRY_PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_PARSE({string_value} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(string_value))]
		public static T TryParse<T>([ExprParameter] string string_value, [SqlQueryDependent] SqlType<T> data_type)
			=> throw new ServerSideOnlyException(nameof(TryParse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-PARSE-transact-sql">TRY_PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_PARSE({string_value} as {data_type})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(string_value))]
		public static T TryParse<T>([ExprParameter] string string_value, [SqlQueryDependent] Func<SqlType<T>> data_type)
			=> throw new ServerSideOnlyException(nameof(TryParse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-PARSE-transact-sql">TRY_PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "TRY_PARSE({0} as {1})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(string_value))]
		public static T TryParse<T>([ExprParameter] string string_value)
			=> throw new ServerSideOnlyException(nameof(TryParse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-PARSE-transact-sql">TRY_PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <param name="culture">Optional string that identifies the culture in which string_value is formatted.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_PARSE({string_value} as {data_type} USING {culture})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(string_value))]
		public static T TryParse<T>([ExprParameter] string string_value, [SqlQueryDependent] SqlType<T> data_type, [ExprParameter, SqlQueryDependent] string culture)
			=> throw new ServerSideOnlyException(nameof(TryParse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-PARSE-transact-sql">TRY_PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="data_type">Literal value representing the data type requested for the result.</param>
		/// <param name="culture">Optional string that identifies the culture in which string_value is formatted.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TRY_PARSE({string_value} as {data_type} USING {culture})", ServerSideOnly=true, BuilderType=typeof(DataTypeBuilder))]
		[return: NotNullIfNotNull(nameof(string_value))]
		public static T TryParse<T>([ExprParameter] string string_value, [SqlQueryDependent] Func<SqlType<T>> data_type, [ExprParameter, SqlQueryDependent] string culture)
			=> throw new ServerSideOnlyException(nameof(TryParse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRY-PARSE-transact-sql">TRY_PARSE (Transact-SQL)</see></b></para>
		/// <para>Returns the result of an expression, translated to the requested data type in SQL Server.</para>
		/// </summary>
		/// <param name="string_value"><b>nvarchar</b>(4000) value representing the formatted value to parse into the specified data type.</param>
		/// <param name="culture">Optional string that identifies the culture in which string_value is formatted.</param>
		/// <returns>Returns the result of the expression, translated to the requested data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "TRY_PARSE({0} as {2} USING {1})", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(string_value))]
		public static T TryParse<T>([ExprParameter] string string_value, [ExprParameter, SqlQueryDependent] string culture)
			=> throw new ServerSideOnlyException(nameof(TryParse));

		#endregion

		#endregion

		#region Data type

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/datalength-transact-sql">DATALENGTH (Transact-SQL)</see></b></para>
		/// <para>This function returns the number of bytes used to represent any expression.</para>
		/// <c>DATALENGTH</c> becomes really helpful when used with data types that can store variable-length data, such as:
		/// <list type="bullet">
		/// <item>ntext</item>
		/// <item>nvarchar</item>
		/// <item>text</item>
		/// <item>varbinary</item>
		/// <item>varchar</item>
		/// </list>
		/// <para>
		/// For a NULL value, <c>DATALENGTH</c> returns NULL.
		/// </para>
		/// <remarks type="note">
		/// Use the <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/len-transact-sql">LEN</see>
		/// to return the number of characters encoded into a given string expression,
		/// and <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/datalength-transact-sql">DATALENGTH</see> to return the size in bytes
		/// for a given string expression. These outputs may differ depending on the data type and type of encoding used in the column.
		/// For more information on storage differences between different encoding types,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/collations/collation-and-unicode-support">Collation and Unicode Support</see>.
		/// </remarks>
		/// </summary>
		/// <typeparam name="T">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see> of any data type.</typeparam>
		/// <param name="expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see> of any data type.</param>
		/// <returns><b>bigint</b> if expression has an <b>nvarchar(max)</b>, <b>varbinary(max)</b>, or <b>varchar(max)</b> data type; otherwise <b>int</b>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DATALENGTH", 0, ServerSideOnly=true)]
		[CLSCompliant(false)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static int? DataLength<T>(T expression)
			=> throw new ServerSideOnlyException(nameof(DataLength));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/datalength-transact-sql">DATALENGTH (Transact-SQL)</see></b></para>
		/// <para>This function returns the number of bytes used to represent any expression.</para>
		/// <c>DATALENGTH</c> becomes really helpful when used with data types that can store variable-length data, such as:
		/// <list type="bullet">
		/// <item>ntext</item>
		/// <item>nvarchar</item>
		/// <item>text</item>
		/// <item>varbinary</item>
		/// <item>varchar</item>
		/// </list>
		/// <para>
		/// For a NULL value, <c>DATALENGTH</c> returns NULL.
		/// </para>
		/// <remarks type="note">
		/// Use the <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/len-transact-sql">LEN</see>
		/// to return the number of characters encoded into a given string expression,
		/// and <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/datalength-transact-sql">DATALENGTH</see> to return the size in bytes
		/// for a given string expression. These outputs may differ depending on the data type and type of encoding used in the column.
		/// For more information on storage differences between different encoding types,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/collations/collation-and-unicode-support">Collation and Unicode Support</see>.
		/// </remarks>
		/// </summary>
		/// <typeparam name="T">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see> of any data type.</typeparam>
		/// <param name="expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see> of any data type.</param>
		/// <returns><b>bigint</b> if expression has an <b>nvarchar(max)</b>, <b>varbinary(max)</b>, or <b>varchar(max)</b> data type; otherwise <b>int</b>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DATALENGTH", 0, ServerSideOnly=true)]
		[CLSCompliant(false)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static long? DataLengthBig<T>(T expression)
			=> throw new ServerSideOnlyException(nameof(DataLengthBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ident-current-transact-sql">IDENT_CURRENT (Transact-SQL)</see></b></para>
		/// <para>Returns the last identity value generated for a specified table or view. The last identity value generated can be for any session and any scope.</para>
		/// </summary>
		/// <param name="table_or_view">Is the name of the table or view whose identity value is returned. <c>table_or_view</c> is <b>varchar</b>, with no default.</param>
		/// <returns>numeric(<see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/max-precision-transact-sql">@@MAXPRECISION</see>, 0))</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "IDENT_CURRENT", ServerSideOnly=true)]
		public static decimal IdentityCurrent(string table_or_view)
			=> throw new ServerSideOnlyException(nameof(IdentityCurrent));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ident-incr-transact-sql">IDENT_INCR (Transact-SQL)</see></b></para>
		/// <para>Returns the increment value specified when creating a table or view's identity column.</para>
		/// </summary>
		/// <param name="table_or_view">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// specifying the table or view to check for a valid identity increment value.
		/// <c>table_or_view</c> can be a character string constant enclosed in quotation marks. It can also be a variable, a function, or a column name.
		/// <c>table_or_view</c> is <b>char</b>, <b>nchar</b>, <b>varchar</b>, or <b>nvarchar</b>.</param>
		/// <returns>numeric(<see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/max-precision-transact-sql">@@MAXPRECISION</see>, 0))</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "IDENT_INCR", ServerSideOnly=true)]
		public static decimal IdentityIncrement(string table_or_view)
			=> throw new ServerSideOnlyException(nameof(IdentityIncrement));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ident-seed-transact-sql">IDENT_SEED (Transact-SQL)</see></b></para>
		/// <para>Returns the original seed value specified when creating an identity column in a table or a view.
		/// Changing the current value of an identity column by using DBCC CHECKIDENT doesn't change the value returned by this function.</para>
		/// </summary>
		/// <param name="table_or_view">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// specifying the table or view to check for a valid identity increment value.
		/// <c>table_or_view</c> can be a character string constant enclosed in quotation marks. It can also be a variable, a function, or a column name.
		/// <c>table_or_view</c> is <b>char</b>, <b>nchar</b>, <b>varchar</b>, or <b>nvarchar</b>.</param>
		/// <returns>numeric(<see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/max-precision-transact-sql">@@MAXPRECISION</see>, 0))</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "IDENT_SEED", ServerSideOnly=true)]
		public static decimal IdentitySeed(string table_or_view)
			=> throw new ServerSideOnlyException(nameof(IdentitySeed));

		#endregion

		#region Date and Time

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEFIRST-transact-sql">@@DATEFIRST (Transact-SQL)</see></b></para>
		/// <para>This function returns the current value of <see href="https://docs.microsoft.com/en-us/sql/t-sql/statements/set-datefirst-transact-sql">SET DATEFIRST</see>,
		/// for a specific session.</para>
		/// </summary>
		/// <returns>tinyint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@DATEFIRST", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static byte DateFirst => throw new ServerSideOnlyException(nameof(DateFirst));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CURRENT-TIMESTAMP-transact-sql">CURRENT_TIMESTAMP (Transact-SQL)</see></b></para>
		/// <para>This function returns the current database system timestamp as a <b>datetime</b> value, without the database time zone offset.
		/// <c>CURRENT_TIMESTAMP</c> derives this value from the operating system of the computer on which the instance of SQL Server runs.</para>
		/// </summary>
		/// <returns>datetime</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "CURRENT_TIMESTAMP", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static DateTime CurrentTimestamp => throw new ServerSideOnlyException(nameof(CurrentTimestamp));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CURRENT-TIMEZONE-transact-sql">CURRENT_TIMEZONE (Transact-SQL)</see></b></para>
		/// <para>This function returns the name of the time zone observed by a server or an instance. For SQL Managed Instance, return value
		/// is based on the time zone of the instance itself assigned during instance creation, not the time zone of the underlying operating system.</para>
		/// </summary>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CURRENT_TIMEZONE", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string CurrentTimezone()
			=> throw new ServerSideOnlyException(nameof(CurrentTimezone));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CURRENT-TIMEZONE-ID-transact-sql">CURRENT_TIMEZONE_ID (Transact-SQL)</see></b></para>
		/// <para>This function returns the ID of the time zone observed by a server or an instance. For Azure SQL Managed Instance, return value
		/// is based on the time zone of the instance itself assigned during instance creation, not the time zone of the underlying operating system.</para>
		/// </summary>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CURRENT_TIMEZONE_ID", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string CurrentTimezoneID()
			=> throw new ServerSideOnlyException(nameof(CurrentTimezoneID));

		[Sql.Enum]
		public enum DateParts
		{
			Year,
			Quarter,
			Month,
			DayOfYear,
			Day,
			Week,
			WeekDay,
			Hour,
			Minute,
			Second,
			Millisecond,
			Microsecond,
			Nanosecond,
		}

		sealed class DatePartBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var datepart = builder.GetValue<DateParts>("datepart");
				builder.AddFragment("datepart", datepart.ToString());
			}
		}

		#region DateAdd

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEADD-transact-sql">DATEADD (Transact-SQL)</see></b></para>
		/// <para>This function adds a number (a signed integer) to a datepart of an input date, and returns a modified date/time value.</para>
		/// </summary>
		/// <param name="datepart">The part of date to which <c>DATEADD</c> adds an <b>integer</b> number. This table lists all valid datepart arguments.</param>
		/// <param name="number">An expression that can resolve to an int that <c>DATEADD</c> adds to a datepart of date.
		/// <c>DATEADD</c> accepts user-defined variable values for number.
		/// <c>DATEADD</c> will truncate a specified number value that has a decimal fraction. It will not round the number value in this situation.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEADD({datepart}, {number}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static DateTime? DateAdd([SqlQueryDependent] DateParts datepart, [ExprParameter] int? number, [ExprParameter] string? date)
			=> throw new ServerSideOnlyException(nameof(DateAdd));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEADD-transact-sql">DATEADD (Transact-SQL)</see></b></para>
		/// <para>This function adds a number (a signed integer) to a datepart of an input date, and returns a modified date/time value.</para>
		/// </summary>
		/// <param name="datepart">The part of date to which <c>DATEADD</c> adds an <b>integer</b> number. This table lists all valid datepart arguments.</param>
		/// <param name="number">An expression that can resolve to an int that <c>DATEADD</c> adds to a datepart of date.
		/// <c>DATEADD</c> accepts user-defined variable values for number.
		/// <c>DATEADD</c> will truncate a specified number value that has a decimal fraction. It will not round the number value in this situation.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEADD({datepart}, {number}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static DateTime? DateAdd([SqlQueryDependent] DateParts datepart, [ExprParameter] int? number, [ExprParameter] DateTime? date)
			=> throw new ServerSideOnlyException(nameof(DateAdd));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEADD-transact-sql">DATEADD (Transact-SQL)</see></b></para>
		/// <para>This function adds a number (a signed integer) to a datepart of an input date, and returns a modified date/time value.</para>
		/// </summary>
		/// <param name="datepart">The part of date to which <c>DATEADD</c> adds an <b>integer</b> number. This table lists all valid datepart arguments.</param>
		/// <param name="number">An expression that can resolve to an int that <c>DATEADD</c> adds to a datepart of date.
		/// <c>DATEADD</c> accepts user-defined variable values for number.
		/// <c>DATEADD</c> will truncate a specified number value that has a decimal fraction. It will not round the number value in this situation.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEADD({datepart}, {number}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static DateTimeOffset? DateAdd([SqlQueryDependent] DateParts datepart, [ExprParameter] int? number, [ExprParameter] DateTimeOffset? date)
			=> throw new ServerSideOnlyException(nameof(DateAdd));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEADD-transact-sql">DATEADD (Transact-SQL)</see></b></para>
		/// <para>This function adds a number (a signed integer) to a datepart of an input date, and returns a modified date/time value.</para>
		/// </summary>
		/// <param name="datepart">The part of date to which <c>DATEADD</c> adds an <b>integer</b> number. This table lists all valid datepart arguments.</param>
		/// <param name="number">An expression that can resolve to an int that <c>DATEADD</c> adds to a datepart of date.
		/// <c>DATEADD</c> accepts user-defined variable values for number.
		/// <c>DATEADD</c> will truncate a specified number value that has a decimal fraction. It will not round the number value in this situation.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEADD({datepart}, {number}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static TimeSpan? DateAdd([SqlQueryDependent] DateParts datepart, [ExprParameter] int? number, [ExprParameter] TimeSpan? date)
			=> throw new ServerSideOnlyException(nameof(DateAdd));

		#endregion

		#region DateDiff

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-transact-sql">DATEDIFF (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static int? DateDiff([SqlQueryDependent] DateParts datepart, [ExprParameter] string? startdate, [ExprParameter] string? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiff));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-transact-sql">DATEDIFF (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static int? DateDiff([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTime? startdate, [ExprParameter] DateTime? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiff));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-transact-sql">DATEDIFF (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static int? DateDiff([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTimeOffset? startdate, [ExprParameter] DateTimeOffset? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiff));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-transact-sql">DATEDIFF (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static int? DateDiff([SqlQueryDependent] DateParts datepart, [ExprParameter] TimeSpan? startdate, [ExprParameter] TimeSpan? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiff));

		#endregion

		#region DateDiffBig

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-BIG-transact-sql">DATEDIFF_BIG (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>bigint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF_BIG({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static long? DateDiffBig([SqlQueryDependent] DateParts datepart, [ExprParameter] string? startdate, [ExprParameter] string? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiffBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-BIG-transact-sql">DATEDIFF_BIG (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>bigint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF_BIG({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static long? DateDiffBig([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTime? startdate, [ExprParameter] DateTime? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiffBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-BIG-transact-sql">DATEDIFF_BIG (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>bigint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF_BIG({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static long? DateDiffBig([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTimeOffset? startdate, [ExprParameter] DateTimeOffset? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiffBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEDIFF-BIG-transact-sql">DATEDIFF_BIG (Transact-SQL)</see></b></para>
		/// <para>This function returns the count (as a signed integer value) of the specified datepart boundaries crossed
		/// between the specified <c>startdate</c> and <c>enddate</c>.</para>
		/// </summary>
		/// <returns>bigint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEDIFF_BIG({datepart}, {startdate}, {enddate})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		public static long? DateDiffBig([SqlQueryDependent] DateParts datepart, [ExprParameter] TimeSpan? startdate, [ExprParameter] TimeSpan? enddate)
			=> throw new ServerSideOnlyException(nameof(DateDiffBig));

		#endregion

		#region TimeFromParts

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TIMEFROMPARTS-transact-sql">TIMEFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>date</b> value that maps to the specified year, month, and day values.</para>
		/// </summary>
		/// <returns>date</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "TIMEFROMPARTS", ServerSideOnly=true)]
		public static TimeSpan? TimeFromParts(int? hour, int? minute, int? seconds, int? fractions, int? precision)
			=> throw new ServerSideOnlyException(nameof(TimeFromParts));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TIMEFROMPARTS-transact-sql">TIMEFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>date</b> value that maps to the specified year, month, and day values.</para>
		/// </summary>
		/// <returns>date</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "TIMEFROMPARTS({0}, {1}, {2}, 0, 0)", ServerSideOnly=true)]
		public static TimeSpan? TimeFromParts(int? hour, int? minute, int? seconds)
			=> throw new ServerSideOnlyException(nameof(TimeFromParts));

		#endregion

		#region DateFromParts

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEFROMPARTS-transact-sql">DATEFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>date</b> value that maps to the specified year, month, and day values.</para>
		/// </summary>
		/// <param name="year">An integer expression that specifies a year.</param>
		/// <param name="month">An integer expression that specifies a month, from 1 to 12.</param>
		/// <param name="day">An integer expression that specifies a day.</param>
		/// <returns>date</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DATEFROMPARTS", ServerSideOnly=true)]
		public static DateTime? DateFromParts(int? year, int? month, int? day)
			=> throw new ServerSideOnlyException(nameof(DateFromParts));

		#endregion

		#region SmallDateTimeFromParts

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SMALLDATETIMEFROMPARTS-transact-sql">SMALLDATETIMEFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>smalldatetime</b> value for the specified date and time.</para>
		/// </summary>
		/// <returns>datetime</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SMALLDATETIMEFROMPARTS", ServerSideOnly=true)]
		public static DateTime? SmallDateTimeFromParts(int? year, int? month, int? day, int? hour, int? minute)
			=> throw new ServerSideOnlyException(nameof(SmallDateTimeFromParts));

		#endregion

		#region DateTimeFromParts

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIMEFROMPARTS-transact-sql">DATETIMEFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>datetime</b> value for the specified date and time arguments.</para>
		/// </summary>
		/// <returns>datetime</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DATETIMEFROMPARTS", ServerSideOnly=true)]
		public static DateTime? DateTimeFromParts(int? year, int? month, int? day, int? hour, int? minute, int? seconds, int? milliseconds)
			=> throw new ServerSideOnlyException(nameof(DateTimeFromParts));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIMEFROMPARTS-transact-sql">DATETIMEFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>datetime</b> value for the specified date and time arguments.</para>
		/// </summary>
		/// <returns>datetime</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, 0)", ServerSideOnly=true)]
		public static DateTime? DateTimeFromParts(int? year, int? month, int? day, int? hour, int? minute, int? seconds)
			=> throw new ServerSideOnlyException(nameof(DateTimeFromParts));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIMEFROMPARTS-transact-sql">DATETIMEFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>datetime</b> value for the specified date and time arguments.</para>
		/// </summary>
		/// <returns>datetime</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "DATETIMEFROMPARTS({0}, {1}, {2}, 0, 0, 0, 0)", ServerSideOnly=true)]
		public static DateTime? DateTimeFromParts(int? year, int? month, int? day)
			=> throw new ServerSideOnlyException(nameof(DateTimeFromParts));

		#endregion

		#region DateTime2FromParts

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIME2FROMPARTS-transact-sql">DATETIME2FROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>datetime2</b> value for the specified date and time arguments.</para>
		/// </summary>
		/// <returns>datetime2</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DATETIME2FROMPARTS", ServerSideOnly=true)]
		public static DateTime? DateTime2FromParts(int? year, int? month, int? day, int? hour, int? minute, int? seconds, int? fractions, int? precision)
			=> throw new ServerSideOnlyException(nameof(DateTime2FromParts));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIME2FROMPARTS-transact-sql">DATETIME2FROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>datetime2</b> value for the specified date and time arguments.</para>
		/// </summary>
		/// <returns>datetime2</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "DATETIME2FROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, 0, 0)", ServerSideOnly=true)]
		public static DateTime? DateTime2FromParts(int? year, int? month, int? day, int? hour, int? minute, int? seconds)
			=> throw new ServerSideOnlyException(nameof(DateTime2FromParts));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIME2FROMPARTS-transact-sql">DATETIME2FROMPARTS (Transact-SQL)</see></b></para>
		/// <para>This function returns a <b>datetime2</b> value for the specified date and time arguments.</para>
		/// </summary>
		/// <returns>datetime2</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "DATETIME2FROMPARTS({0}, {1}, {2}, 0, 0, 0, 0, 0)", ServerSideOnly=true)]
		public static DateTime? DateTime2FromParts(int? year, int? month, int? day)
			=> throw new ServerSideOnlyException(nameof(DateTime2FromParts));

		#endregion

		#region DateTimeOffsetFromParts

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIMEOFFSETFROMPARTS-transact-sql">DATETIMEOFFSETFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetimeoffset</b> value for the specified date and time arguments.
		/// The returned value has a precision specified by the precision argument, and an offset as specified by the offset arguments.</para>
		/// </summary>
		/// <returns>datetimeoffset</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DATETIMEOFFSETFROMPARTS", ServerSideOnly=true)]
		public static DateTimeOffset? DateTimeOffsetFromParts(
			int? year, int? month, int? day,
			int? hour, int? minute, int? seconds, int? fractions,
			int? hour_offset, int? minute_offset, int? precision)
			=> throw new ServerSideOnlyException(nameof(DateTimeOffsetFromParts));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIMEOFFSETFROMPARTS-transact-sql">DATETIMEOFFSETFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetimeoffset</b> value for the specified date and time arguments.
		/// The returned value has a precision specified by the precision argument, and an offset as specified by the offset arguments.</para>
		/// </summary>
		/// <returns>datetimeoffset</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "DATETIMEOFFSETFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, 0, 0, 0, 0)", ServerSideOnly=true)]
		public static DateTimeOffset? DateTimeOffsetFromParts(int? year, int? month, int? day, int? hour, int? minute, int? seconds)
			=> throw new ServerSideOnlyException(nameof(DateTimeOffsetFromParts));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATETIMEOFFSETFROMPARTS-transact-sql">DATETIMEOFFSETFROMPARTS (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetimeoffset</b> value for the specified date and time arguments.
		/// The returned value has a precision specified by the precision argument, and an offset as specified by the offset arguments.</para>
		/// </summary>
		/// <returns>datetimeoffset</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "DATETIMEOFFSETFROMPARTS({0}, {1}, {2}, 0, 0, 0, 0, 0, 0, 0)", ServerSideOnly=true)]
		public static DateTimeOffset? DateTimeOffsetFromParts(int? year, int? month, int? day)
			=> throw new ServerSideOnlyException(nameof(DateTimeOffsetFromParts));

		#endregion

		#region DateName

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATENAME-transact-sql">DATENAME (Transact-SQL)</see></b></para>
		/// <para>This function returns a character string representing the specified datepart of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument that <c>DATENAME</c> will return. This table lists all valid datepart arguments.</param>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATENAME({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static string? DateName([SqlQueryDependent] DateParts datepart, [ExprParameter] string? date)
			=> throw new ServerSideOnlyException(nameof(DateName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATENAME-transact-sql">DATENAME (Transact-SQL)</see></b></para>
		/// <para>This function returns a character string representing the specified datepart of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument that <c>DATENAME</c> will return. This table lists all valid datepart arguments.</param>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATENAME({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static string? DateName([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTime? date)
			=> throw new ServerSideOnlyException(nameof(DateName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATENAME-transact-sql">DATENAME (Transact-SQL)</see></b></para>
		/// <para>This function returns a character string representing the specified datepart of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument that <c>DATENAME</c> will return. This table lists all valid datepart arguments.</param>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATENAME({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static string? DateName([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTimeOffset? date)
			=> throw new ServerSideOnlyException(nameof(DateName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATENAME-transact-sql">DATENAME (Transact-SQL)</see></b></para>
		/// <para>This function returns a character string representing the specified datepart of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument that <c>DATENAME</c> will return. This table lists all valid datepart arguments.</param>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATENAME({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static string? DateName([SqlQueryDependent] DateParts datepart, [ExprParameter] TimeSpan? date)
			=> throw new ServerSideOnlyException(nameof(DateName));

		#endregion

		#region DatePart

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEPART-transact-sql">DATEPART (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer representing the specified <c>datepart</c> of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument for which <c>DATEPART</c> will return an <b>integer</b>. This table lists all valid datepart arguments.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEPART({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? DatePart([SqlQueryDependent] DateParts datepart, [ExprParameter] string? date)
			=> throw new ServerSideOnlyException(nameof(DatePart));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEPART-transact-sql">DATEPART (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer representing the specified <c>datepart</c> of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument for which <c>DATEPART</c> will return an <b>integer</b>. This table lists all valid datepart arguments.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEPART({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? DatePart([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTime? date)
			=> throw new ServerSideOnlyException(nameof(DatePart));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEPART-transact-sql">DATEPART (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer representing the specified <c>datepart</c> of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument for which <c>DATEPART</c> will return an <b>integer</b>. This table lists all valid datepart arguments.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEPART({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? DatePart([SqlQueryDependent] DateParts datepart, [ExprParameter] DateTimeOffset? date)
			=> throw new ServerSideOnlyException(nameof(DatePart));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DATEPART-transact-sql">DATEPART (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer representing the specified <c>datepart</c> of the specified <c>date</c>.</para>
		/// </summary>
		/// <param name="datepart">The specific part of the date argument for which <c>DATEPART</c> will return an <b>integer</b>. This table lists all valid datepart arguments.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATEPART({datepart}, {date})", ServerSideOnly=true, BuilderType=typeof(DatePartBuilder))]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? DatePart([SqlQueryDependent] DateParts datepart, [ExprParameter] TimeSpan? date)
			=> throw new ServerSideOnlyException(nameof(DatePart));

		#endregion

		#region Day

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DAY-transact-sql">DAY (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer that represents the day (day of the month) of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DAY", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Day(string? date)
			=> throw new ServerSideOnlyException(nameof(Day));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DAY-transact-sql">DAY (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer that represents the day (day of the month) of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DAY", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Day(DateTime? date)
			=> throw new ServerSideOnlyException(nameof(Day));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DAY-transact-sql">DAY (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer that represents the day (day of the month) of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DAY", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Day(DateTimeOffset? date)
			=> throw new ServerSideOnlyException(nameof(Day));

		#endregion

		#region EndOfMonth

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/EOMONTH-transact-sql">EOMONTH (Transact-SQL)</see></b></para>
		/// <para>This function returns the last day of the month containing a specified date, with an optional offset.</para>
		/// </summary>
		/// <param name="start_date">A date expression that specifies the date for which to return the last day of the month.</param>
		/// <returns>date</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "EOMONTH", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(start_date))]
		public static DateTime? EndOfMonth(string? start_date)
			=> throw new ServerSideOnlyException(nameof(EndOfMonth));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/EOMONTH-transact-sql">EOMONTH (Transact-SQL)</see></b></para>
		/// <para>This function returns the last day of the month containing a specified date, with an optional offset.</para>
		/// </summary>
		/// <param name="start_date">A date expression that specifies the date for which to return the last day of the month.</param>
		/// <param name="month_to_add">An optional integer expression that specifies the number of months to add to <c>start_date</c>.</param>
		/// <returns>date</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "EOMONTH", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(start_date))]
		public static DateTime? EndOfMonth(string? start_date, int? month_to_add)
			=> throw new ServerSideOnlyException(nameof(EndOfMonth));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/EOMONTH-transact-sql">EOMONTH (Transact-SQL)</see></b></para>
		/// <para>This function returns the last day of the month containing a specified date, with an optional offset.</para>
		/// </summary>
		/// <param name="start_date">A date expression that specifies the date for which to return the last day of the month.</param>
		/// <returns>date</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "EOMONTH", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(start_date))]
		public static DateTime? EndOfMonth(DateTime? start_date)
			=> throw new ServerSideOnlyException(nameof(EndOfMonth));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/EOMONTH-transact-sql">EOMONTH (Transact-SQL)</see></b></para>
		/// <para>This function returns the last day of the month containing a specified date, with an optional offset.</para>
		/// </summary>
		/// <param name="start_date">A date expression that specifies the date for which to return the last day of the month.</param>
		/// <param name="month_to_add">An optional integer expression that specifies the number of months to add to <c>start_date</c>.</param>
		/// <returns>date</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "EOMONTH", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(start_date))]
		public static DateTime? EndOfMonth(DateTime? start_date, int? month_to_add)
			=> throw new ServerSideOnlyException(nameof(EndOfMonth));

		#endregion

		#region GetDate

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/GETDATE-transact-sql">GETDATE (Transact-SQL)</see></b></para>
		/// <para>Returns the current database system timestamp as a <b>datetime</b> value without the database time zone offset.</para>
		/// </summary>
		/// <returns>datetime</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "GETDATE", ServerSideOnly=true)]
		public static DateTime GetDate()
			=> throw new ServerSideOnlyException(nameof(GetDate));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/GETUTCDATE-transact-sql">GETUTCDATE (Transact-SQL)</see></b></para>
		/// <para>Returns the current database system timestamp as a <b>datetime</b> value. The database time zone offset is not included.
		/// This value represents the current UTC time (Coordinated Universal Time). This value is derived from the operating system of the
		/// computer on which the instance of SQL Server is running.</para>
		/// </summary>
		/// <returns>datetime</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "GETUTCDATE", ServerSideOnly=true)]
		public static DateTime GetUtcDate()
			=> throw new ServerSideOnlyException(nameof(GetUtcDate));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SYSDATETIME-transact-sql">SYSDATETIME (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetime2(7)</b> value that contains the date and time of the computer on which the instance of SQL Server is running.</para>
		/// </summary>
		/// <returns>datetime2(7)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SYSDATETIME", ServerSideOnly=true)]
		public static DateTime SysDatetime()
			=> throw new ServerSideOnlyException(nameof(SysDatetime));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SYSDATETIMEOFFSET-transact-sql">SYSDATETIMEOFFSET (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetimeoffset(7)</b> value that contains the date and time of the computer on which the instance of SQL Server is running.
		/// The time zone offset is included.</para>
		/// </summary>
		/// <returns>datetimeoffset(7)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SYSDATETIMEOFFSET", ServerSideOnly=true)]
		public static DateTimeOffset SysDatetimeOffset()
			=> throw new ServerSideOnlyException(nameof(SysDatetimeOffset));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SYSUTCDATETIME-transact-sql">SYSUTCDATETIME (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetime2</b> value that contains the date and time of the computer on which the instance of SQL Server is running.
		/// The date and time is returned as UTC time (Coordinated Universal Time). The fractional second precision specification has a range from 1 to 7 digits.
		/// The default precision is 7 digits.</para>
		/// </summary>
		/// <returns>datetime2</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SYSUTCDATETIME", ServerSideOnly=true)]
		public static DateTime SysUtcDatetime()
			=> throw new ServerSideOnlyException(nameof(SysUtcDatetime));

		#endregion

		#region IsDate

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ISDATE-transact-sql">ISDATE (Transact-SQL)</see></b></para>
		/// <para>Returns 1 if the expression is a valid <b>datetime</b> value; otherwise, 0.
		/// ISDATE returns 0 if the expression is a <b>datetime2</b> value.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ISDATE", ServerSideOnly=true)]
		public static int IsDate(string expression)
			=> throw new ServerSideOnlyException(nameof(IsDate));

		#endregion

		#region Month

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/MONTH-transact-sql">MONTH (Transact-SQL)</see></b></para>
		/// <para>Returns an integer that represents the month of the specified date.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "MONTH", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Month(string? date)
			=> throw new ServerSideOnlyException(nameof(Month));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/MONTH-transact-sql">MONTH (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer that represents the day (day of the month) of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "MONTH", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Month(DateTime? date)
			=> throw new ServerSideOnlyException(nameof(Month));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/MONTH-transact-sql">MONTH (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer that represents the day (day of the month) of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "MONTH", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Month(DateTimeOffset? date)
			=> throw new ServerSideOnlyException(nameof(Month));

		#endregion

		#region Year

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/YEAR-transact-sql">YEAR (Transact-SQL)</see></b></para>
		/// <para>Returns an integer that represents the year of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "YEAR", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Year(string? date)
			=> throw new ServerSideOnlyException(nameof(Year));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/YEAR-transact-sql">YEAR (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer that represents the day (day of the month) of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "YEAR", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Year(DateTime? date)
			=> throw new ServerSideOnlyException(nameof(Year));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/YEAR-transact-sql">YEAR (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer that represents the day (day of the month) of the specified <c>date</c>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "YEAR", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(date))]
		public static int? Year(DateTimeOffset? date)
			=> throw new ServerSideOnlyException(nameof(Year));

		#endregion

		#region SwitchOffset

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SWITCHOFFSET-transact-sql">SWITCHOFFSET (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetimeoffset</b> value that is changed from the stored time zone offset to a specified new time zone offset.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SWITCHOFFSET", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(datetimeoffset_expression))]
		public static DateTimeOffset? SwitchOffset(DateTimeOffset? datetimeoffset_expression, string timezoneoffset_expression)
			=> throw new ServerSideOnlyException(nameof(SwitchOffset));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TODATETIMEOFFSET-transact-sql">TODATETIMEOFFSET (Transact-SQL)</see></b></para>
		/// <para>Returns a <b>datetimeoffset</b> value that is translated from a <b>datetime2</b> expression.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "TODATETIMEOFFSET", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(datetime_expression))]
		public static DateTimeOffset? ToDatetimeOffset(DateTimeOffset? datetime_expression, string timezoneoffset_expression)
			=> throw new ServerSideOnlyException(nameof(ToDatetimeOffset));

		#endregion

		#endregion

		#region Json

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ISJSON-transact-sql">ISJSON (Transact-SQL)</see></b></para>
		/// <para>Tests whether a string contains valid JSON.</para>
		/// </summary>
		/// <param name="expression">The string to test.</param>
		/// <returns>Returns 1 if the string contains valid JSON; otherwise, returns 0. Returns null if expression is null.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ISJSON", ServerSideOnly = true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static bool? IsJson(string? expression)
			=> throw new ServerSideOnlyException(nameof(IsJson));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/JSON-VALUE-transact-sql">JSON_VALUE (Transact-SQL)</see></b></para>
		/// <para>Extracts a scalar value from a JSON string.</para>
		/// </summary>
		/// <param name="expression">An expression. Typically the name of a variable or a column that contains JSON text.</param>
		/// <param name="path">A JSON path that specifies the property to extract.</param>
		/// <returns>Returns a single text value of type nvarchar(4000).
		/// The collation of the returned value is the same as the collation of the input expression.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "JSON_VALUE", ServerSideOnly=true)]
		public static string? JsonValue(string? expression, string path)
			=> throw new ServerSideOnlyException(nameof(JsonValue));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/JSON-QUERY-transact-sql">JSON_QUERY (Transact-SQL)</see></b></para>
		/// <para>Extracts an object or an array from a JSON string.</para>
		/// </summary>
		/// <param name="expression">An expression. Typically the name of a variable or a column that contains JSON text.</param>
		/// <param name="path">A JSON path that specifies the property to extract.</param>
		/// <returns>Returns a JSON fragment of type nvarchar(max).
		/// The collation of the returned value is the same as the collation of the input expression.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "JSON_QUERY", ServerSideOnly=true)]
		public static string? JsonQuery(string? expression, string path)
			=> throw new ServerSideOnlyException(nameof(JsonQuery));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/JSON-MODIFY-transact-sql">JSON_MODIFY (Transact-SQL)</see></b></para>
		/// <para>Updates the value of a property in a JSON string and returns the updated JSON string.</para>
		/// </summary>
		/// <param name="expression">An expression. Typically the name of a variable or a column that contains JSON text.</param>
		/// <param name="path">A JSON path expression that specifies the property to update.</param>
		/// <param name="newValue">The new value for the property specified by path. The new value must be a [n]varchar or text.</param>
		/// <returns>Returns the updated value of expression as properly formatted JSON text.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "JSON_MODIFY", ServerSideOnly=true)]
		[return: NotNullIfNotNull(nameof(expression))]
		public static string? JsonModify(string? expression, string path, string newValue)
			=> throw new ServerSideOnlyException(nameof(JsonModify));

		public record JsonData
		{
			[Column("key")]   public string? Key   { get; set; }
			[Column("value")] public string? Value { get; set; }
			[Column("type")]  public int?    Type  { get; set; }
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OPENJSON-transact-sql">OPENJSON (Transact-SQL)</see></b></para>
		/// <para>A table-valued function that parses JSON text and returns objects and properties from the JSON input as rows and columns.</para>
		/// </summary>
		/// <param name="json">An expression. Typically the name of a variable or a column that contains JSON text.</param>
		/// <returns>Returns a rowset view over the elements of a JSON object or array.</returns>
		/// <exception cref="ServerSideOnlyException" />
		/// <remarks>Only available on SQL Server 2016 or later, and compatibility mode for the database must be set to 130 or higher</remarks>
		[Sql.TableExpression("OPENJSON({2}) {1}")]
		public static IQueryable<JsonData> OpenJson(string? json)
			=> throw new ServerSideOnlyException(nameof(OpenJson));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OPENJSON-transact-sql">OPENJSON (Transact-SQL)</see></b></para>
		/// <para>A table-valued function that parses JSON text and returns objects and properties from the JSON input as rows and columns.</para>
		/// </summary>
		/// <param name="json">An expression. Typically the name of a variable or a column that contains JSON text.</param>
		/// <param name="path">A JSON path expression that specifies the property to query.</param>
		/// <returns>Returns a rowset view over the elements of a JSON object or array.</returns>
		/// <exception cref="ServerSideOnlyException" />
		/// <remarks>Only available on SQL Server 2016 or later, and compatibility mode for the database must be set to 130 or higher</remarks>
		[Sql.TableExpression("OPENJSON({2}, {3}) {1}")]
		public static IQueryable<JsonData> OpenJson(string? json, [ExprParameter(DoNotParameterize = true)] string path)
			=> throw new ServerSideOnlyException(nameof(OpenJson));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OPENJSON-transact-sql">OPENJSON (Transact-SQL)</see></b></para>
		/// <para>A table-valued function that parses JSON text and returns objects and properties from the JSON input as rows and columns.</para>
		/// </summary>
		/// <param name="json">An expression. Typically the name of a variable or a column that contains JSON text.</param>
		/// <returns>Returns a rowset view over the elements of a JSON object or array.</returns>
		/// <exception cref="ServerSideOnlyException" />
		/// <remarks>Only available on SQL Server 2016 or later, and compatibility mode for the database must be set to 130 or higher</remarks>
		[ExpressionMethod(nameof(GenerateOpenJsonStringImpl))]
		public static IQueryable<JsonData> OpenJson(this IDataContext dc, [ExprParameter] string? json)
		{
			return dc.QueryFromExpression(() => dc.OpenJson(json));
		}

		private static Expression<Func<IDataContext, string?, IQueryable<JsonData>>> GenerateOpenJsonStringImpl()
		{
			return (dc, json) => dc.FromSql<JsonData>($"OPENJSON({json})");
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OPENJSON-transact-sql">OPENJSON (Transact-SQL)</see></b></para>
		/// <para>A table-valued function that parses JSON text and returns objects and properties from the JSON input as rows and columns.</para>
		/// </summary>
		/// <param name="json">An expression. Typically the name of a variable or a column that contains JSON text.</param>
		/// <param name="path">A JSON path expression that specifies the property to query.</param>
		/// <returns>Returns a rowset view over the elements of a JSON object or array.</returns>
		/// <exception cref="ServerSideOnlyException" />
		/// <remarks>Only available on SQL Server 2017 or later, and compatibility mode for the database must be set to 140 or higher</remarks>
		[ExpressionMethod(nameof(GenerateOpenJsonStringStringImpl))]
		public static IQueryable<JsonData> OpenJson(this IDataContext dc, [ExprParameter] string? json, [ExprParameter] string path)
		{
			return dc.QueryFromExpression(() => dc.OpenJson(json, path));
		}

		private static Expression<Func<IDataContext, string?, string, IQueryable<JsonData>>> GenerateOpenJsonStringStringImpl()
		{
			return (dc, json, path) => dc.FromSql<JsonData>($"OPENJSON({json}, {path})");
		}

		#endregion

		#region Mathematical

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ABS-transact-sql">ABS (Transact-SQL)</see></b></para>
		/// <para>A mathematical function that returns the absolute (positive) value of the specified numeric expression.
		/// (<c>ABS</c> changes negative values to positive values. <c>ABS</c> has no effect on zero or positive values.)</para>
		/// </summary>
		/// <param name="numeric_expression">An expression of the exact numeric or approximate numeric data type category.</param>
		/// <returns>Returns the same type as numeric_expression.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ABS", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Abs<T>(T numeric_expression)
			=> throw new ServerSideOnlyException(nameof(Abs));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ACOS-transact-sql">ACOS (Transact-SQL)</see></b></para>
		/// <para>A function that returns the angle, in radians, whose cosine is the specified float expression. This is also called arccosine.</para>
		/// </summary>
		/// <param name="float_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of either type <b>float</b> or of a type that can implicitly convert to float. Only a value ranging from -1.00 to 1.00 is valid.
		/// For values outside this range, no value is returned, and ACOS will report a domain error.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ACOS", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Acos<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Acos));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ASIN-transact-sql">ASIN (Transact-SQL)</see></b></para>
		/// <para>A function that returns the angle, in radians, whose sine is the specified <b>float</b> expression. This is also called <b>arcsine</b>.</para>
		/// </summary>
		/// <param name="float_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of either type <b>float</b> or of a type that can implicitly convert to float. Only a value ranging from -1.00 to 1.00 is valid.
		/// For values outside this range, no value is returned, and ASIN will report a domain error.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ASIN", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Asin<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Asin));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ATAN-transact-sql">ATAN (Transact-SQL)</see></b></para>
		/// <para>A function that returns the angle, in radians, whose tangent is a specified <b>float</b> expression. This is also called <b>arctangent</b>.</para>
		/// </summary>
		/// <param name="float_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of either type <b>float</b> or of a type that implicitly convert to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ATAN", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Atan<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Atan));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ATN2-transact-sql">ATN2 (Transact-SQL)</see></b></para>
		/// <para>Returns the angle, in radians, between the positive x-axis and the ray from the origin to the point (y, x),
		/// where x and y are the values of the two specified float expressions.</para>
		/// </summary>
		/// <param name="float_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ATN2", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Atn2<T>(T float_expression, T float_expression2)
			=> throw new ServerSideOnlyException(nameof(Atn2));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CEILING-transact-sql">CEILING (Transact-SQL)</see></b></para>
		/// <para>This function returns the smallest integer greater than, or equal to, the specified numeric expression.</para>
		/// </summary>
		/// <param name="numeric_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of the exact numeric or approximate numeric data type category. For this function, the <b>bit</b> data type is invalid.</param>
		/// <returns>Return values have the same type as <c>numeric_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CEILING", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Ceiling<T>(T numeric_expression)
			=> throw new ServerSideOnlyException(nameof(Ceiling));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/COS-transact-sql">COS (Transact-SQL)</see></b></para>
		/// <para>A mathematical function that returns the trigonometric cosine of the specified angle - measured in radians - in the specified expression.</para>
		/// </summary>
		/// <param name="float_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "COS", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Cos<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Cos));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/COT-transact-sql">COT (Transact-SQL)</see></b></para>
		/// <para>A mathematical function that returns the trigonometric cotangent of the specified angle - in radians - in the specified <b>float</b> expression.</para>
		/// </summary>
		/// <param name="float_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b>, or of a type that can implicitly convert to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "COT", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Cot<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Cot));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DEGREES-transact-sql">DEGREES (Transact-SQL)</see></b></para>
		/// <para>This function returns the corresponding angle, in degrees, for an angle specified in radians.</para>
		/// </summary>
		/// <param name="numeric_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of the exact numeric or approximate numeric data type category, except for the <b>bit</b> data type.</param>
		/// <returns>Return values have the same type as <c>numeric_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DEGREES", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Degrees<T>(T numeric_expression)
			=> throw new ServerSideOnlyException(nameof(Degrees));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/EXP-transact-sql">EXP (Transact-SQL)</see></b></para>
		/// <para>Returns the exponential value of the specified <b>float</b> expression.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "EXP", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Exp<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Exp));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FLOOR-transact-sql">FLOOR (Transact-SQL)</see></b></para>
		/// <para>Returns the largest integer less than or equal to the specified numeric expression.</para>
		/// </summary>
		/// <param name="numeric_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of the exact numeric or approximate numeric data type category, except for the <b>bit</b> data type.</param>
		/// <returns>Return values have the same type as <c>numeric_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FLOOR", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Floor<T>(T numeric_expression)
			=> throw new ServerSideOnlyException(nameof(Floor));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LOG-transact-sql">LOG (Transact-SQL)</see></b></para>
		/// <para>Returns the natural logarithm of the specified <b>float</b> expression in SQL Server.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LOG", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Log<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Log));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LOG-transact-sql">LOG (Transact-SQL)</see></b></para>
		/// <para>Returns the natural logarithm of the specified <b>float</b> expression in SQL Server.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <param name="base">Optional integer argument that sets the base for the logarithm.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LOG", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Log<T>(T float_expression, int @base)
			=> throw new ServerSideOnlyException(nameof(Log));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LOG10-transact-sql">LOG10 (Transact-SQL)</see></b></para>
		/// <para>Returns the base-10 logarithm of the specified <b>float</b> expression.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LOG", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Log10<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Log10));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PI-transact-sql">PI (Transact-SQL)</see></b></para>
		/// <para>Returns the constant value of PI.</para>
		/// </summary>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "PI", ServerSideOnly=true)]
		public static double PI()
			=> throw new ServerSideOnlyException(nameof(PI));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/POWER-transact-sql">POWER (Transact-SQL)</see></b></para>
		/// <para>Returns the base-10 logarithm of the specified <b>float</b> expression.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <param name="y">Is the power to which to raise <c>float_expression</c>. y can be an expression of the exact numeric or
		/// approximate numeric data type category, except for the <b>bit</b> data type.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "POWER", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Power<T>(T float_expression, T y)
			=> throw new ServerSideOnlyException(nameof(Power));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SIGN-transact-sql">SIGN (Transact-SQL)</see></b></para>
		/// <para>Returns the positive (+1), zero (0), or negative (-1) sign of the specified expression.</para>
		/// </summary>
		/// <param name="numeric_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of the exact numeric or approximate numeric data type category, except for the <b>bit</b> data type.</param>
		/// <returns>Return values have the same type as <c>numeric_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SIGN", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Sign<T>(T numeric_expression)
			=> throw new ServerSideOnlyException(nameof(Sign));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/RAND-transact-sql">RAND (Transact-SQL)</see></b></para>
		/// <para>Returns a pseudo-random <b>float</b> value from 0 through 1, exclusive.</para>
		/// </summary>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "RAND", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static double Random()
			=> throw new ServerSideOnlyException(nameof(Random));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/RAND-transact-sql">RAND (Transact-SQL)</see></b></para>
		/// <para>Returns a pseudo-random <b>float</b> value from 0 through 1, exclusive.</para>
		/// </summary>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "RAND", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static double Random(int seed)
			=> throw new ServerSideOnlyException(nameof(Random));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ROUND-transact-sql">ROUND (Transact-SQL)</see></b></para>
		/// <para>Returns a numeric value, rounded to the specified length or precision.</para>
		/// </summary>
		/// <param name="numeric_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of the exact numeric or approximate numeric data type category, except for the <b>bit</b> data type.</param>
		/// <param name="length">Is the precision to which <c>numeric_expression</c> is to be rounded.
		/// <c>length</c> must be an expression of type <b>tinyint</b>, <b>smallint</b>, or <b>int</b>.
		/// When <c>length</c> is a positive number, <c>numeric_expression</c> is rounded to the number of
		/// decimal positions specified by <c>length</c>. When <c>length</c> is a negative number,
		/// <c>numeric_expression</c> is rounded on the left side of the decimal point, as specified by <c>length</c>.</param>
		/// <param name="function">Is the type of operation to perform. <c>function</c> must be <b>tinyint</b>, <b>smallint</b>, or <b>int</b>.
		/// When function is omitted or has a value of 0 (default), <c>numeric_expression</c> is rounded. When a value other than 0 is specified,
		/// <c>numeric_expression</c> is truncated</param>
		/// <returns>Return values have the same type as <c>numeric_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ROUND", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Round<T>(T numeric_expression, int length, int function)
			=> throw new ServerSideOnlyException(nameof(Round));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ROUND-transact-sql">ROUND (Transact-SQL)</see></b></para>
		/// <para>Returns a numeric value, rounded to the specified length or precision.</para>
		/// </summary>
		/// <param name="numeric_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of the exact numeric or approximate numeric data type category, except for the <b>bit</b> data type.</param>
		/// <param name="length">Is the precision to which <c>numeric_expression</c> is to be rounded.
		/// <c>length</c> must be an expression of type <b>tinyint</b>, <b>smallint</b>, or <b>int</b>.
		/// When <c>length</c> is a positive number, <c>numeric_expression</c> is rounded to the number of
		/// decimal positions specified by <c>length</c>. When <c>length</c> is a negative number,
		/// <c>numeric_expression</c> is rounded on the left side of the decimal point, as specified by <c>length</c>.</param>
		/// <returns>Return values have the same type as <c>numeric_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ROUND", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Round<T>(T numeric_expression, int length)
			=> throw new ServerSideOnlyException(nameof(Round));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/RADIANS-transact-sql">RADIANS (Transact-SQL)</see></b></para>
		/// <para>Returns radians when a numeric expression, in degrees, is entered.</para>
		/// </summary>
		/// <param name="numeric_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of the exact numeric or approximate numeric data type category, except for the <b>bit</b> data type.</param>
		/// <returns>Return values have the same type as <c>numeric_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "RADIANS", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(numeric_expression))]
		public static T Radians<T>(T numeric_expression)
			=> throw new ServerSideOnlyException(nameof(Radians));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SIN-transact-sql">SIN (Transact-SQL)</see></b></para>
		/// <para>Returns the trigonometric sine of the specified angle, in radians, and in an approximate numeric, <b>float</b>, expression.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to float, in radians.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SIN", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Sin<T>(T float_expression, T y)
			=> throw new ServerSideOnlyException(nameof(Sin));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SQRT-transact-sql">SQRT (Transact-SQL)</see></b></para>
		/// <para>Returns the square root of the specified float value.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SQRT", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Sqrt<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Sqrt));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SQUARE-transact-sql">SQUARE (Transact-SQL)</see></b></para>
		/// <para>Returns the square of the specified float value.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SQUARE", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Square<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Square));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TAN-transact-sql">TAN (Transact-SQL)</see></b></para>
		/// <para>Returns the tangent of the input expression.</para>
		/// </summary>
		/// <param name="float_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>float</b> or of a type that can be implicitly converted to <b>float</b>.</param>
		/// <returns>float</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "TAN", ServerSideOnly=true, IgnoreGenericParameters=true)]
		[return: NotNullIfNotNull(nameof(float_expression))]
		public static T Tan<T>(T float_expression)
			=> throw new ServerSideOnlyException(nameof(Tan));

		#endregion

		#region Logical

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/logical-functions-CHOOSE-transact-sql">CHOOSE (Transact-SQL)</see></b></para>
		/// <para>Returns the last identity value generated for a specified table or view. The last identity value generated can be for any session and any scope.</para>
		/// </summary>
		/// <param name="index">Is an integer expression that represents a 1-based index into the list of the items following it.</param>
		/// <param name="values">List of comma separated values of any data type.</param>
		/// <returns>Returns the data type with the highest precedence from the set of types passed to the function.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHOOSE", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static T Choose<T>(int? index, params T[] values)
			=> throw new ServerSideOnlyException(nameof(Choose));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/logical-functions-IIF-transact-sql">IIF (Transact-SQL)</see></b></para>
		/// <para>Returns one of two values, depending on whether the Boolean expression evaluates to true or false in SQL Server.</para>
		/// </summary>
		/// <param name="boolean_expression">A valid Boolean expression.</param>
		/// <param name="true_value">Value to return if <c>boolean_expression</c> evaluates to true.</param>
		/// <param name="false_value">Value to return if <c>boolean_expression</c> evaluates to false.</param>
		/// <returns>Returns the data type with the highest precedence from the types in <c>true_value</c> and <c>false_value</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "IIF", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static T Iif<T>(bool? boolean_expression, T true_value, T false_value)
			=> throw new ServerSideOnlyException(nameof(Iif));

		#endregion

		#region Metadata

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/app-name-transact-sql">APP_NAME (Transact-SQL)</see></b></para>
		/// <para>This function returns the application name for the current session, if the application sets that name value.</para>
		/// </summary>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "APP_NAME", ServerSideOnly=true)]
		public static string AppName()
			=> throw new ServerSideOnlyException(nameof(AppName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/col-length-transact-sql">COL_LENGTH (Transact-SQL)</see></b></para>
		/// <para>This function returns the defined length of a column, in bytes.</para>
		/// </summary>
		/// <param name="table">The name of the table whose column length information we want to determine. <c>table</c> is an expression of type <b>nvarchar</b>.</param>
		/// <param name="column">The column name whose length we want to determine. <c>column</c> is an expression of type <b>nvarchar</b>.</param>
		/// <returns>smallint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "COL_LENGTH", ServerSideOnly=true)]
		public static short? ColumnLength(string table, string column)
			=> throw new ServerSideOnlyException(nameof(ColumnLength));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/col-name-transact-sql">COL_NAME (Transact-SQL)</see></b></para>
		/// <para>This function returns the name of a table column, based on the table identification number and column identification number values of that table column.</para>
		/// </summary>
		/// <param name="table_id">The identification number of the table containing that column. The <c>table_id</c> argument has an <b>int</b> data type.</param>
		/// <param name="column_id">The identification number of the column. The <c>column_id</c> argument has an <b>int</b> data type.</param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "COL_NAME", ServerSideOnly=true)]
		public static string? ColumnName(int? table_id, int column_id)
			=> throw new ServerSideOnlyException(nameof(ColumnName));

		public enum ColumnPropertyName
		{
			AllowsNull,   ColumnId,         FullTextTypeColumn, GeneratedAlwaysType, IsColumnSet,
			IsComputed,   IsCursorType,     IsDeterministic,    IsFulltextIndexed,   IsHidden,
			IsIdentity,   IsIdNotForRepl,   IsIndexable,        IsOutParam,          IsPrecise,
			IsRowGuidCol, IsSparse,         IsSystemVerified,   IsXmlIndexable,      Precision,
			Scale,        SystemDataAccess, UserDataAccess,     UsesAnsiTrim,        StatisticalSemantics,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/columnproperty-transact-sql">COLUMNPROPERTY (Transact-SQL)</see></b></para>
		/// <para>This function returns column or parameter information.</para>
		/// </summary>
		/// <param name="id">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// containing the identifier (ID) of the table or procedure.</param>
		/// <param name="column">An expression containing the name of the column or parameter.</param>
		/// <param name="property">For the <c>id</c> argument, the property argument specifies the information type that the <c>COLUMNPROPERTY</c> function will return.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "COLUMNPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<ColumnPropertyName>))]
		public static int? ColumnProperty(int? id, string column, [SqlQueryDependent] ColumnPropertyName property)
			=> throw new ServerSideOnlyException(nameof(ColumnProperty));

		public enum DatabasePropertyName
		{
			Collation,            ComparisonStyle,            Edition,                  IsAnsiNullDefault,   IsAnsiNullsEnabled,
			IsAnsiPaddingEnabled, IsAnsiWarningsEnabled,      IsArithmeticAbortEnabled, IsAutoClose,         IsAutoCreateStatistics,
			IsAutoShrink,         IsAutoUpdateStatistics,     IsClone,                  IsFulltextEnabled,   IsAutoCreateStatisticsIncremental,
			IsInStandBy,          IsLocalCursorsDefault,      IsMergePublished,         IsNullConcat,        IsCloseCursorsOnCommitEnabled,
			IsPublished,          IsRecursiveTriggersEnabled, IsSubscribed,             IsSyncWithBackup,    IsMemoryOptimizedElevateToSnapshotEnabled,
			IsVerifiedClone,      IsNumericRoundAbortEnabled, IsXTPSupported,           LastGoodCheckDbTime, IsQuotedIdentifiersEnabled,
			LCID,                 IsParameterizationForced,   MaxSizeInBytes,           Recovery,            IsTornPageDetectionEnabled,
			ServiceObjective,     ServiceObjectiveId,         SQLSortOrder,             Status,              Updateability,
			UserAccess,           Version,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/databasepropertyex-transact-sql">DATABASEPROPERTYEX (Transact-SQL)</see></b></para>
		/// <para>For a specified database in SQL Server, this function returns the current setting of the specified database option or property.</para>
		/// </summary>
		/// <param name="database">An expression specifying the name of the database for which <c>DATABASEPROPERTYEX</c> will return the named property information.
		/// <c>database</c> has an <b>nvarchar(128)</b> data type.</param>
		/// <param name="property">An expression specifying the name of the database property to return. <c>property</c> has a <b>varchar(128)</b> data type</param>
		/// <returns>sql_variant</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "DATABASEPROPERTYEX", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<DatabasePropertyName>))]
		public static object? DatabasePropertyEx(string database, [SqlQueryDependent] DatabasePropertyName property)
			=> throw new ServerSideOnlyException(nameof(DatabasePropertyEx));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/db-id-transact-sql">DB_ID (Transact-SQL)</see></b></para>
		/// <para>This function returns the database identification (ID) number of a specified database.</para>
		/// </summary>
		/// <param name="database_name">The name of the database whose database ID number <c>DB_ID</c> will return.
		/// If the call to <c>DB_ID</c> omits <c>database_name</c>, <c>DB_ID</c> returns the ID of the current database.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DB_ID", ServerSideOnly=true)]
		public static int? DbID(string database_name)
			=> throw new ServerSideOnlyException(nameof(DbID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/db-id-transact-sql">DB_ID (Transact-SQL)</see></b></para>
		/// <para>This function returns the database identification (ID) number of a specified database.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DB_ID", ServerSideOnly=true)]
		public static int DbID()
			=> throw new ServerSideOnlyException(nameof(DbID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/db-name-transact-sql">DB_NAME (Transact-SQL)</see></b></para>
		/// <para>This function returns the name of a specified database.</para>
		/// </summary>
		/// <param name="database_id">The identification number (ID) of the database whose name <c>DB_NAME</c> will return.
		/// If the call to <c>DB_NAME</c> omits <c>database_id</c>, <c>DB_NAME</c> returns the name of the current database.</param>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DB_NAME", ServerSideOnly=true)]
		public static string? DbName(int database_id)
			=> throw new ServerSideOnlyException(nameof(DbName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/db-name-transact-sql">DB_NAME (Transact-SQL)</see></b></para>
		/// <para>This function returns the name of a specified database.</para>
		/// </summary>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DB_NAME", ServerSideOnly=true)]
		public static string DbName()
			=> throw new ServerSideOnlyException(nameof(DbName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/file-id-transact-sql">FILE_ID (Transact-SQL)</see></b></para>
		/// <para>For the given logical name for a component file of the current database, this function returns the file identification (ID) number.</para>
		/// </summary>
		/// <param name="file_name">An expression of type <b>sysname</b>, representing the logical name of the file whose file ID value <c>FILE_ID</c> will return.</param>
		/// <returns>smallint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FILE_ID", ServerSideOnly=true)]
		public static short? FileID(string? file_name)
			=> throw new ServerSideOnlyException(nameof(FileID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/file-idex-transact-sql">FILE_IDEX (Transact-SQL)</see></b></para>
		/// <para>This function returns the file identification (ID) number for the specified logical name of a data, log, or full-text file of the current database.</para>
		/// </summary>
		/// <param name="file_name">An expression of type <b>sysname</b> that returns the file ID value 'FILE_IDEX' for the name of the file.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FILE_IDEX", ServerSideOnly=true)]
		public static int? FileIDEx(string? file_name)
			=> throw new ServerSideOnlyException(nameof(FileIDEx));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/file-name-transact-sql">FILE_NAME (Transact-SQL)</see></b></para>
		/// <para>This function returns the logical file name for a given file identification (ID) number.</para>
		/// </summary>
		/// <param name="file_id">The file identification number whose file name <c>FILE_NAME</c> will return. file_id has an <b>int</b> data type.</param>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FILE_NAME", ServerSideOnly=true)]
		public static string? FileName(int? file_id)
			=> throw new ServerSideOnlyException(nameof(FileName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/filegroup-id-transact-sql">FILEGROUP_ID (Transact-SQL)</see></b></para>
		/// <para>This function returns the filegroup identification (ID) number for a specified filegroup name.</para>
		/// </summary>
		/// <param name="filegroup_name">An expression of type <b>sysname</b>, representing the filegroup name whose filegroup ID <c>FILEGROUP_ID</c> will return.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FILEGROUP_ID", ServerSideOnly=true)]
		public static int? FileGroupID(string? filegroup_name)
			=> throw new ServerSideOnlyException(nameof(FileGroupID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/filegroup-name-transact-sql">FILEGROUP_NAME (Transact-SQL)</see></b></para>
		/// <para>This function returns the filegroup name for the specified filegroup identification (ID) number.</para>
		/// </summary>
		/// <param name="filegroup_id">The filegroup ID number whose filegroup name <c>FILEGROUP_NAME</c> will return. filegroup_id has a <b>smallint</b> data type.</param>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FILEGROUP_Name", ServerSideOnly=true)]
		public static string? FileGroupName(short? filegroup_id)
			=> throw new ServerSideOnlyException(nameof(FileGroupName));

		public enum FileGroupPropertyName
		{
			IsReadOnly, IsUserDefinedFG, IsDefault,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FILEGROUPPROPERTY-transact-sql">FILEGROUPPROPERTY (Transact-SQL)</see></b></para>
		/// <para>This function returns the filegroup property value for a specified name and filegroup value.</para>
		/// </summary>
		/// <param name="filegroup_name">An expression of type <b>sysname</b> that represents the filegroup name for which
		/// <c>FILEGROUPPROPERTY</c> returns the named property information.</param>
		/// <param name="property">An expression of type <b>varchar(128)</b> that returns the name of the filegroup property.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "FILEGROUPPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<FileGroupPropertyName>))]
		public static int? FileGroupProperty(string? filegroup_name, [SqlQueryDependent] FileGroupPropertyName property)
			=> throw new ServerSideOnlyException(nameof(FileGroupProperty));

		public enum FilePropertyName
		{
			IsReadOnly, IsPrimaryFile, IsLogFile, SpaceUsed,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FILEPROPERTY-transact-sql">FILEPROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns the specified file name property value when a file name in the current database and a property name are specified.
		/// Returns NULL for files that are not in the current database.</para>
		/// </summary>
		/// <param name="file_name">Is an expression that contains the name of the file associated with the current database for which to return property information.
		/// <c>file_name</c> is <b>nchar(128)</b>.</param>
		/// <param name="property">Is an expression that contains the name of the file property to return. <c>property</c> is <b>varchar(128)</b>,
		/// and can be one of the following values.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "FILEPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<FilePropertyName>))]
		public static int? FileProperty(string? file_name, [SqlQueryDependent] FilePropertyName property)
			=> throw new ServerSideOnlyException(nameof(FileProperty));

		public enum FilePropertyExName
		{
			BlobTier, AccountType, IsInferredTier, IsPageBlob,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FILEPROPERTYEX-transact-sql">FILEPROPERTYEX (Transact-SQL)</see></b></para>
		/// <para>Returns the specified extended file property value when a file name in the current database and a property name are specified.
		/// Returns NULL for files that are not in the current database or for extended file properties that do not exist.
		/// Currently, extended file properties only apply to databases that are in Azure Blob storage.</para>
		/// </summary>
		/// <param name="file_name">Is an expression that contains the name of the file associated with the current database for which to return property information.
		/// <c>file_name</c> is <b>nchar(128)</b>.</param>
		/// <param name="property">Is an expression that contains the name of the file property to return. <c>property</c> is <b>varchar(128)</b>,
		/// and can be one of the following values.</param>
		/// <returns>sql_variant</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "FILEPROPERTYEX", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<FilePropertyExName>))]
		public static object? FilePropertyEx(string? file_name, [SqlQueryDependent] FilePropertyExName property)
			=> throw new ServerSideOnlyException(nameof(FilePropertyEx));

		public enum FullTextCatalogPropertyName
		{
			AccentSensitivity,     IndexSize,      ItemCount,      LogSize,      MergeStatus,
			PopulateCompletionAge, PopulateStatus, UniqueKeyCount, ImportStatus,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FULLTEXTCATALOGPROPERTY-transact-sql">FULLTEXTCATALOGPROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns information about full-text catalog properties in SQL Server.</para>
		/// </summary>
		/// <param name="catalog_name">Is an expression containing the name of the full-text catalog.</param>
		/// <param name="property">Is an expression containing the name of the full-text catalog property.
		/// The table lists the properties and provides descriptions of the information returned.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "FULLTEXTCATALOGPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<FullTextCatalogPropertyName>))]
		public static int? FullTextCatalogProperty(string? catalog_name, [SqlQueryDependent] FullTextCatalogPropertyName property)
			=> throw new ServerSideOnlyException(nameof(FullTextCatalogProperty));

		public enum FullTextServicePropertyName
		{
			ResourceUsage, ConnectTimeout, IsFulltextInstalled, DataTimeout, LoadOSResources, VerifySignature,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FULLTEXTSERVICEPROPERTY-transact-sql">FULLTEXTSERVICEPROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns information related to the properties of the Full-Text Engine. These properties can be set and retrieved by using <b>sp_fulltext_service</b>.</para>
		/// </summary>
		/// <param name="property">Is an expression containing the name of the full-text service-level property.
		/// The table lists the properties and provides descriptions of the information returned.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "FULLTEXTSERVICEPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<FullTextServicePropertyName>))]
		public static int? FullTextServiceProperty([SqlQueryDependent] FullTextServicePropertyName property)
			=> throw new ServerSideOnlyException(nameof(FullTextServiceProperty));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/index-col-transact-sql">INDEX_COL (Transact-SQL)</see></b></para>
		/// <para>Returns the indexed column name. Returns NULL for XML indexes.</para>
		/// </summary>
		/// <param name="table_or_view">Is the name of the table or indexed view. <c>table_or_view_name</c> must be delimited by single quotation marks and
		/// can be fully qualified by database name and schema name.</param>
		/// <param name="index_id">Is the ID of the index. <c>index_ID</c> is <b>int</b>.</param>
		/// <param name="key_id">Is the index key column position. <c>key_ID</c> is <b>int</b>.</param>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "INDEX_COL", ServerSideOnly=true)]
		public static string? IndexColumn(string table_or_view, int index_id, int key_id)
			=> throw new ServerSideOnlyException(nameof(IndexColumn));

		public enum IndexKeyPropertyName
		{
			ColumnId,
			IsDescending,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/INDEXKEY-PROPERTY-transact-sql">INDEXKEY_PROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns information about the index key. Returns NULL for XML indexes.</para>
		/// </summary>
		/// <param name="object_ID">Is the object identification number of the table or indexed view. <c>object_ID</c> is <b>int</b>.</param>
		/// <param name="index_ID">Is the index identification number. <c>index_ID</c> is <b>int</b>.</param>
		/// <param name="key_ID">Is the index key column position. <c>key_ID</c> is <b>int</b>.</param>
		/// <param name="property">Is the name of the property for which information will be returned.
		/// <c>property</c> is a character string and can be one of the following values.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "INDEXKEY_PROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<IndexKeyPropertyName>))]
		public static int? IndexKeyProperty(int? object_ID, int? index_ID, int? key_ID, [SqlQueryDependent] IndexKeyPropertyName property)
			=> throw new ServerSideOnlyException(nameof(IndexKeyProperty));

		public enum IndexPropertyName
		{
			IndexDepth,          IndexFillFactor, IndexID,        IsAutoStatistics, IsClustered,
			IsDisabled,          IsFulltextKey,   IsHypothetical, IsPadIndex,       IsPageLockDisallowed,
			IsRowLockDisallowed, IsStatistics,    IsUnique,       IsColumnstore,    IsOptimizedForSequentialKey,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/INDEXPROPERTY-transact-sql">INDEXPROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns information about the index key. Returns NULL for XML indexes.</para>
		/// </summary>
		/// <param name="object_ID">Is an expression that contains the object identification number of the table or indexed view for
		/// which to provide index property information.  <c>object_ID</c> is <b>int</b>.</param>
		/// <param name="index_or_statistics_name">Is an expression that contains the name of the index or statistics for which to return property information.
		/// <c>index_or_statistics_name</c> is <b>nvarchar(128).</b></param>
		/// <param name="property">Is an expression that contains the name of the database property to return.
		/// <c>property</c> is <b>varchar(128)</b>, and can be one of these values.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "INDEXPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<IndexPropertyName>))]
		public static int? IndexProperty(int? object_ID, string? index_or_statistics_name, [SqlQueryDependent] IndexPropertyName property)
			=> throw new ServerSideOnlyException(nameof(IndexProperty));

		sealed class NextValueForBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.AddFragment("sequence_name", builder.GetValue<string>("sequence_name"));
			}
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/NEXT-VALUE-FOR-transact-sql">NEXT VALUE FOR (Transact-SQL)</see></b></para>
		/// <para>Generates a sequence number from the specified sequence object.</para>
		/// </summary>
		/// <param name="sequence_name">The name of the sequence object that generates the number.</param>
		/// <returns>Returns a number using the type of the sequence.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "NEXT VALUE FOR {sequence_name}", ServerSideOnly=true, BuilderType=typeof(NextValueForBuilder))]
		public static object? NextValueFor([SqlQueryDependent] string sequence_name)
			=> throw new ServerSideOnlyException(nameof(NextValueFor));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/NEXT-VALUE-FOR-transact-sql">NEXT VALUE FOR (Transact-SQL)</see></b></para>
		/// <para>Generates a sequence number from the specified sequence object.</para>
		/// </summary>
		/// <param name="sequence_name">The name of the sequence object that generates the number.</param>
		/// <returns>Returns a number using the type of the sequence.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "NEXT VALUE FOR {sequence_name} OVER ({order_by_clause})",
			ServerSideOnly=true, BuilderType=typeof(NextValueForBuilder), TokenName=AnalyticFunctions.FunctionToken, ChainPrecedence=1, IsWindowFunction=true)]
		public static AnalyticFunctions.INeedsOrderByOnly<object?> NextValueForOver([SqlQueryDependent] string sequence_name)
			=> throw new ServerSideOnlyException(nameof(NextValueForOver));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OBJECT-DEFINITION-transact-sql">OBJECT_DEFINITION (Transact-SQL)</see></b></para>
		/// <para>Returns the Transact-SQL source text of the definition of a specified object.</para>
		/// </summary>
		/// <param name="object_id">Is the ID of the object to be used. <c>object_id</c> is <b>int</b>,
		/// and assumed to represent an object in the current database context.</param>
		/// <returns>nvarchar(max)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "OBJECT_DEFINITION", ServerSideOnly=true)]
		public static string? ObjectDefinition(int? object_id)
			=> throw new ServerSideOnlyException(nameof(ObjectDefinition));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/object-id-transact-sql">OBJECT_ID (Transact-SQL)</see></b></para>
		/// <para>Returns the database object identification number of a schema-scoped object.</para>
		/// </summary>
		/// <param name="object_name">Is the object to be used. <c>object_name</c> is either <b>varchar</b> or <b>nvarchar</b>. If <c>object_name</c> is <b>varchar</b>,
		/// it is implicitly converted to <b>nvarchar</b>. Specifying the database and schema names is optional.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "OBJECT_ID", ServerSideOnly=true)]
		public static int? ObjectID(string object_name)
			=> throw new ServerSideOnlyException(nameof(ObjectID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/object-id-transact-sql">OBJECT_ID (Transact-SQL)</see></b></para>
		/// <para>Returns the database object identification number of a schema-scoped object.</para>
		/// </summary>
		/// <param name="object_name">Is the object to be used. <c>object_name</c> is either <b>varchar</b> or <b>nvarchar</b>. If <c>object_name</c> is <b>varchar</b>,
		/// it is implicitly converted to <b>nvarchar</b>. Specifying the database and schema names is optional.</param>
		/// <param name="object_type">Is the schema-scoped object type. <c>object_type</c> is either <b>varchar</b> or <b>nvarchar</b>.
		/// If <c>object_type</c> is varchar, it is implicitly converted to <b>nvarchar</b>. For a list of object <b>types</b>, see the type column in
		/// <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "OBJECT_ID", ServerSideOnly=true)]
		public static int? ObjectID(string object_name, string object_type)
			=> throw new ServerSideOnlyException(nameof(ObjectID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/object-name-transact-sql">OBJECT_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the database object name for schema-scoped objects. For a list of schema-scoped objects,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.</para>
		/// </summary>
		/// <param name="object_id">Is the ID of the object to be used. <c>object_id</c> is <b>int</b> and is assumed to be
		/// a schema-scoped object in the specified database, or in the current database context.</param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "OBJECT_NAME", ServerSideOnly=true)]
		public static string? ObjectName(int? object_id)
			=> throw new ServerSideOnlyException(nameof(ObjectName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/object-name-transact-sql">OBJECT_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the database object name for schema-scoped objects. For a list of schema-scoped objects,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.</para>
		/// </summary>
		/// <param name="object_id">Is the ID of the object to be used. <c>object_id</c> is <b>int</b> and is assumed to be
		/// a schema-scoped object in the specified database, or in the current database context.</param>
		/// <param name="database_id">Is the ID of the database where the object is to be looked up. <c>database_id</c> is <b>int</b>.</param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "OBJECT_NAME", ServerSideOnly=true)]
		public static string? ObjectName(int? object_id, int? database_id)
			=> throw new ServerSideOnlyException(nameof(ObjectName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/object-schema-name-transact-sql">OBJECT_SCHEMA_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the database schema name for schema-scoped objects. For a list of schema-scoped objects,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.</para>
		/// </summary>
		/// <param name="object_id">Is the ID of the object to be used. <c>object_id</c> is <b>int</b> and is assumed to be
		/// a schema-scoped object in the specified database, or in the current database context.</param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "OBJECT_SCHEMA_NAME", ServerSideOnly=true)]
		public static string? ObjectSchemaName(int? object_id)
			=> throw new ServerSideOnlyException(nameof(ObjectSchemaName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/object-schema-name-transact-sql">OBJECT_SCHEMA_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the database schema name for schema-scoped objects. For a list of schema-scoped objects,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.</para>
		/// </summary>
		/// <param name="object_id">Is the ID of the object to be used. <c>object_id</c> is <b>int</b> and is assumed to be
		/// a schema-scoped object in the specified database, or in the current database context.</param>
		/// <param name="database_id">Is the ID of the database where the object is to be looked up. <c>database_id</c> is <b>int</b>.</param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "OBJECT_SCHEMA_NAME", ServerSideOnly=true)]
		public static string? ObjectSchemaName(int? object_id, int? database_id)
			=> throw new ServerSideOnlyException(nameof(ObjectSchemaName));

		public enum ObjectPropertyName
		{
			CnstIsClustKey,           CnstIsColumn,             CnstIsDeleteCascade,      CnstIsDisabled,           CnstIsNonclustKey,
			CnstIsNotRepl,            CnstIsNotTrusted,         CnstIsUpdateCascade,      ExecIsAfterTrigger,       ExecIsAnsiNullsOn,
			ExecIsDeleteTrigger,      ExecIsFirstDeleteTrigger, ExecIsFirstInsertTrigger, ExecIsFirstUpdateTrigger, ExecIsInsertTrigger,
			ExecIsInsteadOfTrigger,   ExecIsLastDeleteTrigger,  ExecIsLastInsertTrigger,  ExecIsLastUpdateTrigger,  ExecIsQuotedIdentOn,
			ExecIsStartup,            ExecIsTriggerDisabled,    ExecIsTriggerNotForRepl,  ExecIsUpdateTrigger,      ExecIsWithNativeCompilation,
			HasAfterTrigger,          HasDeleteTrigger,         HasInsertTrigger,         HasInsteadOfTrigger,      HasUpdateTrigger,
			IsAnsiNullsOn,            IsCheckCnst,              IsConstraint,             IsDefault,                IsDefaultCnst,
			IsDeterministic,          IsEncrypted,              IsExecuted,               IsExtendedProc,           IsForeignKey,
			IsIndexed,                IsIndexable,              IsInlineFunction,         IsMSShipped,              IsPrimaryKey,
			IsProcedure,              IsQuotedIdentOn,          IsQueue,                  IsReplProc,               IsRule,
			IsScalarFunction,         IsSchemaBound,            IsSystemTable,            IsSystemVerified,         IsTable,
			IsTableFunction,          IsTrigger,                IsUniqueCnst,             IsUserTable,              IsView,
			OwnerId,                  SchemaId,                 TableDeleteTrigger,       TableDeleteTriggerCount,  TableFullTextBackgroundUpdateIndexOn,
			TableFullTextMergeStatus, TableFulltextCatalogId,   TableFulltextFailCount,   TableFulltextItemCount,   TableFulltextDocsProcessed,
			TableFulltextKeyColumn,   TableHasCheckCnst,        TableHasClustIndex,       TableHasDefaultCnst,      TableFulltextChangeTrackingOn,
			TableHasDeleteTrigger,    TableHasForeignKey,       TableHasForeignRef,       TableHasIdentity,         TableFulltextPendingChanges,
			TableHasIndex,            TableHasInsertTrigger,    TableHasNonclustIndex,    TableHasPrimaryKey,       TableFulltextPopulateStatus,
			TableHasRowGuidCol,       TableHasTextImage,        TableHasTimestamp,        TableHasUniqueCnst,       TableHasActiveFulltextIndex,
			TableHasUpdateTrigger,    TableInsertTrigger,       TableInsertTriggerCount,  TableIsFake,              TableHasVarDecimalStorageFormat,
			TableIsLockedOnBulkLoad,  TableIsMemoryOptimized,   TableIsPinned,            TableTextInRowLimit,      TableUpdateTrigger,
			TableUpdateTriggerCount,  TableHasColumnSet,        TableTemporalType,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OBJECTPROPERTY-transact-sql">OBJECTPROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns information about schema-scoped objects in the current database.
		/// For a list of schema-scoped objects, see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.
		/// This function cannot be used for objects that are not schema-scoped, such as data definition language (DDL) triggers and event notifications.</para>
		/// </summary>
		/// <param name="id">Is an expression that represents the ID of the object in the current database.
		/// <c>id</c> is <b>int</b> and is assumed to be a schema-scoped object in the current database context.</param>
		/// <param name="property">Is an expression that represents the information to be returned for the object specified by <c>id</c>.
		/// <c>property</c> can be one of the following values.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "OBJECTPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<ObjectPropertyName>))]
		public static int? ObjectProperty(int? id, [SqlQueryDependent] ObjectPropertyName property)
			=> throw new ServerSideOnlyException(nameof(ObjectProperty));

		public enum ObjectPropertyExName
		{
			BaseType,               CnstIsClustKey,          CnstIsColumn,             CnstIsDeleteCascade,      CnstIsDisabled,
			CnstIsNonclustKey,      CnstIsNotRepl,           CnstIsNotTrusted,         CnstIsUpdateCascade,      ExecIsAfterTrigger,
			ExecIsAnsiNullsOn,      ExecIsDeleteTrigger,     ExecIsFirstDeleteTrigger, ExecIsFirstInsertTrigger, ExecIsFirstUpdateTrigger,
			ExecIsInsertTrigger,    ExecIsInsteadOfTrigger,  ExecIsLastDeleteTrigger,  ExecIsLastInsertTrigger,  ExecIsLastUpdateTrigger,
			ExecIsQuotedIdentOn,    ExecIsStartup,           ExecIsTriggerDisabled,    ExecIsTriggerNotForRepl,  ExecIsUpdateTrigger,
			HasAfterTrigger,        HasDeleteTrigger,        HasInsertTrigger,         HasInsteadOfTrigger,      ExecIsWithNativeCompilation,
			HasUpdateTrigger,       IsAnsiNullsOn,           IsCheckCnst,              IsConstraint,             IsDefault,
			IsDefaultCnst,          IsDeterministic,         IsEncrypted,              IsExecuted,               IsExtendedProc,
			IsForeignKey,           IsIndexed,               IsIndexable,              IsInlineFunction,         IsMSShipped,
			IsPrecise,              IsPrimaryKey,            IsProcedure,              IsQuotedIdentOn,          IsQueue,
			IsReplProc,             IsRule,                  IsScalarFunction,         IsSchemaBound,            IsSystemTable,
			IsSystemVerified,       IsTable,                 IsTableFunction,          IsTrigger,                IsUniqueCnst,
			IsUserTable,            IsView,                  OwnerId,                  SchemaId,                 SystemDataAccess,
			TableDeleteTrigger,     TableDeleteTriggerCount, TableFullTextMergeStatus, TableFulltextCatalogId,   TableFullTextBackgroundUpdateIndexOn,
			TableFulltextFailCount, TableFulltextItemCount,  TableFulltextKeyColumn,   TableHasCheckCnst,        TableFullTextChangeTrackingOn,
			TableHasClustIndex,     TableHasDefaultCnst,     TableHasDeleteTrigger,    TableHasForeignKey,       TableFulltextDocsProcessed,
			TableHasForeignRef,     TableHasIdentity,        TableHasIndex,            TableHasInsertTrigger,    TableFulltextPendingChanges,
			TableHasNonclustIndex,  TableHasPrimaryKey,      TableHasRowGuidCol,       TableHasTextImage,        TableFulltextPopulateStatus,
			TableHasTimestamp,      TableHasUniqueCnst,      TableHasUpdateTrigger,    TableInsertTrigger,       TableFullTextSemanticExtraction,
			TableIsFake,            TableInsertTriggerCount, TableIsLockedOnBulkLoad,  TableIsMemoryOptimized,   TableHasActiveFulltextIndex,
			TableIsPinned,          TableTextInRowLimit,     TableUpdateTrigger,       TableUpdateTriggerCount,  TableHasVarDecimalStorageFormat,
			UserDataAccess,         TableHasColumnSet,       Cardinality,              TableTemporalType,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/OBJECTPROPERTYEX-transact-sql">OBJECTPROPERTYEX (Transact-SQL)</see></b></para>
		/// <para>Returns information about schema-scoped objects in the current database. For a list of these objects,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.
		/// OBJECTPROPERTYEX cannot be used for objects that are not schema-scoped, such as data definition language (DDL) triggers and event notifications.</para>
		/// </summary>
		/// <param name="id">Is an expression that represents the ID of the object in the current database.
		/// <c>id</c> is <b>int</b> and is assumed to be a schema-scoped object in the current database context.</param>
		/// <param name="property">Is an expression that contains the information to be returned for the object specified by <c>id</c>.
		/// The return type is <b>sql_variant</b>. The following table shows the base data type for each property value.</param>
		/// <returns>sql_variant</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "OBJECTPROPERTYEX", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<ObjectPropertyExName>))]
		public static object? ObjectPropertyEx(int? id, [SqlQueryDependent] ObjectPropertyExName property)
			=> throw new ServerSideOnlyException(nameof(ObjectPropertyEx));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ORIGINAL-DB-NAME-transact-sql">ORIGINAL_DB_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the database name specified by the user in the database connection string.
		/// This database is specified by using the <b>sqlcmd-d</b> option (USE <c>database</c>).
		/// It can also be specified with the Open Database Connectivity (ODBC) data source expression (initial catalog = <c>databasename</c>). </para>
		/// <para>This database is different from the default user database.</para>
		/// </summary>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ORIGINAL_DB_NAME", ServerSideOnly=true)]
		public static string? OriginalDbName()
			=> throw new ServerSideOnlyException(nameof(OriginalDbName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PARSENAME-transact-sql">PARSENAME (Transact-SQL)</see></b></para>
		/// <para>Returns the database schema name for schema-scoped objects. For a list of schema-scoped objects,
		/// see <see href="https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-objects-transact-sql">sys.objects (Transact-SQL)</see>.</para>
		/// </summary>
		/// <param name="object_name">Is the parameter that holds the name of the object for which to retrieve the specified object part.
		/// This parameter is an optionally-qualified object name. If all parts of the object name are qualified, this name can have four parts: the server name,
		/// the database name, the schema name, and the object name. Each part of the 'object_name' string is type
		/// sysname which is equivalent to nvarchar(128) or 256 bytes. If any part of the string exceeds 256 bytes,
		/// PARSENAME will return NULL for that part as it is not a valid sysname.</param>
		/// <param name="object_piece">Is the object part to return. object_piece is of type int, and can have these values:
		/// <list type="bullet">
		/// <item>1 = Object name</item>
		/// <item>2 = Schema name</item>
		/// <item>3 = Database name</item>
		/// <item>4 = Server name</item>
		/// </list></param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "PARSENAME", ServerSideOnly=true)]
		public static string? ParseName(string? object_name, int object_piece)
			=> throw new ServerSideOnlyException(nameof(ParseName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/schema-id-transact-sql">SCHEMA_ID (Transact-SQL)</see></b></para>
		/// <para>Returns the schema ID associated with a schema name.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SCHEMA_ID", ServerSideOnly=true)]
		public static int? SchemaID()
			=> throw new ServerSideOnlyException(nameof(SchemaID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/schema-id-transact-sql">SCHEMA_ID (Transact-SQL)</see></b></para>
		/// <para>Returns the schema ID associated with a schema name.</para>
		/// </summary>
		/// <param name="schema_name">Is the name of the schema. <c>schema_name</c> is a <b>sysname</b>.
		/// If <c>schema_name</c> is not specified, SCHEMA_ID will return the ID of the default schema of the caller.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SCHEMA_ID", ServerSideOnly=true)]
		public static int? SchemaID(string schema_name)
			=> throw new ServerSideOnlyException(nameof(SchemaID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SCHEMA-NAME-transact-sql">SCHEMA_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the schema name associated with a schema ID.</para>
		/// </summary>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SCHEMA_NAME", ServerSideOnly=true)]
		public static string? SchemaName()
			=> throw new ServerSideOnlyException(nameof(SchemaName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SCHEMA-NAME-transact-sql">SCHEMA_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the schema name associated with a schema ID.</para>
		/// </summary>
		/// <param name="schema_id">The ID of the schema. <c>schema_id</c> is an <b>int</b>.
		/// If <c>schema_id</c> is not defined, SCHEMA_NAME will return the name of the default schema of the caller.</param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SCHEMA_NAME", ServerSideOnly=true)]
		public static string? SchemaName(int? schema_id)
			=> throw new ServerSideOnlyException(nameof(SchemaName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SCOPE-IDENTITY-transact-sql">SCOPE_IDENTITY (Transact-SQL)</see></b></para>
		/// <para>Returns the last identity value inserted into an identity column in the same scope.
		/// A scope is a module: a stored procedure, trigger, function, or batch. Therefore,
		/// if two statements are in the same stored procedure, function, or batch, they are in the same scope.</para>
		/// </summary>
		/// <returns>numeric(38,0)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SCOPE_IDENTITY", ServerSideOnly=true)]
		public static decimal ScopeIdentity()
			=> throw new ServerSideOnlyException(nameof(ScopeIdentity));

		public enum ServerPropertyName
		{
			BuildClrVersion,     Collation,           CollationID,             ComparisonStyle,           ComputerNamePhysicalNetBIOS,
			Edition,             EditionID,           EngineEdition,           FilestreamConfiguredLevel, FilestreamEffectiveLevel,
			FilestreamShareName, HadrManagerStatus,   InstanceDefaultDataPath, InstanceDefaultLogPath,    InstanceDefaultBackupPath,
			InstanceName,        IsBigDataCluster,    IsClustered,             IsFullTextInstalled,       IsAdvancedAnalyticsInstalled,
			IsHadrEnabled,       IsLocalDB,           IsPolyBaseInstalled,     IsSingleUser,              IsExternalAuthenticationOnly,
			IsXTPSupported,      LCID,                LicenseType,             MachineName,               IsIntegratedSecurityOnly,
			NumLicenses,         ProcessID,           ProductBuild,            ProductBuildType,          IsTempDbMetadataMemoryOptimized,
			ProductLevel,        ProductMajorVersion, ProductMinorVersion,     ProductUpdateLevel,        ProductUpdateReference,
			ProductVersion,      ResourceVersion,     ServerName,              SqlCharSet,                ResourceLastUpdateDateTime,
			SqlCharSetName,      SqlSortOrder,        SqlSortOrderName,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SERVERPROPERTY-transact-sql">SERVERPROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns property information about the server instance.</para>
		/// </summary>
		/// <param name="property">Is an expression that contains the property information to be returned for the server.</param>
		/// <returns>sql_variant</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "SERVERPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<ServerPropertyName>))]
		public static object? ServerProperty([SqlQueryDependent] ServerPropertyName property)
			=> throw new ServerSideOnlyException(nameof(ServerProperty));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/STATS-DATE-transact-sql">STATS_DATE (Transact-SQL)</see></b></para>
		/// <para>Returns the date of the most recent update for statistics on a table or indexed view.</para>
		/// </summary>
		/// <param name="object_id">ID of the table or indexed view with the statistics.</param>
		/// <param name="stats_id">ID of the statistics object.</param>
		/// <returns>Returns <b>datetime</b> on success. Returns <b>NULL</b> if a statistics blob was not created.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "STATS_DATE", ServerSideOnly=true)]
		public static DateTime? StatsDate(int? object_id, int? stats_id)
			=> throw new ServerSideOnlyException(nameof(StatsDate));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TYPE-ID-transact-sql">TYPE_ID (Transact-SQL)</see></b></para>
		/// <para>Returns the ID for a specified data type name.</para>
		/// </summary>
		/// <param name="type_name">Is the name of the data type. type_name is of type <b>nvarchar</b>.
		/// <c>type_name</c> can be a system or user-defined data type.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "TYPE_ID", ServerSideOnly=true)]
		public static int? TypeID(string type_name)
			=> throw new ServerSideOnlyException(nameof(TypeID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TYPE-NAME-transact-sql">TYPE_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the unqualified type name of a specified type ID.</para>
		/// </summary>
		/// <param name="type_id">Is the ID of the type that will be used. <c>type_id</c> is an <b>int</b>,
		/// and it can refer to a type in any schema that the caller has permission to access.</param>
		/// <returns>sysname</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "TYPE_NAME", ServerSideOnly=true)]
		public static string? TypeName(int? type_id)
			=> throw new ServerSideOnlyException(nameof(TypeName));

		public enum TypePropertyName
		{
			AllowsNull, OwnerId, Precision, Scale, UsesAnsiTrim,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TYPEPROPERTY-transact-sql">TYPEPROPERTY (Transact-SQL)</see></b></para>
		/// <para>Returns information about a data type.</para>
		/// </summary>
		/// <param name="type">Is the name of the data type.</param>
		/// <param name="property">Is the type of information to be returned for the data type.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "TYPEPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<TypePropertyName>))]
		public static int? TypeProperty(string? type, [SqlQueryDependent] TypePropertyName property)
			=> throw new ServerSideOnlyException(nameof(TypeProperty));

		#endregion

		#region Replication

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/replication-functions-publishingservername">PUBLISHINGSERVERNAME (Transact-SQL)</see></b></para>
		/// <para>Returns the name of the originating Publisher for a published database participating in a database mirroring session.
		/// This function is executed at a Publisher instance of SQL Server on the publication database.
		/// Use it to determine the original Publisher of the published database.</para>
		/// </summary>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "PUBLISHINGSERVERNAME", ServerSideOnly=true)]
		public static string? PublishingServerName()
			=> throw new ServerSideOnlyException(nameof(PublishingServerName));

		#endregion

		#region String

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ASCII-transact-sql">ASCII (Transact-SQL)</see></b></para>
		/// <para>Returns the ASCII code value of the leftmost character of a character expression.</para>
		/// </summary>
		/// <param name="character_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>char</b> or <b>varchar</b>.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ASCII", ServerSideOnly=true)]
		public static int Ascii(char character_expression)
			=> throw new ServerSideOnlyException(nameof(Ascii));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ASCII-transact-sql">ASCII (Transact-SQL)</see></b></para>
		/// <para>Returns the ASCII code value of the leftmost character of a character expression.</para>
		/// </summary>
		/// <param name="character_expression">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of type <b>char</b> or <b>varchar</b>.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ASCII", ServerSideOnly=true)]
		public static int? Ascii(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(Ascii));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHAR-transact-sql">CHAR (Transact-SQL)</see></b></para>
		/// <para>Returns the ASCII code value of the leftmost character of a character expression.</para>
		/// </summary>
		/// <param name="integer_expression">An integer from 0 through 255. <c>CHAR</c> returns a <c>NULL</c> value for integer expressions
		/// outside this input range or not representing a complete character. <c>CHAR</c> also returns a <c>NULL</c> value when
		/// the character exceeds the length of the return type. Many common character sets share ASCII as a sub-set and will
		/// return the same character for integer values in the range 0 through 127.</param>
		/// <returns>char(1)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHAR", ServerSideOnly=true)]
		public static char? Char(int? integer_expression)
			=> throw new ServerSideOnlyException(nameof(Char));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHARINDEX-transact-sql">CHARINDEX (Transact-SQL)</see></b></para>
		/// <para>This function searches for one character expression inside a second character expression,
		/// returning the starting position of the first expression if found.</para>
		/// </summary>
		/// <param name="expressionToFind">A character <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// containing the sequence to find. <c>expressionToFind</c> has an 8000 character limit.</param>
		/// <param name="expressionToSearch">A character expression to search.</param>
		/// <returns><b>bigint</b> if <c>expressionToSearch</c> has an <b>nvarchar(max)</b>, <b>varbinary(max)</b>, or <b>varchar(max)</b> data type; <b>int</b> otherwise.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHARINDEX", ServerSideOnly=true)]
		public static int? CharIndex(string? expressionToFind, string? expressionToSearch)
			=> throw new ServerSideOnlyException(nameof(CharIndex));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHARINDEX-transact-sql">CHARINDEX (Transact-SQL)</see></b></para>
		/// <para>This function searches for one character expression inside a second character expression,
		/// returning the starting position of the first expression if found.</para>
		/// </summary>
		/// <param name="expressionToFind">A character <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// containing the sequence to find. <c>expressionToFind</c> has an 8000 character limit.</param>
		/// <param name="expressionToSearch">A character expression to search.</param>
		/// <returns><b>bigint</b> if <c>expressionToSearch</c> has an <b>nvarchar(max)</b>, <b>varbinary(max)</b>, or <b>varchar(max)</b> data type; <b>int</b> otherwise.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHARINDEX", ServerSideOnly=true)]
		public static long? CharIndexBig(string? expressionToFind, string? expressionToSearch)
			=> throw new ServerSideOnlyException(nameof(CharIndexBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHARINDEX-transact-sql">CHARINDEX (Transact-SQL)</see></b></para>
		/// <para>This function searches for one character expression inside a second character expression,
		/// returning the starting position of the first expression if found.</para>
		/// </summary>
		/// <param name="expressionToFind">A character <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// containing the sequence to find. <c>expressionToFind</c> has an 8000 character limit.</param>
		/// <param name="expressionToSearch">A character expression to search.</param>
		/// <param name="start_location">An <b>integer</b> or <b>bigint</b> expression at which the search starts.
		/// If <c>start_location</c> is not specified, has a negative value, or has a zero (0) value,
		/// the search starts at the beginning of <c>expressionToSearch</c>.</param>
		/// <returns><b>bigint</b> if <c>expressionToSearch</c> has an <b>nvarchar(max)</b>, <b>varbinary(max)</b>, or <b>varchar(max)</b> data type; <b>int</b> otherwise.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHARINDEX", ServerSideOnly=true)]
		public static int? CharIndex(string? expressionToFind, string? expressionToSearch, int? start_location)
			=> throw new ServerSideOnlyException(nameof(CharIndex));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHARINDEX-transact-sql">CHARINDEX (Transact-SQL)</see></b></para>
		/// <para>This function searches for one character expression inside a second character expression,
		/// returning the starting position of the first expression if found.</para>
		/// </summary>
		/// <param name="expressionToFind">A character <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// containing the sequence to find. <c>expressionToFind</c> has an 8000 character limit.</param>
		/// <param name="expressionToSearch">A character expression to search.</param>
		/// <param name="start_location">An <b>integer</b> or <b>bigint</b> expression at which the search starts.
		/// If <c>start_location</c> is not specified, has a negative value, or has a zero (0) value,
		/// the search starts at the beginning of <c>expressionToSearch</c>.</param>
		/// <returns><b>bigint</b> if <c>expressionToSearch</c> has an <b>nvarchar(max)</b>, <b>varbinary(max)</b>, or <b>varchar(max)</b> data type; <b>int</b> otherwise.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHARINDEX", ServerSideOnly=true)]
		public static long? CharIndex(string? expressionToFind, string? expressionToSearch, long? start_location)
			=> throw new ServerSideOnlyException(nameof(CharIndex));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHARINDEX-transact-sql">CHARINDEX (Transact-SQL)</see></b></para>
		/// <para>This function searches for one character expression inside a second character expression,
		/// returning the starting position of the first expression if found.</para>
		/// </summary>
		/// <param name="expressionToFind">A character <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// containing the sequence to find. <c>expressionToFind</c> has an 8000 character limit.</param>
		/// <param name="expressionToSearch">A character expression to search.</param>
		/// <param name="start_location">An <b>integer</b> or <b>bigint</b> expression at which the search starts.
		/// If <c>start_location</c> is not specified, has a negative value, or has a zero (0) value,
		/// the search starts at the beginning of <c>expressionToSearch</c>.</param>
		/// <returns><b>bigint</b> if <c>expressionToSearch</c> has an <b>nvarchar(max)</b>, <b>varbinary(max)</b>, or <b>varchar(max)</b> data type; <b>int</b> otherwise.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHARINDEX", ServerSideOnly=true)]
		public static long? CharIndexBig(string? expressionToFind, string? expressionToSearch, int? start_location)
			=> throw new ServerSideOnlyException(nameof(CharIndexBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CONCAT-transact-sql">CONCAT (Transact-SQL)</see></b></para>
		/// <para>This function returns a string resulting from the concatenation, or joining, of two or more string values in an end-to-end manner.</para>
		/// </summary>
		/// <param name="string_value">A string value to concatenate to the other values. The <c>CONCAT</c> function requires at least two
		/// <c>string_value</c> arguments, and no more than 254 <c>string_value</c> arguments.</param>
		/// <returns>string_value</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CONCAT", ServerSideOnly=true)]
		public static string? Concat(params string?[] string_value)
			=> throw new ServerSideOnlyException(nameof(Concat));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CONCAT-WS-transact-sql">CONCAT_WS (Transact-SQL)</see></b></para>
		/// <para>This function returns a string resulting from the concatenation, or joining, of two or more string values in an end-to-end manner.
		/// It separates those concatenated string values with the delimiter specified in the first function argument.
		/// (<c>CONCAT_WS</c> indicates concatenate with separator.)</para>
		/// </summary>
		/// <param name="separator">An expression of any character type (<c>char</c>, <c>nchar</c>, <c>nvarchar</c>, or <c>varchar</c>).</param>
		/// <param name="arguments">An expression of any type. The <c>CONCAT_WS</c> function requires at least two arguments, and no more than 254 arguments.</param>
		/// <returns>A string value whose length and type depend on the input.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CONCAT_WS", ServerSideOnly=true)]
		public static string? ConcatWithSeparator(string? separator, params string?[] arguments)
			=> throw new ServerSideOnlyException(nameof(ConcatWithSeparator));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DIFFERENCE-transact-sql">DIFFERENCE (Transact-SQL)</see></b></para>
		/// <para>This function returns an integer value measuring the difference between the
		/// <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/soundex-transact-sql">SOUNDEX()</see> values of two different character expressions.</para>
		/// </summary>
		/// <param name="character_expression1">An alphanumeric
		/// <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see> of character data.
		/// <c>character_expression</c> can be a constant, variable, or column.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DIFFERENCE", ServerSideOnly=true)]
		public static int? Difference(string? character_expression1, string? character_expression2)
			=> throw new ServerSideOnlyException(nameof(Difference));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FORMAT-transact-sql">FORMAT (Transact-SQL)</see></b></para>
		/// <para>Returns a value formatted with the specified format and optional culture. Use the FORMAT function for locale-aware
		/// formatting of date/time and number values as strings. For general data type conversions, use CAST or CONVERT.</para>
		/// </summary>
		/// <param name="value">Expression of a supported data type to format. For a list of valid types, see the table in the following Remarks section.</param>
		/// <returns><b>nvarchar</b> or null</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FORMAT", ServerSideOnly=true)]
		public static string? Format(object? value, string? format)
			=> throw new ServerSideOnlyException(nameof(Format));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LEFT-transact-sql">LEFT (Transact-SQL)</see></b></para>
		/// <para>Returns the left part of a character string with the specified number of characters.</para>
		/// </summary>
		/// <param name="character_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of character or binary data. <c>character_expression</c> can be a constant, variable, or column.
		/// <c>character_expression</c> can be of any data type, except <b>text</b> or <b>ntext</b>,
		/// that can be implicitly converted to <b>varchar</b> or <b>nvarchar</b>. Otherwise, use the
		/// <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql">CAST</see>
		/// function to explicitly convert <c>character_expression</c>.</param>
		/// <param name="integer_expression">Is a positive integer that specifies how many characters of the character_expression will be returned.
		/// If <c>integer_expression</c> is negative, an error is returned. If <c>integer_expression</c> is type <b>bigint</b> and contains a large value,
		/// <c>character_expression</c> must be of a large data type such as <b>varchar(max)</b>.
		/// The <c>integer_expression</c> parameter counts a UTF-16 surrogate character as one character.</param>
		/// <returns>Returns <b>varchar</b> when <c>character_expression</c> is a non-Unicode character data type.
		/// Returns <b>nvarchar</b> when <c>character_expression</c> is a Unicode character data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LEFT", ServerSideOnly=true)]
		public static string? Left(string? character_expression, int? integer_expression)
			=> throw new ServerSideOnlyException(nameof(Left));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LEN-transact-sql">LEN (Transact-SQL)</see></b></para>
		/// <para>Returns the number of characters of the specified string expression, excluding trailing spaces.</para>
		/// </summary>
		/// <param name="character_expression">Is the string <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// to be evaluated. <c>character_expression</c> can be a constant, variable, or column of either character or binary data.</param>
		/// <returns><b>bigint</b> if expression is of the <b>varchar(max)</b>, <b>nvarchar(max)</b> or <b>varbinary(max)</b> data types; otherwise, <b>int</b>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LEN", ServerSideOnly=true)]
		public static int? Len(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(Len));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LEN-transact-sql">LEN (Transact-SQL)</see></b></para>
		/// <para>Returns the number of characters of the specified string expression, excluding trailing spaces.</para>
		/// </summary>
		/// <param name="character_expression">Is the string <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// to be evaluated. <c>character_expression</c> can be a constant, variable, or column of either character or binary data.</param>
		/// <returns><b>bigint</b> if expression is of the <b>varchar(max)</b>, <b>nvarchar(max)</b> or <b>varbinary(max)</b> data types; otherwise, <b>int</b>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LEN", ServerSideOnly=true)]
		public static long? LenBig(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(LenBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LOWER-transact-sql">LOWER (Transact-SQL)</see></b></para>
		/// <para>Returns a character expression after converting uppercase character data to lowercase.</para>
		/// </summary>
		/// <param name="character_expression">Is the string <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of character or binary data. <c>character_expression</c> can be a constant, variable, or column.
		/// <c>character_expression</c> must be of a data type that is implicitly convertible to <b>varchar</b>.</param>
		/// <returns><b>varchar</b> or <b>nvarchar</b></returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LOWER", ServerSideOnly=true)]
		public static string? Lower(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(Lower));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/LTRIM-transact-sql">LTRIM (Transact-SQL)</see></b></para>
		/// <para>Returns a character expression after it removes leading blanks.</para>
		/// </summary>
		/// <param name="character_expression">Is the string <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of character or binary data. <c>character_expression</c> can be a constant, variable, or column.
		/// <c>character_expression</c> must be of a data type that is implicitly convertible to <b>varchar</b>.</param>
		/// <returns><b>varchar</b> or <b>nvarchar</b></returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "LTRIM", ServerSideOnly=true)]
		public static string? LeftTrim(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(LeftTrim));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/NCHAR-transact-sql">NCHAR (Transact-SQL)</see></b></para>
		/// <para>Returns the Unicode character with the specified integer code, as defined by the Unicode standard.</para>
		/// </summary>
		/// <param name="integer_expression">When the collation of the database does not contain the Supplementary Character (SC) flag,
		/// this is a positive integer from 0 through 65535 (0 through 0xFFFF). If a value outside this range is specified, NULL is returned.</param>
		/// <returns><b>nchar(1)</b> when the default database collation does not support supplementary characters.
		/// <b>nvarchar(2)</b> when the default database collation supports supplementary characters.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "NCHAR", ServerSideOnly=true)]
		public static char? NChar(int? integer_expression)
			=> throw new ServerSideOnlyException(nameof(NChar));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PATINDEX-transact-sql">PATINDEX (Transact-SQL)</see></b></para>
		/// <para>Returns the starting position of the first occurrence of a pattern in a specified expression, or zeros if the pattern is not found, on all valid text and character data types.</para>
		/// </summary>
		/// <param name="pattern">Is a character expression that contains the sequence to be found. Wildcard characters can be used; however,
		/// the % character must come before and follow pattern (except when you search for first or last characters).
		/// <c>pattern</c> is an expression of the character string data type category. pattern is limited to 8000 characters.</param>
		/// <param name="expression">Is an expression, typically a column that is searched for the specified pattern.
		/// <c>expression</c> is of the character string data type category.</param>
		/// <returns><b>bigint</b> if <c>expression</c> is of the <b>varchar(max)</b> or <b>nvarchar(max)</b> data types; otherwise int.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "PATINDEX", ServerSideOnly=true)]
		public static int? PatIndex(string? pattern, string? expression)
			=> throw new ServerSideOnlyException(nameof(PatIndex));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PATINDEX-transact-sql">PATINDEX (Transact-SQL)</see></b></para>
		/// <para>Returns the starting position of the first occurrence of a pattern in a specified expression,
		/// or zeros if the pattern is not found, on all valid text and character data types.</para>
		/// </summary>
		/// <param name="pattern">Is a character expression that contains the sequence to be found. Wildcard characters can be used; however,
		/// the % character must come before and follow pattern (except when you search for first or last characters).
		/// <c>pattern</c> is an expression of the character string data type category. pattern is limited to 8000 characters.</param>
		/// <param name="expression">Is an expression, typically a column that is searched for the specified pattern.
		/// <c>expression</c> is of the character string data type category.</param>
		/// <returns><b>bigint</b> if <c>expression</c> is of the <b>varchar(max)</b> or <b>nvarchar(max)</b> data types; otherwise int.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "PATINDEX", ServerSideOnly=true)]
		public static long? PatIndexBig(string? pattern, string? expression)
			=> throw new ServerSideOnlyException(nameof(PatIndexBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/QUOTENAME-transact-sql">QUOTENAME (Transact-SQL)</see></b></para>
		/// <para>Returns a Unicode string with the delimiters added to make the input string a valid SQL Server delimited identifier.</para>
		/// </summary>
		/// <param name="character_string">Is a string of Unicode character data. <c>character_string</c> is <b>sysname</b> and is limited to 128 characters.
		/// Inputs greater than 128 characters return NULL.</param>
		/// <returns>nvarchar(258)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "QUOTENAME", ServerSideOnly=true)]
		public static string? QuoteName(string? character_string)
			=> throw new ServerSideOnlyException(nameof(QuoteName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/QUOTENAME-transact-sql">QUOTENAME (Transact-SQL)</see></b></para>
		/// <para>Returns a Unicode string with the delimiters added to make the input string a valid SQL Server delimited identifier.</para>
		/// </summary>
		/// <param name="character_string">Is a string of Unicode character data. <c>character_string</c> is <b>sysname</b> and is limited to 128 characters.
		/// Inputs greater than 128 characters return NULL.</param>
		/// <param name="quote_character">Is a one-character string to use as the delimiter.
		/// Can be a single quotation mark ( ' ), a left or right bracket ( [] ), a double quotation mark ( " ),
		/// a left or right parenthesis ( () ), a greater than or less than sign ( &gt;&lt; ), a left or right brace ( {} ) or a backtick ( ` ).
		/// NULL returns if an unacceptable character is supplied. If <c>quote_character</c> is not specified, brackets are used.</param>
		/// <returns>nvarchar(258)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "QUOTENAME", ServerSideOnly=true)]
		public static string? QuoteName(string? character_string, string? quote_character)
			=> throw new ServerSideOnlyException(nameof(QuoteName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/REPLACE-transact-sql">REPLACE (Transact-SQL)</see></b></para>
		/// <para>Replaces all occurrences of a specified string value with another string value.</para>
		/// </summary>
		/// <param name="string_expression">Is the string expression to be searched. <c>string_expression</c> can be of a character or binary data type.</param>
		/// <param name="string_pattern">Is the substring to be found. <c>string_pattern</c> can be of a character or binary data type.
		/// <c>string_pattern</c> must not exceed the maximum number of bytes that fits on a page.
		/// If <c>string_pattern</c> is an empty string (''), <c>string_expression</c> is returned unchanged.</param>
		/// <param name="string_replacement">Is the replacement string. <c>string_replacement</c> can be of a character or binary data type.</param>
		/// <returns>Returns <b>nvarchar</b> if one of the input arguments is of the <b>nvarchar</b> data type; otherwise,
		/// <b>REPLACE</b> returns <b>varchar</b>. Returns NULL if any one of the arguments is NULL.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "REPLACE", ServerSideOnly=true)]
		public static string? Replace(string? string_expression, string? string_pattern, string? string_replacement)
			=> throw new ServerSideOnlyException(nameof(Replace));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/REPLICATE-transact-sql">REPLICATE (Transact-SQL)</see></b></para>
		/// <para>Repeats a string value a specified number of times.</para>
		/// </summary>
		/// <param name="string_expression">Is an expression of a character string or binary data type.</param>
		/// <param name="integer_expression">Is an expression of any integer type, including <b>bigint</b>.
		/// If <c>integer_expression</c> is negative, NULL is returned.</param>
		/// <returns>Returns the same type as <c>string_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "REPLICATE", ServerSideOnly=true)]
		public static string? Replicate(string? string_expression, int? integer_expression)
			=> throw new ServerSideOnlyException(nameof(Replicate));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/REPLICATE-transact-sql">REPLICATE (Transact-SQL)</see></b></para>
		/// <para>Repeats a string value a specified number of times.</para>
		/// </summary>
		/// <param name="string_expression">Is an expression of a character string or binary data type.</param>
		/// <param name="integer_expression">Is an expression of any integer type, including <b>bigint</b>.
		/// If <c>integer_expression</c> is negative, NULL is returned.</param>
		/// <returns>Returns the same type as <c>string_expression</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "REPLICATE", ServerSideOnly=true)]
		public static string? Replicate(byte[]? string_expression, int? integer_expression)
			=> throw new ServerSideOnlyException(nameof(Replicate));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/REVERSE-transact-sql">REVERSE (Transact-SQL)</see></b></para>
		/// <para>Returns the reverse order of a string value.</para>
		/// </summary>
		/// <param name="string_expression"><c>string_expression</c> is an expression of a string or binary data type.
		/// <c>string_expression</c> can be a constant, variable, or column of either character or binary data.</param>
		/// <returns><b>varchar</b> or <b>nvarchar</b></returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "REVERSE", ServerSideOnly=true)]
		public static string? Reverse(string? string_expression)
			=> throw new ServerSideOnlyException(nameof(Reverse));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/RIGHT-transact-sql">RIGHT (Transact-SQL)</see></b></para>
		/// <para>Returns the right part of a character string with the specified number of characters.</para>
		/// </summary>
		/// <param name="character_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of character or binary data. <c>character_expression</c> can be a constant, variable, or column.
		/// <c>character_expression</c> can be of any data type, except <b>text</b> or <b>ntext</b>,
		/// that can be implicitly converted to <b>varchar</b> or <b>nvarchar</b>. Otherwise, use the
		/// <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql">CAST</see>
		/// function to explicitly convert <c>character_expression</c>.</param>
		/// <param name="integer_expression">Is a positive integer that specifies how many characters of the character_expression will be returned.
		/// If <c>integer_expression</c> is negative, an error is returned. If <c>integer_expression</c> is type <b>bigint</b> and contains a large value,
		/// <c>character_expression</c> must be of a large data type such as <b>varchar(max)</b>.
		/// The <c>integer_expression</c> parameter counts a UTF-16 surrogate character as one character.</param>
		/// <returns>Returns <b>varchar</b> when <c>character_expression</c> is a non-Unicode character data type.
		/// Returns <b>nvarchar</b> when <c>character_expression</c> is a Unicode character data type.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "RIGHT", ServerSideOnly=true)]
		public static string? Right(string? character_expression, int? integer_expression)
			=> throw new ServerSideOnlyException(nameof(Right));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/RTRIM-transact-sql">RTRIM (Transact-SQL)</see></b></para>
		/// <para>Returns a character string after truncating all trailing spaces.</para>
		/// </summary>
		/// <param name="character_expression">Is an <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of character data. character_expression can be a constant, variable, or column of either character or binary data.</param>
		/// <returns><b>varchar</b> or <b>nvarchar</b></returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "RTRIM", ServerSideOnly=true)]
		public static string? RightTrim(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(RightTrim));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SOUNDEX-transact-sql">SOUNDEX (Transact-SQL)</see></b></para>
		/// <para>Returns a four-character (SOUNDEX) code to evaluate the similarity of two strings.</para>
		/// </summary>
		/// <param name="character_expression">Is an alphanumeric expression of character data. character_expression can be a constant, variable, or column.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SOUNDEX", ServerSideOnly=true)]
		public static string? SoundEx(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(SoundEx));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SPACE-transact-sql">SPACE (Transact-SQL)</see></b></para>
		/// <para>Returns a string of repeated spaces.</para>
		/// </summary>
		/// <param name="integer_expression">Is a positive integer that indicates the number of spaces. If integer_expression is negative, a null string is returned.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SPACE", ServerSideOnly=true)]
		public static string? Space(int? integer_expression)
			=> throw new ServerSideOnlyException(nameof(Space));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/STR-transact-sql">STR (Transact-SQL)</see></b></para>
		/// <para>Returns character data converted from numeric data. The character data is right-justified, with a specified length and decimal precision.</para>
		/// </summary>
		/// <param name="float_expression">Is an expression of approximate numeric (<b>float</b>) data type with a decimal point.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "STR", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string? Str<T>(T? float_expression)
			=> throw new ServerSideOnlyException(nameof(Str));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/STR-transact-sql">STR (Transact-SQL)</see></b></para>
		/// <para>Returns character data converted from numeric data. The character data is right-justified, with a specified length and decimal precision.</para>
		/// </summary>
		/// <param name="float_expression">Is an expression of approximate numeric (<b>float</b>) data type with a decimal point.</param>
		/// <param name="length">Is the total length. This includes decimal point, sign, digits, and spaces. The default is 10.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "STR", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string? Str<T>(T? float_expression, int length)
			=> throw new ServerSideOnlyException(nameof(Str));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/STR-transact-sql">STR (Transact-SQL)</see></b></para>
		/// <para>Returns character data converted from numeric data. The character data is right-justified, with a specified length and decimal precision.</para>
		/// </summary>
		/// <param name="float_expression">Is an expression of approximate numeric (<b>float</b>) data type with a decimal point.</param>
		/// <param name="length">Is the total length. This includes decimal point, sign, digits, and spaces. The default is 10.</param>
		/// <param name="decimal">Is the number of places to the right of the decimal point. decimal must be less than or equal to 16.
		/// If decimal is more than 16 then the result is truncated to sixteen places to the right of the decimal point.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "STR", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string? Str<T>(T? float_expression, int length, int @decimal)
			=> throw new ServerSideOnlyException(nameof(Str));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/STRING-ESCAPE-transact-sql">STRING_ESCAPE (Transact-SQL)</see></b></para>
		/// <para>Returns character data converted from numeric data. The character data is right-justified, with a specified length and decimal precision.</para>
		/// </summary>
		/// <param name="text">Is a <b>nvarchar</b> expression expression representing the object that should be escaped.</param>
		/// <param name="type">Escaping rules that will be applied. Currently the value supported is <c>'json'</c>.</param>
		/// <returns>varchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "STRING_ESCAPE", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string? StringEscape(string? text, string? type)
			=> throw new ServerSideOnlyException(nameof(StringEscape));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/STUFF-transact-sql">STUFF (Transact-SQL)</see></b></para>
		/// <para>The STUFF function inserts a string into another string. It deletes a specified length of characters in the first string
		/// at the start position and then inserts the second string into the first string at the start position.</para>
		/// </summary>
		/// <param name="character_expression">Is an expression of character data. <c>character_expression</c> can be a constant,
		/// variable, or column of either character or binary data.</param>
		/// <param name="start">Is an integer value that specifies the location to start deletion and insertion.
		/// If <c>start</c> is negative or zero, a null string is returned. If start is longer than the first <c>character_expression</c>,
		/// a null string is returned. start can be of type <b>bigint</b>.</param>
		/// <param name="length">Is an integer that specifies the number of characters to delete. If <c>length</c> is negative, a null string is returned.
		/// If <c>length</c> is longer than the first <c>character_expression</c>, deletion occurs up to the last character in the last <c>character_expression</c>.
		/// If <c>length</c> is zero, insertion occurs at start location and no characters are deleted. length can be of type <b>bigint</b>.</param>
		/// <param name="replaceWith_expression">Is an expression of character data. <c>character_expression</c> can be a constant, variable,
		/// or column of either character or binary data. This expression replaces <c>length</c> characters of <c>character_expression</c> beginning at start.
		/// Providing <c>NULL</c> as the <c>replaceWith_expression</c>, removes characters without inserting anything.</param>
		/// <returns>Returns character data if character_expression is one of the supported character data types.
		/// Returns binary data if character_expression is one of the supported binary data types.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "STUFF", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string? Stuff(string? character_expression, int? start, int? length, string? replaceWith_expression)
			=> throw new ServerSideOnlyException(nameof(Stuff));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/SUBSTRING-transact-sql">SUBSTRING (Transact-SQL)</see></b></para>
		/// <para>Returns part of a character, binary, text, or image expression in SQL Server.</para>
		/// </summary>
		/// <param name="expression">Is a <b>character</b>, <b>binary</b>, <b>text</b>, <b>ntext</b>, or <b>image</b> expression.</param>
		/// <param name="start">Is an integer or <b>bigint</b> expression that specifies where the returned characters start.
		/// (The numbering is 1 based, meaning that the first character in the expression is 1).
		/// If <c>start</c> is less than 1, the returned expression will begin at the first character that is specified in <c>expression</c>.
		/// In this case, the number of characters that are returned is the largest value of either the sum of <c>start + length</c> - 1 or 0.
		/// If <c>start</c> is greater than the number of characters in the value expression, a zero-length expression is returned.</param>
		/// <param name="length">Is a positive integer or <b>bigint</b> expression that specifies how many characters of the <c>expression</c> will be returned.
		/// If <c>length</c> is negative, an error is generated and the statement is terminated. If the sum of start and <c>length</c> is greater
		/// than the number of characters in <c>expression</c>, the whole value expression beginning at start is returned.</param>
		/// <returns>Returns character data if expression is one of the supported character data types.
		/// Returns binary data if expression is one of the supported binary data types.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "SUBSTRING", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string? Substring(string? expression, int? start, int? length)
			=> throw new ServerSideOnlyException(nameof(Substring));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRANSLATE-transact-sql">TRANSLATE (Transact-SQL)</see></b></para>
		/// <para>Returns part of a character, binary, text, or image expression in SQL Server.</para>
		/// </summary>
		/// <param name="inputString"><c>inputString</c> Is the string expression to be searched. <c>inputString</c> can be any character data type
		/// (nvarchar, varchar, nchar, char).</param>
		/// <param name="characters ">Is a string expression containing characters that should be replaced. <c>characters</c> can be any character data type.</param>
		/// <param name="translations">Is a string expression containing the replacement characters.
		/// <c>translations</c> must be the same data type and length as characters.</param>
		/// <returns>Returns a character expression of the same data type as <c>inputString</c> where characters from
		/// the second argument are replaced with the matching characters from third argument.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "TRANSLATE", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static string? Translate(string? inputString, string? characters, string? translations)
			=> throw new ServerSideOnlyException(nameof(Translate));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRIM-transact-sql">TRIM (Transact-SQL)</see></b></para>
		/// <para>Removes the space character <c>char(32)</c> or other specified characters from the start and end of a string.</para>
		/// </summary>
		/// <param name="string">Is an expression of any character type (<c>nvarchar</c>, <c>varchar</c>, <c>nchar</c>, or <c>char</c>)
		/// where characters should be removed.</param>
		/// <returns>Returns a character expression with a type of string argument where the space character <c>char(32)</c> or
		/// other specified characters are removed from both sides. Returns <c>NULL</c> if input string is <c>NULL</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "TRIM", ServerSideOnly=true)]
		public static string? Trim(string? @string)
			=> throw new ServerSideOnlyException(nameof(Trim));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRIM-transact-sql">TRIM (Transact-SQL)</see></b></para>
		/// <para>Removes the space character <c>char(32)</c> or other specified characters from the start and end of a string.</para>
		/// </summary>
		/// <param name="characters">Is a literal, variable, or function call of any non-LOB character type
		/// (<c>nvarchar</c>, <c>varchar</c>, <c>nchar</c>, or <c>char</c>) containing characters that should be removed.
		/// <c>nvarchar(max)</c> and <c>varchar(max)</c> types aren't allowed.</param>
		/// <param name="string">Is an expression of any character type (<c>nvarchar</c>, <c>varchar</c>, <c>nchar</c>, or <c>char</c>)
		/// where characters should be removed.</param>
		/// <returns>Returns a character expression with a type of string argument where the space character <c>char(32)</c> or
		/// other specified characters are removed from both sides. Returns <c>NULL</c> if input string is <c>NULL</c>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "TRIM({0} FROM {1})", ServerSideOnly=true)]
		public static string? Trim(string characters, string? @string)
			=> throw new ServerSideOnlyException(nameof(Trim));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/UNICODE-transact-sql">UNICODE (Transact-SQL)</see></b></para>
		/// <para>Removes the space character <c>char(32)</c> or other specified characters from the start and end of a string.</para>
		/// </summary>
		/// <param name="ncharacter_expression">Is an <b>nchar</b> or <b>nvarchar</b> expression.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "UNICODE", ServerSideOnly=true)]
		public static int? Unicode(string ncharacter_expression)
			=> throw new ServerSideOnlyException(nameof(Unicode));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/UPPER-transact-sql">UPPER (Transact-SQL)</see></b></para>
		/// <para>Returns a character expression after converting uppercase character data to lowercase.</para>
		/// </summary>
		/// <param name="character_expression">Is the string <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of character or binary data. <c>character_expression</c> can be a constant, variable, or column.
		/// <c>character_expression</c> must be of a data type that is implicitly convertible to <b>varchar</b>.</param>
		/// <returns><b>varchar</b> or <b>nvarchar</b></returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "UPPER", ServerSideOnly=true)]
		public static string? Upper(string? character_expression)
			=> throw new ServerSideOnlyException(nameof(Upper));

		sealed class CollateBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var collationName = builder.GetValue<string>("collation_name");
				builder.AddFragment("collation_name", collationName);
			}
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/statements/collations">COLLATE (Transact-SQL)</see></b></para>
		/// <para>Returns a character expression after converting uppercase character data to lowercase.</para>
		/// <para>Windows_collation_name is the collation name for a
		/// <see href="https://docs.microsoft.com/en-us/sql/t-sql/statements/windows-collation-name-transact-sql">Windows Collation Name</see>.</para>
		/// <seealso href="https://docs.microsoft.com/en-us/sql/relational-databases/collations/collation-and-unicode-support">Collation and Unicode support</seealso>
		/// </summary>
		/// <param name="collation_name">Is the name of the collation to be applied to the expression, column definition, or database definition.
		/// <c>collation_name</c> can be only a specified <c>Windows_collation_name</c> or a <c>SQL_collation_name</c>.
		/// <c>collation_name</c> must be a literal value. <c>collation_name</c> cannot be represented by a variable or expression.</param>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "{string} COLLATE {collation_name}", ServerSideOnly=true, BuilderType=typeof(CollateBuilder))]
		public static string? Collate([ExprParameter] string? @string, [SqlQueryDependent] string collation_name)
			=> throw new ServerSideOnlyException(nameof(Collate));

		#endregion

		#region System Statistical

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CONNECTIONS-transact-sql">@@CONNECTIONS (Transact-SQL)</see></b></para>
		/// <para>This function returns the number of attempted connections - both successful and unsuccessful - since SQL Server was last started.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@CONNECTIONS", ServerSideOnly=true)]
		public static int Connections => throw new ServerSideOnlyException(nameof(Connections));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CPU-BUSY-transact-sql">@@CPU_BUSY (Transact-SQL)</see></b></para>
		/// <para>This function returns the amount of time that SQL Server has spent in active operation since its latest start.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@CPU_BUSY", ServerSideOnly=true)]
		public static int CpuBusy => throw new ServerSideOnlyException(nameof(CpuBusy));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/IDLE-transact-sql">@@IDLE (Transact-SQL)</see></b></para>
		/// <para>Returns the time that SQL Server has been idle since it was last started.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@IDLE", ServerSideOnly=true)]
		public static int Idle => throw new ServerSideOnlyException(nameof(Idle));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/IO_BUSY-transact-sql">@@IO_BUSY (Transact-SQL)</see></b></para>
		/// <para>Returns the time that SQL Server has spent performing input and output operations since SQL Server was last started.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@IO_BUSY", ServerSideOnly=true)]
		public static int IOBusy => throw new ServerSideOnlyException(nameof(IOBusy));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PACK-SENT-transact-sql">@@PACK_SENT (Transact-SQL)</see></b></para>
		/// <para>Returns the number of output packets written to the network by SQL Server since it was last started.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@PACK_SENT", ServerSideOnly=true)]
		public static int PackSent => throw new ServerSideOnlyException(nameof(PackSent));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PACKET-ERRORS-transact-sql">@@PACKET_ERRORS (Transact-SQL)</see></b></para>
		/// <para>Returns the number of network packet errors that have occurred on SQL Server connections since SQL Server was last started.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@PACKET_ERRORS", ServerSideOnly=true)]
		public static int PacketErrors => throw new ServerSideOnlyException(nameof(PacketErrors));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TIMETICKS-transact-sql">@@TIMETICKS (Transact-SQL)</see></b></para>
		/// <para>Returns the number of microseconds per tick.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@TIMETICKS", ServerSideOnly=true)]
		public static int TimeTicks => throw new ServerSideOnlyException(nameof(TimeTicks));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TOTAL-ERRORS-transact-sql">@@TOTAL_ERRORS (Transact-SQL)</see></b></para>
		/// <para>Returns the number of microseconds per tick.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@TOTAL_ERRORS", ServerSideOnly=true)]
		public static int TotalErrors => throw new ServerSideOnlyException(nameof(TotalErrors));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TOTAL-READ-transact-sql">@@TOTAL_READ (Transact-SQL)</see></b></para>
		/// <para>Returns the number of microseconds per tick.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@TOTAL_READ", ServerSideOnly=true)]
		public static int TotalRead => throw new ServerSideOnlyException(nameof(TotalRead));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TOTAL-WRITE-transact-sql">@@TOTAL_WRITE (Transact-SQL)</see></b></para>
		/// <para>Returns the number of microseconds per tick.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@TOTAL_WRITE", ServerSideOnly=true)]
		public static int TotalWrite => throw new ServerSideOnlyException(nameof(TotalWrite));

		#endregion

		#region System

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/IDENTITY-transact-sql">@@IDENTITY (Transact-SQL)</see></b></para>
		/// <para>Is a system function that returns the last-inserted identity value.</para>
		/// </summary>
		/// <returns>numeric(38,0)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@IDENTITY", ServerSideOnly=true)]
		public static decimal? Identity => throw new ServerSideOnlyException(nameof(Identity));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/PACK-RECEIVED-transact-sql">@@PACK_RECEIVED (Transact-SQL)</see></b></para>
		/// <para>Returns the number of input packets read from the network by SQL Server since it was last started.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@PACK_RECEIVED", ServerSideOnly=true)]
		public static int PackReceived => throw new ServerSideOnlyException(nameof(PackReceived));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/TRANCOUNT-transact-sql">@@TRANCOUNT (Transact-SQL)</see></b></para>
		/// <para>Returns the number of input packets read from the network by SQL Server since it was last started.</para>
		/// </summary>
		/// <returns>integer</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@TRANCOUNT", ServerSideOnly=true)]
		public static int TransactionCount => throw new ServerSideOnlyException(nameof(TransactionCount));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/BINARY-CHECKSUM-transact-sql">BINARY_CHECKSUM (Transact-SQL)</see></b></para>
		/// <para>Returns the binary checksum value computed over a row of a table or over a list of expressions.</para>
		/// </summary>
		/// <para>
		/// Specifies that the computation covers all the table columns. BINARY_CHECKSUM ignores columns of noncomparable data types in its computation.
		/// Noncomparable data types include
		/// <list type="bullet">
		/// <item>cursor</item>
		/// <item>image</item>
		/// <item>ntext</item>
		/// <item>text</item>
		/// <item>xml</item>
		/// </list>
		/// and noncomparable common language runtime (CLR) user-defined types.
		/// </para>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "BINARY_CHECKSUM(*)", ServerSideOnly=true)]
		public static int BinaryCheckSum()
			=> throw new ServerSideOnlyException(nameof(BinaryCheckSum));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/BINARY-CHECKSUM-transact-sql">BINARY_CHECKSUM (Transact-SQL)</see></b></para>
		/// <para>Returns the binary checksum value computed over a row of a table or over a list of expressions.</para>
		/// </summary>
		/// <param name="expressions">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see> of any type.
		/// BINARY_CHECKSUM ignores expressions of noncomparable data types in its computation.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "BINARY_CHECKSUM", ServerSideOnly=true)]
		public static int BinaryCheckSum(params object[] expressions)
			=> throw new ServerSideOnlyException(nameof(BinaryCheckSum));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHECKSUM-transact-sql">CHECKSUM (Transact-SQL)</see></b></para>
		/// <para>The <c>CHECKSUM</c> function returns the checksum value computed over a table row, or over an expression list.
		/// Use <c>CHECKSUM</c> to build hash indexes.</para>
		/// </summary>
		/// <para>
		/// This argument specifies that the checksum computation covers all table columns.
		/// <c>CHECKSUM</c> returns an error if any column has a noncomparable data type. Noncomparable data types include:
		/// <list type="bullet">
		/// <item>cursor</item>
		/// <item>image</item>
		/// <item>ntext</item>
		/// <item>text</item>
		/// <item>XML</item>
		/// </list>
		/// Another noncomparable data type is <b>sql_variant</b> with any one of the preceding data types as its base type.
		/// </para>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "CHECKSUM(*)", ServerSideOnly=true)]
		public static int CheckSum()
			=> throw new ServerSideOnlyException(nameof(CheckSum));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CHECKSUM-transact-sql">CHECKSUM (Transact-SQL)</see></b></para>
		/// <para>The <c>CHECKSUM</c> function returns the checksum value computed over a table row, or over an expression list.
		/// Use <c>CHECKSUM</c> to build hash indexes.</para>
		/// </summary>
		/// <param name="expressions">An <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// of any type, except a noncomparable data type.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CHECKSUM", ServerSideOnly=true)]
		public static int CheckSum(params object[] expressions)
			=> throw new ServerSideOnlyException(nameof(CheckSum));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/COMPRESS-transact-sql">COMPRESS (Transact-SQL)</see></b></para>
		/// <para>This function compresses the input expression, using the GZIP algorithm. The function returns a byte array of type <b>varbinary(max)</b>.</para>
		/// </summary>
		/// <param name="expression">A
		/// <list type="bullet">
		/// <item>binary(n)</item>
		/// <item>char(n)</item>
		/// <item>nchar(n)</item>
		/// <item>nvarchar(max)</item>
		/// <item>nvarchar(n)</item>
		/// <item>varbinary(max)</item>
		/// <item>varbinary(n)</item>
		/// <item>varchar(max)</item>
		/// </list>
		/// or
		/// <list type="bullet">
		/// <item>varchar(n)</item>
		/// </list>
		/// expression.</param>
		/// <returns><b>varbinary(max)</b> representing the compressed content of the input.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "COMPRESS", ServerSideOnly=true)]
		public static byte[] Compress(string? expression)
			=> throw new ServerSideOnlyException(nameof(Compress));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/COMPRESS-transact-sql">COMPRESS (Transact-SQL)</see></b></para>
		/// <para>This function compresses the input expression, using the GZIP algorithm. The function returns a byte array of type <b>varbinary(max)</b>.</para>
		/// </summary>
		/// <param name="expression">A
		/// <list type="bullet">
		/// <item>binary(n)</item>
		/// <item>char(n)</item>
		/// <item>nchar(n)</item>
		/// <item>nvarchar(max)</item>
		/// <item>nvarchar(n)</item>
		/// <item>varbinary(max)</item>
		/// <item>varbinary(n)</item>
		/// <item>varchar(max)</item>
		/// </list>
		/// or
		/// <list type="bullet">
		/// <item>varchar(n)</item>
		/// </list>
		/// expression.</param>
		/// <returns><b>varbinary(max)</b> representing the compressed content of the input.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "COMPRESS", ServerSideOnly=true)]
		public static byte[] Compress(byte[]? expression)
			=> throw new ServerSideOnlyException(nameof(Compress));

		public enum ConnectionPropertyName
		{
			Net_Transport,
			Protocol_Type,
			Auth_Scheme,
			Local_Net_Address,
			Local_TCP_Port,
			Client_Net_Address,
			Physical_Net_Transport,
		}

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CONNECTIONPROPERTY-transact-sql">CONNECTIONPROPERTY (Transact-SQL)</see></b></para>
		/// <para>For a request that comes in to the server, this function returns information about the
		/// connection properties of the unique connection which supports that request.</para>
		/// </summary>
		/// <param name="property">The property of the connection.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Extension(ProviderName.SqlServer, "CONNECTIONPROPERTY", ServerSideOnly=true, BuilderType=typeof(PropertyBuilder<ConnectionPropertyName>))]
		public static object? ConnectionProperty([SqlQueryDependent] ConnectionPropertyName property)
			=> throw new ServerSideOnlyException(nameof(ConnectionProperty));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CURRENT-REQUEST-ID-transact-sql">CURRENT_REQUEST_ID (Transact-SQL)</see></b></para>
		/// <para>This function returns the ID of the current request within the current session.</para>
		/// </summary>
		/// <returns>smallint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CURRENT_REQUEST_ID", ServerSideOnly=true)]
		public static short CurrentRequestID()
			=> throw new ServerSideOnlyException(nameof(CurrentRequestID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/CURRENT-TRANSACTION-ID-transact-sql">CURRENT_TRANSACTION_ID (Transact-SQL)</see></b></para>
		/// <para>This function returns the transaction ID of the current transaction in the current session.</para>
		/// </summary>
		/// <returns>bigint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "CURRENT_TRANSACTION_ID", ServerSideOnly=true)]
		public static long CurrentTransactionID()
			=> throw new ServerSideOnlyException(nameof(CurrentTransactionID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/DECOMPRESS-transact-sql">DECOMPRESS (Transact-SQL)</see></b></para>
		/// <para>This function will decompress an input expression value, using the GZIP algorithm.
		/// <c>DECOMPRESS</c> will return a byte array (VARBINARY(MAX) type).</para>
		/// </summary>
		/// <param name="expression">A <b>varbinary(n)</b>, <b>varbinary(max)</b>, or <b>binary(n)</b> value.</param>
		/// <returns><b>varbinary(max)</b> representing the compressed content of the input.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "DECOMPRESS", ServerSideOnly=true)]
		public static byte[] Decompress(byte[] expression)
			=> throw new ServerSideOnlyException(nameof(Decompress));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FORMATMESSAGE-transact-sql">FORMATMESSAGE (Transact-SQL)</see></b></para>
		/// <para>Constructs a message from an existing message in sys.messages or from a provided string.
		/// The functionality of FORMATMESSAGE resembles that of the RAISERROR statement.
		/// However, RAISERROR prints the message immediately, while FORMATMESSAGE returns the formatted message for further processing.</para>
		/// </summary>
		/// <param name="msg_number">Is the ID of the message stored in sys.messages. If msg_number is &lt;= 13000, or if the message does not exist in sys.messages, NULL is returned.</param>
		/// <param name="param_values">Is a parameter value for use in the message. Can be more than one parameter value. The values must be specified in the order in which the placeholder variables appear in the message. The maximum number of values is 20.</param>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FORMATMESSAGE", ServerSideOnly=true)]
		public static string? FormatMessage(int msg_number, params object?[] param_values)
			=> throw new ServerSideOnlyException(nameof(FormatMessage));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/FORMATMESSAGE-transact-sql">FORMATMESSAGE (Transact-SQL)</see></b></para>
		/// <para>Constructs a message from an existing message in sys.messages or from a provided string.
		/// The functionality of FORMATMESSAGE resembles that of the RAISERROR statement.
		/// However, RAISERROR prints the message immediately, while FORMATMESSAGE returns the formatted message for further processing.</para>
		/// </summary>
		/// <param name="msg_string">Is a string enclosed in single quotes and containing parameter value placeholders.
		/// The error message can have a maximum of 2,047 characters. If the message contains 2,048 or more characters,
		/// only the first 2,044 are displayed and an ellipsis is added to indicate that the message has been truncated.
		/// Note that substitution parameters consume more characters than the output shows because of internal storage behavior.</param>
		/// <param name="param_values">Is a parameter value for use in the message. Can be more than one parameter value.
		/// The values must be specified in the order in which the placeholder variables appear in the message. The maximum number of values is 20.</param>
		/// <returns>nvarchar</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "FORMATMESSAGE", ServerSideOnly=true)]
		public static string? FormatMessage(string msg_string, params object?[] param_values)
			=> throw new ServerSideOnlyException(nameof(FormatMessage));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/GETANSINULL-transact-sql">GETANSINULL (Transact-SQL)</see></b></para>
		/// <para>Returns the default nullability for the database for this session.</para>
		/// </summary>
		/// <param name="database">Is the name of the database for which to return nullability information.
		/// <c>database</c> is either <b>char</b> or <b>nchar</b>. If <b>char</b>, <c>database</c> is implicitly converted to <b>nchar</b>.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "GETANSINULL", ServerSideOnly=true)]
		public static int? GetAnsiNull(string database)
			=> throw new ServerSideOnlyException(nameof(GetAnsiNull));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/GETANSINULL-transact-sql">GETANSINULL (Transact-SQL)</see></b></para>
		/// <para>Returns the default nullability for the database for this session.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "GETANSINULL", ServerSideOnly=true)]
		public static int? GetAnsiNull()
			=> throw new ServerSideOnlyException(nameof(GetAnsiNull));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/HOST-ID-transact-sql">HOST_ID (Transact-SQL)</see></b></para>
		/// <para>Returns the workstation identification number. The workstation identification number is the process ID (PID)
		/// of the application on the client computer that is connecting to SQL Server.</para>
		/// </summary>
		/// <returns>char(10)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "HOST_ID", ServerSideOnly=true)]
		public static string HostID()
			=> throw new ServerSideOnlyException(nameof(HostID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/HOST-NAME-transact-sql">HOST_NAME (Transact-SQL)</see></b></para>
		/// <para>Returns the workstation identification number. The workstation identification number is the process ID (PID)
		/// of the application on the client computer that is connecting to SQL Server.</para>
		/// </summary>
		/// <returns>nvarchar(128)</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "HOST_NAME", ServerSideOnly=true)]
		public static string HostName()
			=> throw new ServerSideOnlyException(nameof(HostName));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ISNULL-transact-sql">ISNULL (Transact-SQL)</see></b></para>
		/// <para>Replaces NULL with the specified replacement value.</para>
		/// </summary>
		/// <param name="check_expression">Is the <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// to be checked for NULL. <c>check_expression</c> can be of any type.</param>
		/// <param name="replacement_value">Is the expression to be returned if <c>check_expression</c> is NULL.
		/// <c>replacement_value</c> must be of a type that is implicitly convertible to the type of <c>check_expression</c>.</param>
		/// <returns>Returns the same type as <c>check_expression</c>. If a literal NULL is provided as <c>check_expression</c>,
		/// returns the datatype of the <c>replacement_value</c>. If a literal NULL is provided as <c>check_expression</c> and no
		/// <c>replacement_value</c> is provided, returns an <b>int</b>.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ISNULL", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static T IsNull<T>(T check_expression, T replacement_value)
			=> throw new ServerSideOnlyException(nameof(IsNull));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ISNUMERIC-transact-sql">ISNUMERIC (Transact-SQL)</see></b></para>
		/// <para>Determines whether an expression is a valid numeric type.</para>
		/// </summary>
		/// <param name="expression">Is the <see href="https://docs.microsoft.com/en-us/sql/t-sql/language-elements/expressions-transact-sql">expression</see>
		/// to be evaluated.</param>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ISNUMERIC", ServerSideOnly=true, IgnoreGenericParameters=true)]
		public static int IsNumeric<T>(T expression)
			=> throw new ServerSideOnlyException(nameof(IsNumeric));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/MIN-ACTIVE-ROWVERSION-transact-sql">MIN_ACTIVE_ROWVERSION (Transact-SQL)</see></b></para>
		/// <para>Returns the workstation identification number. The workstation identification number is the process ID (PID)
		/// of the application on the client computer that is connecting to SQL Server.</para>
		/// </summary>
		/// <returns>Returns a <b>binary(8)</b> value.</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "MIN_ACTIVE_ROWVERSION", ServerSideOnly=true)]
		public static byte[] MinActiveRowVersion()
			=> throw new ServerSideOnlyException(nameof(MinActiveRowVersion));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/NEWID-transact-sql">NEWID (Transact-SQL)</see></b></para>
		/// <para>Creates a unique value of type <b>uniqueidentifier</b>.</para>
		/// </summary>
		/// <returns>uniqueidentifier</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "NEWID", ServerSideOnly=true)]
		public static Guid NewID()
			=> throw new ServerSideOnlyException(nameof(NewID));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ROWCOUNT-transact-sql">@@ROWCOUNT (Transact-SQL)</see></b></para>
		/// <para>Returns the number of rows affected by the last statement. If the number of rows is more than 2 billion,
		/// use <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ROWCOUNT-BIG-transact-sql">ROWCOUNT_BIG</see>.</para>
		/// </summary>
		/// <returns>int</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Expression(ProviderName.SqlServer, "@@ROWCOUNT", ServerSideOnly=true)]
		public static int RowCount => throw new ServerSideOnlyException(nameof(RowCount));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ROWCOUNT-BIG-transact-sql">ROWCOUNT_BIG (Transact-SQL)</see></b></para>
		/// <para>Returns the number of rows affected by the last statement executed. This function operates like
		/// <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ROWCOUNT-transact-sql">@@ROWCOUNT</see>,
		/// except the return type of ROWCOUNT_BIG is bigint.</para>
		/// </summary>
		/// <returns>bigint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "ROWCOUNT_BIG", ServerSideOnly=true)]
		public static long RowCountBig()
			=> throw new ServerSideOnlyException(nameof(RowCountBig));

		/// <summary>
		/// <para><b><see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/ROWCOUNT-BIG-transact-sql">XACT_STATE (Transact-SQL)</see></b></para>
		/// <para>Is a scalar function that reports the user transaction state of a current running request.
		/// XACT_STATE indicates whether the request has an active user transaction, and whether the transaction is capable of being committed.</para>
		/// </summary>
		/// <returns>smallint</returns>
		/// <exception cref="ServerSideOnlyException" />
		[Sql.Function(ProviderName.SqlServer, "XACT_STATE", ServerSideOnly=true)]
		public static short XactState()
			=> throw new ServerSideOnlyException(nameof(XactState));

		#endregion

		#region Vector Support

		/// <summary>
		/// A string with the name of the distance metric to use to calculate the distance between the two given vectors.
		/// </summary>
		[Sql.Enum]
		public enum DistanceMetric
		{
			/// <summary>
			/// Cosine (angular) distance.
			/// <list type="table">
			/// <listheader>
			/// [0, 2]
			/// </listheader>
			/// <item>
			/// <term>0</term>
			/// <description>identical vectors</description>
			/// </item>
			/// <item>
			/// <term>2</term>
			/// <description>opposing vectors</description>
			/// </item>
			/// </list>
			/// </summary>
			Cosine,
			/// <summary>
			/// Euclidean distance.
			/// <list type="table">
			/// <listheader>
			/// [0, +∞]
			/// </listheader>
			/// <item>
			/// <term>0</term>
			/// <description>identical vectors</description>
			/// </item>
			/// </list>
			/// </summary>
			Euclidean,
			/// <summary>
			/// Dot product-based indication of distance, obtained by calculating the negative dot product.
			/// <list type="table">
			/// <listheader>
			/// [-∞, +∞]
			/// </listheader>
			/// <item>
			/// <description>Smaller numbers indicate more similar vectors</description>
			/// </item>
			/// </list>
			/// </summary>
			Dot,
		}

		sealed class DistanceMetricBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var distanceMetric = builder.GetValue<DistanceMetric>("distanceMetric");
				builder.AddFragment("distanceMetric", distanceMetric switch
				{
					DistanceMetric.Cosine    => "'cosine'",
					DistanceMetric.Euclidean => "'euclidean'",
					DistanceMetric.Dot       => "'dot'",
					_                        => throw new NotSupportedException($"Distance metric '{distanceMetric}' is not supported."),
				});
			}
		}

		/// <summary>
		/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using a specified distance metric.
		/// </summary>
		/// <param name="distanceMetric">
		/// A string with the name of the distance metric to use to calculate the distance between the two given vectors. The following distance metrics are supported:
		/// <list type="table">
		/// <item>
		/// <term>cosine</term>
		/// <description>Cosine distance</description>
		/// </item>
		/// <item>
		/// <term>euclidean</term>
		/// <description>Euclidean distance</description>
		/// </item>
		/// <item>
		/// <term>dot</term>
		/// <description>(Negative) Dot product</description>
		/// </item>
		/// </list>
		/// </param>
		/// <param name="vector1">An expression that evaluates to <b>vector</b> data type.</param>
		/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
		/// <returns>
		/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
		/// </returns>
		/// <exception cref="NotImplementedException"></exception>
		[Sql.Extension(ProviderName.SqlServer, "VECTOR_DISTANCE({distanceMetric}, {vector1}, {vector2})", ServerSideOnly=true, BuilderType = typeof(DistanceMetricBuilder))]
		public static float VectorDistance([SqlQueryDependent] DistanceMetric distanceMetric, [ExprParameter] float[] vector1, [ExprParameter] float[] vector2)
		{
			throw new NotImplementedException();
		}

		/// <param name="vector1">An expression that evaluates to <b>vector</b> data type.</param>
		extension([ExprParameter] float[] vector1)
		{
			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using a specified distance metric.
			/// </summary>
			/// <param name="distanceMetric">
			/// A string with the name of the distance metric to use to calculate the distance between the two given vectors. The following distance metrics are supported:
			/// <list type="table">
			/// <item>
			/// <term>cosine</term>
			/// <description>Cosine distance</description>
			/// </item>
			/// <item>
			/// <term>euclidean</term>
			/// <description>Euclidean distance</description>
			/// </item>
			/// <item>
			/// <term>dot</term>
			/// <description>(Negative) Dot product</description>
			/// </item>
			/// </list>
			/// </param>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTOR_DISTANCE({distanceMetric}, {vector1}, {vector2})", ServerSideOnly = true, BuilderType = typeof(DistanceMetricBuilder))]
			public float VectorDistance([SqlQueryDependent] DistanceMetric distanceMetric, [ExprParameter] float[] vector2)
			{
				return VectorDistance(distanceMetric, vector1, vector2);
			}

			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using the <b>cosine</b> distance metric.
			/// </summary>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_DISTANCE('cosine', {0}, {1})", ServerSideOnly=true)]
			public float CosineVectorDistance(float[] vector2)
			{
				return VectorDistance(DistanceMetric.Cosine, vector1, vector2);
			}

			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using the <b>Euclidean</b> distance metric.
			/// </summary>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_DISTANCE('euclidean', {0}, {1})", ServerSideOnly=true)]
			public float EuclideanVectorDistance(float[] vector2)
			{
				return VectorDistance(DistanceMetric.Euclidean, vector1, vector2);
			}

			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using the <b>dot product-based indication of</b> distance metric.
			/// </summary>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_DISTANCE('dot', {0}, {1})", ServerSideOnly=true)]
			public float DotVectorDistance(float[] vector2)
			{
				return VectorDistance(DistanceMetric.Dot, vector1, vector2);
			}
		}

		/// <summary>
		/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using a specified distance metric.
		/// </summary>
		/// <param name="distanceMetric">
		/// A string with the name of the distance metric to use to calculate the distance between the two given vectors. The following distance metrics are supported:
		/// <list type="table">
		/// <item>
		/// <term>cosine</term>
		/// <description>Cosine distance</description>
		/// </item>
		/// <item>
		/// <term>euclidean</term>
		/// <description>Euclidean distance</description>
		/// </item>
		/// <item>
		/// <term>dot</term>
		/// <description>(Negative) Dot product</description>
		/// </item>
		/// </list>
		/// </param>
		/// <param name="vector1">An expression that evaluates to <b>vector</b> data type.</param>
		/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
		/// <returns>
		/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
		/// </returns>
		/// <exception cref="NotImplementedException"></exception>
		[Sql.Extension(ProviderName.SqlServer, "VECTOR_DISTANCE({distanceMetric}, {vector1}, {vector2})", ServerSideOnly = true, BuilderType = typeof(DistanceMetricBuilder))]
		public static float VectorDistance<T>([SqlQueryDependent] DistanceMetric distanceMetric, [ExprParameter] T vector1, [ExprParameter] T vector2)
			where T : unmanaged
		{
			throw new NotImplementedException();
		}

		/// <param name="vector1">An expression that evaluates to <b>vector</b> data type.</param>
		extension<T>([ExprParameter] T vector1) where T : unmanaged
		{
			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using a specified distance metric.
			/// </summary>
			/// <param name="distanceMetric">
			/// A string with the name of the distance metric to use to calculate the distance between the two given vectors. The following distance metrics are supported:
			/// <list type="table">
			/// <item>
			/// <term>cosine</term>
			/// <description>Cosine distance</description>
			/// </item>
			/// <item>
			/// <term>euclidean</term>
			/// <description>Euclidean distance</description>
			/// </item>
			/// <item>
			/// <term>dot</term>
			/// <description>(Negative) Dot product</description>
			/// </item>
			/// </list>
			/// </param>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTOR_DISTANCE({distanceMetric}, {vector1}, {vector2})", ServerSideOnly = true, BuilderType = typeof(DistanceMetricBuilder))]
			public float VectorDistance([SqlQueryDependent] DistanceMetric distanceMetric, [ExprParameter] T vector2)
			{
				return VectorDistance(distanceMetric, vector1, vector2);
			}

			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using the <b>cosine</b> distance metric.
			/// </summary>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_DISTANCE('cosine', {0}, {1})", ServerSideOnly=true)]
			public float CosineVectorDistance(T vector2)
			{
				return VectorDistance(DistanceMetric.Cosine, vector1, vector2);
			}

			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using the <b>Euclidean</b> distance metric.
			/// </summary>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_DISTANCE('euclidean', {0}, {1})", ServerSideOnly = true)]
			public float EuclideanVectorDistance(T vector2)
			{
				return VectorDistance(DistanceMetric.Euclidean, vector1, vector2);
			}

			/// <summary>
			/// The <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-distance-transact-sql">VECTOR_DISTANCE</a> function calculates the distance between two vectors using the <b>dot product-based indication of</b> distance metric.
			/// </summary>
			/// <param name="vector2">An expression that evaluates to <b>vector</b> data type.</param>
			/// <returns>
			/// The function returns a scalar <b>float</b> value that represents the distance between the two vectors using the specified distance metric.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_DISTANCE('dot', {0}, {1})", ServerSideOnly = true)]
			public float DotVectorDistance(T vector2)
			{
				return VectorDistance(DistanceMetric.Dot, vector1, vector2);
			}
		}

		/// <summary>
		/// A string with the name of the norm type to use to calculate the norm of the given vector.
		/// </summary>
		[Sql.Enum]
		public enum NormType
		{
			/// <summary>
			///  The 1-norm, which is the sum of the absolute values of the vector components.
			/// </summary>
			Norm1,
			/// <summary>
			/// The 2-norm, also known as the Euclidean Norm, which is the square root of the sum of the squares of the vector components.
			/// </summary>
			Norm2,
			/// <summary>
			/// The infinity norm, which is the maximum of the absolute values of the vector components.
			/// </summary>
			NormInf
		}

		sealed class NormTypeBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var normType = builder.GetValue<NormType>("normType");
				builder.AddFragment("normType", normType switch
				{
					NormType.Norm1   => "'norm1'",
					NormType.Norm2   => "'norm2'",
					NormType.NormInf => "'norminf'",
					_                        => throw new NotSupportedException($"Norm type '{normType}' is not supported."),
				});
			}
		}

		/// <summary>
		/// An expression specifying the name of the database property to return.
		/// </summary>
		[Sql.Enum]
		public enum VectorPropertyType
		{
			/// <summary>
			/// <b>Integer</b> value with dimension count.
			/// </summary>
			Dimensions,
			/// <summary>
			/// <b>sysname</b> with the name of the data type.
			/// </summary>
			BaseType
		}

		sealed class VectorPropertyBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var property = builder.GetValue<VectorPropertyType>("property");
				builder.AddFragment("property", property switch
				{
					VectorPropertyType.Dimensions => "'Dimensions'",
					VectorPropertyType.BaseType   => "'BaseType'",
					_                             => throw new NotSupportedException($"Vector property type '{property}' is not supported."),
				});
			}
		}

		/// <param name="vector">An expression that evaluates to vector data type.</param>
		extension([ExprParameter] float[] vector)
		{
			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in a given norm type.
			/// </summary>
			/// <param name="normType">
			/// A string with the name of the norm type to use to calculate the norm of the given vector. The following norm types are supported:
			/// <list type="table">
			/// <item>
			/// <term>norm1</term>
			/// <description>The 1-norm, which is the sum of the absolute values of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norm2</term>
			/// <description>The 2-norm, also known as the Euclidean Norm, which is the square root of the sum of the squares of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norminf</term>
			/// <description>The infinity norm, which is the maximum of the absolute values of the vector components.</description>
			/// </item>
			/// </list>
			/// </param>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTOR_NORM({vector}, {normType})", ServerSideOnly = true, BuilderType = typeof(NormTypeBuilder))]
			public float VectorNorm([SqlQueryDependent] NormType normType)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in the <b>norm1</b> type.
			/// </summary>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORM({0}, 'norm1')", ServerSideOnly = true)]
			public float VectorNorm1()
			{
				return vector.VectorNorm(NormType.Norm1);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in the <b>norm2</b> type.
			/// </summary>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORM({0}, 'norm2')", ServerSideOnly = true)]
			public float VectorNorm2()
			{
				return vector.VectorNorm(NormType.Norm2);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in the <b>norminf</b> type.
			/// </summary>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORM({0}, 'norminf')", ServerSideOnly = true)]
			public float VectorNormInf()
			{
				return vector.VectorNorm(NormType.NormInf);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in a given norm type.
			/// </summary>
			/// <param name="normType">
			/// A string with the name of the norm type to use to calculate the norm of the given vector. The following norm types are supported:
			/// <list type="table">
			/// <item>
			/// <term>norm1</term>
			/// <description>The 1-norm, which is the sum of the absolute values of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norm2</term>
			/// <description>The 2-norm, also known as the Euclidean Norm, which is the square root of the sum of the squares of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norminf</term>
			/// <description>The infinity norm, which is the maximum of the absolute values of the vector components.</description>
			/// </item>
			/// </list>
			/// </param>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTOR_NORMALIZE({vector}, {normType})", ServerSideOnly = true, BuilderType = typeof(NormTypeBuilder))]
			public float[] VectorNormalize([SqlQueryDependent] NormType normType)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in the <b>norm1</b> type.
			/// </summary>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORMALIZE({0}, 'norm1')", ServerSideOnly = true)]
			public float[] VectorNormalize1()
			{
				return vector.VectorNormalize(NormType.Norm1);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in the <b>norm2</b> type.
			/// </summary>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORMALIZE({0}, 'norm2')", ServerSideOnly = true)]
			public float[] VectorNormalize2()
			{
				return vector.VectorNormalize(NormType.Norm2);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in the <b>norminf</b> type.
			/// </summary>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORMALIZE({0}, 'norminf')", ServerSideOnly = true)]
			public float[] VectorNormalizeInf()
			{
				return vector.VectorNormalize(NormType.NormInf);
			}

			/// <summary>
			/// The <seealso href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vectorproperty-transact-sql">VECTORPROPERTY</seealso> function returns specific properties of a given vector.
			/// The function requires two arguments: the vector itself and the property to be retrieved.
			/// </summary>
			/// <param name="property">
			/// An expression specifying the name of the database property to return.
			/// <list type="table">
			/// <item>
			/// <term>Dimensions</term>
			/// <description>Return vector's dimensions count.</description>
			/// </item>
			/// <item>
			/// <term>BaseType</term>
			/// <description>Return vector's base type.</description>
			/// </item>
			/// </list>
			/// </param>
			/// <returns>
			/// The function returns the specific properties of a given vector based on the property selected. For example:
			/// <list type="">
			/// <item>If the property is <c>Dimensions</c>, the function returns an <b>integer</b> value representing the dimension count of the vector.</item>
			/// <item>If the property is <c>BaseType</c>, the function returns the name of the data type(<b>sysname</b>).</item>
			/// </list>
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTORPROPERTY({vector}, {property})", ServerSideOnly = true, BuilderType = typeof(VectorPropertyBuilder))]
			public string VectorProperty([SqlQueryDependent] VectorPropertyType property)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// The <seealso href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vectorproperty-transact-sql">VECTORPROPERTY</seealso> function returns Dimensions of a given vector.
			/// The function requires two arguments: the vector itself and the property to be retrieved.
			/// </summary>
			/// <returns><b>Integer</b> value with dimension count.</returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTORPROPERTY({0}, 'Dimensions')", ServerSideOnly = true)]
			public int VectorDimensionsProperty()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// The <seealso href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vectorproperty-transact-sql">VECTORPROPERTY</seealso> function returns BaseType of a given vector.
			/// The function requires two arguments: the vector itself and the property to be retrieved.
			/// </summary>
			/// <returns><b>sysname</b> with the name of the data type.</returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTORPROPERTY({0}, 'BaseType')", ServerSideOnly = true)]
			public string VectorBaseTypeProperty()
			{
				throw new NotImplementedException();
			}
		}

		/// <param name="vector">An expression that evaluates to vector data type.</param>
		extension<T>([ExprParameter] T vector) where T : unmanaged
		{
			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in a given norm type.
			/// </summary>
			/// <param name="normType">
			/// A string with the name of the norm type to use to calculate the norm of the given vector. The following norm types are supported:
			/// <list type="table">
			/// <item>
			/// <term>norm1</term>
			/// <description>The 1-norm, which is the sum of the absolute values of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norm2</term>
			/// <description>The 2-norm, also known as the Euclidean Norm, which is the square root of the sum of the squares of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norminf</term>
			/// <description>The infinity norm, which is the maximum of the absolute values of the vector components.</description>
			/// </item>
			/// </list>
			/// </param>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTOR_NORM({vector}, {normType})", ServerSideOnly = true, BuilderType = typeof(NormTypeBuilder))]
			public float VectorNorm([SqlQueryDependent] NormType normType)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in the <b>norm1</b> type.
			/// </summary>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORM({0}, 'norm1')", ServerSideOnly = true)]
			public float VectorNorm1()
			{
				return vector.VectorNorm(NormType.Norm1);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in the <b>norm2</b> type.
			/// </summary>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORM({0}, 'norm2')", ServerSideOnly = true)]
			public float VectorNorm2()
			{
				return vector.VectorNorm(NormType.Norm2);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-norm-transact-sql">VECTOR_NORM</a> to take a vector as an input and return the norm of the vector
			/// (which is a measure of its length or magnitude) in the <b>norminf</b> type.
			/// </summary>
			/// <returns>
			/// The function returns a <b>float</b> value that represents the norm of the vector using the specified norm type.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORM({0}, 'norminf')", ServerSideOnly = true)]
			public float VectorNormInf()
			{
				return vector.VectorNorm(NormType.NormInf);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in a given norm type.
			/// </summary>
			/// <param name="normType">
			/// A string with the name of the norm type to use to calculate the norm of the given vector. The following norm types are supported:
			/// <list type="table">
			/// <item>
			/// <term>norm1</term>
			/// <description>The 1-norm, which is the sum of the absolute values of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norm2</term>
			/// <description>The 2-norm, also known as the Euclidean Norm, which is the square root of the sum of the squares of the vector components.</description>
			/// </item>
			/// <item>
			/// <term>norminf</term>
			/// <description>The infinity norm, which is the maximum of the absolute values of the vector components.</description>
			/// </item>
			/// </list>
			/// </param>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTOR_NORMALIZE({vector}, {normType})", ServerSideOnly = true, BuilderType = typeof(NormTypeBuilder))]
			public T VectorNormalize([SqlQueryDependent] NormType normType)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in the <b>norm1</b> type.
			/// </summary>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORMALIZE({0}, 'norm1')", ServerSideOnly = true)]
			public T VectorNormalize1()
			{
				return vector.VectorNormalize(NormType.Norm1);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in the <b>norm2</b> type.
			/// </summary>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORMALIZE({0}, 'norm2')", ServerSideOnly = true)]
			public T VectorNormalize2()
			{
				return vector.VectorNormalize(NormType.Norm2);
			}

			/// <summary>
			/// Use <a href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vector-normalize-transact-sql">VECTOR_NORMALIZE</a> to take a vector as an input and return the normalized vector
			/// scaled to have a length of 1 in the <b>norminf</b> type.
			/// </summary>
			/// <returns>
			/// The result is a vector with the same direction as the input vector but with a length of 1 according to the given norm.
			/// If the input is <c>NULL</c>, the returned result is also <c>NULL</c>.
			/// An error is returned if <c>norm_type</c> isn't a valid norm type and if the vector isn't of the <a href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type">vector data type</a>.
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTOR_NORMALIZE({0}, 'norminf')", ServerSideOnly = true)]
			public T VectorNormalizeInf()
			{
				return vector.VectorNormalize(NormType.NormInf);
			}

			/// <summary>
			/// The <seealso href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vectorproperty-transact-sql">VECTORPROPERTY</seealso> function returns specific properties of a given vector.
			/// The function requires two arguments: the vector itself and the property to be retrieved.
			/// </summary>
			/// <param name="property">
			/// An expression specifying the name of the database property to return.
			/// <list type="table">
			/// <item>
			/// <term>Dimensions</term>
			/// <description>Return vector's dimensions count.</description>
			/// </item>
			/// <item>
			/// <term>BaseType</term>
			/// <description>Return vector's base type.</description>
			/// </item>
			/// </list>
			/// </param>
			/// <returns>
			/// The function returns the specific properties of a given vector based on the property selected. For example:
			/// <list type="">
			/// <item>If the property is <c>Dimensions</c>, the function returns an <b>integer</b> value representing the dimension count of the vector.</item>
			/// <item>If the property is <c>BaseType</c>, the function returns the name of the data type(<b>sysname</b>).</item>
			/// </list>
			/// </returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Extension(ProviderName.SqlServer, "VECTORPROPERTY({vector}, {property})", ServerSideOnly = true, BuilderType = typeof(VectorPropertyBuilder))]
			public string VectorProperty([SqlQueryDependent] VectorPropertyType property)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// The <seealso href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vectorproperty-transact-sql">VECTORPROPERTY</seealso> function returns Dimensions of a given vector.
			/// The function requires two arguments: the vector itself and the property to be retrieved.
			/// </summary>
			/// <returns><b>Integer</b> value with dimension count.</returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTORPROPERTY({0}, 'Dimensions')", ServerSideOnly = true)]
			public int VectorDimensionsProperty()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// The <seealso href="https://learn.microsoft.com/en-us/sql/t-sql/functions/vectorproperty-transact-sql">VECTORPROPERTY</seealso> function returns BaseType of a given vector.
			/// The function requires two arguments: the vector itself and the property to be retrieved.
			/// </summary>
			/// <returns><b>sysname</b> with the name of the data type.</returns>
			/// <exception cref="NotImplementedException"></exception>
			[Sql.Expression(ProviderName.SqlServer, "VECTORPROPERTY({0}, 'BaseType')", ServerSideOnly = true)]
			public string VectorBaseTypeProperty()
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		sealed class PropertyBuilder<T> : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var method = (MethodInfo)builder.Member;
				var props  = method.GetParameters();
				var ps     = new ISqlExpression[props.Length];

				for (var i = 0; i < props.Length; i++)
				{
					var prop = props[i];

					ps[i] = prop.ParameterType == typeof(T)
						? new SqlExpression(builder.Mapping.GetDbDataType(prop.ParameterType), '\'' + builder.GetValue<T>(prop.Name!)?.ToString() + '\'', Precedence.Primary)
						: builder.GetExpression(prop.Name!)!;
				}

				builder.ResultExpression = new SqlFunction(builder.Mapping.GetDbDataType(method.ReturnType), builder.Expression, ps);
			}
		}
	}
}
