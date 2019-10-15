#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Data;
	using LinqToDB.Extensions;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SqlServerDataProvider : DynamicDataProviderBase
	{
		#region Init

		public SqlServerDataProvider(string name, SqlServerVersion version)
			: this(name, version, SqlServerProvider.SystemDataSqlClient)
		{
		}

		public SqlServerDataProvider(string name, SqlServerVersion version, SqlServerProvider provider)
			: base(name, null)
		{
			Version = version;
			Provider = provider;

			SqlProviderFlags.IsDistinctOrderBySupported       = false;
			SqlProviderFlags.IsSubQueryOrderBySupported       = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported = true;
			SqlProviderFlags.IsUpdateFromSupported            = true;

			if (version == SqlServerVersion.v2000)
			{
				SqlProviderFlags.AcceptsTakeAsParameter   = false;
				SqlProviderFlags.IsSkipSupported          = false;
				SqlProviderFlags.IsCountSubQuerySupported = false;
			}
			else
			{
				SqlProviderFlags.IsApplyJoinSupported              = true;
				SqlProviderFlags.TakeHintsSupported                = TakeHints.Percent | TakeHints.WithTies;
				SqlProviderFlags.IsCommonTableExpressionsSupported = version >= SqlServerVersion.v2008;
			}

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("nchar", (r, i) => DataTools.GetChar(r, i));

			_sqlServer2000SqlOptimizer = new SqlServer2000SqlOptimizer(SqlProviderFlags);
			_sqlServer2005SqlOptimizer = new SqlServer2005SqlOptimizer(SqlProviderFlags);
			_sqlServer2008SqlOptimizer = new SqlServer2008SqlOptimizer    (SqlProviderFlags);
			_sqlServer2012SqlOptimizer = new SqlServer2012SqlOptimizer(SqlProviderFlags);
			_sqlServer2017SqlOptimizer = new SqlServer2017SqlOptimizer(SqlProviderFlags);

			SetField<IDataReader,decimal>((r,i) => r.GetDecimal(i));
			SetField<IDataReader,decimal>("money",      (r,i) => SqlServerTools.DataReaderGetMoney  (r, i));
			SetField<IDataReader,decimal>("smallmoney", (r,i) => SqlServerTools.DataReaderGetMoney  (r, i));
			SetField<IDataReader,decimal>("decimal",    (r,i) => SqlServerTools.DataReaderGetDecimal(r, i));
		}

		private Type                                _parameterType;
		private Type                                _sqlDataRecordType;

		private Action<IDbDataParameter, string>    _setUdtTypeName;
		private Action<IDbDataParameter, string>    _setTypeName;
		private Action<IDbDataParameter, SqlDbType> _setSqlDbType;

		private Func<IDbDataParameter, SqlDbType>   _getSqlDbType;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetProviderField<SqlBinary  , SqlBinary  >("GetSqlBinary"  );
				SetProviderField<SqlBoolean , SqlBoolean > ("GetSqlBoolean");
				SetProviderField<SqlByte    , SqlByte    > ("GetSqlByte"   );
				SetProviderField<SqlDateTime, SqlDateTime>("GetSqlDateTime");
				SetProviderField<SqlDecimal , SqlDecimal >("GetSqlDecimal" );
				SetProviderField<SqlDouble  , SqlDouble  >("GetSqlDouble"  );
				SetProviderField<SqlGuid    , SqlGuid    >("GetSqlGuid"    );
				SetProviderField<SqlInt16   , SqlInt16   >("GetSqlInt16"   );
				SetProviderField<SqlInt32   , SqlInt32   >("GetSqlInt32"   );
				SetProviderField<SqlInt64   , SqlInt64   >("GetSqlInt64"   );
				SetProviderField<SqlMoney   , SqlMoney   >("GetSqlMoney"   );
				SetProviderField<SqlSingle  , SqlSingle  >("GetSqlSingle"  );
				SetProviderField<SqlString  , SqlString  >("GetSqlString"  );
				SetProviderField<SqlXml     , SqlXml     >("GetSqlXml"     );

				SetProviderField<DateTimeOffset>("GetDateTimeOffset");
				SetProviderField<TimeSpan>      ("GetTimeSpan");
			}
			else
			{
				SetProviderField<IDataReader,SqlString  ,SqlString  >((r,i) => r.GetString  (i));
			}

			_parameterType     = connectionType.Assembly.GetType(ParameterTypeName,     true);
			_sqlDataRecordType = connectionType.Assembly.GetType(SqlDataRecordTypeName, true);

			_setUdtTypeName = GetSetParameter<string>   (_parameterType, "UdtTypeName", typeof(string));
			_setTypeName    = GetSetParameter<string>   (_parameterType, "TypeName",    typeof(string));
			_setSqlDbType   = GetSetParameter<SqlDbType>(_parameterType, "SqlDbType",   typeof(SqlDbType));
			_getSqlDbType   = GetGetParameter<SqlDbType>(_parameterType, "SqlDbType");
		}

