using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using OleDbType = LinqToDB.DataProvider.OleDbProviderAdapter.OleDbType;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using Linq.Translation;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;
	using Translation;

	public class AccessOleDbDataProvider : DynamicDataProviderBase<OleDbProviderAdapter>
	{
		public AccessOleDbDataProvider() : base(ProviderName.Access, MappingSchemaInstance, OleDbProviderAdapter.GetInstance())
		{
			SqlProviderFlags.AcceptsTakeAsParameter                   = false;
			SqlProviderFlags.IsSkipSupported                          = false;
			SqlProviderFlags.IsInsertOrUpdateSupported                = false;
			SqlProviderFlags.IsSubQuerySkipSupported                  = false;
			SqlProviderFlags.IsSubQueryOrderBySupported               = false;
			SqlProviderFlags.IsSupportsJoinWithoutCondition           = false;
			SqlProviderFlags.TakeHintsSupported                       = TakeHints.Percent;
			SqlProviderFlags.IsCrossJoinSupported                     = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported         = false;
			SqlProviderFlags.IsParameterOrderDependent                = true;
			SqlProviderFlags.IsUpdateFromSupported                    = false;
			SqlProviderFlags.IsWindowFunctionsSupported               = false;
			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel       = 1;
			SqlProviderFlags.DefaultMultiQueryIsolationLevel          = IsolationLevel.Unspecified;
			SqlProviderFlags.IsOuterJoinSupportsInnerJoin             = false;
			SqlProviderFlags.IsMultiTablesSupportsJoins               = false;
			SqlProviderFlags.IsAccessBuggyLeftJoinConstantNullability = true;

			SqlProviderFlags.IsCountDistinctSupported                     = false;
			SqlProviderFlags.IsAggregationDistinctSupported               = false;

			SetCharField("DBTYPE_WCHAR", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("DBTYPE_WCHAR", DataTools.GetCharExpression);

			SetProviderField<DbDataReader, TimeSpan, DateTime>((r, i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));

			_sqlOptimizer = new AccessSqlOptimizer(SqlProviderFlags);
		}

		public override TableOptions SupportedTableOptions => TableOptions.None;

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new AccessMemberTranslator();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new AccessOleDbSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new AccessOleDbSchemaProvider(this);
		}

#if NET6_0_OR_GREATER
		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is DateOnly d)
				value = d.ToDateTime(TimeOnly.MinValue);

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}
#endif

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			OleDbType? type = null;
			switch (dataType.DataType)
			{
				case DataType.DateTime  :
				case DataType.DateTime2 : type = OleDbType.Date        ; break;
				case DataType.Text      : type = OleDbType.LongVarChar ; break;
				case DataType.NText     : type = OleDbType.LongVarWChar; break;
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
				// "Data type mismatch in criteria expression" fix for culture-aware number decimal separator
				// unfortunately, regular fix using ExecuteScope=>InvariantCultureRegion
				// doesn't work for all situations
				case DataType.Decimal   :
				case DataType.VarNumeric: parameter.DbType = DbType.AnsiString; return;
				case DataType.DateTime  :
				case DataType.DateTime2 : parameter.DbType = DbType.DateTime;   return;
				case DataType.Text      : parameter.DbType = DbType.AnsiString; return;
				case DataType.NText     : parameter.DbType = DbType.String;     return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		static readonly MappingSchema MappingSchemaInstance = new AccessMappingSchema.OleDbMappingSchema();

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new AccessBulkCopy().BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(AccessOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(AccessOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(AccessOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion
	}
}
