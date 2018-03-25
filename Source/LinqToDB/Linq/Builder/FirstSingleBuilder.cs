using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;
	using Common;

	class FirstSingleBuilder : MethodCallBuilder
	{
		public static string[] MethodNames = { "First", "FirstOrDefault", "Single", "SingleOrDefault" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable(MethodNames) &&
				methodCall.Arguments.Count == 1;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var take     = 0;

			if (!buildInfo.IsSubQuery || builder.DataContext.SqlProviderFlags.IsSubQueryTakeSupported)
			{
				switch (methodCall.Method.Name)
				{
					case "First"           :
					case "FirstOrDefault"  :
						take = 1;
						break;

					case "Single"          :
					case "SingleOrDefault" :
						if (!buildInfo.IsSubQuery)
						{
							var takeValue = buildInfo.SelectQuery.Select.TakeValue as SqlValue;

							if (takeValue != null && (int) takeValue.Value >= 2)
							{
								take = 2;
							}
						}

						break;
				}
			}

			if (take != 0)
				builder.BuildTake(sequence, new SqlValue(take), null);

			return new FirstSingleContext(buildInfo.Parent, sequence, methodCall);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			if (methodCall.Arguments.Count == 2)
			{
				var predicate = (LambdaExpression)methodCall.Arguments[1].Unwrap();
				var info      = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), predicate.Parameters[0]);

				if (info != null)
				{
					info.Expression = methodCall.Transform(ex => ConvertMethod(methodCall, 0, info, predicate.Parameters[0], ex));
					info.Parameter  = param;

					return info;
				}
			}
			else
			{
				var info = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), null);

				if (info != null)
				{
					info.Expression = methodCall.Transform(ex => ConvertMethod(methodCall, 0, info, null, ex));
					info.Parameter  = param;

					return info;
				}
			}

			return null;
		}

		public class FirstSingleContext : SequenceContextBase
		{
			public FirstSingleContext(IBuildContext parent, IBuildContext sequence, MethodCallExpression methodCall)
				: base(parent, sequence, null)
			{
				_methodCall = methodCall;
			}

			readonly MethodCallExpression _methodCall;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				Sequence.BuildQuery(query, queryParameter);

				switch (_methodCall.Method.Name)
				{
					case "First"           : GetFirstElement          (query); break;
					case "FirstOrDefault"  : GetFirstOrDefaultElement (query); break;
					case "Single"          : GetSingleElement         (query); break;
					case "SingleOrDefault" : GetSingleOrDefaultElement(query); break;
				}
			}

			static void GetFirstElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps) => query.GetIEnumerable(db, expr, ps).First();

				query.GetElementAsync = async (db, expr, ps, token) =>
				{
					var count = 0;
					var obj   = default(T);

					await query.GetForEachAsync(db, expr, ps,
						r => { obj = r; count++; return false; }, token);

					return count > 0 ? obj : Array<T>.Empty.First();
				};
			}

			static void GetFirstOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps) => query.GetIEnumerable(db, expr, ps).FirstOrDefault();

				query.GetElementAsync = async (db, expr, ps, token) =>
				{
					var count = 0;
					var obj   = default(T);

					await query.GetForEachAsync(db, expr, ps, r => { obj = r; count++; return false; }, token);

					return count > 0 ? obj : Array<T>.Empty.FirstOrDefault();
				};
			}

			static void GetSingleElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps) => query.GetIEnumerable(db, expr, ps).Single();

				query.GetElementAsync = async (db, expr, ps, token) =>
				{
					var count = 0;
					var obj   = default(T);

					await query.GetForEachAsync(db, expr, ps,
						r =>
						{
							if (count == 0)
								obj = r;
							count++;
							return count == 1;
						}, token);

					return count == 1 ? obj : new T[count].Single();
				};
			}

			static void GetSingleOrDefaultElement<T>(Query<T> query)
			{
				query.GetElement      = (db, expr, ps) => query.GetIEnumerable(db, expr, ps).SingleOrDefault();

				query.GetElementAsync = async (db, expr, ps, token) =>
				{
					var count = 0;
					var obj   = default(T);

					await query.GetForEachAsync(db, expr, ps,
						r =>
						{
							if (count == 0)
								obj = r;
							count++;
							return count == 1;
						}, token);

					return count == 1 ? obj : new T[count].SingleOrDefault();
				};
			}

			static object SequenceException()
			{
				return new object[0].First();
			}

			bool _isJoinCreated;

			void CreateJoin()
			{
				if (!_isJoinCreated)
				{
					_isJoinCreated = true;

					var join = SelectQuery.OuterApply();

					Parent.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				}
			}

			int _checkNullIndex = -1;

			int GetCheckNullIndex()
			{
				if (_checkNullIndex < 0)
				{
					_checkNullIndex = SelectQuery.Select.Add(new SqlValue(1));
					_checkNullIndex = ConvertToParentIndex(_checkNullIndex, this);
				}

				return _checkNullIndex;
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				if (expression == null || level == 0)
				{
					if (Builder.DataContext.SqlProviderFlags.IsApplyJoinSupported &&
						Parent.SelectQuery.GroupBy.IsEmpty &&
						Parent.SelectQuery.From.Tables.Count > 0)
					{
						CreateJoin();

						var expr = Sequence.BuildExpression(expression, expression == null ? level : level + 1, enforceServerSide);

						Expression defaultValue;

						if (_methodCall.Method.Name.EndsWith("OrDefault"))
							defaultValue = Expression.Constant(expr.Type.GetDefaultValue(), expr.Type);
						else
							defaultValue = Expression.Convert(
								Expression.Call(
									null,
									MemberHelper.MethodOf(() => SequenceException())),
								expr.Type);

						expr = Expression.Condition(
							Expression.Call(
								ExpressionBuilder.DataReaderParam,
								ReflectionHelper.DataReader.IsDBNull,
								Expression.Constant(GetCheckNullIndex())),
							defaultValue,
							expr);

						return expr;
					}

					if (expression == null)
					{
						if (Sequence.IsExpression(null, level, RequestFor.Object).Result)
							return Builder.BuildMultipleQuery(Parent, _methodCall, enforceServerSide);

						return Builder.BuildSql(_methodCall.Type, Parent.SelectQuery.Select.Add(SelectQuery));
					}

					return null;
				}

				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToSql(expression, level + 1, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToIndex(expression, level, flags);
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				return Sequence.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}
	}
}
