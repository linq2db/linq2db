using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Reflection;

namespace LinqToDB.Expressions
{
	internal static class EqualsToVisitor
	{
		#region Cache
		static readonly ConcurrentDictionary<MethodInfo,IList<SqlQueryDependentAttribute?>?> _queryDependentMethods = new ();

		public static void ClearCaches()
		{
			_queryDependentMethods.Clear();
		}
		#endregion

		internal static bool EqualsTo(
			this Expression                                           expr1,
			Expression                                                expr2,
			IDataContext                                              dataContext,
			IReadOnlyDictionary<Expression, QueryableAccessor>?       queryableAccessorDic,
			IReadOnlyDictionary<MemberInfo, QueryableMemberAccessor>? queryableMemberAccessorDic,
			IReadOnlyDictionary<Expression, Expression>?              queryDependedObjects,
			bool                                                      compareConstantValues = false)
		{
			return EqualsTo(expr1, expr2, PrepareEqualsInfo(dataContext, queryableAccessorDic, queryableMemberAccessorDic, queryDependedObjects, compareConstantValues));
		}

		/// <summary>
		/// Creates reusable equality context.
		/// </summary>
		internal static EqualsToInfo PrepareEqualsInfo(
			IDataContext                                              dataContext,
			IReadOnlyDictionary<Expression, QueryableAccessor>?       queryableAccessorDic       = null,
			IReadOnlyDictionary<MemberInfo, QueryableMemberAccessor>? queryableMemberAccessorDic = null,
			IReadOnlyDictionary<Expression, Expression>?              queryDependedObjects       = null,
			bool                                                      compareConstantValues      = false)
		{
			return new EqualsToInfo(dataContext, queryableAccessorDic, queryableMemberAccessorDic, queryDependedObjects, compareConstantValues);
		}

		internal class EqualsToInfo
		{
			public EqualsToInfo(
				IDataContext                                              dataContext,
				IReadOnlyDictionary<Expression, QueryableAccessor>?       queryableAccessorDic,
				IReadOnlyDictionary<MemberInfo, QueryableMemberAccessor>? queryableMemberAccessorDic,
				IReadOnlyDictionary<Expression, Expression>?              queryDependedObjects,
				bool                                                      compareConstantValues)
			{
				DataContext                = dataContext;
				QueryableAccessorDic       = queryableAccessorDic;
				QueryableMemberAccessorDic = queryableMemberAccessorDic;
				QueryDependedObjects       = queryDependedObjects;
				CompareConstantValues      = compareConstantValues;
			}

			public readonly IDataContext                                              DataContext;
			public readonly IReadOnlyDictionary<MemberInfo, QueryableMemberAccessor>? QueryableMemberAccessorDic;
			public readonly IReadOnlyDictionary<Expression, Expression>?              QueryDependedObjects;
			public readonly IReadOnlyDictionary<Expression, QueryableAccessor>?       QueryableAccessorDic;
			public readonly bool                                                      CompareConstantValues;

			public HashSet<Expression>?          Visited;
			public Dictionary<MemberInfo, bool>? MemberCompareCache;

