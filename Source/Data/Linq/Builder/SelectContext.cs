using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Sql;

namespace LinqToDB.Data.Linq.Builder
{
	// This class implements double functionality (scalar and member type selects)
	// and could be implemented as two different classes.
	// But the class means to have a lot of inheritors, and functionality of the inheritors
	// will be doubled as well. So lets double it once here.
	//
	public class SelectContext : IBuildContext
	{
		#region Init

#if DEBUG
		[CLSCompliant(false)]
		public string _sqlQueryText { get { return SqlQuery == null ? "" : SqlQuery.SqlText; } }

		public MethodCallExpression MethodCall;
#endif

		public IBuildContext[]   Sequence { get; set; }
		public LambdaExpression  Lambda   { get; set; }
		public Expression        Body     { get; set; }
		public ExpressionBuilder Builder  { get; private set; }
		public SqlQuery          SqlQuery { get; set; }
		public IBuildContext     Parent   { get; set; }
		public bool              IsScalar { get; private set; }

		Expression IBuildContext.Expression { get { return Lambda; } }

		public readonly Dictionary<MemberInfo,Expression> Members = new Dictionary<MemberInfo,Expression>(new MemberInfoComparer());

		public SelectContext(IBuildContext parent, LambdaExpression lambda, params IBuildContext[] sequences)
		{
			Parent   = parent;
			Sequence = sequences;
			Builder  = sequences[0].Builder;
			Lambda   = lambda;
			Body     = lambda.Body;
			SqlQuery = sequences[0].SqlQuery;

			foreach (var context in Sequence)
				context.Parent = this;

			IsScalar = !Builder.ProcessProjection(Members, Body);

			Builder.Contexts.Add(this);
		}

		#endregion

		#region BuildQuery

		public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			var expr   = BuildExpression(null, 0);
			var mapper = Builder.BuildMapper<T>(expr);

			query.SetQuery(mapper.Compile());
		}

		#endregion

		#region BuildExpression

