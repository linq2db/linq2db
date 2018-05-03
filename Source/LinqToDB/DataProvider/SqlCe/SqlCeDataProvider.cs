using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SqlCeDataProvider : DynamicDataProviderBase
	{
		public SqlCeDataProvider()
			: this(ProviderName.SqlCe, new SqlCeMappingSchema())
		{
		}

		protected SqlCeDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSubQueryColumnSupported = false;
			SqlProviderFlags.IsCountSubQuerySupported  = false;
			SqlProviderFlags.IsApplyJoinSupported      = true;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;
			SqlProviderFlags.IsCrossJoinSupported      = true;

			SetCharFieldToType<char>("NChar", (r, i) => DataTools.GetChar(r, i));

			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new SqlCeSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace => "System.Data.SqlServerCe";
		protected override string ConnectionTypeName  => $"{ConnectionNamespace}.SqlCeConnection, {ConnectionNamespace}";
		protected override string DataReaderTypeName  => $"{ConnectionNamespace}.SqlCeDataReader, {ConnectionNamespace}";

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_setNText     = GetSetParameter(connectionType, SqlDbType.NText);
			_setNChar     = GetSetParameter(connectionType, SqlDbType.NChar);
			_setNVarChar  = GetSetParameter(connectionType, SqlDbType.NVarChar);
			_setTimestamp = GetSetParameter(connectionType, SqlDbType.Timestamp);
			_setBinary    = GetSetParameter(connectionType, SqlDbType.Binary);
			_setVarBinary = GetSetParameter(connectionType, SqlDbType.VarBinary);
			_setImage     = GetSetParameter(connectionType, SqlDbType.Image);
			_setDateTime  = GetSetParameter(connectionType, SqlDbType.DateTime );
			_setMoney     = GetSetParameter(connectionType, SqlDbType.Money);
			_setBoolean   = GetSetParameter(connectionType, SqlDbType.Bit);
		}

		static Action<IDbDataParameter> _setNText;
		static Action<IDbDataParameter> _setNChar;
		static Action<IDbDataParameter> _setNVarChar;
		static Action<IDbDataParameter> _setTimestamp;
		static Action<IDbDataParameter> _setBinary;
		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setImage;
		static Action<IDbDataParameter> _setDateTime;
		static Action<IDbDataParameter> _setMoney;
		static Action<IDbDataParameter> _setBoolean;

		static Action<IDbDataParameter> GetSetParameter(Type connectionType, SqlDbType value)
		{
			var pType  = connectionType.AssemblyEx().GetType(connectionType.Namespace + ".SqlCeParameter", true);

			var p = Expression.Parameter(typeof(IDbDataParameter));
			var l = Expression.Lambda<Action<IDbDataParameter>>(
				Expression.Assign(
					Expression.PropertyOrField(
						Expression.Convert(p, pType),
						"SqlDbType"),
					Expression.Constant(value)),
				p);

			return l.Compile();
		}

		#region Overrides

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlCeSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD1_6
		public override ISchemaProvider GetSchemaProvider()
		{
			return new SqlCeSchemaProvider();
		}
#endif

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Xml :
					dataType = DataType.NVarChar;

					if (value is SqlXml)
					{
						var xml = (SqlXml)value;
						value = xml.IsNull ? null : xml.Value;
					}
					else if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;

					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte      : parameter.DbType    = DbType.Int16;   break;
				case DataType.UInt16     : parameter.DbType    = DbType.Int32;   break;
				case DataType.UInt32     : parameter.DbType    = DbType.Int64;   break;
				case DataType.UInt64     : parameter.DbType    = DbType.Decimal; break;
				case DataType.VarNumeric : parameter.DbType    = DbType.Decimal; break;
				case DataType.Text       :
				case DataType.NText      : _setNText    (parameter); break;
				case DataType.Char       :
				case DataType.NChar      : _setNChar    (parameter); break;
				case DataType.VarChar    :
				case DataType.NVarChar   : _setNVarChar (parameter); break;
				case DataType.Timestamp  : _setTimestamp(parameter); break;
				case DataType.Binary     : _setBinary   (parameter); break;
				case DataType.VarBinary  : _setVarBinary(parameter); break;
				case DataType.Image      : _setImage    (parameter); break;
				case DataType.Date       :
				case DataType.DateTime   :
				case DataType.DateTime2  : _setDateTime (parameter); break;
				case DataType.Money      : _setMoney    (parameter); break;
				case DataType.Boolean    : _setBoolean  (parameter); break;
				default                  :
					base.SetParameterType(parameter, dataType);
					break;
			}
		}

#endregion

		public void CreateDatabase([JetBrains.Annotations.NotNull] string databaseName, bool deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			CreateFileDatabase(
				databaseName, deleteIfExists, ".sdf",
				dbName =>
				{
					dynamic eng = Activator.CreateInstance(
						GetConnectionType().AssemblyEx().GetType("System.Data.SqlServerCe.SqlCeEngine"),
						"Data Source=" + dbName);

					eng.CreateDatabase();

					var disp = eng as IDisposable;

					if (disp != null)
						disp.Dispose();
				});
		}

		public void DropDatabase([JetBrains.Annotations.NotNull] string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			DropFileDatabase(databaseName, ".sdf");
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SqlCeBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SqlCeTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

#endregion
	}
}
