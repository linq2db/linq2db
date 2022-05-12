using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using LinqToDB.Extensions;
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

				_isObject = true;

				_type = _methodCall.Method.GetGenericArguments()[0];

				Init();
			}

			readonly Type?                         _type;
			readonly bool                          _isObject;
			readonly MethodCallExpression          _methodCall;
			readonly SubQueryContext               _sequence1;
			readonly SubQueryContext               _sequence2;

			readonly Dictionary<Expression, (
				int idx,
				SqlGenericConstructorExpression.Assignment left,
				SqlGenericConstructorExpression.Assignment right)> _matchedPairs = new(ExpressionEqualityComparer.Instance);

			readonly Dictionary<Expression, SqlPlaceholderExpression> _createdSQL = new(ExpressionEqualityComparer.Instance);

			private SqlGenericConstructorExpression? _body;

			static string? GenerateColumnAlias(Expression expr)
			{
				var current = expr;
				string? alias   = null;
				while (current is MemberExpression memberExpression)
				{
					if (alias != null)
						alias = memberExpression.Member.Name + "_" + alias;
					else
						alias = memberExpression.Member.Name;
					current = memberExpression.Expression;
				}

				return alias;
			}

			SqlGenericConstructorExpression MatchSequences(Expression root, Expression leftExpr,  Expression rightExpr, IBuildContext leftSequence, IBuildContext rightSequence)
			{
				if (leftExpr is SqlGenericConstructorExpression leftGenericPrep &&
				    rightExpr is not SqlGenericConstructorExpression)
				{
					throw new NotImplementedException();
				}

				if (rightExpr is SqlGenericConstructorExpression rightGenericPrep &&
				    rightExpr is not SqlGenericConstructorExpression)
				{
					throw new NotImplementedException();
				}

				if (leftExpr is SqlGenericConstructorExpression leftGeneric &&
				    rightExpr is SqlGenericConstructorExpression rightGeneric)
				{
					var newAssignments =
						new List<SqlGenericConstructorExpression.Assignment>(leftGeneric.Assignments.Count);

					var matched = new HashSet<MemberInfo>(MemberInfoComparer.Instance);

					for (int l = 0; l < leftGeneric.Assignments.Count; l++)
					{
						var left = leftGeneric.Assignments[l];

						var found = false;

						var rootObj = root;
						var mi      = root.Type.GetMemberEx(left.MemberInfo);

						if (mi == null)
						{
							// handling inheritance mapping
							//
							if (left.MemberInfo.ReflectedType == null)
								throw new InvalidOperationException();

							rootObj = new ContextRefExpression(left.MemberInfo.ReflectedType, this);
							mi      = left.MemberInfo;
						}

						var ma = Expression.MakeMemberAccess(rootObj, mi);

						var assignmentExpr = (Expression)ma;

						for (int r = 0; r < rightGeneric.Assignments.Count; r++)
						{
							var right = rightGeneric.Assignments[r];
							if (MemberInfoComparer.Instance.Equals(left.MemberInfo, right.MemberInfo))
							{
								if (left.Expression is SqlGenericConstructorExpression || right.Expression is SqlGenericConstructorExpression)
									assignmentExpr = MatchSequences(assignmentExpr, left.Expression, right.Expression, leftSequence, rightSequence);
								else
									_matchedPairs.Add(ma, (_matchedPairs.Count, left, right));

								found = true;
								break;
							}
						}

						if (!found)
						{
							if (left.Expression is not SqlPlaceholderExpression leftPlaceholder)
								throw new NotImplementedException();

							// generate NULL value
							var dbType = QueryHelper.GetDbDataType(leftPlaceholder.Sql);
							var right = new SqlGenericConstructorExpression.Assignment(left.MemberInfo,
								ExpressionBuilder.CreatePlaceholder(rightSequence, new SqlValue(dbType, null),
									Expression.MakeMemberAccess(rightGeneric, left.MemberInfo)));

							_matchedPairs.Add(ma, (_matchedPairs.Count, left, right));
						}

						newAssignments.Add(new SqlGenericConstructorExpression.Assignment(left.MemberInfo, assignmentExpr));

						matched.Add(left.MemberInfo);

					}

					// Enumerate from other side to match not found pairs from first iteration
					//
					for (int r = 0; r < rightGeneric.Assignments.Count; r++)
					{
						var right = rightGeneric.Assignments[r];

						if (matched.Contains(right.MemberInfo))
							continue;

						if (right.Expression is not SqlPlaceholderExpression rightPlaceholder)
							throw new NotImplementedException();

						var ma = Expression.MakeMemberAccess(root, right.MemberInfo);
						newAssignments.Add(new SqlGenericConstructorExpression.Assignment(right.MemberInfo, ma));

						// generate NULL value
						var dbType = QueryHelper.GetDbDataType(rightPlaceholder.Sql);
						var left = new SqlGenericConstructorExpression.Assignment(right.MemberInfo,
							ExpressionBuilder.CreatePlaceholder(leftSequence, new SqlValue(dbType, null),
								Expression.MakeMemberAccess(rightGeneric, right.MemberInfo)));

						_matchedPairs.Add(ma, (_matchedPairs.Count, left, right));

						newAssignments.Add(new SqlGenericConstructorExpression.Assignment(left.MemberInfo, ma));
					}

					// Shortcut, we can result object from any side
					//
					if (leftGeneric.ConstructType  == SqlGenericConstructorExpression.CreateType.Full &&
					    rightGeneric.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
					{
						return leftGeneric;
					};

					//TODO: try to merge with constructor
					return new SqlGenericConstructorExpression(false, leftGeneric.ObjectType, newAssignments);
				}

				throw new NotImplementedException();
			}

			void Init()
			{
				var ref1 = new ContextRefExpression(_type, _sequence1);
				var ref2 = new ContextRefExpression(_type, _sequence2);

				var expr1 = SqlGenericConstructorExpression.Parse(Builder.ConvertToSqlExpr(_sequence1, ref1));
				var expr2 = SqlGenericConstructorExpression.Parse(Builder.ConvertToSqlExpr(_sequence2, ref2));

				var root = new ContextRefExpression(_type, this);

				_body = MatchSequences(root, expr1, expr2, _sequence1, _sequence2);

				foreach (var matchedPair in _matchedPairs.OrderBy(x => x.Value.idx))
				{
					var alias = GenerateColumnAlias(matchedPair.Key);

					var leftExpression = Builder.ConvertToSqlExpr(this, matchedPair.Value.left.Expression);
					if (leftExpression is SqlPlaceholderExpression placehoderLeft)
					{
						if (alias != null)
							placehoderLeft.Alias = alias;

						var leftColumn = Builder.MakeColumn(SelectQuery, placehoderLeft);
						_createdSQL.Add(matchedPair.Key, leftColumn);

						var rightExpression = Builder.ConvertToSqlExpr(this, matchedPair.Value.right.Expression);
						if (rightExpression is not SqlPlaceholderExpression placehoderRight)
							throw new InvalidOperationException();

						if (alias != null)
							placehoderRight.Alias = alias;

						var rightColumn = Builder.MakeColumn(_sequence2.SelectQuery, placehoderRight);
					}
				}
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr = Builder.FinalizeProjection(this,
					Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			// For Set we have to ensure hat columns are not optimized
			protected override bool OptimizeColumns => false;

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) &&
				    (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)))
				{
					return path;
				}

				if (flags.HasFlag(ProjectFlags.SQL))
				{
					if (_body != null)
					{
						var projected = Builder.Project(this, path, null, -1, flags, _body);
						if (projected is MemberExpression)
						{
							if (_createdSQL.TryGetValue(projected, out var placeholderExpression))
								return placeholderExpression;
							return Builder.CreateSqlError(this, projected);
						}
						return projected;
					}
				}
				else if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression))
				{
					if (_body != null)
					{
						if (_body.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
						{
							var expr = Builder.BuildEntityExpression(this, _body.ObjectType, flags);

							return expr;
						}

						//_body.TryConstruct()

						throw new NotImplementedException($"Handle other CreateTypes: {_body.ConstructType}");
					}

					throw new NotImplementedException("Scalar handling not implemented");
				}

				return base.MakeExpression(path, flags);
			}
		}

		#endregion
	}
}
