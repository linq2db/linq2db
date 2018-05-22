using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;
	using System.Data.Common;

	public class SapHanaDataProvider : DynamicDataProviderBase
	{
		public SapHanaDataProvider()
			: this(ProviderName.SapHana, new SapHanaMappingSchema())
		{
		}

		protected SapHanaDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			//supported flags
			SqlProviderFlags.IsCountSubQuerySupported = true;

			//Exception: Sap.Data.Hana.HanaException
			//Message: single-row query returns more than one row
			//when expression returns more than 1 row
			//mark this as supported, it's better to throw exception
			//instead of replace with left join, in which case returns incorrect data
			SqlProviderFlags.IsSubQueryColumnSupported = true;

			SqlProviderFlags.IsTakeSupported = true;

			//testing

			//not supported flags
			SqlProviderFlags.IsSubQueryTakeSupported   = false;
			SqlProviderFlags.IsApplyJoinSupported      = false;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;

			_sqlOptimizer = new SapHanaSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace => "Sap.Data.Hana";
		protected override string ConnectionTypeName  => $"{ConnectionNamespace}.HanaConnection, {SapHanaTools.AssemblyName}";
		protected override string DataReaderTypeName  => $"{ConnectionNamespace}.HanaDataReader, {SapHanaTools.AssemblyName}";

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		private          Type _dataReaderType;
		private volatile Type _connectionType;

		public override Type DataReaderType
		{
			get
			{
				if (_dataReaderType != null)
					return _dataReaderType;

				_dataReaderType = Type.GetType(DataReaderTypeName, false);

				if (_dataReaderType == null)
				{
					var assembly = DbProviderFactories.GetFactory("Sap.Data.Hana").GetType().Assembly;
					_dataReaderType = Type.GetType($"{ConnectionNamespace}.HanaDataReader, {assembly.GetName()}", true);
				}

				return _dataReaderType;
			}
		}

		protected override Type GetConnectionType()
		{
			if (_connectionType == null)
				lock (SyncRoot)
					if (_connectionType == null)
					{
						_connectionType = Type.GetType(ConnectionTypeName, false);

						if (_connectionType == null)
						{
							var db = DbProviderFactories.GetFactory("Sap.Data.Hana").CreateConnection();

							_connectionType = db.GetType();
						}

						OnConnectionTypeCreated(_connectionType);
					}

			return _connectionType;
		}
#endif

		static Action<IDbDataParameter> _setText;
		static Action<IDbDataParameter> _setNText;
		static Action<IDbDataParameter> _setBlob;
		static Action<IDbDataParameter> _setVarBinary;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			const String paramTypeName = "HanaParameter";
			const String dataTypeName  = "HanaDbType";

			_setText      = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "Text");
			_setNText     = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "NClob");
			_setBlob      = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "Blob");
			_setVarBinary = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "VarBinary");
		}

#if !NETSTANDARD1_6
		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new SapHanaSchemaProvider();
		}
#endif

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override Type ConvertParameterType(Type type, DataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType)
			{
				case DataType.NChar:
				case DataType.Char:
					type = typeof (String);
					break;
				case DataType.Boolean: if (type == typeof(bool)) return typeof(byte);  break;
				case DataType.Guid   : if (type == typeof(Guid)) return typeof(string); break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Boolean:
					dataType = DataType.Byte;
					if (value is bool)
						value = (bool)value ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value != null)
						value = value.ToString();
					dataType = DataType.Char;
					parameter.Size = 36;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			switch (dataType)
			{
				case DataType.Text  : _setText(parameter);      break;
				case DataType.Image : _setBlob(parameter);      break;
				case DataType.NText : _setNText(parameter);     break;
				case DataType.Binary: _setVarBinary(parameter); break;
			}
			base.SetParameterType(parameter, dataType);
		}

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SapHanaBulkCopy(this, GetConnectionType()).BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SapHanaTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

		protected override BasicMergeBuilder<TTarget, TSource> GetMergeBuilder<TTarget, TSource>(
			DataConnection connection,
			IMergeable<TTarget, TSource> merge)
		{
			return new SapHanaMergeBuilder<TTarget, TSource>(connection, merge);
		}
	}
}
