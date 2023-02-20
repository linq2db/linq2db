using System.Reflection;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Reflection;
	using SqlQuery;
    using Common;

	internal sealed class SetOperationBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames = { "Concat", "UnionAll", "Union", "Except", "Intersect", "ExceptAll", "IntersectAll" };

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

				throw new NotImplementedException();
				/*builder.ConvertCompareExpression(query, ExpressionType.Equal, ...)

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

				return sequence;*/
			}

			var set1 = new SubQueryContext(sequence1);
			var set2 = new SubQueryContext(sequence2);

			var setOperator = new SqlSetOperator(set2.SelectQuery, setOperation);

			set1.SelectQuery.SetOperators.Add(setOperator);

			var setContext = new SetOperationContext(setOperation, set1, set2, methodCall);


			if (setOperation != SetOperation.UnionAll)
			{
				var sqlExpr = builder.BuildSqlExpression(setContext, new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], setContext), buildInfo.GetFlags());
			}

			return setContext;
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

				_type = _methodCall.Method.GetGenericArguments()[0];
			}

			readonly Type                 _type;
			readonly MethodCallExpression _methodCall;
			readonly SetOperation         _setOperation;
			readonly SubQueryContext      _sequence1;
			readonly SubQueryContext      _sequence2;
			SqlPlaceholderExpression?     _setIdPlaceholder;
			Expression?                   _setIdReference;

			readonly Dictionary<Expression, SqlPlaceholderExpression> _createdSQL = new(ExpressionEqualityComparer.Instance);
			readonly Dictionary<Expression, Expression> _generatedExpressions = new(ExpressionEqualityComparer.Instance);

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) &&
				    (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)))
				{
					return path;
				}

				if (flags.IsRoot() || flags.IsExpand() || flags.IsTraverse())
					return path;

				if (_createdSQL.TryGetValue(path, out var foundPlaceholder))
					return foundPlaceholder;

				if (flags.IsExpression() && _generatedExpressions.TryGetValue(path, out var alreadyGenerated))
					return alreadyGenerated;

				if (_setIdReference != null && ExpressionEqualityComparer.Instance.Equals(_setIdReference, path))
				{
					return _setIdPlaceholder!;
				}

				var expr1 = BuildProjectionExpression(path, _sequence1, flags);
				var expr2 = BuildProjectionExpression(path, _sequence2, flags);

				if (expr1.UnwrapConvert() is SqlGenericConstructorExpression || expr2.UnwrapConvert() is SqlGenericConstructorExpression)
				{
					return MatchConstructors(path, expr1, expr2, flags);
				}

				var path1 = SequenceHelper.ReplaceContext(path, this, _sequence1);
				var path2 = SequenceHelper.ReplaceContext(path, this, _sequence2);

				var convertFlags = flags;

				var descriptor = Builder.SuggestColumnDescriptor(_sequence1, path1, flags);
				descriptor ??= Builder.SuggestColumnDescriptor(_sequence2, path2, flags);

				var sql1 = Builder.ConvertToSqlExpr(_sequence1, path1, convertFlags, columnDescriptor: descriptor);
				var sql2 = Builder.ConvertToSqlExpr(_sequence2, path2, convertFlags, columnDescriptor: descriptor);

				if (flags.IsExpression())
				{
					if (sql1.Find(1, (_, e) => e is SqlGenericConstructorExpression) != null ||
						sql2.Find(1, (_, e) => e is SqlGenericConstructorExpression) != null)
					{
						return MatchConstructors(path, sql1, sql2, flags);
					}
				}

				var placeholder1 = sql1 as SqlPlaceholderExpression;
				var placeholder2 = sql2 as SqlPlaceholderExpression;

				if (flags.IsTest())
					return placeholder1 ?? placeholder2!;

				convertFlags = convertFlags.SqlFlag();

				if (convertFlags != flags)
				{
					// convert again

					descriptor =   Builder.SuggestColumnDescriptor(_sequence1, path1, convertFlags);
					descriptor ??= Builder.SuggestColumnDescriptor(_sequence2, path2, convertFlags);

					sql1 = Builder.ConvertToSqlExpr(_sequence1, path1, convertFlags, columnDescriptor: descriptor);
					sql2 = Builder.ConvertToSqlExpr(_sequence2, path2, convertFlags, columnDescriptor: descriptor);
				}

				placeholder1 = sql1 as SqlPlaceholderExpression;
				placeholder2 = sql2 as SqlPlaceholderExpression;

				if ((placeholder1 == null || ((SqlColumn)placeholder1.Sql).Expression.IsNullValue()) && placeholder2 != null)
				{
					placeholder1 = ExpressionBuilder.CreatePlaceholder(_sequence1, new SqlValue(QueryHelper.GetDbDataType(placeholder2.Sql), null), path1);
				}
				else if ((placeholder2 == null || ((SqlColumn)placeholder2.Sql).Expression.IsNullValue()) && placeholder1 != null)
				{
					placeholder2 = ExpressionBuilder.CreatePlaceholder(_sequence2.SubQuery, new SqlValue(QueryHelper.GetDbDataType(placeholder1.Sql), null), path2);
				}

				if (placeholder1 is null || placeholder2 is null)
					return path;

				placeholder1 = (SqlPlaceholderExpression)SequenceHelper.CorrectSelectQuery(placeholder1, _sequence1.SelectQuery);
				placeholder2 = (SqlPlaceholderExpression)SequenceHelper.CorrectSelectQuery(placeholder2, _sequence2.SelectQuery);

				var alias   = GenerateColumnAlias(path1);
				var column1 = Builder.MakeColumn(SelectQuery, placeholder1.WithAlias(alias), true);
				var column2 = Builder.MakeColumn(SelectQuery, placeholder2.WithAlias(alias), true);

				var resultPlaceholder = column1.WithPath(path);

				_createdSQL.Add(path, resultPlaceholder);

				return resultPlaceholder;
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

			static MethodInfo _keySetIdMethosInfo = Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(typeof(int));

			const           string     ProjectionSetIdFieldName = "__projection__set_id__";
			static readonly Expression _setIdFieldName          = Expression.Constant(ProjectionSetIdFieldName);

			Expression EnsureSQL(Expression expr, ProjectFlags flags)
			{
				do
				{
					var newExpr = Builder.BuildSqlExpression(this, expr, flags.SqlFlag());
					if (ReferenceEquals(expr, newExpr))
						break;

					expr = newExpr;
				} while (true);

				return expr;
			}

			Expression MakeConditionalConstructExpression(Expression path, Expression leftExpression, Expression rightExpression, ProjectFlags flags)
			{
				leftExpression  = SequenceHelper.RemapToNewPath(this, leftExpression, path, flags.ExpressionFlag());

				leftExpression = EnsureSQL(leftExpression, flags);

				rightExpression = SequenceHelper.RemapToNewPath(this, rightExpression, path, flags.ExpressionFlag());

				rightExpression = EnsureSQL(rightExpression, flags);

				var sequenceLeftSetId  = Builder.GenerateSetId(_sequence1.SubQuery.SelectQuery.SourceID);
				var sequenceRightSetId = Builder.GenerateSetId(_sequence2.SubQuery.SelectQuery.SourceID);

				if (_setIdReference == null)
				{

					var sqlValueLeft  = new SqlValue(sequenceLeftSetId);
					var sqlValueRight = new SqlValue(sequenceRightSetId);

					var thisRef  = new ContextRefExpression(_type, this);

					_setIdReference = Expression.Call(_keySetIdMethosInfo, thisRef, Expression.Constant(ProjectionSetIdFieldName));

					var leftRef  = new ContextRefExpression(_type, _sequence1);
					var rightRef = new ContextRefExpression(_type, _sequence2);

					var keyLeft  = Expression.Call(_keySetIdMethosInfo, leftRef, _setIdFieldName);
					var keyRight = Expression.Call(_keySetIdMethosInfo, rightRef, _setIdFieldName);

					var leftIdPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence1, sqlValueLeft, keyLeft, alias: ProjectionSetIdFieldName);
					leftIdPlaceholder = (SqlPlaceholderExpression)Builder.UpdateNesting(this, leftIdPlaceholder);

					var rightIdPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence2, sqlValueRight,
						keyRight, alias: ProjectionSetIdFieldName);
					rightIdPlaceholder = Builder.MakeColumn(SelectQuery, rightIdPlaceholder, asNew: true);

					_setIdPlaceholder = leftIdPlaceholder.WithPath(_setIdReference).WithTrackingPath(_setIdReference);
				}

				if (leftExpression.Type != path.Type)
				{
					leftExpression = Expression.Convert(leftExpression, path.Type);
				}

				if (rightExpression.Type != path.Type)
				{
					rightExpression = Expression.Convert(rightExpression, path.Type);
				}

				var resultExpr = Expression.Condition(
					Expression.Equal(_setIdReference, Expression.Constant(sequenceLeftSetId)),
					leftExpression,
					rightExpression
				);

				return resultExpr;
			}

			Expression BuildProjectionExpression(SubQueryContext context, ProjectFlags projectFlags)
			{
				var thisRef = new ContextRefExpression(_type, this);
				return BuildProjectionExpression(thisRef, context, projectFlags);
			}


			Expression ExpandExpression(SubQueryContext context, Expression expression)
			{
				var prev = expression;
				do
				{
					var expanded = prev.Transform(e =>
					{
						var newExpr = Builder.MakeExpression(context, e, ProjectFlags.Expand);
						if (ExpressionEqualityComparer.Instance.Equals(newExpr, e))
							return e;
						return newExpr;
					});

					if (ReferenceEquals(prev, expanded))
						break;

					prev = expanded;

				} while (true);

				return prev;
			}

			Expression EnsureGenericConstructor(Expression expression)
			{
				var transformed = expression.Transform(e =>
				{
					var newExpr = SqlGenericConstructorExpression.Parse(e);
					if (newExpr != e)
						return new TransformInfo(newExpr, false, true);

					return new TransformInfo(e);
				});

				return transformed;
			}

			Expression BuildProjectionExpression(Expression path, SubQueryContext context, ProjectFlags projectFlags)
			{
				var correctedPath = SequenceHelper.ReplaceContext(path, this, context);

				var projectionExpression = Builder.ConvertToSqlExpr(context.SubQuery, correctedPath, projectFlags);

				projectionExpression = EnsureGenericConstructor(projectionExpression);

				projectionExpression = Builder.BuildSqlExpression(context.SubQuery, projectionExpression, projectFlags);

				var remapped = SequenceHelper.RemapToNewPath(this, projectionExpression, path, projectFlags);

				return remapped;
			}

			// For Set we have to ensure hat columns are not optimized
			protected override bool OptimizeColumns => false;

			bool IsIncompatible(Expression expression)
			{
				if (expression is SqlGenericConstructorExpression generic && generic.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
				{
					var ed = Builder.MappingSchema.GetEntityDescriptor(generic.ObjectType);
					if (ed.InheritanceMapping.Count > 0)
						return true;
				}

				var isIncompatible = null != expression.Find(expression, (_, e) =>
				{
					if (e is MemberExpression || e is ContextRefExpression || e is SqlGenericConstructorExpression)
						return false;

					return true;
				});

				return isIncompatible;
			}

			static bool IsEqualProjections(Expression left, Expression right)
			{
				if (left is SqlGenericConstructorExpression leftGeneric &&
				    right is SqlGenericConstructorExpression rightGeneric)
				{
					if (leftGeneric.ConstructType  == SqlGenericConstructorExpression.CreateType.Full &&
					    rightGeneric.ConstructType == SqlGenericConstructorExpression.CreateType.Full &&
					    leftGeneric.ObjectType     == rightGeneric.ObjectType)
					{
						return true;
					}
				}

				if (ExpressionEqualityComparer.Instance.Equals(left, right))
					return true;

				return false;
			}


			public Expression MergeProjections(Type objectType, IEnumerable<Expression> projections, ref bool incompatible)
			{
				return MergeProjections(objectType,
					projections.Select(e => (e, GetMemberPath(e))).ToList(),
					0, ref incompatible);
			}

			class MemberOrParameter : IEquatable<MemberOrParameter>
			{
				public MemberOrParameter(MemberInfo memberInfo)
				{
					Member = memberInfo;
				}

				public MemberOrParameter(Parameter? parameter)
				{
					Parameter = parameter;
				}

				public readonly MemberInfo? Member;
				public readonly Parameter?  Parameter;

				public bool Equals(MemberOrParameter? other)
				{
					if (ReferenceEquals(null, other))
					{
						return false;
					}

					if (ReferenceEquals(this, other))
					{
						return true;
					}

					return Equals(Member, other.Member) && Equals(Parameter, other.Parameter);
				}

				public override bool Equals(object? obj)
				{
					if (ReferenceEquals(null, obj))
					{
						return false;
					}

					if (ReferenceEquals(this, obj))
					{
						return true;
					}

					if (obj.GetType() != this.GetType())
					{
						return false;
					}

					return Equals((MemberOrParameter)obj);
				}

				public override int GetHashCode()
				{
					unchecked
					{
						return ((Member != null ? Member.GetHashCode() : 0) * 397) ^ (Parameter != null ? Parameter.GetHashCode() : 0);
					}
				}

				public static bool operator ==(MemberOrParameter? left, MemberOrParameter? right)
				{
					return Equals(left, right);
				}

				public static bool operator !=(MemberOrParameter? left, MemberOrParameter? right)
				{
					return !Equals(left, right);
				}
			}

			class Parameter
			{
				public Parameter(int paramIndex)
				{
					ParamIndex = paramIndex;
				}

				public readonly MethodInfo? Method = null;
				public readonly int         ParamIndex;
			}

			static List<Expression> CollectDataExpressions(Expression expression)
			{
				var result = new List<Expression>();
				expression.Visit(result, (items, e) =>
				{
					if (e is MemberExpression me)
					{
						var current = e;
						while (true)
						{
							if (current is MemberExpression cm)
								current = cm.Expression;
							else if (current is SqlGenericParamAccessExpression gp)
								current = gp.Constructor;
							else
								break;
						}

						if (current is ContextRefExpression)
						{
							items.Add(e);
							return false;
						}
					}

					return true;
				});

				return result;
			}

			static List<MemberOrParameter> GetMemberPath(Expression expr)
			{
				var result  = new List<MemberOrParameter>();
				var current = expr;

				while (true)
				{
					MemberOrParameter item;
					if (current is MemberExpression memberExpression)
					{
						item = new MemberOrParameter(memberExpression.Member.DeclaringType?.GetMemberEx(memberExpression.Member) ?? throw new InvalidOperationException());
						current = memberExpression.Expression;
					}
					else if (current is SqlGenericParamAccessExpression paramAccess)
					{
						throw new NotImplementedException();
						item    = new MemberOrParameter(new Parameter(paramAccess.ParamIndex));
						current = paramAccess.Constructor;
					}
					else break;
					result.Insert(0, item);
				}

				return result;
			}

			Expression MergeProjections(Type objectType, List<(Expression path, List<MemberOrParameter> pathList)> pathList, int level, ref bool incompatible)
			{
				var grouped = pathList.GroupBy(p => p.pathList[level])
					.Where(g => g.Key.Member != null)
					.Select(g => new { g.Key, Members = g.ToList() });

				var assignments  = new List<SqlGenericConstructorExpression.Assignment>();

				foreach (var g in grouped)
				{
					var member = g.Key.Member;

					List<(Expression path, List<MemberOrParameter> pathList)>? newList = null;
					(Expression path, List<MemberOrParameter> pathList)        found   = default;

					foreach (var c in g.Members)
					{
						if (c.pathList.Count == level + 1)
						{
							if (found.path == null)
							{
								found = c;
							}
						}
						else
						{
							newList ??= new();
							newList.Add(c);
						}
					}

					if (newList != null)
					{
						if (found.path != null)
							incompatible = true;

						assignments.Add(new SqlGenericConstructorExpression.Assignment(member!, MergeProjections(member!.GetMemberType(), newList, level + 1, ref incompatible), false, false));
					}
					else
					{
						if (found.path != null)
						{
							assignments.Add(new SqlGenericConstructorExpression.Assignment(member!, found.path, false, false));
						}
					}
				}

				return new SqlGenericConstructorExpression(SqlGenericConstructorExpression.CreateType.Auto, objectType, null, assignments.AsReadOnly());
			}

			static Expression NormalizeToDeclaringTypExpression(Expression expression)
			{
				if (expression is MemberExpression me)
				{
					if (me.Expression is not MemberExpression)
					{
						if (me.Expression.Type != me.Member.DeclaringType && me.Member.DeclaringType != null)
						{
							if (me.Expression is ContextRefExpression contextRef)
								return Expression.MakeMemberAccess(contextRef.WithType(me.Member.DeclaringType),
									me.Member);
						}
					}
					return me.Update(NormalizeToDeclaringTypExpression(me.Expression));
				}

				return expression;
			}

			public bool IsCompatibleForCommonProjection(SqlGenericConstructorExpression projection,
				SqlGenericConstructorExpression                                         generated)
			{
				foreach (var generatedAssignment in generated.Assignments)
				{
					var found = projection.Assignments.FirstOrDefault(a =>
						MemberInfoEqualityComparer.Default.Equals(a.MemberInfo, generatedAssignment.MemberInfo));

					if (found == null)
					{
						if (generatedAssignment.Expression is SqlGenericConstructorExpression)
							return false;
					}
					else
					{
						if (generatedAssignment.Expression is SqlGenericConstructorExpression subGenerated)
						{
							if (found.Expression is SqlGenericConstructorExpression subProjection)
							{
								if (!IsCompatibleForCommonProjection(subProjection, subGenerated))
									return false;
							}
							else
								return false;
						}

						if (!IsEqualProjections(found.Expression, generatedAssignment.Expression))
							return false;
					}
				}

				return true;
			}


			Expression MatchConstructors(Expression path, Expression expr1, Expression expr2,  ProjectFlags flags)
			{
				Expression resultExpr;

				if (ExpressionEqualityComparer.Instance.Equals(expr1, path))
				{
					expr1 = Expression.Default(expr1.Type);
				}

				if (ExpressionEqualityComparer.Instance.Equals(expr2, path))
				{
					expr2 = Expression.Default(expr2.Type);
				}

				if (IsEqualProjections(expr1, expr2))
					return expr1;

				if (flags.HasFlag(ProjectFlags.Expression))
				{
					if (_setOperation == SetOperation.Except || _setOperation == SetOperation.ExceptAll)
						return expr1;

					if (IsEqualProjections(expr1, expr2))
						return expr1;

					//if (IsIncompatible(expr1) || IsIncompatible(expr2))
					//{
					//	return MakeConditionalExpression(path, expr1, expr2);
					//}

					var incompatible = false;
					resultExpr = MergeProjections(path.Type, CollectDataExpressions(expr1).Concat(CollectDataExpressions(expr2)), ref incompatible);
					if (resultExpr is SqlGenericConstructorExpression generic)
					{
						if (expr1 is SqlGenericConstructorExpression contructor1 &&
						    expr2 is SqlGenericConstructorExpression contructor2)
						{
							incompatible = !IsCompatibleForCommonProjection(contructor1, generic) ||
							               !IsCompatibleForCommonProjection(contructor2, generic);
						}

						if (expr1 is not SqlGenericConstructorExpression ||
						    expr2 is not SqlGenericConstructorExpression)
						{
							incompatible = !ExpressionEqualityComparer.Instance.Equals(expr1, expr2);
						}

						if (incompatible || Builder.TryConstruct(Builder.MappingSchema, generic, this, flags) == null)
						{
							// fallback to set
							resultExpr = MakeConditionalExpression(path, expr1, expr2, flags);
						}
					}
				}
				else
				{
					var incompatible = false;
					resultExpr = MergeProjections(path.Type, CollectDataExpressions(expr1).Concat(CollectDataExpressions(expr2)), ref incompatible);
				}


				if (flags.IsExpression())
					_generatedExpressions[path] = resultExpr;

				return resultExpr;
			}

			Expression MakeConditionalExpression(Expression path, Expression expr1, Expression expr2, ProjectFlags flags)
			{
				if (_setOperation != SetOperation.UnionAll)
				{
					throw new LinqToDBException(
						$"Could not decide which construction type to use `query.Select(x => new {expr1.Type.Name} {{ ... }})` to specify projection.");
				}

				return MakeConditionalConstructExpression(path, expr1, expr2, flags);
			}
		}

		#endregion
	}
}
