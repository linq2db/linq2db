using System.Runtime.Serialization;
using LinqToDB.SqlProvider;


namespace LinqToDB.Remote.Grpc.Dto
{
	[DataContract]
	public class GrpcLinqServiceInfo
	{
		[DataMember(Order = 1)] 
		public string MappingSchemaType { get; set; } = null!;

		[DataMember(Order = 2)]
		public string SqlBuilderType { get; set; } = null!;

		[DataMember(Order = 3)]
		public string SqlOptimizerType { get; set; } = null!;

		[DataMember(Order = 4)]
		public GrpcSqlProviderFlags SqlProviderFlags { get; set; } = null!;

		[DataMember(Order = 5)]
		public TableOptions SupportedTableOptions { get; set; }

		public static implicit operator LinqServiceInfo(GrpcLinqServiceInfo glsi)
		{
			var spf = new SqlProviderFlags
			{
				AcceptsOuterExpressionInAggregate = glsi.SqlProviderFlags.AcceptsOuterExpressionInAggregate,
				AcceptsTakeAsParameter = glsi.SqlProviderFlags.AcceptsTakeAsParameter,
				AcceptsTakeAsParameterIfSkip = glsi.SqlProviderFlags.AcceptsTakeAsParameterIfSkip,
				CanCombineParameters = glsi.SqlProviderFlags.CanCombineParameters,
				DefaultMultiQueryIsolationLevel = glsi.SqlProviderFlags.DefaultMultiQueryIsolationLevel,
				IsAllSetOperationsSupported = glsi.SqlProviderFlags.IsAllSetOperationsSupported,
				IsApplyJoinSupported = glsi.SqlProviderFlags.IsApplyJoinSupported,
				IsCommonTableExpressionsSupported = glsi.SqlProviderFlags.IsCommonTableExpressionsSupported,
				IsCountDistinctSupported = glsi.SqlProviderFlags.IsCountDistinctSupported,
				IsCountSubQuerySupported = glsi.SqlProviderFlags.IsCountSubQuerySupported,
				IsCrossJoinSupported = glsi.SqlProviderFlags.IsCrossJoinSupported,
				IsDistinctOrderBySupported = glsi.SqlProviderFlags.IsDistinctOrderBySupported,
				IsDistinctSetOperationsSupported = glsi.SqlProviderFlags.IsDistinctSetOperationsSupported,
				IsInsertOrUpdateSupported = glsi.SqlProviderFlags.IsInsertOrUpdateSupported,
				IsInnerJoinAsCrossSupported = glsi.SqlProviderFlags.IsInnerJoinAsCrossSupported,
				IsIdentityParameterRequired = glsi.SqlProviderFlags.IsIdentityParameterRequired,
				IsGroupByColumnRequred = glsi.SqlProviderFlags.IsGroupByColumnRequred,
				IsGroupByExpressionSupported = glsi.SqlProviderFlags.IsGroupByExpressionSupported,
				IsOrderByAggregateFunctionsSupported = glsi.SqlProviderFlags.IsOrderByAggregateFunctionsSupported,
				IsParameterOrderDependent = glsi.SqlProviderFlags.IsParameterOrderDependent,
				IsSkipSupported = glsi.SqlProviderFlags.IsSkipSupported,
				IsTakeSupported = glsi.SqlProviderFlags.IsTakeSupported,
				IsSkipSupportedIfTake = glsi.SqlProviderFlags.IsSkipSupportedIfTake,
				IsSubQueryColumnSupported = glsi.SqlProviderFlags.IsSubQueryColumnSupported,
				IsSubQueryOrderBySupported = glsi.SqlProviderFlags.IsSubQueryOrderBySupported,
				IsSubQueryTakeSupported = glsi.SqlProviderFlags.IsSubQueryTakeSupported,
				IsSybaseBuggyGroupBy = glsi.SqlProviderFlags.IsSybaseBuggyGroupBy,
				IsUpdateFromSupported = glsi.SqlProviderFlags.IsUpdateFromSupported,
				IsUpdateSetTableAliasSupported = glsi.SqlProviderFlags.IsUpdateSetTableAliasSupported,
				MaxInListValuesCount = glsi.SqlProviderFlags.MaxInListValuesCount,
				TakeHintsSupported = glsi.SqlProviderFlags.TakeHintsSupported
			};
			spf.CustomFlags.AddRange(glsi.SqlProviderFlags.CustomFlags);

			var result = new LinqServiceInfo
			{
				MappingSchemaType = glsi.MappingSchemaType,
				SqlBuilderType = glsi.SqlBuilderType,
				SqlOptimizerType = glsi.SqlOptimizerType,
				SqlProviderFlags = spf,
				SupportedTableOptions = glsi.SupportedTableOptions
			};

			return result;
		}

