using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;
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

			FormattableStringHelper.PrepareRawSqlArguments(formatArg,
				methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null,
				builder.DataContext.SqlProviderFlags.IsParameterOrderDependent,
				out var format, out var arguments);

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
