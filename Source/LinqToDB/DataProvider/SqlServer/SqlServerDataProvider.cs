﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.SqlServer.Server;

namespace LinqToDB.DataProvider.SqlServer
{
	using Configuration;
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SqlServerDataProvider : DataProviderBase
	{
		#region Init

		public SqlServerDataProvider(string name, SqlServerVersion version)
			: base(name, (MappingSchema)null)
		{
			Version = version;

			SqlProviderFlags.IsDistinctOrderBySupported = false;
			SqlProviderFlags.IsSubQueryOrderBySupported = false;

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

			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetProviderField<SqlDataReader,SqlBinary  ,SqlBinary  >((r,i) => r.GetSqlBinary  (i));
				SetProviderField<SqlDataReader,SqlBoolean ,SqlBoolean >((r,i) => r.GetSqlBoolean (i));
				SetProviderField<SqlDataReader,SqlByte    ,SqlByte    >((r,i) => r.GetSqlByte    (i));
				SetProviderField<SqlDataReader,SqlDateTime,SqlDateTime>((r,i) => r.GetSqlDateTime(i));
				SetProviderField<SqlDataReader,SqlDecimal ,SqlDecimal >((r,i) => r.GetSqlDecimal (i));
				SetProviderField<SqlDataReader,SqlDouble  ,SqlDouble  >((r,i) => r.GetSqlDouble  (i));
				SetProviderField<SqlDataReader,SqlGuid    ,SqlGuid    >((r,i) => r.GetSqlGuid    (i));
				SetProviderField<SqlDataReader,SqlInt16   ,SqlInt16   >((r,i) => r.GetSqlInt16   (i));
				SetProviderField<SqlDataReader,SqlInt32   ,SqlInt32   >((r,i) => r.GetSqlInt32   (i));
				SetProviderField<SqlDataReader,SqlInt64   ,SqlInt64   >((r,i) => r.GetSqlInt64   (i));
				SetProviderField<SqlDataReader,SqlMoney   ,SqlMoney   >((r,i) => r.GetSqlMoney   (i));
				SetProviderField<SqlDataReader,SqlSingle  ,SqlSingle  >((r,i) => r.GetSqlSingle  (i));
				SetProviderField<SqlDataReader,SqlString  ,SqlString  >((r,i) => r.GetSqlString  (i));
				SetProviderField<SqlDataReader,SqlXml     ,SqlXml     >((r,i) => r.GetSqlXml     (i));

				SetProviderField<SqlDataReader,DateTimeOffset>((r,i) => r.GetDateTimeOffset(i));
				SetProviderField<SqlDataReader,TimeSpan>      ((r,i) => r.GetTimeSpan      (i));
			}
			else
			{
				SetProviderField<IDataReader,SqlString  ,SqlString  >((r,i) => r.GetString  (i));
			}

			_sqlServer2000SqlOptimizer = new SqlServer2000SqlOptimizer(SqlProviderFlags);
			_sqlServer2005SqlOptimizer = new SqlServer2005SqlOptimizer(SqlProviderFlags);
			_sqlServer2008SqlOptimizer = new SqlServerSqlOptimizer    (SqlProviderFlags);
			_sqlServer2012SqlOptimizer = new SqlServer2012SqlOptimizer(SqlProviderFlags);

			SetField<IDataReader,decimal>((r,i) => r.GetDecimal(i));
			SetField<IDataReader,decimal>("money",      (r,i) => SqlServerTools.DataReaderGetMoney  (r, i));
			SetField<IDataReader,decimal>("smallmoney", (r,i) => SqlServerTools.DataReaderGetMoney  (r, i));
			SetField<IDataReader,decimal>("decimal",    (r,i) => SqlServerTools.DataReaderGetDecimal(r, i));
		}

		#endregion

		#region Public Properties

		public override string ConnectionNamespace => typeof(SqlConnection).Namespace;
		public override Type   DataReaderType      => typeof(SqlDataReader);

		public SqlServerVersion Version { get; }

		#endregion

		#region Overrides

		static class MappingSchemaInstance
		{
			public static readonly SqlServer2000MappingSchema SqlServer2000MappingSchema = new SqlServer2000MappingSchema();
			public static readonly SqlServer2005MappingSchema SqlServer2005MappingSchema = new SqlServer2005MappingSchema();
			public static readonly SqlServer2008MappingSchema SqlServer2008MappingSchema = new SqlServer2008MappingSchema();
			public static readonly SqlServer2012MappingSchema SqlServer2012MappingSchema = new SqlServer2012MappingSchema();
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
				}

				return base.MappingSchema;
			}
		}

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			return new SqlConnection(connectionString);
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			switch (Version)
			{
				case SqlServerVersion.v2000 : return new SqlServer2000SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
				case SqlServerVersion.v2005 : return new SqlServer2005SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
				case SqlServerVersion.v2008 : return new SqlServer2008SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
				case SqlServerVersion.v2012 : return new SqlServer2012SqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
			}

			throw new InvalidOperationException();
		}

		readonly ISqlOptimizer _sqlServer2000SqlOptimizer;
		readonly ISqlOptimizer _sqlServer2005SqlOptimizer;
		readonly ISqlOptimizer _sqlServer2008SqlOptimizer;
		readonly ISqlOptimizer _sqlServer2012SqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			switch (Version)
			{
				case SqlServerVersion.v2000 : return _sqlServer2000SqlOptimizer;
				case SqlServerVersion.v2005 : return _sqlServer2005SqlOptimizer;
				case SqlServerVersion.v2008 : return _sqlServer2008SqlOptimizer;
				case SqlServerVersion.v2012 : return _sqlServer2012SqlOptimizer;
			}

			return _sqlServer2008SqlOptimizer;
		}

		public override bool IsCompatibleConnection(IDbConnection connection)
		{
			return typeof(SqlConnection).IsSameOrParentOf(Proxy.GetUnderlyingObject((DbConnection)connection).GetType());
		}

