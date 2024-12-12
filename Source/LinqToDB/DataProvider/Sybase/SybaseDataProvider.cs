using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using Common;
	using Data;
	using Extensions;
	using Linq.Translation;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;
	using Translation;

	sealed class SybaseDataProviderNative  : SybaseDataProvider { public SybaseDataProviderNative()  : base(ProviderName.Sybase,        SybaseProvider.Unmanaged ) {} }
	sealed class SybaseDataProviderManaged : SybaseDataProvider { public SybaseDataProviderManaged() : base(ProviderName.SybaseManaged, SybaseProvider.DataAction) {} }

	public abstract class SybaseDataProvider : DynamicDataProviderBase<SybaseProviderAdapter>
	{
		#region Init

		protected SybaseDataProvider(string name, SybaseProvider provider)
			: this(name, SybaseProviderAdapter.GetInstance(provider == SybaseProvider.AutoDetect ? provider = SybaseProviderDetector.DetectProvider() : provider))
		{
		}

		protected SybaseDataProvider(string name, SybaseProviderAdapter adapter)
			: base(name, MappingSchemaInstance.Get(name), adapter)
		{
			SqlProviderFlags.AcceptsTakeAsParameter           = false;
			SqlProviderFlags.IsSkipSupported                  = false;
			SqlProviderFlags.IsSubQueryTakeSupported          = false;
			SqlProviderFlags.CanCombineParameters             = false;
			SqlProviderFlags.IsCrossJoinSupported             = false;
			// TODO: add versioning as it is available since 16SP3 or just ignore old versions?
			SqlProviderFlags.IsDistinctSetOperationsSupported = false;
			SqlProviderFlags.IsWindowFunctionsSupported       = false;
			SqlProviderFlags.IsDerivedTableOrderBySupported   = false;
			SqlProviderFlags.IsUpdateTakeSupported            = true;

			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel        = 1;
			SqlProviderFlags.IsCorrelatedSubQueryTakeSupported         = false;
			SqlProviderFlags.IsJoinDerivedTableWithTakeInvalid         = true;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  DataTools.GetCharExpression);
			SetCharFieldToType<char>("nchar", DataTools.GetCharExpression);

			SetProviderField<DbDataReader, TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1900, 1, 1));
			SetField<DbDataReader, DateTime>("time", (r,i) => GetDateTimeAsTime(r.GetDateTime(i)));

			_sqlOptimizer = new SybaseSqlOptimizer(SqlProviderFlags);
		}

		static DateTime GetDateTimeAsTime(DateTime value)
		{
			if (value.Year == 1900 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		#endregion

		#region Overrides

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			type = base.ConvertParameterType(type, dataType);

			// native client BulkCopy cannot stand nullable types
			// AseBulkManager.IsWrongType
			if (Name == ProviderName.Sybase)
			{
				type = type.ToNullableUnderlying();
				if (type == typeof(char) || type == typeof(Guid))
					type = typeof(string);
				else if (type == typeof(TimeSpan))
					type = typeof(DateTime);
			}

			return type;
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryData       |
			TableOptions.IsGlobalTemporaryData      |
			TableOptions.CreateIfNotExists          |
			TableOptions.DropIfExists;

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new SybaseMemberTranslator();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new SybaseSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			static readonly MappingSchema _nativeMappingSchema  = new SybaseMappingSchema.NativeMappingSchema();
			static readonly MappingSchema _managedMappingSchema = new SybaseMappingSchema.ManagedMappingSchema();

			public static MappingSchema Get(string name) => name == ProviderName.Sybase ? _nativeMappingSchema : _managedMappingSchema;
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SybaseSchemaProvider(this);
		}

		public override IQueryParametersNormalizer GetQueryParameterNormalizer() => new SybaseParametersNormalizer();

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.SByte      :
					dataType = dataType.WithDataType(DataType.Int16);
					if (value is sbyte sbyteValue)
						value = (short)sbyteValue;
					break;

				case DataType.Time       :
					if (value is TimeSpan ts)
						value = new DateTime(1900, 1, 1) + ts;
					break;

				case DataType.Xml        :
					dataType = dataType.WithDataType(DataType.NVarChar);
						 if (value is XDocument  xdoc) value = xdoc.ToString();
					else if (value is XmlDocument xml) value = xml.InnerXml;
					break;

				case DataType.Guid       :
					if (value != null)
						value = string.Format(CultureInfo.InvariantCulture, "{0}", value);
					dataType = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;

				case DataType.Undefined  :
					if (value == null)
						dataType = dataType.WithDataType(DataType.Char);
					break;

				case DataType.Char       :
				case DataType.NChar      :
					if (Name == ProviderName.Sybase)
						if (value is char chr)
							value = chr.ToString();
					break;

#if NET6_0_OR_GREATER
				case DataType.Date       :
					if (value is DateOnly d)
						value = d.ToDateTime(TimeOnly.MinValue);
					break;
#endif
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			SybaseProviderAdapter.AseDbType? type = null;

			switch (dataType.DataType)
			{
				case DataType.Text          : type = SybaseProviderAdapter.AseDbType.Text;             break;
				case DataType.NText         : type = SybaseProviderAdapter.AseDbType.Unitext;          break;
				case DataType.Blob          :
				case DataType.VarBinary     : type = SybaseProviderAdapter.AseDbType.VarBinary;        break;
				case DataType.Image         : type = SybaseProviderAdapter.AseDbType.Image;            break;
				case DataType.SmallMoney    : type = SybaseProviderAdapter.AseDbType.SmallMoney;       break;
				case DataType.SmallDateTime : type = SybaseProviderAdapter.AseDbType.SmallDateTime;    break;
				case DataType.Timestamp     : type = SybaseProviderAdapter.AseDbType.TimeStamp;        break;
			}

			if (type != null)
			{
				var param = TryGetProviderParameter(dataConnection, parameter);
				if (param != null)
				{
					Adapter.SetDbType(param, type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				// fallback types
				case DataType.Text          : parameter.DbType = DbType.AnsiString; break;
				case DataType.NText         : parameter.DbType = DbType.String;     break;
				case DataType.Timestamp     :
				case DataType.Image         : parameter.DbType = DbType.Binary;     break;
				case DataType.SmallMoney    : parameter.DbType = DbType.Currency;   break;
				case DataType.SmallDateTime : parameter.DbType = DbType.DateTime;   break;

				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal;    break;
				case DataType.Binary        : parameter.DbType = DbType.Binary;     break;
				case DataType.Money         : parameter.DbType = DbType.Currency;   break;
				case DataType.DateTime2     : parameter.DbType = DbType.DateTime;   break;
				default                     :
					base.SetParameterType(dataConnection, parameter, dataType);     break;
			}
		}

		#endregion

		#region BulkCopy

		SybaseBulkCopy? _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SybaseOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SybaseOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			_bulkCopy ??= new (this);

			return _bulkCopy.BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SybaseOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
