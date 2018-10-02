using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Data;
	using LinqToDB.Extensions;
	using Mapping;
	using SqlProvider;

	public class FirebirdDataProvider : DynamicDataProviderBase
	{
		static FirebirdDataProvider()
		{
			var p = Expression.Parameter(typeof(string));
			var l = Expression.Lambda<Func<string, FirebirdDialect>>(
				Expression.Convert(
					Expression.Property(
						Expression.New(
							Type.GetType("FirebirdSql.Data.FirebirdClient.FbConnectionString, FirebirdSql.Data.FirebirdClient")
								.GetConstructorEx(new[] { typeof(string) }),
							p),
						"Dialect"),
					typeof(FirebirdDialect)),
				p);

			_getDialect = l.Compile();
		}

		public FirebirdDataProvider()
			: this(ProviderName.Firebird, new FirebirdMappingSchema(), null)
		{
		}

		public FirebirdDataProvider(ISqlOptimizer sqlOptimizer)
			: this(ProviderName.Firebird, new FirebirdMappingSchema(), sqlOptimizer)
		{
		}

		protected FirebirdDataProvider(string name, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsIdentityParameterRequired       = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR", (r, i) => DataTools.GetChar(r, i));

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));

			_sqlOptimizer = sqlOptimizer ?? new FirebirdSqlOptimizer(SqlProviderFlags);
		}

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1970 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		static Action<IDbDataParameter> _setTimeStamp;

		public    override string ConnectionNamespace => "FirebirdSql.Data.FirebirdClient";
		protected override string ConnectionTypeName  => $"{ConnectionNamespace}.FbConnection, {ConnectionNamespace}";
		protected override string DataReaderTypeName  => $"{ConnectionNamespace}.FbDataReader, {ConnectionNamespace}";

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			//                                             ((FbParameter)parameter).FbDbType =  FbDbType.   TimeStamp;
			_setTimeStamp = GetSetParameter(connectionType, "FbParameter",         "FbDbType", "FbDbType", "TimeStamp");
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new FirebirdSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		private static Func<string, FirebirdDialect> _getDialect;

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			SqlProviderFlags.SetDialect(_getDialect(connectionString));
			return base.CreateConnectionInternal(connectionString);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD1_6
		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new FirebirdSchemaProvider();
		}
#endif

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is bool)
			{
				value = (bool)value ? "1" : "0";
				dataType = DataType.Char;
			}

			//if (SqlProviderFlags.GetDialect() == FirebirdDialect.Dialect1
			//	&& (value is uint))
			//{
			//	value = Convert.ToInt32(value);
			//	dataType = DataType.Int32;
			//}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte      : dataType = DataType.Int16;   break;
				case DataType.UInt16     : dataType = DataType.Int32;   break;
				case DataType.UInt32     : dataType = DataType.Int64;   break;
				case DataType.UInt64     : dataType = DataType.Decimal; break;
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.DateTime   :
				case DataType.DateTime2  : _setTimeStamp(parameter);    return;
			}

			base.SetParameterType(parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new FirebirdBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? FirebirdTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

		#endregion

		#region Merge

		public override int Merge<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName)
		{
			if (delete)
				throw new LinqToDBException("Firebird MERGE statement does not support DELETE by source.");

			return new FirebirdMerge().Merge(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName);
		}

		public override Task<int> MergeAsync<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName, CancellationToken token)
		{
			if (delete)
				throw new LinqToDBException("Firebird MERGE statement does not support DELETE by source.");

			return new FirebirdMerge().MergeAsync(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName, token);
		}

		protected override BasicMergeBuilder<TTarget, TSource> GetMergeBuilder<TTarget, TSource>(
			DataConnection connection,
			IMergeable<TTarget, TSource> merge)
		{
			return new FirebirdMergeBuilder<TTarget, TSource>(connection, merge);
		}

		#endregion
	}
}
