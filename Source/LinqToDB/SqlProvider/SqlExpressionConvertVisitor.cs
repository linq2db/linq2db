using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlProvider
{
	public class SqlExpressionConvertVisitor : SqlQueryVisitor
	{
		protected OptimizationContext OptimizationContext = default!;
		protected EvaluationContext   EvaluationContext   = default!;
		protected NullabilityContext  NullabilityContext  = default!;
		protected DataOptions         DataOptions         = default!;
		protected MappingSchema       MappingSchema       = default!;

		protected SqlProviderFlags? SqlProviderFlags;

		readonly SqlDataType _typeWrapper = new(default(DbDataType));

		public SqlExpressionConvertVisitor(bool allowModify) : base(allowModify ? VisitMode.Modify : VisitMode.Transform)
		{
		}

		public virtual bool CanCompareSearchConditions => false;

		public virtual IQueryElement Convert(OptimizationContext optimizationContext, NullabilityContext nullabilityContext, SqlProviderFlags? sqlProviderFlags, DataOptions dataOptions, MappingSchema mappingSchema, IQueryElement element)
		{
			Cleanup();
			OptimizationContext = optimizationContext;
			EvaluationContext   = optimizationContext.Context;
			NullabilityContext  = nullabilityContext;
			SqlProviderFlags    = sqlProviderFlags;
			DataOptions         = dataOptions;
			MappingSchema       = mappingSchema;

			return ProcessElement(element);
		}

		public override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			var newElement = base.VisitSqlColumnExpression(column, expression);

			if (!ReferenceEquals(newElement, expression))
				return (ISqlExpression)Visit(newElement);

			return expression;
		}

		public override IQueryElement VisitSqlValue(SqlValue element)
		{
			var newElement = base.VisitSqlValue(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);


			if (element.Value is Sql.SqlID)
				return element;

			// TODO:
			// this line produce insane amount of allocations
			// as currently we cannot change ValueConverter signatures, we use pre-created instance of type wrapper
			//var dataType = new SqlDataType(value.ValueType);
			_typeWrapper.Type = element.ValueType;

			if (!MappingSchema.ValueToSqlConverter.CanConvert(_typeWrapper, DataOptions, element.Value))
			{
				// we cannot generate SQL literal, so just convert to parameter
				var param = OptimizationContext.SuggestDynamicParameter(element.ValueType, element.Value);
				return param;
			}

			return element;
		}

		public override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var newElement = base.VisitExprExprPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			return ConvertExprExprPredicate(predicate);
		}

		public virtual IQueryElement ConvertExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			if (predicate.Expr1.ElementType == QueryElementType.SqlRow)
				return ConvertRowExprExpr(predicate, EvaluationContext);

			var reduced = predicate.Reduce(NullabilityContext, EvaluationContext);

			if (!ReferenceEquals(reduced, predicate))
			{
				return Visit(reduced);
			}

			if (!CanCompareSearchConditions && (predicate.Expr1.ElementType == QueryElementType.SearchCondition ||
			                                    predicate.Expr2.ElementType == QueryElementType.SearchCondition))
			{
				var expr1 = predicate.Expr1;
				if (expr1.ElementType == QueryElementType.SearchCondition)
					expr1 = ConvertBooleanExprToCase(expr1);

				var expr2 = predicate.Expr2;
				if (expr2.ElementType == QueryElementType.SearchCondition)
					expr2 = ConvertBooleanExprToCase(expr2);

				return new SqlPredicate.ExprExpr(expr1, predicate.Operator, expr2, predicate.WithNull);
			}


			return predicate;
		}

		protected ISqlExpression ConvertBooleanExprToCase(ISqlExpression expression)
		{
			return new SqlFunction(typeof(bool), "CASE", expression, new SqlValue(true), new SqlValue(false))
			{
				CanBeNull     = false,
				DoNotOptimize = true
			};
		}

		static SqlField ExpectsUnderlyingField(ISqlExpression expr)
		{
			var result = QueryHelper.GetUnderlyingField(expr);
			if (result == null)
				throw new InvalidOperationException($"Cannot retrieve underlying field for '{expr.ToDebugString()}'.");
			return result;
		}

		public override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			var newElement = base.VisitInListPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (predicate.Expr1.ElementType == QueryElementType.SqlRow)
				return ConvertRowInList(predicate);

			if (predicate.Values.Count == 0)
				return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));

			if (predicate.Values.Count == 1 && predicate.Values[0] is SqlParameter parameter)
			{
				var paramValue = parameter.GetParameterValue(EvaluationContext.ParameterValues);

				if (paramValue.ProviderValue == null)
					return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));

				if (paramValue.ProviderValue is IEnumerable items)
				{
					if (predicate.Expr1 is ISqlTableSource table)
					{
						var keys  = table.GetKeys(true);

						if (keys == null || keys.Count == 0)
							throw new SqlException("Cant create IN expression.");

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
								return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));

							return new SqlPredicate.InList(keys[0], null, predicate.IsNot, values);
						}

						{
							var sc = new SqlSearchCondition();

							foreach (var item in items)
							{
								var itemCond = new SqlSearchCondition();

								foreach (var key in keys)
								{
									var field    = ExpectsUnderlyingField(key);
									var cd       = field.ColumnDescriptor;
									var sqlValue = MappingSchema.GetSqlValueFromObject(cd, item!);
									//TODO: review
									var cond = sqlValue.Value == null ?
										new SqlCondition(false, new SqlPredicate.IsNull  (field, false)) :
										new SqlCondition(false, new SqlPredicate.ExprExpr(field, SqlPredicate.Operator.Equal, sqlValue, null));

									itemCond.Conditions.Add(cond);
								}

								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
							}

							if (sc.Conditions.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));

							if (predicate.IsNot)
								return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);

							return new SqlPredicate.Expr(sc, Precedence.LogicalDisjunction);
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
								return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));

							return new SqlPredicate.InList(parameters[0].Sql, null, predicate.IsNot, values);
						}

						var sc = new SqlSearchCondition();

						foreach (var item in items)
						{
							var itemCond = new SqlSearchCondition();

							for (var i = 0; i < parameters.Length; i++)
							{
								var sql   = parameters[i].Sql;
								var value = expr.GetSqlValue(item!, i);
								var cond  = value == null ?
									new SqlCondition(false, new SqlPredicate.IsNull  (sql, false)) :
									new SqlCondition(false, new SqlPredicate.ExprExpr(sql, SqlPredicate.Operator.Equal, value, null));

								itemCond.Conditions.Add(cond);
							}

							sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
						}

						if (sc.Conditions.Count == 0)
							return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));

						if (predicate.IsNot)
							return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);

						return new SqlPredicate.Expr(sc, Precedence.LogicalDisjunction);
					}
				}
			}

			return predicate;
		}

		public override IQueryElement VisitSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var newElement = base.VisitSearchStringPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			return ConvertSearchStringPredicate(predicate);
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

		protected static  string[] StandardLikeCharactersToEscape = {"%", "_", "?", "*", "#", "[", "]"};
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
					? QueryHelper.CreateSqlValue(patternValue, predicate.Expr2.GetExpressionType(), predicate.Expr2)
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

		public override IQueryElement VisitIsTruePredicate(SqlPredicate.IsTrue predicate)
		{
			var newElement = base.VisitIsTruePredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			return predicate.Reduce(NullabilityContext);
		}

		public override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
		{
			var newElement = base.VisitIsNullPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (NullabilityContext.IsEmpty)
				return predicate;
			
			if (!NullabilityContext.CanBeNull(predicate.Expr1))
				return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));

			return predicate;
		}

		public override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			var newElement = base.VisitSqlFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);


			return ConvertSqlFunction(element);
		}

		public override IQueryElement VisitSqlExpression(SqlExpression element)
		{
			var newElement = base.VisitSqlExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);


			return ConvertSqlExpression(element);
		}

		public virtual ISqlExpression ConvertSqlExpression(SqlExpression element)
		{
			return element;
		}

		public virtual ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Average": return new SqlFunction(func.SystemType, "Avg", func.Parameters);
				case "Max":
				case "Min":
				{
					if (func.SystemType == typeof(bool) || func.SystemType == typeof(bool?))
					{
						return new SqlFunction(typeof(int), func.Name,
							new SqlFunction(func.SystemType, "CASE", func.Parameters[0], new SqlValue(1),
								new SqlValue(0)) { CanBeNull = false });
					}

					break;
				}

				case PseudoFunctions.CONVERT:
					return ConvertConversion(func);

				case PseudoFunctions.TO_LOWER: return func.WithName("Lower");
				case PseudoFunctions.TO_UPPER: return func.WithName("Upper");
				case PseudoFunctions.REPLACE:  return func.WithName("Replace");
				case PseudoFunctions.COALESCE: return func.WithName("Coalesce");

				case "ConvertToCaseCompareTo":
				{
					return new SqlFunction(func.SystemType, "CASE",
						new SqlSearchCondition().Expr(func.Parameters[0]).Greater.Expr(func.Parameters[1])
							.ToExpr(), new SqlValue(1),
						new SqlSearchCondition().Expr(func.Parameters[0]).Equal.Expr(func.Parameters[1]).ToExpr(),
						new SqlValue(0),
						new SqlValue(-1)) { CanBeNull = false };
				}
			}

			return func;
		}

		public override IQueryElement VisitLikePredicate(SqlPredicate.Like predicate)
		{
			var newElement = base.VisitLikePredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			return ConvertLikePredicate(predicate);
		}

		public virtual ISqlPredicate ConvertLikePredicate(SqlPredicate.Like predicate)
		{
			return predicate;
		}

		public override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			var newElement = base.VisitSqlBinaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			return ConvertSqlBinaryExpression(element);
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
							(ISqlExpression)Visit(PseudoFunctions.MakeConvert(new SqlDataType(DataType.VarChar, typeof(string), len.Value), new SqlDataType(element.Expr2.GetExpressionType()), element.Expr2)),
							element.Precedence);
					}

					if (element.Expr1.SystemType != typeof(string) && element.Expr2.SystemType == typeof(string))
					{
						var len = element.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(MappingSchema.GetDataType(element.Expr1.SystemType).Type.DataType);

						if (len == null || len <= 0)
							len = 100;

						return new SqlBinaryExpression(
							element.SystemType,
							(ISqlExpression)Visit(PseudoFunctions.MakeConvert(new SqlDataType(DataType.VarChar, typeof(string), len.Value), new SqlDataType(element.Expr1.GetExpressionType()), element.Expr1)),
							element.Operation,
							element.Expr2,
							element.Precedence);
					}

					break;
				}
			}

			return element;
		}


		#region DataTypes

		protected virtual int? GetMaxLength     (SqlDataType type) { return SqlDataType.GetMaxLength     (type.Type.DataType); }
		protected virtual int? GetMaxPrecision  (SqlDataType type) { return SqlDataType.GetMaxPrecision  (type.Type.DataType); }
		protected virtual int? GetMaxScale      (SqlDataType type) { return SqlDataType.GetMaxScale      (type.Type.DataType); }
		protected virtual int? GetMaxDisplaySize(SqlDataType type) { return SqlDataType.GetMaxDisplaySize(type.Type.DataType); }

		/// <summary>
		/// Implements <see cref="PseudoFunctions.CONVERT"/> function converter.
		/// </summary>
		protected virtual ISqlExpression ConvertConversion(SqlFunction func)
		{
			var from = (SqlDataType)func.Parameters[1];
			var to   = (SqlDataType)func.Parameters[0];

			if (!func.DoNotOptimize && (to.Type.SystemType == typeof(object) || from.Type.EqualsDbOnly(to.Type)))
				return func.Parameters[2];

			if (to.Type.Length > 0)
			{
				var maxLength = to.Type.SystemType == typeof(string) ? GetMaxDisplaySize(from) : GetMaxLength(from);
				var newLength = maxLength != null && maxLength >= 0 ? Math.Min(to.Type.Length ?? 0, maxLength.Value) : to.Type.Length;

				if (to.Type.Length != newLength)
					to = new SqlDataType(to.Type.WithLength(newLength));
			}
			else if (!func.DoNotOptimize && from.Type.SystemType == typeof(short) && to.Type.SystemType == typeof(int))
				return func.Parameters[2];

			return new SqlFunction(func.SystemType, "Convert", false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, to, func.Parameters[2]);
		}

		#endregion

		#region SqlRow

		protected ISqlPredicate ConvertRowExprExpr(SqlPredicate.ExprExpr predicate, EvaluationContext context)
		{
			if (SqlProviderFlags == null)
				return predicate;

			var op = predicate.Operator;
			var feature = op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual
				? RowFeature.Equality
				: op is SqlPredicate.Operator.Overlaps
					? RowFeature.Overlaps
					: RowFeature.Comparisons;

			var expr2 = predicate.Expr2;
			while (expr2 is SqlNullabilityExpression nullability)
				expr2 = nullability.SqlExpression;

			switch (expr2)
			{
				// ROW(a, b) IS [NOT] NULL
				case SqlValue { Value: null }:
				{
					if (op is not (SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual))
						throw new LinqException("Null SqlRow is only allowed in equality comparisons");
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.IsNull))
						return RowIsNullFallback((SqlRow)predicate.Expr1, op == SqlPredicate.Operator.NotEqual);
					break;
				}

				// ROW(a, b) operator ROW(c, d)
				case SqlRow rhs:
				{
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature))
						return RowComparisonFallback(op, (SqlRow)predicate.Expr1, rhs, context);
					break;
				}

				// ROW(a, b) operator (SELECT c, d)
				case SelectQuery:
				{
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature) ||
					    !SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.CompareToSelect))
						throw new LinqException("SqlRow comparisons to SELECT are not supported by this DB provider");
					break;
				}

				default:
					throw new LinqException("Inappropriate SqlRow expression, only Sql.Row() and sub-selects are valid.");
			}

			// Default ExprExpr translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.WithNull == null
				? predicate
				: new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, expr2, withNull: null);
		}

		protected virtual ISqlPredicate ConvertRowInList(SqlPredicate.InList predicate)
		{
			if (SqlProviderFlags == null)
				return predicate;

			if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.In))
			{
				var left    = predicate.Expr1;
				var op      = predicate.IsNot ? SqlPredicate.Operator.NotEqual : SqlPredicate.Operator.Equal;
				var isOr    = !predicate.IsNot;
				var rewrite = new SqlSearchCondition();
				foreach (var item in predicate.Values)
					rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(left, op, item, withNull: null), isOr));
				return rewrite;
			}

			// Default InList translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.WithNull == null
				? predicate
				: new SqlPredicate.InList(predicate.Expr1, withNull: null, predicate.IsNot, predicate.Values);
		}

		protected ISqlPredicate RowIsNullFallback(SqlRow row, bool isNot)
		{
			var rewrite = new SqlSearchCondition();
			// (a, b) is null     => a is null     and b is null
			// (a, b) is not null => a is not null and b is not null
			foreach (var value in row.Values)
				rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(value, isNot)));
			return rewrite;
		}

		protected ISqlPredicate RowComparisonFallback(SqlPredicate.Operator op, SqlRow row1, SqlRow row2, EvaluationContext context)
		{
			var rewrite = new SqlSearchCondition();
						
			if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				// (a1, a2) =  (b1, b2) => a1 =  b1 and a2 = b2
				// (a1, a2) <> (b1, b2) => a1 <> b1 or  a2 <> b2
				bool isOr = op == SqlPredicate.Operator.NotEqual;
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
					return new SqlPredicate.ExprExpr(a, nullSafeOp, b, withNull: null);
				});
				foreach (var comp in compares)
					rewrite.Conditions.Add(new SqlCondition(false, comp, isOr));

				return rewrite;
			}

			if (op is SqlPredicate.Operator.Greater or SqlPredicate.Operator.GreaterOrEqual or SqlPredicate.Operator.Less or SqlPredicate.Operator.LessOrEqual)
			{
				// (a1, a2, a3) >  (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >  b3)
				// (a1, a2, a3) >= (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >= b3)
				// (a1, a2, a3) <  (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <  b3)
				// (a1, a2, a3) <= (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <= b3)
				var strictOp = op is SqlPredicate.Operator.Greater or SqlPredicate.Operator.GreaterOrEqual ? SqlPredicate.Operator.Greater : SqlPredicate.Operator.Less;
				var values1 = row1.Values;
				var values2 = row2.Values;
				for (int i = 0; i < values1.Length; ++i)
				{
					for (int j = 0; j < i; j++)
						rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(values1[j], SqlPredicate.Operator.Equal, values2[j], withNull: null), isOr: false));
					rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(values1[i], i == values1.Length - 1 ? op : strictOp, values2[i], withNull: null), isOr: true));
				}

				return rewrite;
			}

			if (op is SqlPredicate.Operator.Overlaps)
			{
				//TODO: make it working if possible
				/*
				if (row1.Values.Length != 2 || row2.Values.Length != 2)
					throw new LinqException("Unsupported SqlRow conversion from operator: " + op);

				rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(row1.Values[0], SqlPredicate.Operator.LessOrEqual, row2.Values[1], withNull: false)));
				rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(row2.Values[0], SqlPredicate.Operator.LessOrEqual, row1.Values[1], withNull: false)));
				*/

				return rewrite;
			}

			throw new LinqException("Unsupported SqlRow operator: " + op);
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

		protected ISqlExpression? AlternativeConvertToBoolean(SqlFunction func, int paramNumber)
		{
			var par = func.Parameters[paramNumber];

			if (par.SystemType!.IsFloatType() || par.SystemType!.IsIntegerType())
			{
				var sc = new SqlSearchCondition();

				sc.Conditions.Add(
					new SqlCondition(false,
						new SqlPredicate.ExprExpr(par, SqlPredicate.Operator.NotEqual, new SqlValue(0),
							DataOptions.LinqOptions.CompareNullsAsValues ? false : null)));

				return new SqlFunction(func.SystemType, "CASE", false, true, sc, new SqlValue(true), new SqlValue(false))
				{
					CanBeNull = false,
				};
			}

			return null;
		}

		protected ISqlExpression ConvertCoalesceToBinaryFunc(SqlFunction func, string funcName, bool supportsParameters = true)
		{
			var last = func.Parameters[func.Parameters.Length - 1];
			if (!supportsParameters && last is SqlParameter p1)
				p1.IsQueryParameter = false;

			for (int i = func.Parameters.Length - 2; i >= 0; i--)
			{
				var param = func.Parameters[i];
				if (!supportsParameters && param is SqlParameter p2)
					p2.IsQueryParameter = false;

				last = new SqlFunction(func.SystemType, funcName, param, last);
			}
			return last;
		}

		protected static bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.Date,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == dateName,
				_                              => false,
			};
		}

		protected static bool IsSmallDateTimeType(ISqlExpression expr, string typeName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.SmallDateTime,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == typeName,
				_ => false,
			};
		}

		protected static bool IsDateTime2Type(ISqlExpression expr, string typeName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.DateTime2,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == typeName,
				_ => false,
			};
		}

		protected static bool IsDateTimeType(ISqlExpression expr, string typeName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.DateTime,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == typeName,
				_ => false,
			};
		}

		protected static bool IsDateDataOffsetType(ISqlExpression expr)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType => ((SqlDataType)expr).Type.DataType == DataType.DateTimeOffset,
				_                            => false,
			};
		}

		protected static bool IsTimeDataType(ISqlExpression expr)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.Time,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == "Time",
				_                              => false,
			};
		}

		protected ISqlExpression FloorBeforeConvert(SqlFunction func)
		{
			return FloorBeforeConvert(func, func.Parameters[1]);
		}

		protected ISqlExpression FloorBeforeConvert(SqlFunction func, ISqlExpression par)
		{
			if (par.SystemType!.IsFloatType() && func.SystemType.IsIntegerType())
			{
				return new SqlFunction(func.SystemType, "Floor", par);
			}

			return par;
		}

		protected static ISqlExpression TryConvertToValue(ISqlExpression expr, EvaluationContext context)
		{
			if (expr.ElementType != QueryElementType.SqlValue)
			{
				if (expr.TryEvaluateExpression(context, out var value))
					expr = new SqlValue(expr.GetExpressionType(), value);
			}

			return expr;
		}

		protected static bool IsBooleanParameter(ISqlExpression expr, int count, int i)
		{
			if ((i % 2 == 1 || i == count - 1) && expr.SystemType == typeof(bool) || expr.SystemType == typeof(bool?))
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SearchCondition: return true;
				}
			}

			return false;
		}

		protected SqlFunction ConvertFunctionParameters(SqlFunction func, bool withParameters = false)
		{
			if (func.Name == "CASE")
			{
				ISqlExpression[]? parameters = null;
				for (var i = 0; i < func.Parameters.Length; i++)
				{
					var p = func.Parameters[i];
					if (IsBooleanParameter(p, func.Parameters.Length, i))
					{
						if (parameters == null)
						{
							parameters = new ISqlExpression[func.Parameters.Length];
							for (var j = 0; j < i; j++)
								parameters[j] = func.Parameters[j];
						}
						parameters[i] = new SqlFunction(typeof(bool), "CASE", p, new SqlValue(true), new SqlValue(false))
						{
							CanBeNull     = false,
							DoNotOptimize = true
						};
					}
					else if (parameters != null)
						parameters[i] = p;
				}

				if (parameters != null)
					return new SqlFunction(
						func.SystemType,
						func.Name,
						false,
						func.Precedence,
						parameters);
			}

			return func;
		}

		#endregion


	}
}
