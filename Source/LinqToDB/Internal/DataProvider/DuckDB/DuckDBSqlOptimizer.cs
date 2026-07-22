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
				}
				else if (e is SqlBinaryExpression binary)
				{
					// add explicit type casts to hint binary operator overloads - see sql builder cast notes.
					// The operands are rewritten from here rather than from the SqlParameter case: this converter
					// registers whatever the callback returns as the replacement for the element it was given, so
					// returning a cast that wraps the parameter would map the parameter to a node containing
					// itself, and the replacer would then expand it without end.
					var expr1 = CastOperand(binary.Expr1);
					var expr2 = CastOperand(binary.Expr2);

					if (!ReferenceEquals(expr1, binary.Expr1) || !ReferenceEquals(expr2, binary.Expr2))
						return new SqlBinaryExpression(binary.Type, expr1, binary.Operation, expr2, binary.Precedence);
				}

				return e;
			}, withStack: true);

			return statement;

			static ISqlExpression CastOperand(ISqlExpression expression)
			{
				// The cast marks this operand only - the parameter instance is shared by every reference to it,
				// so it must not carry the cast itself.
				return expression is SqlParameter { IsQueryParameter: true } parameter
					? QueryHelper.EnsureMandatoryCast(parameter, parameter.Type, canModify: false)
					: expression;
			}
		}
	}
}
