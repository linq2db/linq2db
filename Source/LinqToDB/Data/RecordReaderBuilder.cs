using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;

namespace LinqToDB.Data
{
	sealed class RecordReaderBuilder
	{
		class EntityConstructor : EntityConstructorBase
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
				var idx = Builder.GetReaderIndex(column.ColumnName);
				if (idx < 0)
					return Expression.Constant(Builder.MappingSchema.GetDefaultValue(memberInfo.GetMemberType()));

				return new ConvertFromDataReaderExpression(memberInfo.GetMemberType(), idx, column.ValueConverter, DataReaderParam, Builder.DataContext);
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

		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(DbDataReader),  "rd");

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
			ReaderIndexes = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, static i => i, MappingSchema.ColumnNameComparer);
		}

		int GetReaderIndex(string columnName)
		{
			if (!ReaderIndexes.TryGetValue(columnName, out var value))
				return -1;
			return value;
		}

		public Func<DbDataReader, T> BuildReaderFunction<T>()
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
			
			var lambda = Expression.Lambda<Func<DbDataReader,T>>(generator.ResultExpression, DataReaderParam);

			if (Common.Configuration.OptimizeForSequentialAccess)
				lambda = (Expression<Func<DbDataReader, T>>)SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(lambda, Reader.FieldCount, reduce: true);

			return lambda.CompileExpression();
		}

	}
}
