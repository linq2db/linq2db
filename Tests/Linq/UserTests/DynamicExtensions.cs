using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace Tests.UserTests
{
	public static class DynamicExtensions
	{
		private static readonly MethodInfo SetValueMethodInfo = MemberHelper
			.MethodOf<object>(o => ((IUpdatable<object>)null).Set(null, 0))
			.GetGenericMethodDefinition();

		private static readonly MethodInfo SetExpressionMethodInfo = MemberHelper
			.MethodOf<object>(o => ((IUpdatable<object>)null).Set(null, v => v.ToString()))
			.GetGenericMethodDefinition();

		private static readonly MethodInfo InnerJoinMethodInfo = MemberHelper
			.MethodOf<object>(o => ((IQueryable<object>)null).InnerJoin<object, object, object>(null, null, null))
			.GetGenericMethodDefinition();

		private static readonly MethodInfo SqlPropertyMethodInfo = typeof(Sql).GetMethod("Property")
			.GetGenericMethodDefinition();

		public enum FieldSource
		{
			Propety,
			Column
		}

		public static Func<ParameterExpression, KeyValuePair<string, object>, Expression> GetFieldFunc(
			FieldSource fieldSource)
		{
			switch (fieldSource)
			{
				case FieldSource.Propety:
					return GetPropertyExpression;
				case FieldSource.Column:
					return GetColumnExpression;
				default:
					throw new ArgumentOutOfRangeException(nameof(fieldSource), fieldSource, null);
			}
		}

		public static IQueryable<T> FilterByValues<T>(this IQueryable<T> source,
			IEnumerable<KeyValuePair<string, object>> values,
			Func<ParameterExpression, KeyValuePair<string, object>, Expression> fieldFunc)
		{
			var param = Expression.Parameter(typeof(T));

			foreach (var pair in values)
			{
				var fieldExpression = fieldFunc(param, pair);
				if (fieldExpression != null)
				{
					var equality = Expression.Equal(fieldExpression,
						Expression.Constant(pair.Value, fieldExpression.Type));
					var lambda = Expression.Lambda<Func<T, bool>>(equality, param);
					source = source.Where(lambda);
				}
			}

			return source;
		}

		public static IQueryable<T> FilterByValues<T>(this IQueryable<T> source,
			IEnumerable<KeyValuePair<string, object>> values,
			FieldSource fieldSource = FieldSource.Propety)
		{
			return FilterByValues(source, values, GetFieldFunc(fieldSource));
		}

		public static IUpdatable<T> SetValues<T>(this IUpdatable<T> source,
			IEnumerable<KeyValuePair<string, object>> values,
			Func<ParameterExpression, KeyValuePair<string, object>, Expression> fieldFunc)
		{
			var param = Expression.Parameter(typeof(T));
			object current = source;
			foreach (var pair in values)
			{
				var fieldExpression = fieldFunc(param, pair);
				if (fieldExpression != null)
				{
					var lambda = Expression.Lambda(fieldExpression, param);

					var method = SetValueMethodInfo.MakeGenericMethod(typeof(T), fieldExpression.Type);
					current = method.Invoke(null, new[] { current, lambda, pair.Value });
				}
			}

			return (IUpdatable<T>)current;
		}

		public static IUpdatable<T> SetValues<T>(this IQueryable<T> source,
			IEnumerable<KeyValuePair<string, object>> values,
			FieldSource fieldSource = FieldSource.Propety)
		{
			return source.AsUpdatable().SetValues(values, fieldSource);
		}

		public static IUpdatable<T> SetValues<T>(this IUpdatable<T> source,
			IEnumerable<KeyValuePair<string, object>> values,
			FieldSource fieldSource = FieldSource.Propety)
		{
			return SetValues(source, values, GetFieldFunc(fieldSource));
		}

		public static IUpdatable<T> SetValues<T>(this IQueryable<T> source, T obj, MappingSchema schema = null)
		{
		    return source.AsUpdatable().SetValues(obj, schema);
		}

		public static IUpdatable<T> SetValues<T>(this IUpdatable<T> source, T obj, MappingSchema schema = null)
		{
			schema = schema ?? MappingSchema.Default;
			var descriptor = schema.GetEntityDescriptor(typeof(T));

			var toUpdate = descriptor.Columns.Where(c => !c.SkipOnUpdate);
			var param    = Expression.Parameter(typeof(T));

			object current = source;
			foreach (var c in toUpdate)
				if (c.MemberAccessor.Getter != null)
				{
					var value        = c.MemberAccessor.Getter(obj);
					var memberAccess = Expression.MakeMemberAccess(param, c.MemberInfo);
					var lambda       = Expression.Lambda(memberAccess, param);
					var method       = SetValueMethodInfo.MakeGenericMethod(typeof(T), memberAccess.Type);
					current          = method.Invoke(null, new[] { current, lambda, value });
				}

			return (IUpdatable<T>)current;
		}

		public static Expression GetPropertyExpression(ParameterExpression instance, KeyValuePair<string, object> pair)
		{
			var propInfo = instance.Type.GetProperty(pair.Key);
			if (propInfo == null)
				return null;

			var propExpression = Expression.MakeMemberAccess(instance, propInfo);

			return propExpression;
		}

		public static Expression GetColumnExpression(ParameterExpression instance, KeyValuePair<string, object> pair)
		{
			var valueType = pair.Value != null ? pair.Value.GetType() : typeof(string);

			var method = SqlPropertyMethodInfo.MakeGenericMethod(valueType);
			var sqlPropertyCall =
				Expression.Call(null, method, instance, Expression.Constant(pair.Key, typeof(string)));
			var memberInfo = MemberHelper.GetMemberInfo(sqlPropertyCall);
			var memberAccess = Expression.MakeMemberAccess(instance, memberInfo);

			return memberAccess;
		}

		public class JoinHelper<TOuter, TInner>
		{
			public TOuter Outer { get; set; } 
			public TInner Inner { get; set; } 
		}

		private static ITable<T> CreateTempTable<T>(DataConnection dataConnection, string tableName = null)
		{
			// temporary solution, will unify Temporary Table creation

			if (tableName == null)
			{
				tableName = "temp_" + dataConnection.MappingSchema.GetEntityDescriptor(typeof(T)).TableName;
			}

			if (dataConnection.DataProvider is MySqlDataProvider)
				return dataConnection.CreateTable<T>(tableName, statementHeader: "CREATE TEMPORARY TABLE {0}");
			if (dataConnection.DataProvider is SqlServerDataProvider)
			{
				if (!tableName.StartsWith("#"))
					tableName = tableName + "#";
				return dataConnection.CreateTable<T>(tableName);
			}

			throw new NotImplementedException(
				$"CreateTempTable is not implemented for \"{dataConnection.DataProvider.GetType()}\"");
		}

		public static int BulkUpdate<T>(
			this DataConnection dataConnection, 
			IEnumerable<T>      items, 
			ITable<T>           target, 
			params Expression<Func<T, object>>[] fields
		) 
			where T : class
		{
			var temp = CreateTempTable<T>(dataConnection);
			try
			{
				temp.BulkCopy(items);
				return BuildBulkUpdate(dataConnection, target, temp, fields)
					.Update();
			}
			finally
			{
				temp.Drop();
			}
		}

		public static async Task<int> BulkUpdateAsync<T>(
			this DataConnection dataConnection, 
			IEnumerable<T>      items, 
			ITable<T>           target, 
			params Expression<Func<T, object>>[] fields
		) 
			where T : class
		{
			var temp = CreateTempTable<T>(dataConnection);
			try
			{
				temp.BulkCopy(items);
				return await BuildBulkUpdate(dataConnection, target, temp, fields)
					.UpdateAsync();
			}
			finally
			{
				await temp.DropAsync();
			}
		}

		public static IUpdatable<JoinHelper<T, T>> BuildBulkUpdate<T>(
			DataConnection      dataConnection, 
			ITable<T>           target, 
			ITable<T>           sourceTable, 
			params Expression<Func<T, object>>[] fields
		) where T : class
		{
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var members    = new List<MemberInfo>();

			if (fields.Length == 0)
			{
				members.AddRange(
					descriptor.Columns
						.Where(c => !c.SkipOnUpdate & !c.IsPrimaryKey & !c.IsIdentity)
						.Select(c => c.MemberInfo)
				);
			}
			else
			{
				foreach (var field in fields)
				{
					var member = MemberHelper.GetMemberInfo(field);
					if (member == null)
						throw new LinqToDBException($"Can not retrive member info from lambda function: {field}");
					members.Add(member);
				}
			}

			/* Emulating the following calls

				var join = target.InnerJoin(temporaryTable, (outer, inner) => outer.ID1 == inner.ID1 && outer.ID2 == inner.ID2,
					(outer, inner) => new DynamicExtensions.JoinHelper<BulkUpdateEntity, BulkUpdateEntity>
					{
						Outer = outer,
						Inner = inner
					});

				var updatable = join.AsUpdatable();
				updatable = updatable.Set(pair => pair.Outer.Value1, pair => pair.Inner.Value1);
				updatable = updatable.Set(pair => pair.Outer.Value2, pair => pair.Inner.Value2);
			 */

			var joinMethod = InnerJoinMethodInfo.MakeGenericMethod(typeof(T), typeof(T), typeof(JoinHelper<T, T>));

			var outerParam = Expression.Parameter(typeof(T), "outer");
			var innerParam = Expression.Parameter(typeof(T), "inner");

			Expression cmpExpression = null;
			foreach (var c in descriptor.Columns.Where(c => c.IsPrimaryKey))
			{
				var equals = Expression.Equal(
					Expression.MakeMemberAccess(outerParam, c.MemberInfo),
					Expression.MakeMemberAccess(innerParam, c.MemberInfo)
				);
				cmpExpression = cmpExpression == null ? equals : Expression.AndAlso(cmpExpression, equals);
			}

			if (cmpExpression == null)
				throw new LinqToDBException($"Entity {typeof(T)} does not contain primary key");

			var equalityLambda = Expression.Lambda(cmpExpression, outerParam, innerParam);

			var helperType = typeof(JoinHelper<T, T>);
			var constructor = helperType.GetConstructor(new Type[0]);

			var joinHelperOuterPropertyInfo = helperType.GetProperty("Outer");
			var joinHelperInnerPropertyInfo = helperType.GetProperty("Inner");

			var newExpression =
				Expression.Lambda(
					Expression.MemberInit(
						Expression.New(constructor),
						Expression.Bind(joinHelperOuterPropertyInfo, outerParam),
						Expression.Bind(joinHelperInnerPropertyInfo, innerParam)
					),
					outerParam,
					innerParam);

			var joinQuery = joinMethod.Invoke(null, new object[] { target.AsQueryable(), sourceTable.AsQueryable(), equalityLambda, newExpression });
			object updatable = ((IQueryable<JoinHelper<T, T>>)joinQuery).AsUpdatable();

			var helperParam = Expression.Parameter(helperType, "pair");
			var outerExpression = Expression.MakeMemberAccess(helperParam, joinHelperOuterPropertyInfo);
			var innerExpression = Expression.MakeMemberAccess(helperParam, joinHelperInnerPropertyInfo);

			foreach (var memberInfo in members)
			{
				var method = SetExpressionMethodInfo.MakeGenericMethod(helperType, memberInfo.GetMemberType());

				var propLambda  = Expression.Lambda(Expression.MakeMemberAccess(outerExpression, memberInfo), helperParam);
				var valueLambda = Expression.Lambda(Expression.MakeMemberAccess(innerExpression, memberInfo), helperParam);

				updatable = method.Invoke(null, new object[] { updatable, propLambda, valueLambda });
			}

			return (IUpdatable<JoinHelper<T, T>>)updatable;
		}
	}
}
