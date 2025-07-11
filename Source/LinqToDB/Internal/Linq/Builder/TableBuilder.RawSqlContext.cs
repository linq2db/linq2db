using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
				throw new NotImplementedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				throw new NotImplementedException();
			}
		}

		static BuildSequenceResult BuildRawSqlTable(ExpressionBuilder builder, BuildInfo buildInfo, bool isScalar)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			var formatArg = methodCall.Arguments[1];

			PrepareRawSqlArguments(formatArg,
				methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null,
				out var format, out var arguments);

			var sqlArguments = new ISqlExpression[arguments.Count];

			var context = buildInfo.Parent ?? new SimpleSelectContext(builder.GetTranslationModifier(), builder, typeof(object), buildInfo.SelectQuery);

			for (var i = 0; i < arguments.Count; i++)
			{
				if (!builder.TryConvertToSql(context, arguments[i], out var arg))
					return BuildSequenceResult.Error(arguments[i]);

				sqlArguments[i] = arg;
			}

			return BuildSequenceResult.FromContext(new RawSqlContext(builder.GetTranslationModifier(), builder, buildInfo, methodCall.Method.GetGenericArguments()[0], isScalar, format, sqlArguments));
		}

		public static void PrepareRawSqlArguments(Expression formatArg, Expression? parametersArg, out string format, out IReadOnlyList<Expression> arguments)
		{
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
			}
			else
			{
				var evaluatedSql = formatArg.EvaluateExpression()!;
				if (evaluatedSql is FormattableString formattable)
				{
					format     = formattable.Format;

					var array = formattable.GetArguments();
					var args   = new Expression[array.Length];

					for (var i = 0; i < array.Length; i++)
					{
						Expression expr = Expression.Constant(array[i], array[i]?.GetType() ?? typeof(object));
						args[i] = expr;
					}

					arguments = args;
				}
				else
				{
					var rawSqlString = (RawSqlString)evaluatedSql;

					format        = rawSqlString.Format;
					var arrayExpr = parametersArg!;

					if (arrayExpr.NodeType == ExpressionType.NewArrayInit)
					{
						arguments = ((NewArrayExpression)arrayExpr).Expressions;
					}
					else
					{
						var array = arrayExpr.EvaluateExpression<object[]>()!;
						var args  = new Expression[array.Length];
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
					var table = (SqlRawSqlTable)SqlTable;

					//TODO: It is strictly coupled with SQLBuilder logic. Maybe we can unify. Feels like we should refactor this logic

					// in case when we have alias placeholder we should not generate any fields
					if (table.Parameters.All(p => p.ElementType != QueryElementType.SqlAliasPlaceholder))
					{
						var sql = SqlTable.Fields.FirstOrDefault(f => f.Name == "value");
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
