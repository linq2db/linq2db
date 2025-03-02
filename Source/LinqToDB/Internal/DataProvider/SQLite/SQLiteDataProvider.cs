using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SQLite.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	sealed class SQLiteDataProviderClassic : SQLiteDataProvider { public SQLiteDataProviderClassic() : base(ProviderName.SQLiteClassic, SQLiteProvider.System   ) {} }
	sealed class SQLiteDataProviderMS      : SQLiteDataProvider { public SQLiteDataProviderMS()      : base(ProviderName.SQLiteMS,      SQLiteProvider.Microsoft) {} }

	/*
	 * For now we don't have SQLite versioning as SQLite engine usually provided by ADO.NET provider as nuget dependency
	 * and we should just support some sane number of latest releases:
	 * System.Data.Sqlite: 1.0.115.5+ [3.37.0, 3.46.1]
	 * Microsoft.Data.Sqlite: 6.0.0+  [3.35.5, 3.46.1]
	 * where second version is version, shipped with latest provider release.
	 * This means we don't support anything lower than SQLite 3.35.5 and could also implement/enable features from newer versions if they doesn't break compatibility
	 * https://www.sqlite.org/changes.html
	 */
	public abstract class SQLiteDataProvider : DynamicDataProviderBase<SQLiteProviderAdapter>
	{
		protected SQLiteDataProvider(string name, SQLiteProvider provider)
			: base(name, MappingSchemaInstance.Get(provider), SQLiteProviderAdapter.GetInstance(provider))
		{
			Provider = provider;
			// currently enabled flags require at least 3.33.0 SQLite (for IsUpdateFromSupported)

			SqlProviderFlags.IsSkipSupported                   = false;
			SqlProviderFlags.IsSkipSupportedIfTake             = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctFromSupported           = true; // since 3.39.0
			SqlProviderFlags.SupportsPredicatesComparison      = true;
			SqlProviderFlags.DefaultMultiQueryIsolationLevel   = IsolationLevel.Serializable;

			// this actually requires compilation flag set
			// System.Data.Sqlite enabled it in 2021
			// MS use different runtime build without it enabled yet:
			// https://github.com/ericsink/SQLitePCL.raw/issues/377
			SqlProviderFlags.IsUpdateTakeSupported     = Provider == SQLiteProvider.System;
			SqlProviderFlags.IsUpdateSkipTakeSupported = Provider == SQLiteProvider.System;

			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel = null;

			// 3.15.0
			SqlProviderFlags.RowConstructorSupport = RowFeature.Equality        | RowFeature.Comparisons | RowFeature.UpdateLiteral |
			                                         RowFeature.CompareToSelect | RowFeature.Between     | RowFeature.Update;

			_sqlOptimizer = new SQLiteSqlOptimizer(SqlProviderFlags);

			/*
			 * WHAT'S WRONG WITH SQLITE:
			 * - sqlite has only 5 types: https://sqlite.org/datatype3.html
			 * - types applied to value, not to column => column could contain value of any type (e.g. all 5 types)
			 * - there is "column type affinity" thingy, which doesn't help with data read
			 *
			 * Which means our general approach to build column read expression, where we ask data reader
			 * about column type and read value using corresponding Get*() method, doesn't work as provider cannot
			 * give us detailed type information.
			 *
			 * How it works for supported providers
			 * System.Data.Sqlite:
			 * This provider actually works fine, as it use column type name from create table statement to infer column
			 * type. In other words, while you use proper type names to create your table and don't mix values of different
			 * types in your column - you are safe.
			 *
			 * Microsoft.Data.Sqlite:
			 * This provider decides to leave typing to user and return data to user only using 5 basic types
			 * (v1.x also could return int-typed value, which is just casted long value).
			 *
			 * Which means we need to handle Microsoft.Data.Sqlite in special way to be able to read data from database
			 * without fallback to slow-mode mapping
			 *
			 * There are two ways to fix it:
			 * 1. implement extra type-name resolve as it is done by System.Data.Sqlite (we can still get type name from provider)
			 * 2. implement mixed type support using target field type
			 *
			 * in other words use column type name vs target field type to decide value of which type we should create (read)
			 *
			 * While 2 sounds tempting, it doesn't work well with mapping to custom field types. Also VARIANT-like columns is
			 * not something users usually do, even with sqlite, so we will implement first approach here.
			 *
			 * Type information we can get from provider:
			 * 1. column type name from GetDataTypeName(): could be type name from CREATE TABLE statement or if this
			 *    information missing - standard type: INTEGER, REAL, TEXT, BLOB
			 *    for null/unknown type will be BLOB (or INTEGER in v1)
			 * 2. .net type of value: long, double, string, byte[]
			 *    for null/unknown type it will be byte[] (or int in v1)
			 *
			 * So, in code below we will map default type names and type names, used by System.Data.Sqlite to reader expressions.
			 * With additional fixes for cases, where it doesn't work well due to provider being unable to convert value to
			 * requested type.
			 */
			if (Name == ProviderName.SQLiteMS)
			{
				SetSqliteField((r, i) => r.GetInt64(i), new[] { typeof(long), typeof(string), typeof(double) },
					"INTEGER", "BIGINT", "COUNTER", "IDENTITY", "INT64", "INTEGER64", "LONG", "MEDIUMINT", "UINT", "UINT32", "UNSIGNEDINTEGER32");

				SetSqliteField((r, i) => r.GetDecimal(i), new[] { typeof(long), typeof(string), typeof(double) },
					"CURRENCY", "DECIMAL", "DECIMALTEXT", "MONEY", "NUMBER", "NUMERIC", "VARNUMERIC", "NUMERICTEXT", "SMALLMONEY", "BIGUINT", "UINT64", "ULONG", "UNSIGNEDINTEGER", "UNSIGNEDINTEGER64");

				SetSqliteField((r, i) => r.GetInt32(i), new[] { typeof(long), typeof(string), typeof(double) },
					"INT", "INT32", "INTEGER32", "MEDIUMINT", "SMALLUINT", "UINT16", "UNSIGNEDINTEGER16");

				SetSqliteField((r, i) => r.GetInt16(i), new[] { typeof(long), typeof(string), typeof(double) },
					"INT8", "INT16", "INTEGER8", "INTEGER16", "SMALLINT", "TINYSINT", "SBYTE");

				SetSqliteField((r, i) => r.GetByte(i), new[] { typeof(long), typeof(string), typeof(double) },
					"TINYINT", "UINT8", "UNSIGNEDINTEGER8", "BYTE");

				SetSqliteField((r, i) => r.GetDouble(i), new[] { typeof(long), typeof(string), typeof(double) },
					"REAL", "DOUBLE", "FLOAT");

				SetSqliteField((r, i) => r.GetByte(i), new[] { typeof(long), typeof(string), typeof(double) },
					"SINGLE");

				SetSqliteField((r, i) => r.GetString(i), new[] { typeof(string) },
					"TEXT", "CHAR", "CLOB", "LONGCHAR", "LONGTEXT", "LONGVARCHAR", "MEMO", "NCHAR", "NOTE", "NTEXT", "NVARCHAR", "STRING", "VARCHAR", "VARCHAR2");

				SetSqliteField((r, i) => (byte[])r.GetValue(i), new[] { typeof(byte[]), typeof(string) },
					"BLOB", "BINARY", "GENERAL", "IMAGE", "OLEOBJECT", "RAW", "VARBINARY");

				ReaderExpressions[new ReaderInfo { ToType = typeof(Guid), FieldType = typeof(byte[]) }] = (Expression<Func<DbDataReader, int, Guid>>)((r, i) => r.GetGuid(i));
				ReaderExpressions[new ReaderInfo { ToType = typeof(Guid), FieldType = typeof(string) }] = (Expression<Func<DbDataReader, int, Guid>>)((r, i) => r.GetGuid(i));

				SetSqliteField((r, i) => r.GetBoolean(i), new[] { typeof(long), typeof(string), typeof(double) },
					"BIT", "BOOL", "BOOLEAN", "LOGICAL", "YESNO");

				SetSqliteField((r, i) => r.GetDateTime(i), new[] { typeof(long), typeof(string), typeof(double) },
					"DATETIME", "DATETIME2", "DATE", "SMALLDATE", "SMALLDATETIME", "TIME", "TIMESTAMP", "DATETIMEOFFSET");

				// also specify explicit converter for non-integer numerics, repored as integer by provider
				SetToType<DbDataReader, float, long>((r, i) => r.GetFloat(i));
				SetToType<DbDataReader, double, long>((r, i) => r.GetDouble(i));
				SetToType<DbDataReader, decimal, long>((r, i) => r.GetDecimal(i));
			}
			else
			{
				ReaderExpressions[new ReaderInfo { ToType = typeof(Guid) }] = (Expression<Func<DbDataReader, int, Guid>>)((r, i) => r.GetGuid(i));
			}

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char" , DataTools.GetCharExpression);
			SetCharFieldToType<char>("nchar", DataTools.GetCharExpression);
		}

		private SQLiteProvider Provider { get; }

		private void SetSqliteField<T>(Expression<Func<DbDataReader, int, T>> expr, Type[] fieldTypes, params string[] typeNames)
		{
			foreach (var fieldType in fieldTypes)
			{
				foreach (var typeName in typeNames)
					SetField(typeName, fieldType, expr);

				// defaults: v2
				if (fieldType != typeof(byte[]))
					foreach (var typeName in typeNames)
						SetField(typeName, typeof(byte[]), expr);

				// defaults: v1
				foreach (var typeName in typeNames)
					SetField(typeName, typeof(int), expr);
			}
		}

		protected override string? NormalizeTypeName(string? typeName)
		{
			if (typeName == null)
				return null;

			var idx = typeName.IndexOf('(');
			if (idx != -1)
				return typeName.Substring(0, idx);

			return typeName;
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary               |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData      |
			TableOptions.CreateIfNotExists         |
			TableOptions.DropIfExists;

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new SQLiteMemberTranslator();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new SQLiteSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema ClassicMappingSchema   = new SQLiteMappingSchema.ClassicMappingSchema();
			public static readonly MappingSchema MicrosoftMappingSchema = new SQLiteMappingSchema.MicrosoftMappingSchema();

			public static MappingSchema Get(SQLiteProvider provider) => provider == SQLiteProvider.System ? ClassicMappingSchema : MicrosoftMappingSchema;
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SQLiteSchemaProvider();
		}

		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
			if (options.FindOrDefault(SQLiteOptions.Default).AlwaysCheckDbNull)
				return true;

			return base.IsDBNullAllowed(options, reader, idx);
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			// handles situation, when char values were serialized as character hex value for some
			// versions of Microsoft.Data.Sqlite
			if (Name == ProviderName.SQLiteMS && value is char chr)
				value = chr.ToString();

			if (value is Guid guid)
			{
				// keep in sync with ConvertGuidToSql in mapping schema
				switch (dataType.DataType, dataType.DbType)
				{
					case (DataType.NChar, _) or (DataType.NVarChar, _) or (DataType.NText, _)
						or (DataType.Char, _) or (DataType.VarChar, _) or (DataType.Text, _)
						or (_, "TEXT"):

						value = guid.ToString().ToUpperInvariant();

						if (Name == ProviderName.SQLiteClassic)
							dataType = dataType.WithDataType(DataType.Text);

						break;
					default:
						if (Name == ProviderName.SQLiteMS)
						{
							// reverting compatibility breaking change in Microsoft.Data.Sqlite 3.0.0
							// https://github.com/aspnet/EntityFrameworkCore/issues/15078
							// pre-3.0 and System.Data.Sqlite uses binary type for Guid values, there is no reason to replace it with string value
							// we can allow strings later if there will be request for it
							value = guid.ToByteArray();
						}

						break;
				}
			}

			if (value is DateTime dt)
			{
				value = dt.ToString("yyyy-MM-dd HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);
				if (Name == ProviderName.SQLiteClassic)
					dataType = dataType.WithDataType(DataType.VarChar);
			}

#if NET6_0_OR_GREATER
			if (!Adapter.SupportsDateOnly && value is DateOnly d)
			{
				value     = d.ToDateTime(TimeOnly.MinValue);
				if (dataType.DataType == DataType.Date)
				{
					value = ((DateTime)value).ToString(SQLiteMappingSchema.DATE_FORMAT_RAW, CultureInfo.InvariantCulture);
					if (Name == ProviderName.SQLiteClassic)
						dataType = dataType.WithDataType(DataType.VarChar);
				}
			}
#endif

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.UInt32    : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64    : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2 : dataType = dataType.WithDataType(DataType.DateTime); break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new SQLiteBulkCopy().BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SQLiteOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SQLiteBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SQLiteOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SQLiteBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SQLiteOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