			public void Reset()
			{
				Visited?.Clear();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool CompareMemberExpression(MemberInfo memberInfo, EqualsToInfo info)
		{
			if (info.QueryableMemberAccessorDic == null ||
				!info.QueryableMemberAccessorDic.TryGetValue(memberInfo, out var accessor))
				return true;

			if (info.MemberCompareCache == null || !info.MemberCompareCache.TryGetValue(memberInfo, out var compareResult))
			{
				compareResult = accessor.Expression.EqualsTo(accessor.Accessor(memberInfo, info.DataContext), info);
				(info.MemberCompareCache ??= new (MemberInfoComparer.Instance)).Add(memberInfo, compareResult);
			}

			return compareResult;
		}

		internal static bool EqualsTo(this Expression? expr1, Expression? expr2, EqualsToInfo info)
		{
			if (expr1 == expr2)
			{
				if (info.QueryableMemberAccessorDic == null || expr1 == null)
					return true;
			}

			if (expr1 == null || expr2 == null || expr1.NodeType != expr2.NodeType || expr1.Type != expr2.Type)
				return false;

			switch (expr1.NodeType)
			{
				case ExpressionType.Add               :
				case ExpressionType.AddChecked        :
				case ExpressionType.And               :
				case ExpressionType.AndAlso           :
				case ExpressionType.ArrayIndex        :
				case ExpressionType.Assign            :
				case ExpressionType.Coalesce          :
				case ExpressionType.Divide            :
				case ExpressionType.Equal             :
				case ExpressionType.ExclusiveOr       :
				case ExpressionType.GreaterThan       :
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift         :
				case ExpressionType.LessThan          :
				case ExpressionType.LessThanOrEqual   :
				case ExpressionType.Modulo            :
				case ExpressionType.Multiply          :
				case ExpressionType.MultiplyChecked   :
				case ExpressionType.NotEqual          :
				case ExpressionType.Or                :
				case ExpressionType.OrElse            :
				case ExpressionType.Power             :
				case ExpressionType.RightShift        :
				case ExpressionType.Subtract          :
				case ExpressionType.SubtractChecked   :
				{
					//						var e1 = (BinaryExpression)expr1;
					//						var e2 = (BinaryExpression)expr2;
					return
						((BinaryExpression)expr1).Method == ((BinaryExpression)expr2).Method &&
						((BinaryExpression)expr1).Conversion.EqualsTo(((BinaryExpression)expr2).Conversion, info) &&
						((BinaryExpression)expr1).Left.EqualsTo(((BinaryExpression)expr2).Left, info) &&
						((BinaryExpression)expr1).Right.EqualsTo(((BinaryExpression)expr2).Right, info);
				}

				case ExpressionType.ArrayLength   :
				case ExpressionType.Convert       :
				case ExpressionType.ConvertChecked:
				case ExpressionType.Negate        :
				case ExpressionType.NegateChecked :
				case ExpressionType.Not           :
				case ExpressionType.Quote         :
				case ExpressionType.TypeAs        :
				case ExpressionType.UnaryPlus     :
				{
					//						var e1 = (UnaryExpression)expr1;
					//						var e2 = (UnaryExpression)expr2;
					return
						((UnaryExpression)expr1).Method == ((UnaryExpression)expr2).Method &&
						((UnaryExpression)expr1).Operand.EqualsTo(((UnaryExpression)expr2).Operand, info);
				}

				case ExpressionType.Conditional:
				{
					//						var e1 = (ConditionalExpression)expr1;
					//						var e2 = (ConditionalExpression)expr2;
					return
						((ConditionalExpression)expr1).Test   .EqualsTo(((ConditionalExpression)expr2).Test, info) &&
						((ConditionalExpression)expr1).IfTrue .EqualsTo(((ConditionalExpression)expr2).IfTrue, info) &&
						((ConditionalExpression)expr1).IfFalse.EqualsTo(((ConditionalExpression)expr2).IfFalse, info);
				}

				case ExpressionType.Call          : return EqualsToX((MethodCallExpression)expr1, (MethodCallExpression        )expr2, info);
				case ExpressionType.Constant      : return EqualsToX((ConstantExpression  )expr1, (ConstantExpression          )expr2, info);
				case ExpressionType.Invoke        : return EqualsToX((InvocationExpression)expr1, (InvocationExpression        )expr2, info);
				case ExpressionType.Lambda        : return EqualsToX((LambdaExpression    )expr1, (LambdaExpression            )expr2, info);
				case ExpressionType.ListInit      : return EqualsToX((ListInitExpression  )expr1, (ListInitExpression          )expr2, info);
				case ExpressionType.MemberAccess  : return EqualsToX((MemberExpression    )expr1, (MemberExpression            )expr2, info);
				case ExpressionType.MemberInit    : return EqualsToX((MemberInitExpression)expr1, (MemberInitExpression        )expr2, info);
				case ExpressionType.New           : return EqualsToX((NewExpression       )expr1, (NewExpression               )expr2, info);
				case ExpressionType.NewArrayBounds:
				case ExpressionType.NewArrayInit  : return EqualsToX((NewArrayExpression  )expr1, (NewArrayExpression          )expr2, info);
				case ExpressionType.Default       : return true;
				case ExpressionType.Parameter     : return ((ParameterExpression          )expr1).Name == ((ParameterExpression)expr2).Name;

				case ExpressionType.TypeIs:
				{
					//						var e1 = (TypeBinaryExpression)expr1;
					//						var e2 = (TypeBinaryExpression)expr2;
					return
						((TypeBinaryExpression)expr1).TypeOperand == ((TypeBinaryExpression)expr2).TypeOperand &&
						((TypeBinaryExpression)expr1).Expression.EqualsTo(((TypeBinaryExpression)expr2).Expression, info);
				}

				case ExpressionType.Block:
					return EqualsToX((BlockExpression)expr1, (BlockExpression)expr2, info);

				case ChangeTypeExpression.ChangeTypeType:
					return
						((ChangeTypeExpression)expr1).Type == ((ChangeTypeExpression)expr2).Type &&
						((ChangeTypeExpression)expr1).Expression.EqualsTo(((ChangeTypeExpression)expr2).Expression, info);

				default:
					throw new NotImplementedException($"Unhandled expression type: {expr1.NodeType}");
			}
		}

		static bool EqualsToX(BlockExpression expr1, BlockExpression expr2, EqualsToInfo info)
		{
			for (var i = 0; i < expr1.Expressions.Count; i++)
				if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], info))
					return false;

