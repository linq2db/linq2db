using System;
using System.Collections.Generic;
using System.Numerics;

using LinqToDB.Internal.Common;
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
			statement = CastTimestampValuesOfBetween(statement, mappingSchema);

			return statement;
		}

		// DuckDB types a parameter value of BETWEEN by a date bound beside it, dropping its time, and binds a parameter its uses
		// type differently as text, which fails. Such a value is cast to its own type wherever the statement uses it.
		static SqlStatement CastTimestampValuesOfBetween(SqlStatement statement, MappingSchema mappingSchema)
		{
			var values = new HashSet<SqlParameter>(Utils.ObjectReferenceEqualityComparer<SqlParameter>.Default);

			statement.VisitAll((values, mappingSchema), static (context, e) =>
			{
				if (e is SqlPredicate.Between between && IsTimestampParameterBesideDate(between, context.mappingSchema))
					context.values.Add((SqlParameter)QueryHelper.UnwrapNullablity(between.Expr1));
			});

			if (values.Count == 0)
				return statement;

			return (SqlStatement)new CastParametersVisitor(values).ProcessElement(statement);
		}

		static bool IsTimestampParameterBesideDate(SqlPredicate.Between between, MappingSchema mappingSchema)
		{
			return QueryHelper.UnwrapNullablity(between.Expr1) is SqlParameter { IsQueryParameter: true }
				&& QueryHelper.GetDbDataType(between.Expr1, mappingSchema).DataType is DataType.DateTime or DataType.DateTime2
				&& (IsDate(between.Expr2, mappingSchema) || IsDate(between.Expr3, mappingSchema));

			static bool IsDate(ISqlExpression expression, MappingSchema mappingSchema)
			{
				return QueryHelper.UnwrapNullablity(expression) is not SqlParameter
					&& QueryHelper.GetDbDataType(expression, mappingSchema).DataType == DataType.Date;
			}
		}

		sealed class CastParametersVisitor(HashSet<SqlParameter> parameters) : SqlQueryVisitor(VisitMode.Modify, null)
		{
			protected internal override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
			{
				return parameters.Contains(sqlParameter) ? Cast(sqlParameter) : base.VisitSqlParameter(sqlParameter);
			}

			// The nodes below keep their parameter in a SqlParameter-typed field, which a cast does not fit: a marked one
			// is replaced by the cast, and an inlined one is rendered as a literal.
			protected internal override IQueryElement VisitSqlParameterCastExpression(SqlParameterCastExpression element)
			{
				return parameters.Contains(element.Parameter) ? Cast(element.Parameter) : element;
			}

			protected internal override IQueryElement VisitSqlInlinedSqlExpression(SqlInlinedSqlExpression element)
			{
				return element;
			}

			protected internal override IQueryElement VisitSqlInlinedToSqlExpression(SqlInlinedToSqlExpression element)
			{
				return element;
			}

			static SqlCastExpression Cast(SqlParameter parameter)
			{
				return new SqlCastExpression(parameter, parameter.Type, null, isMandatory: true);
			}
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
			});

			return statement;

			static ISqlExpression CastOperand(ISqlExpression expression)
			{
				// The cast marks this operand only - the parameter instance is shared by every reference to it,
				// so it must not carry the cast itself.
				return expression is SqlParameter { IsQueryParameter: true } parameter
					? QueryHelper.EnsureParameterCast(parameter)
					: expression;
			}
		}
	}
}
