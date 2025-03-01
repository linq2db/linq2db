using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Extensions;
using LinqToDB.Reflection;

namespace LinqToDB.Internal.Linq
{
	internal static class SequentialAccessHelper
	{
		private static readonly TransformVisitor<object?> _reducer = TransformVisitor<object?>.Create(Reducer);
		private static Expression Reducer(Expression e)
		{
			return e is ConvertFromDataReaderExpression
						? e.Reduce()
						: e;
		}

		// shared between two visitors to avoid extra context allocation
		private sealed class OptimizeMappingExpressionForSequentialAccessContext
		{
			public OptimizeMappingExpressionForSequentialAccessContext(int fieldCount)
			{
				NewVariables        = new ParameterExpression?[fieldCount * 2];
				InsertedExpressions = new Expression?[fieldCount * 2];
				Replacements        = new Expression?[fieldCount * 2];
				ReplacedMethods     = new MethodInfo[fieldCount];
				IsNullableStruct    = new bool[fieldCount];
			}

			// slow mode column types
			public Dictionary<int, Tuple<ConvertFromDataReaderExpression.ColumnReader, ISet<Type>>>? SlowColumnTypes;

			public Expression? DataReaderExpr;
			public string?     FailMessage;
			public bool        Updated;

			public readonly ParameterExpression?[] NewVariables;
			public readonly Expression?[]          Replacements;
			public readonly Expression?[]          InsertedExpressions;
			public readonly MethodInfo[]           ReplacedMethods;
			public readonly bool[]                 IsNullableStruct;
		}