			for (var i = 0; i < expr1.Variables.Count; i++)
				if (!expr1.Variables[i].EqualsTo(expr2.Variables[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(NewArrayExpression expr1, NewArrayExpression expr2, EqualsToInfo info)
		{
			if (expr1.Expressions.Count != expr2.Expressions.Count)
				return false;

			for (var i = 0; i < expr1.Expressions.Count; i++)
				if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(NewExpression expr1, NewExpression expr2, EqualsToInfo info)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count)
				return false;

			if (expr1.Members == null && expr2.Members != null)
				return false;

			if (expr1.Members != null && expr2.Members == null)
				return false;

			if (expr1.Constructor != expr2.Constructor)
				return false;

			if (expr1.Members != null)
			{
				if (expr1.Members.Count != expr2.Members!.Count)
					return false;

				for (var i = 0; i < expr1.Members.Count; i++)
					if (expr1.Members[i] != expr2.Members[i])
						return false;
			}

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(MemberInitExpression expr1, MemberInitExpression expr2, EqualsToInfo info)
		{
			if (expr1.Bindings.Count != expr2.Bindings.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, info))
				return false;

			for (var i = 0; i < expr1.Bindings.Count; i++)
			{
				var b1 = expr1.Bindings[i];
				var b2 = expr2.Bindings[i];

				if (!CompareBindings(b1, b2, info))
					return false;
			}

			return true;
		}

		static bool CompareBindings(MemberBinding? b1, MemberBinding? b2, EqualsToInfo info)
		{
			if (b1 == b2)
				return true;

			if (b1 == null || b2 == null || b1.BindingType != b2.BindingType || b1.Member != b2.Member)
				return false;

			switch (b1.BindingType)
			{
				case MemberBindingType.Assignment:
					return ((MemberAssignment)b1).Expression.EqualsTo(((MemberAssignment)b2).Expression, info);

				case MemberBindingType.ListBinding:
					var ml1 = (MemberListBinding)b1;
					var ml2 = (MemberListBinding)b2;

					if (ml1.Initializers.Count != ml2.Initializers.Count)
						return false;

					for (var i = 0; i < ml1.Initializers.Count; i++)
					{
						var ei1 = ml1.Initializers[i];
						var ei2 = ml2.Initializers[i];

						if (ei1.AddMethod != ei2.AddMethod || ei1.Arguments.Count != ei2.Arguments.Count)
							return false;

						for (var j = 0; j < ei1.Arguments.Count; j++)
							if (!ei1.Arguments[j].EqualsTo(ei2.Arguments[j], info))
								return false;
					}

					break;

				case MemberBindingType.MemberBinding:
					var mm1 = (MemberMemberBinding)b1;
					var mm2 = (MemberMemberBinding)b2;

					if (mm1.Bindings.Count != mm2.Bindings.Count)
						return false;

					for (var i = 0; i < mm1.Bindings.Count; i++)
						if (!CompareBindings(mm1.Bindings[i], mm2.Bindings[i], info))
							return false;

					break;
			}

			return true;
		}

		static bool EqualsToX(MemberExpression expr1, MemberExpression expr2, EqualsToInfo info)
		{
			if (expr1.Member == expr2.Member)
			{
				if (expr1.Expression == expr2.Expression || expr1.Expression.Type == expr2.Expression.Type)
				{
					if (info.QueryableAccessorDic != null && info.QueryableAccessorDic.TryGetValue(expr1, out var qa))
						return
							expr1.Expression.EqualsTo(expr2.Expression, info) &&
							qa.Queryable.Expression.EqualsTo(qa.Accessor(expr2).Expression, info);

					if (!CompareMemberExpression(expr1.Member, info))
						return false;
				}

				return expr1.Expression.EqualsTo(expr2.Expression, info);
			}

			return false;
		}

		static bool EqualsToX(ListInitExpression expr1, ListInitExpression expr2, EqualsToInfo info)
		{
			if (expr1.Initializers.Count != expr2.Initializers.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, info))
				return false;

			for (var i = 0; i < expr1.Initializers.Count; i++)
			{
				var i1 = expr1.Initializers[i];
				var i2 = expr2.Initializers[i];

				if (i1.Arguments.Count != i2.Arguments.Count || i1.AddMethod != i2.AddMethod)
					return false;

				for (var j = 0; j < i1.Arguments.Count; j++)
					if (!i1.Arguments[j].EqualsTo(i2.Arguments[j], info))
						return false;
			}

			return true;
		}

		static bool EqualsToX(LambdaExpression expr1, LambdaExpression expr2, EqualsToInfo info)
		{
			if (expr1.Parameters.Count != expr2.Parameters.Count || !expr1.Body.EqualsTo(expr2.Body, info))
				return false;

			for (var i = 0; i < expr1.Parameters.Count; i++)
				if (!expr1.Parameters[i].EqualsTo(expr2.Parameters[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(InvocationExpression expr1, InvocationExpression expr2, EqualsToInfo info)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || !expr1.Expression.EqualsTo(expr2.Expression, info))
				return false;

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(ConstantExpression expr1, ConstantExpression expr2, EqualsToInfo info)
		{
			if (expr1.Value == null && expr2.Value == null)
				return true;

			if (expr1.Type.IsConstantable(false))
				return Equals(expr1.Value, expr2.Value);

			if (expr1.Value == null || expr2.Value == null)
				return false;

			if (expr1.Value is IQueryable queryable)
			{
				var eq1 = queryable.Expression;
				var eq2 = ((IQueryable)expr2.Value).Expression;

				if ((info.Visited ??= new()).Add(eq1))
					return eq1.EqualsTo(eq2, info);
			}
			else if (expr1.Value is IEnumerable list1 && expr2.Value is IEnumerable list2)
			{
				var enum1 = list1.GetEnumerator();
				var enum2 = list2.GetEnumerator();
				using (enum1 as IDisposable)
				using (enum2 as IDisposable)
				{
					while (enum1.MoveNext())
					{
						if (!enum2.MoveNext() || !Equals(enum1.Current, enum2.Current))
							return false;
					}

					if (enum2.MoveNext())
						return false;
				}

				return true;
			}

			return !info.CompareConstantValues || expr1.Value == expr2.Value;
		}

		private static readonly MethodInfo[] SkipSecondArgumentMethods = new []
		{
			Methods.Queryable.Take      , Methods.Queryable.Skip,
			Methods.Enumerable.Take     , Methods.Enumerable.Skip,
			Methods.Queryable.ElementAt , Methods.Queryable.ElementAtOrDefault,
			Methods.Enumerable.ElementAt, Methods.Enumerable.ElementAtOrDefault
		};

		static bool EqualsToX(MethodCallExpression expr1, MethodCallExpression expr2, EqualsToInfo info)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || expr1.Method != expr2.Method)
				return false;

			if (!expr1.Object.EqualsTo(expr2.Object, info))
				return false;

			if (expr1.IsSameGenericMethod(SkipSecondArgumentMethods)
				&& expr1.Arguments[1].NodeType == ExpressionType.Constant && expr2.Arguments[1].NodeType == ExpressionType.Constant)
			{
				// We do not compare last argument
				return expr1.Arguments[0].EqualsTo(expr2.Arguments[0], info);
			}

			var dependentParameters = _queryDependentMethods.GetOrAdd(
				expr1.Method, static mi =>
				{
					var arr = mi
						.GetParameters()
						.Select(static p => (SqlQueryDependentAttribute?)p.GetCustomAttributes(typeof(SqlQueryDependentAttribute), false).OfType<SqlQueryDependentAttribute>().FirstOrDefault())
						.ToList();

					return arr.Any(static a => a != null) ? arr : null;
				});

			if (dependentParameters == null)
			{
				for (var i = 0; i < expr1.Arguments.Count; i++)
				{
					if (!DefaultCompareArguments(expr1.Arguments[i], expr2.Arguments[i], info))
						return false;
				}
			}
			else
			{
				for (var i = 0; i < expr1.Arguments.Count; i++)
				{
					var dependentAttribute = dependentParameters[i];

					if (dependentAttribute != null)
					{
						var enum1 = dependentAttribute.SplitExpression(expr1.Arguments[i]).GetEnumerator();
						var enum2 = dependentAttribute.SplitExpression(expr2.Arguments[i]).GetEnumerator();
						using (enum1)
						using (enum2)
						{
							while (enum1.MoveNext())
							{
								if (!enum2.MoveNext())
									return false;

								var arg1 = enum1.Current;
								var arg2 = enum2.Current;
								if (info.QueryDependedObjects != null && info.QueryDependedObjects.TryGetValue(arg1, out var nevValue))
									arg1 = nevValue;
								if (!dependentAttribute.ExpressionsEqual(info, arg1, arg2, static (info, e1, e2) => e1.EqualsTo(e2, info)))
									return false;
							}

							if (enum2.MoveNext())
								return false;
						}
					}
					else
					{
						if (!DefaultCompareArguments(expr1.Arguments[i], expr2.Arguments[i], info))
							return false;
					}
				}
			}

			if (info.QueryableAccessorDic != null && info.QueryableAccessorDic.TryGetValue(expr1, out var qa))
				return qa.Queryable.Expression.EqualsTo(qa.Accessor(expr2).Expression, info);

			if (!CompareMemberExpression(expr1.Method, info))
				return false;

			return true;
		}

		static bool DefaultCompareArguments(Expression arg1, Expression arg2, EqualsToInfo info)
		{
			if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(arg1.Type))
			{
				if (arg1.NodeType == ExpressionType.Constant && arg2.NodeType == ExpressionType.Constant)
				{
					var query1 = ((Sql.IQueryableContainer)arg1.EvaluateExpression()!).Query;
					var query2 = ((Sql.IQueryableContainer)arg2.EvaluateExpression()!).Query;
					return EqualsTo(query1.Expression, query2.Expression, info);
				}
			}
			if (!arg1.EqualsTo(arg2, info))
				return false;
			return true;
		}
	}
}
