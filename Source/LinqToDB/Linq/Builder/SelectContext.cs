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
		public string Path          => this.GetPath();
		public int    ContextId     { get; }

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

		public SelectContext(IBuildContext? parent, ExpressionBuilder builder, LambdaExpression lambda, SelectQuery selectQuery, bool isSubQuery)
		{
			Parent      = parent;
			Sequence    = Array<IBuildContext>.Empty;
			Builder     = builder;
			Lambda      = lambda;
			Body        = lambda.Body;
			SelectQuery = selectQuery;
			IsSubQuery  = isSubQuery;

			IsScalar = !Builder.ProcessProjection(Members, Body);

			Builder.Contexts.Add(this);
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
		}

		public SelectContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences)
		{
			Parent     = parent;
			Sequence   = sequences;
			Builder    = sequences[0].Builder;
			Lambda     = lambda;
			IsSubQuery = isSubQuery;
			Body       = SequenceHelper.PrepareBody(lambda, sequences);

			SelectQuery   = sequences[0].SelectQuery;

			foreach (var context in Sequence)
				context.Parent = this;

			IsScalar = !Builder.ProcessProjection(Members, Body);

			Builder.Contexts.Add(this);
#if DEBUG
			ContextId = Builder.GenerateContextId();
#endif
		}

		#endregion

		#region BuildQuery

		public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			var expr = Builder.FinalizeProjection(this,
				Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

			var mapper = Builder.BuildMapper<T>(expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		#endregion

		#region BuildExpression

		ParameterExpression? _rootExpression;

		public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region ConvertToSql

		public virtual SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		SqlInfo[] ConvertExpressions(Expression expression, ConvertFlags flags, ColumnDescriptor? columnDescriptor)
		{
			return Builder.ConvertExpressions(this, expression, flags, columnDescriptor);
		}

		#endregion

		#region ConvertToIndex

		public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		#endregion

		public SqlInfo? MakeSql(Expression path)
		{
			throw new NotImplementedException();
			Expression expr;

			if (SequenceHelper.IsSameContext(path, this))
			{
				Body = Builder.Project(this, Body, null, 0, ProjectFlags.SQL, Body);
				expr = Body;
			}
			else
			{
				var correctedPath = path;
				expr = Builder.Project(this, correctedPath, null, 0, ProjectFlags.SQL, Body);
			}

			var sql = Builder.ConvertToSql(this, expr);

			return new SqlInfo(sql, SelectQuery);
		}

		public SqlInfo MakeColumn(Expression path, SqlInfo sqlInfo, string? alias)
		{
			return SequenceHelper.MakeColumn(SelectQuery, sqlInfo, alias);
		}

		public virtual Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			Expression result;

			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.HasFlag(ProjectFlags.Root))
				{
					if (Body is ContextRefExpression)
						return Body;

					return path;
				}

				if (path.Type != Body.Type && flags.HasFlag(ProjectFlags.Expression))
					return new SqlEagerLoadExpression(this, path, GetEagerLoadExpression(path));

				result = Body;
			}
			else
			{
				result = Builder.Project(this, path, null, 0, flags, Body);

				if (!ReferenceEquals(result, Body))
				{
					if (flags.HasFlag(ProjectFlags.Root) && !(result is ContextRefExpression || result is MemberExpression || result is MethodCallExpression))
					{
						return path;
					}
				}
			}

			return result;
		}

		public virtual Expression GetEagerLoadExpression(Expression path)
		{
			return Builder.GetSequenceExpression(this);
		}
		
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
							requestFlag,
							expression,
							level,
							static (requestFlag, ctx, ex, l) => ctx == null ? IsExpressionResult.False : ctx.IsExpression(ex, l, requestFlag),
							static requestFlag => IsExpressionResult.GetResult(requestFlag == RequestFor.Expression), false); */
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
										return IsExpressionResult.GetResult(requestFlag == RequestFor.Expression);
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
					return ctx.GetContext(projection, 0, new BuildInfo(buildInfo, projection!));
			}

			return null;
		}

		#endregion

		#region ConvertToParentIndex

		public virtual int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
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

				result = ProcessMember(Body, 0, memberPath);
			}
			else if (baseExpression.NodeType == ExpressionType.Call)
			{
				var mc            = (MethodCallExpression)baseExpression;
				var newProjection = MakeProjection(mc.Arguments[0], out member);

				if (newProjection == null)
					throw new InvalidOperationException();

				result = mc.Replace(mc.Arguments[0], newProjection);
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

		public bool IsSubQuery { get; }

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

		#endregion

	}
}
