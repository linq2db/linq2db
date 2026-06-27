using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class RecordReaderBuilder
	{
		sealed class EntityConstructor : EntityConstructorBase
		{
			readonly ParameterExpression _readerVariable;
			public           RecordReaderBuilder Builder { get; }

			public EntityConstructor(RecordReaderBuilder builder, ParameterExpression readerVariable)
			{
				_readerVariable = readerVariable;
				Builder              = builder;
			}

			protected override Expression MakeAssignExpression(Expression objExpression, MemberInfo memberInfo, ColumnDescriptor column)
			{
				var memberType = memberInfo.GetMemberType();
				var idx        = Builder.GetReaderIndex(column.ColumnName);
				if (idx < 0)
					// Column absent from the result set (e.g. a raw SQL query that doesn't select it): bind the
					// member's typed default. The constant must carry memberType — an untyped null would be typed
					// object and rejected by the Assignment type check. Harmless on the LINQ path, where generated
					// SELECTs always include every mapped column so this branch isn't reached.
					return Expression.Constant(Builder.MappingSchema.GetDefaultValue(memberType), memberType);

				// Reduce against the sample reader now (fast mode) rather than emitting a slow-mode node that
				// dispatches through a ColumnReader for every row. Mirrors the main mapper path (QueryRunner) and the
				// previous raw-SQL fast path: _readerVariable is the typed (converter-wrapped) reader the read targets,
				// Builder.Reader is the sample used to bake the provider read method (2-arg Reduce also applies the
				// data-reader unwrap interceptor).
				return new ConvertFromDataReaderExpression(memberType, idx, column.ValueConverter, DataContextParam, _readerVariable, (bool?)null)
					.Reduce(Builder.DataContext, Builder.Reader);
			}

			protected override Expression MakeIsNullExpression(Expression objExpression, MemberInfo memberInfo, ColumnDescriptor column)
			{
				var idx = Builder.GetReaderIndex(column.ColumnName);
				if (idx < 0)
					return Expression.Constant(true);

				var isNullExpr = Expression.Call(
					_readerVariable,
					ReflectionHelper.DataReader.IsDBNull,
					ExpressionInstances.Int32Array(idx));

				return isNullExpr;
			}
		}

		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dc");
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(DbDataReader), "rd");

		public IDataContext           DataContext   { get; }
		public MappingSchema          MappingSchema { get; }
		public Type                   ObjectType    { get; }
		public DbDataReader           Reader        { get; }
		public LambdaExpression?      ConverterExpr { get; }
		public Dictionary<string,int> ReaderIndexes { get; }

		public RecordReaderBuilder(IDataContext dataContext, Type objectType, DbDataReader reader, LambdaExpression? converterExpr)
		{
			DataContext   = dataContext;
			MappingSchema = dataContext.MappingSchema;
			ObjectType    = objectType;
			Reader        = reader;
			ConverterExpr = converterExpr;
			// A raw-SQL result set can carry duplicate (or empty) column names — e.g. "select 1, 1" — which a
			// plain ToDictionary would reject. Keep the first index per name, matching GetReaderIndex's first-match lookup.
			var readerIndexes = new Dictionary<string,int>(MappingSchema.ColumnNameComparer);
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var name = reader.GetName(i);
				if (!readerIndexes.ContainsKey(name))
					readerIndexes[name] = i;
			}

			ReaderIndexes = readerIndexes;
		}

		int GetReaderIndex(string columnName)
		{
			if (!ReaderIndexes.TryGetValue(columnName, out var value))
				return -1;
			return value;
		}

		public Func<IDataContext,DbDataReader, T> BuildReaderFunction<T>()
		{
			var generator          = new ExpressionGenerator();
			var typedDataReader    = Expression.Convert(DataReaderParam, Reader.GetType());

			var dataReaderVariable = generator.AssignToVariable(ConverterExpr?.GetBody(typedDataReader) ?? typedDataReader, "ldr");

			var constructor        = new EntityConstructor(this, dataReaderVariable);

			var entityParam        = Expression.Parameter(ObjectType, "e");
			var entityExpression   = constructor.BuildFullEntityExpression(DataContext, MappingSchema, entityParam, ObjectType, ProjectFlags.Expression,
				EntityConstructorBase.FullEntityPurpose.Default);

			Expression finalized   = entityExpression;

			while (true)
			{
				var transformed = finalized.Transform((readerBuilder : this, constructor), static (ctx, e) =>
				{
					if (e is SqlGenericConstructorExpression generic)
					{
						return ctx.constructor.Construct(ctx.readerBuilder.DataContext, ctx.readerBuilder.MappingSchema,
							generic, ProjectFlags.Expression);
					}

					return e;
				});

				if (ReferenceEquals(transformed, finalized))
					break;

				finalized = transformed;
			}

			generator.AddExpression(finalized);
			
			var lambda = Expression.Lambda<Func<IDataContext,DbDataReader,T>>(generator.ResultExpression, DataContextParam, DataReaderParam);

			if (DataContext.Options.LinqOptions.OptimizeForSequentialAccess)
				// reduce: false — MakeAssignExpression already reduced the reads in fast mode, so no
				// ConvertFromDataReaderExpression nodes remain (matches the previous raw-SQL fast path).
				lambda = (Expression<Func<IDataContext, DbDataReader, T>>)SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(lambda, Reader.FieldCount, reduce: false);

			return lambda.CompileExpression();
		}

	}
}
