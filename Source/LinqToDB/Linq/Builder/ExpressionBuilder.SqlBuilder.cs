using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Common.Internal;
	using Data;
	using DataProvider;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	internal partial class ExpressionBuilder
	{
		#region LinqOptions shortcuts

		public CompareNulls CompareNulls => DataOptions.LinqOptions.CompareNulls;

		#endregion

		#region Build Where

		public IBuildContext? BuildWhere(
			IBuildContext?   parent,
			IBuildContext    sequence,
			LambdaExpression condition,
			bool             checkForSubQuery,
			bool             enforceHaving,
			out Expression?  error)
		{
			error = null;

			var buildSequence = sequence;

			if (enforceHaving)
			{
				var root = sequence.Builder.GetRootContext(sequence, true);

				if (root != null && root.BuildContext is GroupByBuilder.GroupByContext groupByContext)
				{
					buildSequence = groupByContext.SubQuery;
				}
			}

			if (!enforceHaving)
			{
				if (buildSequence is not SubQueryContext subQuery || subQuery.NeedsSubqueryForComparison)
				{
					buildSequence = new SubQueryContext(sequence);
				}

				sequence.SetAlias(condition.Parameters[0].Name);
				sequence = buildSequence;
			}

			var body = SequenceHelper.PrepareBody(condition, sequence);
			var expr = body.Unwrap();

			var sc = new SqlSearchCondition();

			if (!_buildVisitor.BuildSearchCondition(buildSequence, expr, sc, out var errorExpr))
			{
				error = errorExpr;
				return null;
			}

			if (!enforceHaving && QueryHelper.ContainsWindowFunction(sc))
			{
				error = new SqlErrorExpression(expr, ErrorHelper.Error_WindowFunctionsInSearchCondition, expr.Type);
				return null;
			}

			if (enforceHaving)
				buildSequence.SelectQuery.Having.ConcatSearchCondition(sc);
			else
				buildSequence.SelectQuery.Where.ConcatSearchCondition(sc);

			if (!enforceHaving)
			{
				return buildSequence;
			}

			return sequence;
		}

		#endregion

		#region Build Skip/Take

		public void BuildTake(IBuildContext sequence, ISqlExpression expr, TakeHints? hints)
		{
			var sql = sequence.SelectQuery;

			if (hints != null && !DataContext.SqlProviderFlags.GetIsTakeHintsSupported(hints.Value))
				throw new LinqToDBException($"TakeHints are {hints} not supported by current database");

			if (hints != null && sql.Select.SkipValue != null)
				throw new LinqToDBException("Take with hints could not be applied with Skip");

			if (sql.Select.TakeValue != null)
			{
				expr = new SqlConditionExpression(
					new SqlPredicate.ExprExpr(sql.Select.TakeValue, SqlPredicate.Operator.Less, expr, null),
					sql.Select.TakeValue,
					expr);
			}

			sql.Select.Take(expr, hints);
		}

		public void BuildSkip(IBuildContext sequence, ISqlExpression expr)
		{
			var sql = sequence.SelectQuery;

			if (sql.Select.TakeHints != null)
				throw new LinqToDBException("Skip could not be applied with Take with hints");

			if (sql.Select.SkipValue != null)
				sql.Select.Skip(new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", expr, Precedence.Additive));
			else
				sql.Select.Skip(expr);

			if (sql.Select.TakeValue != null)
			{
				sql.Select.Take(
					new SqlBinaryExpression(typeof(int), sql.Select.TakeValue, "-", expr, Precedence.Additive),
					sql.Select.TakeHints);
			}
		}

		#endregion

		#region SubQueryToSql

		/// <summary>
		/// Checks that provider can handle limitation inside subquery. This function is tightly coupled with <see cref="SelectQueryOptimizerVisitor.OptimizeApply"/>
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public bool IsSupportedSubquery(IBuildContext parent, IBuildContext context, out string? errorMessage)
		{
			errorMessage = null;

			if (!ValidateSubqueries)
				return true;

			// No check during recursion. Cloning may fail
			if (parent.Builder.IsRecursiveBuild)
				return true;

			if (!context.Builder.DataContext.SqlProviderFlags.IsApplyJoinSupported)
			{
				// We are trying to simulate what will be with query after optimizer's work
				//
				var cloningContext = new CloningContext();

				var clonedParentContext = cloningContext.CloneContext(parent);
				var clonedContext       = cloningContext.CloneContext(context);

				cloningContext.UpdateContextParents();

				SqlJoinedTable? fakeJoin = null;

				using var snapshot = _buildVisitor.CreateSnapshot();

				var expr = parent.Builder.BuildSqlExpression(clonedContext, new ContextRefExpression(clonedContext.ElementType, clonedContext));

				// add fake join there is no still reference
				if (clonedParentContext.SelectQuery.From.Tables.Count                                                    > 0
				    && clonedParentContext.SelectQuery.Find(e => e is SelectQuery sc && sc == clonedContext.SelectQuery) == null)
				{
					fakeJoin = clonedContext.SelectQuery.OuterApply().JoinedTable;

					clonedParentContext.SelectQuery.From.Tables[0].Joins.Add(fakeJoin);
				}

				expr = parent.Builder.ToColumns(clonedParentContext, expr);

				using var visitor = QueryHelper.SelectOptimizer.Allocate();

#if DEBUG

				var sqlText = clonedParentContext.SelectQuery.ToDebugString();

#endif

				var optimizedQuery = (SelectQuery)visitor.Value.Optimize(
					root: clonedParentContext.SelectQuery,
					rootElement: clonedParentContext.SelectQuery,
					providerFlags: parent.Builder.DataContext.SqlProviderFlags,
					removeWeakJoins: false,
					dataOptions: parent.Builder.DataOptions,
					mappingSchema: context.MappingSchema,
					evaluationContext: new EvaluationContext()
				);

				if (!SqlProviderHelper.IsValidQuery(optimizedQuery, 
					    parentQuery: null, 
					    fakeJoin: fakeJoin, 
					    columnSubqueryLevel: null, 
					    parent.Builder.DataContext.SqlProviderFlags, 
					    out errorMessage))
				{
					return false;
				}
			}

			return true;
		}

		#endregion

		#region ConvertExpression

		public Expression ConvertExpression(Expression expression)
		{
			using var visitor = _exposeVisitorPool.Allocate();

			var result = visitor.Value.ExposeExpression(DataContext, _optimizationContext, ParameterValues, expression, includeConvert : true, optimizeConditions : false, compactBinary : false, isSingleConvert: true);

			return result;
		}

		public Expression ConvertSingleExpression(Expression expression, bool inProjection)
		{
			// We can convert only these expressions, so it is shortcut to do not allocate visitor

			if (expression.NodeType is ExpressionType.Call
				                    or ExpressionType.MemberAccess
				                    or ExpressionType.New
				|| expression is BinaryExpression)
			{
				var result = ConvertExpression(expression);

				return result;
			}

			return expression;
		}

		#endregion

		#region BuildExpression

		public bool TryConvertToSql(IBuildContext context, Expression expression, [NotNullWhen(true)] out ISqlExpression? translated)
		{
			var translatedExpr = BuildSqlExpression(context, expression);

			if (translatedExpr is SqlPlaceholderExpression placeholder)
			{
				placeholder = UpdateNesting(context, placeholder);
				translated  = placeholder.Sql;
				return true;

			}

			translated = null;
			return false;
		}

		public ISqlExpression ConvertToSql(IBuildContext? context, Expression expression, bool isPureExpression = false)
		{
			var translatedExpr = BuildSqlExpression(context, expression, BuildPurpose.Sql, isPureExpression ? BuildFlags.FormatAsExpression : BuildFlags.None);

			if (translatedExpr is SqlPlaceholderExpression { Sql: var sql })
			{
				return sql;
			}

			throw SqlErrorExpression.EnsureError(translatedExpr, expression.Type).CreateException();
		}

		public IDisposable UsingColumnDescriptor(ColumnDescriptor? columnDescriptor)
		{
			return _buildVisitor.UsingColumnDescriptor(columnDescriptor);
		}

		public static SqlPlaceholderExpression CreatePlaceholder(IBuildContext? context, ISqlExpression sqlExpression,
			Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			var placeholder = new SqlPlaceholderExpression(context?.SelectQuery, sqlExpression, path, convertType, alias, index, trackingPath ?? path);
			return placeholder;
		}

		public static SqlPlaceholderExpression CreatePlaceholder(SelectQuery? selectQuery, ISqlExpression sqlExpression,
			Expression path, Type? convertType = null, string? alias = null, int? index = null, Expression? trackingPath = null)
		{
			var placeholder = new SqlPlaceholderExpression(selectQuery, sqlExpression, path, convertType, alias, index, trackingPath ?? path);
			return placeholder;
		}

		public bool IsForceParameter(Expression expression, ColumnDescriptor? columnDescriptor)
		{
			if (columnDescriptor?.ValueConverter != null)
			{
				return true;
			}

			var converter = MappingSchema.GetConvertExpression(expression.Type, typeof(DataParameter), false, false);
			if (converter != null)
			{
				return true;
			}

			return false;
		}

		public ISqlExpression PosProcessCustomExpression(Expression expression, ISqlExpression sqlExpression, NullabilityContext nullabilityContext)
		{
			if (sqlExpression is SqlExpression { Expr: "{0}", Parameters.Length: 1 } expr)
			{
				var expressionNull = nullabilityContext.CanBeNull(sqlExpression);
				var argNull        = nullabilityContext.CanBeNull(expr.Parameters[0]);

				if (expressionNull != argNull)
					return SqlNullabilityExpression.ApplyNullability(expr.Parameters[0], expressionNull);

				return expr.Parameters[0];
			}

			return sqlExpression;
		}

		#endregion

		#region CanBeConstant

		public bool CanBeConstant(Expression expr)
		{
			if (!ParametersContext.CanBeConstant(expr))
			{
				return false;
			}
			return _optimizationContext.CanBeConstant(expr);
		}

		#endregion

		#region CanBeEvaluatedOnClient

		/// <summary>
		/// Check if <paramref name="expr"/> could be evaluated on client side.
		/// </summary>
		public bool CanBeEvaluatedOnClient(Expression expr)
		{
			return _optimizationContext.CanBeEvaluatedOnClient(expr);
		}

		#endregion

		#region Build Constant

		readonly Dictionary<(Expression, ColumnDescriptor?, int),SqlValue> _constants = new ();

		public SqlValue? BuildConstant(MappingSchema mappingSchema, Expression expr, ColumnDescriptor? columnDescriptor)
		{
			var key = (expr, columnDescriptor, ((IConfigurationID)mappingSchema).ConfigurationID);
			if (_constants.TryGetValue(key, out var sqlValue))
				return sqlValue;

			var dbType = columnDescriptor?.GetDbDataType(true).WithSystemType(expr.Type) ?? mappingSchema.GetDbDataType(expr.Type);

			var unwrapped = expr.Unwrap();
			if (unwrapped != expr && !mappingSchema.ValueToSqlConverter.CanConvert(dbType.SystemType) &&
			    mappingSchema.ValueToSqlConverter.CanConvert(unwrapped.Type))
			{
				dbType = dbType.WithSystemType(unwrapped.Type);
				expr   = unwrapped;
			}

			dbType = dbType.WithSystemType(expr.Type);

			var hasConverter = false;

			if (columnDescriptor != null)
			{
				expr = columnDescriptor.ApplyConversions(expr, dbType, true);
			}
			else
			{
				if (!mappingSchema.ValueToSqlConverter.CanConvert(dbType.SystemType))
				{
					expr = ColumnDescriptor.ApplyConversions(mappingSchema, expr, dbType, null, true);
				}
				else
				{
					hasConverter = true;
				}
			}

			var value = EvaluateExpression(expr);

			if (dbType.DataType == DataType.Undefined && value is not null && value.GetType() != dbType.SystemType)
			{
				dbType = mappingSchema.GetDbDataType(value.GetType());
			}

			if (dbType.DataType == DataType.Undefined && !hasConverter)
				return null;

			sqlValue = mappingSchema.GetSqlValue(expr.Type, value, dbType);

			if (columnDescriptor != null)
			{
				sqlValue.ValueType = dbType;
			}

			_constants.Add(key, sqlValue);
			
			return sqlValue;
		}

		#endregion

		#region Predicate Converter

		#region ConvertCompare

		public bool TryGenerateComparison(
			IBuildContext?                               context,
			Expression                                   left,
			Expression                                   right,
			[NotNullWhen(true)] out  SqlSearchCondition? searchCondition,
			[NotNullWhen(false)] out SqlErrorExpression? error,
			BuildPurpose?                                buildPurpose = default)
		{
			return _buildVisitor.TryGenerateComparison(context, left, right, out searchCondition, out error, buildPurpose);
		}

		public SqlSearchCondition GenerateComparison(
			IBuildContext? context,
			Expression     left,
			Expression     right,
			BuildPurpose?  buildPurpose = default)
		{
			return _buildVisitor.GenerateComparison(context, left, right, buildPurpose);
		}

		public static List<SqlPlaceholderExpression> CollectPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					list.Add(placeholder);
				}
			});

			return result;
		}

		public static List<SqlPlaceholderExpression> CollectDistinctPlaceholders(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			expression.Visit(result, static (list, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (!list.Contains(placeholder))
						list.Add(placeholder);
				}
			});

			return result;
		}

		public bool CollectNullCompareExpressions(IBuildContext context, Expression expression, List<Expression> result)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant:
				case ExpressionType.Default:
				{
					result.Add(expression);
					return true;
				}
			}

			if (expression is SqlPlaceholderExpression or DefaultValueExpression)
			{
				result.Add(expression);
				return true;
			}

			if (expression is SqlGenericConstructorExpression generic)
			{
				foreach (var assignment in generic.Assignments)
				{
					if (!CollectNullCompareExpressions(context, assignment.Expression, result))
						return false;
				}

				foreach (var parameter in generic.Parameters)
				{
					if (!CollectNullCompareExpressions(context, parameter.Expression, result))
						return false;
				}

				return true;
			}

			if (expression is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				result.AddRange(defaultIfEmptyExpression.NotNullExpressions);
				return true;
			}

			if (expression is SqlEagerLoadExpression)
				return true;

			return false;
		}

		#endregion

		#region Parameters

		public static DbDataType GetMemberDataType(MappingSchema mappingSchema, MemberInfo member)
		{
			var typeResult = new DbDataType(member.GetMemberType());

			var dta = mappingSchema.GetAttribute<DataTypeAttribute>(member.ReflectedType!, member);
			var ca  = mappingSchema.GetAttribute<ColumnAttribute>  (member.ReflectedType!, member);

			var dataType = ca?.DataType ?? dta?.DataType;

			if (dataType != null)
				typeResult = typeResult.WithDataType(dataType.Value);

			var dbType = ca?.DbType ?? dta?.DbType;
			if (dbType != null)
				typeResult = typeResult.WithDbType(dbType);

			if (ca != null && ca.HasLength())
				typeResult = typeResult.WithLength(ca.Length);

			return typeResult;
		}

		#endregion

		#region MakeIsPredicate

		public ISqlPredicate MakeIsPredicate(TableBuilder.TableContext table, Type typeOperand)
		{
			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return SqlPredicate.True;
			}

			return MakeIsPredicate(table, table, table.InheritanceMapping, typeOperand, static (table, name) => table.SqlTable.FindFieldByMemberName(name) ?? throw new LinqToDBException($"Field {name} not found in table {table.SqlTable}"));
		}

		public ISqlPredicate MakeIsPredicate<TContext>(
			TContext                              getSqlContext,
			IBuildContext                         context,
			IReadOnlyList<InheritanceMapping>     inheritanceMapping,
			Type                                  toType,
			Func<TContext,string, ISqlExpression> getSql)
		{
			var inverse = false;
			var discriminators = new List<object?>(inheritanceMapping.Count);

			foreach (var im1 in inheritanceMapping)
			{
				if (toType.IsAssignableFrom(im1.Type))
				{
					if (im1.IsDefault)
					{
						inverse = true;
						break;
					}
					discriminators.Add(im1.Code);
				}
			}

			if (inverse)
			{
				discriminators.Clear();

				foreach (var im2 in inheritanceMapping)
				{
					if (!toType.IsAssignableFrom(im2.Type))
					{
						if (im2.IsDefault)
							throw new InvalidOperationException($"Multiple default inheritance mappings for {toType}");

						discriminators.Add(im2.Code);
					}
				}
			}

			if (discriminators.Count == 0 || discriminators.Count == inheritanceMapping.Count)
			{
				var all = (inverse && discriminators.Count == 0) || (!inverse && discriminators.Count == inheritanceMapping.Count);
				var allCond = new SqlSearchCondition();
				allCond.Predicates.Add(SqlPredicate.MakeBool(all));
				return allCond;
			}

			var cond = new SqlSearchCondition();

			var m      = inheritanceMapping[0];
			var values = new SqlValue[discriminators.Count];
			var dbType = m.Discriminator.GetDbDataType(true);
			var idx    = 0;

			foreach (var value in discriminators)
			{
				values[idx] = MappingSchema.GetSqlValue(m.Discriminator.MemberType, value, dbType);
				idx++;
			}

			cond.Predicates.Add(
				new SqlPredicate.InList(
					getSql(getSqlContext, m.DiscriminatorName),
					DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null,
					inverse,
					values));

			return cond;
		}

		Expression MakeIsPredicateExpression(IBuildContext context, TypeBinaryExpression expression)
		{
			var typeOperand = expression.TypeOperand;
			var table       = new TableBuilder.TableContext(this, context.MappingSchema, new BuildInfo((IBuildContext?)null, ExpressionInstances.UntypedNull, new SelectQuery()), typeOperand);

			if (typeOperand == table.ObjectType)
			{
				var all = true;
				foreach (var m in table.InheritanceMapping)
				{
					if (m.Type == typeOperand)
					{
						all = false;
						break;
					}
				}

				if (all)
					return Expression.Constant(true);
			}

			var mapping = new List<(InheritanceMapping m, int i)>(table.InheritanceMapping.Count);

			for (var i = 0; i < table.InheritanceMapping.Count; i++)
			{
				var m = table.InheritanceMapping[i];
				if (typeOperand.IsAssignableFrom(m.Type) && !m.IsDefault)
					mapping.Add((m, i));
			}

			var isEqual = true;

			if (mapping.Count == 0)
			{
				for (var i = 0; i < table.InheritanceMapping.Count; i++)
				{
					var m = table.InheritanceMapping[i];
					if (!m.IsDefault)
						mapping.Add((m, i));
				}

				isEqual = false;
			}

			Expression? expr = null;

			foreach (var m in mapping)
			{
				var field = table.SqlTable.FindFieldByMemberName(table.InheritanceMapping[m.i].DiscriminatorName) ?? throw new LinqToDBException($"Field {table.InheritanceMapping[m.i].DiscriminatorName} not found in table {table.SqlTable}");
				var ttype = field.ColumnDescriptor.MemberAccessor.TypeAccessor.Type;
				var obj   = expression.Expression;

				if (obj.Type != ttype)
					obj = Expression.Convert(expression.Expression, ttype);

				var memberInfo = ttype.GetMemberEx(field.ColumnDescriptor.MemberInfo) ?? throw new InvalidOperationException();

				var left = Expression.MakeMemberAccess(obj, memberInfo);
				var code = m.m.Code;

				if (code == null)
					code = left.Type.GetDefaultValue();
				else if (left.Type != code.GetType())
					code = Converter.ChangeType(code, left.Type, MappingSchema);

				Expression right = Expression.Constant(code, left.Type);

				var e = isEqual ? Expression.Equal(left, right) : Expression.NotEqual(left, right);

				if (!isEqual)
					expr = expr != null ? Expression.AndAlso(expr, e) : e;
				else
					expr = expr != null ? Expression.OrElse(expr, e) : e;
			}

			return expr!;
		}

		#endregion

		#endregion

		#region Search Condition Builder

		public void BuildSearchCondition(IBuildContext? context, Expression expression, SqlSearchCondition searchCondition)
		{
			_buildVisitor.BuildSearchCondition(context, expression, searchCondition);
		}

		#endregion

		#region Helpers

		bool IsNullConstant(Expression expr)
		{
			// TODO: is it correct to return true for DefaultValueExpression for non-reference type or when default value
			// set to non-null value?
			return expr.UnwrapConvert().IsNullValue();
		}

		TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformer;
		TransformVisitor<ExpressionBuilder>? _removeNullPropagationTransformerForSearch;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		TransformVisitor<ExpressionBuilder> GetRemoveNullPropagationTransformer(bool forSearch)
		{
			if (forSearch)
				return _removeNullPropagationTransformerForSearch ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, true));
			else
				return _removeNullPropagationTransformer ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.RemoveNullPropagation(e, false));
		}

		public Expression RemoveNullPropagation(IBuildContext context, Expression expr, bool toSql)
		{
			return _buildVisitor.RemoveNullPropagation(expr, toSql);
		}

		public Expression RemoveNullPropagation(Expression expr, bool forSearch)
		{
			bool IsAcceptableType(Type type)
			{
				if (!forSearch)
					return type.IsNullableType();

				if (MappingSchema.IsCollectionType(type))
					return true;

				if (!MappingSchema.IsScalarType(type))
					return true;

				return false;
			}

			// Do not modify parameters
			//
			if (CanBeEvaluatedOnClient(expr))
				return expr;

			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
					var conditional = (ConditionalExpression)expr;
					if (conditional.Test.NodeType == ExpressionType.NotEqual)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
							else if (IsNullConstant(conditional.IfFalse)
								&& ((nullRight && IsAcceptableType(binary.Left.Type) ||
									(nullLeft  && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
						}
					}
					else if (conditional.Test.NodeType == ExpressionType.Equal)
					{
						var binary    = (BinaryExpression)conditional.Test;
						var nullRight = IsNullConstant(binary.Right);
						var nullLeft  = IsNullConstant(binary.Left);
						if (nullRight || nullLeft)
						{
							if (nullRight && nullLeft)
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfTrue);
							}
							else if (IsNullConstant(conditional.IfTrue)
									 && ((nullRight && IsAcceptableType(binary.Left.Type) ||
										  (nullLeft && IsAcceptableType(binary.Right.Type)))))
							{
								return GetRemoveNullPropagationTransformer(forSearch).Transform(conditional.IfFalse);
							}
						}
					}
					break;
			}

			return expr;
		}

		public bool ProcessProjection(Dictionary<MemberInfo,Expression> members, Expression expression)
		{
			void CollectParameters(Type forType, MethodBase method, ReadOnlyCollection<Expression> arguments)
			{
				var pms = method.GetParameters();

				var typeMembers = TypeAccessor.GetAccessor(forType).Members;

				for (var i = 0; i < pms.Length; i++)
				{
					var param = pms[i];
					MemberAccessor? foundMember = null;
					foreach (var tm in typeMembers)
					{
						if (tm.Name == param.Name)
						{
							foundMember = tm;
							break;
						}
					}

					if (foundMember == null)
					{
						foreach (var tm in typeMembers)
						{
							if (tm.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
							{
								foundMember = tm;
								break;
							}
						}
					}

					if (foundMember == null)
						continue;

					if (members.ContainsKey(foundMember.MemberInfo))
						continue;

					var converted = arguments[i];

					members.Add(foundMember.MemberInfo, converted);
				}
			}

			expression = GetRemoveNullPropagationTransformer(false).Transform(expression);

			switch (expression.NodeType)
			{
				// new { ... }
				//
				case ExpressionType.New        :
					{
						var expr = (NewExpression)expression;

						if (expr.Members != null)
						{
							for (var i = 0; i < expr.Members.Count; i++)
							{
								var member = expr.Members[i];

								var converted = expr.Arguments[i];
								members.Add(member, converted);

								if (member is MethodInfo info)
									members.Add(info.GetPropertyInfo(), converted);
							}
						}

						var isScalar = MappingSchema.IsScalarType(expr.Type);
						if (!isScalar)
							CollectParameters(expr.Type, expr.Constructor!, expr.Arguments);

						return members.Count > 0 || !isScalar;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr        = (MemberInitExpression)expression;
						var typeMembers = TypeAccessor.GetAccessor(expr.Type).Members;

						var dic  = typeMembers
							.Select(static (m,i) => new { m, i })
							.ToDictionary(static _ => _.m.MemberInfo.Name, static _ => _.i);

						var assignments = new List<(MemberAssignment ma, int order)>();
						foreach (var ma in expr.Bindings.Cast<MemberAssignment>())
							assignments.Add((ma, dic.TryGetValue(ma.Member.Name, out var idx) ? idx : 1000000));

						foreach (var (binding, _) in assignments.OrderBy(static a => a.order))
						{
							var converted = binding.Expression;
							members.Add(binding.Member, converted);

							if (binding.Member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), converted);
						}

						return true;
					}

				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expression;

						// process fabric methods

						if (!MappingSchema.IsScalarType(mc.Type))
							CollectParameters(mc.Type, mc.Method, mc.Arguments);

						return members.Count > 0;
					}

				case ExpressionType.NewArrayInit:
				case ExpressionType.ListInit:
					{
						return true;
					}
				// .Select(p => everything else)
				//
				default                        :
					return false;
			}
		}

		#endregion

		#region CTE

		Dictionary<Expression, CteContext>? _cteContexts;

		public void RegisterCteContext(CteContext cteContext, Expression cteExpression)
		{
			_cteContexts ??= new(ExpressionEqualityComparer.Instance);

			_cteContexts.Add(cteExpression, cteContext);
		}

		public CteContext? FindRegisteredCteContext(Expression cteExpression)
		{
			if (_cteContexts == null)
				return null;

			_cteContexts.TryGetValue(cteExpression, out var cteContext);

			return cteContext;
		}

		#endregion

		#region Query Filter

		Stack<Type[]>? _disabledFilters;

		public void PushDisabledQueryFilters(Type[] disabledFilters)
		{
			_disabledFilters ??= new Stack<Type[]>();
			_disabledFilters.Push(disabledFilters);
		}

		public bool IsFilterDisabled(Type entityType)
		{
			if (_disabledFilters == null || _disabledFilters.Count == 0)
				return false;
			var filter = _disabledFilters.Peek();
			if (filter.Length == 0)
				return true;
			return Array.IndexOf(filter, entityType) >= 0;
		}

		public void PopDisabledFilter()
		{
			if (_disabledFilters == null)
				throw new InvalidOperationException();

			_ = _disabledFilters.Pop();
		}

		#endregion

		#region Grouping Guard

		public bool IsGroupingGuardDisabled { get; set; }

		#endregion

		#region Projection

		public Expression Project(IBuildContext context, Expression? path, List<Expression>? nextPath, int nextIndex, ProjectFlags flags, Expression body, bool strict)
		{
			MemberInfo? member = null;
			Expression? next   = null;

			if (path is MemberExpression memberExpression)
			{
				nextPath ??= new();
				nextPath.Add(memberExpression);

				if (memberExpression.Expression is MemberExpression me)
				{
					// going deeper
					return Project(context, me, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				if (memberExpression.Expression!.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					// going deeper
					return Project(context, ((UnaryExpression)memberExpression.Expression).Operand, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path is SqlGenericParamAccessExpression accessExpression)
			{
				nextPath ??= new();
				nextPath.Add(accessExpression);

				if (accessExpression.Constructor is SqlGenericParamAccessExpression ae)
				{
					// going deeper
					return Project(context, ae, nextPath, nextPath.Count - 1, flags, body, strict);
				}

				// make path projection
				return Project(context, null, nextPath, nextPath.Count - 1, flags, body, strict);
			}

			if (path == null)
			{
				if (nextPath == null || nextIndex < 0)
				{
					if (body == null)
						throw new InvalidOperationException();

					return body;
				}

				next = nextPath[nextIndex];

				if (next is MemberExpression me)
				{
					member = me.Member;
				}
				else if (next is SqlGenericParamAccessExpression paramAccess)
				{
					if (body.NodeType == ExpressionType.New)
					{
						var newExpr = (NewExpression)body;
						if (newExpr.Constructor == paramAccess.ParameterInfo.Member && paramAccess.ParamIndex < newExpr.Arguments.Count)
						{
							return Project(context, null, nextPath, nextIndex - 1, flags,
								newExpr.Arguments[paramAccess.ParamIndex], strict);
						}
					}
					else if (body.NodeType == ExpressionType.Call)
					{
						var methodCall = (MethodCallExpression)body;
						if (methodCall.Method == paramAccess.ParameterInfo.Member && paramAccess.ParamIndex < methodCall.Arguments.Count)
						{
							return Project(context, null, nextPath, nextIndex - 1, flags,
								methodCall.Arguments[paramAccess.ParamIndex], strict);
						}
					}

					// nothing to do right now
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (flags.HasFlag(ProjectFlags.SQL))
			{
				body = RemoveNullPropagation(body, flags.HasFlag(ProjectFlags.Keys));
			}

			if (body is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				body = defaultIfEmptyExpression.InnerExpression;
			}

			switch (body.NodeType)
			{
				case ExpressionType.Extension:
				{
					if (body is SqlPlaceholderExpression placeholder)
					{
						return placeholder;
					}

					if (member != null)
					{
						if (body is ContextRefExpression contextRef)
						{
							var objExpression   = body;
							var memberCorrected = contextRef.Type.GetMemberEx(member);
							if (memberCorrected  is null)
							{
								// inheritance handling
								if (member.DeclaringType != null &&
									contextRef.Type.IsSameOrParentOf(member.DeclaringType))
								{
									memberCorrected = member;
									objExpression   = Expression.Convert(objExpression, member.DeclaringType);
								}
								else
								{
									return next!;
								}
							}

							var ma      = Expression.MakeMemberAccess(objExpression, memberCorrected);
							var newPath = nextPath![0].Replace(next!, ma);

							return newPath;
						}

						if (body.IsNullValue())
						{
							return new DefaultValueExpression(MappingSchema, member.GetMemberType(), true);
						}

						if (body is SqlAdjustTypeExpression adjustType)
						{
							return Project(context, path, nextPath, nextIndex, flags, adjustType.Expression, strict);
						}

						if (body is SqlGenericConstructorExpression genericConstructor)
						{
							Expression? bodyExpresion = null;
							for (int i = 0; i < genericConstructor.Assignments.Count; i++)
							{
								var assignment = genericConstructor.Assignments[i];
								if (MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, member))
								{
									bodyExpresion = assignment.Expression;
									break;
								}
							}

							if (bodyExpresion == null)
							{
								for (int i = 0; i < genericConstructor.Parameters.Count; i++)
								{
									var parameter = genericConstructor.Parameters[i];
									if (MemberInfoEqualityComparer.Default.Equals(parameter.MemberInfo, member))
									{
										bodyExpresion = parameter.Expression;
										break;
									}
								}
							}

							if (bodyExpresion == null)
							{
								// search in base class
								for (int i = 0; i < genericConstructor.Assignments.Count; i++)
								{
									var assignment = genericConstructor.Assignments[i];
									if (assignment.MemberInfo.ReflectedType != member.ReflectedType && assignment.MemberInfo.Name == member.Name)
									{
										var mi = assignment.MemberInfo.ReflectedType!.GetMemberEx(member);
										if (mi != null && mi.GetMemberType() == member.GetMemberType() && MemberInfoEqualityComparer.Default.Equals(assignment.MemberInfo, mi))
										{
											if (member.ReflectedType?.IsInterface == true && assignment.MemberInfo.ReflectedType?.IsClass == true && member is PropertyInfo propInfo && assignment.MemberInfo is PropertyInfo classPropinfo)
											{
												// Validating that interface property is pointing to the correct class property

												var interfaceMap               = assignment.MemberInfo.ReflectedType.GetInterfaceMapEx(member.ReflectedType);
												var interfacePropertyGetMethod = propInfo.GetGetMethod();
												var classPropertyGetMethod     = classPropinfo.GetGetMethod();

												var methodIndex             = Array.IndexOf(interfaceMap.InterfaceMethods, interfacePropertyGetMethod);
												var classImplementingMethod = interfaceMap.TargetMethods[methodIndex];

												var isBackingProperty = classPropertyGetMethod == classImplementingMethod;

												if (!isBackingProperty)
													continue;
											}

											bodyExpresion = assignment.Expression;
											break;
										}
									}
								}
							}

							if (bodyExpresion is not null)
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, bodyExpresion, strict);
							}

							if (strict)
								return CreateSqlError(nextPath![0]);

							return new DefaultValueExpression(null, nextPath![0].Type, true);
						}
					}

					if (next is SqlGenericParamAccessExpression paramAccessExpression)
					{

						/*
						var projected = Project(context, path, nextPath, nextIndex - 1, flags,
							paramAccessExpression);

						return projected;
						*/

						if (body is SqlGenericConstructorExpression constructorExpression)
						{
							var projected = Project(context, path, nextPath, nextIndex - 1, flags,
								constructorExpression.Parameters[paramAccessExpression.ParamIndex].Expression, strict);
							return projected;
						}

						//throw new InvalidOperationException();
					}

					return body;
				}

				case ExpressionType.MemberAccess:
				{
					if (member != null && nextPath != null)
					{
						if (nextPath[nextIndex] is MemberExpression nextMember && body.Type.IsSameOrParentOf(nextMember.Expression!.Type))
						{
							var newMember = body.Type.GetMemberEx(nextMember.Member);
							if (newMember != null)
							{
								var newMemberAccess = Expression.MakeMemberAccess(body, newMember);
								return Project(context, path, nextPath, nextIndex - 1, flags, newMemberAccess, strict);
							}
						}
					}

					break;
				}

				case ExpressionType.New:
				{
					var ne = (NewExpression)body;

					if (ne.Members != null)
					{
						if (member == null)
						{
							break;
						}

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								var projected = Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);

								// set alias
								if (projected is ContextRefExpression contextRef)
								{
									contextRef.BuildContext.SetAlias(member.Name);
								}

								return projected;
							}
						}
					}
					else
					{
						var parameters = ne.Constructor!.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(ne.Constructor.DeclaringType!, parameter);

							if (memberByParam != null &&
								MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					if (member == null)
						return ne;

					if (strict)
						return CreateSqlError(nextPath![0]);

					return new DefaultValueExpression(MappingSchema, nextPath![0].Type, true);
				}

				case ExpressionType.MemberInit:
				{
					var mi = (MemberInitExpression)body;
					var ne = mi.NewExpression;

					if (member == null)
					{
						if (next is SqlGenericParamAccessExpression paramAccess)
						{
							if (paramAccess.ParamIndex >= ne.Arguments.Count)
								return CreateSqlError(nextPath![0]);

							return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[paramAccess.ParamIndex], strict);
						}

						throw new NotImplementedException($"Projecting '{next}' is not supported yet.");
					}

					if (ne.Members != null)
					{

						for (var i = 0; i < ne.Members.Count; i++)
						{
							var memberLocal = ne.Members[i];

							if (MemberInfoEqualityComparer.Default.Equals(memberLocal, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
							}
						}
					}

					var memberInType = body.Type.GetMemberEx(member);
					if (memberInType == null)
					{
						if (member.DeclaringType?.IsSameOrParentOf(body.Type) == true)
							memberInType = member;
					}

					if (memberInType != null)
					{
						for (int index = 0; index < mi.Bindings.Count; index++)
						{
							var binding = mi.Bindings[index];
							switch (binding.BindingType)
							{
								case MemberBindingType.Assignment:
								{
									var assignment = (MemberAssignment)binding;
									if (MemberInfoEqualityComparer.Default.Equals(assignment.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											assignment.Expression, strict);
									}

									break;
								}
								case MemberBindingType.MemberBinding:
								{
									var memberMemberBinding = (MemberMemberBinding)binding;
									if (MemberInfoEqualityComparer.Default.Equals(memberMemberBinding.Member, memberInType))
									{
										return Project(context, path, nextPath, nextIndex - 1, flags,
											new SqlGenericConstructorExpression(
												memberMemberBinding.Member.GetMemberType(),
												memberMemberBinding.Bindings), strict);
									}

									break;
								}
								case MemberBindingType.ListBinding:
									throw new NotImplementedException();
								default:
									throw new NotImplementedException();
							}
						}

						if (ne.Constructor != null && ne.Arguments.Count > 0)
						{
							var parameters = ne.Constructor.GetParameters();
							for (int i = 0; i < ne.Arguments.Count; i++)
							{
								var parameter     = parameters[i];
								var memberByParam = SqlGenericConstructorExpression.FindMember(ne.Constructor.DeclaringType!, parameter);

								if (memberByParam != null &&
									MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
								{
									return Project(context, path, nextPath, nextIndex - 1, flags, ne.Arguments[i], strict);
								}

							}
						}
					}

					if (strict)
						return CreateSqlError(nextPath![0]);

					return new DefaultValueExpression(MappingSchema, nextPath![0].Type, true);

				}
				case ExpressionType.Conditional:
				{
					var cond      = (ConditionalExpression)body;
					var trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, strict);
					var falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, strict);

					var trueHasError = trueExpr is SqlErrorExpression;
					var falseHasError = falseExpr is SqlErrorExpression;

					if (strict && (trueHasError || falseHasError))
					{
						if (trueHasError == falseHasError)
						{
							return trueExpr;
						}

						trueExpr  = Project(context, null, nextPath, nextIndex, flags, cond.IfTrue, false);
						falseExpr = Project(context, null, nextPath, nextIndex, flags, cond.IfFalse, false);
					}

					if (trueExpr is SqlErrorExpression || falseExpr is SqlErrorExpression)
					{
						break;
					}

					if (trueExpr.Type != falseExpr.Type)
					{
						if (trueExpr.IsNullValue())
							trueExpr = new DefaultValueExpression(MappingSchema, falseExpr.Type, true);
						else if (falseExpr.IsNullValue())
							falseExpr = new DefaultValueExpression(MappingSchema, trueExpr.Type, true);
					}

					var newExpr = (Expression)Expression.Condition(cond.Test, trueExpr, falseExpr);

					return newExpr;
				}

				case ExpressionType.Constant:
				{
					var cnt = (ConstantExpression)body;
					if (cnt.Value == null)
					{
						var expr = (path ?? next)!;

						return new DefaultValueExpression(MappingSchema, expr.Type, true);
					}

					break;

				}

				case ExpressionType.Default:
				{
					var expr = (path ?? next)!;

					return new DefaultValueExpression(MappingSchema, expr.Type, false);
				}

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)body;

					if (mc.Method.IsStatic)
					{
						if (mc.Method.Name == nameof(Sql.Alias) && mc.Method.DeclaringType == typeof(Sql))
						{
							return Project(context, path, nextPath, nextIndex, flags, mc.Arguments[0], strict);
						}

						var parameters = mc.Method.GetParameters();

						for (var i = 0; i < parameters.Length; i++)
						{
							var parameter     = parameters[i];
							var memberByParam = SqlGenericConstructorExpression.FindMember(mc.Method.ReturnType, parameter);

							if (memberByParam != null &&
								MemberInfoEqualityComparer.Default.Equals(memberByParam, member))
							{
								return Project(context, path, nextPath, nextIndex - 1, flags, mc.Arguments[i], strict);
							}
						}
					}

					/*if (member != null)
					{
						var ma = Expression.MakeMemberAccess(mc, member);
						return Project(context, path, nextPath, nextIndex - 1, flags, ma, strict);
					}*/

					return new SqlErrorExpression(mc);
				}

				case ExpressionType.TypeAs:
				{
					var unary = (UnaryExpression)body;

					var truePath = Project(context, path, nextPath, nextIndex, flags, unary.Operand, strict);

					var isPredicate = MakeIsPredicateExpression(context, Expression.TypeIs(unary.Operand, unary.Type));

					if (isPredicate is ConstantExpression constExpr)
					{
						if (constExpr.Value is true)
							return truePath;
						return new DefaultValueExpression(MappingSchema, truePath.Type, true);
					}

					var falsePath = Expression.Constant(null, truePath.Type);

					var conditional = Expression.Condition(isPredicate, truePath, falsePath);

					return conditional;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var unaryExpression = (UnaryExpression)body;

					if (unaryExpression.Operand is ContextRefExpression contextRef)
					{
						contextRef = contextRef.WithType(unaryExpression.Type);
						return Project(context, path, nextPath, nextIndex, flags, contextRef, strict);
					}

					return Project(context, path, nextPath, nextIndex, flags, unaryExpression.Operand, strict);
				}
			}

			return CreateSqlError(next!);
		}

		public Expression ParseGenericConstructor(Expression createExpression, ProjectFlags flags, ColumnDescriptor? columnDescriptor, bool force = false)
		{
			if (createExpression.Type.IsNullable())
				return createExpression;

			if (!force && createExpression.Type.IsValueType)
				return createExpression;

			if (!force && MappingSchema.IsScalarType(createExpression.Type))
				return createExpression;

			if (typeof(FormattableString).IsSameOrParentOf(createExpression.Type))
				return createExpression;

			if (flags.IsSql() && IsForceParameter(createExpression, columnDescriptor))
				return createExpression;

			switch (createExpression.NodeType)
			{
				case ExpressionType.New:
				{
					return new SqlGenericConstructorExpression((NewExpression)createExpression);
				}

				case ExpressionType.MemberInit:
				{
					return new SqlGenericConstructorExpression((MemberInitExpression)createExpression);
				}

				case ExpressionType.Call:
				{
					//TODO: Do we still need Alias?
					var mc = (MethodCallExpression)createExpression;
					if (mc.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
						return ParseGenericConstructor(mc.Arguments[0], flags, columnDescriptor);

					if (mc.IsQueryable())
						return mc;

					if (!mc.Method.IsStatic)
						break;

					if (mc.Method.IsSqlPropertyMethodEx() || mc.IsSqlRow() || mc.Method.DeclaringType == typeof(string))
						break;

					return new SqlGenericConstructorExpression(mc);
				}
			}

			return createExpression;
		}

		public SqlPlaceholderExpression MakeColumn(SelectQuery? parentQuery, SqlPlaceholderExpression sqlPlaceholder, bool asNew = false)
		{
			return _buildVisitor.MakeColumn(parentQuery, sqlPlaceholder, asNew);
		}

		#endregion
	}
}
