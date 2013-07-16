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
	using SqlBuilder;

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
			var sequence2 = new SubQueryContext(builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SqlQuery())));
			var union     = new SqlQuery.Union(sequence2.SqlQuery, methodCall.Method.Name == "Concat");

			sequence1.SqlQuery.Unions.Add(union);

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
				_methodCall = methodCall;

				_isObject =
					sequence1.IsExpression(null, 0, RequestFor.Object).Result ||
					sequence2.IsExpression(null, 0, RequestFor.Object).Result;

				if (_isObject)
				{
					var type = _methodCall.Method.GetGenericArguments()[0];
					_unionParameter = Expression.Parameter(type, "t");
				}

				Init(sequence1, sequence2);
			}

			readonly bool                          _isObject;
			readonly MethodCallExpression          _methodCall;
			readonly ParameterExpression           _unionParameter;
			readonly Dictionary<MemberInfo,Member> _members = new Dictionary<MemberInfo,Member>(new MemberInfoComparer());

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

			void Init(SubQueryContext sequence1, SubQueryContext sequence2)
			{
				var info1 = sequence1.ConvertToIndex(null, 0, ConvertFlags.All).ToList();
				var info2 = sequence2.ConvertToIndex(null, 0, ConvertFlags.All).ToList();

				if (!_isObject)
					return;

				var members = new List<UnionMember>();

				foreach (var info in info1)
				{
					if (info.Member == null)
						throw new InvalidOperationException();

					var member = new Member
					{
						SequenceInfo     = info,
						MemberExpression = Expression.MakeMemberAccess(_unionParameter, info.Member)
					};

					//if (sequence1.IsExpression(member.MemberExpression, 1, RequestFor.Object).Result)
					//	throw new LinqException("Types in {0} are constructed incompatibly.", _methodCall.Method.Name);

					members.Add(new UnionMember { Member = member, Info1 = info });
				}

				foreach (var info in info2)
				{
					if (info.Member == null)
						throw new InvalidOperationException();

					var em = members.FirstOrDefault(m =>
						m.Member.SequenceInfo != null &&
						m.Member.SequenceInfo.Member.EqualsTo(info.Member));

					if (em == null)
					{
						var member = new Member { MemberExpression = Expression.MakeMemberAccess(_unionParameter, info.Member) };

						if (sequence2.IsExpression(member.MemberExpression, 1, RequestFor.Object).Result)
							throw new LinqException("Types in {0} are constructed incompatibly.", _methodCall.Method.Name);

						members.Add(new UnionMember { Member = member, Info2 = info });
					}
					else
					{
						em.Info2 = info;
					}
				}

				sequence1.SqlQuery.Select.Columns.Clear();
				sequence2.SqlQuery.Select.Columns.Clear();

				for (var i = 0; i < members.Count; i++)
				{
					var member = members[i];

					if (member.Info1 == null)
					{
						member.Info1 = new SqlInfo
						{
							Sql    = new SqlValue(null),
							Query  = sequence1.SqlQuery,
							Member = member.Info2.Member
						};

						member.Member.SequenceInfo = member.Info1;
					}

					if (member.Info2 == null)
					{
						member.Info2 = new SqlInfo
						{
							Sql    = new SqlValue(null),
							Query  = sequence2.SqlQuery,
							Member = member.Info1.Member
						};
					}

					sequence1.SqlQuery.Select.Columns.Add(new SqlQuery.Column(sequence1.SqlQuery, member.Info1.Sql));
					sequence2.SqlQuery.Select.Columns.Add(new SqlQuery.Column(sequence2.SqlQuery, member.Info2.Sql));

					member.Member.SequenceInfo.Index = i;
					//member.Info1.Index = i;
					//member.Info2.Index = i;

					_members[member.Member.MemberExpression.Member] = member.Member;
				}

				foreach (var key in sequence1.ColumnIndexes.Keys.ToList())
					sequence1.ColumnIndexes[key] = sequence1.SqlQuery.Select.Add(key);

				foreach (var key in sequence2.ColumnIndexes.Keys.ToList())
					sequence2.ColumnIndexes[key] = sequence2.SqlQuery.Select.Add(key);

				/*
				if (_isObject)
				{
					foreach (var info in info1)
					{
						if (info.Member == null)
							throw new InvalidOperationException();

						CheckAndAddMember(sequence1, sequence2, info);
					}

					info2 = sequence2.ConvertToIndex(null, 0, ConvertFlags.All).OrderBy(_ => _.Index).ToList();

					if (info1.Count != info2.Count)
					{
						for (var i = 0; i < info2.Count; i++)
						{
							if (i < info1.Count)
							{
								if (info1[i].Index != info2[i].Index)
									throw new InvalidOperationException();
							}
							else
							{
								CheckAndAddMember(sequence2, sequence1, info2[i]);
							}
						}
					}
				}
				else
					sequence2.ConvertToIndex(null, 0, ConvertFlags.All).OrderBy(_ => _.Index).ToList();
				*/
			}

			void CheckAndAddMember(IBuildContext sequence1, IBuildContext sequence2, SqlInfo info)
			{
				var member = new Member
				{
					SequenceInfo     = info,
					MemberExpression = Expression.PropertyOrField(_unionParameter, info.Member.Name)
				};

				if (sequence1.IsExpression(member.MemberExpression, 1, RequestFor.Object).Result)
					throw new LinqException("Types in {0} are constructed incompatibly.", _methodCall.Method.Name);

				var idx = sequence2.ConvertToIndex(member.MemberExpression, 1, ConvertFlags.Field);

				if (idx[0].Index != member.SequenceInfo.Index)
					throw new LinqException("Types in {0} are constructed incompatibly.", _methodCall.Method.Name);

				_members.Add(member.MemberExpression.Member, member);
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

						if (ReferenceEquals(expression, levelExpression))
						{
							var idx = ConvertToIndex(expression, level, ConvertFlags.Field);
							var n   = idx[0].Index;

							if (Parent != null)
								n = Parent.ConvertToParentIndex(n, this);

							return Builder.BuildSql(expression.Type, n);
						}
					}
				}

				return base.BuildExpression(expression, level);
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
									SqlQuery.Select.Columns.Add(new SqlQuery.Column(SqlQuery, idx.Sql));
									idx.Index = SqlQuery.Select.Columns.Count - 1;
								}
								else
								{
									idx.Index = SqlQuery.Select.Add(idx.Sql);
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
									var ma     = (MemberExpression)expression;
									var member = _members[ma.Member];

									if (member.SqlQueryInfo == null)
									{
										member.SqlQueryInfo = new SqlInfo
										{
											Index  = -2,
											Sql    = SubQuery.SqlQuery.Select.Columns[member.SequenceInfo.Index],
											Query  = SqlQuery,
											Member = member.MemberExpression.Member,
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
