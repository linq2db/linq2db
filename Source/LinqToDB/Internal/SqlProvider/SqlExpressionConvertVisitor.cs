using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	public class SqlExpressionConvertVisitor : SqlQueryVisitor
	{
		protected bool VisitQueries;

		protected bool IsInsidePredicate { get; private set; }

		protected OptimizationContext OptimizationContext = default!;
		protected NullabilityContext  NullabilityContext  = default!;
		protected ISqlExpressionFactory Factory => OptimizationContext.Factory;

		protected EvaluationContext EvaluationContext => OptimizationContext.EvaluationContext;
		protected DataOptions DataOptions => OptimizationContext.DataOptions;
		protected MappingSchema MappingSchema => OptimizationContext.MappingSchema;
		protected SqlProviderFlags SqlProviderFlags => OptimizationContext.SqlProviderFlags;

		public SqlExpressionConvertVisitor(bool allowModify) : base(allowModify ? VisitMode.Modify : VisitMode.Transform, null)
		{
		}

		protected virtual bool SupportsBooleanInColumn => false;
		protected virtual bool SupportsNullInColumn => true;
		protected virtual bool SupportsDistinctAsExistsIntersect => false;
		protected virtual bool SupportsNullIf => true;

		public virtual IQueryElement Convert(OptimizationContext optimizationContext, NullabilityContext nullabilityContext, IQueryElement element, bool visitQueries)
		{
			Cleanup();

			OptimizationContext = optimizationContext;
			NullabilityContext = nullabilityContext;
			VisitQueries = visitQueries;
			SetTransformationInfo(optimizationContext.TransformationInfoConvert);

			var newElement = ProcessElement(element);

			return newElement;
		}

		public override void Cleanup()
		{
			base.Cleanup();

			OptimizationContext = default!;
			NullabilityContext = default!;
			VisitQueries = default;
			IsInsidePredicate = false;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return element;

			var saveIsInsidePredicate = IsInsidePredicate;

			if (element is not SqlNullabilityExpression and not ISqlPredicate)
			{
				IsInsidePredicate = false;
			}

			var newElement = base.Visit(element);

			IsInsidePredicate = saveIsInsidePredicate;

			return newElement;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			var newElement = base.VisitSqlColumnExpression(column, expression);

			newElement = WrapBooleanExpression(newElement, includeFields: false, withNull: column.CanBeNullable(NullabilityContext));
			if (!ReferenceEquals(newElement, expression))
				expression = (ISqlExpression)Visit(Optimize(newElement));

			newElement = WrapColumnExpression(expression);
			if (!ReferenceEquals(newElement, expression))
			{
				expression = (ISqlExpression)Visit(Optimize(newElement));
			}

			return expression;
		}

		protected internal override IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			var result = (SqlOutputClause)base.VisitSqlOutputClause(element);

			if (result.OutputColumns == null)
				return result;

			var newElements = VisitElements(result.OutputColumns, GetVisitMode(element), e => WrapBooleanExpression(e, includeFields : false));
			if (!ReferenceEquals(newElements, result.OutputColumns))
			{
				return new SqlOutputClause()
				{
					OutputTable = result.OutputTable,
					OutputItems = result.OutputItems,
					OutputColumns = newElements,
				};
			}

			return result;
		}

		protected internal override IQueryElement VisitSqlConditionExpression(SqlConditionExpression element)
		{
			var newElement = base.VisitSqlConditionExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlCondition(element);

			if (!ReferenceEquals(newElement, element))
			{
				return Visit(NotifyReplaced(newElement, element));
			}

			if (SupportsNullIf)
			{
				if (element.Condition is SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal } exprExpr
					&& element.TrueValue.IsNullValue)
				{
					if (element.FalseValue.Equals(exprExpr.Expr1, SqlQuery.SqlExtensions.DefaultComparer))
						return NotifyReplaced(new SqlFunction(QueryHelper.GetDbDataType(element.FalseValue, MappingSchema), "NULLIF", false, true, exprExpr.Expr1, exprExpr.Expr2), element);

					if (element.FalseValue.Equals(exprExpr.Expr2, SqlQuery.SqlExtensions.DefaultComparer))
						return NotifyReplaced(new SqlFunction(QueryHelper.GetDbDataType(element.FalseValue, MappingSchema), "NULLIF", false, true, exprExpr.Expr2, exprExpr.Expr1), element);
				}
			}

			return element;
		}

		protected override SqlCaseExpression.CaseItem VisitCaseItem(SqlCaseExpression.CaseItem element)
		{
			var newElement = base.VisitCaseItem(element);

			newElement = ConvertCaseItem(newElement);

			return newElement;
		}

		protected internal override IQueryElement VisitSqlCaseExpression(SqlCaseExpression element)
		{
			var newElement = base.VisitSqlCaseExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlCaseExpression(element);

			if (!ReferenceEquals(newElement, element))
			{
				return Visit(NotifyReplaced(newElement, element));
			}

			return element;
		}

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			if (!VisitQueries)
				return selectQuery;

			var saveNullabilityContext = NullabilityContext;
			NullabilityContext = NullabilityContext.WithJoinSource(selectQuery);

			var newQuery = base.VisitSqlQuery(selectQuery);

			NullabilityContext = saveNullabilityContext;

			return newQuery;
		}

		protected internal override IQueryElement VisitExprPredicate(SqlPredicate.Expr predicate)
		{
			var result = base.VisitExprPredicate(predicate);

			if (!ReferenceEquals(result, predicate))
				return Visit(result);

			var newResult = result;

			if (predicate.Expr1 is ISqlPredicate)
			{
				result = predicate.Expr1;
			}
			else
			{
				if (!SqlProviderFlags.SupportsBooleanType || QueryHelper.GetColumnDescriptor(predicate.Expr1)?.ValueConverter != null)
				{
					var unwrapped = QueryHelper.UnwrapNullablity(predicate.Expr1);
					if (unwrapped is SqlCastExpression castExpression)
					{
						newResult = ConvertCastToPredicate(castExpression);
					}
					else if (unwrapped is SqlParameterizedExpressionBase { IsPredicate: true } or SqlValue { Value: null })
					{
						// do nothing
					}
					else
					{
						newResult = ConvertToBooleanSearchCondition(predicate.Expr1);
					}
				}
			}

			if (!ReferenceEquals(newResult, result))
			{
				result = Visit(Optimize(newResult));
			}

			return result;
		}

		public virtual IQueryElement ConvertCastToPredicate(SqlCastExpression castExpression)
		{
			return ConvertToBooleanSearchCondition(castExpression.Expression);
		}

		protected internal override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			var newElement = base.VisitSqlFieldReference(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			return element;
		}

		protected internal override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			var newElement = base.VisitSqlColumnReference(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			return element;
		}

		protected internal override IQueryElement VisitNotPredicate(SqlPredicate.Not predicate)
		{
			var saveInner    = predicate.Predicate;

			var saveInsidePredicate = IsInsidePredicate;
			IsInsidePredicate = true;
			var newPredicate = base.VisitNotPredicate(predicate);
			IsInsidePredicate = saveInsidePredicate;

			if (!ReferenceEquals(newPredicate, predicate) || !ReferenceEquals(saveInner, predicate.Predicate))
			{
				newPredicate = Optimize(newPredicate);
				return Visit(newPredicate);
			}

			return newPredicate;
		}

		protected internal override IQueryElement VisitSqlValue(SqlValue element)
		{
			var newElement = base.VisitSqlValue(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Value is Sql.SqlID)
				return element;

			if (!MappingSchema.ValueToSqlConverter.CanConvert(element.ValueType, DataOptions, element.Value))
			{
				// we cannot generate SQL literal, so just convert to parameter
				var param = OptimizationContext.SuggestDynamicParameter(element.ValueType, element.Value);
				return param;
			}

			return element;
		}

		protected IQueryElement Optimize(IQueryElement element)
		{
			return OptimizationContext.OptimizerVisitor.Optimize(EvaluationContext, NullabilityContext, OptimizationContext.TransformationInfo, DataOptions, OptimizationContext.MappingSchema, element, VisitQueries, reducePredicates: false);
		}

		protected internal override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var saveInsidePredicate = IsInsidePredicate;
			IsInsidePredicate = true;
			var newElement          = base.VisitExprExprPredicate(predicate);
			IsInsidePredicate = saveInsidePredicate;

			if (!ReferenceEquals(newElement, predicate))
			{
				return Visit(Optimize(newElement));
			}

			var newPredicate = ConvertExprExprPredicate(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
			{
				newPredicate = Optimize(newPredicate);
				newPredicate = Visit(newPredicate);
			}

			return newPredicate;
		}

		protected internal override IQueryElement VisitSqlCompareToExpression(SqlCompareToExpression element)
		{
			var newElement = base.VisitSqlCompareToExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var caseExpression = new SqlCaseExpression(
				new DbDataType(typeof(int)),
				[
					new(new SqlSearchCondition().AddGreater(element.Expression1, element.Expression2, DataOptions.LinqOptions.CompareNulls), new SqlValue(1)),
					new(new SqlSearchCondition().AddEqual(element.Expression1, element.Expression2, DataOptions.LinqOptions.CompareNulls), new SqlValue(0)),
				],
				new SqlValue(-1));

			return Visit(Optimize(caseExpression));
		}

		protected internal override IQueryElement VisitIsDistinctPredicate(SqlPredicate.IsDistinct predicate)
		{
			var newPredicate = base.VisitIsDistinctPredicate(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

			if (!SqlProviderFlags.IsDistinctFromSupported)
			{
				var converted = SupportsDistinctAsExistsIntersect
					? ConvertIsDistinctPredicateAsIntersect(predicate)
					: ConvertIsDistinctPredicate(predicate);

				if (!ReferenceEquals(converted, predicate))
				{
					return Visit(Optimize(converted));
				}
			}

			return predicate;
		}

		public IQueryElement ConvertIsDistinctPredicate(SqlPredicate.IsDistinct predicate)
		{
			/*
				(value1 IS NULL AND value2 IS NOT NULL) OR
				(value1 IS NOT NULL AND value2 IS NULL) OR
				(value1 <> value2)
			 */

			var searchCondition = new SqlSearchCondition(true);

			searchCondition
				.AddAnd(sc => sc
					.Add(new SqlPredicate.IsNull(predicate.Expr1, false))
					.Add(new SqlPredicate.IsNull(predicate.Expr2, true))
				)
				.AddAnd(sc => sc
					.Add(new SqlPredicate.IsNull(predicate.Expr1, true))
					.Add(new SqlPredicate.IsNull(predicate.Expr2, false))
				)
				.Add(
					new SqlPredicate.ExprExpr(predicate.Expr1, SqlPredicate.Operator.NotEqual, predicate.Expr2, null)
				);

			return searchCondition.MakeNot(predicate.IsNot);
		}

		protected virtual IQueryElement ConvertIsDistinctPredicateAsIntersect(SqlPredicate.IsDistinct predicate)
		{
			/*
				EXISTS(value1 INTERSECT value2)
			 */

			var expr1 = new SelectQuery();
			expr1.Select.AddColumn(predicate.Expr1);

			var expr2 = new SelectQuery();
			expr2.Select.AddColumn(predicate.Expr2);

			expr1.SetOperators.Add(new SqlSetOperator(expr2, SetOperation.Intersect));

			return new SqlPredicate.Exists(!predicate.IsNot, expr1);
		}

		public virtual IQueryElement ConvertExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var unwrapped = QueryHelper.UnwrapNullablity(predicate.Expr1);
			if (unwrapped.ElementType == QueryElementType.SqlRow)
			{
				var newPredicate = ConvertRowExprExpr(predicate, EvaluationContext);
				if (!ReferenceEquals(newPredicate, predicate))
				{
					return Visit(Optimize(newPredicate));
				}
			}

			var expr1IsNullable = predicate.Expr1.CanBeNullableOrUnknown(NullabilityContext, false);
			var expr2IsNullable = predicate.Expr2.CanBeNullableOrUnknown(NullabilityContext, false);

			// ExprExpr optimization over complex arguments
			// to avoid "complex_expression IS NULL" checks when possible by reducing NULL to UnknownAsValue
			if (predicate.UnknownAsValue != null && (expr1IsNullable || expr2IsNullable))
			{
				var expr1IsComplexWithUnknown = IsComplexNullable(predicate.Expr1);
				var expr2IsComplexWithUnknown = IsComplexNullable(predicate.Expr2);

				if (expr1IsComplexWithUnknown || expr2IsComplexWithUnknown)
				{
					switch (predicate.Operator)
					{
						case SqlPredicate.Operator.Equal:
						{
							if (IsInsidePredicate && (expr1IsNullable ^ expr2IsNullable))
							{
								// convert A == B where only A or B is null (and complex expression) to
								// IIF(A == B, true, false)
								return WrapCondition(false);
							}

							break;
						}

						case SqlPredicate.Operator.NotEqual:
						{
							if (expr1IsNullable ^ expr2IsNullable)
							{
								// convert A != B where only A or B is null (and complex expression) to
								// IIF(A == B, false, true)
								return WrapCondition(true);
							}

							break;
						}

						default:
						{
							if ((IsInsidePredicate || predicate.UnknownAsValue == true) && (expr1IsNullable || expr2IsNullable))
							{
								// convert A == B where only A or B is null (and complex expression) to
								// IIF(A op B, true, false)
								// or
								// IIF(A inverted_op B, false, true)
								return WrapCondition(predicate.UnknownAsValue.Value);
							}

							break;
						}
					}
				}

				ISqlPredicate WrapCondition(bool invert)
				{
					var trueValue  = new SqlValue(true);
					var falseValue = new SqlValue(false);

					var exprExpr = new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, predicate.Expr2, null);
					var condition = !invert
						? new SqlConditionExpression(exprExpr, trueValue, falseValue)
						// plain Invert will restore UnknownAsValue for comparison operators
						: new SqlConditionExpression(exprExpr.InvertWithoutNull(), falseValue, trueValue);

					if (!SqlProviderFlags.SupportsBooleanType)
						return new SqlPredicate.IsTrue(condition, trueValue, falseValue, null, false);
					else
						return new SqlPredicate.Expr(condition);
				}

				bool IsComplexNullable(ISqlExpression expr)
				{
					if (!QueryHelper.CanBeNullableOrUnknown(expr, NullabilityContext, false))
						return false;

					// decide on level of condition complexity to use IIF(cond, true, false)
					// istead of IS NULL checks
					return null != predicate.Find(static e =>
					{
						return e.ElementType is QueryElementType.SqlQuery;
					});
				}
			}

			// convert bool_exp_1 == bool_expr_2 to (x ? 1 : 0) == (y ? 1 : 0)
			// for providers that doesn't support boolean(predicate) comparison
			// or for predicates that could return UNKNOWN
			// Alternative could be to use IS [NOT] DISTINCT FROM predicate
			if (!SqlProviderFlags.SupportsPredicatesComparison
				// Operator check added as we perform optimization only for boolean operands, which cannot be used with non-equality operators
				|| (predicate.Operator is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual && (expr1IsNullable || expr2IsNullable)))
			{
				var expr1IsPredicate = QueryHelper.UnwrapNullablity(predicate.Expr1).IsPredicate();
				var expr2IsPredicate = QueryHelper.UnwrapNullablity(predicate.Expr2).IsPredicate();

				var expr1IsConstant = QueryHelper.UnwrapNullablity(predicate.Expr1) is (SqlValue or SqlParameter { IsQueryParameter: false });
				var expr2IsConstant = QueryHelper.UnwrapNullablity(predicate.Expr2) is (SqlValue or SqlParameter { IsQueryParameter: false });

				var expr1 = expr1IsPredicate && !expr2IsConstant
					? WrapBooleanExpression(predicate.Expr1, includeFields : true, forceConvert: !SqlProviderFlags.SupportsPredicatesComparison)
					: predicate.Expr1;
				var expr2 = expr2IsPredicate && !expr1IsConstant
					? WrapBooleanExpression(predicate.Expr2, includeFields : true, forceConvert: !SqlProviderFlags.SupportsPredicatesComparison)
					: predicate.Expr2;

				if (!ReferenceEquals(expr1, predicate.Expr1) || !ReferenceEquals(expr2, predicate.Expr2))
				{
					return new SqlPredicate.ExprExpr(expr1, predicate.Operator, expr2, predicate.UnknownAsValue);
				}
			}

			return predicate;
		}

		static SqlField ExpectsUnderlyingField(ISqlExpression expr)
		{
			var result = QueryHelper.GetUnderlyingField(expr);
			if (result == null)
				throw new InvalidOperationException($"Cannot retrieve underlying field for '{expr.ToDebugString()}'.");
			return result;
		}

		protected internal override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			var newElement = base.VisitInListPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (predicate.Expr1.ElementType == QueryElementType.SqlRow)
			{
				var converted = ConvertRowInList(predicate);
				if (!ReferenceEquals(converted, predicate))
				{
					converted = (ISqlPredicate)Optimize(converted);
					converted = (ISqlPredicate)Visit(converted);
					return converted;
				}
			}

			if (predicate.Values.Count == 0)
				return SqlPredicate.MakeBool(predicate.IsNot);

			if (predicate.Values is [SqlParameter parameter])
			{
				var paramValue = parameter.GetParameterValue(EvaluationContext.ParameterValues);

				if (paramValue.ProviderValue == null)
					return SqlPredicate.MakeBool(predicate.IsNot);

				if (paramValue.ProviderValue is IEnumerable items)
				{
					if (predicate.Expr1 is ISqlTableSource table)
					{
						var keys  = table.GetKeys(true);

						if (keys == null || keys.Count == 0)
							throw new LinqToDBException("Cant create IN expression.");

						if (keys.Count == 1)
						{
							var values = new List<ISqlExpression>();
							var field  = ExpectsUnderlyingField(keys[0]);
							var cd     = field.ColumnDescriptor;

							foreach (var item in items)
							{
								values.Add(cd.GetSqlValueFromObject(item!));
							}

							if (values.Count == 0)
								return SqlPredicate.MakeBool(predicate.IsNot);

							return new SqlPredicate.InList(keys[0], null, predicate.IsNot, values);
						}

						{
							var sc = new SqlSearchCondition(true);

							foreach (var item in items)
							{
								var itemCond = new SqlSearchCondition();

								foreach (var key in keys)
								{
									var field    = ExpectsUnderlyingField(key);
									var cd       = field.ColumnDescriptor;
									var sqlValue = cd.GetSqlValueFromObject(item!);
									//TODO: review
									ISqlPredicate p = sqlValue.Value == null ?
										new SqlPredicate.IsNull  (field, false) :
										new SqlPredicate.ExprExpr(field, SqlPredicate.Operator.Equal, sqlValue, null);

									itemCond.Add(p);
								}

								sc.Add(itemCond);
							}

							if (sc.Predicates.Count == 0)
								return SqlPredicate.MakeBool(predicate.IsNot);

							return Optimize(sc.MakeNot(predicate.IsNot));
						}
					}

					if (predicate.Expr1 is SqlObjectExpression expr)
					{
						var parameters = expr.InfoParameters;
						if (parameters.Length == 1)
						{
							var values = new List<ISqlExpression>();

							foreach (var item in items)
								values.Add(expr.GetSqlValue(item!, 0));

							if (values.Count == 0)
								return SqlPredicate.MakeBool(predicate.IsNot);

							return new SqlPredicate.InList(parameters[0].Sql, null, predicate.IsNot, values);
						}

						var sc = new SqlSearchCondition(true);

						foreach (var item in items)
						{
							var itemCond = new SqlSearchCondition();

							for (var i = 0; i < parameters.Length; i++)
							{
								var sql   = parameters[i].Sql;
								var value = expr.GetSqlValue(item!, i);
								ISqlPredicate cond  = value == null ?
									new SqlPredicate.IsNull  (sql, false) :
									new SqlPredicate.ExprExpr(sql, SqlPredicate.Operator.Equal, value, null);

								itemCond.Predicates.Add(cond);
							}

							sc.Add(itemCond);
						}

						if (sc.Predicates.Count == 0)
							return SqlPredicate.MakeBool(predicate.IsNot);

						return Optimize(sc.MakeNot(predicate.IsNot));
					}
				}
			}

			return predicate;
		}

		protected internal override IQueryElement VisitSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var newElement = base.VisitSearchStringPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			var newPredicate = (IQueryElement)ConvertSearchStringPredicate(predicate);
			if (!ReferenceEquals(newPredicate, predicate))
			{
				newPredicate = Optimize(newPredicate);
				newPredicate = Visit(newPredicate);
			}

			return newPredicate;
		}

		public virtual ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == false)
			{
				predicate = new SqlPredicate.SearchString(
					PseudoFunctions.MakeToLower(predicate.Expr1, MappingSchema),
					predicate.IsNot,
					PseudoFunctions.MakeToLower(predicate.Expr2, MappingSchema),
					predicate.Kind,
					new SqlValue(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate);
		}

		#region LIKE support

		/// <summary>
		/// Escape sequence/character to escape special characters in LIKE predicate (defined by <see cref="LikeCharactersToEscape"/>).
		/// Default: <c>"~"</c>.
		/// </summary>
		public virtual string LikeEscapeCharacter => "~";
		public virtual string LikeWildcardCharacter => "%";
		public virtual bool LikePatternParameterSupport => true;
		public virtual bool LikeValueParameterSupport => true;
		/// <summary>
		/// Should be <see langword="true"/> for provider with <c>LIKE ... ESCAPE</c> modifier support.
		/// Default: <see langword="true"/>.
		/// </summary>
		public virtual bool LikeIsEscapeSupported => true;

		public virtual ISqlExpression CreateLikeEscapeCharacter() => new SqlValue(LikeEscapeCharacter);

		protected static readonly string[] StandardLikeCharactersToEscape = {"%", "_", "?", "*", "#", "[", "]"};

		/// <summary>
		/// Characters with special meaning in LIKE predicate (defined by <see cref="LikeCharactersToEscape"/>) that should be escaped to be used as matched character.
		/// Default: <c>["%", "_", "?", "*", "#", "[", "]"]</c>.
		/// </summary>
		public virtual string[] LikeCharactersToEscape => StandardLikeCharactersToEscape;

		public virtual string EscapeLikeCharacters(string str, string escape)
		{
			var newStr = str;

			newStr = newStr.Replace(escape, escape + escape, StringComparison.Ordinal);

			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newStr = newStr.Replace(s, escape + s, StringComparison.Ordinal);
			}

			return newStr;
		}

		ISqlExpression GenerateEscapeReplacement(ISqlExpression expression, ISqlExpression character, ISqlExpression escapeCharacter)
		{
			var result = PseudoFunctions.MakeReplace(expression, character, new SqlConcatExpression(true, escapeCharacter, character), MappingSchema);
			return result;
		}

		/// <summary>
		/// Implements LIKE pattern escaping logic for provider without ESCAPE clause support (<see cref="LikeIsEscapeSupported"/> is <see langword="false"/>).
		/// Default logic prefix characters from <see cref="LikeCharactersToEscape"/> with <see cref="LikeEscapeCharacter"/>.
		/// </summary>
		/// <param name="str">Raw pattern value.</param>
		/// <returns>Escaped pattern value.</returns>
		protected virtual string EscapeLikePattern(string str)
		{
			foreach (var s in LikeCharactersToEscape)
				str = str.Replace(s, LikeEscapeCharacter + s, StringComparison.Ordinal);

			return str;
		}

		public virtual ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			var newExpr = expression;

			escape ??= CreateLikeEscapeCharacter();

			newExpr = GenerateEscapeReplacement(newExpr, escape, escape);

			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newExpr = GenerateEscapeReplacement(newExpr, new SqlValue(s), escape);
			}

			return newExpr;
		}

		protected ISqlPredicate ConvertSearchStringPredicateViaLike(SqlPredicate.SearchString predicate)
		{
			if (predicate.Expr2.TryEvaluateExpression(EvaluationContext, out var patternRaw)
				&& Converter.TryConvertToString(patternRaw, out var patternRawValue))
			{
				if (patternRawValue == null)
					return new SqlPredicate.IsTrue(new SqlValue(true), new SqlValue(true), new SqlValue(false), null, predicate.IsNot);

				var patternValue = LikeIsEscapeSupported
					? EscapeLikeCharacters(patternRawValue, LikeEscapeCharacter)
					: EscapeLikePattern(patternRawValue);

				patternValue = predicate.Kind switch
				{
					SqlPredicate.SearchString.SearchKind.StartsWith => patternValue + LikeWildcardCharacter,
					SqlPredicate.SearchString.SearchKind.EndsWith => LikeWildcardCharacter + patternValue,
					SqlPredicate.SearchString.SearchKind.Contains => LikeWildcardCharacter + patternValue + LikeWildcardCharacter,
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}"),
				};

				var patternExpr = LikePatternParameterSupport
					? QueryHelper.CreateSqlValue(patternValue, QueryHelper.GetDbDataType(predicate.Expr2, MappingSchema), predicate.Expr2)
					: new SqlValue(patternValue);

				var valueExpr = predicate.Expr1;
				if (!LikeValueParameterSupport)
				{
					predicate.Expr1.VisitAll(static e =>
					{
						if (e is SqlParameter p)
							p.IsQueryParameter = false;
					});
				}

				return new SqlPredicate.Like(valueExpr, predicate.IsNot, patternExpr,
					LikeIsEscapeSupported && (!string.Equals(patternValue, patternRawValue, StringComparison.Ordinal)) ? CreateLikeEscapeCharacter() : null);
			}
			else
			{
				ISqlExpression? escape = null;

				var patternExpr = EscapeLikeCharacters(predicate.Expr2, ref escape);

				var anyCharacterExpr = new SqlValue(LikeWildcardCharacter);

				patternExpr = predicate.Kind switch
				{
					SqlPredicate.SearchString.SearchKind.StartsWith => new SqlConcatExpression(true, patternExpr, anyCharacterExpr),
					SqlPredicate.SearchString.SearchKind.EndsWith   => new SqlConcatExpression(true, anyCharacterExpr, patternExpr),
					SqlPredicate.SearchString.SearchKind.Contains   => new SqlConcatExpression(true, anyCharacterExpr, patternExpr, anyCharacterExpr),
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}"),
				};

				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, patternExpr, LikeIsEscapeSupported ? escape : null);
			}
		}

		#endregion

		#region Visitor overrides

		protected internal override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
		{
			var newElement = base.VisitIsNullPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (NullabilityContext.IsEmpty)
				return predicate;

			if (QueryHelper.UnwrapNullablity(predicate.Expr1) is SqlRowExpression sqlRow)
			{
				if (ConvertRowIsNullPredicate(sqlRow, predicate.IsNot, out var rowIsNullFallback))
				{
					return Visit(rowIsNullFallback);
				}
			}

			return predicate;
		}

		protected internal override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			var newElement = base.VisitSqlFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlFunction(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(Optimize(newElement));

			return element;
		}

		protected internal override IQueryElement VisitSqlExtendedFunction(SqlExtendedFunction element)
		{
			var newElement = base.VisitSqlExtendedFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlExtendedFunction(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(Optimize(newElement));

			return element;
		}

		protected internal override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			var saveNullabilityContext = NullabilityContext;
			NullabilityContext = NullabilityContext.WithJoinSource(element.Table.Source);

			var newElement = base.VisitSqlJoinedTable(element);

			NullabilityContext = saveNullabilityContext;

			return newElement;
		}

		protected internal override IQueryElement VisitSqlExpression(SqlExpression element)
		{
			var newElement = base.VisitSqlExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlExpression(element);
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}

			return newElement;
		}

		protected internal override IQueryElement VisitLikePredicate(SqlPredicate.Like predicate)
		{
			var newElement = base.VisitLikePredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			newElement = ConvertLikePredicate(predicate);
			if (!ReferenceEquals(newElement, predicate))
			{
				newElement = Visit(Optimize(newElement));
			}

			return newElement;
		}

		protected internal override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			var newElement = base.VisitSqlBinaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlBinaryExpression(element);
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}

			return newElement;
		}

		protected internal override IQueryElement VisitSqlUnaryExpression(SqlUnaryExpression element)
		{
			var newElement = base.VisitSqlUnaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlUnaryExpression(element);
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}

			return newElement;
		}

		protected internal override IQueryElement VisitSqlInlinedSqlExpression(SqlInlinedSqlExpression element)
		{
			var newElement = base.VisitSqlInlinedSqlExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = element.GetSqlExpression(EvaluationContext);
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}

			return newElement;
		}

		protected internal override IQueryElement VisitSqlInlinedToSqlExpression(SqlInlinedToSqlExpression element)
		{
			var newElement = base.VisitSqlInlinedToSqlExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = element.GetSqlExpression(EvaluationContext);
			if (!ReferenceEquals(newElement, element))
			{
				newElement = Visit(Optimize(newElement));
			}

			return newElement;
		}

		protected internal override IQueryElement VisitBetweenPredicate(SqlPredicate.Between predicate)
		{
			var newElement = base.VisitBetweenPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Between) && QueryHelper.UnwrapNullablity(predicate.Expr1) is SqlRowExpression)
			{
				return Visit(Optimize(ConvertBetweenPredicate(predicate)));
			}

			return newElement;
		}

		protected internal override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			if (predicate.DoNotConvert)
				return base.VisitInSubQueryPredicate(predicate);

			var newPredicate = base.VisitInSubQueryPredicate(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

			var reconciled = ReconcileDurationUnits(predicate);
			if (reconciled != null)
				return Visit(reconciled);

			var doNotSupportCorrelatedSubQueries = SqlProviderFlags.SupportedCorrelatedSubqueriesLevel == 0;

			var testExpression  = predicate.Expr1;
			var valueExpression = predicate.SubQuery.Select.Columns[0].Expression;

			if (NullabilityContext.CanBeNull(testExpression) && NullabilityContext.CanBeNull(valueExpression))
			{
				if (doNotSupportCorrelatedSubQueries)
				{
					newPredicate = EmulateNullability(predicate);

					if (!ReferenceEquals(newPredicate, predicate))
						return Visit(newPredicate);
				}
				else
				{
					return Visit(ConvertToExists(predicate));
				}
			}

			if (!doNotSupportCorrelatedSubQueries && (DataOptions.LinqOptions.PreferExistsForScalar || SqlProviderFlags.IsExistsPreferableForContains))
			{
				return Visit(ConvertToExists(predicate));
			}

			if (NullabilityContext.CanBeNull(testExpression) && !NullabilityContext.CanBeNull(valueExpression) && predicate.IsNot)
			{
				var withoutNull = new SqlPredicate.InSubQuery(testExpression, predicate.IsNot, predicate.SubQuery, true);

				var sc = new SqlSearchCondition(predicate.IsNot)
					.Add(new SqlPredicate.IsNull(testExpression, false))
					.Add(withoutNull);

				return Visit(sc);
			}

			return predicate;
		}

		protected internal override IQueryElement VisitSqlOrderByItem(SqlOrderByItem element)
		{
			var newElement = (SqlOrderByItem)base.VisitSqlOrderByItem(element);

			var wrapped = WrapBooleanExpression(newElement.Expression, includeFields : false);

			if (!ReferenceEquals(wrapped, newElement.Expression))
			{
				if (GetVisitMode(newElement) == VisitMode.Modify)
				{
					newElement.Expression = wrapped;
				}
				else
				{
					newElement = new SqlOrderByItem(wrapped, newElement.IsDescending, newElement.IsPositioned, newElement.NullsPosition);
				}
			}

			return newElement;
		}

		protected internal override IQueryElement VisitSqlSetExpression(SqlSetExpression element)
		{
			var newElement = (SqlSetExpression)base.VisitSqlSetExpression(element);

			while (newElement.Column is SqlCastExpression cast)
			{
				var newColumn = cast.Expression;
				var newValue  = newElement.Expression == null ? null : new SqlCastExpression(newElement.Expression, QueryHelper.GetDbDataType(newColumn, MappingSchema), null, false);

				if (GetVisitMode(newElement) == VisitMode.Modify)
				{
					newElement.Column = newColumn;
					newElement.Expression = newValue;
				}
				else
				{
					newElement = new SqlSetExpression(newColumn, newValue);
				}
			}

			var wrapped = newElement.Expression == null ? null : WrapBooleanExpression(newElement.Expression, includeFields : false, withNull: newElement.Column.CanBeNullable(NullabilityContext));

			if (!ReferenceEquals(wrapped, newElement.Expression))
			{
				if (wrapped != null)
					wrapped = (ISqlExpression)Optimize(wrapped);
				if (GetVisitMode(newElement) == VisitMode.Modify)
				{
					newElement.Expression = wrapped;
				}
				else
				{
					newElement = new SqlSetExpression(newElement.Column, wrapped);
				}
			}

			return newElement;
		}

		protected override ISqlExpression VisitSqlGroupByItem(ISqlExpression element)
		{
			var newItem = base.VisitSqlGroupByItem(element);

			return WrapBooleanExpression(newItem, includeFields: false);
		}

		/// <remarks>
		/// Over integral storage an interval is its stored amount, so the node simply disappears here. The read path
		/// turns the amount back into a <see cref="TimeSpan"/> through the operand's column descriptor, which
		/// <see cref="QueryHelper.GetColumnDescriptor(ISqlExpression)"/> reaches by looking through this node.
		/// <para>
		/// Which makes reaching that descriptor an invariant rather than a convenience: past this point the SQL value
		/// no longer says what unit it counts, so whatever wraps or rewrites it - a cast, a function, a projection
		/// into a derived table, a branch of a set operation - has to leave the descriptor reachable from the result.
		/// Where it does not, the statement stays valid and the value is read through the wrong conversion, which is
		/// the one failure the lowering cannot see for itself. <see cref="BasicSqlBuilder"/> refuses this node rather
		/// than rendering its operand for the same reason: a provider that never lowered it away should say so, not
		/// quietly emit a bare number where a duration was meant.
		/// </para>
		/// </remarks>
		protected internal override IQueryElement VisitSqlIntervalExpression(SqlIntervalExpression element)
		{
			return Visit(element.Value);
		}

		/// <summary>
		/// Whether an elapsed date difference can be lowered to a value here.
		/// </summary>
		/// <remarks>
		/// Declared beside the lowering it describes, and read by the member translator through
		/// <c>ITranslationContext.ProviderFlags</c>. The translator has to ask before it builds anything, because
		/// a difference it does not build stays an ordinary .NET subtraction and is computed on materialisation -
		/// and by the time this visitor runs, the read expression is already bound to its columns, so there is no
		/// going back.
		/// </remarks>
		public virtual bool CanLowerIntervalDifference => false;

		/// <summary>
		/// Whether a member of an elapsed date difference can be lowered. Defaults to whatever the difference
		/// itself can do.
		/// </summary>
		/// <remarks>
		/// Separate because one provider has only this half: Access counts elapsed units well enough to answer
		/// <c>TotalHours</c>, but its <c>DateDiff</c> is a 32-bit count that overflows once scaled to ticks, so
		/// the interval never becomes a value there.
		/// </remarks>
		public virtual bool CanLowerIntervalPart => CanLowerIntervalDifference;

		/// <summary>
		/// The finest unit this provider can actually resolve when it measures elapsed time. Defaults to
		/// <see cref="SqlIntervalUnit.Tick"/> - no loss.
		/// </summary>
		/// <remarks>
		/// Distinct from <see cref="FinestDateUnit"/>, which names the finest unit the provider's own difference
		/// function counts in. A provider may measure elapsed time some other way and still be limited: SQLite goes
		/// through <c>julianday</c>, whose double holds about 47 microseconds of a Julian day number, so its
		/// measurement is rounded to the millisecond however the value is stored.
		/// <para>
		/// Read by the member translator, which declines to build a <em>component</em> in a unit finer than this.
		/// Such a component is not merely less precise - it is identically zero, because the count it is taken
		/// from was rounded to a coarser unit first. Declining leaves the member to .NET, which has both dates and
		/// can answer exactly.
		/// </para>
		/// </remarks>
		public virtual SqlIntervalUnit IntervalResolution => SqlIntervalUnit.Tick;

		protected internal override IQueryElement VisitSqlTemporalArithmeticExpression(SqlTemporalArithmeticExpression element)
		{
			var lowered = LowerTemporalArithmetic(element);
			if (lowered != null)
				return Visit(lowered);

			return base.VisitSqlTemporalArithmeticExpression(element);
		}

		/// <summary>
		/// Lowers a date/time value shifted by an interval.
		/// </summary>
		/// <remarks>
		/// There is no default. A provider with a native interval type applies the operator directly; one that
		/// lowered the interval to a tick count has to spend that count through its own <c>DATEADD</c>, whose
		/// argument is usually a 32-bit integer and so cannot take ticks in one step.
		/// <para>
		/// Left alone, the node reaches the builder and is refused by name. That is the point of it existing: the
		/// generic binary handling would otherwise put a plain operator between a date and a number, which a
		/// database evaluates into something that still looks like a date.
		/// </para>
		/// </remarks>
		/// <returns><see langword="null"/> when the provider cannot express the shift.</returns>
		protected virtual ISqlExpression? LowerTemporalArithmetic(SqlTemporalArithmeticExpression element)
		{
			if (FinestDateUnit is not { } finest)
				return null;

			if (!SqlIntervalUnits.TryGetTicksRatio(finest, out var ticksPerFine, out var fineDenominator))
				return null;

			var longType = Factory.GetDbDataType(typeof(long));
			var ticks    = element.IsSubtract ? Factory.Multiply(longType, element.Interval, -1L) : element.Interval;

			// Days, then seconds within the day, then the sub-second part in the finest unit the provider counts.
			// Split this way because DATEADD and its equivalents take a 32-bit amount: a tick count overflows it
			// within minutes, while a day count covers millennia and each remainder is bounded by its own unit.
			var days      = TruncateDivide(ticks, TimeSpan.TicksPerDay);
			var seconds   = TruncateDivide(TruncateRemainder(ticks, TimeSpan.TicksPerDay), TimeSpan.TicksPerSecond);
			var remainder = TruncateRemainder(ticks, TimeSpan.TicksPerSecond);

			// A fine unit may be finer than a tick, in which case the remainder scales up rather than divides.
			var fine = fineDenominator != 1
				? Factory.Multiply(longType, remainder, fineDenominator)
				: TruncateDivide(remainder, ticksPerFine);

			var shifted = ShiftDate(SqlIntervalUnit.Day, days, element.Temporal);
			if (shifted == null)
				return null;

			shifted = ShiftDate(SqlIntervalUnit.Second, seconds, shifted);
			if (shifted == null)
				return null;

			return ShiftDate(finest, fine, shifted);
		}

		protected internal override IQueryElement VisitSqlIntervalDifferenceExpression(SqlIntervalDifferenceExpression element)
		{
			var lowered = LowerIntervalDifference(element);
			if (lowered != null)
				return Visit(lowered);

			return base.VisitSqlIntervalDifferenceExpression(element);
		}

		/// <summary>
		/// Lowers <c>End - Start</c> into the elapsed time as a value, in whatever form the read path turns back
		/// into a <see cref="TimeSpan"/>.
		/// </summary>
		/// <remarks>
		/// Over integral storage that form is the tick count, which is the default. A provider with a native
		/// interval type overrides this to produce one instead - the value is the same duration either way, and
		/// which representation is used is exactly the provider's business.
		/// </remarks>
		/// <returns><see langword="null"/> when the provider has no exact form, leaving the expression untranslated.</returns>
		protected virtual ISqlExpression? LowerIntervalDifference(SqlIntervalDifferenceExpression element)
		{
			return ElapsedTicks(element);
		}

		/// <summary>
		/// Elapsed ticks between two date/time values, exactly.
		/// </summary>
		/// <remarks>
		/// The one quantity that has to be a tick count rather than any equivalent duration, because
		/// <see cref="TimeSpan.Ticks"/> asks for it by name. Unlike the other members it is not a count of whole
		/// units that anchoring can correct, so a provider answers it or does not - one that cannot produce it
		/// <em>exactly</em>, at the resolution its own date type stores, returns <see langword="null"/> rather than
		/// approximating.
		/// <para>
		/// The default derives it from the counting primitives, so a provider that has those needs nothing more:
		/// whole elapsed days, plus the remainder counted in <see cref="FinestDateUnit"/>. Neither part can
		/// overflow - the day count is small for any range a <see cref="TimeSpan"/> can hold, and the remainder is
		/// measured across a window shorter than one day - which is what makes this preferable to counting the
		/// whole range in a fine unit. A provider with a single exact expression for the difference overrides it
		/// with that instead.
		/// </para>
		/// </remarks>
		/// <returns><see langword="null"/> when the provider has no exact form, leaving the expression untranslated.</returns>
		protected virtual ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			if (FinestDateUnit is not { } finest)
				return null;

			if (!SqlIntervalUnits.TryGetTicksRatio(finest, out var ticksPerFine, out var fineDenominator))
				return null;

			// Deliberately the raw boundary count, uncorrected: whatever it lands on, the remainder is measured
			// from that exact point, so an overshoot comes back as a negative remainder of the same size and the
			// two telescope. Correcting it here would only duplicate the count through a CASE for no gain.
			//
			// Days, not seconds, and the choice is what makes the whole CLR range reachable. A shift takes a
			// 32-bit amount on the providers that come through here, so the anchor's unit sets the ceiling: in
			// seconds that is 2^31 seconds, which is 68 years - close enough to ordinary that a person's age
			// crosses it - while in days it is far past what a date can hold. Nothing else grows in exchange: the
			// remainder spans at most one day, so counting it even in nanoseconds stays four orders below the
			// 64-bit limit, and the whole part reaches about 3.2e18 ticks against a limit of 9.2e18.
			var days = CountDateBoundaries(SqlIntervalUnit.Day, element.Start, element.End);
			if (days == null)
				return null;

			var anchor = ShiftDate(SqlIntervalUnit.Day, days, element.Start);
			if (anchor == null)
				return null;

			var remainder = CountDateBoundaries(finest, anchor, element.End);
			if (remainder == null)
				return null;

			var longType = Factory.GetDbDataType(typeof(long));

			// A fine unit may be finer than a tick - a nanosecond is a hundredth of one - so the ratio is applied
			// as a fraction. Plain division, not the truncating helper: the count is a whole number of ticks by
			// construction, so no rounding rule can disagree about the result.
			if (ticksPerFine != 1)
				remainder = Factory.Multiply(longType, remainder, ticksPerFine);

			if (fineDenominator != 1)
				remainder = Factory.Div(longType, remainder, Factory.Value(longType, fineDenominator));

			// Scaled in two steps rather than by the tick count of a day directly, and the reason is the type a
			// literal takes rather than arithmetic: 864000000000 is past the 32-bit range, and a provider that
			// reads such a literal as decimal makes the whole product decimal with it - the value stays right and
			// arrives as the wrong CLR type, which the reader then refuses. Both factors here fit in 32 bits, so
			// the product stays integral wherever the day count already is.
			var wholeTicks = Factory.Multiply(longType,
				Factory.Multiply(longType, days, (long)TimeSpan.TicksPerDay / TimeSpan.TicksPerSecond),
				TimeSpan.TicksPerSecond);

			return Factory.Add(longType, wholeTicks, remainder);
		}

		/// <summary>
		/// Shifts a date/time value by a whole number of units - the provider's <c>DATEADD</c>.
		/// </summary>
		/// <remarks>
		/// Must be exact: the anchor correction in <see cref="IntervalLowering"/> shifts by a computed count and
		/// compares the result against the original, so an approximate shift would produce an off-by-one count.
		/// </remarks>
		protected virtual ISqlExpression? ShiftDate(SqlIntervalUnit unit, ISqlExpression amount, ISqlExpression date)
		{
			return null;
		}

		/// <summary>
		/// Counts unit boundaries crossed between two date/time values - the provider's <c>DATEDIFF</c>.
		/// </summary>
		/// <remarks>
		/// Boundary counting, not elapsed time. It is deliberately the wrong answer on its own: it is the cheap
		/// starting estimate that the anchor correction turns into the elapsed count, and being a count of whole
		/// units it cannot overflow the way a fine-grained difference over the same range would.
		/// </remarks>
		protected virtual ISqlExpression? CountDateBoundaries(SqlIntervalUnit unit, ISqlExpression start, ISqlExpression end)
		{
			return null;
		}

		/// <summary>
		/// Finest unit this provider can count date boundaries in, used for the fractional part of a
		/// <c>Total*</c> member. <see langword="null"/> means totals of a date difference are not supported.
		/// </summary>
		/// <remarks>
		/// Only ever applied to a window shorter than one of the requested units, so it cannot overflow however
		/// far apart the two dates are - which is what makes a total expressible at all. Taking the whole
		/// difference in this unit would overflow: a century in ticks is more than <see cref="long"/> holds.
		/// </remarks>
		protected virtual SqlIntervalUnit? FinestDateUnit => null;

		/// <summary>
		/// Whether <see cref="ElapsedTicks"/> is fine enough to answer the individual members, or only the
		/// difference taken as a whole. Defaults to yes.
		/// </summary>
		/// <remarks>
		/// A tick count answers every member with far less SQL, but only where it resolves what the provider
		/// stores. Access counts seconds while an OLE Automation date holds fractions of one, so its count can sit
		/// a second from the truth - enough to move <c>Hours</c> across a boundary - and counting each unit with
		/// the correction against the actual dates stays exact there. The tick count still answers the difference
		/// itself, which has no other form.
		/// </remarks>
		protected virtual bool ElapsedTicksResolveMembers => true;

		protected internal override IQueryElement VisitSqlIntervalPartExpression(SqlIntervalPartExpression element)
		{
			// Lower before visiting children: the child interval carries the unit this needs, and visiting it
			// first would unwrap it to a bare amount.
			var lowered = LowerIntervalPart(element);
			if (lowered != null)
				return Visit(lowered);

			return base.VisitSqlIntervalPartExpression(element);
		}

		/// <summary>
		/// Lowers an interval part to SQL. The default uses <see cref="IntervalLowering"/>'s integral-storage
		/// strategy; providers with a native interval type override this to use it instead.
		/// </summary>
		/// <returns><see langword="null"/> when the part cannot be produced exactly, leaving it untranslated.</returns>
		protected virtual ISqlExpression? LowerIntervalPart(SqlIntervalPartExpression element)
		{
			// A part of a computed difference is answered by counting elapsed units directly. .NET defines the
			// member as _ticks / TicksPerUnit, and the anchor count reproduces that quotient exactly - forming a
			// tick count first would only reintroduce the overflow and precision limits it exists to avoid.
			// Unwrap first: the translator's placeholder may carry a nullability wrapper around the difference.
			if (QueryHelper.UnwrapNullablity(element.Interval) is SqlIntervalDifferenceExpression difference)
			{
				// Ticks is the exception: it is the whole difference, not a count of units, so counting cannot
				// answer it. It is the same quantity the provider produces for a bare difference over integral
				// storage.
				if (element is { Unit: SqlIntervalUnit.Tick, Kind: SqlIntervalPartKind.Total })
				{
					var ticks = ElapsedTicks(difference);

					return ticks == null ? null : Factory.Cast(ticks, element.Type);
				}

				// Every member follows from the tick count, and .NET defines them that way, so where the count is
				// fine enough this is both the shorter SQL and the closer reading of the CLR.
				if (ElapsedTicksResolveMembers && ElapsedTicks(difference) is { } exact)
					return IntervalLowering.FromTicks(Factory, exact, element, TruncateDivide, TruncateRemainder);

				// The two ask for different counts. A component is the whole number itself, so it needs the
				// corrected one - nothing follows it to absorb an overshoot. A total is followed by its leftover,
				// which is measured from wherever the anchor landed and cancels the overshoot, so it takes the raw
				// count and leaves the correction out of the expression entirely.
				if (element.Kind == SqlIntervalPartKind.Component)
				{
					var whole = IntervalLowering.ElapsedUnits(Factory, difference, element.Unit, CountDateBoundaries, ShiftDate);

					if (whole != null)
						return Factory.Cast(IntervalLowering.WrapComponent(Factory, whole, element.Unit, element.Within, TruncateRemainder), element.Type);
				}
				else
				{
					var total = IntervalLowering.ElapsedTotal(
						Factory, difference, element.Unit, FinestDateUnit, CountDateBoundaries, ShiftDate);

					if (total != null)
						return Factory.Cast(total, element.Type);
				}

				// Counting could not reach this unit either. A coarse tick count still beats leaving the member
				// untranslated, which is what it would have been before any of this existed.
				var elapsed = ElapsedTicks(difference);

				return elapsed == null ? null : IntervalLowering.FromTicks(Factory, elapsed, element, TruncateDivide, TruncateRemainder);
			}

			return IntervalLowering.LowerPart(Factory, element, TruncateDivide, TruncateRemainder);
		}

		/// <summary>
		/// Integer division truncating toward zero.
		/// </summary>
		/// <remarks>
		/// The default is the division itself, which is what dividing two integers means in SQL and matches CLR
		/// integer division on negatives. A provider whose division is not integral overrides it - MySQL and
		/// DuckDB return a fraction, Access has no integer division at all - and so does one whose truncation is
		/// spelled its own way.
		/// </remarks>
		protected virtual ISqlExpression TruncateDivide(ISqlExpression value, long divisor)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			return Factory.Div(longType, value, Factory.Value(longType, divisor));
		}

		/// <summary>
		/// Remainder of the same truncating division, which is what <c>%</c> means on integers in most databases
		/// and what the CLR operator means.
		/// </summary>
		/// <remarks>
		/// Kept separate from <see cref="TruncateDivide"/> because composing it out of one - as
		/// <c>value - trunc(value / divisor) * divisor</c> - repeats the value three times, and the components of
		/// an interval nest two of these, so the repetition multiplies. A provider whose remainder disagrees on
		/// negatives, or that spells it as a function, overrides this.
		/// </remarks>
		protected virtual ISqlExpression TruncateRemainder(ISqlExpression value, long divisor)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			return Factory.Mod(longType, value, Factory.Value(longType, divisor));
		}

		protected internal override IQueryElement VisitSqlCastExpression(SqlCastExpression element)
		{
			var newElement = base.VisitSqlCastExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var converted = ConvertConversion(element);
			if (!ReferenceEquals(converted, element))
			{
				return Visit(Optimize(converted));
			}

			return element;
		}

		protected internal override IQueryElement VisitSqlCoalesceExpression(SqlCoalesceExpression element)
		{
			var newElement = base.VisitSqlCoalesceExpression(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var wrappedElement = WrapBooleanCoalesceItems(element, newElement);
			if (wrappedElement != null)
				return Visit(wrappedElement);

			var converted = ConvertCoalesce(element);

			if (!ReferenceEquals(converted, element))
				return Visit(Optimize(converted));

			return element;
		}

		protected internal override IQueryElement VisitSqlConcatExpression(SqlConcatExpression element)
		{
			var newElement = base.VisitSqlConcatExpression(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var converted = ConvertConcat(element);

			if (!ReferenceEquals(converted, element))
				return Visit(Optimize(converted));

			return element;
		}

		protected virtual SqlCoalesceExpression? WrapBooleanCoalesceItems(SqlCoalesceExpression element, IQueryElement newElement, bool forceConvert = false)
		{
			var isWrapped = false;
			ISqlExpression[]? wrappedExpressions = null;

			for (var i = 0; i < element.Expressions.Length; i++)
			{
				var wrapped = WrapBooleanExpression(element.Expressions[i], includeFields : false, forceConvert: forceConvert);

				if (!ReferenceEquals(wrapped, element.Expressions[i]))
				{
					isWrapped = true;

					if (GetVisitMode(newElement) == VisitMode.Modify)
					{
						element.Expressions[i] = wrapped;
					}
					else
					{
						if (wrappedExpressions == null)
						{
							wrappedExpressions = new ISqlExpression[element.Expressions.Length];
							Array.Copy(element.Expressions, wrappedExpressions, wrappedExpressions.Length);
						}

						wrappedExpressions[i] = wrapped;
					}
				}
			}

			if (isWrapped)
			{
				return GetVisitMode(newElement) == VisitMode.Modify
					? element
					: new SqlCoalesceExpression(wrappedExpressions!);
			}

			return null;
		}

		#endregion Visitor overrides

		public virtual ISqlExpression ConvertCoalesce(SqlCoalesceExpression element)
		{
			var reduced = RemoveNullValues(element);
			if (reduced is not SqlCoalesceExpression coalesce)
				return reduced;

			var type = QueryHelper.GetDbDataType(coalesce.Expressions[0], MappingSchema);
			return new SqlFunction(type, "Coalesce", parametersNullability: ParametersNullabilityType.IfAllParametersNullable, coalesce.Expressions);
		}

		/// <summary>
		/// Removes NULL-literal operands from a COALESCE operand list — a literal NULL can never be the
		/// value COALESCE returns, so it is redundant. Returns the sole surviving operand when only one
		/// remains, a reduced <see cref="SqlCoalesceExpression"/> over the survivors when several remain,
		/// or the last operand when every operand is a NULL literal. Returns <paramref name="element"/>
		/// unchanged when it has no NULL-literal operands.
		/// Shared so providers that fold COALESCE into a native construct (Informix <c>Nvl</c>, Access
		/// <c>IIF</c>) apply the same normalization the base <see cref="ConvertCoalesce"/> does before
		/// folding; otherwise a no-op guard such as <c>Coalesce(x, NULL)</c> folds to <c>Nvl(x, NULL)</c>
		/// / <c>IIF(x IS NULL, NULL, x)</c> (issue #5531).
		/// </summary>
		protected ISqlExpression RemoveNullValues(SqlCoalesceExpression element)
		{
			List<ISqlExpression>? kept = null;

			for (var i = 0; i < element.Expressions.Length; i++)
			{
				if (element.Expressions[i] is SqlValue { Value: null })
				{
					if (kept == null)
					{
						kept = new List<ISqlExpression>(element.Expressions.Length - 1);
						for (var j = 0; j < i; j++)
							kept.Add(element.Expressions[j]);
					}
				}
				else
				{
					kept?.Add(element.Expressions[i]);
				}
			}

			if (kept == null)
				return element;

			if (kept.Count == 0)
				return element.Expressions[^1];

			if (kept.Count == 1)
				return kept[0];

			return new SqlCoalesceExpression(kept.ToArray());
		}

		/// <summary>
		/// When <see langword="true"/> (default), <see cref="ConvertConcat"/> wraps every non-string
		/// operand in an explicit <c>CAST(... AS VARCHAR(N))</c> before adding it to the concat chain.
		/// Required for providers whose concat operator is <c>+</c> (SQL Server pre-2025, SqlCe,
		/// Access) — SQL-standard data-type precedence would otherwise try to coerce
		/// string operands to the non-string side's type. Most providers whose final concat operator
		/// is <c>||</c> (PostgreSQL / Oracle / SQLite / SAP HANA / DuckDB / Firebird / DB2 / Informix /
		/// SQL Server 2025+) or <c>CONCAT(...)</c> function (MySQL / ClickHouse) auto-coerce
		/// non-string operands and override this to <see langword="false"/> for cleaner SQL.
		/// Sybase ASE is the exception: it emits <c>||</c> but keeps this <see langword="true"/>,
		/// since ASE requires an explicit <c>convert()</c> for non-character operands under both
		/// <c>+</c> and <c>||</c>.
		/// </summary>
		protected virtual bool ConcatRequiresExplicitStringCast => true;

		public virtual ISqlExpression ConvertConcat(SqlConcatExpression element)
		{
			// Single-operand concat is identity — no transformation needed.
			if (element.Expressions.Length == 1)
				return element.Expressions[0];

			// Flatten same-semantic nested SqlConcatExpression operands. `string + string + string`
			// arrives as `Add(Add(a, b), c)` and `TranslateBinaryStringConcat` recurses via
			// `string.Concat(left, right)`, producing a nested SqlConcatExpression. The SqlBuilder
			// emits each operand verbatim, so a nested operand would render as nested CONCAT(...)
			// (Function style) or as redundantly-parenthesised `||` / `+` chains. Flatten before
			// the cast / coalesce pass so each operand reaches the wrap logic individually.
			// Different `PreserveNull` semantics don't compose this way (a strict-null inner
			// inside a null-as-empty outer can't be flattened without changing observable
			// nullability), so only matching-semantic children fold in.
			element = FlattenNestedConcat(element);

			if (element.Expressions.Length == 1)
				return element.Expressions[0];

			ISqlExpression[]? transformed = null;

			for (var i = 0; i < element.Expressions.Length; i++)
			{
				var original = element.Expressions[i];
				var item     = original;

				// Cast non-string operands to string when the provider's concat operator
				// requires an explicit string type (SQL Server pre-2025 / SqlCe / Access `+`).
				// `||` / `CONCAT(...)` providers override `ConcatRequiresExplicitStringCast` to
				// false and let SQL auto-coerce. The cast result has SystemType == string, so
				// re-entry from `Visit` is naturally idempotent here.
				var systemType = item.SystemType;
				if (systemType != typeof(string) && ConcatRequiresExplicitStringCast)
				{
					var len = systemType == null || systemType == typeof(object)
						? 100
						: GetMaxDisplaySize(MappingSchema.GetDataType(systemType).Type) ?? 100;
					item = PseudoFunctions.MakeCast(item, new DbDataType(typeof(string), DataType.VarChar, null, len));
				}

				// For null-as-empty semantic, wrap each *nullable* operand in Coalesce(item, '').
				// Non-nullable operands don't need the wrap, and skipping them also avoids an
				// infinite re-entry loop: `SqlExpressionOptimizerVisitor.VisitSqlCoalesceExpression`
				// collapses `Coalesce(non_null, '')` straight back to `non_null` (the '' fallback
				// is unreachable), so a wrapped non-nullable operand would re-appear as a "naked"
				// operand on the next `Visit(Optimize(converted))` pass and get wrapped again.
				// The remaining idempotence check (`IsConcatCoalesceWrap`) handles nullable
				// operands whose Coalesce wrap survived the optimizer pass (the base
				// `VisitSqlCoalesceExpression` lowers it to `SqlFunction("Coalesce", _, '')`).
				if (!element.PreserveNull
					&& item.CanBeNullable(NullabilityContext)
					&& !IsConcatCoalesceWrap(item))
				{
					var itemType = QueryHelper.GetDbDataType(item, MappingSchema);
					item = new SqlCoalesceExpression(item, new SqlValue(itemType, string.Empty));
				}

				if (!ReferenceEquals(item, original))
				{
					if (transformed == null)
					{
						transformed = new ISqlExpression[element.Expressions.Length];
						Array.Copy(element.Expressions, transformed, i);
					}
				}

				if (transformed != null)
					transformed[i] = item;
			}

			if (transformed == null)
				return element;

			return new SqlConcatExpression(element.PreserveNull, transformed);
		}

		static SqlConcatExpression FlattenNestedConcat(SqlConcatExpression element)
		{
			var hasNested = false;
			for (var i = 0; i < element.Expressions.Length; i++)
			{
				if (element.Expressions[i] is SqlConcatExpression sub && sub.PreserveNull == element.PreserveNull)
				{
					hasNested = true;
					break;
				}
			}

			if (!hasNested)
				return element;

			var flat = new List<ISqlExpression>(element.Expressions.Length);
			foreach (var op in element.Expressions)
			{
				if (op is SqlConcatExpression sub && sub.PreserveNull == element.PreserveNull)
					flat.AddRange(sub.Expressions);
				else
					flat.Add(op);
			}

			return new SqlConcatExpression(element.PreserveNull, flat.ToArray());
		}

		static bool IsConcatCoalesceWrap(ISqlExpression expr)
		{
			// `SqlCoalesceExpression(item, '')` is the shape we add. Between visits the base
			// `VisitSqlCoalesceExpression` lowers it to `SqlFunction("Coalesce", item, '')`,
			// Access (which has no `COALESCE`) further rewrites that to
			// `SqlConditionExpression(IsNull(item), '', item)`, and the SqlExpressionOptimizer
			// fuses the condition with a nested SqlConditionExpression into a SqlCaseExpression
			// whose leading WHEN-clause result is the `''` fallback. Detect all four —
			// otherwise a re-entrant pass would wrap the already-Coalesce'd operand in another
			// Coalesce and recurse forever (exponential expression growth on Access).
			return expr switch
			{
				SqlCoalesceExpression          { Expressions: [_, SqlValue { Value: "" }] } => true,
				SqlFunction { Name: "Coalesce", Parameters:   [_, SqlValue { Value: "" }] } => true,
				SqlConditionExpression
				{
					Condition:  SqlPredicate.IsNull { IsNot: false },
					TrueValue:  SqlValue { Value: "" },
				} => true,
				SqlCaseExpression { Cases: [{ ResultExpression: SqlValue { Value: "" } }, ..] } => true,
				_                                                                           => false,
			};
		}

		public virtual ISqlExpression ConvertSqlExpression(SqlExpression element)
		{
			return element;
		}

		public virtual ISqlExpression ConvertSqlExtendedFunction(SqlExtendedFunction func)
		{
			switch (func.FunctionName)
			{
				case "MAX":
				case "MIN":
				{
					if (func.SystemType == typeof(bool) || func.SystemType == typeof(bool?))
					{
						if (func.Arguments[0].Expression is not ISqlPredicate predicate)
						{
							predicate = new SqlPredicate.Expr(func.Arguments[0].Expression);
						}

						var argument = func.Arguments[0].WithExpression(new SqlConditionExpression(predicate, new SqlValue(1), new SqlValue(0)));
						var newFunc  = func.WithArguments(new[] { argument }, func.ArgumentsNullability);
						newFunc = newFunc.WithType(MappingSchema.GetDbDataType(typeof(int)));
						return newFunc;
					}

					break;
				}
			}

			return func;
		}

		public virtual ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "MAX":
				case "MIN":
				{
					if (func.SystemType == typeof(bool) || func.SystemType == typeof(bool?))
					{
						if (func.Parameters[0] is not ISqlPredicate predicate)
						{
							predicate = new SqlPredicate.Expr(func.Parameters[0]);
						}

						return new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), func.Name, new SqlConditionExpression(predicate, new SqlValue(1), new SqlValue(0)));
					}

					break;
				}

				case PseudoFunctions.CONVERT_FORMAT:
				{
					return new SqlFunction(func.Type, "Convert", func.Parameters[0], func.Parameters[2], func.Parameters[3]);
				}

				case PseudoFunctions.TO_LOWER: return func.WithName("Lower");
				case PseudoFunctions.TO_UPPER: return func.WithName("Upper");
				case PseudoFunctions.REPLACE: return func.WithName("Replace");
				case PseudoFunctions.LENGTH: return func.WithName("Length");
			}

			return func;
		}

		public virtual ISqlPredicate ConvertLikePredicate(SqlPredicate.Like predicate)
		{
			return predicate;
		}

		ISqlPredicate EmulateNullability(SqlPredicate.InSubQuery inPredicate)
		{
			var sc = new SqlSearchCondition(true);

			var testExpr = inPredicate.Expr1;

			var intTestSubQuery = inPredicate.SubQuery.CloneQuery();
			intTestSubQuery = WrapIfNeeded(intTestSubQuery);
			var inSubqueryExpr = intTestSubQuery.Select.Columns[0].Expression;

			intTestSubQuery.Select.Columns.Clear();
			intTestSubQuery.Select.AddNewColumn(new SqlValue(1));
			intTestSubQuery.Where.SearchCondition.AddIsNull(inSubqueryExpr);

			// The non-null branch tests `testExpr IN (subQuery)`. A NULL row in the subquery makes that IN
			// return UNKNOWN for a value not otherwise present (SQL three-valued logic), dropping the row —
			// but LINQ's Contains treats a NULL element as simply non-matching (the null testExpr case is
			// handled by the branch above). When the subquery column is nullable, filter the NULLs out so the
			// membership test is a clean TRUE/FALSE. Matters for providers without correlated subqueries,
			// which can't fall back to NOT EXISTS.
			var valueSubQuery = inPredicate.SubQuery.CloneQuery();
			valueSubQuery = WrapIfNeeded(valueSubQuery);

			if (NullabilityContext.CanBeNull(inPredicate.SubQuery.Select.Columns[0].Expression))
				valueSubQuery.Where.SearchCondition.AddIsNotNull(valueSubQuery.Select.Columns[0].Expression);

			sc.AddAnd(sub => sub
					.AddIsNull(testExpr)
					.Add(new SqlPredicate.InSubQuery(new SqlValue(1), false, intTestSubQuery, doNotConvert: true))
				)
				.AddAnd(sub => sub
					.AddIsNotNull(testExpr)
					.Add(new SqlPredicate.InSubQuery(testExpr, false, valueSubQuery, doNotConvert: true))
				);

			var result = Optimize(sc.MakeNot(inPredicate.IsNot));

			return (ISqlPredicate)result;
		}

		static SelectQuery WrapIfNeeded(SelectQuery selectQuery)
		{
			if (selectQuery.Select.HasModifier || selectQuery.HasGroupBy || selectQuery.HasSetOperators || QueryHelper.IsAggregationQuery(selectQuery))
			{
				var newQuery = new SelectQuery();
				newQuery.From.Tables.Add(new SqlTableSource(selectQuery, null));

				foreach (var column in selectQuery.Select.Columns)
				{
					newQuery.Select.AddNew(column);
				}

				selectQuery = newQuery;
			}

			return selectQuery;
		}

		/// <summary>
		/// Brings a membership test between two declared durations to common terms, or returns <see langword="null"/>
		/// when there is nothing to reconcile.
		/// </summary>
		/// <remarks>
		/// A comparison is reconciled while it is translated, because both operands are still expressions there. A
		/// membership test is not: it becomes a predicate over one expression and a sub-query, and the two numbers
		/// are then compared as they stand - 1800 against 18000000000 is the same ninety minutes written twice.
		/// Neither value is known while the query is built, so no conversion of a constant can bridge them.
		/// <para>
		/// Both sides go to ticks rather than one side to the other's unit: converting the test down to a coarser
		/// unit would truncate it, and a duration that the column cannot represent would then match a stored value
		/// it does not equal.
		/// </para>
		/// <para>
		/// The sub-query is cloned before its column is rewritten unless this visitor owns it outright, the same
		/// condition <see cref="ConvertToExists"/> applies for the same reason - it may be shared, and a statement
		/// reached through the query cache must not be edited in place.
		/// </para>
		/// </remarks>
		ISqlPredicate? ReconcileDurationUnits(SqlPredicate.InSubQuery predicate)
		{
			if (predicate.SubQuery.Select.Columns is not [var singleColumn])
				return null;

			var testDescriptor   = QueryHelper.GetColumnDescriptor(predicate.Expr1);
			var columnDescriptor = QueryHelper.GetColumnDescriptor(singleColumn.Expression);

			if (testDescriptor?.DurationUnit is not { } testUnit || columnDescriptor?.DurationUnit is not { } columnUnit || testUnit == columnUnit)
				return null;

			// The two meet in the finer of their units, not in ticks. Either way the coarser one is the one that
			// moves - a coarser unit converts into a finer exactly, while the other direction has a remainder to
			// drop - so the finer column keeps the amount it was stored as and stays a column an index can be
			// walked by, and only one side is multiplied rather than both.
			//
			// Ticks are the fallback and were the whole rule before: they are finer than anything a column can be
			// declared in, so meeting there is always safe. It is simply further than the two need to go.
			var meeting = SqlIntervalUnits.IsFinerThan(SqlIntervalType.ToIntervalUnit(columnUnit), SqlIntervalType.ToIntervalUnit(testUnit))
				? columnUnit
				: testUnit;

			var subQuery = predicate.SubQuery;

			if (GetVisitMode(subQuery) == VisitMode.Transform)
				subQuery = subQuery.CloneQuery();

			var subQueryColumn = subQuery.Select.Columns[0];

			subQueryColumn.Expression = TotalIn(subQueryColumn.Expression, columnDescriptor, columnUnit, meeting);

			return new SqlPredicate.InSubQuery(
				TotalIn(predicate.Expr1, testDescriptor, testUnit, meeting),
				predicate.IsNot,
				subQuery,
				predicate.DoNotConvert);
		}

		/// <summary>
		/// A declared duration counted in <paramref name="meeting"/> rather than in the unit it is stored as. Where
		/// the two are the same the count is the stored amount, and the column is left as it is.
		/// </summary>
		ISqlExpression TotalIn(ISqlExpression expression, ColumnDescriptor descriptor, DurationUnit unit, DurationUnit meeting)
		{
			var interval = new SqlIntervalExpression(expression, descriptor.GetDbDataType(true), SqlIntervalType.ForDuration(unit));

			return new SqlIntervalPartExpression(interval, SqlIntervalType.ToIntervalUnit(meeting), SqlIntervalPartKind.Total, Factory.GetDbDataType(typeof(long)));
		}

		ISqlPredicate ConvertToExists(SqlPredicate.InSubQuery inPredicate)
		{
			ISqlExpression[] testExpressions;
			if (inPredicate.Expr1 is SqlRowExpression sqlRow)
			{
				testExpressions = sqlRow.Values;
			}
			else
			{
				testExpressions = [inPredicate.Expr1];
			}

			var subQuery = inPredicate.SubQuery;

			if (inPredicate.SubQuery.Where.SearchCondition.IsOr)
				throw new InvalidOperationException("Not expected root SearchCondition.");

			if (GetVisitMode(subQuery) == VisitMode.Transform || subQuery.Where.SearchCondition.IsOr)
			{
				subQuery = subQuery.CloneQuery();
				subQuery.Where.EnsureConjunction();
			}

			subQuery = WrapIfNeeded(subQuery);

			var predicates = new List<ISqlPredicate>(testExpressions.Length);

			var sc = new SqlSearchCondition(false);

			for (var i = 0; i < testExpressions.Length; i++)
			{
				var testValue = testExpressions[i];
				var expr      = subQuery.Select.Columns[i].Expression;

				predicates.Add(new SqlPredicate.ExprExpr(
					testValue,
					SqlPredicate.Operator.Equal,
					expr,
					DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null));
			}

			subQuery.Select.Columns.Clear();
			subQuery.Where.SearchCondition.AddRange(predicates);

			sc.AddExists(subQuery, inPredicate.IsNot);

			var result = Optimize(sc);

			result = Visit(result);

			return (ISqlPredicate)result;
		}

		public virtual ISqlPredicate ConvertBetweenPredicate(SqlPredicate.Between between)
		{
			var newPredicate = new SqlSearchCondition()
				.AddGreaterOrEqual(between.Expr1, between.Expr2, CompareNulls.LikeSql)
				.AddLessOrEqual(between.Expr1, between.Expr3, CompareNulls.LikeSql)
				.MakeNot(between.IsNot);

			return newPredicate;
		}

		public virtual IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "+":
				{
					if (element.Expr2 is SqlUnaryExpression { Operation: SqlUnaryOperation.Negation, Expr: var expr2 })
					{
						return new SqlBinaryExpression(
							element.Type,
							element.Expr1,
							"-",
							expr2,
							element.Precedence);
					}

					break;
				}

				case "-":
				{
					if (element.Expr2 is SqlUnaryExpression { Operation: SqlUnaryOperation.Negation, Expr: var expr2 })
					{
						return new SqlBinaryExpression(
							element.Type,
							element.Expr1,
							"+",
							expr2,
							element.Precedence);
					}

					break;
				}

				case "*":
				{
					if (element.Expr2 is SqlValue { Value: -1 })
					{
						return new SqlUnaryExpression(
							element.Type,
							element.Expr1,
							SqlUnaryOperation.Negation,
							Precedence.Unary);
					}

					if (element.Expr1 is SqlValue { Value: -1 })
					{
						return new SqlUnaryExpression(
							element.Type,
							element.Expr2,
							SqlUnaryOperation.Negation,
							Precedence.Unary);
					}

					break;
				}

				case "/":
				{
					if (element.Expr2 is SqlValue { Value: -1 })
					{
						return new SqlUnaryExpression(
							element.Type,
							element.Expr1,
							SqlUnaryOperation.Negation,
							Precedence.Unary);
					}

					break;
				}
			}

			return element;
		}

		public virtual ISqlExpression ConvertSqlUnaryExpression(SqlUnaryExpression element)
		{
			if (element is
				{
					Operation: SqlUnaryOperation.Negation,
					Expr: SqlUnaryExpression
					{
						Operation: SqlUnaryOperation.Negation,
						Expr: var expr,
					},
				})
			{
				return expr;
			}

			return element;
		}

		protected virtual ISqlExpression ConvertSqlCondition(SqlConditionExpression element)
		{
			var trueValue  = WrapBooleanExpression(element.TrueValue, includeFields : false);
			var falseValue = WrapBooleanExpression(element.FalseValue, includeFields : false);

			if (!ReferenceEquals(trueValue, element.TrueValue) || !ReferenceEquals(falseValue, element.FalseValue))
			{
				return new SqlConditionExpression(element.Condition, trueValue, falseValue);
			}

			return element;
		}

		protected virtual ISqlExpression ConvertSqlCaseExpression(SqlCaseExpression element)
		{
			if (element.ElseExpression != null)
			{
				var elseExpression = WrapBooleanExpression(element.ElseExpression, includeFields : true);

				if (!ReferenceEquals(elseExpression, element.ElseExpression))
				{
					return new SqlCaseExpression(element.Type, element.Cases, elseExpression);
				}
			}

			return element;
		}

		protected virtual SqlCaseExpression.CaseItem ConvertCaseItem(SqlCaseExpression.CaseItem newElement)
		{
			var resultExpr = WrapBooleanExpression(newElement.ResultExpression, includeFields : true);

			if (!ReferenceEquals(resultExpr, newElement.ResultExpression))
			{
				newElement = new SqlCaseExpression.CaseItem(newElement.Condition, resultExpr);
			}

			return newElement;
		}

		protected virtual ISqlExpression WrapBooleanExpression(ISqlExpression expr, bool includeFields, bool forceConvert = false, bool withNull = true)
		{
			if (expr.SystemType == typeof(bool)
				|| expr.SystemType == typeof(bool?))
			{
				var unwrapped = QueryHelper.UnwrapNullablity(expr);

				var wrap = includeFields && unwrapped.ElementType is QueryElementType.Column or QueryElementType.SqlField or QueryElementType.SqlCteTableField;
				if (!wrap && unwrapped.IsPredicate())
				{
					if (unwrapped.TryEvaluateExpression(EvaluationContext, out var res))
					{
						if (res is bool booleanValue)
						{
							return new SqlValue(booleanValue);
						}
						else if (res is null)
						{
							return new SqlValue(typeof(bool?), null);
						}
					}

					wrap = !SqlProviderFlags.SupportsBooleanType || (!withNull && unwrapped.CanBeNullableOrUnknown(NullabilityContext, withoutUnknownErased: true)) || forceConvert;
				}

				if (wrap)
				{
					var predicate = unwrapped switch
					{
						SqlParameterizedExpressionBase { IsPredicate: true } => new SqlPredicate.Expr(expr),
						ISqlPredicate isp                                    => isp,
						_                                                    => ConvertToBooleanSearchCondition(expr),
					};

					var trueValue  = new SqlValue(true);
					var falseValue = new SqlValue(false);

					if ((forceConvert || !SqlProviderFlags.SupportsBooleanType) && withNull && expr.CanBeNullableOrUnknown(NullabilityContext, false))
					{
						var toType = QueryHelper.GetDbDataType(expr, MappingSchema);

						expr = new SqlCaseExpression(
							toType,
							new SqlCaseExpression.CaseItem[]
							{
								new(predicate, trueValue),
								new(new SqlPredicate.Not(predicate), falseValue),
							},
							new SqlValue(toType, null));
					}
					else if (!withNull || !SqlProviderFlags.SupportsBooleanType || forceConvert)
					{
						expr = new SqlConditionExpression(predicate, trueValue, falseValue);
					}

					expr = (ISqlExpression)Visit(expr);
				}
			}

			return expr;
		}

		protected virtual ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			if (!SupportsNullInColumn)
			{
				var unwrappedExpr = QueryHelper.UnwrapNullablity(expr);

				if (unwrappedExpr is SqlValue sqlValue && sqlValue.Value == null)
				{
					return new SqlCastExpression(sqlValue, QueryHelper.GetDbDataType(sqlValue, MappingSchema), null, true);
				}
				else if (unwrappedExpr is SqlParameter { IsQueryParameter: false } sqlParameter)
				{
					var paramValue = sqlParameter.GetParameterValue(EvaluationContext.ParameterValues);

					if (paramValue.ProviderValue == null)
						return new SqlCastExpression(sqlParameter, QueryHelper.GetDbDataType(sqlParameter, MappingSchema), null, true);
				}
			}

			return expr;
		}

		#region DataTypes

		protected virtual int? GetMaxLength(DbDataType type) { return SqlDataType.GetMaxLength(type.DataType); }
		protected virtual int? GetMaxPrecision(DbDataType type) { return SqlDataType.GetMaxPrecision(type.DataType); }
		protected virtual int? GetMaxScale(DbDataType type) { return SqlDataType.GetMaxScale(type.DataType); }
		protected virtual int? GetMaxDisplaySize(DbDataType type) { return SqlDataType.GetMaxDisplaySize(type.DataType); }

		/// <summary>
		/// Implements <see cref="SqlCastExpression"/> conversion.
		/// </summary>
		protected virtual ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var toDataType = cast.ToType;

			if (!cast.IsMandatory && cast.SystemType == typeof(string))
			{
				object? value = cast.Expression is SqlValue sqlValue
					? sqlValue.Value
					: cast.Expression is SqlParameter { IsQueryParameter: false } param
						? param.GetParameterValue(EvaluationContext.ParameterValues).ProviderValue
						: null;

				if (value is char charValue)
					return new SqlValue(cast.Type, charValue.ToString());
				if (value is string stringValue)
					return new SqlValue(cast.Type, stringValue);
			}

			var fromDbType = QueryHelper.GetDbDataType(cast.Expression, MappingSchema);

			if (toDataType.Length > 0)
			{
				var maxLength = toDataType.SystemType == typeof(string) ? GetMaxDisplaySize(fromDbType) : GetMaxLength(fromDbType);
				var newLength = maxLength is not null and >= 0 ? Math.Min(toDataType.Length ?? 0, maxLength.Value) : fromDbType.Length;

				var newDataType = toDataType.WithLength(newLength);
				if (!newDataType.EqualsDbOnly(toDataType))
				{
					return new SqlCastExpression(cast.Expression, newDataType, cast.FromType);
				}
			}
			else if (!cast.IsMandatory && fromDbType.SystemType == typeof(short) && toDataType.SystemType == typeof(int))
			{
				return cast.Expression;
			}

			return cast;
		}

		#endregion

		#region SqlRow

		protected ISqlPredicate ConvertRowExprExpr(SqlPredicate.ExprExpr predicate, EvaluationContext context)
		{
			var op = predicate.Operator;
			var feature = op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual
				? RowFeature.Equality
				: op is SqlPredicate.Operator.Overlaps
					? RowFeature.Overlaps
					: RowFeature.Comparisons;

			var expr2 = QueryHelper.UnwrapNullablity(predicate.Expr2);

			switch (expr2)
			{
				// ROW(a, b) IS [NOT] NULL
				case SqlValue { Value: null }:
				{
					if (op is not (SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual))
						throw new LinqToDBException("Null SqlRow is only allowed in equality comparisons");

					if (ConvertRowIsNullPredicate((SqlRowExpression)predicate.Expr2, op is SqlPredicate.Operator.NotEqual, out var rowIsNullFallback))
					{
						return rowIsNullFallback;
					}

					break;
				}

				// ROW(a, b) operator ROW(c, d)
				case SqlRowExpression rhs:
				{
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature))
						return RowComparisonFallback(op, (SqlRowExpression)predicate.Expr1, rhs, context);
					break;
				}

				// ROW(a, b) operator (SELECT c, d)
				case SelectQuery:
				{
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature) ||
						!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.CompareToSelect))
						throw new LinqToDBException("SqlRow comparisons to SELECT are not supported by this DB provider");
					break;
				}

				default:
					throw new LinqToDBException("Inappropriate SqlRow expression, only Sql.Row() and sub-selects are valid.");
			}

			// Default ExprExpr translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.UnknownAsValue == null
				? predicate
				: new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, expr2, unknownAsValue: null);
		}

		bool ConvertRowIsNullPredicate(SqlRowExpression sqlRow, bool IsNot, [NotNullWhen(true)] out ISqlPredicate? rowIsNullFallback)
		{
			if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.IsNull))
			{
				rowIsNullFallback = RowIsNullFallback(sqlRow, IsNot);
				return true;
			}

			rowIsNullFallback = null;
			return false;
		}

		protected virtual ISqlPredicate ConvertRowInList(SqlPredicate.InList predicate)
		{
			if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.In))
			{
				var left    = predicate.Expr1;
				var op      = predicate.IsNot ? SqlPredicate.Operator.NotEqual : SqlPredicate.Operator.Equal;
				var isOr    = !predicate.IsNot;
				var rewrite = new SqlSearchCondition(isOr);
				foreach (var item in predicate.Values)
					rewrite.Predicates.Add(new SqlPredicate.ExprExpr(left, op, item, unknownAsValue: null));
				return rewrite;
			}

			// Default InList translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.WithNull == null
				? predicate
				: new SqlPredicate.InList(predicate.Expr1, withNull: null, predicate.IsNot, predicate.Values);
		}

		protected ISqlPredicate RowIsNullFallback(SqlRowExpression row, bool isNot)
		{
			var rewrite = new SqlSearchCondition();
			// (a, b) is null     => a is null     and b is null
			// (a, b) is not null => a is not null and b is not null
			foreach (var value in row.Values)
				rewrite.Predicates.Add(new SqlPredicate.IsNull(value, isNot));
			return rewrite;
		}

		protected ISqlPredicate RowComparisonFallback(SqlPredicate.Operator op, SqlRowExpression row1, SqlRowExpression row2, EvaluationContext context)
		{
			if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				// (a1, a2) =  (b1, b2) => a1 =  b1 and a2 = b2
				// (a1, a2) <> (b1, b2) => a1 <> b1 or  a2 <> b2
				bool isOr = op == SqlPredicate.Operator.NotEqual;

				var rewrite = new SqlSearchCondition(isOr);

				var compares = row1.Values.Zip(row2.Values, (a, b) =>
				{
					// There is a trap here, neither `a` nor `b` should be a constant null value,
					// because ExprExpr reduces `a == null` to `a is null`,
					// which is not the same and not equivalent to the Row expression.
					// We use `a >= null` instead, which is equivalent (always evaluates to `unknown`) but is never reduced by ExprExpr.
					// Reducing to `false` is an inaccuracy that causes problems when composed in more complicated ways,
					// e.g. the NOT IN SqlRow tests fail.
					SqlPredicate.Operator nullSafeOp = (a.TryEvaluateExpression(context, out var val) && val == null) ||
													   (b.TryEvaluateExpression(context, out     val) && val == null)
						? SqlPredicate.Operator.GreaterOrEqual
						: op;
					return new SqlPredicate.ExprExpr(a, nullSafeOp, b, unknownAsValue: null);
				});

				foreach (var comp in compares)
					rewrite.Predicates.Add(comp);

				return rewrite;
			}

			if (op is SqlPredicate.Operator.Greater or SqlPredicate.Operator.GreaterOrEqual or SqlPredicate.Operator.Less or SqlPredicate.Operator.LessOrEqual)
			{
				var rewrite = new SqlSearchCondition(true);

				// (a1, a2, a3) >  (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >  b3)
				// (a1, a2, a3) >= (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >= b3)
				// (a1, a2, a3) <  (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <  b3)
				// (a1, a2, a3) <= (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <= b3)
				var strictOp = op is SqlPredicate.Operator.Greater or SqlPredicate.Operator.GreaterOrEqual ? SqlPredicate.Operator.Greater : SqlPredicate.Operator.Less;
				var values1 = row1.Values;
				var values2 = row2.Values;

				for (int i = 0; i < values1.Length; ++i)
				{
					var sub = new SqlSearchCondition();
					for (int j = 0; j < i; j++)
					{
						sub.Add(new SqlPredicate.ExprExpr(values1[j], SqlPredicate.Operator.Equal, values2[j], unknownAsValue: null));
					}

					sub.Add(new SqlPredicate.ExprExpr(values1[i], i == values1.Length - 1 ? op : strictOp, values2[i], unknownAsValue: null));

					rewrite.Add(sub);
				}

				return rewrite;
			}

			if (op is SqlPredicate.Operator.Overlaps)
			{
				//TODO:: retest

				/*if (row1.Values.Length == 2 && row2.Values.Length == 2)
				{
					var rewrite = new SqlSearchCondition(true);

					static void AddCase(SqlSearchCondition condition, (ISqlExpression start, ISqlExpression end) caseRow1, (ISqlExpression start, ISqlExpression end) caseRow2)
					{
						// (s1 <= e1) and (s2 <= e2) and ((s2 < e1 and e2 > s1) or (s1 < e2 and e1 > s2))

						condition.AddAnd(subCase =>
							subCase
								.AddLessOrEqual(caseRow1.start, caseRow1.end, false)
								.AddLessOrEqual(caseRow2.start, caseRow2.end, false)
								.AddOr(x =>
									x
										.AddAnd(sub =>
											sub
												.AddLess(caseRow2.start, caseRow1.end, false)
												.AddGreater(caseRow2.end, caseRow1.start, false)
										)
										.AddAnd(sub =>
											sub
												.AddLess(caseRow1.start, caseRow2.end, false)
												.AddGreater(caseRow1.end, caseRow2.start, false)
										)
								));
					}

					// add possible permutations

					AddCase(rewrite, (row1.Values[0], row1.Values[1]), (row2.Values[0], row2.Values[1]));
					AddCase(rewrite, (row1.Values[0], row1.Values[1]), (row2.Values[1], row2.Values[0]));
					AddCase(rewrite, (row1.Values[1], row1.Values[0]), (row2.Values[0], row2.Values[1]));
					AddCase(rewrite, (row1.Values[1], row1.Values[0]), (row2.Values[1], row2.Values[0]));

					return rewrite;
				}*/
			}

			throw new LinqToDBException($"Unsupported SqlRow operator: {op}");
		}

		#endregion

		#region Helper functions

		public ISqlExpression Add(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "+", expr2, Precedence.Additive);
		}

		public ISqlExpression Add<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Add(expr1, expr2, typeof(T));
		}

		public ISqlExpression Add(ISqlExpression expr1, int value)
		{
			return Add<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Inc(ISqlExpression expr1)
		{
			return Add(expr1, 1);
		}

		public ISqlExpression Sub(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "-", expr2, Precedence.Subtraction);
		}

		public ISqlExpression Sub<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Sub(expr1, expr2, typeof(T));
		}

		public ISqlExpression Sub(ISqlExpression expr1, int value)
		{
			return Sub<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Dec(ISqlExpression expr1)
		{
			return Sub(expr1, 1);
		}

		public ISqlExpression Mul(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "*", expr2, Precedence.Multiplicative);
		}

		public ISqlExpression Mul<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Mul(expr1, expr2, typeof(T));
		}

		public ISqlExpression Mul(ISqlExpression expr1, int value)
		{
			return Mul<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Div(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative);
		}

		public ISqlExpression Div<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Div(expr1, expr2, typeof(T));
		}

		public ISqlExpression Div(ISqlExpression expr1, int value)
		{
			return Div<int>(expr1, new SqlValue(value));
		}

		protected SqlSearchCondition ConvertToBooleanSearchCondition(ISqlExpression expression)
		{
			var sc = new SqlSearchCondition();

			ISqlPredicate predicate;
			var dbType = QueryHelper.GetDbDataType(expression, MappingSchema);
			if (dbType.SystemType.UnwrapNullableType() == typeof(bool) || dbType.DataType == DataType.Boolean)
			{
				predicate = new SqlPredicate.IsTrue(expression, new SqlValue(true), new SqlValue(false), DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null, false);
			}
			else
			{
				predicate = new SqlPredicate.ExprExpr(expression, SqlPredicate.Operator.Equal, new SqlValue(0), DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null)
					.MakeNot();
			}

			sc.Add(predicate);

			return sc;
		}

		protected ISqlExpression ConvertBooleanToCase(ISqlExpression expr, DbDataType toType)
		{
			var caseExpr = new SqlCaseExpression(
				toType,
				[
					new(new SqlPredicate.IsNull(expr, false), new SqlValue(toType, null)),
					new(new SqlPredicate.ExprExpr(expr, SqlPredicate.Operator.NotEqual, new SqlValue(0), null), new SqlValue(toType, true)),
				],
				new SqlValue(toType, false)
			);

			return caseExpr;
		}

		protected ISqlExpression ConvertCoalesceToBinaryFunc(SqlCoalesceExpression coalesce, string funcName, bool supportsParameters = true)
		{
			var last = coalesce.Expressions[^1];
			MarkParameters(last);

			for (int i = coalesce.Expressions.Length - 2; i >= 0; i--)
			{
				var param = coalesce.Expressions[i];
				MarkParameters(param);

				last = new SqlFunction(QueryHelper.GetDbDataType(coalesce, MappingSchema), funcName, ParametersNullabilityType.IfAllParametersNullable, param, last);
			}

			return last;

			void MarkParameters(ISqlExpression expr)
			{
				if (supportsParameters)
					return;

				expr.VisitAll(e =>
				{
					if (e is SqlParameter param)
						param.IsQueryParameter = false;
				});
			}
		}

		protected static bool IsDateDataType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.Date || string.Equals(dataType.DbType, typeName, StringComparison.Ordinal);
		}

		protected static bool IsSmallDateTimeType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.SmallDateTime || string.Equals(dataType.DbType, typeName, StringComparison.Ordinal);
		}

		protected static bool IsDateTime2Type(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.DateTime2 || string.Equals(dataType.DbType, typeName, StringComparison.Ordinal);
		}

		protected static bool IsDateTimeType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.DateTime2 || string.Equals(dataType.DbType, typeName, StringComparison.Ordinal);
		}

		protected static bool IsDateDataOffsetType(DbDataType dataType)
		{
			return dataType.DataType == DataType.DateTimeOffset;
		}

		protected static bool IsTimeDataType(DbDataType dataType)
		{
			return dataType.DataType == DataType.Time || string.Equals(dataType.DbType, "Time", StringComparison.Ordinal);
		}

		protected SqlCastExpression FloorBeforeConvert(SqlCastExpression cast)
		{
			return cast switch
			{
				{
					Expression: { SystemType.IsFloatType: true } and not SqlFunction { Name: "Floor" },
					SystemType.IsIntegerType: true,
				} => cast.WithExpression(new SqlFunction(QueryHelper.GetDbDataType(cast.Expression, MappingSchema), "Floor", cast.Expression)),

				_ => cast,
			};
		}

		protected ISqlExpression TryConvertToValue(ISqlExpression expr, EvaluationContext context)
		{
			if (expr.ElementType != QueryElementType.SqlValue)
			{
				if (expr.TryEvaluateExpression(context, out var value))
					expr = new SqlValue(QueryHelper.GetDbDataType(expr, MappingSchema), value);
			}

			return expr;
		}

		#endregion
	}
}
