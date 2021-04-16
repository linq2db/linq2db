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

		public readonly Dictionary<MemberInfo,Expression> Members = new Dictionary<MemberInfo,Expression>(new MemberInfoComparer());

		public SelectContext(IBuildContext? parent, ExpressionBuilder builder, LambdaExpression lambda, SelectQuery selectQuery)
		{
			Parent        = parent;
			Sequence      = Array<IBuildContext>.Empty;
			Builder       = builder;
			Lambda        = lambda;
			Body          = lambda.Body;
			SelectQuery   = selectQuery;

			IsScalar = !Builder.ProcessProjection(Members, Body);

			Builder.Contexts.Add(this);
		}

		public SelectContext(IBuildContext? parent, LambdaExpression lambda, params IBuildContext[] sequences)
		{
			Parent   = parent;
			Sequence = sequences;
			Builder  = sequences[0].Builder;
			Lambda   = lambda;
			Body     = SequenceHelper.PrepareBody(lambda, sequences);

			SelectQuery   = sequences[0].SelectQuery;

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
				var key = new ConvertKey(expression, level, ConvertFlags.Field);

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
					var projection = MakeProjection(Body, out _);

					var expr = Builder.BuildExpression(this, projection!, enforceServerSide);

					/*
					var expr = sequence != null
						? sequence.BuildExpression(projection, 0, enforceServerSide)
						: Builder.BuildExpression(this, projection!, enforceServerSide);
						*/

					// var expr       = Builder.BuildExpression(this, , enforceServerSide);

					if (Builder.IsBlockDisable)
						return expr;

					_rootExpression = Builder.BuildVariable(expr);
				}

				return _rootExpression;
			}

			var levelExpression = expression;

			if (IsScalar)
			{
				var projection = MakeProjection(expression, out _);

				//var expr = sequence.BuildExpression(projection, 0, enforceServerSide);
				var expr       = Builder.BuildExpression(this, projection, enforceServerSide);

				if (Builder.IsBlockDisable)
					return expr;

				_rootExpression = Builder.BuildVariable(expr);

				return _rootExpression;


				/*
				if (Body.NodeType != ExpressionType.Parameter && level == 0)
					if (ReferenceEquals(levelExpression, expression))
						if (IsSubQuery() && IsExpression(null, 0, RequestFor.Expression).Result)
						{
							var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
							var idx = Parent?.ConvertToParentIndex(info.Index, this) ?? info.Index;

							return Builder.BuildSql(expression.Type, idx, info.Sql);
						}

				return ProcessScalar(
					expression,
					level,
					(ctx, ex, l) => ctx!.BuildExpression(ex, l, enforceServerSide),
					() => GetSequence(expression, level)!.BuildExpression(null, 0, enforceServerSide), true);
			*/
			}
			else
			{
				var projection = MakeProjection(expression, out _);

				Builder.AssociationRoot = expression;

				//var expr = sequence.BuildExpression(projection, 0, enforceServerSide);
				var resultExpr = Builder.BuildExpression(this, projection, enforceServerSide);

				return resultExpr;

				throw new NotImplementedException();
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
												var resultExpression = memberExpression.Transform(e =>
												{
													if (!ReferenceEquals(e, memberExpression))
													{
														switch (e.NodeType)
														{
															case ExpressionType.MemberAccess :
															case ExpressionType.Parameter :
																{
																	var sequence = GetSequence(e, 0)!;
																	return Builder.BuildExpression(sequence, e, enforceServerSide);
																}
															default:
																{
																	if (e is ContextRefExpression refExpression)
																	{
																		return Builder.BuildExpression(refExpression.BuildContext, e, enforceServerSide);
																	}

																	break;
																}
														}

														if (enforceServerSide)
															return Builder.BuildExpression(this, e, true);
													}

													return e;
												});

												return resultExpression;
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

								var expr = expression.Transform(ex => ReferenceEquals(ex, levelExpression) ? memberExpression : ex);

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

		readonly Dictionary<MemberInfo,SqlInfo[]> _sql = new(new MemberInfoComparer());

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

			/*if (IsScalar)
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
								expression,
								level,
								(ctx, ex, l) => ctx!.ConvertToSql(ex, l, flags),
								() => new[] { new SqlInfo(Builder.ConvertToSql(this, expression)) }, true);
						}
				}
			}
			else*/
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
							var projection = MakeProjection(expression, out var member);

							if (projection != expression)
							{
								var ctx = Builder.GetContext(this, projection);
								if (ctx != null)
									return ctx.ConvertToSql(projection, 0, flags);
							}

							var result = new[] {new SqlInfo(member, Builder.ConvertToSql(this, projection ?? Body))};
							/*
							var result = projection == null
								? Builder.ConvertExpressions(this, Body, flags, null)
								: ConvertExpressions(projection, flags, null);
								*/

							return result;

							if (Body.NodeType != ExpressionType.Parameter && level == 0)
							{
								var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

								if (levelExpression != expression)
									if (flags != ConvertFlags.Field && IsExpression(expression, level, RequestFor.Field).Result)
										flags = ConvertFlags.Field;
							}

							return ProcessScalar(
								expression,
								level,
								(ctx, ex, l) => ctx!.ConvertToSql(ex, l, flags),
								() => new[] { new SqlInfo(Builder.ConvertToSql(this, expression)) }, true);
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
							where !(m.Key is MethodInfo || flags == ConvertFlags.Key && EagerLoading.IsDetailsMember(this, m.Value))
							select ConvertMember(m.Key, m.Value, flags) into mm
							from m in mm
							select m;

						return q.ToArray();
					}
					else
					{
						return Builder.ConvertExpressions(this, Body, flags, null);
					}

					throw new NotImplementedException();
				}

				switch (flags)
				{
					case ConvertFlags.All   :
					case ConvertFlags.Key   :
					case ConvertFlags.Field :
						{
							var projection = MakeProjection(expression, out _);

							var result = projection == null
								? Builder.ConvertExpressions(this, Body, flags, null)
								: ConvertExpressions(projection, flags, null);

							return result;

							var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);
							levelExpression = levelExpression.Unwrap();

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										if (level != 0 && levelExpression == expression)
										{
											var member = ((MemberExpression)levelExpression).Member;

											if (!_sql.TryGetValue(member, out var sql))
											{
												var memberExpression = GetMemberExpression(
													member, levelExpression == expression, levelExpression.Type, expression);

												var ed = Builder.MappingSchema.GetEntityDescriptor(member.DeclaringType!);
												var descriptor = ed.FindColumnDescriptor(member);

												sql = ConvertExpressions(memberExpression, flags, descriptor)
													.Select(si => si.Clone(member)).ToArray();

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
														ColumnDescriptor? descriptor = null;
														if (mex is MemberExpression ma)
														{
															var ed = Builder.MappingSchema.GetEntityDescriptor(ma.Expression.Type);
															descriptor = ed.FindColumnDescriptor(ma.Member);
														}
														return ConvertExpressions(buildExpression, flags, descriptor);
													default:
														return ctx.ConvertToSql(ex, l, flags);
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

			return ConvertExpressions(expression, flags, descriptor)
				.Select(si => si.Clone(member))
				.ToArray();
		}

		SqlInfo[] ConvertExpressions(Expression expression, ConvertFlags flags, ColumnDescriptor? columnDescriptor)
		{
			return Builder.ConvertExpressions(this, expression, flags, columnDescriptor)
				.ToArray();
		}

		#endregion

		#region ConvertToIndex

		readonly Dictionary<ConvertKey,SqlInfo[]> _expressionIndex = new(ConvertKey.ExpressionFlagsComparer);

		struct ConvertKey
		{
			public ConvertKey(Expression? expression, int level, ConvertFlags flags)
			{
				Expression = expression;
				Level      = level;
				Flags      = flags;
			}

			public Expression?  Expression { get; }
			public int          Level      { get; }
			public ConvertFlags Flags      { get; }

			private sealed class ExpressionFlagsEqualityComparer : IEqualityComparer<ConvertKey>
			{
				private readonly IEqualityComparer<Expression> _expressionEqualityComparer;

				public ExpressionFlagsEqualityComparer(IEqualityComparer<Expression> expressionEqualityComparer)
				{
					_expressionEqualityComparer = expressionEqualityComparer;
				}

				public bool Equals(ConvertKey x, ConvertKey y)
				{
					return x.Flags == y.Flags && x.Level == y.Level && _expressionEqualityComparer.Equals(x.Expression, y.Expression);
				}

				public int GetHashCode(ConvertKey obj)
				{
					unchecked
					{
						var hashCode = _expressionEqualityComparer.GetHashCode(obj.Expression);
						hashCode = (hashCode * 397) ^ obj.Level;
						hashCode = (hashCode * 397) ^ (int)obj.Flags;
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<ConvertKey> ExpressionFlagsComparer { get; } = new ExpressionFlagsEqualityComparer(ExpressionEqualityComparer.Instance);
		}

		public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			var key = new ConvertKey(expression, level, flags);

			//if (!_expressionIndex.TryGetValue(key, out var info))
				var info = ConvertToIndexInternal(expression, level, flags);

				var newInfo = info
					.Select(i =>
					{
						if (i.Query == SelectQuery)
							return i;

						var index = SelectQuery.Select.Add(i.Query!.Select.Columns[i.Index]);

						return new SqlInfo(i.MemberChain, SelectQuery.Select.Columns[index], SelectQuery, index);
					})
					.ToArray();

				//_expressionIndex.Add(key, newInfo);

				return newInfo;

			return info;
		}

		readonly Dictionary<Tuple<MemberInfo?,ConvertFlags>,SqlInfo[]> _memberIndex = new();

		class SqlData
		{
			public SqlInfo[]  Sql    = null!;
			public MemberInfo Member = null!;
		}

		SqlInfo[] ConvertToIndexInternal(Expression? expression, int level, ConvertFlags flags)
		{
			if (IsScalar)
			{
				/*
				throw new NotImplementedException();
				if (Body.NodeType == ExpressionType.Parameter)
					for (var i = 0; i < Sequence.Length; i++)
						if (Body == Lambda.Parameters[i])
							return Sequence[i].ConvertToIndex(expression, level, flags);

				if (expression == null)
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
				*/

				if (expression == null || expression is ContextRefExpression refExpression && refExpression.BuildContext == this)
				{

					var projection = MakeProjection(Body, out var member);
					var idx        = Builder.ConvertExpressions(this, projection, flags, null);

					for (var i = 0; i < idx.Length; i++)
					{
						idx[i] = SetInfo(idx[i], member);
					}

					return idx;
				}

				switch (flags)
				{
					case ConvertFlags.Field:
					case ConvertFlags.Key:
					case ConvertFlags.All:
						{
							var projection = MakeProjection(expression!, out var member);

							var key = new ConvertKey(projection, level, flags);
							if (_expressionIndex.TryGetValue(key, out var info))
								return info;

							var idx        = Builder.ConvertExpressions(this, projection, flags, null);

							for (var i = 0; i < idx.Length; i++)
							{
								idx[i] = SetInfo(idx[i], member);
							}

							_expressionIndex.Add(key, idx);

							return idx;
						}
						/*return ProcessScalar(
							expression,
							level,
							(ctx, ex, l) => ctx!.ConvertToIndex(ex, l, flags),
							() => GetSequence(expression, level)!.ConvertToIndex(expression, level + 1, flags), true);*/
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
								var q =
									from m in Members
									where !(m.Key is MethodInfo || flags == ConvertFlags.Key && EagerLoading.IsDetailsMember(this, m.Value))
									select new SqlData
									{
										Sql    = ConvertToIndex(Expression.MakeMemberAccess(p, m.Key), 1, flags),
										Member = m.Key
									} into mm
									from m in mm.Sql.Select(s => s.Clone(mm.Member))
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
							var projection = MakeProjection(expression, out var member);

							var key = new ConvertKey(projection, level, flags);
							if (_expressionIndex.TryGetValue(key, out var info))
								return info;

							var idx        = Builder.ConvertExpressions(this, projection, flags, null);

							for (var i = 0; i < idx.Length; i++)
							{
								idx[i] = SetInfo(idx[i], member);
							}

							_expressionIndex.Add(key, idx);

							return idx;

							/*if (level == 0)
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
											expression!,
											(MemberExpression)levelExpression,
											level,
											(n, ctx, ex, l, _) => n == 0 ?
												GetSequence(expression!, level)!.ConvertToIndex(expression, level + 1, flags) :
												ctx.ConvertToIndex(ex, l, flags));
									}

								case ExpressionType.Parameter:
								case ExpressionType.Extension:

									if (levelExpression != expression)
										return GetSequence(expression!, level)!.ConvertToIndex(expression, level + 1, flags);
									break;
							}

							break;*/
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
					return new IsExpressionResult(Sequence.Length == 1 ?
						ReferenceEquals(expression, Lambda.Parameters[0]) :
						Lambda.Parameters.Any(p => ReferenceEquals(p, expression)));
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
					{
						var projection = MakeProjection(expression, out _);

						var sequence = Builder.GetContext(this, projection);
						if (sequence == null)
							return IsExpressionResult.False;

						if (sequence == this)
						{
							return new IsExpressionResult(requestFlag == RequestFor.Expression);
						}

						return sequence.IsExpression(projection, 0, requestFlag);

						/*
						return ProcessScalar(
							expression,
							level,
							(ctx, ex, l) =>
								ctx == null ? IsExpressionResult.False : ctx.IsExpression(ex, l, requestFlag),
							() => new IsExpressionResult(requestFlag == RequestFor.Expression), false);
					*/
					}	
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
									return new IsExpressionResult(Members.Values.Any(member => IsExpression(member, 0, requestFlag).Result));

								return new IsExpressionResult(requestFlag == RequestFor.Object);
							}

							var projection = MakeProjection(expression, out _);

							if (projection == null)
								break;

							switch (projection.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										var ctx = Builder.GetContext(this, projection);
										if (ctx == null)
											return IsExpressionResult.False;

										return ctx.IsExpression(projection, projection.GetLevel(Builder.MappingSchema), requestFlag);
									}

								case ExpressionType.Parameter    :
									{
										var sequence  = GetSequence(projection, 0);

										if (sequence == null)
										{
											var buildInfo = new BuildInfo(Parent, expression, new SelectQuery());
											if (!Builder.IsSequence(buildInfo))
												break;

											sequence = Builder.BuildSequence(buildInfo);
											return sequence.IsExpression(projection, level, requestFlag);
										}

										var idx = Sequence.Length == 0 ? 0 : Array.IndexOf(Sequence, sequence);
										if (idx < 0)
										{
											return IsExpressionResult.False;
										}

										return sequence.IsExpression(expression, 0, requestFlag);
									}

								case ExpressionType.New          :
								case ExpressionType.MemberInit   : return new IsExpressionResult(requestFlag == RequestFor.Object);
								case ExpressionType.Conditional:
								{
									var cond = (ConditionalExpression)projection;

									var trueResult  = IsExpression(cond.IfTrue, 0, requestFlag);
									var falseResult = IsExpression(cond.IfFalse, 0, requestFlag);

									if (requestFlag == RequestFor.Expression)
										return new IsExpressionResult(trueResult.Result && falseResult.Result);

									return new IsExpressionResult(trueResult.Result || falseResult.Result);
								}

								default:
									{
										if (projection is ContextRefExpression refExpression)
										{
											return refExpression.BuildContext.IsExpression(null, 0, requestFlag);
										}
										return new IsExpressionResult(requestFlag == RequestFor.Expression);
									}
							}

							break;

							/*var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
									{
										var member     = ((MemberExpression)levelExpression).Member;

										var memberExpression = GetProjectedExpression(member, false);
										if (memberExpression == null)
										{
											var nm = Members.Keys.FirstOrDefault(m => m.Name == member.Name);

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
												return new IsExpressionResult(requestFlag == RequestFor.Expression);
											//throw new InvalidOperationException(
											//	string.Format("Invalid member '{0}.{1}'", member.DeclaringType, member.Name));
										}

										if (ReferenceEquals(levelExpression, expression))
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
											(n,ctx,ex,l,ex1) => n == 0 ?
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
								case ExpressionType.MemberInit   : return new IsExpressionResult(requestFlag == RequestFor.Object);
								case ExpressionType.Conditional:
								{
									var cond = (ConditionalExpression)levelExpression;

									var trueResult  = IsExpression(cond.IfTrue, 0, requestFlag);
									var falseResult = IsExpression(cond.IfFalse, 0, requestFlag);

									if (requestFlag == RequestFor.Expression)
										return new IsExpressionResult(trueResult.Result && falseResult.Result);

									return new IsExpressionResult(trueResult.Result || falseResult.Result);
								}

								default:
									{
										if (levelExpression is ContextRefExpression refExpression)
										{
											if (levelExpression == expression)
												return refExpression.BuildContext.IsExpression(null, 0, requestFlag);
											return refExpression.BuildContext.IsExpression(expression, level + 1, requestFlag);
										}
										return new IsExpressionResult(requestFlag == RequestFor.Expression);
									}
							}

							break;*/
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
			if (SequenceHelper.IsSameContext(expression, this))
				return this;

			var projection = MakeProjection(expression, out _);

			if (projection != expression)
			{
				var ctx = Builder.GetContext(this, projection);
				if (ctx != null)
					return ctx.GetContext(projection, 0, buildInfo);
			}

			return null;

			if (IsScalar)
			{
				return ProcessScalar(
					expression,
					level,
					(ctx, ex, l) => ctx!.GetContext(ex, l, buildInfo),
					() => throw new NotImplementedException(), true);
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
							if (levelExpression is ContextRefExpression)
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

		public virtual void SetAlias(string alias)
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

		T ProcessScalar<T>(Expression expression, int level, Func<IBuildContext?,Expression?,int,T> action, Func<T> defaultAction, bool throwOnError)
		{
			throw new NotImplementedException();
			if (level == 0)
			{
				if (Body.NodeType == ExpressionType.Parameter && Lambda.Parameters.Count == 1)
				{
					var sequence = GetSequence(Body, 0)!;

					return ReferenceEquals(expression, Body) ?
						action(sequence, null,       0) :
						action(sequence, expression, 1);
				}

				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

				if (!ReferenceEquals(levelExpression, expression))
				{
					var ctx = GetSequence(expression, level);
					return ctx == null ? defaultAction() : action(ctx, expression, Sequence.Contains(ctx) ? level + 1 : 0);
				}

				if (expression.NodeType == ExpressionType.Parameter)
				{
					var sequence  = GetSequence(expression, level)!;
					var idx       = Array.IndexOf(Sequence, sequence);
					var parameter = Sequence.Length == 0 ? Lambda.Parameters[0] :
						idx < 0 ? null : Lambda.Parameters[idx];

					if (parameter != null && ReferenceEquals(levelExpression, parameter))
						return action(sequence, null, 0);
				}

				return Body.NodeType switch
				{
					ExpressionType.MemberAccess => action(GetSequence(expression, level)!, null, 0),
					_                           => defaultAction(),
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

					var result = action(this, newExpression, 0);

					Builder.RemoveConvertedExpression(newExpression);

					return result;
				}
				else if (root is ContextRefExpression refExpression)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level - 1);
					var newExpression   = GetExpression(expression, levelExpression, Body);

					Builder.UpdateConvertedExpression(expression, newExpression);

					var result = action(refExpression.BuildContext, newExpression, 0);

					Builder.RemoveConvertedExpression(newExpression);

					return result;
				}
			}

			if (throwOnError)
				throw new NotImplementedException();

			return default!;
		}

		public static List<MemberInfo> GetMemberPath(MemberExpression? memberExpression, out Expression? root)
		{
			var result = new List<MemberInfo>();
			root = null;
			while (memberExpression != null)
			{
				root = memberExpression.Expression;
				result.Add(memberExpression.Member);
				memberExpression = memberExpression.Expression as MemberExpression;
			}

			result.Reverse();
			return result;
		}

		public static List<Expression> GetMemberPath2(Expression? expression, out Expression? root)
		{
			var result = new List<Expression>();
			root = null;

			var current = expression;
			while (current != null)
			{
				var prev = current;
				switch (current.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						result.Add(current);
						current = ((MemberExpression)current).Expression;
						break;
					}
					case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)current;
						if (mc.IsQueryable())
						{
							result.Add(mc);
							current = mc.Arguments[0];
						}
						break;
					}
				}

				if (prev == current)
				{
					result.Add(current);
					break;
				}
			}

			result.Reverse();
			return result;
		}

		public static Expression ProcessMember(Expression currentBody, int currentIndex, List<MemberInfo> members)
		{
			if (currentIndex >= members.Count)
			{
				return currentBody;
			}

			var currentMember = members[currentIndex];

			{
				switch (currentBody.NodeType)
				{
					case ExpressionType.New:
					{
						var newExpression = (NewExpression)currentBody;

						if (newExpression.Members != null)
						{
							for (var index = 0; index < newExpression.Members.Count; index++)
							{
								var member = newExpression.Members[index];
								if (currentMember == member)
								{
									return ProcessMember(newExpression.Arguments[index], currentIndex + 1, members);
								}
							}
						}

						break;
					}

					case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)currentBody;

						/*
						if (mc.Object != null)
						{
							return ProcessMember(mc.Object, currentIndex + 1, members);
						}

						if (currentMember.DeclaringType?.IsAssignableFrom(mc.Type) == true)
							return ProcessMember(mc.Arguments[0], currentIndex + 1, members);
						*/

						if (currentMember.DeclaringType?.IsAssignableFrom(mc.Type) == true)
						{
							var newMa = Expression.MakeMemberAccess(currentBody, currentMember);

							return ProcessMember(newMa, currentIndex + 1, members);
						}

						return currentBody;
					}

					case ExpressionType.MemberInit:
					{
						var memberInitExpression = (MemberInitExpression)currentBody;
						var newExpression        = memberInitExpression.NewExpression;

						if (newExpression.Members != null)
						{
							for (var index = 0; index < newExpression.Members.Count; index++)
							{
								var member = newExpression.Members[index];
								if (currentMember == member)
								{
									return ProcessMember(newExpression.Arguments[index], currentIndex + 1, members);
								}
							}
						}

						for (int index = 0; index < memberInitExpression.Bindings.Count; index++)
						{
							var binding = memberInitExpression.Bindings[index];
							switch (binding.BindingType)
							{
								case MemberBindingType.Assignment:
								{
									var assignment = (MemberAssignment)binding;
									if (assignment.Member == currentMember)
									{
										return ProcessMember(assignment.Expression, currentIndex + 1, members);
									}
									break;
								}	
								case MemberBindingType.MemberBinding:
									break;
								case MemberBindingType.ListBinding:
									break;
								default:
									throw new NotImplementedException();
							}
						}

						break;
					}

					case ExpressionType.Conditional:
					{
						var cond = (ConditionalExpression)currentBody;

						var trueExpr  = ProcessMember(cond.IfTrue, currentIndex, members);
						var falseExpr = ProcessMember(cond.IfFalse, currentIndex, members);

						return Expression.Condition(cond.Test, trueExpr, falseExpr);
					}

					case ExpressionType.MemberAccess:
					{
						if (currentMember.DeclaringType?.IsAssignableFrom(currentBody.Type) != true)
							return currentBody;

						var newMa = Expression.MakeMemberAccess(currentBody, currentMember);

						return ProcessMember(newMa, currentIndex + 1, members);
					}

					case ExpressionType.Extension:
					{
						if (currentBody is ContextRefExpression)
						{
							var newBody = Expression.MakeMemberAccess(currentBody, currentMember);
							return ProcessMember(newBody, currentIndex + 1, members);
						}

						throw new NotImplementedException();
					}

				}
			}
			/*
			else if (currentPath is MethodCallExpression mc && mc.IsQueryable())
			{
				/*var args = mc.Arguments.ToArray();
				if (typeof(IGrouping<,>).IsSameOrParentOf(args[0].Type))
					args[0] = S;
				return mc.Update(mc.Object, args);#1#
				return mc;
			}
			else
			{
				if (currentIndex == 0 && (currentPath.NodeType == ExpressionType.Call 
				                          || currentPath.NodeType == ExpressionType.Parameter 
				                          || currentPath is ContextRefExpression))
					return ProcessMember(currentBody, 1, path);
				throw new NotImplementedException();
			}

			*/
			return Expression.Constant(DefaultValue.GetValue(currentMember.GetMemberType()), currentMember.GetMemberType());
		}

		Expression? MakeProjection(Expression? baseExpression, out MemberInfo? member)
		{
			member = null;

			if (baseExpression == null)
				return Body;

			var result = baseExpression;

			if (baseExpression is ContextRefExpression cr && cr.BuildContext == this)
			{
				return Body;
			}

			if (baseExpression.NodeType == ExpressionType.MemberAccess)
			{
				var memberPath = GetMemberPath((MemberExpression)baseExpression, out var root);

				member = memberPath[memberPath.Count - 1];

				/*if (memberPath.Count        > 0 && memberPath[0] is ContextRefExpression contextRef &&
				    contextRef.BuildContext == this)
				{
					result   = result.Replace(memberPath[0], new ContextRefExpression(memberPath[0].Type, Sequence[0]));
					sequence = Sequence[0];
					return result;
				}*/

				result = ProcessMember(Body, 0, memberPath);
			}

			result = result.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					var idx = Lambda.Parameters.IndexOf((ParameterExpression)e);
					if (idx >= 0 && idx < Sequence.Length)
					{
						return new ContextRefExpression(e.Type, Sequence[idx]);
					}
				}

				return e;
			});

			return result;
		}

		/*
		Expression MakeProjectionOld(MemberExpression memberExpression)
		{
			Expression ProcessMember(Expression currentBody, int currentIndex, List<MemberInfo> memberPath)
			{
				if (currentIndex >= memberPath.Count)
				{
					return currentBody;
				}

				var currentMember = memberPath[currentIndex];

				switch (currentBody.NodeType)
				{
					case ExpressionType.New:
					{
						var newExpression = (NewExpression)currentBody;

						for (var index = 0; index < newExpression.Members.Count; index++)
						{
							var member = newExpression.Members[index];
							if (currentMember == member)
							{
								return ProcessMember(newExpression.Arguments[index], currentIndex + 1, memberPath);
							}
						}


						break;
					}

					case ExpressionType.Conditional:
					{
						var cond = (ConditionalExpression)currentBody;

						var trueExpr  = ProcessMember(cond.IfTrue, currentIndex, memberPath);
						var falseExpr = ProcessMember(cond.IfFalse, currentIndex, memberPath);

						return Expression.Condition(cond.Test, trueExpr, falseExpr);
					}

					case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression)currentBody;
						return ma;
					}

				}

				return Expression.Constant(DefaultValue.GetValue(currentMember.GetMemberType()), currentMember.GetMemberType());
			}


			var memberPath = GetMemberPath(memberExpression, out var root);

			var result = ProcessMember(CorrectedBody, 0, memberPath);

			return result;
		}
		*/

		T ProcessMemberAccess<T>(Expression expression, MemberExpression levelExpression, int level,
			Func<int,IBuildContext,Expression?,int,Expression,T> action)
		{
			throw new NotImplementedException();
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
						return action(1, sequence, null, 0, memberExpression);
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
						return action(2, sequence, newExpression, nextLevel, memberExpression);
					break;

				case ExpressionType.New          :
				case ExpressionType.MemberInit   :
					{
						var mmExpresion = GetMemberExpression(memberExpression, expression, level + 1);
						return action(3, this, mmExpresion, 0, memberExpression);
					}
				case ExpressionType.Conditional:
				{
					if (levelExpression != expression)
					{
						var cond = (ConditionalExpression)memberExpression;

						var trueExpression  = GetMemberExpression(cond.IfTrue, expression, level  + 1);
						var falseExpression = GetMemberExpression(cond.IfFalse, expression, level + 1);


						if (trueExpression.NodeType == ExpressionType.MemberAccess)
						{
							return ProcessMemberAccess(trueExpression, (MemberExpression)trueExpression, 1, action);
						}

						if (falseExpression.NodeType == ExpressionType.MemberAccess)
						{
							return ProcessMemberAccess(falseExpression, (MemberExpression)falseExpression, 1, action);
						}

						var newCod = Expression.Condition(cond.Test, trueExpression, falseExpression);

						//ProcessMemberAccess<T>(newCod, trueExpression, 0, action);

						return action(3, this, newCod, 0, memberExpression);
					}
					break;
				}
			}

			return action(0, this, null, 0, memberExpression);
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
			throw new NotImplementedException();
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

			if (memberExpression is ConstantExpression cnt && cnt.Value == null)
			{
				return Expression.Constant(DefaultValue.GetValue(expression.Type), expression.Type);
			}

			if (!memberExpression.Type.IsAssignableFrom(levelExpression.Type))
				return memberExpression;

			return !ReferenceEquals(levelExpression, expression) ?
				expression.Transform(ex => ReferenceEquals(ex, levelExpression) ? memberExpression : ex) :
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
