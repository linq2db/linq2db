#if NETFRAMEWORK || NETCOREAPP
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;

	public class SapHanaDataProvider : DynamicDataProviderBase<SapHanaProviderAdapter>
	{
		public SapHanaDataProvider()
			: this(ProviderName.SapHanaNative)
		{
		}

		public SapHanaDataProvider(string name)
			: this(name, MappingSchemaInstance)
		{
		}
		protected SapHanaDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, SapHanaProviderAdapter.GetInstance())
		{
			SqlProviderFlags.IsParameterOrderDependent  = true;

			//supported flags
			SqlProviderFlags.IsCountSubQuerySupported   = true;

			//Exception: Sap.Data.Hana.HanaException
			//Message: single-row query returns more than one row
			//when expression returns more than 1 row
			//mark this as supported, it's better to throw exception
			//instead of replace with left join, in which case returns incorrect data
			SqlProviderFlags.IsSubQueryColumnSupported   = true;

			SqlProviderFlags.IsTakeSupported             = true;
			SqlProviderFlags.IsDistinctOrderBySupported  = false;

			//not supported flags
			SqlProviderFlags.IsSubQueryTakeSupported    = false;
			SqlProviderFlags.IsApplyJoinSupported       = false;
			SqlProviderFlags.IsInsertOrUpdateSupported  = false;
			SqlProviderFlags.IsUpdateFromSupported      = false;

			_sqlOptimizer = new SapHanaNativeSqlOptimizer(SqlProviderFlags);
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new SapHanaSchemaProvider();
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsLocalTemporaryData;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SapHanaSqlBuilder(mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType.DataType)
			{
				case DataType.NChar:
				case DataType.Char:
					type = typeof (string);
					break;
				case DataType.Boolean: if (type == typeof(bool)) return typeof(byte);   break;
				case DataType.Guid   : if (type == typeof(Guid)) return typeof(string); break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.Boolean:
					dataType = dataType.WithDataType(DataType.Byte);
					if (value is bool b)
						value = b ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value != null)
						value = value.ToString();
					dataType = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			SapHanaProviderAdapter.HanaDbType? type = null;
			switch (dataType.DataType)
			{
				case DataType.Text : type = SapHanaProviderAdapter.HanaDbType.Text; break;
				case DataType.Image: type = SapHanaProviderAdapter.HanaDbType.Blob; break;
			}

			if (type != null)
			{
				var param = TryGetProviderParameter(parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Adapter.SetDbType(param, type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				// fallback types
				case DataType.Text  : parameter.DbType = DbType.String; return;
				case DataType.Image : parameter.DbType = DbType.Binary; return;

				case DataType.NText : parameter.DbType = DbType.Xml;    return;
				case DataType.Binary: parameter.DbType = DbType.Binary; return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SapHanaBulkCopy(this).BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SapHanaTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SapHanaBulkCopy(this).BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SapHanaTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

#if !NETFRAMEWORK
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new SapHanaBulkCopy(this).BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SapHanaTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}
#endif

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			// provider fails to set AllowDBNull for some results
			return true;
		}

		private static readonly MappingSchema MappingSchemaInstance = new SapHanaMappingSchema.NativeMappingSchema();
	}
}
#endif
