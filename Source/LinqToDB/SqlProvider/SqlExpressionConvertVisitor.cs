using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlProvider
{
	public class SqlExpressionConvertVisitor : SqlQueryVisitor
	{
		protected bool VisitQueries;

		protected bool IsInsidePredicate { get; private set; }

		protected OptimizationContext OptimizationContext = default!;
		protected NullabilityContext  NullabilityContext  = default!;

		protected EvaluationContext EvaluationContext => OptimizationContext.EvaluationContext;
		protected DataOptions       DataOptions       => OptimizationContext.DataOptions;
		protected MappingSchema     MappingSchema     => OptimizationContext.MappingSchema;
		protected SqlProviderFlags  SqlProviderFlags  => OptimizationContext.SqlProviderFlags;

		public SqlExpressionConvertVisitor(bool allowModify) : base(allowModify ? VisitMode.Modify : VisitMode.Transform, null)
		{
		}

		protected virtual bool SupportsBooleanInColumn           => false;
		protected virtual bool SupportsNullInColumn              => true;
		protected virtual bool SupportsDistinctAsExistsIntersect => false;

		public virtual IQueryElement Convert(OptimizationContext optimizationContext, NullabilityContext nullabilityContext, IQueryElement element, bool visitQueries)
		{
			Cleanup();

			OptimizationContext = optimizationContext;
			NullabilityContext  = nullabilityContext;
			VisitQueries        = visitQueries;
			SetTransformationInfo(optimizationContext.TransformationInfoConvert);

			var newElement = ProcessElement(element);

			return newElement;
		}

		public override void Cleanup()
		{
			base.Cleanup();

			OptimizationContext = default!;
			NullabilityContext  = default!;
			VisitQueries        = default;
			IsInsidePredicate   = false;
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

		protected override IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			var result = (SqlOutputClause)base.VisitSqlOutputClause(element);

			if (result.OutputColumns != null)
			{
				var newElements = VisitElements(result.OutputColumns, GetVisitMode(element), e => WrapBooleanExpression(e, includeFields : false));
				if (!ReferenceEquals(newElements, result.OutputColumns))
				{
					return new SqlOutputClause()
					{
						OutputTable = result.OutputTable,
						OutputItems = result.OutputItems,
						OutputColumns = newElements
					};
				}
			}

			return result;
		}

		protected override IQueryElement VisitSqlConditionExpression(SqlConditionExpression element)
		{
			var newElement = base.VisitSqlConditionExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlCondition(element);

			if (!ReferenceEquals(newElement, element))
			{
				return Visit(NotifyReplaced(newElement, element));
			}

			return element;
		}

		protected override SqlCaseExpression.CaseItem VisitCaseItem(SqlCaseExpression.CaseItem element)
		{
			var newElement = base.VisitCaseItem(element);

			newElement = ConvertCaseItem(newElement);

			return newElement;
		}

		protected override IQueryElement VisitSqlCaseExpression(SqlCaseExpression element)
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

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			if (!VisitQueries)
				return selectQuery;

			var newQuery = base.VisitSqlQuery(selectQuery);

			return newQuery;
		}

		protected override IQueryElement VisitExprPredicate(SqlPredicate.Expr predicate)
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
					else if (unwrapped is SqlExpression { IsPredicate: true } or SqlValue { Value: null })
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

		protected override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			var newElement = base.VisitSqlFieldReference(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			return element;
		}

		protected override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			var newElement = base.VisitSqlColumnReference(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			return element;
		}

		protected override IQueryElement VisitNotPredicate(SqlPredicate.Not predicate)
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

		protected override IQueryElement VisitSqlValue(SqlValue element)
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
			return OptimizationContext.OptimizerVisitor.Optimize(EvaluationContext, NullabilityContext, OptimizationContext.TransformationInfo, DataOptions, OptimizationContext.MappingSchema, element, VisitQueries, reducePredicates : false);
		}

		protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var saveInsidePredicate = IsInsidePredicate;
			IsInsidePredicate       = true;
			var newElement          = base.VisitExprExprPredicate(predicate);
			IsInsidePredicate       = saveInsidePredicate;

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

		protected override IQueryElement VisitSqlCompareToExpression(SqlCompareToExpression element)
		{
			var newElement = base.VisitSqlCompareToExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var caseExpression = new SqlCaseExpression(new DbDataType(typeof(int)),
				new SqlCaseExpression.CaseItem[]
				{
					new(new SqlSearchCondition().AddGreater(element.Expression1, element.Expression2, DataOptions.LinqOptions.CompareNulls), new SqlValue(1)),
					new(new SqlSearchCondition().AddEqual(element.Expression1, element.Expression2, DataOptions.LinqOptions.CompareNulls), new SqlValue(0))
				},
				new SqlValue(-1));

			return Visit(Optimize(caseExpression));
		}

		protected override IQueryElement VisitIsDistinctPredicate(SqlPredicate.IsDistinct predicate)
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
					.Add(new SqlPredicate.IsNull(predicate.Expr2, true)
					))
				.AddAnd(sc => sc
					.Add(new SqlPredicate.IsNull(predicate.Expr1, true))
					.Add(new SqlPredicate.IsNull(predicate.Expr2, false)
					)
				)
				.Add(new SqlPredicate.ExprExpr(predicate.Expr1, SqlPredicate.Operator.NotEqual, predicate.Expr2, null)
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
						: new SqlConditionExpression(exprExpr.Invert(NullabilityContext), falseValue, trueValue);

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
				var expr1IsPredicate = QueryHelper.UnwrapNullablity(predicate.Expr1) is (ISqlPredicate or SqlExpression { IsPredicate: true });
				var expr2IsPredicate = QueryHelper.UnwrapNullablity(predicate.Expr2) is (ISqlPredicate or SqlExpression { IsPredicate: true });

				var expr1IsConstant = QueryHelper.UnwrapNullablity(predicate.Expr1) is (SqlValue or SqlParameter { IsQueryParameter: false });
				var expr2IsConstant = QueryHelper.UnwrapNullablity(predicate.Expr2) is (SqlValue or SqlParameter { IsQueryParameter: false });

				var expr1 = expr1IsPredicate && !expr2IsConstant
					? WrapBooleanExpression(predicate.Expr1, includeFields : true, withNull: true, forceConvert: !SqlProviderFlags.SupportsPredicatesComparison)
					: predicate.Expr1;
				var expr2 = expr2IsPredicate && !expr1IsConstant
					? WrapBooleanExpression(predicate.Expr2, includeFields : true, withNull: true, forceConvert: !SqlProviderFlags.SupportsPredicatesComparison)
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

		protected override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
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
								values.Add(MappingSchema.GetSqlValueFromObject(cd, item!));
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
									var sqlValue = MappingSchema.GetSqlValueFromObject(cd, item!);
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

		protected override IQueryElement VisitSearchStringPredicate(SqlPredicate.SearchString predicate)
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
					PseudoFunctions.MakeToLower(predicate.Expr1),
					predicate.IsNot,
					PseudoFunctions.MakeToLower(predicate.Expr2),
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
		public virtual string LikeEscapeCharacter         => "~";
		public virtual string LikeWildcardCharacter       => "%";
		public virtual bool   LikePatternParameterSupport => true;
		public virtual bool   LikeValueParameterSupport   => true;
		/// <summary>
		/// Should be <c>true</c> for provider with <c>LIKE ... ESCAPE</c> modifier support.
		/// Default: <c>true</c>.
		/// </summary>
		public virtual bool   LikeIsEscapeSupported       => true;

		protected static string[] StandardLikeCharactersToEscape = {"%", "_", "?", "*", "#", "[", "]"};

		/// <summary>
		/// Characters with special meaning in LIKE predicate (defined by <see cref="LikeCharactersToEscape"/>) that should be escaped to be used as matched character.
		/// Default: <c>["%", "_", "?", "*", "#", "[", "]"]</c>.
		/// </summary>
		public virtual string[] LikeCharactersToEscape => StandardLikeCharactersToEscape;

		public virtual string EscapeLikeCharacters(string str, string escape)
		{
			var newStr = str;

			newStr = newStr.Replace(escape, escape + escape);

			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newStr = newStr.Replace(s, escape + s);
			}

			return newStr;
		}

		static ISqlExpression GenerateEscapeReplacement(ISqlExpression expression, ISqlExpression character, ISqlExpression escapeCharacter)
		{
			var result = PseudoFunctions.MakeReplace(expression, character, new SqlBinaryExpression(typeof(string), escapeCharacter, "+", character, Precedence.Additive));
			return result;
		}

		public static ISqlExpression GenerateEscapeReplacement(ISqlExpression expression, ISqlExpression character)
		{
			var result = PseudoFunctions.MakeReplace(
				expression,
				character,
				new SqlBinaryExpression(typeof(string), new SqlValue("["), "+",
					new SqlBinaryExpression(typeof(string), character, "+", new SqlValue("]"), Precedence.Additive),
					Precedence.Additive));
			return result;
		}

		/// <summary>
		/// Implements LIKE pattern escaping logic for provider without ESCAPE clause support (<see cref="LikeIsEscapeSupported"/> is <c>false</c>).
		/// Default logic prefix characters from <see cref="LikeCharactersToEscape"/> with <see cref="LikeEscapeCharacter"/>.
		/// </summary>
		/// <param name="str">Raw pattern value.</param>
		/// <returns>Escaped pattern value.</returns>
		protected virtual string EscapeLikePattern(string str)
		{
			foreach (var s in LikeCharactersToEscape)
				str = str.Replace(s, LikeEscapeCharacter + s);

			return str;
		}

		public virtual ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			var newExpr = expression;

			escape ??= new SqlValue(LikeEscapeCharacter);

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
					SqlPredicate.SearchString.SearchKind.EndsWith   => LikeWildcardCharacter + patternValue,
					SqlPredicate.SearchString.SearchKind.Contains   => LikeWildcardCharacter + patternValue + LikeWildcardCharacter,
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}")
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
					LikeIsEscapeSupported && (patternValue != patternRawValue) ? new SqlValue(LikeEscapeCharacter) : null);
			}
			else
			{
				ISqlExpression? escape = null;

				var patternExpr = EscapeLikeCharacters(predicate.Expr2, ref escape);

				var anyCharacterExpr = new SqlValue(LikeWildcardCharacter);

				patternExpr = predicate.Kind switch
				{
					SqlPredicate.SearchString.SearchKind.StartsWith => new SqlBinaryExpression(typeof(string), patternExpr, "+", anyCharacterExpr, Precedence.Additive),
					SqlPredicate.SearchString.SearchKind.EndsWith   => new SqlBinaryExpression(typeof(string), anyCharacterExpr, "+", patternExpr, Precedence.Additive),
					SqlPredicate.SearchString.SearchKind.Contains   => new SqlBinaryExpression(typeof(string), new SqlBinaryExpression(typeof(string), anyCharacterExpr, "+", patternExpr, Precedence.Additive), "+", anyCharacterExpr, Precedence.Additive),
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}")
				};

				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, patternExpr, LikeIsEscapeSupported ? escape : null);
			}
		}

		#endregion

		#region Visitor overrides

		protected override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
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

		protected override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			var newElement = base.VisitSqlFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = ConvertSqlFunction(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(Optimize(newElement));

			return element;
		}

		protected override IQueryElement VisitSqlExpression(SqlExpression element)
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

		protected override IQueryElement VisitLikePredicate(SqlPredicate.Like predicate)
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

		protected override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
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

		protected override IQueryElement VisitSqlInlinedSqlExpression(SqlInlinedSqlExpression element)
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

		protected override IQueryElement VisitSqlInlinedToSqlExpression(SqlInlinedToSqlExpression element)
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

		protected override IQueryElement VisitBetweenPredicate(SqlPredicate.Between predicate)
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

		protected override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			if (predicate.DoNotConvert)
				return base.VisitInSubQueryPredicate(predicate);

			var newPredicate = base.VisitInSubQueryPredicate(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

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

		protected override IQueryElement VisitSqlOrderByItem(SqlOrderByItem element)
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
					newElement = new SqlOrderByItem(wrapped, newElement.IsDescending, newElement.IsPositioned);
				}
			}

			return newElement;
		}

		protected override IQueryElement VisitSqlSetExpression(SqlSetExpression element)
		{
			var newElement = (SqlSetExpression)base.VisitSqlSetExpression(element);

			while (newElement.Column is SqlCastExpression cast)
			{
				var newColumn = cast.Expression;
				var newValue  = newElement.Expression == null ? null : new SqlCastExpression(newElement.Expression, QueryHelper.GetDbDataType(newColumn, MappingSchema), null, false);

				if (GetVisitMode(newElement) == VisitMode.Modify)
				{
					newElement.Column     = newColumn;
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

		protected override IQueryElement VisitSqlCastExpression(SqlCastExpression element)
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

		protected override IQueryElement VisitSqlCoalesceExpression(SqlCoalesceExpression element)
		{
			var newElement = base.VisitSqlCoalesceExpression(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var converted = ConvertCoalesce(element);

			if (!ReferenceEquals(converted, element))
				return Visit(Optimize(converted));

			return element;
		}

		#endregion Visitor overrides

		public virtual ISqlExpression ConvertCoalesce(SqlCoalesceExpression element)
		{
			var type = QueryHelper.GetDbDataType(element.Expressions[0], MappingSchema);
			return new SqlFunction(type, "Coalesce", parametersNullability: ParametersNullabilityType.IfAllParametersNullable, element.Expressions);
		}

		public virtual ISqlExpression ConvertSqlExpression(SqlExpression element)
		{
			return element;
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

						return new SqlFunction(typeof(int), func.Name, new SqlConditionExpression(predicate, new SqlValue(1), new SqlValue(0)));
					}

					break;
				}

				case PseudoFunctions.CONVERT_FORMAT:
				{
					return new SqlFunction(func.SystemType, "Convert", func.Parameters[0], func.Parameters[2], func.Parameters[3]);
				}

				case PseudoFunctions.TO_LOWER: return func.WithName("Lower");
				case PseudoFunctions.TO_UPPER: return func.WithName("Upper");
				case PseudoFunctions.REPLACE:  return func.WithName("Replace");
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

			var intTestSubQuery = inPredicate.SubQuery.Clone();
			intTestSubQuery = WrapIfNeeded(intTestSubQuery);
			var inSubqueryExpr = intTestSubQuery.Select.Columns[0].Expression;

			intTestSubQuery.Select.Columns.Clear();
			intTestSubQuery.Select.AddNewColumn(new SqlValue(1));
			intTestSubQuery.Where.SearchCondition.AddIsNull(inSubqueryExpr);

			sc.AddAnd(sub => sub
					.AddIsNull(testExpr)
					.Add(new SqlPredicate.InSubQuery(new SqlValue(1), false, intTestSubQuery, doNotConvert: true))
				)
				.AddAnd(sub => sub
					.AddIsNotNull(testExpr)
					.Add(new SqlPredicate.InSubQuery(testExpr, false, inPredicate.SubQuery, doNotConvert: true))
				);

			var result = Optimize(sc.MakeNot(inPredicate.IsNot));

			return (ISqlPredicate)result;
		}

		static SelectQuery WrapIfNeeded(SelectQuery selectQuery)
		{
			if (selectQuery.Select.HasModifier || !selectQuery.GroupBy.IsEmpty || QueryHelper.IsAggregationQuery(selectQuery))
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

			for (int i = 0; i < testExpressions.Length; i++)
			{
				var testValue = testExpressions[i];
				var expr      = subQuery.Select.Columns[i].Expression;

				predicates.Add(new SqlPredicate.ExprExpr(testValue, SqlPredicate.Operator.Equal, expr, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null));
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
					if (element.Expr1.SystemType == typeof(string) && element.Expr2.SystemType != typeof(string))
					{
						var len = element.Expr2.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(MappingSchema.GetDataType(element.Expr2.SystemType).Type.DataType);

						if (len == null || len <= 0)
							len = 100;

						return new SqlBinaryExpression(
							element.SystemType,
							element.Expr1,
							element.Operation,
							(ISqlExpression)Visit(PseudoFunctions.MakeCast(element.Expr2, new DbDataType(typeof(string), DataType.VarChar, null, len.Value))),
							element.Precedence);
					}

					if (element.Expr1.SystemType != typeof(string) && element.Expr2.SystemType == typeof(string))
					{
						var len = element.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(MappingSchema.GetDataType(element.Expr1.SystemType).Type.DataType);

						if (len == null || len <= 0)
							len = 100;

						return new SqlBinaryExpression(
							element.SystemType,
							(ISqlExpression)Visit(PseudoFunctions.MakeCast(element.Expr1, new DbDataType(typeof(string), DataType.VarChar, null, len.Value))),
							element.Operation,
							element.Expr2,
							element.Precedence);
					}

					break;
				}
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

				var wrap = includeFields && unwrapped.ElementType is QueryElementType.Column or QueryElementType.SqlField;
				if (!wrap && unwrapped is ISqlPredicate or SqlExpression { IsPredicate: true })
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
					var predicate = unwrapped as ISqlPredicate;
					if (predicate == null && unwrapped is SqlExpression { IsPredicate: true })
						predicate = new SqlPredicate.Expr(expr);
					if (predicate == null)
						predicate = ConvertToBooleanSearchCondition(expr);

					var trueValue  = new SqlValue(true);
					var falseValue = new SqlValue(false);

					if ((forceConvert || !SqlProviderFlags.SupportsBooleanType) && withNull && expr.CanBeNullableOrUnknown(NullabilityContext, false))
					{
						var toType = QueryHelper.GetDbDataType(expr, MappingSchema);

						expr = new SqlCaseExpression(toType,
							new SqlCaseExpression.CaseItem[]
							{
								new(predicate, trueValue),
								new(new SqlPredicate.Not(predicate), falseValue)
							}, new SqlValue(toType, null));
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

		protected virtual int? GetMaxLength(DbDataType      type) { return SqlDataType.GetMaxLength(type.DataType); }
		protected virtual int? GetMaxPrecision(DbDataType   type) { return SqlDataType.GetMaxPrecision(type.DataType); }
		protected virtual int? GetMaxScale(DbDataType       type) { return SqlDataType.GetMaxScale(type.DataType); }
		protected virtual int? GetMaxDisplaySize(DbDataType type) { return SqlDataType.GetMaxDisplaySize(type.DataType); }

		/// <summary>
		/// Implements <see cref="SqlCastExpression"/> conversion.
		/// </summary>
		protected virtual ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var toDataType = cast.ToType;

			if (cast.SystemType == typeof(string) && cast.Expression is SqlValue value)
			{
				if (value.Value is char charValue)
					return new SqlValue(cast.Type, charValue.ToString());
			}

			var fromDbType = QueryHelper.GetDbDataType(cast.Expression, MappingSchema);

			if (toDataType.Length > 0)
			{
				var maxLength = toDataType.SystemType == typeof(string) ? GetMaxDisplaySize(fromDbType) : GetMaxLength(fromDbType);
				var newLength = maxLength != null && maxLength >= 0 ? Math.Min(toDataType.Length ?? 0, maxLength.Value) : fromDbType.Length;

				var newDataType = toDataType.WithLength(newLength);
				if (!newDataType.Equals(toDataType))
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
					SqlPredicate.Operator nullSafeOp = a.TryEvaluateExpression(context, out var val) && val == null ||
					                                   b.TryEvaluateExpression(context, out     val) && val == null
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
			if (dbType.SystemType.ToNullableUnderlying() == typeof(bool) || dbType.DataType == DataType.Boolean)
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
			var caseExpr = new SqlCaseExpression(toType,
				new SqlCaseExpression.CaseItem[]
				{
					new(new SqlPredicate.IsNull(expr, false), new SqlValue(toType, null)),
					new(new SqlPredicate.ExprExpr(expr, SqlPredicate.Operator.NotEqual, new SqlValue(0), null), new SqlValue(toType, true))
				}, new SqlValue(toType, false));
			

			return caseExpr;
		}

		protected ISqlExpression ConvertCoalesceToBinaryFunc(SqlCoalesceExpression coalesce, string funcName, bool supportsParameters = true)
		{
			var last = coalesce.Expressions[^1];
			if (!supportsParameters && last is SqlParameter p1)
				p1.IsQueryParameter = false;

			for (int i = coalesce.Expressions.Length - 2; i >= 0; i--)
			{
				var param = coalesce.Expressions[i];
				if (!supportsParameters && param is SqlParameter p2)
					p2.IsQueryParameter = false;

				last = new SqlFunction(coalesce.SystemType!, funcName, param, last);
			}

			return last;
		}

		protected static bool IsDateDataType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.Date || dataType.DbType == typeName;
		}

		protected static bool IsSmallDateTimeType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.SmallDateTime || dataType.DbType == typeName;
		}

		protected static bool IsDateTime2Type(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.DateTime2 || dataType.DbType == typeName;
		}

		protected static bool IsDateTimeType(DbDataType dataType, string typeName)
		{
			return dataType.DataType == DataType.DateTime2 || dataType.DbType == typeName;
		}

		protected static bool IsDateDataOffsetType(DbDataType dataType)
		{
			return dataType.DataType == DataType.DateTimeOffset;
		}

		protected static bool IsTimeDataType(DbDataType dataType)
		{
			return dataType.DataType == DataType.Time || dataType.DbType == "Time";
		}

		protected SqlCastExpression FloorBeforeConvert(SqlCastExpression cast)
		{
			if (cast.Expression.SystemType!.IsFloatType() && cast.SystemType.IsIntegerType())
			{
				if (cast.Expression is SqlFunction { Name: "Floor" })
					return cast;

				return cast.WithExpression(new SqlFunction(cast.Expression.SystemType!, "Floor", cast.Expression));
			}

			return cast;
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
