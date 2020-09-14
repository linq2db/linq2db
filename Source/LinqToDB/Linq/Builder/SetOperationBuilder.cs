﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Tools;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Reflection;
	using SqlQuery;

	class SetOperationBuilder : MethodCallBuilder
	{
		#region Builder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.Arguments.Count == 2 && methodCall.IsQueryable("Concat", "UnionAll", "Union", "Except", "Intersect", "ExceptAll", "IntersectAll");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence1 = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var sequence2 = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			SetOperation setOperation;
			switch (methodCall.Method.Name)
			{
				case "Concat"       : 
				case "UnionAll"     : setOperation = SetOperation.UnionAll;     break;
				case "Union"        : setOperation = SetOperation.Union;        break;
				case "Except"       : setOperation = SetOperation.Except;       break;
				case "ExceptAll"    : setOperation = SetOperation.ExceptAll;    break;
				case "Intersect"    : setOperation = SetOperation.Intersect;    break;
				case "IntersectAll" : setOperation = SetOperation.IntersectAll; break;
				default:
					throw new ArgumentException($"Invalid method name {methodCall.Method.Name}.");
			}

			var needsEmulation = !builder.DataContext.SqlProviderFlags.IsAllSetOperationsSupported &&
			                     setOperation.In(SetOperation.ExceptAll, SetOperation.IntersectAll)
			                     ||
			                     !builder.DataContext.SqlProviderFlags.IsDistinctSetOperationsSupported &&
			                     setOperation.In(SetOperation.Except, SetOperation.Intersect);

			if (needsEmulation)
			{
				// emulation

				var sequence = new SubQueryContext(sequence1);
				var query    = sequence2;
				var except   = query.SelectQuery;

				var sql = sequence.SelectQuery;

				if (setOperation.In(SetOperation.Except, SetOperation.Intersect))
					sql.Select.IsDistinct = true;

				except.ParentSelect = sql;

				if (setOperation.In(SetOperation.Except, SetOperation.ExceptAll))
					sql.Where.Not.Exists(except);
				else
					sql.Where.Exists(except);

				var keys1 = sequence.ConvertToSql(null, 0, ConvertFlags.All);
				var keys2 = query.   ConvertToSql(null, 0, ConvertFlags.All);

				if (keys1.Length != keys2.Length)
					throw new InvalidOperationException();

				for (var i = 0; i < keys1.Length; i++)
				{
					except.Where
						.Expr(keys1[i].Sql)
						.Equal
						.Expr(keys2[i].Sql);
				}

				return sequence;
			}

			var set1 = new SubQueryContext(sequence1);
			var set2 = new SubQueryContext(sequence2);

			var setOperator = new SqlSetOperator(set2.SelectQuery, setOperation);

			set1.SelectQuery.SetOperators.Add(setOperator);

			return new SetOperationContext(set1, set2, methodCall);
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		#endregion

		#region Context

		sealed class SetOperationContext : SubQueryContext
		{
			public SetOperationContext(SubQueryContext sequence1, SubQueryContext sequence2, MethodCallExpression methodCall)
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

			readonly Type?                         _type;
			readonly bool                          _isObject;
			readonly MethodCallExpression          _methodCall;
			readonly ParameterExpression?          _unionParameter;
			readonly Dictionary<MemberInfo,Member> _members = new Dictionary<MemberInfo,Member>(new MemberInfoComparer());
			readonly SubQueryContext               _sequence1;
			readonly SubQueryContext               _sequence2;

			class Member
			{
				public SqlInfo?          SequenceInfo;
				public SqlInfo?          SqlQueryInfo;
				public MemberExpression  MemberExpression = null!;
			}

			[DebuggerDisplay("{Member, Sql1: {Info1}, Sql2: {Info2}")]
			class UnionMember
			{
				public Member   Member = null!;
				public SqlInfo? Info1;
				public SqlInfo? Info2;
			}

			void Init()
			{
				var info1 = _sequence1.ConvertToIndex(null, 0, ConvertFlags.All).ToList();
				var info2 = _sequence2.ConvertToIndex(null, 0, ConvertFlags.All).ToList();

				if (!_isObject)
					return;

				var unionMembers = new List<UnionMember>();

				foreach (var info in info1)
				{
					if (info.MemberChain.Length == 0)
						throw new InvalidOperationException();

					var mi = info.MemberChain.First(m => m.DeclaringType!.IsSameOrParentOf(_unionParameter!.Type));

					var member = new Member
					{
						SequenceInfo     = info,
						MemberExpression = Expression.MakeMemberAccess(_unionParameter, mi)
					};

					unionMembers.Add(new UnionMember { Member = member, Info1 = info });
				}

				foreach (var info in info2)
				{
					if (info.MemberChain.Length == 0)
						throw new InvalidOperationException();

					var em = unionMembers.FirstOrDefault(m =>
						m.Member.SequenceInfo != null &&
						m.Info2 == null &&
						m.Member.SequenceInfo.CompareMembers(info));

					if (em == null)
					{
						em = unionMembers.FirstOrDefault(m =>
							m.Member.SequenceInfo != null &&
							m.Info2 == null &&
							m.Member.SequenceInfo.CompareLastMember(info));
					}

					if (em == null)
					{
						var member = new Member { MemberExpression = Expression.MakeMemberAccess(_unionParameter, info.MemberChain[0]) };

						if (_sequence2.IsExpression(member.MemberExpression, 1, RequestFor.Object).Result)
							throw new LinqException("Types in {0} are constructed incompatibly.", _methodCall.Method.Name);

						unionMembers.Add(new UnionMember { Member = member, Info2 = info });
					}
					else
					{
						em.Info2 = info;
					}
				}

				var aliases1 = _sequence1.SelectQuery.Select.Columns.ToLookup(c => c.Expression, c => c.Alias);
				var aliases2 = _sequence2.SelectQuery.Select.Columns.ToLookup(c => c.Expression, c => c.Alias);

				_sequence1.SelectQuery.Select.Columns.Clear();
				_sequence2.SelectQuery.Select.Columns.Clear();

				for (var i = 0; i < unionMembers.Count; i++)
				{
					var member = unionMembers[i];

					if (member.Info1 == null)
					{
						var type = member.Info2!.Sql.SystemType;
						if (type == null)
							type = member.Info2!.MemberChain.Last().GetMemberType();
						member.Info1 = new SqlInfo
						(
							member.Info2!.MemberChain,
							new SqlValue(type, null),
							_sequence1.SelectQuery
						);

						member.Member.SequenceInfo = member.Info1;
					}

					if (member.Info2 == null)
					{
						var spam = unionMembers.First(m => m.Info2 != null).Info2!.MemberChain.First();
						var type = spam.GetMemberType();

						member.Info2 = new SqlInfo
						(
							member.Info1.MemberChain,
							new SqlValue(type, null),
							_sequence2.SelectQuery
						);
					}

					static string? GetAlias(ILookup<ISqlExpression, string?> aliases, ISqlExpression expression)
					{
						if (aliases.Contains(expression))
							return aliases[expression].FirstOrDefault();
						return null;
					}

					_sequence1.SelectQuery.Select.Columns.Add(new SqlColumn(_sequence1.SelectQuery, member.Info1.Sql, GetAlias(aliases1, member.Info1.Sql)));
					_sequence2.SelectQuery.Select.Columns.Add(new SqlColumn(_sequence2.SelectQuery, member.Info2.Sql, GetAlias(aliases2, member.Info2.Sql)));

					if (member.Member.SequenceInfo != null)
						member.Member.SequenceInfo = member.Member.SequenceInfo.WithIndex(i);

					_members[member.Member.MemberExpression.Member] = member.Member;
				}

				foreach (var key in _sequence1.ColumnIndexes.Keys.ToList())
					_sequence1.ColumnIndexes[key] = _sequence1.SelectQuery.Select.Add(key);

				foreach (var key in _sequence2.ColumnIndexes.Keys.ToList())
					_sequence2.ColumnIndexes[key] = _sequence2.SelectQuery.Select.Add(key);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				if (_isObject)
				{
					if (expression == null)
					{
						var type  = _methodCall.Method.GetGenericArguments()[0];
						var nctor = (NewExpression?)Expression.Find(e => e is NewExpression ne && e.Type == type && ne.Arguments?.Count > 0);

						Expression expr;

						if (nctor != null)
						{
							var members = nctor.Members
								.Select(m => m is MethodInfo info ? info.GetPropertyInfo() : m)
								.ToList();

							expr = Expression.New(
								nctor.Constructor,
								members.Select(m => ExpressionHelper.PropertyOrField(_unionParameter!, m.Name)),
								members);

							var ex = Builder.BuildExpression(this, expr, enforceServerSide);
							return ex;
						}

						var new1 = Expression.Find(e => e.NodeType == ExpressionType.MemberInit && e.Type == type);
						var needsRewrite = false;
						if (new1 != null)
						{
							var new2 = _sequence2.Expression.Find(e => e.NodeType == ExpressionType.MemberInit && e.Type == type);
							if (new2 == null)
								needsRewrite = true;
							else
							{
								// Comparing bindings

								var init1 = (MemberInitExpression)new1;
								var init2 = (MemberInitExpression)new2;
								needsRewrite = init1.Bindings.Count != init2.Bindings.Count;
								if (!needsRewrite)
								{
									var accessorDic = new Dictionary<Expression, QueryableAccessor>();

									foreach (var binding in init1.Bindings)
									{
										if (binding.BindingType != MemberBindingType.Assignment)
										{
											needsRewrite = true;
											break;
										}

										var foundBinding = init2.Bindings.FirstOrDefault(b => b.Member == binding.Member);
										if (foundBinding == null || foundBinding.BindingType != MemberBindingType.Assignment)
										{
											needsRewrite = true;
											break;
										}

										var assignment1 = (MemberAssignment)binding;
										var assignment2 = (MemberAssignment)foundBinding;

										if (!assignment1.Expression.EqualsTo(assignment2.Expression, Builder.DataContext, accessorDic, null, null) || 
										    !(assignment1.Expression.NodeType == ExpressionType.MemberAccess || assignment1.Expression.NodeType == ExpressionType.Parameter))
										{
											needsRewrite = true;
											break;
										}

										// is is parameters, we have to select
										if (assignment1.Expression.NodeType == ExpressionType.MemberAccess
										    && Builder.GetRootObject(assignment1.Expression)?.NodeType == ExpressionType.Constant)
										{
											needsRewrite = true;
											break;
										}

									}
								}
							}
						}

						if (needsRewrite)
						{
							var ta = TypeAccessor.GetAccessor(type);

							expr = Expression.MemberInit(
								Expression.New(ta.Type),
								_members.Select(m =>
									Expression.Bind(m.Value.MemberExpression.Member, m.Value.MemberExpression)));
							var ex = Builder.BuildExpression(this, expr, enforceServerSide);
							return ex;
						}
						else
						{
							var ex = _sequence1.BuildExpression(null, level, enforceServerSide);
							return ex;
						}
					}

					if (level == 0 || level == 1)
					{
						var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, 1);

						if (ReferenceEquals(expression, levelExpression) && !IsExpression(expression, 1, RequestFor.Object).Result)
						{
							var idx = ConvertToIndex(expression, level, ConvertFlags.Field);
							var n   = idx[0].Index;

							if (Parent != null)
								n = Parent.ConvertToParentIndex(n, this);

							return Builder.BuildSql(expression.Type, n, idx[0].Sql);
						}
					}
				}

				var testExpression = expression?.GetLevelExpression(Builder.MappingSchema, level);

				if (_sequence1.IsExpression(testExpression, level, RequestFor.Association).Result
				 || _sequence2.IsExpression(testExpression, level, RequestFor.Association).Result)
				{
					throw new LinqToDBException(
						"Associations with Concat/Union or other Set operations are not supported.");
				}

				var ret   = _sequence1.BuildExpression(expression, level, enforceServerSide);

				//if (level == 1)
				//	_sequence2.BuildExpression(expression, level);

				return ret;
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor testFlag)
			{
				if (testFlag == RequestFor.Root && ReferenceEquals(expression, _unionParameter))
					return IsExpressionResult.True;

				return base.IsExpression(expression, level, testFlag);
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				if (_isObject)
				{
					return ConvertToSql(expression, level, flags)
						.Select(idx =>
						{
							if (idx.Index >= 0) return idx;

							var newIdx = idx;
							if (idx.Index == -2)
							{
								SelectQuery.Select.Columns.Add(new SqlColumn(SelectQuery, idx.Sql));
								newIdx = idx.WithIndex(SelectQuery.Select.Columns.Count - 1);
							}
							else
							{
								newIdx = idx.WithIndex(SelectQuery.Select.Add(idx.Sql));
							}

							if (!ReferenceEquals(newIdx, idx))
							{
								foreach (var member in _members)
								{
									if (ReferenceEquals(idx, member.Value.SqlQueryInfo))
										member.Value.SqlQueryInfo = newIdx;
								}
							}

							return newIdx;
						})
						.ToArray();
				}

				return base.ConvertToIndex(expression, level, flags);
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
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

							if (expression != null && expression.NodeType == ExpressionType.MemberAccess)
							{
								var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level == 0 ? 1 : level);

								if (expression == levelExpression)
								{
									var ma = (MemberExpression)expression;

									if (!_members.TryGetValue(ma.Member, out var member))
									{
										var ed = Builder.MappingSchema.GetEntityDescriptor(_type!);

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
										throw new LinqToDBException($"Expression '{expression}' is not a field.");

									if (member.SqlQueryInfo == null)
									{
										member.SqlQueryInfo = new SqlInfo
										(
											member.MemberExpression.Member,
											SubQuery.SelectQuery.Select.Columns[member.SequenceInfo!.Index],
											SelectQuery,
											-2
										);
									}

									return new[] { member.SqlQueryInfo };
								}

								return base.ConvertToSql(expression, level, flags);
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
