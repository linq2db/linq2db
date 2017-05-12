using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Reflection;
	using SqlQuery;

	class ConcatUnionBuilder : MethodCallBuilder
	{
		#region Builder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.Arguments.Count == 2 && methodCall.IsQueryable("Concat", "Union");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence1 = new SubQueryContext(builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0])));
			var sequence2 = new SubQueryContext(builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery())));
			var union     = new SelectQuery.Union(sequence2.SelectQuery, methodCall.Method.Name == "Concat");

			sequence1.SelectQuery.Unions.Add(union);

			return new UnionContext(sequence1, sequence2, methodCall);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		#endregion

		#region Context

		sealed class UnionContext : SubQueryContext
		{
			public UnionContext(SubQueryContext sequence1, SubQueryContext sequence2, MethodCallExpression methodCall)
				: base(sequence1)
			{
				_sequence1  = sequence1;
				_sequence2  = sequence2;
				_methodCall = methodCall;

				_sequence2.Parent = this;

				_isObject =
					sequence1.IsExpression(null, 0, RequestFor.Object).Result ||
					sequence2.IsExpression(null, 0, RequestFor.Object).Result;

				if (_isObject)
				{
					_type           = _methodCall.Method.GetGenericArguments()[0];
					_unionParameter = Expression.Parameter(_type, "t");
				}

				Init();
			}

			readonly Type                          _type;
			readonly bool                          _isObject;
			readonly MethodCallExpression          _methodCall;
			readonly ParameterExpression           _unionParameter;
			readonly Dictionary<MemberInfo,Member> _members = new Dictionary<MemberInfo,Member>(new MemberInfoComparer());
			readonly SubQueryContext               _sequence1;
			readonly SubQueryContext               _sequence2;

			class Member
			{
				public SqlInfo          SequenceInfo;
				public SqlInfo          SqlQueryInfo;
				public MemberExpression MemberExpression;
			}

			class UnionMember
			{
				public Member  Member;
				public SqlInfo Info1;
				public SqlInfo Info2;
			}

			void Init()
			{
				var info1 = _sequence1.ConvertToIndex(null, 0, ConvertFlags.All).ToList();
				var info2 = _sequence2.ConvertToIndex(null, 0, ConvertFlags.All).ToList();

				if (!_isObject)
					return;

				var members = new List<UnionMember>();

				foreach (var info in info1)
				{
					if (info.Members.Count == 0)
						throw new InvalidOperationException();

					var mi = info.Members.First(m => m.DeclaringType.IsSameOrParentOf(_unionParameter.Type));

					var member = new Member
					{
						SequenceInfo     = info,
						MemberExpression = Expression.MakeMemberAccess(_unionParameter, mi)
					};

					members.Add(new UnionMember { Member = member, Info1 = info });
				}

				foreach (var info in info2)
				{
					if (info.Members.Count == 0)
						throw new InvalidOperationException();

					var em = members.FirstOrDefault(m =>
						m.Member.SequenceInfo != null &&
						m.Info2 == null &&
						m.Member.SequenceInfo.CompareMembers(info));

					if (em == null)
					{
						em = members.FirstOrDefault(m =>
							m.Member.SequenceInfo != null &&
							m.Info2 == null &&
							m.Member.SequenceInfo.CompareLastMember(info));
					}

					if (em == null)
					{
						var member = new Member { MemberExpression = Expression.MakeMemberAccess(_unionParameter, info.Members[0]) };

						if (_sequence2.IsExpression(member.MemberExpression, 1, RequestFor.Object).Result)
							throw new LinqException("Types in {0} are constructed incompatibly.", _methodCall.Method.Name);

						members.Add(new UnionMember { Member = member, Info2 = info });
					}
					else
					{
						em.Info2 = info;
					}
				}

				_sequence1.SelectQuery.Select.Columns.Clear();
				_sequence2.SelectQuery.Select.Columns.Clear();

				for (var i = 0; i < members.Count; i++)
				{
					var member = members[i];

					if (member.Info1 == null)
					{
						var type = members.First(m => m.Info1 != null).Info1.Members.First().GetMemberType();
						member.Info1 = new SqlInfo(member.Info2.Members)
						{
							Sql   = new SqlValue(type, null),
							Query = _sequence1.SelectQuery,
						};

						member.Member.SequenceInfo = member.Info1;
					}

					if (member.Info2 == null)
					{
						var spam = members.First(m => m.Info2 != null).Info2.Members.First();
						var type = spam.GetMemberType();

						member.Info2 = new SqlInfo(member.Info1.Members)
						{
							Sql   = new SqlValue(type, null),
							Query = _sequence2.SelectQuery,
						};
					}

					_sequence1.SelectQuery.Select.Columns.Add(new SelectQuery.Column(_sequence1.SelectQuery, member.Info1.Sql));
					_sequence2.SelectQuery.Select.Columns.Add(new SelectQuery.Column(_sequence2.SelectQuery, member.Info2.Sql));

					member.Member.SequenceInfo.Index = i;

					_members[member.Member.MemberExpression.Member] = member.Member;
				}

				foreach (var key in _sequence1.ColumnIndexes.Keys.ToList())
					_sequence1.ColumnIndexes[key] = _sequence1.SelectQuery.Select.Add(key);

				foreach (var key in _sequence2.ColumnIndexes.Keys.ToList())
					_sequence2.ColumnIndexes[key] = _sequence2.SelectQuery.Select.Add(key);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0);
				var mapper = Builder.BuildMapper<T>(expr);

				query.SetQuery(mapper);
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				if (_isObject)
				{
					if (expression == null)
					{
						var type  = _methodCall.Method.GetGenericArguments()[0];
						var nctor = (NewExpression)Expression.Find(e =>
						{
							if (e.NodeType == ExpressionType.New && e.Type == type)
							{
								var ne = (NewExpression)e;
								return ne.Arguments != null && ne.Arguments.Count > 0;
							}

							return false;
						});

						Expression expr;

						if (nctor != null)
						{
							var members = nctor.Members
								.Select(m => m is MethodInfo ? ((MethodInfo)m).GetPropertyInfo() : m)
								.ToList();

							expr = Expression.New(
								nctor.Constructor,
								members
									.Select(m => Expression.PropertyOrField(_unionParameter, m.Name))
									.Cast<Expression>(),
								members);
						}
						else
						{
							var ta = TypeAccessor.GetAccessor(type);

							expr = Expression.MemberInit(
								Expression.New(ta.Type),
								_members
									.Select(m => Expression.Bind(m.Value.MemberExpression.Member, m.Value.MemberExpression))
									.Cast<MemberBinding>());
						}

						var ex = Builder.BuildExpression(this, expr);

						return ex;
					}

					if (level == 0 || level == 1)
					{
						var levelExpression = expression.GetLevelExpression(1);

						if (ReferenceEquals(expression, levelExpression) && !IsExpression(expression, 1, RequestFor.Object).Result)
						{
							var idx = ConvertToIndex(expression, level, ConvertFlags.Field);
							var n   = idx[0].Index;

							if (Parent != null)
								n = Parent.ConvertToParentIndex(n, this);

							return Builder.BuildSql(expression.Type, n);
						}
					}
				}

				var ret = _sequence1.BuildExpression(expression, level);

				//if (level == 1)
				//	_sequence2.BuildExpression(expression, level);

				return ret;
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor testFlag)
			{
				if (testFlag == RequestFor.Root && ReferenceEquals(expression, _unionParameter))
					return IsExpressionResult.True;

				return base.IsExpression(expression, level, testFlag);
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				if (_isObject)
				{
					return ConvertToSql(expression, level, flags)
						.Select(idx =>
						{
							if (idx.Index < 0)
							{
								if (idx.Index == -2)
								{
									SelectQuery.Select.Columns.Add(new SelectQuery.Column(SelectQuery, idx.Sql));
									idx.Index = SelectQuery.Select.Columns.Count - 1;
								}
								else
								{
									idx.Index = SelectQuery.Select.Add(idx.Sql);
								}
							}

							return idx;
						})
						.ToArray();
				}

				return base.ConvertToIndex(expression, level, flags);
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				if (_isObject)
				{
					switch (flags)
					{
						case ConvertFlags.All   :
						case ConvertFlags.Key   :

							if (expression == null)
							{
								return _members.Values
									.Select(m => ConvertToSql(m.MemberExpression, 0, ConvertFlags.Field)[0])
									.ToArray();
							}

							break;

						case ConvertFlags.Field :

							if (expression != null && (level == 0 || level == 1) && expression.NodeType == ExpressionType.MemberAccess)
							{
								var levelExpression = expression.GetLevelExpression(1);

								if (expression == levelExpression)
								{
									var ma = (MemberExpression)expression;

									Member member;

									if (!_members.TryGetValue(ma.Member, out member))
									{
										var ed = Builder.MappingSchema.GetEntityDescriptor(_type);

										if (ed.Aliases != null && ed.Aliases.ContainsKey(ma.Member.Name))
										{
											var alias = ed[ma.Member.Name];

											if (alias != null)
											{
												var cd = ed[alias.MemberName];

												if (cd != null)
													_members.TryGetValue(cd.MemberInfo, out member);
											}
										}
									}

									if (member == null)
										throw new LinqToDBException(
											string.Format("Expression '{0}' is not a field.", expression));

									if (member.SqlQueryInfo == null)
									{
										member.SqlQueryInfo = new SqlInfo(member.MemberExpression.Member)
										{
											Index = -2,
											Sql   = SubQuery.SelectQuery.Select.Columns[member.SequenceInfo.Index],
											Query = SelectQuery,
										};
									}

									return new[] { member.SqlQueryInfo };
								}
							}

							break;
					}

					throw new InvalidOperationException();
				}

				return base.ConvertToSql(expression, level, flags);
			}
		}

		#endregion
	}
}
