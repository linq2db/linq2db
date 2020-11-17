using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;
	using Common;
	using Extensions;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SQLiteDataProvider : DynamicDataProviderBase<SQLiteProviderAdapter>
	{
		/// <summary>
		/// Creates the specified SQLite provider based on the provider name.
		/// </summary>
		/// <param name="name">If ProviderName.SQLite is provided,
		/// the detection mechanism preferring System.Data.SQLite
		/// to Microsoft.Data.Sqlite will be used.</param>
		public SQLiteDataProvider(string name)
			: this(name, MappingSchemaInstance.Get(name))
		{
		}

		protected SQLiteDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, SQLiteProviderAdapter.GetInstance(name))
		{
			SqlProviderFlags.IsSkipSupported                   = false;
			SqlProviderFlags.IsSkipSupportedIfTake             = true;
			SqlProviderFlags.IsInsertOrUpdateSupported         = false;
			SqlProviderFlags.IsUpdateSetTableAliasSupported    = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = true;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.DefaultMultiQueryIsolationLevel   = IsolationLevel.Serializable;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("nchar", (r, i) => DataTools.GetChar(r, i));

			_sqlOptimizer = new SQLiteSqlOptimizer(SqlProviderFlags);

			if (Name == ProviderName.SQLiteMS)
			{
				SetToType<IDataReader, float, byte >((r, i) => r.GetFloat(i));
				SetToType<IDataReader, float, short>((r, i) => r.GetFloat(i));
				SetToType<IDataReader, float, int  >((r, i) => r.GetFloat(i));
				SetToType<IDataReader, float, long >((r, i) => r.GetFloat(i));

				SetToType<IDataReader, double, byte >((r, i) => r.GetDouble(i));
				SetToType<IDataReader, double, short>((r, i) => r.GetDouble(i));
				SetToType<IDataReader, double, int  >((r, i) => r.GetDouble(i));
				SetToType<IDataReader, double, long >((r, i) => r.GetDouble(i));

				SetToType<IDataReader, decimal, byte >((r, i) => r.GetDecimal(i));
				SetToType<IDataReader, decimal, short>((r, i) => r.GetDecimal(i));
				SetToType<IDataReader, decimal, int  >((r, i) => r.GetDecimal(i));
				SetToType<IDataReader, decimal, long >((r, i) => r.GetDecimal(i));
			}
		}

		protected override string? NormalizeTypeName(string? typeName)
		{
			if (typeName == null)
				return null;

			if (typeName.StartsWith("char("))
				return "char";

			if (typeName.StartsWith("nchar("))
				return "nchar";

			return typeName;
		}

		public override IDisposable? ExecuteScope(DataConnection dataConnection)
		{
			if (Adapter.DisposeCommandOnError)
				return new CallOnExceptionRegion(() => dataConnection.DisposeCommand());

			return base.ExecuteScope(dataConnection);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary               |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData      |
			TableOptions.CreateIfNotExists         |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SQLiteSqlBuilder(mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema ClassicMappingSchema   = new SQLiteMappingSchema.ClassicMappingSchema();
			public static readonly MappingSchema MicrosoftMappingSchema = new SQLiteMappingSchema.MicrosoftMappingSchema();

			public static MappingSchema Get(string name) => name == ProviderName.SQLiteClassic ? ClassicMappingSchema : MicrosoftMappingSchema;
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SQLiteSchemaProvider();
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			if (SQLiteTools.AlwaysCheckDbNull)
				return true;

			return base.IsDBNullAllowed(reader, idx);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			// handles situation, when char values were serialized as character hex value for some
			// versions of Microsoft.Data.Sqlite
			if (Name == ProviderName.SQLiteMS && value is char)
				value = value.ToString();

			// reverting compatibility breaking change in Microsoft.Data.Sqlite 3.0.0
			// https://github.com/aspnet/EntityFrameworkCore/issues/15078
			// pre-3.0 and System.Data.Sqlite uses binary type for Guid values, there is no reason to replace it with string value
			// we can allow strings later if there will be request for it
			if (Name == ProviderName.SQLiteMS && value is Guid guid)
			{
				value = guid.ToByteArray();
			}

			base.SetParameter(dataConnection, parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
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

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SQLiteBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SQLiteBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if !NETFRAMEWORK
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SQLiteBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		#endregion
	}
}
