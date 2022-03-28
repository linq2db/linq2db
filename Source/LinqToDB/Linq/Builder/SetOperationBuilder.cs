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
	using Reflection;
	using SqlQuery;

	class SetOperationBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = { "Concat", "UnionAll", "Union", "Except", "Intersect", "ExceptAll", "IntersectAll" };

		#region Builder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.Arguments.Count == 2 && methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence1 = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var sequence2 = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			SetOperation setOperation;
			switch (methodCall.Method.Name)
			{
				case "Concat"      :
				case "UnionAll"    : setOperation = SetOperation.UnionAll;     break;
				case "Union"       : setOperation = SetOperation.Union;        break;
				case "Except"      : setOperation = SetOperation.Except;       break;
				case "ExceptAll"   : setOperation = SetOperation.ExceptAll;    break;
				case "Intersect"   : setOperation = SetOperation.Intersect;    break;
				case "IntersectAll": setOperation = SetOperation.IntersectAll; break;
				default            :
					throw new ArgumentException($"Invalid method name {methodCall.Method.Name}.");
			}

			var needsEmulation = !builder.DataContext.SqlProviderFlags.IsAllSetOperationsSupported &&
								 (setOperation == SetOperation.ExceptAll || setOperation == SetOperation.IntersectAll)
								 ||
								 !builder.DataContext.SqlProviderFlags.IsDistinctSetOperationsSupported &&
								 (setOperation == SetOperation.Except || setOperation == SetOperation.Intersect);

			if (needsEmulation)
			{
				// emulation

				var sequence = new SubQueryContext(sequence1);
				var query    = sequence2;
				var except   = query.SelectQuery;

				var sql = sequence.SelectQuery;

				if (setOperation == SetOperation.Except || setOperation == SetOperation.Intersect)
					sql.Select.IsDistinct = true;

				except.ParentSelect = sql;

				if (setOperation == SetOperation.Except || setOperation == SetOperation.ExceptAll)
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


			var set1  = sequence1 as SetOperationContext;
			var set2  = sequence2 as SetOperationContext;

			// mixing set operators in single query will result in wrong results
			if (set1 != null && set1.Sequences[0].SelectQuery.SetOperators[0].Operation != setOperation)
				set1 = null;
			if (set2 != null && set2.Sequences[0].SelectQuery.SetOperators[0].Operation != setOperation)
				set2 = null;

			if (set1 != null)
			{
				if (set2 == null)
				{
					var seq2        = new SubQueryContext(sequence2);
					var setOperator = new SqlSetOperator(seq2.SelectQuery, setOperation);
					set1.AddSequence(seq2, setOperator);
				}
				else
				{
					set1.AddSequence(set2.Sequences[0], new SqlSetOperator(set2.Sequences[0].SelectQuery, setOperation));
					for (var i = 1; i < set2.Sequences.Count; i++)
						set1.AddSequence(set2.Sequences[i], set2.Sequences[0].SelectQuery.SetOperators[i - 1]);
					set2.Sequences[0].SelectQuery.SetOperators.Clear();
				}
			}
			else
			{
				var seq1 = new SubQueryContext(sequence1);
				if (set2 == null)
				{
					var seq2 = new SubQueryContext(sequence2);
					set1     = new SetOperationContext(seq1, seq2, methodCall, new SqlSetOperator(seq2.SelectQuery, setOperation));
				}
				else
				{
					set1 = new SetOperationContext(seq1, set2.Sequences[0], methodCall, new SqlSetOperator(set2.Sequences[0].SelectQuery, setOperation));
					for (var i = 1; i < set2.Sequences.Count; i++)
						set1.AddSequence(set2.Sequences[i], set2.Sequences[0].SelectQuery.SetOperators[i - 1]);
					set2.Sequences[0].SelectQuery.SetOperators.Clear();
				}
			}

			return set1;
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
			public SetOperationContext(SubQueryContext sequence1, SubQueryContext sequence2, MethodCallExpression methodCall, SqlSetOperator setOperator)
				: base(sequence1)
			{
				_isObject =
					sequence1.IsExpression(null, 0, RequestFor.Object).Result ||
					sequence2.IsExpression(null, 0, RequestFor.Object).Result;

				if (_isObject)
				{
					_type           = methodCall.Method.GetGenericArguments()[0];
					_unionParameter = Expression.Parameter(_type, "t");
				}

				// initial sequences
				AddSequence(sequence1, null);
				AddSequence(sequence2, setOperator);
			}

			readonly Type?                         _type;
			readonly bool                          _isObject;
			readonly ParameterExpression?          _unionParameter;
			readonly Dictionary<MemberInfo,Member> _members   = new(new MemberInfoComparer());
			         List<UnionMember>?            _unionMembers;

			public readonly List<SubQueryContext>  Sequences = new ();

			[DebuggerDisplay("{Member.MemberExpression}, SequenceInfo: ({SequenceInfo}), SqlQueryInfo: ({SqlQueryInfo})")]
			class Member
			{
				public SqlInfo?          SequenceInfo;
				public SqlInfo?          SqlQueryInfo;
				public MemberExpression  MemberExpression = null!;
			}

			[DebuggerDisplay("{Member.MemberExpression}, Infos.Count: {Infos.Count}, Infos[0]: ({Infos[0]}), Infos[1]: ({Infos[1]})")]
			class UnionMember
			{
				public UnionMember(Member member, SqlInfo info)
				{
					Member = member;
					Infos.Add(info);
				}

				public readonly Member        Member;
				public readonly List<SqlInfo> Infos  = new ();
				public          string        Alias  = null!;
			}

			public void AddSequence(SubQueryContext sequence, SqlSetOperator? setOperator)
			{
				var isFirst = Sequences.Count == 0;
				Sequences.Add(sequence);

				// no need to set "sequence1.Parent = this" for first sequence?
				if (!isFirst)
					sequence.Parent = this;

				if (setOperator != null)
					Sequences[0].SelectQuery.SetOperators.Add(setOperator);

				var infos = sequence.ConvertToIndex(null, 0, ConvertFlags.All);

				if (!_isObject)
					return;

				if (isFirst)
					_unionMembers = new ();

				foreach (var info in infos)
				{
					if (info.MemberChain.Length == 0)
						throw new InvalidOperationException();

					if (isFirst)
					{
						var mi = info.MemberChain.First(m => m.DeclaringType!.IsSameOrParentOf(_unionParameter!.Type));

						var member = new Member
						{
							SequenceInfo     = info,
							MemberExpression = Expression.MakeMemberAccess(_unionParameter, mi)
						};

						_unionMembers!.Add(new UnionMember(member, info));
					}
					else
					{
						UnionMember? em = null;

						foreach (var m in _unionMembers!)
						{
							if (m.Member.SequenceInfo != null &&
								m.Infos.Count < Sequences.Count &&
								m.Member.SequenceInfo.CompareMembers(info))
							{
								em = m;
								break;
							}
						}

						if (em == null)
						{
							foreach (var m in _unionMembers!)
							{
								if (m.Member.SequenceInfo != null &&
									m.Infos.Count < Sequences.Count &&
									m.Member.SequenceInfo.CompareLastMember(info))
								{
									em = m;
									break;
								}
							}
						}

						if (em == null)
						{
							var member = new Member { MemberExpression = Expression.MakeMemberAccess(_unionParameter, info.MemberChain[0]) };

							if (sequence.IsExpression(member.MemberExpression, 1, RequestFor.Object).Result)
								throw new LinqException("Types in UNION are constructed incompatibly.");

							_unionMembers.Add(em = new UnionMember(member, info));
							if (em.Infos.Count < Sequences.Count)
							{
								var dbType = QueryHelper.GetDbDataType(info.Sql);
								if (dbType.SystemType == typeof(object))
									dbType = dbType.WithSystemType(info.MemberChain.Last().GetMemberType());

								while (em.Infos.Count < Sequences.Count)
								{
									var idx = Sequences.Count - em.Infos.Count - 1;

									var newInfo = new SqlInfo(
										info.MemberChain,
										new SqlValue(dbType, null),
										Sequences[idx].SelectQuery,
										_unionMembers.Count - 1);

									em.Infos.Insert(0, newInfo);

									if (idx == 0)
										em.Member.SequenceInfo = newInfo;
								}
							}
						}
						else
						{
							em.Infos.Add(info);
						}
					}
				}

				// add nulls for missing columns in current sequence
				var midx = -1;
				foreach (var member in _unionMembers!)
				{
					midx++;
					if (member.Infos.Count == Sequences.Count)
						continue;

					//if (sequence.IsExpression(member.Member.MemberExpression, 1, RequestFor.Object).Result)
					//	throw new LinqException("Types in UNION are constructed incompatibly.");

					var info = member.Infos[0];

					var dbType = QueryHelper.GetDbDataType(info.Sql);
					if (dbType.SystemType == typeof(object))
						dbType = dbType.WithSystemType(info.MemberChain.Last().GetMemberType());

					var newInfo = new SqlInfo(
						info.MemberChain,
						new SqlValue(dbType, null),
						sequence.SelectQuery,
						midx);
					member.Infos.Add(newInfo);
				}

				// currently re-run for each sequence > 2...
				if (Sequences.Count > 1)
					FinalizeAliases();
			}

			private void FinalizeAliases()
			{
				for (var i = 0; i < _unionMembers!.Count; i++)
				{
					var member = _unionMembers[i];

					member.Alias = GetShortAlias(member);

					member.Member.SequenceInfo = member.Member.SequenceInfo!.WithIndex(i);

					_members[member.Member.MemberExpression.Member] = member.Member;
				}

				var nonUnique = _unionMembers
					.GroupBy(static m => m.Alias, StringComparer.InvariantCultureIgnoreCase)
					.Where(static g => g.Count() > 1);

				foreach (var g in nonUnique)
					foreach (var member in g)
						member.Alias = GetFullAlias(member);

				var idx = 0;
				foreach (var sequence in Sequences)
				{
					sequence.SelectQuery.Select.Columns.Clear();
					sequence.ColumnIndexes.Clear();

					for (var i = 0; i < _unionMembers.Count; i++)
					{
						var member = _unionMembers[i];

						sequence.SelectQuery.Select.AddNew(member.Infos[idx].Sql);
						SetAliasForColumns(sequence.SelectQuery.Select.Columns[i], member.Alias);
						sequence.ColumnIndexes[i] = i;
					}

					idx++;
				}

				static string GetFullAlias(UnionMember member)
				{
					foreach (var info in member.Infos)
					{
						if (info.MemberChain.Length > 0)
							return string.Join("_", info.MemberChain.Select(static m => m.Name));
					}

					return member.Member.MemberExpression.Member.Name;
				}

				static string GetShortAlias(UnionMember member)
				{
					foreach (var info in member.Infos)
					{
						if (info.MemberChain.Length > 0)
							return info.MemberChain[info.MemberChain.Length - 1].Name;
					}

					return member.Member.MemberExpression.Member.Name;
				}

				static void SetAliasForColumns(SqlColumn column, string alias)
				{
					var current = column;
					column.RawAlias = null;

					while (current.Expression is SqlColumn c)
					{
						c.RawAlias = null;
						current = c;
					}

					current.RawAlias = alias;
				}
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
						var type  = _type!;
						var nctor = (NewExpression?)Expression.Find(type, static (type, e) => e is NewExpression ne && e.Type == type && ne.Arguments?.Count > 0);

						Expression expr;

						if (nctor != null)
						{
							var recordType = RecordsHelper.GetRecordType(Builder.MappingSchema, nctor.Type);
							if ((recordType & RecordType.CallConstructorOnRead) != 0)
							{
								if (nctor.Members != null)
									throw new LinqToDBException($"Call to '{nctor.Type}' record constructor cannot have initializers.");
								else if (nctor.Arguments.Count == 0)
									throw new LinqToDBException($"Call to '{nctor.Type}' record constructor requires parameters.");
								else
								{
									var ctorParms = nctor.Constructor!.GetParameters();

									var parms = new List<Expression>();
									for (var i = 0; i < ctorParms.Length; i++)
									{
										var p = ctorParms[i];
										parms.Add(ExpressionHelper.PropertyOrField(_unionParameter!, p.Name!));
									}

									expr = Expression.New(nctor.Constructor!, parms);
								}
							}
							else
							{
								if (nctor.Members == null)
									throw new LinqToDBException($"Call to '{nctor.Type}' constructor lacks initializers.");
								else
								{
									var members = nctor.Members
										.Select(m => m is MethodInfo info ? info.GetPropertyInfo() : m)
										.ToList();

									expr = Expression.New(
										nctor.Constructor!,
										members.Select(m => ExpressionHelper.PropertyOrField(_unionParameter!, m.Name)),
										members);

								}
							}

							var ex = Builder.BuildExpression(this, expr, enforceServerSide);
							return ex;
						}

						var findVisitor  = FindVisitor<Type>.Create(type, static (type, e) => e.NodeType == ExpressionType.MemberInit && e.Type == type);
						var news         = new MemberInitExpression?[Sequences.Count];
						var needsRewrite = false;
						var hasValue     = false;

						for (var i = 0; i < Sequences.Count; i++)
						{
							news[i] = (MemberInitExpression?)findVisitor.Find(Sequences[i].Expression);
							if (news[i] == null)
								needsRewrite = true;
							else
								hasValue = true;
						}

						if (!needsRewrite)
						{
							// Comparing bindings
							var first = news[0]!;

							for (var i = 1; i < news.Length; i++)
							{
								if (first.Bindings.Count != news[i]!.Bindings.Count)
								{
									needsRewrite = true;
									break;
								}
							}

							if (!needsRewrite)
							{
								foreach (var binding in first.Bindings)
								{
									if (binding.BindingType != MemberBindingType.Assignment)
									{
										needsRewrite = true;
										break;
									}

									foreach (var next in news.Skip(1))
									{
										MemberBinding? foundBinding = null;
										foreach (var b in next!.Bindings)
										{
											if (b.Member == binding.Member)
											{
												foundBinding = b;
												break;
											}
										}

										if (foundBinding == null || foundBinding.BindingType != MemberBindingType.Assignment)
										{
											needsRewrite = true;
											break;
										}

										var assignment1 = (MemberAssignment)binding;
										var assignment2 = (MemberAssignment)foundBinding;

										if (!assignment1.Expression.EqualsTo(assignment2.Expression, Builder.OptimizationContext.GetSimpleEqualsToContext(false)) ||
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

									if (needsRewrite)
										break;
								}
							}
						}
						else
							needsRewrite = hasValue;

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
							Expression? ex = null;
							foreach (var s in Sequences)
							{
								var res = s.BuildExpression(null, level, enforceServerSide);
								if (ex == null)
									ex = res;
							}

							return ex!;
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

				foreach (var sequence in Sequences)
				{
					if (sequence.IsExpression(testExpression, level, RequestFor.Association).Result)
					{
						throw new LinqToDBException(
							"Associations with Concat/Union or other Set operations are not supported.");
					}
				}

				var ret   = Sequences[0].BuildExpression(expression, level, enforceServerSide);

				//if (level == 1)
				//	_sequence2.BuildExpression(expression, level);

				return ret;
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				if (requestFlag == RequestFor.Root && ReferenceEquals(expression, _unionParameter))
					return IsExpressionResult.True;

				return base.IsExpression(expression, level, requestFlag);
			}


			// For Set we have to ensure hat columns are not optimized
			protected override bool OptimizeColumns => false;

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				if (_isObject)
				{
					return ConvertToSql(expression, level, flags)
						.Select(idx =>
							idx
								.WithIndex(GetIndex(idx.Index, (SqlColumn)idx.Sql))
						)
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
											member.SequenceInfo!.Index
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
