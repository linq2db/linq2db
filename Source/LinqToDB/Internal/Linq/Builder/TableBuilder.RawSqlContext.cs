using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class TableBuilder
	{
		sealed class SimpleSelectContext : BuildContextBase
		{
			public SimpleSelectContext(TranslationModifier translationModifier, ExpressionBuilder builder, Type elementType, SelectQuery selectQuery) 
				: base(translationModifier, builder, elementType, selectQuery)
			{
			}

			public override MappingSchema MappingSchema => Builder.MappingSchema;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotSupportedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				throw new NotSupportedException();
			}
		}

		static BuildSequenceResult BuildRawSqlTable(ExpressionBuilder builder, BuildInfo buildInfo, bool? isScalar)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;
			var entityType = methodCall.Method.GetGenericArguments()[0];

			isScalar ??= builder.MappingSchema.IsScalarType(entityType);

			var formatArg = methodCall.Arguments[1];

			PrepareRawSqlArguments(formatArg,
				methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null,
				true,
				out var format, out var arguments, out var cacheDependencies);

			if (cacheDependencies != null)
			{
				foreach (var (expression, value) in cacheDependencies)
					builder.ParametersContext.MarkAsValue(expression, value);
			}

			var sqlArguments = new ISqlExpression[arguments.Count];

			var context = buildInfo.Parent ?? new SimpleSelectContext(builder.GetTranslationModifier(), builder, typeof(object), buildInfo.SelectQuery);

			for (var i = 0; i < arguments.Count; i++)
			{
				if (!builder.TryConvertToSql(context, arguments[i], out var arg))
					return BuildSequenceResult.Error(arguments[i]);

				sqlArguments[i] = arg;
			}

			return BuildSequenceResult.FromContext(new RawSqlContext(builder.GetTranslationModifier(), builder, buildInfo, entityType, isScalar.Value, format, sqlArguments));
		}

		static readonly MethodInfo _getArgumentsMethod = MemberHelper.MethodOf<FormattableString>(fs => fs.GetArguments());
		static readonly MethodInfo _getTypeMethod      = MemberHelper.MethodOf<object>(o => o.GetType());

		static bool IsConstantValue(Expression expression)
		{
			while (expression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				expression = ((UnaryExpression)expression).Operand;

			return expression.NodeType == ExpressionType.Constant;
		}

		// Accessors built over a captured argument array are typed by the build-time values, so a cached query
		// is reusable only while the count, the runtime types and any inlined ISqlExpression stay the same.
		// The count goes first: comparison stops at the first mismatch, before indexing a shorter array.
		static void AddArgumentDependencies(ref List<(Expression expression, object? value)>? dependencies, Expression arrayExpr, object?[] array)
		{
			dependencies ??= [];
			dependencies.Add((Expression.ArrayLength(arrayExpr), array.Length));

			for (var i = 0; i < array.Length; i++)
			{
				var itemExpr = Expression.ArrayIndex(arrayExpr, ExpressionInstances.Int32(i));
				var typeExpr = Expression.Condition(
					Expression.Equal(itemExpr, Expression.Constant(null)),
					Expression.Constant(null, typeof(Type)),
					Expression.Call(itemExpr, _getTypeMethod));

				dependencies.Add((typeExpr, array[i]?.GetType()));

				if (array[i] is ISqlExpression)
					dependencies.Add((itemExpr, array[i]));
			}
		}

		public static void PrepareRawSqlArguments(
			Expression                                        formatArg,
			Expression?                                       parametersArg,
			bool                                              parameterizeCapturedArguments,
			out string                                        format,
			out IReadOnlyList<Expression>                     arguments,
			out List<(Expression expression, object? value)>? cacheDependencies)
		{
			cacheDependencies = null;

			// Consider that FormattableString is used
			if (formatArg.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)formatArg;

				if (mc.Arguments[1].NodeType != ExpressionType.NewArrayInit)
				{
					format    = mc.Arguments[0].EvaluateExpression<string>()!;
					var args  = new Expression[mc.Arguments.Count - 1];

					for (var i = 0; i < args.Length; i++)
						args[i] = mc.Arguments[i + 1];

					arguments = args;
				}
				else
				{
					format    = mc.Arguments[0].EvaluateExpression<string>()!;
					arguments = ((NewArrayExpression)mc.Arguments[1]).Expressions;
				}

				// format text is baked into SQL, so a captured format must take part in cache comparison
				if (!IsConstantValue(mc.Arguments[0]))
					cacheDependencies = [(mc.Arguments[0], format)];
			}
			else
			{
				var isConstant   = IsConstantValue(formatArg);
				var evaluatedSql = formatArg.EvaluateExpression()!;
				if (evaluatedSql is FormattableString formattable)
				{
					format     = formattable.Format;

					var formattableExpr = formatArg.Type == typeof(FormattableString) ? formatArg : Expression.Convert(formatArg, typeof(FormattableString));

					if (!isConstant)
						cacheDependencies = [(Expression.Property(formattableExpr, nameof(FormattableString.Format)), format)];

					var array = formattable.GetArguments();
					var args   = new Expression[array.Length];

					var argumentsExpr = parameterizeCapturedArguments && !isConstant ? Expression.Call(formattableExpr, _getArgumentsMethod) : null;

					if (argumentsExpr != null)
						AddArgumentDependencies(ref cacheDependencies, argumentsExpr, array);

					for (var i = 0; i < array.Length; i++)
					{
						var value = array[i];

						if (argumentsExpr == null || value is null or ISqlExpression)
						{
							args[i] = Expression.Constant(value, value?.GetType() ?? typeof(object));
							continue;
						}

						args[i] = Expression.Convert(Expression.ArrayIndex(argumentsExpr, ExpressionInstances.Int32(i)), value.GetType());
					}

					arguments = args;
				}
				else
				{
					var rawSqlString = (RawSqlString)evaluatedSql;

					format        = rawSqlString.Format;
					var arrayExpr = parametersArg!;

					if (!isConstant)
						cacheDependencies = [(Expression.PropertyOrField(formatArg.Type == typeof(RawSqlString) ? formatArg : Expression.Convert(formatArg, typeof(RawSqlString)), nameof(RawSqlString.Format)), format)];

					if (arrayExpr.NodeType == ExpressionType.NewArrayInit)
					{
						arguments = ((NewArrayExpression)arrayExpr).Expressions;
					}
					else
					{
						var array = arrayExpr.EvaluateExpression<object[]>()!;
						var args  = new Expression[array.Length];

						if (parameterizeCapturedArguments && !IsConstantValue(arrayExpr))
							AddArgumentDependencies(ref cacheDependencies, arrayExpr, array);

						for (var i = 0; i < array.Length; i++)
						{
							var type = array[i]?.GetType() ?? typeof(object);

							if (typeof(ISqlExpression).IsAssignableFrom(type))
							{
								args[i] = Expression.Constant(array[i]);
								continue;
							}

							Expression expr = Expression.ArrayIndex(arrayExpr, ExpressionInstances.Int32(i));
							if (type != typeof(object))
								expr = Expression.Convert(expr, type);

							args[i] = expr;
						}

						arguments = args;
					}
				}
			}
		}

		//TODO: We have to separate TableContext in proper hierarchy
		sealed class RawSqlContext : TableContext
		{
			public bool IsScalar { get; }

			public RawSqlContext(TranslationModifier translationModifier, ExpressionBuilder builder, BuildInfo buildInfo, Type originalType, bool isScalar, string sql, ISqlExpression[] parameters)
				: base(translationModifier, builder, builder.MappingSchema, buildInfo, new SqlRawSqlTable(builder.MappingSchema.GetEntityDescriptor(originalType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated), sql, isScalar, parameters))
			{
				IsScalar = isScalar;

				if (isScalar)
				{
					// Marking All field as not nullable for satisfying DefaultIfEmptyBuilder
					SqlTable.CanBeNull = false;

					var dbDataType = MappingSchema.GetDbDataType(originalType);
					var field      = new SqlField(dbDataType, "value", true);
					SqlTable.Add(field);
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (IsScalar && flags.IsSqlOrExpression() && SequenceHelper.IsSameContext(path, this))
				{
					var table = (SqlRawSqlTable)NamedTable;

					//TODO: It is strictly coupled with SQLBuilder logic. Maybe we can unify. Feels like we should refactor this logic

					// in case when we have alias placeholder we should not generate any fields
					if (table.Parameters.All(p => p.ElementType != QueryElementType.SqlAliasPlaceholder))
					{
						var sql = SqlTable.Fields.Find(f => string.Equals(f.Name, "value", StringComparison.Ordinal));
						if (sql != null)
						{
							return ExpressionBuilder.CreatePlaceholder(this, sql, path);
						}
					}
				}

				return base.MakeExpression(path, flags);
			}
		}
	}
}
