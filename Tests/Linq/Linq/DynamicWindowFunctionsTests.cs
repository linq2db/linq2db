using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	public static class DynamicWindowFunctionsExtensions
	{
		#region Helpers

		static Expression? Unwrap(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote:
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
					return ((UnaryExpression)ex).Operand.Unwrap();
			}

			return ex;
		}

		static MethodInfo? FindMethodInfoInType(Type type, string methodName, int paramCount)
		{
			var method = type.GetRuntimeMethods()
				.FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == paramCount);
			return method;
		}

		static MethodInfo FindMethodInfo(Type type, string methodName, int paramCount)
		{
			var method = FindMethodInfoInType(type, methodName, paramCount);

			if (method != null)
				return method;

			method = type.GetInterfaces().Select(it => FindMethodInfoInType(it, methodName, paramCount))
				.FirstOrDefault(m => m != null);

			if (method == null)
				throw new InvalidOperationException($"Method '{methodName}' not found in type '{type.Name}'.");

			return method;
		}

		static Expression ExtractOrderByPart(Expression query, List<Tuple<Expression, bool>> orderBy)
		{
			var current = query;
			while (current.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)current;
				if (typeof(Queryable) == mc.Method.DeclaringType)
				{
					var supported = true;
					switch (mc.Method.Name)
					{
						case "OrderBy":
						case "ThenBy":
						{
							orderBy.Add(Tuple.Create(mc.Arguments[1], false));
							break;
						}
						case "OrderByDescending":
						case "ThenByDescending":
						{
							orderBy.Add(Tuple.Create(mc.Arguments[1], true));
							break;
						}
						default:
							supported = false;
							break;
					}

					if (!supported)
						break;

					current = mc.Arguments[0];
				}
				else
					break;
			}

			return current;
		}

		static Expression FinalizeFunction(Expression functionBody)
		{
			var toValueMethodInfo = FindMethodInfo(functionBody.Type, "ToValue", 0);
			functionBody = Expression.Call(functionBody, toValueMethodInfo);
			return functionBody;
		}

		static Expression GenerateOrderBy(Expression entity, Expression functionBody, List<Tuple<Expression, bool>> orderBy)
		{
			var isFirst = true;

			for (int i = orderBy.Count - 1; i >= 0; i--)
			{
				var order = orderBy[i];
				string methodName;
				if (order.Item2)
					methodName = isFirst ? "OrderByDesc" : "ThenByDesc";
				else
					methodName = isFirst ? "OrderBy" : "ThenBy";
				isFirst = false;

				var currentType = functionBody.Type;
				var methodInfo = FindMethodInfo(currentType, methodName, 1).GetGenericMethodDefinition();

				var arg = ((LambdaExpression)Unwrap(order.Item1)!).GetBody(entity);

				functionBody = Expression.Call(functionBody, methodInfo.MakeGenericMethod(arg.Type), arg);
			}

			return functionBody;
		}

		static Expression GeneratePartitionBy(Expression functionBody, Expression[] partitionBy)
		{
			if (partitionBy.Length == 0)
				return functionBody;

			var method = FindMethodInfo(functionBody.Type, "PartitionBy", 1);

			var partitionsExpr = Expression.NewArrayInit(typeof(object), partitionBy);

			var call = Expression.Call(functionBody, method, partitionsExpr);

			return call;
		}

		#endregion
		
		public class RankHolder<T>
		{
			public T Data = default!;
			public long Rank;
		}

		public static IQueryable<RankHolder<T>> SelectRanked<T>(this IOrderedQueryable<T> query,
			params Expression<Func<T, object>>[] partitionBy)
		{
			var orderBy = new List<Tuple<Expression, bool>>();
			var withoutOrder = ExtractOrderByPart(query.Expression, orderBy);

			Expression<Func<T, AnalyticFunctions.IOverMayHavePartitionAndOrder<long>>> overExpression =
				t => Sql.Ext.Rank().Over();

			var entityParam = Expression.Parameter(typeof(T), "e");
			var windowFunctionBody = overExpression.Body;
			windowFunctionBody = GeneratePartitionBy(windowFunctionBody,
				partitionBy.Select(p => p.GetBody(entityParam)).ToArray());

			windowFunctionBody = GenerateOrderBy(entityParam, windowFunctionBody, orderBy);
			windowFunctionBody = FinalizeFunction(windowFunctionBody);

			// Generating Select
			//
			var dataMember = MemberHelper.FieldOf((RankHolder<T> h) => h.Data);
			var rankMember = MemberHelper.FieldOf((RankHolder<T> h) => h.Rank);

			var bindings = new List<MemberBinding>
			{
				Expression.Bind(rankMember, windowFunctionBody), 
				Expression.Bind(dataMember, entityParam)
			};

			var newExpression = Expression.New(typeof(RankHolder<T>).GetConstructor(Type.EmptyTypes)!);

			var selectLambda = Expression.Lambda(Expression.MemberInit(newExpression, bindings), entityParam);
			var selectQuery  = TypeHelper.MakeMethodCall(LinqToDB.Reflection.Methods.Queryable.Select, withoutOrder, Expression.Quote(selectLambda));

			// Done! query is ready
			//
			var rankQuery = query.Provider.CreateQuery<RankHolder<T>>(selectQuery);
			return rankQuery;
		}
	}

	[TestFixture]
	public class DynamicWindowFunctionsTests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column] public int Id     { get; set; }
			[Column] public int Value1 { get; set; }
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var query = table.OrderBy(t => t.Id).ThenByDescending(t => t.Value1).SelectRanked(x => x.Value1);
				var result = query.ToArray();
			}
		}
	}
}
