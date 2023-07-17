using System;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlProvider
{
	public class SqlExpressionConvertVisitor : SqlQueryVisitor
	{
		OptimizationContext _optimizationContext = default!;
		EvaluationContext   _evaluationContext   = default!;
		NullabilityContext  _nullabilityContext  = default!;
		DataOptions         _dataOptions         = default!;
		MappingSchema       _mappingSchema       = default!;

		SqlProviderFlags?   _sqlProviderFlags;

		readonly SqlDataType _typeWrapper = new(default(DbDataType));

		public SqlExpressionConvertVisitor(VisitMode visitMode) : base(visitMode)
		{
		}


		public virtual IQueryElement Convert(OptimizationContext optimizationContext, NullabilityContext nullabilityContext, SqlProviderFlags? sqlProviderFlags, DataOptions dataOptions, MappingSchema mappingSchema, IQueryElement element)
		{
			Cleanup();
			_optimizationContext = optimizationContext;
			_evaluationContext   = optimizationContext.Context;
			_nullabilityContext  = nullabilityContext;
			_sqlProviderFlags    = sqlProviderFlags;
			_dataOptions         = dataOptions;
			_mappingSchema       = mappingSchema;

			return Visit(element);
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

			if (!_mappingSchema.ValueToSqlConverter.CanConvert(_typeWrapper, _dataOptions, element.Value))
			{
				// we cannot generate SQL literal, so just convert to parameter
				var param = _optimizationContext.SuggestDynamicParameter(element.ValueType, element.Value);
				return param;
			}

			return element;
		}

		public override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var newElement = base.VisitExprExprPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (predicate.Expr1.ElementType == QueryElementType.SqlRow)
				return ConvertRowExprExpr(predicate, _evaluationContext);

			return predicate;
		}

		public override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			var newElement = base.VisitInListPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (predicate.Expr1.ElementType == QueryElementType.SqlRow)
				return ConvertRowInList(predicate);

			return predicate;
		}

		public override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			var newElement = base.VisitSqlFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);


			switch (element.Name)
			{
				case "Average": return new SqlFunction(element.SystemType, "Avg", element.Parameters);
				case "Max":
				case "Min":
				{
					if (element.SystemType == typeof(bool) || element.SystemType == typeof(bool?))
					{
						return new SqlFunction(typeof(int), element.Name,
							new SqlFunction(element.SystemType, "CASE", element.Parameters[0], new SqlValue(1), new SqlValue(0)) { CanBeNull = false });
					}

					break;
				}

				case PseudoFunctions.CONVERT:
					return ConvertConversion(element);

				case PseudoFunctions.TO_LOWER: return element.WithName("Lower");
				case PseudoFunctions.TO_UPPER: return element.WithName("Upper");
				case PseudoFunctions.REPLACE : return element.WithName("Replace");
				case PseudoFunctions.COALESCE: return element.WithName("Coalesce");

				case "ConvertToCaseCompareTo":
				{
					return new SqlFunction(element.SystemType, "CASE",
						new SqlSearchCondition().Expr(element.Parameters[0]).Greater.Expr(element.Parameters[1])
							.ToExpr(), new SqlValue(1),
						new SqlSearchCondition().Expr(element.Parameters[0]).Equal.Expr(element.Parameters[1]).ToExpr(),
						new SqlValue(0),
						new SqlValue(-1)) { CanBeNull = false };
				}
			}

			return element;
		}

		public override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			var newElement = base.VisitSqlBinaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			switch (element.Operation)
			{
				case "+":
				{
					if (element.Expr1.SystemType == typeof(string) && element.Expr2.SystemType != typeof(string))
					{
						var len = element.Expr2.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(_mappingSchema.GetDataType(element.Expr2.SystemType).Type.DataType);

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
						var len = element.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(_mappingSchema.GetDataType(element.Expr1.SystemType).Type.DataType);

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
			if (_sqlProviderFlags == null)
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
					if (!_sqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.IsNull))
						return RowIsNullFallback((SqlRow)predicate.Expr1, op == SqlPredicate.Operator.NotEqual);
					break;
				}

				// ROW(a, b) operator ROW(c, d)
				case SqlRow rhs:
				{
					if (!_sqlProviderFlags.RowConstructorSupport.HasFlag(feature))
						return RowComparisonFallback(op, (SqlRow)predicate.Expr1, rhs, context);
					break;
				}

				// ROW(a, b) operator (SELECT c, d)
				case SelectQuery:
				{
					if (!_sqlProviderFlags.RowConstructorSupport.HasFlag(feature) ||
					    !_sqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.CompareToSelect))
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
			if (_sqlProviderFlags == null)
				return predicate;

			if (!_sqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.In))
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
	}
}
