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

			return new SetOperationContext(setOperation, set1, set2, methodCall);
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
			public SetOperationContext(SetOperation setOperation, SubQueryContext sequence1, SubQueryContext sequence2,
				MethodCallExpression                methodCall)
				: base(sequence1)
			{
				_setOperation = setOperation;
				_sequence1    = sequence1;
				_sequence2    = sequence2;
				_methodCall   = methodCall;

				_sequence2.Parent = this;

				_isObject = true;

				_type = _methodCall.Method.GetGenericArguments()[0];

				Init();
			}

			readonly         Type                      _type;
			readonly         bool                      _isObject;
			readonly         MethodCallExpression      _methodCall;
			private readonly SetOperation              _setOperation;
			readonly         SubQueryContext           _sequence1;
			readonly         SubQueryContext           _sequence2;
			private          Expression                _leftSqlExpr  = null!;
			private          Expression                _rightSqlExpr = null!;
			private          SqlPlaceholderExpression? _setIdPlaceholder;

			readonly Dictionary<Expression, (
				int idx,
				SqlGenericConstructorExpression.Assignment left,
				SqlGenericConstructorExpression.Assignment right)> _matchedPairs = new(ExpressionEqualityComparer.Instance);

			private List<
				(
				Expression key,
				SqlGenericConstructorExpression.Parameter left,
				SqlGenericConstructorExpression.Parameter right)>? _matchedParamPairs;

			readonly Dictionary<Expression, SqlPlaceholderExpression> _createdSQL = new(ExpressionEqualityComparer.Instance);

			List<SqlPlaceholderExpression>? _createdParamSQL;

			private SqlGenericConstructorExpression? _body;

			static string? GenerateColumnAlias(Expression expr)
			{
				var     current = expr;
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

			static string? GenerateColumnAlias(SqlGenericConstructorExpression.Parameter param)
			{
				if (param.MemberInfo != null)
					return param.MemberInfo.Name;

				return GenerateColumnAlias(param.Expression);
			}

			SqlGenericConstructorExpression? MatchSequences(Expression root, Expression leftExpr, Expression rightExpr, IBuildContext leftSequence, IBuildContext rightSequence, 
				//ref Expression? leftCreate, ref Expression? rightCreate, 
				ref List<Expression>? mismatches)
			{
				leftExpr  = SqlGenericConstructorExpression.Parse(leftExpr);
				rightExpr = SqlGenericConstructorExpression.Parse(rightExpr);

				if (leftExpr is SqlGenericConstructorExpression leftGenericPrep &&
				    rightExpr is not SqlGenericConstructorExpression)
				{
					if (rightExpr is SqlPlaceholderExpression paceholder && paceholder.Sql is SqlValue value && value.Value == null)
					{
						return MatchSequences(root, leftExpr, new SqlGenericConstructorExpression(leftGenericPrep),
							leftSequence, rightSequence, ref mismatches);
					}

					throw new NotImplementedException();
				}

				if (rightExpr is SqlGenericConstructorExpression rightGenericPrep &&
				    leftExpr is not SqlGenericConstructorExpression)
				{
					if (leftExpr is SqlPlaceholderExpression paceholder && paceholder.Sql is SqlValue value && value.Value == null)
					{
						return MatchSequences(root, new SqlGenericConstructorExpression(rightGenericPrep),
							rightExpr,
							leftSequence, rightSequence, ref mismatches);
					}

					throw new NotImplementedException();
				}

				if (leftExpr is SqlGenericConstructorExpression leftGeneric &&
				    rightExpr is SqlGenericConstructorExpression rightGeneric)
				{
					var newAssignments = new List<SqlGenericConstructorExpression.Assignment>(leftGeneric.Assignments.Count);

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
								{
									assignmentExpr = MatchSequences(assignmentExpr, left.Expression, right.Expression,
										leftSequence, rightSequence, ref mismatches);
								}
								else
								{
									_matchedPairs.Add(ma, (_matchedPairs.Count, left, right));
								}

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
									Expression.Default(dbType.SystemType)), left.IsMandatory, left.IsLoaded);

							_matchedPairs.Add(ma, (_matchedPairs.Count, left, right));
						}

						newAssignments.Add(new SqlGenericConstructorExpression.Assignment(left.MemberInfo, assignmentExpr, left.IsMandatory, left.IsLoaded));

						matched.Add(left.MemberInfo);

					}

					// Enumerate from other side to match not found pairs from first iteration
					//
					for (int r = 0; r < rightGeneric.Assignments.Count; r++)
					{
						var right = rightGeneric.Assignments[r];

						if (matched.Contains(right.MemberInfo))
							continue;

						var assignmentExpr = (Expression)Expression.MakeMemberAccess(root, right.MemberInfo);

						if (right.Expression is SqlGenericConstructorExpression genericRight)
						{
							assignmentExpr = MatchSequences(assignmentExpr, new SqlGenericConstructorExpression(
									SqlGenericConstructorExpression.CreateType.Incompatible,
									genericRight.Type, null, null), right.Expression,
								leftSequence, rightSequence, ref mismatches);
						}
						else
						{
							if (right.Expression is not SqlPlaceholderExpression rightPlaceholder)
							{
								throw new NotImplementedException();
							}

							// generate NULL value
							var dbType = QueryHelper.GetDbDataType(rightPlaceholder.Sql);
							var left = new SqlGenericConstructorExpression.Assignment(right.MemberInfo,
								ExpressionBuilder.CreatePlaceholder(leftSequence, new SqlValue(dbType, null),
									Expression.MakeMemberAccess(rightGeneric, right.MemberInfo)), right.IsMandatory, right.IsLoaded);

							_matchedPairs.Add(assignmentExpr, (_matchedPairs.Count, left, right));
						}

						newAssignments.Add(new SqlGenericConstructorExpression.Assignment(right.MemberInfo, assignmentExpr, right.IsMandatory, right.IsLoaded));
					}

					var createType = SuggestCreateType(leftExpr.Type, leftGeneric.ConstructType,
						rightGeneric.ConstructType);

					//TODO: try to merge with constructor
					var resultConstructor = new SqlGenericConstructorExpression(createType, leftGeneric.ObjectType, null, newAssignments.AsReadOnly());

					var maxParametersCount = Math.Max(leftGeneric.Parameters.Count, rightGeneric.Parameters.Count);

					List<SqlGenericConstructorExpression.Parameter>? newParameters = null;      
					if (maxParametersCount > 0)
					{
						var isConstructMismatch = rightGeneric.Constructor       != leftGeneric.Constructor ||
						                          rightGeneric.ConstructorMethod != leftGeneric.ConstructorMethod;

						_matchedParamPairs ??= new ();

						newParameters = new List<SqlGenericConstructorExpression.Parameter>(maxParametersCount);
						for (int i = 0; i < leftGeneric.Parameters.Count; i++)
						{
							var left = leftGeneric.Parameters[i];

							if (left.MemberInfo != null)
							{
								var ma = Expression.MakeMemberAccess(root, left.MemberInfo);
								if (_matchedPairs.ContainsKey(ma))
								{
									// already matched by member
									newParameters.Add(left.WithExpression(ma));
									continue;
								}
							}

							SqlGenericConstructorExpression.Parameter? right = null;

							if (!isConstructMismatch)
							{
								if (rightGeneric.Parameters.Count == 0)
								{
									throw new NotImplementedException("Missing parameters handling not implemented yet");
								}

								right = rightGeneric.Parameters[i];
							}
							else
							{
								throw new NotImplementedException();

								// we cannot use positioned match. We have to use matching via MemberInfo
								if (left.MemberInfo != null)
								{
									throw new NotImplementedException();
								}
							}

							if (right == null)
							{
								throw new LinqToDBException("Could not find parameter match");
							}

							var assignmentExpr = (Expression)new SqlGenericParamAccessExpression(root, i, left.ParamType);

							if (left.Expression is SqlGenericConstructorExpression || right.Expression is SqlGenericConstructorExpression)
							{
								assignmentExpr = MatchSequences(assignmentExpr, left.Expression, right.Expression,
									leftSequence, rightSequence, ref mismatches);
							}
							else
							{
								_matchedParamPairs.Add((assignmentExpr, left, right));
							}


							/*if (leftParam.Expression is SqlGenericConstructorExpression ||  rightParam.Expression is SqlGenericConstructorExpression)
					{
								_ = MatchSequences(key)
							}*/

							newParameters.Add(left.WithExpression(assignmentExpr));

						}

						resultConstructor = resultConstructor.ReplaceParameters(newParameters);
					}

					return resultConstructor;
				}

				// Scalar
				return null;
			}

			public SqlGenericConstructorExpression.CreateType SuggestCreateType(Type entityType,
				SqlGenericConstructorExpression.CreateType left, SqlGenericConstructorExpression.CreateType right)
			{
				var ed             = Builder.MappingSchema.GetEntityDescriptor(entityType);
				var hasInheritance = ed.InheritanceMapping.Count > 0;

				var createType = (hasInheritance, left, right) switch
				{
					(_, SqlGenericConstructorExpression.CreateType.Full, SqlGenericConstructorExpression.CreateType.Full) => SqlGenericConstructorExpression.CreateType.Full,
					(_, _, SqlGenericConstructorExpression.CreateType.Incompatible ) => SqlGenericConstructorExpression.CreateType.Incompatible,
					(_, SqlGenericConstructorExpression.CreateType.Incompatible, _ ) => SqlGenericConstructorExpression.CreateType.Incompatible,
					(false, SqlGenericConstructorExpression.CreateType.Full, _) => SqlGenericConstructorExpression.CreateType.Auto,
					(false, _, SqlGenericConstructorExpression.CreateType.Full) => SqlGenericConstructorExpression.CreateType.Auto,
					(true, SqlGenericConstructorExpression.CreateType.Full,  _) => SqlGenericConstructorExpression.CreateType.Incompatible,
					(true, _, SqlGenericConstructorExpression.CreateType.Full ) => SqlGenericConstructorExpression.CreateType.Incompatible,
					_ => SqlGenericConstructorExpression.CreateType.Auto
				};

				return createType;
			}

			private static MethodInfo _keySetIdMethosInfo =
				Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(typeof(int));

			private const           string     ProjectionSetIdFieldName = "__projection__set_id__";
			private static readonly Expression _setIdFieldName          = Expression.Constant(ProjectionSetIdFieldName);

			Expression CorrectAssignments(Expression root, SqlGenericConstructorExpression constructorExpression)
			{
				if (constructorExpression.Assignments.Count > 0)
				{
					var newAssignments = new List<SqlGenericConstructorExpression.Assignment>(constructorExpression.Assignments.Count);
					foreach (var assignment in constructorExpression.Assignments)
					{
						var currentRoot = root;

						if (assignment.MemberInfo.DeclaringType != null)
						{
							if (!assignment.MemberInfo.DeclaringType.IsSameOrParentOf(currentRoot.Type))
							{
								if (currentRoot is ContextRefExpression contextRef)
									currentRoot = contextRef.WithType(assignment.MemberInfo.DeclaringType);
								else
									currentRoot = Expression.Convert(currentRoot, assignment.MemberInfo.DeclaringType);
							}
						}

						Expression newExpression = Expression.MakeMemberAccess(currentRoot, assignment.MemberInfo);
						if (assignment.Expression is SqlGenericConstructorExpression assignmentGeneric)
						{
							newExpression = CorrectAssignments(newExpression, assignmentGeneric);
						}

						newAssignments.Add(assignment.WithExpression(newExpression));
					}

					constructorExpression = constructorExpression.ReplaceAssignments(newAssignments);
				}

				if (constructorExpression.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
				{
					return Builder.TryConstruct(constructorExpression, this, ProjectFlags.Expression);
				}	

				/*
				if (constructorExpression.Parameters.Count > 0)
								{
					var newParameters = new List<SqlGenericConstructorExpression.Parameter>(constructorExpression.Parameters.Count);

					for (var i = 0; i < constructorExpression.Parameters.Count; i++)
					{
						var parameter = constructorExpression.Parameters[i];
						var paramKey = new SqlGenericParamAccessExpression(root, i, parameter.ParamType);

						/*
						if (parameter.Expression is SqlGenericConstructorExpression assignmentGeneric)
						{
							newExpression = CorrectAssignments(newExpression, assignmentGeneric);
								}

						newParameters.Add(parameter.WithExpression(newExpression));
					#1#
							}

					//constructorExpression.ReplaceParameters(newParameters);
						}
				*/

				return constructorExpression;
			}

			Expression MakeConditionalConstructExpression(Expression root, Expression leftExpression, Expression rightExpression)
			{
				if (leftExpression is not SqlGenericConstructorExpression)
				{
					if (leftExpression is SqlPlaceholderExpression placeholder && placeholder.Sql is SqlValue value && value.Value == null)
						leftExpression = Expression.Default(leftExpression.Type);
					else
						throw new InvalidOperationException();
				}

				if (rightExpression is not SqlGenericConstructorExpression)
				{
					if (rightExpression is SqlPlaceholderExpression placeholder && placeholder.Sql is SqlValue value && value.Value == null)
						rightExpression = Expression.Default(rightExpression.Type);
					else
						throw new InvalidOperationException();
				}

				var sequenceLeftSetId  = Builder.GenerateSetId(_sequence1.SubQuery.SelectQuery.SourceID);
				var sequenceRightSetId = Builder.GenerateSetId(_sequence2.SubQuery.SelectQuery.SourceID);

				if (_setIdPlaceholder == null)
				{

					var sqlValueLeft  = new SqlValue(sequenceLeftSetId);
					var sqlValueRight = new SqlValue(sequenceRightSetId);

					var thisRef  = new ContextRefExpression(_type, this);
					var leftRef  = new ContextRefExpression(_type, _sequence1);
					var rightRef = new ContextRefExpression(_type, _sequence2);

					var keyLeft  = Expression.Call(_keySetIdMethosInfo, leftRef, _setIdFieldName);
					var keyRight = Expression.Call(_keySetIdMethosInfo, rightRef, _setIdFieldName);

					var leftIdPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence1.SubQuery, sqlValueLeft,
						keyLeft, alias: ProjectionSetIdFieldName);
					leftIdPlaceholder = Builder.MakeColumn(_sequence1.SelectQuery, leftIdPlaceholder, asNew: true);
					leftIdPlaceholder = leftIdPlaceholder.WithPath(thisRef);
					leftIdPlaceholder = Builder.MakeColumn(SelectQuery, leftIdPlaceholder, asNew: true);

					var rightIdPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence2.SubQuery, sqlValueRight,
						keyRight, alias: ProjectionSetIdFieldName);
					rightIdPlaceholder = Builder.MakeColumn(_sequence2.SelectQuery, rightIdPlaceholder, asNew: true);
					rightIdPlaceholder = Builder.MakeColumn(SelectQuery, rightIdPlaceholder, asNew: true);

					_setIdPlaceholder = leftIdPlaceholder;
				}

				if (leftExpression is SqlGenericConstructorExpression genericLeft)
					leftExpression = CorrectAssignments(root, genericLeft);

				if (rightExpression is SqlGenericConstructorExpression genericRight)
					rightExpression = CorrectAssignments(root, genericRight);

				if (leftExpression.Type != root.Type)
				{
					leftExpression = Expression.Convert(leftExpression, root.Type);
				}

				if (rightExpression.Type != root.Type)
				{
					rightExpression = Expression.Convert(rightExpression, root.Type);
				}

				var resultExpr = Expression.Condition(
					Expression.Equal(_setIdPlaceholder, Expression.Constant(sequenceLeftSetId)),
					leftExpression,
					rightExpression
				);

				return resultExpr;
			}


			Expression MakeConditionalConstructExpression()
			{
				if (_type == null)
					throw new InvalidOperationException();

				return MakeConditionalConstructExpression(new ContextRefExpression(_type, this), 
					_leftSqlExpr,
					_rightSqlExpr);
			}

			void Init()
			{
				var ref1 = new ContextRefExpression(_type, _sequence1.SubQuery);
				var ref2 = new ContextRefExpression(_type, _sequence2.SubQuery);

				_leftSqlExpr  = SqlGenericConstructorExpression.Parse(Builder.ConvertToSqlExpr(_sequence1.SubQuery, ref1));
				_rightSqlExpr = SqlGenericConstructorExpression.Parse(Builder.ConvertToSqlExpr(_sequence2.SubQuery, ref2));

				var root = new ContextRefExpression(_type, this);

				List<Expression>? mismatches = null;

				_body = MatchSequences(root, _leftSqlExpr, _rightSqlExpr, _sequence1, _sequence2, ref mismatches);

				if (_body == null)
				{
					if (_leftSqlExpr is SqlPlaceholderExpression placeholderLeft && _rightSqlExpr is SqlPlaceholderExpression placeholderRight)
					{
						var leftColumn = Builder.MakeColumn(_sequence1.SelectQuery, placeholderLeft, asNew: true);

						leftColumn = Builder.MakeColumn(SelectQuery, leftColumn, asNew: true);
						leftColumn = leftColumn.WithPath(root);

						_createdSQL.Add(root, leftColumn);

						var rightColumn = Builder.MakeColumn(_sequence2.SelectQuery, placeholderRight, asNew: true);
						rightColumn = Builder.MakeColumn(SelectQuery, rightColumn, asNew: true);
					}
					else
					{
						throw new LinqException($"Set operation over {_leftSqlExpr} and {_rightSqlExpr} is not supported.");
					}
				}
				else
				{
					foreach (var matchedPair in _matchedPairs.OrderBy(x => x.Value.idx))
					{
						var alias = GenerateColumnAlias(matchedPair.Key);

						var leftExpression = Builder.ConvertToSqlExpr(_sequence1.SubQuery, matchedPair.Value.left.Expression);
						if (leftExpression is SqlPlaceholderExpression placeholderLeft)
						{
							if (alias != null)
								placeholderLeft.Alias = alias;

							var leftColumn = Builder.MakeColumn(_sequence1.SelectQuery, placeholderLeft, asNew: true);
						
							leftColumn = Builder.MakeColumn(SelectQuery, leftColumn, asNew: true);
							leftColumn = leftColumn.WithPath(matchedPair.Key);

							_createdSQL.Add(matchedPair.Key, leftColumn);

							var rightExpression = Builder.ConvertToSqlExpr(_sequence2.SubQuery, matchedPair.Value.right.Expression);
							if (rightExpression is not SqlPlaceholderExpression placeholderRight)
								throw new InvalidOperationException();

							if (alias != null)
								placeholderRight.Alias = alias;

							var rightColumn = Builder.MakeColumn(_sequence2.SelectQuery, placeholderRight, asNew: true);
							rightColumn = Builder.MakeColumn(SelectQuery, rightColumn, asNew: true);
						}
					}

					if (_matchedParamPairs != null)
					{
						foreach (var (key, left, right) in _matchedParamPairs)
						{
							var alias = GenerateColumnAlias(left);

							var leftExpression = Builder.ConvertToSqlExpr(_sequence1.SubQuery, left.Expression);
							if (leftExpression is SqlPlaceholderExpression placeholderLeft)
							{
								if (alias != null)
									placeholderLeft.Alias = alias;

								var leftColumn = Builder.MakeColumn(_sequence1.SelectQuery, placeholderLeft, asNew: true);

								leftColumn = Builder.MakeColumn(SelectQuery, leftColumn, asNew: true);
								leftColumn = leftColumn.WithPath(key);

								_createdSQL.Add(key, leftColumn);

								var rightExpression = Builder.ConvertToSqlExpr(_sequence2.SubQuery, right.Expression);
								if (rightExpression is not SqlPlaceholderExpression placeholderRight)
									throw new InvalidOperationException();

								if (alias != null)
									placeholderRight.Alias = alias;

								var rightColumn = Builder.MakeColumn(_sequence2.SelectQuery, placeholderRight, asNew: true);
								rightColumn = Builder.MakeColumn(SelectQuery, rightColumn, asNew: true);
							}
						}
					}
				}
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();

				/*var expr = Builder.FinalizeProjection(this,
					Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);*/
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

			static bool IsIncompatible(Expression expression)
			{
				var isIncompatible = null != expression.Find(expression, (_, e) =>
				{
					if (e.NodeType        == ExpressionType.Extension && e is SqlGenericConstructorExpression ctr &&
					    ctr.ConstructType == SqlGenericConstructorExpression.CreateType.Incompatible)
					{
						return true;
					}
					return false;
				});

				return isIncompatible;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) &&
				    (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)))
				{
					return path;
				}

				if (SequenceHelper.IsSameContext(path, this))
				{
					if (_body == null)
					{
						return _createdSQL.Values.Single();
					}

					if (flags.HasFlag(ProjectFlags.Expression))
					{
						if (IsIncompatible(_body))
						{
							if (_setOperation != SetOperation.UnionAll)
							{
								throw new LinqToDBException($"Could not decide which construction typer to use `query.Select(x => new {_body.Type.Name} {{ ... }})` to specify projection.");
							}
							else
							{
								return MakeConditionalConstructExpression();
							}
						}

						var constructed = Builder.TryConstruct(_body, this, flags);

						return constructed;
					}

					return _body;
				}

				if (flags.HasFlag(ProjectFlags.Root))
					return path;
				
				if (_body != null)
				{
					var projected = Builder.Project(this, path, null, -1, flags, _body);

					if (flags.HasFlag(ProjectFlags.Expression))
					{
						if (IsIncompatible(projected))
						{
							var leftExpression  = Builder.Project(this, path, null, -1, flags, _leftSqlExpr);
							var rightExpression = Builder.Project(this, path, null, -1, flags, _rightSqlExpr);

							var result = MakeConditionalConstructExpression(path, leftExpression, rightExpression);
							return result;
						}
					}

					if (projected is MemberExpression || projected is SqlGenericParamAccessExpression)
					{
						if (_createdSQL.TryGetValue(projected, out var placeholderExpression))
							return placeholderExpression;
						return ExpressionBuilder.CreateSqlError(this, projected);
					}

					return projected;
				}


				return base.MakeExpression(path, flags);
			}
		}

		#endregion
	}
}
