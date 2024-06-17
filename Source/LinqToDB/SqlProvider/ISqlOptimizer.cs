﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

	public interface ISqlOptimizer
	{
		/// <summary>
		/// Finalizes query.
		/// </summary>
		/// <returns>Query which is ready for optimization.</returns>
		SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions);

		/// <summary>
		/// Examine query for parameter dependency.
		/// </summary>
		/// <returns></returns>
		bool IsParameterDependent(NullabilityContext nullability, SqlStatement statement);

		/// <summary>
		/// Corrects skip/take for specific DataProvider
		/// </summary>
		void ConvertSkipTake(NullabilityContext nullability, MappingSchema mappingSchema, DataOptions dataOptions, SelectQuery selectQuery, OptimizationContext optimizationContext, out ISqlExpression? takeExpr, out ISqlExpression? skipExpr);

		SqlExpressionOptimizerVisitor CreateOptimizerVisitor(bool allowModify);
		SqlExpressionConvertVisitor   CreateConvertVisitor(bool   allowModify);
	}
}