		public static Expression OptimizeMappingExpressionForSequentialAccess(Expression expression, int fieldCount, bool reduce)
		{
			if (reduce)
				expression = _reducer.Transform(expression);

			static Expression TranformFunc(OptimizeMappingExpressionForSequentialAccessContext context, Expression e)
			{
				if (context.FailMessage != null)
					return e;

				if (e is MethodCallExpression call)
				{
					// we work only with:
					// - instance method of data reader
					// - method, marked with ColumnReaderAttribute
					// - ColumnReader.GetValueSequential
					var columnIndex = TryGetColumnIndex(call);

					if (columnIndex != null)
					{
						// test IsDBNull method by-name to support overrides
						if (call.Object != null && typeof(DbDataReader).IsAssignableFrom(call.Object.Type) && call.Method.Name == nameof(DbDataReader.IsDBNull))
						{
							var index = columnIndex.Value * 2;
							if (context.NewVariables[index] == null)
							{
								var variable                       = Expression.Variable(typeof(bool), FormattableString.Invariant($"is_null_{columnIndex}"));
								context.NewVariables[index]        = variable;
								context.Replacements[index]        = variable;
								context.InsertedExpressions[index] = Expression.Assign(variable, call);
							}

							return context.Replacements[index]!;
						}
						else
						{
							// other methods we treat as Get* methods
							var index = columnIndex.Value * 2 + 1;
							if (context.NewVariables[index] == null)
							{
								var type = call.Type;
								ParameterExpression variable;

								if (context.NewVariables[index - 1] == null)
								{
									// no IsDBNull call: column is not nullable
									// (also could be a bad expression)
									variable                           = Expression.Variable(type, FormattableString.Invariant($"get_value_{columnIndex}"));
									context.InsertedExpressions[index] = Expression.Assign(variable, Expression.Convert(call, type));
								}
								else
								{
									var isNullable = !type.IsNullableType();
									if (isNullable)
									{
										type                                        = type.AsNullable();
										context.IsNullableStruct[columnIndex.Value] = true;
									}

									variable                   = Expression.Variable(type, FormattableString.Invariant($"get_value_{columnIndex}"));
									context.InsertedExpressions[index] = Expression.Assign(
										variable,
										Expression.Condition(
											context.NewVariables[index - 1]!,
											Expression.Constant(null, type),
											isNullable ? Expression.Convert(call, type) : call));
								}

								context.NewVariables[index]                = variable;
								context.Replacements[index]                = context.IsNullableStruct[columnIndex.Value] ? Expression.Property(variable, "Value") : variable;
								context.ReplacedMethods[columnIndex.Value] = call.Method;
							}
							else if (context.ReplacedMethods[columnIndex.Value] != call.Method)
							{
								// tried to replace multiple methods
								context.FailMessage = $"Multiple data reader methods called for column: {context.ReplacedMethods[columnIndex.Value]} vs {call.Method}";
								return e;
							}

							return context.Replacements[index]!;
						}
					}
					else if (call.Method == Methods.LinqToDB.ColumnReader.GetValueSequential
						&& call.Object is ConstantExpression c3
						&& c3.Value is ConvertFromDataReaderExpression.ColumnReader columnReader)
					{
						columnIndex = columnReader.ColumnIndex;
						var index   = columnIndex.Value * 2 + 1;

						if (context.NewVariables[index] == null)
						{
							context.NewVariables[index] = Expression.Variable(typeof(object), FormattableString.Invariant($"get_value_{columnIndex}"));
							if (context.SlowColumnTypes == null)
							{
								context.SlowColumnTypes = new Dictionary<int, Tuple<ConvertFromDataReaderExpression.ColumnReader, ISet<Type>>>();
								context.DataReaderExpr  = call.Arguments[0];
							}

							context.SlowColumnTypes.Add(columnIndex.Value, new Tuple<ConvertFromDataReaderExpression.ColumnReader, ISet<Type>>(columnReader, new HashSet<Type>()));
						}

						context.SlowColumnTypes![columnIndex.Value].Item2.Add(columnReader.ColumnType);

						// replacement expression build later when we know all types
						return call.Update(
							call.Object,
							call.Arguments.Take(2).Select(a => a.Transform(context, TranformFunc)).Concat(new[] { context.NewVariables[index]! }));
					}

					foreach (var arg in call.Arguments)
					{
						// unknown method call with data reader parameter
						if (typeof(DbDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							context.FailMessage = $"Method {call.Method.DeclaringType?.Name}.{call.Method.Name} with {nameof(DbDataReader)} parameter not supported";
						}
					}
				}
				else if (e is InvocationExpression invoke)
				{
					foreach (var arg in invoke.Arguments)
					{
						// invoke call with data reader parameter
						if (typeof(DbDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							context.FailMessage = $"Method invoke with {nameof(DbDataReader)} parameter not supported";
						}
					}
				}
				else if (e is NewExpression newExpr)
				{
					foreach (var arg in newExpr.Arguments)
					{
						// unknown constructor call with data reader parameter
						if (typeof(DbDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							context.FailMessage = $"{newExpr.Type} constructor with {nameof(DbDataReader)} parameter not supported";
						}
					}
				}

				return e;
			};

			var ctx = new OptimizeMappingExpressionForSequentialAccessContext(fieldCount);

			expression = expression.Transform(ctx, TranformFunc);

			// expression cannot be optimized
			if (ctx.FailMessage != null)
				throw new LinqToDBException($"{nameof(OptimizeMappingExpressionForSequentialAccess)} optimization failed: {ctx.FailMessage}");

			// generate value readers for slow mode
			if (ctx.SlowColumnTypes != null)
			{
				foreach (var kvp in ctx.SlowColumnTypes)
				{
					var isNullVariable = ctx.NewVariables[kvp.Key * 2]!;
					var valueVariable  = ctx.NewVariables[kvp.Key * 2 + 1]!;

					ctx.InsertedExpressions[kvp.Key * 2 + 1] = Expression.Assign(
						valueVariable,
						Expression.Condition(
							isNullVariable,
							Expression.Constant(null, valueVariable.Type),
							Expression.Call(
								Expression.Constant(kvp.Value.Item1),
								Methods.LinqToDB.ColumnReader.GetRawValueSequential,
								ctx.DataReaderExpr!,
								Expression.Constant(kvp.Value.Item2.ToArray())),
							valueVariable.Type));
				}
			}

			// insert variables and variable init code to mapping expression
			expression = expression.Transform(ctx, static (context, e) =>
			{
				if (!context.Updated && e is BlockExpression block)
				{
					context.Updated = true;

					var found = false;
					int skip;
					for (skip = 0; skip < block.Expressions.Count; skip++)
					{
						if (block.Expressions[skip] is BinaryExpression binary
							&& binary.NodeType == ExpressionType.Assign
							&& binary.Left is ParameterExpression pe
							&& pe.Name == "ldr")
						{
							found = true;
							break;
						}
					}

					if (!found)
						throw new LinqToDBException($"{nameof(OptimizeMappingExpressionForSequentialAccess)} optimization failed: cannot find data reader assignment");

					// first N expressions init context variables
					return block.Update(
						block.Variables.Concat(context.NewVariables.Where(v => v != null))!,
						block.Expressions.Take(skip + 1).Concat(context.InsertedExpressions.Where(e => e != null)).Concat(block.Expressions.Skip(skip + 1))!);
				}

				return e;
			});

			return expression;
		}

		private static int? TryGetColumnIndex(MethodCallExpression call)
		{
			// ColumnReaderAttribute method
			var attr = call.Method.GetAttribute<ColumnReaderAttribute>();
			if (attr != null && call.Arguments[attr.IndexParameterIndex] is ConstantExpression c1 && c1.Type == typeof(int))
				return (int)c1.Value!;

			// instance method of data reader
			// check that method accept single integer constant as parameter
			// this is currently how we detect method that we must process
			if (attr == null && !call.Method.IsStatic && typeof(DbDataReader).IsAssignableFrom(call.Object!.Type)
				&& call.Arguments.Count == 1 && call.Arguments[0] is ConstantExpression c2 && c2.Type == typeof(int))
				return (int)c2.Value!;

			return null;
		}

		sealed class OptimizeColumnReaderForSequentialAccessContext
		{
			public OptimizeColumnReaderForSequentialAccessContext(Expression isNullParameter, Expression rawValueParameter, int columnIndex)
			{
				IsNullParameter   = isNullParameter;
				RawValueParameter = rawValueParameter;
				ColumnIndex       = columnIndex;
			}

			public readonly Expression IsNullParameter;
			public readonly Expression RawValueParameter;
			public readonly int        ColumnIndex;

			public string? FailMessage;
		}

		public static Expression OptimizeColumnReaderForSequentialAccess(Expression expression, Expression isNullParameter, Expression rawValueParameter, int columnIndex)
		{
			var ctx = new OptimizeColumnReaderForSequentialAccessContext(isNullParameter, rawValueParameter, columnIndex);
			expression = expression.Transform(ctx, static (context, e) =>
			{
				if (context.FailMessage != null)
					return e;

				if (e is MethodCallExpression call)
				{
					// we work only with instance method of data reader or method marked with ColumnReaderAttribute
					var idx = TryGetColumnIndex(call);

					if (idx != null)
					{
						// check that method accept single integer constant as parameter
						// this is currently how we detect method that we must process
						if (idx != context.ColumnIndex)
						{
							context.FailMessage = FormattableString.Invariant($"Expected column index: {context.ColumnIndex}, but found {idx}");
							return e;
						}

						// test IsDBNull method by-name to support overrides
						if (call.Method.Name == nameof(IDataReader.IsDBNull))
							return context.IsNullParameter;
						else // otherwise we treat it as Get*Value method (as we already extracted index without errors for it)
							return call.Type != context.RawValueParameter.Type ? Expression.Convert(context.RawValueParameter, call.Type) : context.RawValueParameter;
					}

					foreach (var arg in call.Arguments)
					{
						// unknown method or constructor call with data reader parameter
						if (typeof(DbDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							context.FailMessage = $"Method {call.Method.DeclaringType?.Name}.{call.Method.Name} with {nameof(DbDataReader)} not supported";
						}
					}
				}

				return e;
			});

			// expression cannot be optimized
			if (ctx.FailMessage != null)
				throw new LinqToDBException($"{nameof(OptimizeColumnReaderForSequentialAccess)} optimization failed (slow mode): {ctx.FailMessage}");

			return expression;
		}

		sealed class ExtractRawValueReaderContext
		{
			public ExtractRawValueReaderContext(int columnIndex)
			{
				ColumnIndex = columnIndex;
			}

			public readonly int ColumnIndex;

			public string?               FailMessage;
			public MethodCallExpression? RawCall;
		}

		public static MethodCallExpression ExtractRawValueReader(Expression expression, int columnIndex)
		{
			var ctx = new ExtractRawValueReaderContext(columnIndex);

			expression.Visit(ctx, static(context, e) =>
			{
				if (context.FailMessage != null)
					return;

				if (e is MethodCallExpression call)
				{
					var idx = TryGetColumnIndex(call);

					if (idx != null)
					{
						if (idx != context.ColumnIndex)
						{
							context.FailMessage = FormattableString.Invariant($"Expected column index: {context.ColumnIndex}, but found {idx}");
							return;
						}

						if (call.Method.Name != nameof(DbDataReader.IsDBNull))
						{
							if (context.RawCall == null)
								context.RawCall = call;
							else if (context.RawCall.Method != call.Method)
								throw new LinqToDBConvertException(
									$"Different data reader methods used for same column: '{context.RawCall.Method.DeclaringType?.Name}.{context.RawCall.Method.Name}' vs '{call.Method.DeclaringType?.Name}.{call.Method.Name}'");
						}
					}
				}
			});

			if (ctx.FailMessage != null)
				throw new LinqToDBException($"{nameof(OptimizeColumnReaderForSequentialAccess)} optimization failed (slow mode): {ctx.FailMessage}");

			if (ctx.RawCall == null)
				throw new LinqToDBException($"Cannot find column value reader in expression");

			return ctx.RawCall;
		}
	}
}
