using System;
using System.Collections.Generic;
using System.Data;
using OleDbType = LinqToDB.DataProvider.Wrappers.Mappers.OleDb.OleDbType;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class AccessDataProvider : DynamicDataProviderBase
	{
		public AccessDataProvider()
			: this(ProviderName.Access, new AccessMappingSchema())
		{
		}

		protected AccessDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.AcceptsTakeAsParameter           = false;
			SqlProviderFlags.IsSkipSupported                  = false;
			SqlProviderFlags.IsCountSubQuerySupported         = false;
			SqlProviderFlags.IsInsertOrUpdateSupported        = false;
			SqlProviderFlags.TakeHintsSupported               = TakeHints.Percent;
			SqlProviderFlags.IsCrossJoinSupported             = false;
			SqlProviderFlags.IsInnerJoinAsCrossSupported      = false;
			SqlProviderFlags.IsDistinctOrderBySupported       = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported = false;
			SqlProviderFlags.IsParameterOrderDependent        = true;
			SqlProviderFlags.IsUpdateFromSupported            = false;

			SetCharField            ("DBTYPE_WCHAR", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("DBTYPE_WCHAR", (r, i) => DataTools.GetChar(r, i));

			SetProviderField<IDataReader, TimeSpan, DateTime>((r, i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));
			SetProviderField<IDataReader, DateTime, DateTime>((r, i) => GetDateTime(r, i));

			_sqlOptimizer = new AccessSqlOptimizer(SqlProviderFlags);
		}

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1899 && value.Month == 12 && value.Day == 30)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

#if NET45 || NET46
		// for some unknown reason, dynamic load doesn't work for System.Data providers: OleDb, Odbc and SqlClient (netfx only)
		public             override Type DataReaderType      => typeof(System.Data.OleDb.OleDbDataReader);

		Type? _connectionType;
		protected internal override Type GetConnectionType()
		{
			if (_connectionType != null)
				return _connectionType;

			_connectionType = typeof(System.Data.OleDb.OleDbConnection);
			OnConnectionTypeCreated(_connectionType);
			return _connectionType;
		}
#endif

		public string AssemblyName => "System.Data.OleDb";

		public    override string ConnectionNamespace   => "System.Data.OleDb";
		protected override string ConnectionTypeName => $"{ConnectionNamespace}.OleDbConnection, {AssemblyName}";
		protected override string DataReaderTypeName => $"{ConnectionNamespace}.OleDbDataReader, {AssemblyName}";

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			if (Wrappers.Mappers.OleDb.ParameterType == null)
			{
				Wrappers.Mappers.OleDb.Initialize(connectionType.Assembly);
			}
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new AccessSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new AccessSchemaProvider(this);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			OleDbType? type = null;
			switch (dataType.DataType)
			{
				// "Data type mismatch in criteria expression" fix for culture-aware number decimal separator
				// unfortunatelly, regular fix using ExecuteScope=>InvariantCultureRegion
				// doesn't work for all situations
				case DataType.Decimal   :
				case DataType.VarNumeric: parameter.DbType = DbType.AnsiString; return;
				case DataType.DateTime  :
				case DataType.DateTime2 : type = OleDbType.Date        ; break;
				case DataType.Text      : type = OleDbType.LongVarChar ; break;
				case DataType.NText     : type = OleDbType.LongVarWChar; break;
			}

			if (type != null && Wrappers.Mappers.OleDb.TypeSetter != null)
			{
				var param = TryConvertParameter(Wrappers.Mappers.OleDb.ParameterType, parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Wrappers.Mappers.OleDb.TypeSetter(param, type.Value);
					return;
				}
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{

			return new AccessBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? AccessTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

#endregion
	}
}
