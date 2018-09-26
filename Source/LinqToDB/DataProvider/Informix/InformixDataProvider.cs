using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;

	public class InformixDataProvider : DynamicDataProviderBase
	{
		public InformixDataProvider()
			: this(ProviderName.Informix, new InformixMappingSchema())
		{
		}

		protected InformixDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsParameterOrderDependent         = true;
			SqlProviderFlags.IsSubQueryTakeSupported           = false;
			SqlProviderFlags.IsInsertOrUpdateSupported         = false;
			SqlProviderFlags.IsGroupByExpressionSupported      = false;
			SqlProviderFlags.IsCrossJoinSupported              = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;


			SetCharField("CHAR",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("NCHAR", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("CHAR",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("NCHAR", (r, i) => DataTools.GetChar(r, i));

			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetProviderField<IDataReader,float,  float  >((r,i) => GetFloat  (r, i));
				SetProviderField<IDataReader,double, double >((r,i) => GetDouble (r, i));
				SetProviderField<IDataReader,decimal,decimal>((r,i) => GetDecimal(r, i));

				SetField<IDataReader, float  >((r, i) => GetFloat  (r, i));
				SetField<IDataReader, double >((r, i) => GetDouble (r, i));
				SetField<IDataReader, decimal>((r, i) => GetDecimal(r, i));
			}

			_sqlOptimizer = new InformixSqlOptimizer(SqlProviderFlags);
		}

		static float GetFloat(IDataReader dr, int idx)
		{
			using (new InvariantCultureRegion())
				return dr.GetFloat(idx);
		}

		static double GetDouble(IDataReader dr, int idx)
		{
			using (new InvariantCultureRegion())
				return dr.GetDouble(idx);
		}

		static decimal GetDecimal(IDataReader dr, int idx)
		{
			using (new InvariantCultureRegion())
				return dr.GetDecimal(idx);
		}

		Type _ifxBlob;
		Type _ifxClob;
		Type _ifxDecimal;
		Type _ifxDateTime;
		Type _ifxTimeSpan;

		public override IDisposable ExecuteScope()
		{
			return new InvariantCultureRegion();
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_ifxBlob     = connectionType.AssemblyEx().GetType("IBM.Data.Informix.IfxBlob",     true);
			_ifxClob     = connectionType.AssemblyEx().GetType("IBM.Data.Informix.IfxClob",     true);
			_ifxDecimal  = connectionType.AssemblyEx().GetType("IBM.Data.Informix.IfxDecimal",  true);
			_ifxDateTime = connectionType.AssemblyEx().GetType("IBM.Data.Informix.IfxDateTime", true);
			_ifxTimeSpan = connectionType.AssemblyEx().GetType("IBM.Data.Informix.IfxTimeSpan", true);

			if (!Configuration.AvoidSpecificDataProviderAPI)
			{
				SetField(typeof(Int64), "BIGINT", "GetBigInt");

				SetProviderField(_ifxDecimal,  typeof(decimal),  "GetIfxDecimal");
				SetProviderField(_ifxDateTime, typeof(DateTime), "GetIfxDateTime");
				SetProviderField(_ifxTimeSpan, typeof(TimeSpan), "GetIfxTimeSpan");
			}

			var p = Expression.Parameter(typeof(TimeSpan));

			_newIfxTimeSpan = Expression.Lambda<Func<TimeSpan,object>>(
				Expression.Convert(
					Expression.New(_ifxTimeSpan.GetConstructorEx(new[] { typeof(TimeSpan) }), p),
					typeof(object)),
				p).Compile();

			_setText = GetSetParameter(connectionType, "IfxParameter", "IfxType", "IfxType", "Clob");

			MappingSchema.AddScalarType(_ifxBlob,     GetNullValue(_ifxBlob),     true, DataType.VarBinary);
			MappingSchema.AddScalarType(_ifxClob,     GetNullValue(_ifxClob),     true, DataType.Text);
			MappingSchema.AddScalarType(_ifxDateTime, GetNullValue(_ifxDateTime), true, DataType.DateTime2);
			MappingSchema.AddScalarType(_ifxDecimal,  GetNullValue(_ifxDecimal),  true, DataType.Decimal);
			MappingSchema.AddScalarType(_ifxTimeSpan, GetNullValue(_ifxTimeSpan), true, DataType.Time);
			//AddScalarType(typeof(IfxMonthSpan),   IfxMonthSpan.  Null, DataType.Time);
		}

		static object GetNullValue(Type type)
		{
			try
			{
				var getValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object)));
				return getValue.Compile()();
			}
			catch (SecurityException)
			{
				return null;
			}
		}

		public    override string ConnectionNamespace => "IBM.Data.Informix";
		protected override string ConnectionTypeName  => "IBM.Data.Informix.IfxConnection, IBM.Data.Informix";
		protected override string DataReaderTypeName  => "IBM.Data.Informix.IfxDataReader, IBM.Data.Informix";

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new InformixSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD1_6
		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new InformixSchemaProvider();
		}
#endif

		Func<TimeSpan,object> _newIfxTimeSpan;

		public override void SetParameter(IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			if (value is TimeSpan)
			{
				if (dataType.DataType != DataType.Int64)
					value = _newIfxTimeSpan((TimeSpan)value);
			}
			else if (value is Guid || value == null && dataType.DataType == DataType.Guid)
			{
				value    = value?.ToString();
				dataType = dataType.WithDataType(DataType.Char);
			}
			else if (value is bool)
			{
				value = (bool)value ? 't' : 'f';
				dataType = dataType.WithDataType(DataType.Char);
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		static Action<IDbDataParameter> _setText;

		protected override void SetParameterType(IDbDataParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.UInt16    : dataType = dataType.WithDataType(DataType.Int32);    break;
				case DataType.UInt32    : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64    : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.VarNumeric: dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2 : dataType = dataType.WithDataType(DataType.DateTime); break;
				case DataType.Text      : _setText(parameter); return;
				case DataType.NText     : _setText(parameter); return;
			}

			base.SetParameterType(parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new InformixBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? InformixTools.DefaultBulkCopyType : options.BulkCopyType,
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
				throw new LinqToDBException("Informix MERGE statement does not support DELETE by source.");

			return new InformixMerge().Merge(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName);
		}

		public override Task<int> MergeAsync<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source,
			string tableName, string databaseName, string schemaName, CancellationToken token)
		{
			if (delete)
				throw new LinqToDBException("Informix MERGE statement does not support DELETE by source.");

			return new InformixMerge().MergeAsync(dataConnection, deletePredicate, delete, source, tableName, databaseName, schemaName, token);
		}

		protected override BasicMergeBuilder<TTarget, TSource> GetMergeBuilder<TTarget, TSource>(
			DataConnection connection,
			IMergeable<TTarget, TSource> merge)
		{
			return new InformixMergeBuilder<TTarget, TSource>(connection, merge);
		}

		#endregion
	}
}