#if !NETSTANDARD1_6
		public override ISchemaProvider GetSchemaProvider()
		{
			return Version == SqlServerVersion.v2000 ? new SqlServer2000SchemaProvider() : new SqlServerSchemaProvider();
		}
#endif

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
						if (value != null && _udtTypes.TryGetValue(value.GetType(), out var s))
							if (parameter is SqlParameter)
#if NETSTANDARD1_6
								((SqlParameter)parameter).TypeName = s;
#else
								((SqlParameter)parameter).UdtTypeName = s;
#endif
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
					if (value is DataTable
						|| value is DbDataReader
						|| value is IEnumerable<SqlDataRecord>
						|| value is IEnumerable<DbDataRecord>)
					{
						dataType = dataType.WithDataType(DataType.Structured);
					}

					break;
			}

			base.SetParameter(parameter, name, dataType, value);

			if (parameter is SqlParameter param)
			{
				// Setting for NVarChar and VarChar constant size. It reduces count of cached plans.
				switch (param.SqlDbType)
				{
					case SqlDbType.Structured:
						{
							if (!dataType.DbType.IsNullOrEmpty())
								param.TypeName = dataType.DbType;

							// TVP doesn't support DBNull
							if (param.Value is DBNull)
								param.Value = null;

							break;
						}
					case SqlDbType.VarChar:
						{
							var strValue = value as string;
							if ((strValue != null && strValue.Length > 8000) || (value != null && strValue == null))
								param.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 8000 && (strValue == null || strValue.Length <= dataType.Length))
								param.Size = dataType.Length.Value;
							else
								param.Size = 8000;

							break;
						}
					case SqlDbType.NVarChar:
						{
							var strValue = value as string;
							if ((strValue != null && strValue.Length > 4000) || (value != null && strValue == null))
								param.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 4000 && (strValue == null || strValue.Length <= dataType.Length))
								param.Size = dataType.Length.Value;
							else
								param.Size = 4000;

							break;
						}
					case SqlDbType.VarBinary:
						{
							var binaryValue = value as byte[];
							if ((binaryValue != null && binaryValue.Length > 8000) || (value != null && binaryValue == null))
								param.Size = -1;
							else if (dataType.Length != null && dataType.Length <= 8000 && (binaryValue == null || binaryValue.Length <= dataType.Length))
								param.Size = dataType.Length.Value;
							else
								param.Size = 8000;

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
				case DataType.DateTime2     :
					parameter.DbType =
						Version == SqlServerVersion.v2000 || Version == SqlServerVersion.v2005 ?
							DbType.DateTime :
							DbType.DateTime2;
					break;
				case DataType.Text          : ((SqlParameter)parameter).SqlDbType = SqlDbType.Text;          break;
				case DataType.NText         : ((SqlParameter)parameter).SqlDbType = SqlDbType.NText;         break;
				case DataType.Binary        : ((SqlParameter)parameter).SqlDbType = SqlDbType.Binary;        break;
				case DataType.Blob          :
				case DataType.VarBinary     : ((SqlParameter)parameter).SqlDbType = SqlDbType.VarBinary;     break;
				case DataType.Image         : ((SqlParameter)parameter).SqlDbType = SqlDbType.Image;         break;
				case DataType.Money         : ((SqlParameter)parameter).SqlDbType = SqlDbType.Money;         break;
				case DataType.SmallMoney    : ((SqlParameter)parameter).SqlDbType = SqlDbType.SmallMoney;    break;
				case DataType.Date          : ((SqlParameter)parameter).SqlDbType = SqlDbType.Date;          break;
				case DataType.Time          : ((SqlParameter)parameter).SqlDbType = SqlDbType.Time;          break;
				case DataType.SmallDateTime : ((SqlParameter)parameter).SqlDbType = SqlDbType.SmallDateTime; break;
				case DataType.Timestamp     : ((SqlParameter)parameter).SqlDbType = SqlDbType.Timestamp;     break;
				case DataType.Xml           : ((SqlParameter)parameter).SqlDbType = SqlDbType.Xml;           break;
				case DataType.Structured    : ((SqlParameter)parameter).SqlDbType = SqlDbType.Structured;    break;
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
				_bulkCopy = new SqlServerBulkCopy(this);

			return _bulkCopy.BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SqlServerTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion

		#region Merge

		public override int Merge<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
		{
			return new SqlServerMerge().Merge(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName);
		}

		public override Task<int> MergeAsync<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName, CancellationToken token)
		{
			return new SqlServerMerge().MergeAsync(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName, token);
		}

		protected override BasicMergeBuilder<TTarget, TSource> GetMergeBuilder<TTarget, TSource>(
			DataConnection connection,
			IMergeable<TTarget, TSource> merge)
		{
			return new SqlServerMergeBuilder<TTarget, TSource>(connection, merge);
		}

		#endregion
	}
}
