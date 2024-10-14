using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common.Internal;
	using Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;

	[BuildsMethodCall("Concat", "UnionAll", "Union", "Except", "Intersect", "ExceptAll", "IntersectAll")]
	internal sealed class SetOperationBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.Arguments.Count == 2 && call.IsQueryable();

		#region Builder

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult1 = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (buildResult1.BuildContext == null)
				return buildResult1;

			var buildResult2 = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
			if (buildResult2.BuildContext == null)
				return buildResult2;

			var sequence1 = buildResult1.BuildContext;
			var sequence2 = buildResult2.BuildContext;

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

			var elementType = methodCall.Method.GetGenericArguments()[0];

			var needsEmulation = !builder.DataContext.SqlProviderFlags.IsAllSetOperationsSupported &&
			                     (setOperation is SetOperation.ExceptAll or SetOperation.IntersectAll)
			                     ||
			                     !builder.DataContext.SqlProviderFlags.IsDistinctSetOperationsSupported &&
			                     (setOperation is SetOperation.Except or SetOperation.Intersect);

			var set1 = new SubQueryContext(sequence1);
			var set2 = new SubQueryContext(sequence2);

			var setOperator = new SqlSetOperator(set2.SelectQuery, setOperation);

			set1.SelectQuery.SetOperators.Add(setOperator);

			var setContext = new SetOperationContext(setOperation, new SelectQuery(), set1, set2, methodCall);

			if (setOperation != SetOperation.UnionAll)
			{
				var sqlExpr = builder.BuildSqlExpression(setContext, new ContextRefExpression(elementType, setContext));
			}

			if (needsEmulation)
			{
				return BuildSequenceResult.FromContext(setContext.Emulate());
			}

			return BuildSequenceResult.FromContext(setContext);
		}

		#endregion

		#region Context

		internal sealed class SetOperationContext : SubQueryContext
		{
			public SetOperationContext(SetOperation setOperation, SelectQuery selectQuery, SubQueryContext sequence1, SubQueryContext sequence2,
				MethodCallExpression                methodCall)
				: base(sequence1, selectQuery, true)
			{
				_setOperation = setOperation;
				_sequence1    = sequence1;
				_sequence2    = sequence2;
				_methodCall   = methodCall;

				_sequence2.Parent = this;

				_type = _methodCall.Method.GetGenericArguments()[0];
			}

			readonly Type                 _type;
			readonly MethodCallExpression _methodCall;
			readonly SetOperation         _setOperation;
			readonly SubQueryContext      _sequence1;
			readonly SubQueryContext      _sequence2;
			SqlPlaceholderExpression?     _setIdPlaceholder;
			Expression?                   _setIdReference;

			int?                          _leftSetId;
			int?                          _rightSetId;

			Expression _projection1 = default!;
			Expression _projection2 = default!;

			Expression? _leftSetPredicate;
			Expression? _rightSetPredicate;

			Dictionary<Expression[], (SqlPlaceholderExpression placeholder1, SqlPlaceholderExpression placeholder2)> _pathMapping = default!;

			public override bool NeedsSubqueryForComparison => true;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() || flags.IsTraverse() || flags.IsAggregationRoot() || flags.IsAssociationRoot())
					return path;

				if (_setIdReference != null && ExpressionEqualityComparer.Instance.Equals(_setIdReference, path))
				{
					return _setIdPlaceholder!;
				}

				if (ReferenceEquals(_pathMapping, null))
				{
					InitializeProjections();
				}

				Expression projection1;
				Expression projection2;

				if (SequenceHelper.IsSameContext(path, this))
				{
					projection1 = _projection1;
					projection2 = _projection2;
				}
				else
				{
					projection1 = Builder.Project(this, path, null, 0, flags, _projection1, true);
					projection2 = Builder.Project(this, path, null, 0, flags, _projection2, true);

					if (projection1 is SqlErrorExpression)
					{
						if (Builder.HandleAlias(this, path, flags, out var newResult))
							return newResult;

						if (flags.IsKeys() && projection2 is not SqlErrorExpression)
						{
							return RemapPathToPlaceholders(projection2, _pathMapping!);
						}

						projection1 = Builder.Project(this, path, null, 0, flags, _projection1, false);
					}

					if (projection2 is SqlErrorExpression)
					{
						if (Builder.HandleAlias(this, path, flags, out var newResult))
							return newResult;

						if (flags.IsKeys() && projection1 is not SqlErrorExpression)
						{
							return RemapPathToPlaceholders(projection1, _pathMapping!);
						}

						projection2 = Builder.Project(this, path, null, 0, flags, _projection2, false);
					}

					// for Expression we can allow non translatable errors
					if (flags.IsExpression())
					{
						if (projection1 is SqlErrorExpression)
							projection1 = Expression.Default(path.Type);
						if (projection2 is SqlErrorExpression)
							projection2 = Expression.Default(path.Type);
					}

					if (projection1 is SqlPathExpression && projection2 is SqlErrorExpression)
					{
						projection2 = projection1;
					}
					else if (projection2 is SqlPathExpression && projection1 is SqlErrorExpression)
					{
						projection1 = projection2;
					}

					if (projection1 is SqlErrorExpression || projection2 is SqlErrorExpression)
					{
						return ExpressionBuilder.CreateSqlError(this, path);
					}
				}

				var projection = MergeProjections(path, projection1, projection2, flags);

				// remap fields
				var result = RemapPathToPlaceholders(projection, _pathMapping!);

				return result;
			}

			static Expression RemapPathToPlaceholders(Expression expression,
				Dictionary<Expression[], (SqlPlaceholderExpression placeholder1, SqlPlaceholderExpression placeholder2)> pathMapping)
			{
				var result = expression.Transform(e =>
				{
					if (e is SqlPathExpression pathExpression)
					{
						if (!pathMapping!.TryGetValue(pathExpression.Path, out var pair))
						{
							throw new InvalidOperationException("Could not find required path for projection.");
						}

						Expression result = pair.placeholder1;

						if (result.Type != e.Type)
						{
							result = Expression.Convert(result, e.Type);
						}

						return result;
					}

					if (e is SqlEagerLoadExpression eager)
					{
						return eager.Update(RemapPathToPlaceholders(eager.SequenceExpression, pathMapping), eager.Predicate);
					}

					return e;
				});

				return result;
			}

			static Expression MakeCondition(Expression test, Expression ifTrue, Expression ifFalse)
			{
				if (ifTrue.Type != ifFalse.Type)
				{
					if (ifTrue.Type.IsAssignableFrom(ifFalse.Type))
						ifFalse = Expression.Convert(ifFalse, ifTrue.Type);
					else if (ifFalse.Type.IsAssignableFrom(ifTrue.Type))
						ifTrue = Expression.Convert(ifTrue, ifFalse.Type);
					else 
						ifFalse = Expression.Convert(ifFalse, ifTrue.Type);
				}

				return Expression.Condition(test, ifTrue, ifFalse);
			}

			bool TryMergeViaDifferencePredicate(Expression projection1, Expression projection2, [NotNullWhen(true)] out Expression? merged)
			{
				var testExpr = GetLeftSetPredicate();

				if (testExpr != null)
				{
					merged = MakeCondition(testExpr, projection1, projection2);
					return true;
				}

				merged = null;
				return false;
			}

			Expression MergeProjections(Expression path, Expression projection1, Expression projection2, ProjectFlags flags)
			{
				if (TryMergeProjections(projection1, projection2, flags, out var merged))
					return merged;

				if (TryMergeViaDifferencePredicate(projection1, projection2, out merged))
					return merged;

				if (_setOperation != SetOperation.UnionAll)
				{
					throw new LinqToDBException(
						$"Could not decide which construction type to use `query.Select(x => new {projection1.Type.Name} {{ ... }})` to specify projection.");
				}

				EnsureSetIdFieldCreated();

				var sequenceLeftSetId = _leftSetId!.Value;

				if (projection1.Type != path.Type)
				{
					projection1 = Expression.Convert(projection1, path.Type);
				}

				if (projection2.Type != path.Type)
				{
					projection2 = Expression.Convert(projection2, path.Type);
				}

				var resultExpr = Expression.Condition(
					Expression.Equal(_setIdReference!, Expression.Constant(sequenceLeftSetId)),
					projection1,
					projection2
				);

				return resultExpr;
			}

			bool IsNullValueOrSqlNull(Expression expression)
			{
				if (expression.IsNullValue())
					return true;

				if (expression is SqlPlaceholderExpression placeholder)
					return QueryHelper.IsNullValue(placeholder.Sql);

				return false;
			}

			Expression? GetDifferencePredicateConstants(bool isLeft)
			{
				foreach (var map in _pathMapping)
				{
					var (placeholder1, placeholder2) = map.Value;
					if (placeholder1.Sql is SqlColumn column1 && placeholder2.Sql is SqlColumn column2)
					{
						if (column1.Expression is SqlColumn { Expression: SqlValue sqlValue1 } && column2.Expression is SqlColumn { Expression: SqlValue sqlValue2 })
						{
							if (!Equals(sqlValue1.Value, sqlValue2.Value))
							{
								if (isLeft)
									return Expression.Equal(new SqlPathExpression(map.Key, placeholder1.Type),
										Expression.Constant(sqlValue1.Value));

								return Expression.Equal(new SqlPathExpression(map.Key, placeholder2.Type),
									Expression.Constant(sqlValue2.Value));
							}
						}
					}
				}

				return null;
			}

			static Expression MakeNullCondition(SqlPathExpression path, bool isNotNull)
			{
				if (!path.Type.IsNullableType())
				{
					path = path.WithType(path.Type.AsNullable());
				}

				if (isNotNull)
					return Expression.NotEqual(path, Expression.Default(path.Type));

				return Expression.Equal(path, Expression.Default(path.Type));
			}

			Expression? GetDifferencePredicateViaNotNullable(bool isLeft)
			{
				var nullability1 = NullabilityContext.GetContext(_sequence1.SelectQuery);
				var nullability2 = NullabilityContext.GetContext(_sequence2.SelectQuery);

				foreach (var map in _pathMapping)
				{
					var (placeholder1, placeholder2) = map.Value;
					if (placeholder1.Sql is SqlColumn column1 && placeholder2.Sql is SqlColumn column2)
					{
						if (!column1.Expression.CanBeNullable(nullability1))
						{
							if (QueryHelper.IsNullValue(column2.Expression))
							{
								return MakeNullCondition(new SqlPathExpression(map.Key, placeholder1.Type), isLeft == true);
							}

						}
						else if (!column2.Expression.CanBeNullable(nullability2))
						{
							if (QueryHelper.IsNullValue(column1.Expression))
								return MakeNullCondition(new SqlPathExpression(map.Key, placeholder2.Type), isLeft == false);
						}
					}
				}

				return null;
			}

			Expression? GetLeftSetPredicate()
			{
				return _leftSetPredicate ??= GetDifferencePredicate(true);
			}

			Expression? GetRightSetPredicate()
			{
				return _rightSetPredicate ??= GetDifferencePredicate(false);
			}

			Expression? GetDifferencePredicate(bool isLeft)
			{
				var predicate = GetDifferencePredicateConstants(isLeft) ?? GetDifferencePredicateViaNotNullable(isLeft);
				if (predicate != null)
				{
					predicate = RemapPathToPlaceholders(predicate, _pathMapping);
					return predicate;
				}

				// Last chance to add anchor field __projection__set_id__
				//
				if (_setOperation == SetOperation.UnionAll)
				{
					EnsureSetIdFieldCreated();

					var sequenceSetId = isLeft ? _leftSetId!.Value : _rightSetId!.Value;

					return Expression.Equal(_setIdReference!, Expression.Constant(sequenceSetId));
				}

				return null;
			}

			bool TryMergeProjections(Expression projection1, Expression projection2, ProjectFlags flags, [NotNullWhen(true)] out Expression? merged)
			{
				merged = null;

				if (projection1.Type != projection2.Type)
				{
					if (projection1.Type.UnwrapNullableType() != projection2.Type.UnwrapNullableType())
						return false;
				}

				if (ExpressionEqualityComparer.Instance.Equals(projection1, projection2))
				{
					merged = projection1;
					return true;
				}

				if (SequenceHelper.UnwrapDefaultIfEmpty(projection1) is SqlGenericConstructorExpression generic1 &&
				    SequenceHelper.UnwrapDefaultIfEmpty(projection2) is SqlGenericConstructorExpression generic2)
				{
					if (generic1.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
					{
						if (generic2.ConstructType != SqlGenericConstructorExpression.CreateType.Full)
						{
							var constructed = Builder.TryConstruct(MappingSchema, generic1, flags);
							if (constructed == null)
								return false;
							if (TryMergeProjections(Builder.ParseGenericConstructor(constructed, flags, null), generic2, flags, out merged))
								return true;
							return false;
						}
					}

					if (generic2.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
					{
						if (generic1.ConstructType != SqlGenericConstructorExpression.CreateType.Full)
						{
							var constructed = Builder.TryConstruct(MappingSchema, generic2, flags);
							if (constructed == null)
								return false;
							if (TryMergeProjections(generic1, Builder.ParseGenericConstructor(constructed, flags, null), flags, out merged))
								return true;
							return false;
						}
					}

					var resultAssignments = new List<SqlGenericConstructorExpression.Assignment>(generic1.Assignments.Count);

					foreach (var a1 in generic1.Assignments)
					{
						var found = generic2.Assignments.FirstOrDefault(a2 =>
							MemberInfoComparer.Instance.Equals(a2.MemberInfo, a1.MemberInfo));

						if (found == null)
						{
							if (a1.Expression is not SqlPathExpression)
								return false;
							resultAssignments.Add(a1);
						}
						else if (!TryMergeProjections(a1.Expression, found.Expression, flags, out var mergedAssignment))
							return false;
						else
							resultAssignments.Add(a1.WithExpression(mergedAssignment));
					}

					foreach (var a2 in generic2.Assignments)
					{
						var found = generic1.Assignments.FirstOrDefault(a1 =>
							MemberInfoComparer.Instance.Equals(a2.MemberInfo, a1.MemberInfo));

						if (found == null)
						{
							if (a2.Expression is not SqlPathExpression)
								return false;
							resultAssignments.Add(a2);
						}
					}

					if (generic1.Parameters.Count != generic2.Parameters.Count)
					{
						return false;
					}

					var resultGeneric = generic1.ReplaceAssignments(resultAssignments);

					if (generic1.Parameters.Count > 0)
					{
						var resultParameters = new List<SqlGenericConstructorExpression.Parameter>(generic1.Parameters.Count);

						for (int i = 0; i < generic1.Parameters.Count; i++)
						{
							if (!TryMergeProjections(generic1.Parameters[i].Expression,
								    generic2.Parameters[i].Expression, flags, out var mergedAssignment))
								return false;

							resultParameters.Add(generic1.Parameters[i].WithExpression(mergedAssignment));
						}

						resultGeneric = resultGeneric.ReplaceParameters(resultParameters);
					}

					if (Builder.TryConstruct(MappingSchema, resultGeneric, flags) == null)
						return false;

					merged = resultGeneric;
					return true;
				}

				if (projection1 is ConditionalExpression cond1 && projection2 is ConditionalExpression cond2)
				{
					if (!ExpressionEqualityComparer.Instance.Equals(cond1.Test, cond2.Test))
						return false;

					if (!TryMergeProjections(cond1.IfTrue, cond2.IfTrue, flags, out var ifTrueMerged) ||
					    !TryMergeProjections(cond1.IfFalse, cond2.IfFalse, flags, out var ifFalseMerged))
					{
						return false;
					}

					merged = cond1.Update(cond1.Test, ifTrueMerged, ifFalseMerged);
					return true;
				}

				if (projection1 is SqlPathExpression && IsNullValueOrSqlNull(projection2))
				{
					merged = projection1;
					return true;
				}

				if (projection2 is SqlPathExpression && IsNullValueOrSqlNull(projection1))
				{
					merged = projection2;
					return true;
				}

				if (projection1.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					if (TryMergeProjections(((UnaryExpression)projection1).Operand, projection2, flags, out merged))
					{
						if (merged.Type != projection1.Type)
						{
							merged = Expression.Convert(merged, projection1.Type);
						}

						return true;
					}
				}

				if (projection2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					if (TryMergeProjections(projection1, ((UnaryExpression)projection2).Operand, flags, out merged))
					{
						if (merged.Type != projection2.Type)
						{
							merged = Expression.Convert(merged, projection2.Type);
						}

						return true;
					}
				}

				if (TryMergeViaDifferencePredicate(projection1, projection2, out merged))
					return true;

				return false;
			}

			class PathComparer : IEqualityComparer<Expression[]>
			{
				public static readonly PathComparer Instance = new PathComparer();

				public bool Equals(Expression[]? x, Expression[]? y)
				{
					if (ReferenceEquals(x, y))
						return true;

					if (x == null || y == null)
						return false;

					return x.SequenceEqual(y, ExpressionEqualityComparer.Instance);
				}

				public int GetHashCode(Expression[] obj)
				{
					return obj.Aggregate(0, (acc, val) => acc ^ ExpressionEqualityComparer.Instance.GetHashCode(val!));
				}
			}

			static bool ExtractValue(SqlPlaceholderExpression placeholder, [NotNullWhen(true)] out SqlValue? sqlValue)
			{
				if (placeholder.Sql is SqlColumn column && QueryHelper.UnwrapNullablity(column.Expression) is SqlValue sqlValue1)
				{
					sqlValue = sqlValue1;
					return true;
				}

				sqlValue = null;
				return false;
			}

			void InitializeProjections()
			{
				var ref1 = new ContextRefExpression(ElementType, _sequence1);

				_projection1 = BuildProjectionExpression(ref1, _sequence1, out var placeholders1, out var eager1);

				var ref2 = new ContextRefExpression(ElementType, _sequence2);

				_projection2 = BuildProjectionExpression(ref2, _sequence2, out var placeholders2, out var eager2);

				var pathMapping = new Dictionary<Expression[], (SqlPlaceholderExpression placeholder1, SqlPlaceholderExpression placeholder2)>(PathComparer.Instance);

				switch (_setOperation)
				{
					case SetOperation.Union:
						break;
					case SetOperation.UnionAll:
						break;
					case SetOperation.Except:
					case SetOperation.ExceptAll:
						_projection2 = _projection1;
						eager2       = eager1;
						break;
					case SetOperation.Intersect:
						break;
					case SetOperation.IntersectAll:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				foreach (var (placeholder, path) in placeholders1)
				{
					var placeholderPath = new SqlPathExpression(path, placeholder.Type);
					var alias           = GenerateColumnAlias(placeholderPath);

					var placeholder1 = placeholder;

					var (placeholder2, _) = placeholders2.FirstOrDefault(p2 => PathComparer.Instance.Equals(p2.path, path));
					if (placeholder2 == null)
					{
						placeholder2 = ExpressionBuilder.CreatePlaceholder(_sequence2,
							new SqlValue(QueryHelper.GetDbDataType(placeholder.Sql, MappingSchema), null), placeholderPath);
					}
					else
					{
						if (ExtractValue(placeholder1, out var sqlValue1) && !ExtractValue(placeholder2, out _))
							sqlValue1.ValueType = QueryHelper.GetDbDataType(placeholder2.Sql, MappingSchema);
						else
						{
							if (ExtractValue(placeholder2, out var sqlValue2) && !ExtractValue(placeholder1, out _))
								sqlValue2.ValueType = QueryHelper.GetDbDataType(placeholder1.Sql, MappingSchema);
						}

						placeholder2 = Builder.UpdateNesting(_sequence2, placeholder2);
						placeholder2 = (SqlPlaceholderExpression)SequenceHelper.CorrectSelectQuery(placeholder2, _sequence2.SelectQuery);
					}

					placeholder1 = Builder.UpdateNesting(_sequence1, placeholder1);
					placeholder1 = (SqlPlaceholderExpression)SequenceHelper.CorrectSelectQuery(placeholder1, _sequence1.SelectQuery);
					placeholder1 = placeholder1.WithPath(placeholderPath).WithAlias(alias);

					var column1 = Builder.MakeColumn(SelectQuery, placeholder1.WithAlias(alias), true);

					placeholder2 = placeholder2.WithPath(placeholderPath).WithAlias(alias);
					var column2 = Builder.MakeColumn(SelectQuery, placeholder2.WithAlias(alias), true);

					pathMapping.Add(path, (column1, column2));
				}

				if (_setOperation != SetOperation.Except && _setOperation != SetOperation.ExceptAll)
				{
					foreach (var (placeholder, path) in placeholders2)
					{
						if (pathMapping.ContainsKey(path))
							continue;

						var placeholder2 = Builder.UpdateNesting(_sequence2, placeholder);
						placeholder2 = (SqlPlaceholderExpression)SequenceHelper.CorrectSelectQuery(placeholder2, _sequence2.SelectQuery);

						var placeholderPath = new SqlPathExpression(path, placeholder2.Type);
						var alias           = GenerateColumnAlias(placeholderPath);

						placeholder2 = placeholder2.WithAlias(alias);

						var column1 = ExpressionBuilder.CreatePlaceholder(_sequence1, new SqlValue(QueryHelper.GetDbDataType(placeholder2.Sql, MappingSchema), null), placeholderPath);
						column1 = Builder.MakeColumn(SelectQuery, column1, true);

						var column2 = Builder.MakeColumn(SelectQuery, placeholder2, true);

						pathMapping.Add(path, (column1, column2));
					}
				}

				_pathMapping = pathMapping;

				Dictionary<SqlEagerLoadExpression, SqlEagerLoadExpression>? eagerMapping = null;
				foreach (var e1 in eager1)
				{
					if (eagerMapping?.ContainsKey(e1) == true)
						continue;

					var found = eager2.FirstOrDefault(e2 => ExpressionEqualityComparer.Instance.Equals(e2, e1));

					eagerMapping ??= new(ExpressionEqualityComparer.Instance);

					if (found != null)
					{
						eagerMapping.Add(e1, e1);
					}
					else
					{
						var predicate = GetLeftSetPredicate();
						if (predicate == null)
							throw new InvalidOperationException("No way to distinguish difference between tho different sets.");
						eagerMapping.Add(e1, e1.AppendPredicate(predicate));
					}
				}

				foreach (var e2 in eager2)
				{
					if (eagerMapping?.ContainsKey(e2) == true)
						continue;

					eagerMapping ??= new(ExpressionEqualityComparer.Instance);

					var predicate = GetRightSetPredicate();
					if (predicate == null)
						throw new InvalidOperationException("No way to distinguish difference between tho different sets.");

					eagerMapping.Add(e2, e2.AppendPredicate(predicate));
				}

				if (eagerMapping != null)
				{
					_projection1 = ReplaceEagerExpressions(_projection1, eagerMapping);
					_projection2 = ReplaceEagerExpressions(_projection2, eagerMapping);
				}
			}

			static Expression ReplaceEagerExpressions(Expression expression, Dictionary<SqlEagerLoadExpression, SqlEagerLoadExpression> raplacements)
			{
				var result = expression.Transform(e =>
				{
					if (e is SqlEagerLoadExpression eager)
					{
						if (raplacements.TryGetValue(eager, out var newEager))
							return newEager;
					}

					return e;
				});

				return result;
			}

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

			const string ProjectionSetIdFieldName = "__projection__set_id__";

			Expression GetSetIdReference()
			{
				if (_setIdReference == null)
				{
					var thisRef = new ContextRefExpression(_type, this);
					_setIdReference = SequenceHelper.CreateSpecialProperty(thisRef, typeof(int), ProjectionSetIdFieldName);

					_leftSetId  = Builder.GenerateSetId(_sequence1.SubQuery.SelectQuery.SourceID);
					_rightSetId = Builder.GenerateSetId(_sequence2.SubQuery.SelectQuery.SourceID);
				}

				return _setIdReference;
			}

			void EnsureSetIdFieldCreated()
			{
				if (_setIdPlaceholder != null)
					return;

				var setIdReference = GetSetIdReference();

				var intDataType   = MappingSchema.GetDbDataType(typeof(int));
				var sqlValueLeft  = new SqlValue(intDataType,_leftSetId!);
				var sqlValueRight = new SqlValue(intDataType, _rightSetId!);

				var leftRef  = new ContextRefExpression(_type, _sequence1);
				var rightRef = new ContextRefExpression(_type, _sequence2);

				var keyLeft  = SequenceHelper.CreateSpecialProperty(leftRef, typeof(int), ProjectionSetIdFieldName);
				var keyRight = SequenceHelper.CreateSpecialProperty(rightRef, typeof(int), ProjectionSetIdFieldName);

				var leftIdPlaceholder =
					ExpressionBuilder.CreatePlaceholder(_sequence1, sqlValueLeft, keyLeft, alias : ProjectionSetIdFieldName);
				leftIdPlaceholder = (SqlPlaceholderExpression)Builder.UpdateNesting(this, leftIdPlaceholder);

				var rightIdPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence2, sqlValueRight,
					keyRight, alias : ProjectionSetIdFieldName);
				rightIdPlaceholder = Builder.MakeColumn(SelectQuery, rightIdPlaceholder, asNew : true);

				_setIdPlaceholder = leftIdPlaceholder.WithPath(setIdReference).WithTrackingPath(setIdReference);
			}

			class ExpressionOptimizerVisitor : ExpressionVisitorBase
			{
				protected override Expression VisitConditional(ConditionalExpression node)
				{
					var newNode = base.VisitConditional(node);
					if (!ReferenceEquals(newNode, node))
						return Visit(newNode);

					if (node.IfTrue is ConditionalExpression condTrue                        &&
					    ExpressionEqualityComparer.Instance.Equals(node.Test, condTrue.Test) &&
					    ExpressionEqualityComparer.Instance.Equals(node.IfFalse, condTrue.IfFalse))
					{
						return condTrue;
					}

					return node;
				}
			}

			class ExpressionPathVisitor : ExpressionVisitorBase
			{
				Stack<Expression> _stack = new();

				bool _isDictionary;

				public List<(SqlPlaceholderExpression placeholder, Expression[] path)> FoundPlaceholders { get; } = new();

				public List<SqlEagerLoadExpression> FoundEager { get; } = new();

				protected override Expression VisitConditional(ConditionalExpression node)
				{
					_stack.Push(Expression.Constant("?"));
					_stack.Push(Visit(node.Test));

					_stack.Push(Expression.Constant(true));
					var ifTrue = Visit(node.IfTrue);
					_stack.Pop();

					_stack.Push(Expression.Constant(false));
					var ifFalse = Visit(node.IfFalse);
					_stack.Pop();

					var test = _stack.Pop();
					_stack.Pop();

					return node.Update(test, ifTrue, ifFalse);
				}

				protected override Expression VisitBinary(BinaryExpression node)
				{
					_stack.Push(Expression.Constant("binary"));
					_stack.Push(Expression.Constant(node.NodeType));
					_stack.Push(Visit(node.Left));
					_stack.Push(Visit(node.Right));

					var right = _stack.Pop();
					var left  = _stack.Pop();

					_stack.Pop();
					_stack.Pop();

					return node.Update(left, node.Conversion, right);
				}

				public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
				{
					var stack = _stack.ToArray();
					Array.Reverse(stack);

					FoundPlaceholders.Add((node, stack));

					return new SqlPathExpression(stack, node.Type);
				}

				public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
				{
					_stack.Push(Expression.Constant("construct"));
					_stack.Push(Expression.Constant(node.Type));

					if (node.Assignments.Count > 0)
					{
						var newAssignments = new List<SqlGenericConstructorExpression.Assignment>(node.Assignments.Count);

						foreach(var a in node.Assignments)
						{
							var memberInfo = a.MemberInfo.DeclaringType?.GetMemberEx(a.MemberInfo) ?? a.MemberInfo;

							var assignmentExpression = a.Expression;

							_stack.Push(Expression.Constant(memberInfo));
							newAssignments.Add(a.WithExpression(Visit(assignmentExpression)));
							_stack.Pop();
						}

						node = node.ReplaceAssignments(newAssignments);
					}

					if (node.Parameters.Count > 0)
					{
						var newParameters = new List<SqlGenericConstructorExpression.Parameter>(node.Parameters.Count);
						for (var index = 0; index < node.Parameters.Count; index++)
						{
							var param = node.Parameters[index];

							var paramExpression = param.Expression;

							if (param.MemberInfo != null)
							{
								// mimic assignment
								var memberInfo = param.MemberInfo.DeclaringType?.GetMemberEx(param.MemberInfo) ?? param.MemberInfo;

								_stack.Push(Expression.Constant(memberInfo));
								newParameters.Add(param.WithExpression(Visit(paramExpression)));
								_stack.Pop();
							}
							else
							{
								_stack.Push(Expression.Constant(index));
								newParameters.Add(param.WithExpression(Visit(paramExpression)));
								_stack.Pop();
							}
						}

						node = node.ReplaceParameters(newParameters);
					}

					_stack.Pop();
					_stack.Pop();

					return node;
				}

				public override Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
				{
					_stack.Push(Expression.Constant("default_if_empty"));

					var newNode = base.VisitSqlDefaultIfEmptyExpression(node);

					_stack.Pop();

					return newNode;
				}

				protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
				{
					_stack.Push(Expression.Constant(node.BindingType));
					_stack.Push(Expression.Constant(node.Member));

					var newNode = base.VisitMemberAssignment(node);

					_stack.Pop();
					_stack.Pop();

					return newNode;
				}

				protected override MemberBinding VisitMemberBinding(MemberBinding node)
				{
					_stack.Push(Expression.Constant(node.BindingType));
					_stack.Push(Expression.Constant(node.Member));

					var newNode = base.VisitMemberBinding(node);

					_stack.Pop();
					_stack.Pop();

					return newNode;
				}

				protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
				{
					_stack.Push(Expression.Constant(node.BindingType));
					_stack.Push(Expression.Constant(node.Member));

					var newNode = base.VisitMemberListBinding(node);

					_stack.Pop();
					_stack.Pop();

					return newNode;
				}

				protected override Expression VisitMemberInit(MemberInitExpression node)
				{
					_stack.Push(Expression.Constant("init"));
					_stack.Push(Expression.Constant(node.NewExpression.Constructor));

					var newExpr = base.VisitMemberInit(node);

					_stack.Pop();
					_stack.Pop();

					return newExpr;
				}

				protected override ElementInit VisitElementInit(ElementInit node)
				{
					_stack.Push(Expression.Constant(node.AddMethod));

					var arguments = new List<Expression>(node.Arguments.Count);

					for (int i = 0; i < node.Arguments.Count; i++)
					{
						_stack.Push(Expression.Constant(i));

						var nodeArgument = node.Arguments[i];

						var arg = Visit(nodeArgument);

						_stack.Pop();

						arguments.Add(arg);
					}

					var newNode = node.Update(arguments);

					_stack.Pop();

					return newNode;
				}

				protected override Expression VisitListInit(ListInitExpression node)
				{
					_stack.Push(Expression.Constant("list init"));

					var initializers = new List<ElementInit>(node.Initializers.Count);

					var saveIDictionary = _isDictionary;
					_isDictionary = typeof(IDictionary<,>).IsSameOrParentOf(node.Type);

					for (int i = 0; i < node.Initializers.Count; i++)
					{
						_stack.Push(Expression.Constant(i));
						initializers.Add(VisitElementInit(node.Initializers[i]));
						_stack.Pop();
					}

					_isDictionary = saveIDictionary;

					var newExpr = node.Update((NewExpression)Visit(node.NewExpression), initializers);

					_stack.Pop();

					return newExpr;
				}

				protected override Expression VisitNew(NewExpression node)
				{
					_stack.Push(Expression.Constant("new"));
					_stack.Push(Expression.Constant(node.Constructor));

					var args = new List<Expression>(node.Arguments.Count);

					for (int i = 0; i < node.Arguments.Count; i++)
					{
						_stack.Push(Expression.Constant(i));
						args.Add(Visit(node.Arguments[i]));
						_stack.Pop();
					}

					var newNode = node.Update(args);

					_stack.Pop();
					_stack.Pop();

					return newNode;
				}

				protected override Expression VisitNewArray(NewArrayExpression node)
				{
					_stack.Push(Expression.Constant("new array"));
					_stack.Push(Expression.Constant(node.Type));

					var args = new List<Expression>(node.Expressions.Count);

					for (int i = 0; i < node.Expressions.Count; i++)
					{
						_stack.Push(Expression.Constant(i));
						args.Add(Visit(node.Expressions[i]));
						_stack.Pop();
					}

					var newNode = node.Update(args);

					_stack.Pop();
					_stack.Pop();

					return newNode;
				}

				protected override Expression VisitMethodCall(MethodCallExpression node)
				{
					_stack.Push(Expression.Constant("call"));

					var obj = Visit(node.Object);
					if (obj != null)
						_stack.Push(Expression.Constant(obj));

					_stack.Push(Expression.Constant(node.Method));

					var args = new List<Expression>(node.Arguments.Count);

					for (var index = 0; index < node.Arguments.Count; index++)
					{
						var arg = node.Arguments[index];
						_stack.Push(Expression.Constant(index));
						args.Add(Visit(arg));
						_stack.Pop();
					}

					_stack.Pop();

					if (obj != null)
						_stack.Pop();

					_stack.Pop();

					return node.Update(obj, args);
				}

				internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
				{
					var saveStack = _stack;
					_stack = new();

					var newEager  = node.Update(Visit(node.SequenceExpression), Visit(node.Predicate));

					_stack = saveStack;

					FoundEager.Add(newEager);

					return newEager;
				}
			}

			Expression BuildProjectionExpression(Expression path, IBuildContext context,
				out List<(SqlPlaceholderExpression placeholder, Expression[] path)> foundPlaceholders,
				out List<SqlEagerLoadExpression> foundEager)
			{
				var correctedPath = SequenceHelper.ReplaceContext(path, this, context);

				var current = correctedPath;
				do
				{
					var projected = Builder.BuildSqlExpression(context, current, buildPurpose: BuildPurpose.Expression, buildFlags: BuildFlags.ForceDefaultIfEmpty | BuildFlags.ForSetProjection | BuildFlags.ResetPrevious);

					projected = Builder.BuildExtractExpression(context, projected);

					var lambdaResolver = new LambdaResolveVisitor(context, BuildPurpose.Sql, true);
					projected = lambdaResolver.Visit(projected);

					var optimizer = new ExpressionOptimizerVisitor();
					projected = optimizer.Visit(projected);

					if (ExpressionEqualityComparer.Instance.Equals(projected, current))
						break;

					current = projected;
				} while (true);

				var pathBuilder = new ExpressionPathVisitor();
				var withPath    = pathBuilder.Visit(current);

				foundPlaceholders = pathBuilder.FoundPlaceholders;

				foundEager = pathBuilder.FoundEager;

				return withPath;
			}

			// For Set we have to ensure hat columns are not optimized
			protected override bool OptimizeColumns => false;

			public override IBuildContext Clone(CloningContext context)
			{
				var cloned = new SetOperationContext(_setOperation, context.CloneElement(SelectQuery),
					context.CloneContext(_sequence1), context.CloneContext(_sequence2),
					context.CloneExpression(_methodCall));

				// for correct updating self-references below
				context.RegisterCloned(this, cloned);

				cloned._setIdPlaceholder  = context.CloneExpression(_setIdPlaceholder);
				cloned._setIdReference    = context.CloneExpression(_setIdReference);
				cloned._leftSetId         = _leftSetId;
				cloned._rightSetId        = _rightSetId;
				cloned._leftSetPredicate  = context.CloneExpression(_leftSetPredicate);
				cloned._rightSetPredicate = context.CloneExpression(_rightSetPredicate);

				return cloned;
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return this;
			}

			public IBuildContext Emulate()
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				if (_projection1 == null)
					InitializeProjections();

				var sequence = _sequence1;
				var query    = _sequence2;
				var except   = query.SelectQuery;

				var sql = sequence.SelectQuery;

				if (_setOperation is SetOperation.Except or SetOperation.Intersect)
					sql.Select.IsDistinct = true;

				sql.Where.EnsureConjunction();

				if (_setOperation is SetOperation.Except or SetOperation.ExceptAll)
					sql.Where.SearchCondition.AddNotExists(except);
				else
					sql.Where.SearchCondition.AddExists(except);

				var sc = new SqlSearchCondition();

				for (int i = 0; i < _sequence1.SelectQuery.Select.Columns.Count; i++)
				{
					var column1 = _sequence1.SelectQuery.Select.Columns[i];
					var column2 = _sequence2.SelectQuery.Select.Columns[i];

					sc.AddEqual(column1.Expression, column2.Expression, Builder.DataOptions.LinqOptions.CompareNulls);
				}

				_sequence2.SelectQuery.Select.Columns.Clear();

				except.Where.ConcatSearchCondition(sc);

				sequence.SelectQuery.Select.Columns.Clear();
				sequence.SelectQuery.SetOperators.Clear();

				SubQuery.Parent = null;

				return SubQuery;
			}
		}

		#endregion
	}
}
