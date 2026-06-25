namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public static class ErrorHelper
	{
		public const string Error_Correlated_Subqueries            = "Provider does not support correlated subqueries.";
		public const string Error_Correlated_Subqueries_Level      = "Provider does not support correlated subqueries in more than {0} level.";
		public const string Error_OUTER_Joins                      = "Provider does not support CROSS/OUTER/LATERAL joins.";
		public const string Error_Subquery_in_Column               = "Provider does not support columns with subqueries.";
		public const string Error_Take_in_Subquery                 = "Provider does not support Take value in subquery.";
		public const string Error_Take_in_Correlated_Subquery      = "Provider does not support Take value in correlated subquery.";
		public const string Error_Join_Without_Condition           = "Provider does not support JOIN without condition.";
		public const string Error_JoinOnOuterReferenceNotSupported = "Provider does not support JOIN conditions that reference outer queries.";
		public const string Error_Join_ParentReference_Condition   = "Provider does not support subqueries with JOIN which has reference to parent table in condition.";
		public const string Error_Skip_in_Subquery                 = "Provider does not support Skip value in subquery.";
		public const string Error_OrderBy_in_Subquery              = "Provider does not support ORDER BY in subquery.";
		public const string Error_Take_in_Derived                  = "Provider does not support Take value in derived table.";
		public const string Error_OrderBy_in_Derived               = "Provider does not support ORDER BY in derived table.";
		public const string Error_MutiTable_Insert                 = "Provider does not support multi-table insert.";
		public const string Error_SqlRow_in_Update                 = "Provider does not support SqlRow on the left-hand side of an UPDATE SET.";
		public const string Error_SqlRow_in_Update_Value           = "Provider does not support SqlRow literal on the right-hand side of an UPDATE SET.";
		public const string Error_RowNumber                        = "Provider does not support ROW_NUMBER function.";
		public const string Error_OrderByRequiredForIndexing       = "For retrieving index of row, specify OrderBy part.";
		public const string Error_DistinctByRequiresOrderBy        = "DistinctBy requires at least one ordering key.";

		public const string Error_WindowFunctionsInSearchCondition          = "Window functions cannot be used in search condition.";
		public const string Error_WindowFunction_PercentRank                = "PERCENT_RANK is not supported by current provider.";
		public const string Error_WindowFunction_CumeDist                   = "CUME_DIST is not supported by current provider.";
		public const string Error_WindowFunction_NTile                      = "NTILE is not supported by current provider.";
		public const string Error_WindowFunction_NthValue                   = "NTH_VALUE is not supported by current provider.";
		public const string Error_WindowFunction_LeadLag                    = "LEAD/LAG is not supported by current provider.";
		public const string Error_WindowFunction_FirstLastValue             = "FIRST_VALUE/LAST_VALUE is not supported by current provider.";
		public const string Error_WindowFunction_PercentileCont             = "PERCENTILE_CONT is not supported by current provider.";
		public const string Error_WindowFunction_PercentileDisc             = "PERCENTILE_DISC is not supported by current provider.";
		public const string Error_WindowFunction_Variance                   = "STDDEV/VARIANCE is not supported by current provider.";
		public const string Error_WindowFunction_Correlation                = "COVAR/CORR is not supported by current provider.";
		public const string Error_WindowFunction_LinearRegression           = "REGR_* is not supported by current provider.";
		public const string Error_WindowFunction_Median                     = "MEDIAN is not supported by current provider.";
		public const string Error_WindowFunction_FrameRows                  = "ROWS frame is not supported by current provider.";
		public const string Error_WindowFunction_FrameRange                 = "RANGE frame is not supported by current provider.";
		public const string Error_WindowFunction_FrameGroups                = "GROUPS frame is not supported by current provider.";
		public const string Error_WindowFunction_FrameExclude               = "Frame EXCLUDE clause is not supported by current provider.";
		public const string Error_WindowFunction_FrameRangeGroupsOrderBy    = "A RANGE or GROUPS frame with a value offset requires exactly one ORDER BY expression.";
		public const string Error_WindowFunction_OrderedSetFilter           = "FILTER (WHERE ...) on ordered-set aggregates (PERCENTILE_CONT/PERCENTILE_DISC) is not supported by current provider.";
		public const string Error_WindowFunction_Keep                       = "KEEP clause is not supported by current provider.";
		public const string Error_WindowFunction_NullTreatment              = "IGNORE NULLS is not supported by current provider.";
		public const string Error_WindowFunction_NthValueFrom               = "NTH_VALUE FROM LAST is not supported by current provider.";
		public const string Error_WindowFunction_LeadLagDefault             = "Default value argument for LEAD/LAG is not supported by current provider.";
		public const string Error_WindowFunction_AggregateWindowFunctions   = "Aggregate window functions are not supported by current provider.";
		public const string Error_WindowFunction_AggregateDistinct          = "DISTINCT is not supported in window aggregate functions by current provider.";
		public const string Error_WindowFunction_NotSupported               = "Window functions are not supported by current provider.";

		public const string Error_Upsert_MergeLowering_NotSupported =
			"Upsert configuration requires MERGE lowering (bulk source, non-PK match, conditional Insert, or SkipInsert), "
			+ "but the current provider does not support the two-branch MERGE shape. "
			+ "Reshape the Upsert call to a single-entity PK match without SkipInsert / Insert.When, "
			+ "or target a MERGE-capable provider.";

		public const string Error_Upsert_MergeWithPredicate_NotSupported =
			"Upsert configuration attaches a predicate to an Insert.When / Update.When branch routed through MERGE lowering, "
			+ "but the current provider's MERGE dialect has no WHEN [NOT] MATCHED AND <cond> or UPDATE … WHERE <cond> form. "
			+ "Remove the .When predicate, or target a provider that supports conditional MERGE branches.";

		public const string Error_Internal_UpdateInsertEmitter_CannotEmitUpdatePredicate =
			"Internal error: BuildInsertOrUpdateQueryAsUpdateInsert cannot emit an UPDATE predicate. "
			+ "Providers with IsInsertOrUpdateWithPredicateSupported=false must route Upsert.Update.When "
			+ "through SetIfExistsUpdateElseInsert (3-query orchestration).";

		public const string Error_Upsert_SkipInsert_With_Insert =
			"Upsert configuration is contradictory: SkipInsert() cannot be combined with Insert(...).";

		public const string Error_Upsert_SkipUpdate_With_Update =
			"Upsert configuration is contradictory: SkipUpdate() cannot be combined with Update(...).";

		public const string Error_Upsert_InsertBranch_DoNothing_With_Ops =
			"Insert branch configuration is contradictory: DoNothing() cannot be combined with Set/Ignore/When.";

		public const string Error_Upsert_UpdateBranch_DoNothing_With_Ops =
			"Update branch configuration is contradictory: DoNothing() cannot be combined with Set/Ignore/When.";

		public const string Error_Upsert_EmulationDisallowed =
			"Upsert cannot be expressed natively for this provider / configuration and would fall back to an emulated UPDATE+INSERT sequence. "
			+ "LinqOptions.UpsertEmulationPolicy is set to Throw — change the provider, adjust the Upsert configuration, or set it to Allow to permit emulation.";

		public const string Error_GroupGuard =
							"""
							You should explicitly specify selected fields for server-side GroupBy() call or add AsEnumerable() call before GroupBy() to perform client-side grouping.
							Set Configuration.Linq.GuardGrouping = false to disable this check.
							Additionally this guard exception can be disabled by extension GroupBy(...).DisableGuard().
							NOTE! By disabling this guard you accept Eager Loading for grouping query.
							""";

		public static class Oracle
		{
			public const string Error_ColumnSubqueryShouldNotContainParentIsNotNull = "Column expression should not contain parent's IS NOT NULL condition.";
		}

		public static class Sybase
		{
			public const string Error_JoinToDerivedTableWithTakeInvalid = "Feature not supported by database: database has issue with JOIN to limited recordset.";
			public const string Error_UpdateWithTopOrderBy              = "Feature not supported by database: UPDATE statement with the TOP + ORDER BY clause.";
			public const string Error_DeleteWithTopOrderBy              = "Feature not supported by database: DELETE statement with the TOP + ORDER BY clause.";
			public const string Error_UpdateWithSkip                    = "Feature not supported by database: UPDATE statement with the Skip.";
			public const string Error_DeleteWithSkip                    = "Feature not supported by database: DELETE statement with the Skip.";
		}

		public static class ClickHouse
		{
			public const string Error_CorrelatedDelete = "Feature not supported by database: Correlated DELETE";
			public const string Error_CorrelatedUpdate = "Feature not supported by database: Correlated UPDATE";
		}

		public static class MySql
		{
			public const string Error_SkipInUpdate = "Feature not supported by database: Skip in UPDATE.";
		}
	}
}
