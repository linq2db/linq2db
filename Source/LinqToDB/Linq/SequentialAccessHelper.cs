using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq
{
	using Extensions;
	using LinqToDB.Expressions;
	using LinqToDB.Linq.Internal;
	using LinqToDB.Reflection;

	internal static class SequentialAccessHelper
	{
		public static Expression OptimizeMappingExpressionForSequentialAccess(Expression expression, int fieldCount, bool reduce)
		{
			if (reduce)
				expression = expression.Transform(e =>
					e is ConvertFromDataReaderExpression conv
						? conv.Reduce()
						: e);

			string? failMessage = null;

			var newVariables        = new ParameterExpression?[fieldCount * 2];
			var insertedExpressions = new Expression?[fieldCount * 2];
			var replacements        = new Expression?[fieldCount * 2];
			var replacedMethods     = new MethodInfo[fieldCount];
			var isNullableStruct    = new bool[fieldCount];

			Func<Expression, Expression> tranformFunc = null!;
			tranformFunc = e =>
			{
				if (failMessage != null)
					return e;

				if (e is MethodCallExpression call)
				{
					// column index
					int? columnIndex = null;

					// we work only with:
					// - instance method of data reader
					// - method, marked with ColumnReaderAttribute
					// - ColumnReader.GetValueSequential

					// ColumnReaderAttribute method
					var attr = call.Method.GetCustomAttribute<ColumnReaderAttribute>();
					if (attr != null && call.Arguments[attr.IndexParameterIndex] is ConstantExpression c1 && c1.Type == typeof(int))
						columnIndex = (int)c1.Value;

					// instance method of data reader
					// check that method accept single integer constant as parameter
					// this is currently how we detect method that we must process
					if (attr == null && !call.Method.IsStatic && typeof(IDataReader).IsAssignableFrom(call.Object.Type)
						&& call.Arguments.Count == 1 && call.Arguments[0] is ConstantExpression c2 && c2.Type == typeof(int))
						columnIndex = (int)c2.Value;

					if (columnIndex != null)
					{
						// test IsDBNull method by-name to support overrides
						if (call.Object != null && typeof(IDataReader).IsAssignableFrom(call.Object.Type) && call.Method.Name == nameof(IDataReader.IsDBNull))
						{
							var index = columnIndex.Value * 2;
							if (newVariables[index] == null)
							{
								var variable               = Expression.Variable(typeof(bool), $"is_null_{columnIndex}");
								newVariables[index]        = variable;
								replacements[index]        = variable;
								insertedExpressions[index] = Expression.Assign(variable, call);
							}

							return replacements[index]!;
						}
						else
						{
							// other methods we treat as Get* methods
							var index = columnIndex.Value * 2 + 1;
							if (newVariables[index] == null)
							{
								var type = call.Type;
								ParameterExpression variable;

								if (newVariables[index - 1] == null)
								{
									// no IsDBNull call: column is not nullable
									// (also could be a bad expression)

									variable                   = Expression.Variable(type, $"get_value_{columnIndex}");
									insertedExpressions[index] = Expression.Assign(variable, Expression.Convert(call, type));
								}
								else
								{
									var isNullable = type.IsValueType && !type.IsNullable();
									if (isNullable)
									{
										type                                = typeof(Nullable<>).MakeGenericType(type);
										isNullableStruct[columnIndex.Value] = true;
									}

									variable                   = Expression.Variable(type, $"get_value_{columnIndex}");
									insertedExpressions[index] = Expression.Assign(
										variable,
										Expression.Condition(
											newVariables[index - 1],
											Expression.Constant(null, type),
											isNullable ? Expression.Convert(call, type) : call));
								}

								newVariables[index]                = variable;
								replacements[index]                = isNullableStruct[columnIndex.Value] ? Expression.Property(variable, "Value") : variable;
								replacedMethods[columnIndex.Value] = call.Method;
							}
							else if (replacedMethods[columnIndex.Value] != call.Method)
							{
								// tried to replace multiple methods
								failMessage = $"Multiple data reader methods called for column: {replacedMethods[columnIndex.Value]} vs {call.Method}";
								return e;
							}

							return replacements[index]!;
						}
					}
					else if (call.Method == Methods.LinqToDB.ColumnReader.GetValueSequential
						&& call.Object is ConstantExpression c3
						&& c3.Value is ConvertFromDataReaderExpression.ColumnReader columnReader)
					{
						columnIndex = columnReader.ColumnIndex;
						var index   = columnIndex.Value * 2 + 1;

						if (replacedMethods[columnIndex.Value] == null)
						{
							var type       = call.Type;
							isNullableStruct[columnIndex.Value] = type.IsValueType && !type.IsNullable();
							if (isNullableStruct[columnIndex.Value])
								type = typeof(Nullable<>).MakeGenericType(type);

							var variable                       = Expression.Variable(typeof(object), $"get_value_{columnIndex}");
							newVariables[index]                = variable;
							insertedExpressions[index]         = Expression.Assign(variable, call.Update(call.Object, call.Arguments.Select(a => a.Transform(tranformFunc))));
							replacements[index]                = variable;
							replacedMethods[columnIndex.Value] = call.Method;
						}
						else if (replacedMethods[columnIndex.Value] != call.Method)
						{
							// tried to replace multiple methods
							failMessage = $"Multiple data reader methods called for column: {replacedMethods[columnIndex.Value]} vs {call.Method}";
							return e;
						}

						return replacements[index]!;
					}

					foreach (var arg in call.Arguments)
					{
						// unknown method call with data reader parameter
						if (typeof(IDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							failMessage = $"Method {call.Method.DeclaringType?.Name}.{call.Method.Name} with {nameof(IDataReader)} parameter not supported";
						}
					}
				}
				else if (e is InvocationExpression invoke)
				{
					foreach (var arg in invoke.Arguments)
					{
						// invoke call with data reader parameter
						if (typeof(IDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							failMessage = $"Method invoke with {nameof(IDataReader)} parameter not supported";
						}
					}
				}
				else if (e is NewExpression newExpr)
				{
					foreach (var arg in newExpr.Arguments)
					{
						// unknown constructor call with data reader parameter
						if (typeof(IDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							failMessage = $"{newExpr.Type} constructor with {nameof(IDataReader)} parameter not supported";
						}
					}
				}

				return e;
			};

			expression = expression.Transform(tranformFunc);

			// expression cannot be optimized
			if (failMessage != null)
				throw new LinqToDBException($"{nameof(OptimizeMappingExpressionForSequentialAccess)} optimization failed: {failMessage}");

			// insert variables and variable init code to mapping expression
			var updated = false;
			expression = expression.Transform(e =>
			{
				if (!updated && e is BlockExpression block)
				{
					updated = true;

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
						block.Variables.Concat(newVariables.Where(v => v != null)),
						block.Expressions.Take(skip + 1).Concat(insertedExpressions.Where(e => e != null)).Concat(block.Expressions.Skip(skip + 1)));
				}

				return e;
			});

			return expression;
		}

		public static Expression OptimizeColumnReaderForSequentialAccess(Expression expression, Expression isNullParameter, int columnIndex)
		{
			var     failed      = false;
			string? failMessage = null;

			expression = expression.Transform(e =>
			{
				if (failed)
					return e;

				if (e is MethodCallExpression call)
				{
					// we work only with instance method of data reader
					if (!call.Method.IsStatic && typeof(IDataReader).IsAssignableFrom(call.Object.Type))
					{
						// check that method accept single integer constant as parameter
						// this is currently how we detect method that we must process
						if (call.Arguments.Count == 1
							&& call.Arguments[0] is ConstantExpression c
							&& c.Type == typeof(int))
						{
							var idx = (int)c.Value;
							if (idx != columnIndex)
							{
								failed = true;
								failMessage = $"Expected column index: {columnIndex}, but found {idx}";
								return e;
							}


							// test IsDBNull method by-name to support overrides
							if (call.Method.Name == nameof(IDataReader.IsDBNull))
								return isNullParameter;

							// TODO: add additional validation for other methods?
							return e;
						}

						failed = true;
						failMessage = $"Unsupported reader method call: {call.Method.DeclaringType?.Name}.{call.Method.Name}";
						return e;
					}

					foreach (var arg in call.Arguments)
					{
						// unknown method or constructor call with data reader parameter
						if (typeof(IDataReader).IsAssignableFrom(arg.Unwrap().Type))
						{
							failMessage = $"Method {call.Method.DeclaringType?.Name}.{call.Method.Name} with {nameof(IDataReader)} not supported";
							failed = true;
						}
					}
				}

				return e;
			});

			// expression cannot be optimized
			if (failed)
				throw new LinqToDBException($"{nameof(OptimizeColumnReaderForSequentialAccess)} optimization failed (slow mode): {failMessage}");

			return expression;
		}
	}
}