#if NET45 || NET46
		Type _dataReaderType;
		public override Type DataReaderType
		{
			get
			{
				if (_dataReaderType != null)
					return _dataReaderType;

				if (Provider == SqlServerProvider.SystemDataSqlClient)
				{
					_dataReaderType = typeof(System.Data.SqlClient.SqlDataReader);
					return _dataReaderType;
				}

				return base.DataReaderType;
			}
		}

		Type _connectionType;
		protected internal override Type GetConnectionType()
		{
			if (_connectionType != null)
				return _connectionType;

			if (Provider == SqlServerProvider.SystemDataSqlClient)
			{
				_connectionType = typeof(System.Data.SqlClient.SqlConnection);
				OnConnectionTypeCreated(_connectionType);
				return _connectionType;
			}

			return base.GetConnectionType();
		}
#endif

		#endregion

		#region Public Properties

		public             string AssemblyName          => Provider == SqlServerProvider.SystemDataSqlClient    ? "System.Data.SqlClient" : "Microsoft.Data.SqlClient";
		public    override string ConnectionNamespace   => Provider == SqlServerProvider.MicrosoftDataSqlClient ? "Microsoft.Data.SqlClient" : "System.Data.SqlClient";
		protected override string ConnectionTypeName    => $"{ConnectionNamespace}.SqlConnection, {AssemblyName}";
		protected override string DataReaderTypeName    => $"{ConnectionNamespace}.SqlDataReader, {AssemblyName}";
		protected          string ParameterTypeName     => $"{ConnectionNamespace}.SqlParameter";
		protected          string SqlDataRecordTypeName => Provider == SqlServerProvider.MicrosoftDataSqlClient ? "Microsoft.Data.SqlClient.Server.SqlDataRecord" : "Microsoft.SqlServer.Server.SqlDataRecord";

#if !NETSTANDARD2_0
		public override string DbFactoryProviderName => Provider == SqlServerProvider.MicrosoftDataSqlClient ? "Microsoft.Data.SqlClient" : "System.Data.SqlClient";
