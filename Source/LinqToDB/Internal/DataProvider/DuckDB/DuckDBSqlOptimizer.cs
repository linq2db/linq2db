using System;
using System.Numerics;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSqlOptimizer(SqlProviderFlags sqlProviderFlags) : BasicSqlOptimizer(sqlProviderFlags)
	{
		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new DuckDBSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.FinalizeStatement(statement, context, dataOptions, mappingSchema);

			statement = TuneParameters(statement);

			return statement;
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
				{
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					break;
				}
				case QueryType.Update:
				{
					statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					break;
				}
			}

			// DuckDB does not support prepared parameters in RETURNING clauses.
			// Force inline all parameters in OUTPUT clause.
			InlineParametersInOutputClause(statement);

			return statement;
		}

		/// <summary>
		/// DuckDB does not support prepared parameters in RETURNING clauses.
		/// This method finds all parameters in output clauses and marks them for inlining.
		/// </summary>
		static void InlineParametersInOutputClause(SqlStatement statement)
		{
			var output = statement switch
			{
				SqlDeleteStatement del   => del.OutputClause,
				SqlInsertStatement ins   => ins.OutputClause,
				SqlUpdateStatement upd   => upd.OutputClause,
				SqlMergeStatement  merge => merge.OutputClause,
				_ => null,
			};

			if (output?.HasOutput != true)
				return;

			var visitor = new InlineOutputParametersVisitor();
			visitor.Visit(output);
		}

		sealed class InlineOutputParametersVisitor() : SqlQueryVisitor(VisitMode.ReadOnly, null)
		{
			protected internal override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
			{
				sqlParameter.IsQueryParameter = false;
				return base.VisitSqlParameter(sqlParameter);
			}
		}

		private static SqlStatement TuneParameters(SqlStatement statement)
		{
			statement = statement.Convert(static (visitor, e) =>
			{
				if (e is SqlParameter p)
				{
					// disable parameters for unsupported types
					// add explicit type casts to hint binary operator overloads

					var pType   = p.Type.SystemType.UnwrapNullableType();
					var adapter = DuckDBProviderAdapter.Instance;

					// sync with DuckDBBulkCopy._convertToParameter when edit IsQueryParameter
					// BitString - not implemented by provider
					if (p.Type.DataType == DataType.BitArray
						// TIME_NS - not implemented by provider
						|| p.Type.DataType == DataType.Time && p.Type.Precision > 6
						// BIGNUM - not implemented by provider
						|| p.Type.DataType == DataType.VarNumeric && pType == typeof(BigInteger)
						// some DuckDB* provider types are read-only and cannot be written as parameters
						|| pType == adapter.DuckDBInterval
						|| pType == adapter.DuckDBTimestamp && p.Type.Precision > 6)
					{
						p.IsQueryParameter = false;
					}

					if (p.IsQueryParameter && visitor.ParentElement?.ElementType is QueryElementType.SqlBinaryExpression)
					{
						// see sql builder BuildParameter notes
						p.NeedsCast = true;
					}
				}

				return e;
			}, withStack: true);

			return statement;
		}
	}
}
