using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;
	using Common;
	using Mapping;

	// This class implements double functionality (scalar and member type selects)
	// and could be implemented as two different classes.
	// But the class means to have a lot of inheritors, and functionality of the inheritors
	// will be doubled as well. So lets double it once here.
	//

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SelectContext : IBuildContext
	{
		#region Init

#if DEBUG
		public string _sqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
		public string Path => this.GetPath();
		public MethodCallExpression? Debug_MethodCall;
#endif

		public IBuildContext[]   Sequence    { [DebuggerStepThrough] get; }
		public LambdaExpression  Lambda      { [DebuggerStepThrough] get; set; }
		public Expression        Body        { [DebuggerStepThrough] get; set; }
		public ExpressionBuilder Builder     { [DebuggerStepThrough] get; }
		public SelectQuery       SelectQuery { [DebuggerStepThrough] get; set; }
		public SqlStatement?     Statement   { [DebuggerStepThrough] get; set; }
		public IBuildContext?    Parent      { [DebuggerStepThrough] get; set; }
		public bool              IsScalar    { [DebuggerStepThrough] get; }

		public bool              AllowAddDefault { [DebuggerStepThrough] get; set; } = true;

		Expression IBuildContext.Expression => Lambda;

		public readonly Dictionary<MemberInfo,Expression> Members = new (new MemberInfoComparer());

		public SelectContext(IBuildContext? parent, ExpressionBuilder builder, LambdaExpression lambda, SelectQuery selectQuery)
		{
			Parent      = parent;
			Sequence    = Array<IBuildContext>.Empty;
			Builder     = builder;
			Lambda      = lambda;
			Body        = lambda.Body;
			SelectQuery = selectQuery;

			IsScalar = !Builder.ProcessProjection(Members, Body);

			Builder.Contexts.Add(this);
		}

		public SelectContext(IBuildContext? parent, LambdaExpression lambda, params IBuildContext[] sequences)
		{
			Parent      = parent;
			Sequence    = sequences;
			Builder     = sequences[0].Builder;
			Lambda      = lambda;
			Body        = lambda.Body;
			SelectQuery = sequences[0].SelectQuery;

			foreach (var context in Sequence)
				context.Parent = this;

			IsScalar = !Builder.ProcessProjection(Members, Body);

			Builder.Contexts.Add(this);
		}

		#endregion

		#region BuildQuery

		public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			var expr   = BuildExpression(null, 0, Sequence.Length == 0);
			var mapper = Builder.BuildMapper<T>(expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		#endregion

		#region BuildExpression

		ParameterExpression? _rootExpression;

		public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
#if DEBUG && TRACK_BUILD
		{
			Debug.WriteLine("{0}.BuildExpression start {1}:\n{2}".Args(GetType().Name, level, (expression ?? Body).GetDebugView()));
			Debug.WriteLine("{0}.BuildExpression start:\n{1}".Args(GetType().Name, SelectQuery));

			var expr = BuildExpressionInternal(expression, level, enforceServerSide);

			Debug.WriteLine("{0}.BuildExpression end:\n{1}".Args(GetType().Name, (expression ?? Body).GetDebugView()));
			Debug.WriteLine("{0}.BuildExpression end:\n{1}".Args(GetType().Name, SelectQuery));

			return expr;
		}

		Expression BuildExpressionInternal(Expression? expression, int level, bool enforceServerSide)
#endif
		{
			{
				var key = Tuple.Create(expression, level, ConvertFlags.Field);

				if (_expressionIndex.TryGetValue(key, out var info))
				{
					var idx  = Parent?.ConvertToParentIndex(info[0].Index, this) ?? info[0].Index;

					var expr = expression ?? Body;

					if (IsExpression(expr, level, RequestFor.Object).Result)
						return Builder.BuildExpression(this, expr, enforceServerSide);

					return Builder.BuildSql(expr.Type, idx, info[0].Sql);
				}
			}

			if (expression == null)
			{
				if (_rootExpression == null)
				{
					var expr = Builder.BuildExpression(this, Body, enforceServerSide);

					if (Builder.IsBlockDisable)
						return expr;

					_rootExpression = Builder.BuildVariable(expr);
				}

				return _rootExpression;
			}

			var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

			if (IsScalar)
			{
				if (Body.NodeType != ExpressionType.Parameter && level == 0)
					if (ReferenceEquals(levelExpression, expression))
						if (IsSubQuery() && IsExpression(null, 0, RequestFor.Expression).Result)
						{
							var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
							var idx = Parent?.ConvertToParentIndex(info.Index, this) ?? info.Index;

							return Builder.BuildSql(expression.Type, idx, info.Sql);
						}

				return ProcessScalar(
					(context: this, expression, level, enforceServerSide),
					expression,
					level,
					static (context, ctx, ex, l) => ctx!.BuildExpression(ex, l, context.enforceServerSide),
					static context => context.context.GetSequence(context.expression, context.level)!.BuildExpression(null, 0, context.enforceServerSide), true);
			}
			else
			{
				if (level == 0)
				{
					var sequence = GetSequence(expression, level)!;

					Builder.AssociationRoot = expression;

					return ReferenceEquals(levelExpression, expression) ?
						sequence.BuildExpression(null,       0,         enforceServerSide) :
						sequence.BuildExpression(expression, level + 1, enforceServerSide);
				}

				switch (levelExpression.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var memberInfo = ((MemberExpression)levelExpression).Member;

							var memberExpression = GetMemberExpression(
								memberInfo,
								ReferenceEquals(levelExpression, expression),
								levelExpression.Type,
								expression);

							if (ReferenceEquals(levelExpression, expression))
							{
								if (IsSubQuery())
								{
									switch (memberExpression.NodeType)
									{
										case ExpressionType.New        :
										case ExpressionType.MemberInit :
										{
											return Builder.BuildExpression(this, memberExpression, enforceServerSide);
										}
									}

									var me = memberExpression.NodeType == ExpressionType.Parameter ? null : memberExpression;

									if (!IsExpression(me, 0, RequestFor.Object).Result &&
										!IsExpression(me, 0, RequestFor.Field). Result)
									{
										var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
										var idx  = Parent?.ConvertToParentIndex(info.Index, this) ?? info.Index;

										return Builder.BuildSql(expression.Type, idx, info.Sql);
									}
								}

								return Builder.BuildExpression(this, memberExpression, enforceServerSide, memberInfo.Name);
							}

							{
								var sequence = GetSequence(expression, level);

								switch (memberExpression.NodeType)
								{
									case ExpressionType.Parameter  :
										{
											var parameter = Lambda.Parameters[Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence)];

											if (ReferenceEquals(memberExpression, parameter))
												return sequence!.BuildExpression(expression, level + 1, enforceServerSide);

											break;
										}

									case ExpressionType.New        :
									case ExpressionType.MemberInit :
										{
											var mmExpression = GetMemberExpression(memberExpression, expression, level + 1);
											return Builder.BuildExpression(this, mmExpression, enforceServerSide);
										}
									default:
										{
											if (memberExpression is ContextRefExpression refExpression)
											{
												return refExpression.BuildContext.BuildExpression(memberExpression, level + 1, enforceServerSide);
											}
											break;
										}
								}

								var expr = expression.Replace(levelExpression, memberExpression);

								if (sequence == null)
									return Builder.BuildExpression(this, expr, enforceServerSide);

								return sequence.BuildExpression(expr, 1, enforceServerSide);
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

		readonly Dictionary<(MemberInfo, ConvertFlags),SqlInfo[]> _sql = new();

		public virtual SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			if (expression != null && level > 0 && expression.NodeType == ExpressionType.Call)
			{
				var e = (MethodCallExpression)expression;

				if (e.Method.DeclaringType == typeof(Enumerable) && !typeof(IGrouping<,>).IsSameOrParentOf(e.Arguments[0].Type))
				{
					return new[] { new SqlInfo(Builder.SubQueryToSql(this, e)) };
				}
			}

			if (IsScalar)
			{
				if (expression == null)
					return Builder.ConvertExpressions(this, Body, flags, null);

				switch (flags)
				{
					case ConvertFlags.Field :
					case ConvertFlags.Key   :
					case ConvertFlags.All   :
						{
							if (Body.NodeType != ExpressionType.Parameter && level == 0)
							{
								var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

								if (levelExpression != expression)
									if (flags != ConvertFlags.Field && IsExpression(expression, level, RequestFor.Field).Result)
										flags = ConvertFlags.Field;
							}

							return ProcessScalar(
								(flags, context: this, expression),
								expression,
								level,
								static (context, ctx, ex, l) => ctx!.ConvertToSql(ex, l, context.flags),
								static context => new[] { new SqlInfo(context.context.Builder.ConvertToSql(context.context, context.expression)) }, true);
						}
				}
			}
			else
			{
				if (expression == null)
				{
					if (flags != ConvertFlags.Field)
					{
						var list = new List<SqlInfo>();
						foreach (var mi in Members)
							if (!(mi.Key is MethodInfo || flags == ConvertFlags.Key && EagerLoading.IsDetailsMember(this, mi.Value)))
								list.AddRange(ConvertMember(mi.Key, mi.Value, flags));

						return list.ToArray();
					}

					throw new NotImplementedException();
				}

				switch (flags)
				{
					case ConvertFlags.All   :
					case ConvertFlags.Key   :
					case ConvertFlags.Field :
						{
							var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);
							levelExpression = levelExpression.Unwrap();

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										if (level != 0 && levelExpression == expression)
										{
											var member = ((MemberExpression)levelExpression).Member;

											var cacheKey = (member, flags);

											if (!_sql.TryGetValue(cacheKey, out var sql))
											{
												var memberExpression = GetMemberExpression(
															member, levelExpression == expression, levelExpression.Type, expression);

												var ed = Builder.MappingSchema.GetEntityDescriptor(member.DeclaringType!);
												var descriptor = ed.FindColumnDescriptor(member);

												sql = ConvertExpressions(memberExpression, flags, descriptor).Clone(member);

												_sql.Add(cacheKey, sql);
											}

											return sql;
										}

										return ProcessMemberAccess(
											(context: this, flags, expression, levelExpression),
											expression, (MemberExpression)levelExpression, level,
											static (context, n, ctx,ex,l,mex) =>
											{
												switch (n)
												{
													case 0 :
														var buildExpression = GetExpression(context.expression, context.levelExpression, mex);
														ColumnDescriptor? descriptor = null;
														if (mex is MemberExpression ma)
														{
															var ed     = context.context.Builder.MappingSchema.GetEntityDescriptor(ma.Expression!.Type);
															descriptor = ed.FindColumnDescriptor(ma.Member);
														}
														return context.context.ConvertExpressions(buildExpression, context.flags, descriptor);
													default:
														return ctx.ConvertToSql(ex, l, context.flags);
												}
											});
									}

								case ExpressionType.Parameter:
									if (levelExpression != expression)
										return GetSequence(expression, level)!.ConvertToSql(expression, level + 1, flags);

									if (level == 0)
										return GetSequence(expression, level)!.ConvertToSql(null, 0, flags);

									break;

								case ExpressionType.Extension:
									{
										if (levelExpression is ContextRefExpression)
										{
											if (levelExpression != expression)
												return GetSequence(expression, level)!.ConvertToSql(expression,
													level + 1,
													flags);

											if (level == 0)
												return GetSequence(expression, level)!.ConvertToSql(null, 0, flags);
										}

										goto default;
									}

								default:
									if (level == 0)
										return Builder.ConvertExpressions(this, expression, flags, null);
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
			var ed         = Builder.MappingSchema.GetEntityDescriptor(member.DeclaringType!);
			var descriptor = ed.FindColumnDescriptor(member);

			return ConvertExpressions(expression, flags, descriptor).Clone(member);
		}

		SqlInfo[] ConvertExpressions(Expression expression, ConvertFlags flags, ColumnDescriptor? columnDescriptor)
		{
			return Builder.ConvertExpressions(this, expression, flags, columnDescriptor);
		}

		#endregion

		#region ConvertToIndex

		readonly Dictionary<Tuple<Expression?,int,ConvertFlags>,SqlInfo[]> _expressionIndex = new ();

		public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			var key = Tuple.Create(expression, level, flags);

			if (!_expressionIndex.TryGetValue(key, out var info))
			{
				info = ConvertToIndexInternal(expression, level, flags);

				var newInfo = new SqlInfo[info.Length];
				for (var i = 0; i < newInfo.Length; i++)
				{
					var si = info[i];
					if (si.Query == SelectQuery)
						newInfo[i] = si;
					else
					{
						var index = SelectQuery.Select.Add(
							si.Query != null
								? si.Query.Select.Columns[si.Index]
								: si.Sql);

						newInfo[i] = new SqlInfo(si.MemberChain, SelectQuery.Select.Columns[index], SelectQuery, index);
					}
				}

				_expressionIndex.Add(key, newInfo);

				return newInfo;
			}

			return info;
		}

		readonly Dictionary<Tuple<MemberInfo?,ConvertFlags>,SqlInfo[]> _memberIndex = new ();

		class SqlData
		{
			public SqlInfo[]  Sql    = null!;
			public MemberInfo Member = null!;
		}

		SqlInfo[] ConvertToIndexInternal(Expression? expression, int level, ConvertFlags flags)
		{
			if (IsScalar)
			{
				if (Body.NodeType == ExpressionType.Parameter)
					for (var i = 0; i < Sequence.Length; i++)
						if (Body == Lambda.Parameters[i])
							return Sequence[i].ConvertToIndex(expression, level, flags);

				if (expression == null || expression is ContextRefExpression refExpression && refExpression.BuildContext == this)
				{
					var key = Tuple.Create((MemberInfo?)null, flags);

					if (!_memberIndex.TryGetValue(key, out var idx))
					{
						idx = ConvertToSql(null, 0, flags);

						for (var i = 0; i < idx.Length; i++)
						{
							idx[i] = SetInfo(idx[i], null);
						}

						_memberIndex.Add(key, idx);
					}

					return idx;
				}

				switch (flags)
				{
					case ConvertFlags.Field :
					case ConvertFlags.Key   :
					case ConvertFlags.All   :
						return ProcessScalar(
							(context: this, flags, level, expression),
							expression,
							level,
							static (context, ctx, ex, l) => ctx!.ConvertToIndex(ex, l, context.flags),
							static context => context.context.GetSequence(context.expression, context.level)!.ConvertToIndex(context.expression, context.level + 1, context.flags), true);
				}
			}
			else
			{
				if (expression == null)
				{
					switch (flags)
					{
						case ConvertFlags.Field :
						case ConvertFlags.Key   :
						case ConvertFlags.All   :
							{
								var p = Expression.Parameter(Body.Type, "p");

								var list = new List<SqlInfo>();
								foreach (var m in Members)
								{
									if (!(m.Key is MethodInfo || flags == ConvertFlags.Key && EagerLoading.IsDetailsMember(this, m.Value)))
									{
										foreach (var si in ConvertToIndex(Expression.MakeMemberAccess(p, m.Key), 1, flags))
											list.Add(si.Clone(m.Key));
									}
								}

								return list.ToArray();
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
								var idx = Builder.ConvertExpressions(this, expression!, flags, null);

								for (var i = 0; i < idx.Length; i++)
								{
									idx[i] = SetInfo(idx[i], null);
								}

								return idx;
							}

							var levelExpression = expression!.GetLevelExpression(Builder.MappingSchema, level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										if (levelExpression == expression)
										{
											var member = Tuple.Create((MemberInfo?)((MemberExpression)levelExpression).Member, flags);

											if (!_memberIndex.TryGetValue(member, out var idx))
											{
												idx = ConvertToSql(expression, level, flags);

												if (flags == ConvertFlags.Field && idx.Length != 1)
													throw new InvalidOperationException();

												for (var i = 0; i < idx.Length; i++)
												{
													idx[i] = SetInfo(idx[i], member.Item1);
												}

												_memberIndex.Add(member, idx);
											}

											return idx;
										}

										return ProcessMemberAccess(
											(context: this, expression, level, flags),
											expression!,
											(MemberExpression)levelExpression,
											level,
											static (context, n, ctx, ex, l, _) => n == 0 ?
												context.context.GetSequence(context.expression!, context.level)!.ConvertToIndex(context.expression, context.level + 1, context.flags) :
												ctx.ConvertToIndex(ex, l, context.flags));
									}

								case ExpressionType.Parameter:
								case ExpressionType.Extension:

									if (levelExpression != expression)
										return GetSequence(expression!, level)!.ConvertToIndex(expression, level + 1, flags);
									break;
							}

							break;
						}
				}
			}

			throw new NotImplementedException();
		}

		SqlInfo SetInfo(SqlInfo info, MemberInfo? member)
		{
			info = info.WithQuery(SelectQuery);

			if (info.Sql == SelectQuery)
				info = info.WithIndex(SelectQuery.Select.Columns.Count - 1);
			else
			{
				info = info.WithIndex(SelectQuery.Select.Add(info.Sql));
				var column = SelectQuery.Select.Columns[info.Index];
				if (member != null && column.RawAlias == null)
					column.Alias = member.Name;
			}

			return info;
		}

		#endregion

		#region IsExpression

		Expression? _lastAssociationExpression;
		int        _lastAssociationLevel = -1;

		public virtual IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			switch (requestFlag)
			{
				case RequestFor.Association :
					if (expression == _lastAssociationExpression && level == _lastAssociationLevel)
						return IsExpressionResult.False;

					_lastAssociationExpression = expression;
					_lastAssociationLevel      = level;

					break;
			}

			var res = IsExpressionInternal(expression, level, requestFlag);

			switch (requestFlag)
			{
				case RequestFor.Association :
					_lastAssociationExpression = null;
					_lastAssociationLevel      = -1;
					break;
			}

			return res;
		}

		public IsExpressionResult IsExpressionInternal(Expression? expression, int level, RequestFor requestFlag)
		{
			switch (requestFlag)
			{
				case RequestFor.SubQuery : return IsExpressionResult.False;
				case RequestFor.Root     :
					bool result;
					if (Sequence.Length == 1)
						result = ReferenceEquals(expression, Lambda.Parameters[0]);
					else
					{
						result = false;
						foreach (var p in Lambda.Parameters)
						{
							if (ReferenceEquals(p, expression))
							{
								result = true;
								break;
							}
						}
					}
					return IsExpressionResult.GetResult(result);
			}

			if (IsScalar)
			{
				if (expression == null)
					return IsExpression(Body, 0, requestFlag);

				switch (requestFlag)
				{
					case RequestFor.Table       :
					case RequestFor.Association :
					case RequestFor.Field       :
					case RequestFor.Expression  :
					case RequestFor.Object      :
					case RequestFor.GroupJoin   :
						return ProcessScalar(
							requestFlag,
							expression,
							level,
							static (requestFlag, ctx, ex, l) => ctx == null ? IsExpressionResult.False : ctx.IsExpression(ex, l, requestFlag),
							static requestFlag => IsExpressionResult.GetResult(requestFlag == RequestFor.Expression), false);
					default                     : return IsExpressionResult.False;
				}
			}
			else
			{
				switch (requestFlag)
				{
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
								{
									var result = false;
									foreach (var member in Members.Values)
									{
										if (IsExpression(member, 0, requestFlag).Result)
										{
											result = true;
											break;
										}
									}
									return IsExpressionResult.GetResult(result);
								}

								return IsExpressionResult.GetResult(requestFlag == RequestFor.Object);
							}

							var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										var member = ((MemberExpression)levelExpression).Member;

										var memberExpression = GetProjectedExpression(member, false);
										if (memberExpression == null)
										{
											MemberInfo? nm = null;
											foreach (var m in Members.Keys)
											{
												if (m.Name == member.Name)
												{
													nm = m;
													break;
												}
											}

											if (nm != null && member.DeclaringType!.IsInterface)
											{
												if (member.DeclaringType.IsSameOrParentOf(nm.DeclaringType!))
													memberExpression = GetProjectedExpression(nm, false);
												else
												{
													var mdt = member.DeclaringType.GetDefiningTypes(member);
													var ndt = Body.Type.           GetDefiningTypes(nm);

													if (mdt.Intersect(ndt).Any())
														memberExpression = GetProjectedExpression(nm, false);
												}
											}

											if (memberExpression == null)
												return IsExpressionResult.GetResult(requestFlag == RequestFor.Expression);
											//throw new InvalidOperationException(
											//	string.Format("Invalid member '{0}.{1}'", member.DeclaringType, member.Name));
										}

										if (ReferenceEquals(levelExpression, expression))
										{
											switch (memberExpression.NodeType)
											{
												case ExpressionType.New        :
												case ExpressionType.MemberInit :
													return IsExpressionResult.GetResult(requestFlag == RequestFor.Object);
											}
										}

										return ProcessMemberAccess(
											requestFlag,
											expression,
											(MemberExpression)levelExpression,
											level,
											static (requestFlag, n, ctx,ex,l,ex1) => n == 0 ?
												new IsExpressionResult(requestFlag == RequestFor.Expression, ex1) :
												ctx.IsExpression(ex, l, requestFlag));
									}

								case ExpressionType.Parameter    :
									{
										var sequence  = GetSequence(expression, level);

										if (sequence == null)
										{
											var buildInfo = new BuildInfo(Parent, expression, new SelectQuery());
											if (!Builder.IsSequence(buildInfo))
												break;

											sequence = Builder.BuildSequence(buildInfo);
											return sequence.IsExpression(levelExpression, level, requestFlag);
										}

										var idx = Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence);
										if (idx < 0)
										{
											return IsExpressionResult.False;
										}

										var parameter = Lambda.Parameters[idx];

										if (ReferenceEquals(levelExpression, expression))
										{
											if (ReferenceEquals(levelExpression, parameter))
												return sequence.IsExpression(null, 0, requestFlag);
										}
										else if (level == 0)
											return sequence.IsExpression(expression, 1, requestFlag);

										break;
									}

								case ExpressionType.New          :
								case ExpressionType.MemberInit   : return IsExpressionResult.GetResult(requestFlag == RequestFor.Object);
								default:
									{
										if (levelExpression is ContextRefExpression refExpression)
										{
											if (levelExpression == expression)
												return refExpression.BuildContext.IsExpression(null, 0, requestFlag);
											return refExpression.BuildContext.IsExpression(expression, level + 1, requestFlag);
										}
										return IsExpressionResult.GetResult(requestFlag == RequestFor.Expression);
									}
							}

							break;
						}
					default: return IsExpressionResult.False;
				}
			}


			return IsExpressionResult.False;
		}

		#endregion

		#region GetContext

		public virtual IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			if (expression == null)
				return this;

			if (IsScalar)
			{
				return ProcessScalar(
					buildInfo,
					expression,
					level,
					static (buildInfo, ctx, ex, l) => ctx!.GetContext(ex, l, buildInfo),
					static _ => throw new NotImplementedException(), true);
			}
			else
			{
				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

				switch (levelExpression.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							if (levelExpression == expression && Sequence.Length == 1 && !(Sequence[0] is GroupByBuilder.GroupByContext))
							{
								var memberExpression = GetMemberExpression(
									((MemberExpression)levelExpression).Member,
									levelExpression == expression,
									levelExpression.Type,
									expression);

								return GetContext(memberExpression, 0, new BuildInfo(this, memberExpression, buildInfo.SelectQuery));
							}

							var context = ProcessMemberAccess(
								buildInfo,
								expression,
								(MemberExpression)levelExpression,
								level,
								static (buildInfo, n,ctx,ex,l,_) => n == 0 ?
									null :
									ctx.GetContext(ex, l, buildInfo));

							if (context == null)
								throw new NotImplementedException();

							return context;
						}

					case ExpressionType.Parameter    :
						{
							var sequence  = GetSequence(expression, level)!;
							var paramIndex = Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence);
							var parameter  = paramIndex >= 0 ? Lambda.Parameters[paramIndex] : null;

							if (ReferenceEquals(levelExpression, expression))
							{
								if (ReferenceEquals(levelExpression, parameter))
									return sequence.GetContext(null, 0, buildInfo);
							}
							else if (level == 0)
								return sequence.GetContext(expression, 1, buildInfo);

							break;
						}
					default:
						{
							if (levelExpression is ContextRefExpression refExpression)
							{
								var sequence = GetSequence(expression, level);

								if (sequence != null)
								{
									if (ReferenceEquals(levelExpression, expression))
									{
										return sequence.GetContext(null, 0, buildInfo);
									}

									if (level == 0)
										return sequence.GetContext(expression, 1, buildInfo);
								}
							}

							break;
						}
				}

				if (level == 0)
				{
					var sequence = GetSequence(expression, level);
					if (sequence != null)
						return sequence.GetContext(expression, level + 1, buildInfo);
					if (Builder.IsSequence(buildInfo))
					{
						sequence = Builder.BuildSequence(buildInfo);
						return sequence;
					}
				}
			}

			throw new NotImplementedException();
		}

		#endregion

		#region ConvertToParentIndex

		public virtual int ConvertToParentIndex(int index, IBuildContext context)
		{
			if (!ReferenceEquals(context.SelectQuery, SelectQuery))
				index = SelectQuery.Select.Add(context.SelectQuery.Select.Columns[index]);

			return Parent?.ConvertToParentIndex(index, this) ?? index;
		}

		#endregion

		#region SetAlias

		public virtual void SetAlias(string? alias)
		{
			if (!alias.IsNullOrEmpty() && !alias.Contains('<') && SelectQuery.Select.From.Tables.Count == 1)
			{
				SelectQuery.Select.From.Tables[0].Alias = alias;
			}
		}

		#endregion

		#region GetSubQuery

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			return null;
		}

		#endregion

		public virtual SqlStatement GetResultStatement()
		{
			return Statement ??= new SqlSelectStatement(SelectQuery);
		}

		public virtual void CompleteColumns()
		{
			ExpressionBuilder.EnsureAggregateColumns(this, SelectQuery);

			foreach (var sequence in Sequence)
			{
				sequence.CompleteColumns();
			}
		}

		#region Helpers

		T ProcessScalar<T, TContext>(TContext context, Expression expression, int level, Func<TContext, IBuildContext?,Expression?,int,T> action, Func<TContext, T> defaultAction, bool throwOnError)
		{
			if (level == 0)
			{
				if (Body.NodeType == ExpressionType.Parameter && Lambda.Parameters.Count == 1)
				{
					var sequence = GetSequence(Body, 0)!;

					return ReferenceEquals(expression, Body) ?
						action(context, sequence, null,       0) :
						action(context, sequence, expression, 1);
				}

				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

				if (!ReferenceEquals(levelExpression, expression))
				{
					var ctx = GetSequence(expression, level);
					return ctx == null ? defaultAction(context) : action(context, ctx, expression, Sequence.Contains(ctx) ? level + 1 : 0);
				}

				if (expression.NodeType == ExpressionType.Parameter)
				{
					var sequence  = GetSequence(expression, level)!;
					var idx       = Array.IndexOf(Sequence, sequence);
					var parameter = Sequence.Length == 0 ? Lambda.Parameters[0] :
						idx < 0 ? null : Lambda.Parameters[idx];

					if (parameter != null && ReferenceEquals(levelExpression, parameter))
						return action(context, sequence, null, 0);
				}

				return Body.NodeType switch
				{
					ExpressionType.MemberAccess => action(context, GetSequence(expression, level)!, null, 0),
					_                           => defaultAction(context),
				};
			}
			else
			{
				var root = Builder.GetRootObject(Body);

				if (root.NodeType == ExpressionType.Parameter)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level - 1);
					var newExpression   = GetExpression(expression, levelExpression, Body);

					Builder.UpdateConvertedExpression(expression, newExpression);

					var result = action(context, this, newExpression, 0);

					Builder.RemoveConvertedExpression(newExpression);

					return result;
				}
				else if (root is ContextRefExpression refExpression)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level - 1);
					var newExpression   = GetExpression(expression, levelExpression, Body);

					Builder.UpdateConvertedExpression(expression, newExpression);

					var result = action(context, refExpression.BuildContext, newExpression, 0);

					Builder.RemoveConvertedExpression(newExpression);

					return result;
				}
			}

			if (throwOnError)
				throw new NotImplementedException();

			return default!;
		}

		T ProcessMemberAccess<T, TContext>(TContext context, Expression expression, MemberExpression levelExpression, int level,
			Func<TContext, int, IBuildContext,Expression?,int,Expression,T> action)
		{
			var memberExpression = GetProjectedExpression(levelExpression.Member, true)!;
			memberExpression = memberExpression.Unwrap();

			var newExpression    = GetExpression(expression, levelExpression, memberExpression);
			var sequence         = GetSequence  (expression, level);
			var nextLevel        = 1;

			if (sequence != null)
			{
				var idx = Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence);

				if (idx >= 0)
				{
					var parameter = Lambda.Parameters[idx];

					if (ReferenceEquals(memberExpression, parameter) && ReferenceEquals(levelExpression, expression))
						return action(context, 1, sequence, null, 0, memberExpression);
				}
				else
				{
					nextLevel = 0;
				}
			}

			switch (memberExpression.NodeType)
			{
				case ExpressionType.MemberAccess :
				case ExpressionType.Parameter    :
					if (sequence != null)
						return action(context, 2, sequence, newExpression, nextLevel, memberExpression);
					break;

				case ExpressionType.New          :
				case ExpressionType.MemberInit   :
					{
						var mmExpresion = GetMemberExpression(memberExpression, expression, level + 1);
						return action(context, 3, this, mmExpresion, 0, memberExpression);
					}
			}

			return action(context, 0, this, null, 0, memberExpression);
		}

		protected bool IsSubQuery()
		{
			for (IBuildContext? p = Parent; p != null; p = p.Parent)
				if (p.IsExpression(null, 0, RequestFor.SubQuery).Result)
					return true;
			return false;
		}

		Expression? GetProjectedExpression(MemberInfo memberInfo, bool throwOnError)
		{
			if (!Members.TryGetValue(memberInfo, out var memberExpression))
			{
				var member = Body?.Type.GetMemberEx(memberInfo);
				if (member != null)
					Members.TryGetValue(member, out memberExpression);

				if (memberExpression == null)
				{
					if (typeof(ExpressionBuilder.GroupSubQuery<,>).IsSameOrParentOf(Body!.Type))
					{
						var newMember = Body.Type.GetField("Element")!;
						if (Members.TryGetValue(newMember, out memberExpression))
						{
							if (memberInfo.DeclaringType!.IsSameOrParentOf(memberExpression.Type))
								memberExpression = Expression.MakeMemberAccess(memberExpression, memberInfo);
						}
					}
				}
			}

			if (throwOnError && memberExpression == null)
				throw new LinqToDBException($"Member '{memberInfo.Name}' not found in type '{Body?.Type.Name ?? "<Unknown>"}'.");
			return memberExpression;
		}

		IBuildContext? GetSequence(Expression expression, int level)
		{
			if (Sequence.Length == 1 && Sequence[0].Parent == null)
				return Sequence[0];

			Expression? root = null;

			if (IsScalar)
			{
				root = Builder.GetRootObject(expression);
			}
			else
			{
				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);
				levelExpression = levelExpression.Unwrap();

				switch (levelExpression.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var memberExpression = GetProjectedExpression(((MemberExpression)levelExpression).Member, true)!;

							root = Builder.GetRootObject(memberExpression);

							if (root is ContextRefExpression refExpression)
							{
								return refExpression.BuildContext;
							}

							if (root.NodeType != ExpressionType.Parameter)
								return null;

							break;
						}

					case ExpressionType.Parameter :
						{
							root = Builder.GetRootObject(expression).Unwrap();
							break;
						}
					case ExpressionType.Extension:
						{
							root = Builder.GetRootObject(expression).Unwrap();
							break;
						}
				}
			}

			if (root != null)
				for (var i = 0; i < Lambda.Parameters.Count; i++)
					if (ReferenceEquals(root, Lambda.Parameters[i]))
						return Sequence[i];

			foreach (var context in Sequence)
			{
				if (context.Parent != null)
				{
					var ctx = Builder.GetContext(context, root);
					if (ctx != null)
						return ctx;
				}
			}

			return null;
		}

		static Expression GetExpression(Expression expression, Expression levelExpression, Expression memberExpression)
		{
			if (memberExpression is MemberExpression me)
			{
				//TODO: Why do we need such quirks with grouping?
				if (typeof(IGrouping<,>).IsSameOrParentOf(me.Member.DeclaringType!) && memberExpression.Type == expression.Type)
					return memberExpression;
			}

			if (!memberExpression.Type.IsAssignableFrom(levelExpression.Type) && !levelExpression.Type.IsAssignableFrom(memberExpression.Type))
				return memberExpression;

			return !ReferenceEquals(levelExpression, expression) ?
				expression.Replace(levelExpression, memberExpression) :
				memberExpression;
		}

		Expression GetMemberExpression(Expression newExpression, Expression expression, int level)
		{
			var levelExpresion = expression.GetLevelExpression(Builder.MappingSchema, level);

			switch (newExpression.NodeType)
			{
				case ExpressionType.New        :
				case ExpressionType.MemberInit : break;
				default                        :
					var le = expression.GetLevelExpression(Builder.MappingSchema, level - 1);
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
								return ReferenceEquals(levelExpresion, expression) ?
									expr.Arguments[i].Unwrap() :
									GetMemberExpression(expr.Arguments[i].Unwrap(), expression, level + 1);

						throw new LinqException("Invalid expression {0}", expression);
					}

				case ExpressionType.MemberInit:
					{
						var expr = (MemberInitExpression)newExpression;

						foreach (var binding in expr.Bindings.Cast<MemberAssignment>())
						{
							if (me.Member.EqualsTo(binding.Member))
								return ReferenceEquals(levelExpresion, expression) ?
									binding.Expression.Unwrap() :
									GetMemberExpression(binding.Expression.Unwrap(), expression, level + 1);
						}

						throw new LinqException("Invalid expression {0}", expression);
					}
			}

			return expression;
		}

		protected Expression GetMemberExpression(MemberInfo member, bool add, Type type, Expression sourceExpression)
		{
			var memberExpression = GetProjectedExpression(member, false);
			if (memberExpression == null)
			{
				foreach (var m in Members)
				{
					if (m.Key.Name == member.Name)
					{
						if (m.Key.EqualsTo(member, IsScalar ? null : Body.Type))
							return m.Value;
					}
				}

				if (member.DeclaringType!.IsSameOrParentOf(Body.Type))
				{
					if (Body.NodeType == ExpressionType.MemberInit)
					{
						var ed = Builder.MappingSchema.GetEntityDescriptor(Body.Type);

						if (ed.Aliases != null)
						{
							if (ed.Aliases.TryGetValue(member.Name, out var value))
								return GetMemberExpression(ed.TypeAccessor[value!].MemberInfo, add, type, sourceExpression);

							foreach (var a in ed.Aliases)
							{
								if (a.Value == member.Name)
								{
									foreach (var m in Members)
										if (m.Key.Name == a.Key)
											return m.Value;

									break;
								}
							}
						}
					}

					if (add && AllowAddDefault)
					{
						memberExpression = Expression.Constant(type.GetDefaultValue(), type);
						Members.Add(member, memberExpression);

						return memberExpression;
					}
				}

				throw new LinqToDBException($"'{sourceExpression}' cannot be converted to SQL.");
			}

			return memberExpression;
		}

		#endregion
	}
}
