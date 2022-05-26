using LinqToDB.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB
{

		public interface IModificationHandler
	{
		Type EntityType { get; }
	}

	public interface IModificationHandler<T> : IModificationHandler where T : notnull
	{
		IDictionary<LambdaExpression, object> ExpressionValues { get; }
		Expression<Func<T, bool>> Predicate { get; set; }
		ITable<T> Table { get; }
	}

	public static class IModificationHandlerExtensions
	{
		public static T? FirstOrDefault<T>(this IModificationHandler<T> modificationHandler) where T : notnull => modificationHandler.GetQueryable().FirstOrDefault();
		public static T? FirstOrDefault<T>(this IModificationHandler<T> modificationHandler, Expression<Func<T, bool>> predicate) where T : notnull => modificationHandler.GetQueryable().FirstOrDefault(predicate);
		public static T? FirstOrInsert<T>(this IModificationHandler<T> modificationHandler) where T : notnull => modificationHandler.GetQueryable().FirstOrInsert(modificationHandler.GetValueInsertable());
		public static T FirstOrInsertWithOutput<T>(this IModificationHandler<T> modificationHandler, T obj) where T : notnull => modificationHandler.Table.FirstOrInsertWithOutput(obj, modificationHandler.Predicate);
		public static int InsertIfNotExists<T>(this IModificationHandler<T> modificationHandler, T obj) where T : notnull => modificationHandler.Table.InsertIfNotExists(obj, modificationHandler.Predicate);

		public static IModificationHandler<T> AddPredicateValue<T, TValue>(this IModificationHandler<T> modificationHandler, Expression<Func<T, TValue>> getField, TValue value) where T : notnull
		{
			modificationHandler.Predicate = modificationHandler.Predicate.AndAlso(getField.Equal(value));
			modificationHandler.SetValue(getField, value);
			return modificationHandler;
		}

		public static IModificationHandler<T> SetValue<T, TV>(this IModificationHandler<T> modificationHandler, Expression<Func<T, TV>> field, TV value) where T : notnull
		{
			modificationHandler.ExpressionValues[field] = Expression.Constant(value);
			return modificationHandler;
		}

		public static IModificationHandler<T> SetValue<T, TV>(this IModificationHandler<T> modificationHandler, Expression<Func<T, TV>> field, Expression<Func<T, TV>> value) where T : notnull
		{
			modificationHandler.ExpressionValues[field] = Expression.Constant(value);
			return modificationHandler;
		}

		public static IModificationHandler<T> SetValue<T, TV>(this IModificationHandler<T> modificationHandler, Expression<Func<T, TV>> field, Expression<Func<TV>> value) where T : notnull
		{
			modificationHandler.ExpressionValues[field] = Expression.Constant(value);
			return modificationHandler;
		}

		public static IQueryable<T> GetQueryable<T>(this IModificationHandler<T> modificationHandler) where T : notnull => modificationHandler.Table.Where(modificationHandler.Predicate);

		public static IValueInsertable<T> GetValueInsertable<T>(this IModificationHandler<T> modificationHandler) where T : notnull
		{
			var source = modificationHandler.Table.AsValueInsertable();//  table.DataContext.Into(table);
			foreach (var kvp in modificationHandler.ExpressionValues)
			{
				source = source.ValueLambda(kvp.Key, kvp.Value);
			}
			return source;
		}

		public static IUpdatable<T> GetUpdatable<T>(this IModificationHandler<T> modificationHandler) where T : notnull
		{
			var updatable = modificationHandler.GetQueryable().AsUpdatable();
			foreach (var kvp in modificationHandler.ExpressionValues)
			{
				updatable = updatable.SetLambda(kvp.Key, kvp.Value);
			}
			return updatable;
		}

		public static int Delete<T>(this IModificationHandler<T> modificationHandler) where T : notnull => modificationHandler.GetQueryable().Delete();
		public static Task<int> DeleteaAsync<T>(this IModificationHandler<T> modificationHandler, CancellationToken token = default) where T : notnull => modificationHandler.GetQueryable().DeleteAsync(token);
		public static int Insert<T>(this IModificationHandler<T> modificationHandler) where T : notnull => modificationHandler.GetValueInsertable().Insert();
		public static Task<int> InsertAsync<T>(this IModificationHandler<T> modificationHandler, CancellationToken token = default) where T : notnull => modificationHandler.GetValueInsertable().InsertAsync(token);
		public static int Update<T>(this IModificationHandler<T> modificationHandler) where T : notnull => modificationHandler.GetUpdatable().Update();
		public static Task<int> UpdateAsync<T>(this IModificationHandler<T> modificationHandler, CancellationToken token = default) where T : notnull => modificationHandler.GetUpdatable().UpdateAsync(token);

	}

}