		public static implicit operator GrpcLinqServiceInfo(LinqServiceInfo lsi)
		{
			var glsi = new GrpcLinqServiceInfo
			{
				MappingSchemaType = lsi.MappingSchemaType,
				SqlBuilderType = lsi.SqlBuilderType,
				SqlOptimizerType = lsi.SqlOptimizerType,
				SqlProviderFlags = new GrpcSqlProviderFlags
				{
					AcceptsOuterExpressionInAggregate = lsi.SqlProviderFlags.AcceptsOuterExpressionInAggregate,
					AcceptsTakeAsParameter = lsi.SqlProviderFlags.AcceptsTakeAsParameter,
					AcceptsTakeAsParameterIfSkip = lsi.SqlProviderFlags.AcceptsTakeAsParameterIfSkip,
					CanCombineParameters = lsi.SqlProviderFlags.CanCombineParameters,
					CustomFlags = lsi.SqlProviderFlags.CustomFlags,
					DefaultMultiQueryIsolationLevel = lsi.SqlProviderFlags.DefaultMultiQueryIsolationLevel,
					IsAllSetOperationsSupported = lsi.SqlProviderFlags.IsAllSetOperationsSupported,
					IsApplyJoinSupported = lsi.SqlProviderFlags.IsApplyJoinSupported,
					IsCommonTableExpressionsSupported = lsi.SqlProviderFlags.IsCommonTableExpressionsSupported,
					IsCountDistinctSupported = lsi.SqlProviderFlags.IsCountDistinctSupported,
					IsCountSubQuerySupported = lsi.SqlProviderFlags.IsCountSubQuerySupported,
					IsCrossJoinSupported = lsi.SqlProviderFlags.IsCrossJoinSupported,
					IsDistinctOrderBySupported = lsi.SqlProviderFlags.IsDistinctOrderBySupported,
					IsDistinctSetOperationsSupported = lsi.SqlProviderFlags.IsDistinctSetOperationsSupported,
					IsInsertOrUpdateSupported = lsi.SqlProviderFlags.IsInsertOrUpdateSupported,
					IsInnerJoinAsCrossSupported = lsi.SqlProviderFlags.IsInnerJoinAsCrossSupported,
					IsIdentityParameterRequired = lsi.SqlProviderFlags.IsIdentityParameterRequired,
					IsGroupByColumnRequred = lsi.SqlProviderFlags.IsGroupByColumnRequred,
					IsGroupByExpressionSupported = lsi.SqlProviderFlags.IsGroupByExpressionSupported,
					IsOrderByAggregateFunctionsSupported = lsi.SqlProviderFlags.IsOrderByAggregateFunctionsSupported,
					IsParameterOrderDependent = lsi.SqlProviderFlags.IsParameterOrderDependent,
					IsSkipSupported = lsi.SqlProviderFlags.IsSkipSupported,
					IsTakeSupported = lsi.SqlProviderFlags.IsTakeSupported,
					IsSkipSupportedIfTake = lsi.SqlProviderFlags.IsSkipSupportedIfTake,
					IsSubQueryColumnSupported = lsi.SqlProviderFlags.IsSubQueryColumnSupported,
					IsSubQueryOrderBySupported = lsi.SqlProviderFlags.IsSubQueryOrderBySupported,
					IsSubQueryTakeSupported = lsi.SqlProviderFlags.IsSubQueryTakeSupported,
					IsSybaseBuggyGroupBy = lsi.SqlProviderFlags.IsSybaseBuggyGroupBy,
					IsUpdateFromSupported = lsi.SqlProviderFlags.IsUpdateFromSupported,
					IsUpdateSetTableAliasSupported = lsi.SqlProviderFlags.IsUpdateSetTableAliasSupported,
					MaxInListValuesCount = lsi.SqlProviderFlags.MaxInListValuesCount,
					TakeHintsSupported = lsi.SqlProviderFlags.TakeHintsSupported
				},
				SupportedTableOptions = lsi.SupportedTableOptions
			};

			return glsi;
		}
	}

}