#endif

		public SqlServerVersion Version { get; }

		public SqlServerProvider Provider { get; }

		#endregion

		#region Overrides

		static class MappingSchemaInstance
		{
			public static readonly SqlServer2000MappingSchema SqlServer2000MappingSchema = new SqlServer2000MappingSchema();
			public static readonly SqlServer2005MappingSchema SqlServer2005MappingSchema = new SqlServer2005MappingSchema();
			public static readonly SqlServer2008MappingSchema SqlServer2008MappingSchema = new SqlServer2008MappingSchema();
			public static readonly SqlServer2012MappingSchema SqlServer2012MappingSchema = new SqlServer2012MappingSchema();
			public static readonly SqlServer2017MappingSchema SqlServer2017MappingSchema = new SqlServer2017MappingSchema();
		}

		public override MappingSchema MappingSchema
		{
			get
			{
				switch (Version)
				{
					case SqlServerVersion.v2000 : return MappingSchemaInstance.SqlServer2000MappingSchema;
					case SqlServerVersion.v2005 : return MappingSchemaInstance.SqlServer2005MappingSchema;
					case SqlServerVersion.v2008 : return MappingSchemaInstance.SqlServer2008MappingSchema;
					case SqlServerVersion.v2012 : return MappingSchemaInstance.SqlServer2012MappingSchema;
					case SqlServerVersion.v2017 : return MappingSchemaInstance.SqlServer2017MappingSchema;
				}

				return base.MappingSchema;
			}
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			switch (Version)
			{
				case SqlServerVersion.v2000 : return new SqlServer2000SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
				case SqlServerVersion.v2005 : return new SqlServer2005SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
				case SqlServerVersion.v2008 : return new SqlServer2008SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
				case SqlServerVersion.v2012 : return new SqlServer2012SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
				case SqlServerVersion.v2017 : return new SqlServer2017SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
			}

			throw new InvalidOperationException();
		}

		readonly ISqlOptimizer _sqlServer2000SqlOptimizer;
		readonly ISqlOptimizer _sqlServer2005SqlOptimizer;
		readonly ISqlOptimizer _sqlServer2008SqlOptimizer;
		readonly ISqlOptimizer _sqlServer2012SqlOptimizer;
		readonly ISqlOptimizer _sqlServer2017SqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			switch (Version)
			{
				case SqlServerVersion.v2000 : return _sqlServer2000SqlOptimizer;
				case SqlServerVersion.v2005 : return _sqlServer2005SqlOptimizer;
				case SqlServerVersion.v2008 : return _sqlServer2008SqlOptimizer;
				case SqlServerVersion.v2012 : return _sqlServer2012SqlOptimizer;
				case SqlServerVersion.v2017 : return _sqlServer2017SqlOptimizer;
			}

			return _sqlServer2008SqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return Version == SqlServerVersion.v2000 ? new SqlServer2000SchemaProvider() : new SqlServerSchemaProvider();
		}

		static readonly ConcurrentDictionary<string,bool> _marsFlags = new ConcurrentDictionary<string,bool>();

		public override object GetConnectionInfo(DataConnection dataConnection, string parameterName)
		{
			switch (parameterName)
			{
				case "IsMarsEnabled" :
					if (dataConnection.ConnectionString != null)
					{
						if (!_marsFlags.TryGetValue(dataConnection.Connection.ConnectionString, out var flag))
						{
							flag = dataConnection.Connection.ConnectionString.Split(';')
								.Select(s => s.Split('='))
								.Where (s => s.Length == 2 && s[0].Trim().ToLower() == "multipleactiveresultsets")
								.Select(s => s[1].Trim().ToLower())
								.Any   (s => s == "true" || s == "1" || s == "yes");

							_marsFlags[dataConnection.Connection.ConnectionString] = flag;
						}

						return flag;
					}

					return false;
			}

			return null;
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			switch (dataType.DataType)
			{
				case DataType.Udt        :
					{
						if (parameter.GetType() == _parameterType
							&& value != null
							&& _udtTypes.TryGetValue(value.GetType(), out var s))
							_setUdtTypeName(parameter, s);
					}

					break;
				case DataType.NText:
					     if (value is DateTimeOffset dto) value = dto.ToString("yyyy-MM-ddTHH:mm:ss.ffffff zzz");
					else if (value is DateTime dt)
					{
						value = dt.ToString(
							dt.Millisecond == 0
								? "yyyy-MM-ddTHH:mm:ss"
								: "yyyy-MM-ddTHH:mm:ss.fff");
					}
					else if (value is TimeSpan ts)
					{
						value = ts.ToString(
							ts.Days > 0
								? ts.Milliseconds > 0
									? "d\\.hh\\:mm\\:ss\\.fff"
									: "d\\.hh\\:mm\\:ss"
								: ts.Milliseconds > 0
									? "hh\\:mm\\:ss\\.fff"
									: "hh\\:mm\\:ss");
					}
					break;

				case DataType.Undefined:
					if (value != null
						&& (value is DataTable
						|| value is DbDataReader
							|| value is IEnumerable<DbDataRecord>
							|| value.GetType().IsEnumerableTType(_sqlDataRecordType)))
					{
						dataType = dataType.WithDataType(DataType.Structured);
					}

					break;
			}

			base.SetParameter(parameter, name, dataType, value);

			if (parameter.GetType() == _parameterType)
			{
				// Setting for NVarChar and VarChar constant size. It reduces count of cached plans.
				switch (_getSqlDbType(parameter))
				{
					case SqlDbType.Structured:
						{
							if (!dataType.DbType.IsNullOrEmpty())
								_setTypeName(parameter, dataType.DbType);

							// TVP doesn't support DBNull
							if (parameter.Value is DBNull)
								parameter.Value = null;

							break;
						}
					case SqlDbType.VarChar:
						{
							var strValue = value as string;
							if ((strValue != null && strValue.Length > 8000) || (value != null && strValue == null))
								parameter.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 8000 && (strValue == null || strValue.Length <= dataType.Length))
								parameter.Size = dataType.Length.Value;
							else
								parameter.Size = 8000;

							break;
						}
					case SqlDbType.NVarChar:
						{
							var strValue = value as string;
							if ((strValue != null && strValue.Length > 4000) || (value != null && strValue == null))
								parameter.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 4000 && (strValue == null || strValue.Length <= dataType.Length))
								parameter.Size = dataType.Length.Value;
							else
								parameter.Size = 4000;

							break;
						}
					case SqlDbType.VarBinary:
						{
							var binaryValue = value as byte[];
							if ((binaryValue != null && binaryValue.Length > 8000) || (value != null && binaryValue == null))
								parameter.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 8000 && (binaryValue == null || binaryValue.Length <= dataType.Length))
								parameter.Size = dataType.Length.Value;
							else
								parameter.Size = 8000;

							break;
						}
				}
			}
		}

		protected override void SetParameterType(IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			switch (dataType.DataType)
			{
				case DataType.SByte         : parameter.DbType = DbType.Int16;   break;
				case DataType.UInt16        : parameter.DbType = DbType.Int32;   break;
				case DataType.UInt32        : parameter.DbType = DbType.Int64;   break;
				case DataType.UInt64        : parameter.DbType = DbType.Decimal; break;
				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal; break;
				case DataType.DateTime      :
				case DataType.DateTime2     :
					parameter.DbType =
						Version == SqlServerVersion.v2000 || Version == SqlServerVersion.v2005 ?
							DbType.DateTime :
							DbType.DateTime2;
					break;
				case DataType.Text          : _setSqlDbType(parameter, SqlDbType.Text);          break;
				case DataType.NText         : _setSqlDbType(parameter, SqlDbType.NText);         break;
				case DataType.Binary        : _setSqlDbType(parameter, SqlDbType.Binary);        break;
				case DataType.Blob          :
				case DataType.VarBinary     : _setSqlDbType(parameter, SqlDbType.VarBinary);     break;
				case DataType.Image         : _setSqlDbType(parameter, SqlDbType.Image);         break;
				case DataType.Money         : _setSqlDbType(parameter, SqlDbType.Money);         break;
				case DataType.SmallMoney    : _setSqlDbType(parameter, SqlDbType.SmallMoney);    break;
				case DataType.Date          : _setSqlDbType(parameter, SqlDbType.Date);          break;
				case DataType.Time          : _setSqlDbType(parameter, SqlDbType.Time);          break;
				case DataType.SmallDateTime : _setSqlDbType(parameter, SqlDbType.SmallDateTime); break;
				case DataType.Timestamp     : _setSqlDbType(parameter, SqlDbType.Timestamp);     break;
				case DataType.Xml           : _setSqlDbType(parameter, SqlDbType.Xml);           break;
				case DataType.Structured    : _setSqlDbType(parameter, SqlDbType.Structured);    break;
				default                     : base.SetParameterType(parameter, dataType);                    break;
			}
		}

		#endregion

		#region Udt support

		static readonly ConcurrentDictionary<Type,string> _udtTypes = new ConcurrentDictionary<Type,string>();

		internal static void SetUdtType(Type type, string udtName)
		{
			_udtTypes[type] = udtName;
		}

		internal static Type GetUdtType(string udtName)
		{
			foreach (var udtType in _udtTypes)
				if (udtType.Value == udtName)
					return udtType.Key;

			return null;
		}

		public void AddUdtType(Type type, string udtName)
		{
			MappingSchema.SetScalarType(type);

			_udtTypes[type] = udtName;
		}

		public void AddUdtType<T>(string udtName, T defaultValue, DataType dataType = DataType.Undefined)
		{
			MappingSchema.AddScalarType(typeof(T), defaultValue, dataType);

			_udtTypes[typeof(T)] = udtName;
		}

		#endregion

		#region BulkCopy

		SqlServerBulkCopy _bulkCopy;

		public override BulkCopyRowsCopied BulkCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (_bulkCopy == null)
				_bulkCopy = new SqlServerBulkCopy(this, GetConnectionType());

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SqlServerTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion
	}
}