		public virtual Expression BuildExpression(Expression expression, int level)
		{
			{
				var key = Tuple.Create(expression, level, ConvertFlags.Field);

				SqlInfo[] info;

				if (_expressionIndex.TryGetValue(key, out info))
				{
					var idx  = Parent == null ? info[0].Index : Parent.ConvertToParentIndex(info[0].Index, this);
					return Builder.BuildSql((expression ?? Body).Type, idx);
				}
			}

			if (expression == null)
				return Builder.BuildExpression(this, Body);

			var levelExpression = expression.GetLevelExpression(level);

			if (IsScalar)
			{
				if (Body.NodeType != ExpressionType.Parameter && level == 0)
					if (levelExpression == expression)
						if (IsSubQuery() && IsExpression(null, 0, RequestFor.Expression).Result)
						{
							var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
							var idx = Parent == null ? info.Index : Parent.ConvertToParentIndex(info.Index, this);

							return Builder.BuildSql(expression.Type, idx);
						}

				return ProcessScalar(
					expression,
					level,
					(ctx, ex, l) => ctx.BuildExpression(ex, l),
					() => GetSequence(expression, level).BuildExpression(null, 0));
			}
			else
			{
				if (level == 0)
				{
					var sequence = GetSequence(expression, level);

					return levelExpression == expression ?
						sequence.BuildExpression(null,       0) :
						sequence.BuildExpression(expression, level + 1);
				}

				switch (levelExpression.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var memberExpression = GetMemberExpression(
								((MemberExpression)levelExpression).Member,
								levelExpression == expression,
								levelExpression.Type);

							if (levelExpression == expression)
							{
								if (IsSubQuery())
								{
									switch (memberExpression.NodeType)
									{
										case ExpressionType.New        :
										case ExpressionType.MemberInit :
											{
												var sequence = GetSequence(memberExpression, 0);

												return memberExpression.Transform(e =>
												{
													if (e != memberExpression)
													{
														switch (e.NodeType)
														{
															case ExpressionType.MemberAccess :
																if (!sequence.IsExpression(e, 0, RequestFor.Object).Result &&
																	!sequence.IsExpression(e, 0, RequestFor.Field). Result)
																{
																	var info = ConvertToIndex(e, 0, ConvertFlags.Field).Single();
																	var idx  = Parent == null ? info.Index : Parent.ConvertToParentIndex(info.Index, this);

																	return Builder.BuildSql(e.Type, idx);
																}

																return Builder.BuildExpression(this, e);
														}
													}

													return e;
												});
											}
									}

									var me = memberExpression.NodeType == ExpressionType.Parameter ? null : memberExpression;

									if (!IsExpression(me, 0, RequestFor.Object).Result &&
										!IsExpression(me, 0, RequestFor.Field). Result)
									{
										var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
										var idx  = Parent == null ? info.Index : Parent.ConvertToParentIndex(info.Index, this);

										return Builder.BuildSql(expression.Type, idx);
									}
								}

								return Builder.BuildExpression(this, memberExpression);
							}

							{
								var sequence = GetSequence(expression, level);

								switch (memberExpression.NodeType)
								{
									case ExpressionType.Parameter  :
										{
											var parameter = Lambda.Parameters[Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence)];
										
											if (memberExpression == parameter)
												return sequence.BuildExpression(expression, level + 1);

											break;
										
										}

									case ExpressionType.New        :
									case ExpressionType.MemberInit :
										{
											var mmExpresion = GetMemberExpression(memberExpression, expression, level + 1);
											return Builder.BuildExpression(this, mmExpresion);
										}
								}

								var expr = expression.Transform(ex => ex == levelExpression ? memberExpression : ex);

								return sequence.BuildExpression(expr, 1);
							}
						}

					case ExpressionType.Parameter :
						break;
				}
			}

			throw new NotImplementedException();
		}

		#endregion

		#region ConvertToSql

		readonly Dictionary<MemberInfo,SqlInfo[]> _sql = new Dictionary<MemberInfo,SqlInfo[]>(new MemberInfoComparer());

		public virtual SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			if (expression != null && level > 0 && expression.NodeType == ExpressionType.Call)
			{
				var e = (MethodCallExpression)expression;

				if (e.Method.DeclaringType == typeof(Enumerable))
				{
					return new[] { new SqlInfo { Sql = Builder.SubQueryToSql(this, e) } };
				}
			}

			if (IsScalar)
			{
				if (expression == null)
					return Builder.ConvertExpressions(this, Body, flags);

				switch (flags)
				{
					case ConvertFlags.Field :
					case ConvertFlags.Key   :
					case ConvertFlags.All   :
						{
							if (Body.NodeType != ExpressionType.Parameter && level == 0)
							{
								var levelExpression = expression.GetLevelExpression(level);

								if (levelExpression != expression)
									if (flags != ConvertFlags.Field && IsExpression(expression, level, RequestFor.Field).Result)
										flags = ConvertFlags.Field;
							}

							return ProcessScalar(
								expression,
								level,
								(ctx, ex, l) => ctx.ConvertToSql(ex, l, flags),
								() => new[] { new SqlInfo { Sql = Builder.ConvertToSql(this, expression) } });
						}
				}
			}
			else
			{
				if (expression == null)
				{
					if (flags != ConvertFlags.Field)
					{
						var q =
							from m in Members
							where !(m.Key is MethodInfo)
							select ConvertMember(m.Key, m.Value, flags) into mm
							from m in mm
							select m;

						return q.ToArray();
					}

					throw new NotImplementedException();
				}

				switch (flags)
				{
					case ConvertFlags.All   :
					case ConvertFlags.Key   :
					case ConvertFlags.Field :
						{
							var levelExpression = expression.GetLevelExpression(level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										if (level != 0 && levelExpression == expression)
										{
											var member = ((MemberExpression)levelExpression).Member;

											SqlInfo[] sql;

											if (!_sql.TryGetValue(member, out sql))
											{
												var memberExpression = GetMemberExpression(
													member, levelExpression == expression, levelExpression.Type);

												sql = ConvertExpressions(memberExpression, flags);

												if (sql.Length == 1 && flags != ConvertFlags.Key)
													sql[0].Member = member;

												_sql.Add(member, sql);
											}

											return sql;
										}

										return ProcessMemberAccess(
											expression, (MemberExpression)levelExpression, level,
											(n,ctx,ex,l,mex) =>
											{
												switch (n)
												{
													case 0 :
														var buildExpression = GetExpression(expression, levelExpression, mex);
														return ConvertExpressions(buildExpression, flags);
													default:
														return ctx.ConvertToSql(ex, l, flags);
												}
											});
									}

								case ExpressionType.Parameter:
									if (levelExpression != expression)
										return GetSequence(expression, level).ConvertToSql(expression, level + 1, flags);

									if (level == 0)
										return GetSequence(expression, level).ConvertToSql(null, 0, flags);

									break;
							}

							break;
						}
				}
			}

			throw new NotImplementedException();
		}

		SqlInfo[] ConvertMember(MemberInfo member, Expression expression, ConvertFlags flags)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.MemberAccess :
				case ExpressionType.Parameter :
					if (IsExpression(expression, 0, RequestFor.Field).Result)
						flags = ConvertFlags.Field;

					var sql = ConvertToSql(expression, 0, flags)[0];

					return new[] { new SqlInfo { Sql = sql.Sql, Member = member, Query = sql.Query } };
			}

			var exprs =  ConvertExpressions(expression, flags);

			if (exprs.Length == 1)
				exprs[0].Member = member;

			return exprs;
		}

		SqlInfo[] ConvertExpressions(Expression expression, ConvertFlags flags)
		{
			return Builder.ConvertExpressions(this, expression, flags)
				.Select<SqlInfo,SqlInfo>(CheckExpression)
				.ToArray();
		}

		SqlInfo CheckExpression(SqlInfo expression)
		{
			if (expression.Sql is SqlQuery.SearchCondition)
			{
				expression.Sql = Builder.Convert(this, new SqlFunction(typeof(bool), "CASE", expression.Sql, new SqlValue(true), new SqlValue(false)));
			}

			return expression;
		}

		#endregion

		#region ConvertToIndex

		readonly Dictionary<Tuple<Expression,int,ConvertFlags>,SqlInfo[]> _expressionIndex = new Dictionary<Tuple<Expression,int,ConvertFlags>,SqlInfo[]>();

		public virtual SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			var key = Tuple.Create(expression, level, flags);

			SqlInfo[] info;

			if (!_expressionIndex.TryGetValue(key, out info))
			{
				info = ConvertToIndexInternal(expression, level, flags);

				var newInfo = new SqlInfo[info.Length];

				for (var i = 0; i < info.Length; i++)
				{
					var ni = info[i];

					if (ni.Query != SqlQuery)
					{
						ni = new SqlInfo
						{
							Query  = SqlQuery,
							Member = ni.Member,
							Index  = SqlQuery.Select.Add(ni.Query.Select.Columns[ni.Index])
						};
					}

					newInfo[i] = ni;
				}

				_expressionIndex.Add(key, newInfo);

				return newInfo;
			}

			return info;
		}

		readonly Dictionary<Tuple<MemberInfo,ConvertFlags>,SqlInfo[]> _memberIndex = new Dictionary<Tuple<MemberInfo,ConvertFlags>,SqlInfo[]>();

		SqlInfo[] ConvertToIndexInternal(Expression expression, int level, ConvertFlags flags)
		{
			if (IsScalar)
			{
				if (Body.NodeType == ExpressionType.Parameter)
					for (var i = 0; i < Sequence.Length; i++)
						if (Body == Lambda.Parameters[i])
							return Sequence[i].ConvertToIndex(expression, level, flags);

				if (expression == null)
				{
					var key = Tuple.Create((MemberInfo)null, flags);

					SqlInfo[] idx;

					if (!_memberIndex.TryGetValue(key, out idx))
					{
						idx = ConvertToSql(null, 0, flags);

						foreach (var info in idx)
							SetInfo(info);

						_memberIndex.Add(key, idx);
					}

					return idx;
				}

				switch (flags)
				{
					case ConvertFlags.Field :
					case ConvertFlags.All   :
						return ProcessScalar(
							expression,
							level,
							(ctx, ex, l) => ctx.ConvertToIndex(ex, l, flags),
							() => GetSequence(expression, level).ConvertToIndex(expression, level + 1, flags));
				}
			}
			else
			{
				if (expression == null)
				{
					switch (flags)
					{
						case ConvertFlags.Field : throw new NotImplementedException();
						case ConvertFlags.Key   :
						case ConvertFlags.All   :
							{
								var p = Expression.Parameter(Body.Type, "p");
								var q =
									from m in Members.Keys
									where !(m is MethodInfo)
									select new
									{
										Sql    = ConvertToIndex(Expression.MakeMemberAccess(p, m), 1, flags),
										Member = m
									} into mm
									from m in mm.Sql.Select(s => new SqlInfo
										{
											Sql    = s.Sql,
											Index  = s.Index,
											Member = mm.Member,
											Query  = s.Query
										})
									select m;

								return q.ToArray();
							}
					}
				}

				switch (flags)
				{
					case ConvertFlags.All   :
					case ConvertFlags.Key   :
					case ConvertFlags.Field :
						{
							if (level == 0)
							{
								var idx = Builder.ConvertExpressions(this, expression, flags);

								foreach (var info in idx)
									SetInfo(info);

								return idx;
							}

							var levelExpression = expression.GetLevelExpression(level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										if (levelExpression == expression)
										{
											var member = Tuple.Create(((MemberExpression)levelExpression).Member, flags);

											SqlInfo[] idx;

											if (!_memberIndex.TryGetValue(member, out idx))
											{
												idx = ConvertToSql(expression, level, flags);

												if (flags == ConvertFlags.Field && idx.Length != 1)
													throw new InvalidOperationException();

												foreach (var info in idx)
													SetInfo(info);

												_memberIndex.Add(member, idx);
											}

											return idx;
										}

										return ProcessMemberAccess(
											expression,
											(MemberExpression)levelExpression,
											level,
											(n, ctx, ex, l, _) => n == 0 ?
												GetSequence(expression, level).ConvertToIndex(expression, level + 1, flags) :
												ctx.ConvertToIndex(ex, l, flags));
									}

								case ExpressionType.Parameter:

									if (levelExpression != expression)
										return GetSequence(expression, level).ConvertToIndex(expression, level + 1, flags);
									break;
							}

							break;
						}
				}
			}

			throw new NotImplementedException();
		}

		void SetInfo(SqlInfo info)
		{
			info.Query = SqlQuery;

			if (info.Sql == SqlQuery)
				info.Index = SqlQuery.Select.Columns.Count - 1;
			else
				info.Index = SqlQuery.Select.Add(info.Sql);
		}

		#endregion

		#region IsExpression

		public virtual IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
		{
			switch (requestFlag)
			{
				case RequestFor.SubQuery : return IsExpressionResult.False;
				case RequestFor.Root     :
					return new IsExpressionResult(Sequence.Length == 1 ?
						expression == Lambda.Parameters[0] :
						Lambda.Parameters.Any(p => p == expression));
			}

			if (IsScalar)
			{
				if (expression == null)
					return IsExpression(Body, 0, requestFlag);

				switch (requestFlag)
				{
					default                     : return IsExpressionResult.False;
					case RequestFor.Table       :
					case RequestFor.Association :
					case RequestFor.Field       :
					case RequestFor.Expression  :
					case RequestFor.Object      :
					case RequestFor.GroupJoin   :
						return ProcessScalar(
							expression,
							level,
							(ctx, ex, l) => ctx.IsExpression(ex, l, requestFlag),
							() => new IsExpressionResult(requestFlag == RequestFor.Expression));
				}
			}
			else
			{
				switch (requestFlag)
				{
					default                     : return IsExpressionResult.False;
					case RequestFor.Table       :
					case RequestFor.Association :
					case RequestFor.Field       :
					case RequestFor.Expression  :
					case RequestFor.Object      :
					case RequestFor.GroupJoin   :
						{
							if (expression == null)
							{
								if (requestFlag == RequestFor.Expression)
									return new IsExpressionResult(Members.Values.Any(member => IsExpression(member, 0, requestFlag).Result));

								return new IsExpressionResult(requestFlag == RequestFor.Object);
							}

							var levelExpression = expression.GetLevelExpression(level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										var memberExpression = Members[((MemberExpression)levelExpression).Member];

										if (levelExpression == expression)
										{
											switch (memberExpression.NodeType)
											{
												case ExpressionType.New        :
												case ExpressionType.MemberInit :
													return new IsExpressionResult(requestFlag == RequestFor.Object);
											}
										}

										return ProcessMemberAccess(
											expression,
											(MemberExpression)levelExpression,
											level,
											(n,ctx,ex,l,_) => n == 0 ?
												new IsExpressionResult(requestFlag == RequestFor.Expression) : 
												ctx.IsExpression(ex, l, requestFlag));
									}

								case ExpressionType.Parameter    :
									{
										var sequence  = GetSequence(expression, level);
										var parameter = Lambda.Parameters[Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence)];

										if (levelExpression == expression)
										{
											if (levelExpression == parameter)
												return sequence.IsExpression(null, 0, requestFlag);
										}
										else if (level == 0)
											return sequence.IsExpression(expression, 1, requestFlag);

										break;
									}

								case ExpressionType.New          :
								case ExpressionType.MemberInit   : return new IsExpressionResult(requestFlag == RequestFor.Object);
								default                          : return new IsExpressionResult(requestFlag == RequestFor.Expression);
							}

							break;
						}
				}
			}

			throw new NotImplementedException();
		}

		#endregion

		#region GetContext

		public virtual IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
		{
			if (expression == null)
				return this;

			if (IsScalar)
			{
				return ProcessScalar(
					expression,
					level,
					(ctx, ex, l) => ctx.GetContext(ex, l, buildInfo),
					() => { throw new NotImplementedException(); });
			}
			else
			{
				var levelExpression = expression.GetLevelExpression(level);

				switch (levelExpression.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var context = ProcessMemberAccess(
								expression,
								(MemberExpression)levelExpression,
								level,
								(n,ctx,ex,l,_) => n == 0 ?
									null :
									ctx.GetContext(ex, l, buildInfo));

							if (context == null)
								throw new NotImplementedException();

							return context;
						}

					case ExpressionType.Parameter    :
						{
							var sequence  = GetSequence(expression, level);
							var parameter = Lambda.Parameters[Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence)];

							if (levelExpression == expression)
							{
								if (levelExpression == parameter)
									return sequence.GetContext(null, 0, buildInfo);
							}
							else if (level == 0)
								return sequence.GetContext(expression, 1, buildInfo);

							break;
						}
				}

				if (level == 0)
				{
					var sequence = GetSequence(expression, level);
					return sequence.GetContext(expression, level + 1, buildInfo);
				}
			}

			throw new NotImplementedException();
		}

		#endregion

		#region ConvertToParentIndex

		public virtual int ConvertToParentIndex(int index, IBuildContext context)
		{
			if (context.SqlQuery != SqlQuery)
				index = SqlQuery.Select.Add(context.SqlQuery.Select.Columns[index]);

			return Parent == null ? index : Parent.ConvertToParentIndex(index, this);
		}

		#endregion

		#region SetAlias

		public virtual void SetAlias(string alias)
		{
		}

		#endregion

		#region GetSubQuery

		public ISqlExpression GetSubQuery(IBuildContext context)
		{
			return null;
		}

		#endregion

		#region Helpers

		T ProcessScalar<T>(Expression expression, int level, Func<IBuildContext,Expression,int,T> action, Func<T> defaultAction)
		{
			if (level == 0)
			{
				if (Body.NodeType == ExpressionType.Parameter)
				{
					var sequence = GetSequence(Body, 0);

					return expression == Body ?
						action(sequence, null,       0) :
						action(sequence, expression, 1);
				}

				var levelExpression = expression.GetLevelExpression(level);

				if (levelExpression != expression)
					return action(GetSequence(expression, level), expression, level + 1);

				if (expression.NodeType == ExpressionType.Parameter)
				{
					var sequence  = GetSequence(expression, level);
					var parameter = Lambda.Parameters[Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence)];

					if (levelExpression == parameter)
						return action(sequence, null, 0);
				}

				switch (Body.NodeType)
				{
					case ExpressionType.MemberAccess : return action(GetSequence(expression, level), null, 0);
					default                          : return defaultAction();
				}
			}
			else
			{
				var root = Body.GetRootObject();

				if (root.NodeType == ExpressionType.Parameter)
				{
					var levelExpression = expression.GetLevelExpression(level - 1);
					var newExpression   = GetExpression(expression, levelExpression, Body);

					return action(this, newExpression, 0);
				}
			}

			throw new NotImplementedException();
		}

		T ProcessMemberAccess<T>(Expression expression, MemberExpression levelExpression, int level,
			Func<int,IBuildContext,Expression,int,Expression,T> action)
		{
			var memberExpression = Members[levelExpression.Member];
			var newExpression    = GetExpression(expression, levelExpression, memberExpression);
			var sequence         = GetSequence  (expression, level);

			if (sequence != null)
			{
				var parameter = Lambda.Parameters[Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence)];

				if (memberExpression == parameter && levelExpression == expression)
					return action(1, sequence, null, 0, memberExpression);
			}

			switch (memberExpression.NodeType)
			{
				case ExpressionType.MemberAccess :
				case ExpressionType.Parameter    :
					if (sequence != null)
						return action(2, sequence, newExpression, 1, memberExpression);
					throw new NotImplementedException();

				case ExpressionType.New          :
				case ExpressionType.MemberInit   :
					{
						var mmExpresion = GetMemberExpression(memberExpression, expression, level + 1);
						return action(3, this, mmExpresion, 0, memberExpression);
					}
			}

			return action(0, this, null, 0, memberExpression);
		}

		protected bool IsSubQuery()
		{
			for (var p = Parent; p != null; p = p.Parent)
				if (p.IsExpression(null, 0, RequestFor.SubQuery).Result)
					return true;
			return false;
		}

		IBuildContext GetSequence(Expression expression, int level)
		{
			if (Sequence.Length == 1)
				return Sequence[0];

			Expression root = null;

			if (IsScalar)
			{
				root = expression.GetRootObject();
			}
			else
			{
				var levelExpression = expression.GetLevelExpression(level);

				switch (levelExpression.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var memberExpression = Members[((MemberExpression)levelExpression).Member];

							root =  memberExpression.GetRootObject();

							if (root.NodeType != ExpressionType.Parameter)
								return null;

							for (var i = 0; i < Lambda.Parameters.Count; i++)
								if (root == Lambda.Parameters[i])
									return Sequence[i];

							break;
						}

					case ExpressionType.Parameter :
						{
							root = expression.GetRootObject();
							break;
						}
				}
			}

			if (root != null)
				for (var i = 0; i < Lambda.Parameters.Count; i++)
					if (root == Lambda.Parameters[i])
						return Sequence[i];

			throw new NotImplementedException();
		}

		static Expression GetExpression(Expression expression, Expression levelExpression, Expression memberExpression)
		{
			return levelExpression != expression ?
				expression.Transform(ex => ex == levelExpression ? memberExpression : ex) :
				memberExpression;
		}

		static Expression GetMemberExpression(Expression newExpression, Expression expression, int level)
		{
			var levelExpresion = expression.GetLevelExpression(level);

			switch (newExpression.NodeType)
			{
				case ExpressionType.New        :
				case ExpressionType.MemberInit : break;
				default                        :
					var le = expression.GetLevelExpression(level - 1);
					return GetExpression(expression, le, newExpression);
			}

			if (levelExpresion.NodeType != ExpressionType.MemberAccess)
				throw new LinqException("Invalid expression {0}", levelExpresion);

			var me = (MemberExpression)levelExpresion;

			switch (newExpression.NodeType)
			{
				case ExpressionType.New:
					{
						var expr = (NewExpression)newExpression;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
						if (expr.Members == null)
							throw new LinqException("Invalid expression {0}", expression);
// ReSharper restore HeuristicUnreachableCode
// ReSharper restore ConditionIsAlwaysTrueOrFalse

						for (var i = 0; i < expr.Members.Count; i++)
							if (me.Member == expr.Members[i])
								return levelExpresion == expression ?
									expr.Arguments[i].Unwrap() :
									GetMemberExpression(expr.Arguments[i].Unwrap(), expression, level + 1);

						throw new LinqException("Invalid expression {0}", expression);
					}

				case ExpressionType.MemberInit:
					{
						var expr = (MemberInitExpression)newExpression;

						foreach (var binding in expr.Bindings.Cast<MemberAssignment>())
						{
							if (me.Member == binding.Member)
								return levelExpresion == expression ?
									binding.Expression.Unwrap() :
									GetMemberExpression(binding.Expression.Unwrap(), expression, level + 1);
						}

						throw new LinqException("Invalid expression {0}", expression);
					}
			}

			return expression;
		}

		Expression GetMemberExpression(MemberInfo member, bool add, Type type)
		{
			Expression memberExpression;

			if (!Members.TryGetValue(member, out memberExpression))
			{
				if (add && member.DeclaringType.IsSameOrParentOf(Body.Type))
				{
					memberExpression = Expression.Constant(type.GetDefaultValue(), type);
					Members.Add(member, memberExpression);
				}
				else
					throw new InvalidOperationException();
			}

			return memberExpression;
		}

		#endregion
	}
}
