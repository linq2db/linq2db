using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;

namespace LinqToDB.Remote.Grpc.Dto
{
	[DataContract]
	public class GrpcSqlProviderFlags
	{
		[DataMember(Order = 1)]
		public bool IsSybaseBuggyGroupBy
		{
			get; set;
		}

		[DataMember(Order = 2)]
		public bool IsParameterOrderDependent
		{
			get; set;
		}

		[DataMember(Order = 3)]
		public bool AcceptsTakeAsParameter
		{
			get; set;
		}

		[DataMember(Order = 4)]
		public bool AcceptsTakeAsParameterIfSkip
		{
			get; set;
		}

		[DataMember(Order = 5)]
		public bool IsTakeSupported
		{
			get; set;
		}

		[DataMember(Order = 6)]
		public bool IsSkipSupported
		{
			get; set;
		}

		[DataMember(Order = 7)]
		public bool IsSkipSupportedIfTake
		{
			get; set;
		}

		[DataMember(Order = 8)]
		public TakeHints? TakeHintsSupported
		{
			get; set;
		}

		[DataMember(Order = 9)]
		public bool IsSubQueryTakeSupported
		{
			get; set;
		}

		[DataMember(Order = 10)]
		public bool IsSubQueryColumnSupported
		{
			get; set;
		}

		[DataMember(Order = 11)]
		public bool IsSubQueryOrderBySupported
		{
			get; set;
		}

		[DataMember(Order = 12)]
		public bool IsCountSubQuerySupported
		{
			get; set;
		}

		[DataMember(Order = 13)]
		public bool IsIdentityParameterRequired
		{
			get; set;
		}

		[DataMember(Order = 14)]
		public bool IsApplyJoinSupported
		{
			get; set;
		}

		[DataMember(Order = 15)]
		public bool IsInsertOrUpdateSupported
		{
			get; set;
		}

		[DataMember(Order = 16)]
		public bool CanCombineParameters
		{
			get; set;
		}

		[DataMember(Order = 17)]
		public bool IsGroupByExpressionSupported
		{
			get; set;
		}

		[DataMember(Order = 18)]
		public int MaxInListValuesCount
		{
			get; set;
		}

		[DataMember(Order = 19)]
		public bool IsUpdateSetTableAliasSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider requires that selected subquery column must be used in group by even for constant column.
		/// </summary>
		[DataMember(Order = 20)]
		public bool IsGroupByColumnRequred
		{
			get; set;
		}

		/// <summary>
		/// Provider supports:
		/// CROSS JOIN a Supported
		/// </summary>
		[DataMember(Order = 21)]
		public bool IsCrossJoinSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports:
		/// INNER JOIN a ON 1 = 1 
		/// </summary>
		[DataMember(Order = 22)]
		public bool IsInnerJoinAsCrossSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports CTE expressions.
		/// If provider does not support CTE, unsuported exception will be thrown when using CTE.
		/// </summary>
		[DataMember(Order = 23)]
		public bool IsCommonTableExpressionsSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports DISTINCT and ORDER BY with fields that are not in projection.
		/// </summary>
		[DataMember(Order = 24)]
		public bool IsDistinctOrderBySupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports aggregate functions in ORDER BY statement.
		/// </summary>
		[DataMember(Order = 25)]
		public bool IsOrderByAggregateFunctionsSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports EXCEPT ALL, INTERSECT ALL set operators. Otherwise it will be emulated.
		/// </summary>
		[DataMember(Order = 26)]
		public bool IsAllSetOperationsSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports EXCEPT, INTERSECT set operators. Otherwise it will be emulated.
		/// </summary>
		[DataMember(Order = 27)]
		public bool IsDistinctSetOperationsSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports COUNT(DISTINCT column) function. Otherwise it will be emulated.
		/// </summary>
		[DataMember(Order = 28)]
		public bool IsCountDistinctSupported
		{
			get; set;
		}

		/// <summary>
		/// Provider supports aggregated expression with Outer reference
		/// <code>
		/// SELECT
		/// (
		///		SELECT SUM(inner.FieldX + outer.FieldOuter)
		///		FROM table2 inner
		/// ) AS Sum_Column
		/// FROM table1 outer
		///</code>
		/// Otherwise aggeragated expression will be wrapped in subquery and aggregate function will be applied to subquery column.
		/// <code>
		/// SELECT
		/// (
		///		SELECT
		///			SUM(sub.Column)
		///		FROM 
		///			(
		///				SELECT inner.FieldX + outer.FieldOuter AS Column
		///				FROM table2 inner
		///			) sub
		/// ) AS Sum_Column
		/// FROM table1 outer
		///</code>
		/// </summary>
		[DataMember(Order = 29)]
		public bool AcceptsOuterExpressionInAggregate
		{
			get; set;
		}

		/// <summary>
		/// Provider supports
		/// <code>
		/// UPDATE A
		/// SET ...
		/// FROM B
		/// </code> syntax
		/// </summary>
		[DataMember(Order = 30)]
		public bool IsUpdateFromSupported
		{
			get; set;
		}

		/// <summary>
		/// Used when there is query which needs several additional database request for completing query.
		/// Default is <see cref="IsolationLevel.RepeatableRead"/>
		/// </summary>
		[DataMember(Order = 31)]
		public IsolationLevel DefaultMultiQueryIsolationLevel
		{
			get; set;
		} = IsolationLevel.RepeatableRead;

		/// <summary>
		/// Flags for use by external providers.
		/// </summary>
		[DataMember(Order = 32)]
		public List<string> CustomFlags
		{
			get; set;
		} = new List<string>();
	}

}
