using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Conversion;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using static LinqToDB.Linq.Translation.ITranslationContext;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class ExpressionBuildVisitor : ExpressionVisitorBase
	{
		public ExpressionBuilder Builder { get; }

		BuildPurpose               _buildPurpose;
		BuildFlags                 _buildFlags;
		readonly Stack<Expression> _disableSubqueries = new();
		NewExpression?             _disableNew;
		bool                       _preferClientSide;
		NullabilityContext?        _nullabilityContext;

		public string?           Alias             { get; private set; }
		public ColumnDescriptor? CurrentDescriptor { get; private set; }

		public IBuildContext? BuildContext
		{
			get;
			private set
			{
				if (ReferenceEquals(field, value))
					return;

				field = value;
				_nullabilityContext = null;
			}
		}

		// Caches
		SnapshotDictionary<ExprCacheKey, Expression>?                _associations;
		SnapshotDictionary<ExprCacheKey, Expression>                 _translationCache             = new(ExprCacheKey.SqlCacheKeyComparer);
		SnapshotDictionary<ColumnCacheKey, SqlPlaceholderExpression> _columnCache                  = new(ColumnCacheKey.ColumnCacheKeyComparer);

		public ExpressionBuildVisitor(ExpressionBuilder builder)
		{
			Builder = builder;
			_buildPurpose = BuildPurpose.Expression;
		}

		public MappingSchema MappingSchema => BuildContext?.MappingSchema ?? Builder.MappingSchema;
		public DataOptions DataOptions => Builder.DataOptions;

		ContextRefExpression? FoundRoot { get; set; }

		static bool HasClonedContext(Expression? expression, CloningContext cloningContext)
		{
			return expression != null &&
				   null != expression.Find(cloningContext, static (cc, e) => e is ContextRefExpression contextRef && cc.IsCloned(contextRef.BuildContext));
		}

		public ExpressionBuildVisitor Clone(CloningContext cloningContext)
		{
			var translationCache = _translationCache
				.Where(p => HasClonedContext(p.Key.Expression, cloningContext))
				.ToDictionary(
					p =>
						new ExprCacheKey(
							cloningContext.CorrectExpression(p.Key.Expression),
							cloningContext.CorrectContext(p.Key.Context), p.Key.ColumnDescriptor,
							cloningContext.CorrectElement(p.Key.SelectQuery), p.Key.Flags),
					p => cloningContext.CorrectExpression(p.Value),
					ExprCacheKey.SqlCacheKeyComparer
				);

			var columnCache = _columnCache
				.Where(p => cloningContext.IsCloned(p.Key.SelectQuery) || HasClonedContext(p.Key.Expression, cloningContext))
				.ToDictionary(
					p =>
						new ColumnCacheKey(
							cloningContext.CorrectExpression(p.Key.Expression),
							p.Key.ResultType,
							cloningContext.CorrectElement(p.Key.SelectQuery),
							cloningContext.CorrectElement(p.Key.ParentQuery)),
					p => cloningContext.CorrectExpression(p.Value),
					ColumnCacheKey.ColumnCacheKeyComparer
				);

			var associations = _associations?
				.Where(p => HasClonedContext(p.Key.Expression, cloningContext))
				.ToDictionary(
					p =>
						new ExprCacheKey(
							cloningContext.CorrectExpression(p.Key.Expression),
							cloningContext.CorrectContext(p.Key.Context), p.Key.ColumnDescriptor,
							cloningContext.CorrectElement(p.Key.SelectQuery), p.Key.Flags),

					p => cloningContext.CorrectExpression(p.Value),
					ExprCacheKey.SqlCacheKeyComparer
				);

			var newVisitor = new ExpressionBuildVisitor(Builder);
			newVisitor._associations = associations == null ? null : new SnapshotDictionary<ExprCacheKey, Expression>(associations);
			newVisitor._translationCache = new(translationCache);
			newVisitor._columnCache = new(columnCache);

			return newVisitor;
		}

		public sealed class CacheSnapshot : IDisposable
		{
			readonly ExpressionBuildVisitor _visitor;

			readonly SnapshotDictionary<ExprCacheKey, Expression>?                _savedAssociations;
			readonly SnapshotDictionary<ExprCacheKey, Expression>                 _savedTranslationCache;
			readonly SnapshotDictionary<ColumnCacheKey, SqlPlaceholderExpression> _savedColumnCache;
			bool                                                                  _isAccepted;

			public CacheSnapshot(ExpressionBuildVisitor visitor)
			{
				_visitor = visitor;

				_savedAssociations = visitor._associations;
				_savedTranslationCache = visitor._translationCache;
				_savedColumnCache = visitor._columnCache;

				_savedAssociations?.TakeSnapshot();
				_savedTranslationCache.TakeSnapshot();
				_savedColumnCache.TakeSnapshot();
			}

			public void Accept()
			{
				_isAccepted = true;
			}

			public void Dispose()
			{
				if (_isAccepted)
				{
					_savedAssociations?.Commit();
					_savedTranslationCache.Commit();
					_savedColumnCache.Commit();
				}
				else
				{
					_savedAssociations?.Rollback();
					if (_savedAssociations == null)
					{
						_visitor._associations = null;
					}

					_savedTranslationCache.Rollback();
					_savedColumnCache.Rollback();
				}
			}
		}

		[DebuggerDisplay("Saved: {_savedState}")]
		public readonly struct StateHolder<TState> : IDisposable
		{
			readonly ExpressionBuildVisitor                 _visitor;
			readonly Action<ExpressionBuildVisitor, TState> _stateSetter;
			readonly TState                                 _savedState;

			[DebuggerStepThrough]
			public StateHolder(ExpressionBuildVisitor visitor, TState state, Func<ExpressionBuildVisitor, TState> stateAccessor, Action<ExpressionBuildVisitor, TState> stateSetter)
			{
				_visitor = visitor;
				_stateSetter = stateSetter;
				_savedState = stateAccessor(visitor);
				_stateSetter(visitor, state);
			}

			[DebuggerStepThrough]
			public void Dispose()
			{
				_stateSetter(_visitor, _savedState);
			}
		}

		public StateHolder<BuildPurpose> UsingBuildPurpose(BuildPurpose buildPurpose)
		{
			return new StateHolder<BuildPurpose>(this, buildPurpose, static v => v._buildPurpose, static (v, f) => v._buildPurpose = f);
		}

		public StateHolder<BuildFlags> UsingBuildFlags(BuildFlags buildFlags)
		{
			return new StateHolder<BuildFlags>(this, buildFlags, static v => v._buildFlags, static (v, f) => v._buildFlags = f);
		}

		public StateHolder<BuildFlags> CombineBuildFlags(BuildFlags buildFlags)
		{
			return new StateHolder<BuildFlags>(this, CombineFlags(_buildFlags, buildFlags), static v => v._buildFlags, static (v, f) => v._buildFlags = f);
		}

		public StateHolder<string?> UsingAlias(string? alias)
		{
			return new StateHolder<string?>(this, alias, static v => v.Alias, static (v, f) => v.Alias = f);
		}

		public StateHolder<IBuildContext?> UsingBuildContext(IBuildContext? buildContext)
		{
			return new StateHolder<IBuildContext?>(this, buildContext, static v => v.BuildContext, static (v, f) => v.BuildContext = f);
		}

		public StateHolder<ColumnDescriptor?> UsingColumnDescriptor(ColumnDescriptor? columnDescriptor)
		{
			return new StateHolder<ColumnDescriptor?>(this, columnDescriptor, static v => v.CurrentDescriptor, static (v, f) => v.CurrentDescriptor = f);
		}

		public StateHolder<NewExpression?> UsingDisableNew(NewExpression? disableNew)
		{
			return new StateHolder<NewExpression?>(this, disableNew, static v => v._disableNew, static (v, f) => v._disableNew = f);
		}

		public StateHolder<bool> UsingPreferClientSide(bool preferClientSide)
		{
			return new StateHolder<bool>(this, preferClientSide, static v => v._preferClientSide, static (v, f) => v._preferClientSide = f);
		}

		static BuildFlags CombineFlags(BuildFlags currentFlags, BuildFlags additional)
		{
			if (additional.HasFlag(BuildFlags.ResetPrevious))
				return additional & ~BuildFlags.ResetPrevious;
			return currentFlags | additional;
		}

#pragma warning disable RS0059 // Do not add multiple public overloads with optional parameters
		public Expression BuildExpression(IBuildContext? buildContext, Expression expression, BuildPurpose buildPurpose, BuildFlags buildFlags = BuildFlags.None, string? alias = null)
#pragma warning restore RS0059 // Do not add multiple public overloads with optional parameters
		{
			using (UsingBuildPurpose(buildPurpose))
			using (UsingBuildContext(buildContext))
			using (UsingAlias(alias ?? Alias))
			using (CombineBuildFlags(buildFlags))
			{
				var result = Visit(expression);

				return result;
			}
		}

#pragma warning disable RS0059 // Do not add multiple public overloads with optional parameters
		public Expression BuildExpression(IBuildContext? buildContext, Expression expression, BuildFlags buildFlags = BuildFlags.None, string? alias = null)
#pragma warning restore RS0059 // Do not add multiple public overloads with optional parameters
		{
			using (UsingBuildContext(buildContext))
			using (UsingAlias(alias ?? Alias))
			using (CombineBuildFlags(buildFlags))
			{
				var result = Visit(expression);

				return result;
			}
		}

#pragma warning disable RS0059 // Do not add multiple public overloads with optional parameters
		public Expression BuildExpression(Expression expression, BuildPurpose buildPurpose, BuildFlags? buildFlags = null)
#pragma warning restore RS0059 // Do not add multiple public overloads with optional parameters
		{
			using (UsingBuildPurpose(buildPurpose))
			using (CombineBuildFlags(buildFlags ?? _buildFlags))
			{
				var result = Visit(expression);

				return result;
			}
		}

		public Expression BuildExpression(Expression expression, BuildPurpose buildPurpose)
		{
			using (UsingBuildPurpose(buildPurpose))
			{
				var result = Visit(expression);

				return result;
			}
		}

		public Expression BuildSqlExpression(Expression expression)
		{
			var result = BuildExpression(expression, BuildPurpose.Sql);
			if (BuildContext != null)
				result = Builder.UpdateNesting(BuildContext, result);
			return result;
		}

		public Expression BuildSqlExpression(IBuildContext context, Expression expression)
		{
			var result = BuildExpression(context, expression, BuildPurpose.Sql);
			result = Builder.UpdateNesting(context, result);
			return result;
		}

		public Expression BuildExpression(IBuildContext? context, Expression expression)
		{
			using (UsingBuildContext(context))
			{
				var result = Visit(expression);

				return result;
			}
		}

		public Expression BuildExpression(IBuildContext? context, Expression expression, BuildPurpose buildPurpose)
		{
			using (UsingBuildPurpose(buildPurpose))
			using (UsingBuildContext(context))
			{
				var result = Visit(expression);

				return result;
			}
		}

		public Expression BuildRoot(Expression expression)
		{
			FoundRoot = null;
			return BuildExpression(expression, BuildPurpose.Root);
		}

		public Expression BuildAssociationRoot(Expression expression)
		{
			FoundRoot = null;
			return BuildExpression(expression, BuildPurpose.AssociationRoot);
		}

		public Expression BuildAggregationRoot(Expression expression)
		{
			FoundRoot = null;
			return BuildExpression(expression, BuildPurpose.AggregationRoot);
		}

		static bool IsSame(Expression expr1, Expression expr2)
		{
			return ExpressionEqualityComparer.Instance.Equals(expr1, expr2);
		}

		bool HandleSqlRelated(Expression node, [NotNullWhen(true)] out Expression? translated)
		{
			translated = null;
			if (BuildContext == null || _buildPurpose is not (BuildPurpose.Sql or BuildPurpose.Expression))
			{
				return false;
			}

			ISqlExpression? sql = null;

			if (typeof(ISqlExpression).IsSameOrParentOf(node.Type))
			{
				var valid = true;
				if (node is MethodCallExpression mc)
				{
					var type = mc.Object?.Type ?? mc.Method.DeclaringType;
					if (type != null && MappingSchema.HasAttribute<Sql.ExpressionAttribute>(type, mc.Method))
						valid = false;
				}
				else if (node is MemberExpression me)
				{
					var type = me.Expression?.Type ?? me.Member.DeclaringType;
					if (type != null && MappingSchema.HasAttribute<Sql.ExpressionAttribute>(type, me.Member))
						valid = false;
				}

				if (valid)
					sql = ConvertToInlinedSqlExpression(BuildContext, node);
			}
			else if (typeof(IToSqlConverter).IsSameOrParentOf(node.Type))
			{
				sql = ConvertToSqlConvertible(BuildContext, node);
			}

			if (sql != null)
			{
				translated = ExpressionBuilder.CreatePlaceholder(BuildContext, sql, node, alias: Alias);
				return true;
			}

			return false;
		}

		ISqlExpression? ConvertToInlinedSqlExpression(IBuildContext? context, Expression newExpr)
		{
			var innerSql = Builder.EvaluateExpression<ISqlExpression>(newExpr);
			if (innerSql == null)
				return null;

			var param = Builder.ParametersContext.BuildParameter(context, newExpr, null, doNotCheckCompatibility : true);
			if (param == null)
				return null;

			return new SqlInlinedSqlExpression(param, innerSql);
		}

		ISqlExpression? ConvertToSqlConvertible(IBuildContext? context, Expression expression)
		{
			if (Builder.EvaluateExpression(Expression.Convert(expression, typeof(IToSqlConverter))) is not IToSqlConverter converter)
				throw new LinqToDBException($"Expression '{expression}' cannot be converted to `IToSqlConverter`");

			var innerExpr = converter.ToSql(converter);

			var param = Builder.ParametersContext.BuildParameter(context, expression, null, doNotCheckCompatibility : true);
			if (param == null)
				return null;

			return new SqlInlinedToSqlExpression(param, innerExpr);
		}

		Expression MakeWithCache(IBuildContext context, Expression expression)
		{
			var flags = GetProjectFlags();

			var cacheKey = new ExprCacheKey(expression, context, null, null, flags);

			if (GetAlreadyTranslated(cacheKey, out var translated))
			{
				return translated;
			}

			var saveRoot = FoundRoot;

			Builder.PushTranslationModifier(context.TranslationModifier, true);

			translated = context.MakeExpression(expression, flags);

			Builder.PopTranslationModifier();

			FoundRoot = saveRoot;

#if DEBUG
			if (!IsSame(translated, expression))
			{
				Debug.WriteLine($"--> Translated: {expression} => {translated}");
			}
#endif

			if (!Builder.IsUnderRecursiveBuild(expression))
			{
				if (!_translationCache.ContainsKey(cacheKey))
					_translationCache.Add(cacheKey, translated);
			}

			return translated;
		}

		ProjectFlags GetProjectFlags()
		{
			var flags = ProjectFlags.None;

			switch (_buildPurpose)
			{
				case BuildPurpose.Sql:
					flags |= ProjectFlags.SQL;
					if (_buildFlags.HasFlag(BuildFlags.ForKeys))
						flags |= ProjectFlags.Keys;
					break;
				case BuildPurpose.Expression:
					flags |= ProjectFlags.Expression;
					if (_buildFlags.HasFlag(BuildFlags.ForKeys))
						flags |= ProjectFlags.Keys;
					break;
				case BuildPurpose.Root:
					flags |= ProjectFlags.Root;
					break;
				case BuildPurpose.AssociationRoot:
					flags |= ProjectFlags.AssociationRoot;
					break;
				case BuildPurpose.AggregationRoot:
					flags |= ProjectFlags.AggregationRoot;
					break;
				case BuildPurpose.SubQuery:
					flags |= ProjectFlags.Subquery;
					break;
				case BuildPurpose.Extract:
					flags |= ProjectFlags.ExtractProjection;
					if (_buildFlags.HasFlag(BuildFlags.ForKeys))
						flags |= ProjectFlags.Keys;
					break;
				case BuildPurpose.Traverse:
					flags |= ProjectFlags.Traverse;
					break;
				case BuildPurpose.Table:
					flags |= ProjectFlags.Table;
					break;
				case BuildPurpose.Expand:
					flags |= ProjectFlags.Expand;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (_buildFlags.HasFlag(BuildFlags.ForExpanding))
				flags |= ProjectFlags.Expand;

			if (_buildFlags.HasFlag(BuildFlags.ForMemberRoot))
				flags |= ProjectFlags.MemberRoot;

			if (_buildFlags.HasFlag(BuildFlags.ForSetProjection))
				flags |= ProjectFlags.ForSetProjection;

			return flags;
		}

		public ContextRefExpression? GetCacheRootContext(Expression expression)
		{
			if (expression is MemberExpression { Expression: { } expr })
			{
				return GetCacheRootContext(expr);
			}

			if (expression is MethodCallExpression methodCallExpression && methodCallExpression.IsQueryable())
			{
				return GetCacheRootContext(methodCallExpression.Arguments[0]);
			}

			return expression as ContextRefExpression;
		}

		public ContextRefExpression? GetAggregationRootContext(Expression expression)
		{
			if (expression is MemberExpression { Expression: { } expr })
			{
				return GetCacheRootContext(expr);
			}

			if (expression is MethodCallExpression methodCallExpression && methodCallExpression.IsQueryable())
			{
				return GetCacheRootContext(methodCallExpression.Arguments[0]);
			}

			return expression as ContextRefExpression;
		}

		[Conditional("DEBUG")]
		void DebugCacheHit(ExprCacheKey cacheKey, Expression translated)
		{
			//Debug.WriteLine($"Cache hit: {cacheKey.Expression} ({cacheKey.Flags}) => {translated}");
		}

		ExprCacheKey GetSqlCacheKey(Expression path)
		{
			if (FoundRoot == null)
				throw new InvalidOperationException("Called when root is not initialized.");

			return GetSqlCacheKey(path, FoundRoot.BuildContext.SelectQuery);
		}

		ExprCacheKey GetSqlCacheKey(Expression path, SelectQuery selectQuery)
		{
			return new ExprCacheKey(path, null, CurrentDescriptor, selectQuery, ProjectFlags.SQL);
		}

		Expression RegisterTranslatedSql(Expression translated, Expression path)
		{
			if (FoundRoot != null)
			{
				translated = RegisterTranslatedSql(FoundRoot.BuildContext.SelectQuery, translated, path);
			}

			return translated;
		}

		static bool HasConstant(Expression expression)
		{
			return expression.Find(static e => e.NodeType == ExpressionType.Constant) != null;
		}

		Expression RegisterTranslatedSql(SelectQuery selectQuery, Expression translated, Expression path)
		{
			if (translated is SqlPlaceholderExpression placeholder)
			{
				if (Alias != null)
				{
					placeholder = placeholder.WithAlias(Alias);
				}

				translated = placeholder.WithTrackingPath(path);

				var cacheKey = GetSqlCacheKey(path, selectQuery);

				if (!(placeholder.Sql.HasParameter() && HasConstant(placeholder.Path)))
				{
					if (!_translationCache.ContainsKey(cacheKey))
						_translationCache.Add(cacheKey, translated);
				}
			}

			return translated;
		}

		bool GetAlreadyTranslated(SelectQuery selectQuery, Expression path, [NotNullWhen(true)] out Expression? translated)
		{
			var cacheKey = GetSqlCacheKey(path, selectQuery);

			return GetAlreadyTranslated(cacheKey, out translated);
		}

		bool GetAlreadyTranslated(ExprCacheKey cacheKey, [NotNullWhen(true)] out Expression? translated)
		{
			if (_translationCache.TryGetValue(cacheKey, out translated))
			{
				if (cacheKey.Flags == ProjectFlags.SQL && _buildPurpose is BuildPurpose.Expression && SequenceHelper.HasError(translated))
				{
					// If we have error in translated expression, we should not use it.
					translated = null;
					return false;
				}

				DebugCacheHit(cacheKey, translated);
				return true;
			}

			return false;
		}

#if DEBUG
		[DebuggerStepThrough]
		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			var newNode = base.Visit(node);

			/*
			if (newNode != null && node != null && !IsSame(newNode, node!))
			{
				Debug.WriteLine($"--> Node  {_buildPurpose}, {_buildFlags}, {node.NodeType}, \t {node} \t\t -> {newNode}");
			}
			*/

			return newNode;
		}
#endif

		[Conditional("DEBUG")]
		public void LogVisit(Expression node, [CallerMemberName] string callerName = "")
		{
			//Debug.WriteLine($"{callerName}: {_buildPurpose}, {_buildFlags}, {node}");
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			var shouldProcess = _buildPurpose is BuildPurpose.Extract or BuildPurpose.Expand || _buildFlags.HasFlag(BuildFlags.ForExpanding);

			if (!shouldProcess)
				return node;

			using (UsingColumnDescriptor(null))
			{
				var newNode = base.VisitLambda(node);

				FoundRoot = null;

				return newNode;
			}
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
		{
			if (IsRootExpression())
				return node;

			var columnDescriptor = node.Member.DeclaringType != null
				? MappingSchema.GetEntityDescriptor(node.Member.DeclaringType).FindColumnDescriptor(node.Member)
				: CurrentDescriptor;

			using (UsingAlias(node.Member.Name))
			using (UsingColumnDescriptor(columnDescriptor))
			{
				var newNode = base.VisitMemberAssignment(node);

				if (newNode.Expression is SqlPlaceholderExpression { Alias: null } placeholder)
				{
					newNode = newNode.Update(placeholder.WithAlias(Alias));
				}

				return newNode;
			}
		}

		internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
		{
			if (IsRootExpression())
				return assignment;

			var columnDescriptor = assignment.MemberInfo.DeclaringType != null
				? MappingSchema.GetEntityDescriptor(assignment.MemberInfo.DeclaringType).FindColumnDescriptor(assignment.MemberInfo)
				: CurrentDescriptor;

			using (UsingAlias(assignment.MemberInfo.Name))
			using (UsingColumnDescriptor(columnDescriptor))
			{
				SqlGenericConstructorExpression.Assignment? newNode = null;
				if (BuildContext != null && IsSqlOrExpression())
				{
					if (TryConvertToSql(assignment.Expression, out var translated))
					{
						newNode = assignment.WithExpression(Visit(translated));
					}
				}

				if (newNode == null)
				{
					newNode = base.VisitSqlGenericAssignment(assignment);
				}

				return newNode;
			}
		}

		protected override Expression VisitListInit(ListInitExpression node)
		{
			using (UsingDisableNew(node.NewExpression))
			{
				var newExpression = base.VisitListInit(node);
				FoundRoot = null;

				return newExpression;
			}
		}

		public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			var newNode = base.VisitSqlGenericConstructorExpression(node);
			FoundRoot = null;

			if (!IsSame(newNode, node))
				return Visit(newNode);

			return node;
		}

		internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
		{
			var columnDescriptor = parameter.MemberInfo?.DeclaringType != null
				? MappingSchema.GetEntityDescriptor(parameter.MemberInfo.DeclaringType).FindColumnDescriptor(parameter.MemberInfo)
				: CurrentDescriptor;

			using (UsingAlias(parameter.MemberInfo?.Name ?? parameter.ParameterInfo.Name))
			using (UsingColumnDescriptor(columnDescriptor))
			{
				var newNode = base.VisitSqlGenericParameter(parameter);

				return newNode;
			}
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			LogVisit(node);

			if (_buildPurpose is BuildPurpose.Traverse)
			{
				var newNode = base.VisitMethodCall(node);
				FoundRoot = null;
				return newNode;
			}

			if (node.Method.DeclaringType == typeof(Sql))
			{
				switch (node.Method.Name)
				{
					case nameof(Sql.Alias):
					{
						using (UsingAlias(Builder.EvaluateExpression<string>(node.Arguments[1])))
						{
							var translated = Visit(node.Arguments[0]);
							return RegisterTranslatedSql(translated, node);
						}
					}

					case nameof(Sql.Parameter) when IsSqlOrExpression():
					{
						using (CombineBuildFlags(BuildFlags.ForceParameter))
						{
							var translated = Visit(node.Arguments[0]);
							return RegisterTranslatedSql(translated, node);
						}
					}

					case nameof(Sql.Constant) when IsSqlOrExpression():
					{
						using (CombineBuildFlags(BuildFlags.ForceParameter))
						{
							if (HandleValue(node.Arguments[0], out var translated))
							{
								if (translated is SqlPlaceholderExpression { Sql: SqlParameter sqlParameter })
								{
									sqlParameter.IsQueryParameter = false;
								}

								translated = RegisterTranslatedSql(translated, node);
								return translated;
							}

							return node;
						}
					}
				}
			}

			if (Builder.IsAssociation(node, out _))
			{
				Expression? root;

				if (node.Object != null)
				{
					root = BuildAssociationRoot(node.Object);
					if (!IsSame(root, node.Object))
					{
						return Visit(node.Update(root, node.Arguments));
					}
				}
				else
				{
					root = BuildAssociationRoot(node.Arguments[0]);
					if (!IsSame(root, node.Arguments[0]))
					{
						var newArguments = node.Arguments.ToArray();
						newArguments[0] = root;
						return Visit(node.Update(node.Object, newArguments));
					}
				}

				if (FoundRoot != null && FoundRoot.BuildContext.AutomaticAssociations)
				{
					var association = TryCreateAssociation(node, FoundRoot, BuildContext);
					if (!IsSame(association, node))
						return Visit(association);
				}
			}
			else
			{
				if (_buildPurpose is BuildPurpose.AggregationRoot or BuildPurpose.AssociationRoot)
					return node;

				if (node.Type == typeof(bool))
				{
					var translatedPredicate = ConvertPredicateMethod(node);
					if (!IsSame(translatedPredicate, node))
						return Visit(translatedPredicate);
				}

				if (HandleSubquery(node, out var translated))
					return Visit(translated);
			}

			if (IsSqlOrExpression() && BuildContext != null)
			{
				var exposed = Builder.ConvertSingleExpression(node);

				if (!IsSame(exposed, node))
				{
					var translatedExposed = Visit(exposed);
					if (SequenceHelper.HasError(translatedExposed))
						return node;
					return translatedExposed;
				}

				if (TranslateMember(BuildContext, node, out var translatedMember))
				{
					return Visit(translatedMember);
				}

				if (HandleExtension(BuildContext, node, out translatedMember))
				{
					return Visit(translatedMember);
				}

				if (HandleValue(node, out var translated))
				{
					translated = RegisterTranslatedSql(translated, node);
					return Visit(translated);
				}

				if (HandleStringFormat(node, out var translatedFormat))
					return Visit(translatedFormat);

				if (node.Type == typeof(bool))
				{
					var translatedPredicate = ConvertPredicateMethod(node);
					if (!IsSame(translatedPredicate, node))
						return Visit(translatedPredicate);
				}
			}

			if (HandleSqlRelated(node, out var translatedSqlRelated))
				return translatedSqlRelated;

			if (_buildPurpose is BuildPurpose.Sql || _buildFlags.HasFlag(BuildFlags.ForSetProjection))
			{
				var generic = Builder.ParseGenericConstructor(node, ProjectFlags.SQL, CurrentDescriptor);
				if (!IsSame(generic, node))
					return Visit(generic);
			}

			if (_buildPurpose is BuildPurpose.Expression or BuildPurpose.Traverse or BuildPurpose.Expand or BuildPurpose.Extract or BuildPurpose.SubQuery)
			{
				var newNode = base.VisitMethodCall(node);
				FoundRoot = null;
				return newNode;
			}

			FoundRoot = null;
			return node;
		}

		protected override Expression VisitNewArray(NewArrayExpression node)
		{
			if (IsSqlOrExpression())
			{
				if (HandleValue(node, out var translated))
					return Visit(translated);
			}

			var newNode = base.VisitNewArray(node);
			FoundRoot = null;
			return newNode;
		}

		protected override Expression VisitNew(NewExpression node)
		{
			LogVisit(node);

			if (IsSqlOrExpression())
			{
				if (TranslateMember(BuildContext, node, out var translatedMember))
					return Visit(translatedMember);

				if (!ReferenceEquals(_disableNew, node))
				{
					if (HandleValue(node, out var translated))
						return Visit(translated);

					if (HandleSqlRelated(node, out translated))
						return Visit(translated);

					if (_buildPurpose is BuildPurpose.Sql || _buildFlags.HasFlag(BuildFlags.ForSetProjection))
					{
						var generic = Builder.ParseGenericConstructor(node, ProjectFlags.SQL, CurrentDescriptor);
						if (!IsSame(generic, node))
							return Visit(generic);
					}

					if (HandleAsParameter(node, out translated))
						return Visit(translated);
				}
			}

			if (IsRootExpression())
				return node;

			using var saveDescriptor = UsingColumnDescriptor(null);

			FoundRoot = null;

			if (node.Members?.Count > 0)
			{
				Expression[]? newArguments = null;
				for (int i = 0, n = node.Members.Count; i < n; i++)
				{
					var memberInfo   = node.Members[i];

					using var saveAlias = UsingAlias(memberInfo.Name);

					var nodeArgument = node.Arguments[i];
					var newArgument  = Visit(nodeArgument);

					if (newArguments != null)
					{
						newArguments[i] = newArgument;
					}
					else if (!ReferenceEquals(newArgument, nodeArgument))
					{
						newArguments = new Expression[n];
						for (int j = 0; j < i; j++)
						{
							newArguments[j] = node.Arguments[j];
						}

						newArguments[i] = newArgument;
					}
				}

				if (newArguments != null)
				{
					var newNode = node.Update(newArguments);
					return newNode;
				}

				return node;
			}

			return base.VisitNew(node);
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			if (_buildPurpose is BuildPurpose.Sql)
			{
				if (HandleValue(node, out var translated))
					return Visit(translated);

				if (HandleSqlRelated(node, out translated))
					return Visit(translated);

				using (UsingColumnDescriptor(null))
				{
					var generic = Builder.ParseGenericConstructor(node, ProjectFlags.SQL, CurrentDescriptor);
					if (!IsSame(generic, node))
						return Visit(generic);
				}
			}

			using var saveDescriptor = UsingColumnDescriptor(null);

			Expression newExpression;
			using (UsingDisableNew(node.NewExpression))
			using (UsingAlias(null))
			{
				newExpression = base.VisitMemberInit(node);
			}

			if (!IsSame(newExpression, node))
				return Visit(newExpression);

			return newExpression;
		}

		protected override Expression VisitSwitch(SwitchExpression node)
		{
			if (_buildPurpose is not BuildPurpose.Sql)
				return base.VisitSwitch(node);

			var hasDefaultPart =
				node.DefaultBody == null
				|| node.DefaultBody is not UnaryExpression
				{
					NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
					Operand: MethodCallExpression { Method: var m }
				}
				|| m != ConvertBuilder.DefaultConverter;

			var ps = new ISqlExpression[node.Cases.Count * 2 + (hasDefaultPart? 1 : 0)];
			var svExpr = Visit(node.SwitchValue);

			if (svExpr is not SqlPlaceholderExpression svPlaceholder)
				return base.VisitSwitch(node);

			var sv = svPlaceholder.Sql;

			for (var i = 0; i < node.Cases.Count; i++)
			{
				var sc = new SqlSearchCondition(true);
				foreach (var testValue in node.Cases[i].TestValues)
				{
					var testValueExpr = Visit(testValue);
					if (testValueExpr is not SqlPlaceholderExpression { Sql: var sql })
						return base.VisitSwitch(node);

					sc.Add(new SqlPredicate.ExprExpr(sv, SqlPredicate.Operator.Equal, sql, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null));
				}

				ps[i * 2] = sc;

				var caseExpr = Visit(node.Cases[i].Body);
				if (caseExpr is not SqlPlaceholderExpression casePlaceholder)
					return base.VisitSwitch(node);

				ps[i * 2 + 1] = casePlaceholder.Sql;
			}

			if (hasDefaultPart)
			{
				var defaultExpr = Visit(node.DefaultBody!);
				if (defaultExpr is not SqlPlaceholderExpression defaultPlaceholder)
					return base.VisitSwitch(node);

				ps[^1] = defaultPlaceholder.Sql;
			}

			//TODO: Convert everything to SqlSimpleCaseExpression
			var cases = new List<SqlCaseExpression.CaseItem>(ps.Length);

			for (var i = 0; i < ps.Length; i += 2)
			{
				var caseExpr = ps[i];
				var value    = ps[i + 1];

				if (caseExpr is not SqlSearchCondition sc)
					return base.VisitSwitch(node);

				cases.Add(new SqlCaseExpression.CaseItem(sc, value));
			}

			ISqlExpression? defaultExpression = null;

			if (hasDefaultPart)
				defaultExpression = ps[^1];

			var caseExpression = new SqlCaseExpression(MappingSchema.GetDataType(node.Type).Type, cases, defaultExpression);

			return CreatePlaceholder(caseExpression, node);
		}

		public override Expression VisitChangeTypeExpression(ChangeTypeExpression node)
		{
			if (_buildPurpose is BuildPurpose.Sql)
			{
				var translated = Visit(node.Expression);

				if (!IsSame(translated, node.Expression))
				{
					if (translated is SqlPlaceholderExpression sqlPlaceholder)
					{
						var dbDataType = CurrentDescriptor?.MemberType == translated.Type
							? CurrentDescriptor.GetDbDataType(true)
							: MappingSchema.GetDbDataType(node.Type);

						var sql = new SqlCastExpression(sqlPlaceholder.Sql, dbDataType, null);

						return Visit(CreatePlaceholder(sql, node));
					}

					return node.Update(translated);
				}
			}

			return base.VisitChangeTypeExpression(node);
		}

		protected override Expression VisitInvocation(InvocationExpression node)
		{
			if (IsSqlOrExpression() && HandleValue(node, out var translated))
				return Visit(translated);

			return base.VisitInvocation(node);
		}

		public bool HandleExtension(IBuildContext context, Expression expr, [NotNullWhen(true)] out Expression? translated)
		{
			// Handling ExpressionAttribute
			//
			if (expr.NodeType is ExpressionType.Call or ExpressionType.MemberAccess)
			{
				MemberInfo memberInfo;
				if (expr.NodeType == ExpressionType.Call)
				{
					memberInfo = ((MethodCallExpression)expr).Method;
				}
				else
				{
					memberInfo = ((MemberExpression)expr).Member;
				}

				var isServerSideOnly = memberInfo.IsServerSideOnly(MappingSchema);
				var attribute        = memberInfo.GetExpressionAttribute(MappingSchema);

				if (attribute != null)
				{
					// prevent to handling it here
					var converted = Builder.ConvertSingleExpression(expr);
					if (!IsSame(converted, expr))
					{
						translated = null;
						return false;
					}

					if (!isServerSideOnly && !attribute.PreferServerSide && Builder.CanBeEvaluatedOnClient(expr))
					{
						attribute  = null;
						translated = null;
						return false;
					}

					translated = ConvertExtension(attribute, context, expr, isServerSideOnly);

					if (translated is not SqlPlaceholderExpression placeholder)
					{
						if (isServerSideOnly)
						{
							translated = SqlErrorExpression.EnsureError(translated);
							return true;
						}

						translated = null;
						return false;
					}

					return !IsSame(translated, expr);
				}
			}

			translated = null;
			return false;
		}

		public Expression ConvertExtension(Sql.ExpressionAttribute attr, IBuildContext context, Expression expr, bool isServerSideOnly)
		{
			var rootContext     = context;
			var rootSelectQuery = context.SelectQuery;

			var root = GetAggregationRootContext(expr);
			if (root != null)
			{
				rootContext = root.BuildContext;
			}
			else
			{
				var findAggregationRoot = attr.IsAggregate || attr.IsWindowFunction;

				if (!findAggregationRoot)
				{
					// handling old stuff of backward compatibility
					var current = context;
					while (true)
					{
						if (current is SelectContext { InnerContext: SubQueryContext { IsSelectWrapper: true } } selectContext)
						{
							current = selectContext.InnerContext;
						}
						else if (current is SubQueryContext { IsSelectWrapper: true } subQuery)
						{
							current = subQuery.SubQuery;
						}
						else if (current is GroupByBuilder.GroupByContext)
						{
							findAggregationRoot = true;
							break;
						}
						else
							break;
					}
				}

				if (findAggregationRoot)
				{
					if (expr is MethodCallExpression { Method.IsStatic: true, Arguments: [ContextRefExpression contextRef, ..] })
					{
						rootContext = contextRef.BuildContext;
					}

					_ = BuildAggregationRoot(new ContextRefExpression(rootContext.ElementType, rootContext));
					if (FoundRoot != null)
					{
						rootContext = FoundRoot.BuildContext;
					}
				}
			}

			if (rootContext is GroupByBuilder.GroupByContext groupBy)
			{
				rootSelectQuery = groupBy.SubQuery.SelectQuery;
			}

			Expression transformed;
			if (GetAlreadyTranslated(rootSelectQuery, expr, out var translated))
			{
				transformed = translated;
			}
			else
			{
				transformed = attr.GetExpression((buildVisitor: this, context: rootContext),
					Builder.DataContext,
					Builder,
					rootSelectQuery,
					expr,
					static (context, e, descriptor, inline) =>
						context.buildVisitor.ConvertToExtensionSql(context.context, e, descriptor, inline));
			}

			if (transformed is SqlPlaceholderExpression placeholder)
			{
				Builder.RegisterExtensionAccessors(expr);

				placeholder = placeholder.WithSql(Builder.PosProcessCustomExpression(placeholder.Sql, NullabilityContext.GetContext(placeholder.SelectQuery)));
				placeholder = placeholder.WithPath(expr);

				if (placeholder.Type == typeof(bool) && attr.IsPredicate)
				{
					if (placeholder.Sql is not ISqlPredicate)
					{
						var predicate       = ConvertExpressionToPredicate(placeholder.Sql);
						var searchCondition = new SqlSearchCondition().Add(predicate);
						placeholder = placeholder.WithSql(searchCondition);
					}
				}

				placeholder = (SqlPlaceholderExpression)RegisterTranslatedSql(rootSelectQuery, placeholder, expr);

				return placeholder;
			}

			if (isServerSideOnly)
			{
				if (transformed is SqlErrorExpression errorExpr)
					return SqlErrorExpression.EnsureError(errorExpr, expr.Type);
				return SqlErrorExpression.EnsureError(expr, expr.Type);
			}

			return expr;
		}

		public Expression ConvertToExtensionSql(IBuildContext context, Expression expression, ColumnDescriptor? columnDescriptor, bool? inlineParameters)
		{
			var translationModifier = context.TranslationModifier;
			if (inlineParameters == true)
				translationModifier = translationModifier.WithInlineParameters(true);

			Builder.PushTranslationModifier(translationModifier, true);

			using var saveFlags      = UsingBuildFlags(_buildFlags | BuildFlags.ForExtension);
			using var saveDescriptor = UsingColumnDescriptor(columnDescriptor);
			try
			{
				expression = expression.UnwrapConvertToObject();
				var unwrapped = expression.Unwrap();

				if (unwrapped is LambdaExpression lambda)
				{
					var contextRefExpression = new ContextRefExpression(lambda.Parameters[0].Type, context);

					var body = lambda.GetBody(contextRefExpression);

					return BuildSqlExpression(context, body);
				}

				if (unwrapped is ContextRefExpression contextRef)
				{
					contextRef = contextRef.WithType(contextRef.BuildContext.ElementType);

					var result = BuildSqlExpression(contextRef.BuildContext, contextRef);

					if (result is SqlPlaceholderExpression)
					{
						if (result.Type != expression.Type)
						{
							result = Expression.Convert(result, expression.Type);
							result = BuildSqlExpression(contextRef.BuildContext, result);
						}

						result = Builder.UpdateNesting(context, result);

						return result;
					}

					if (HandleTableContextForExtension(expression, out var translated))
						return translated;
				}
				else
				{
					var converted = BuildSqlExpression(context, expression);

					if (converted is SqlPlaceholderExpression or SqlErrorExpression)
					{
						return converted;
					}

					if (HandleTableContextForExtension(expression, out var translated))
						return translated;

					// Weird case, see Stuff2 test
					if (!Builder.CanBeEvaluatedOnClient(expression))
					{
						var buildResult = Builder.TryBuildSequence(new BuildInfo(context, expression, new SelectQuery()));
						if (buildResult.BuildContext != null)
						{
							unwrapped = new ContextRefExpression(buildResult.BuildContext.ElementType, buildResult.BuildContext);
							var result = BuildSqlExpression(buildResult.BuildContext, unwrapped);

							if (result is SqlPlaceholderExpression { SelectQuery: not null } placeholder)
							{
								_ = Builder.ToColumns(placeholder.SelectQuery, placeholder);

								return ExpressionBuilder.CreatePlaceholder(context, placeholder.SelectQuery, unwrapped);
							}
						}
					}
				}

				return expression;
			}
			finally
			{
				Builder.PopTranslationModifier();
			}
		}

		bool HandleTableContextForExtension(Expression expr, [NotNullWhen(true)] out Expression? translated)
		{
			var table = SequenceHelper.GetTableOrCteContext(Builder, expr);
			if (table != null)
			{
				var allPlaceholder = CreatePlaceholder(table.SqlTable.All, expr);
				translated = allPlaceholder;
				return true;
			}

			translated = null;
			return false;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			Expression? result;
			LogVisit(node);

			if (_buildFlags.HasFlag(BuildFlags.ForExpanding))
			{
				if (!HasContextReferenceOrSql(node))
					return node;
			}

			FoundRoot = null;
			if (node.Expression != null)
			{
				result = HandleRoot(node);
				if (result != null)
					return result;
			}
			else
			{
				FoundRoot = null;
			}

			using var saveAlias = UsingAlias(node.Member.Name);

			var context = FoundRoot;

			if (node.Expression != null)
			{
				result = HandleNullableT(node);
				if (result != null)
					return result;
			}

			if (context != null)
			{
				if (_buildPurpose is BuildPurpose.Expression or BuildPurpose.Sql)
				{
					var exprCacheKey = GetSqlCacheKey(node);
					if (GetAlreadyTranslated(exprCacheKey, out var alreadyTranslated))
					{
						return Visit(alreadyTranslated);
					}
				}

				// Should be called before any other checks. SetOperationContext, TableLikeQueryContext, CteContext may intercept this call
				//
				var translated = MakeWithCache(context.BuildContext, node);
				if (!IsSame(translated, node) && translated is not SqlErrorExpression)
				{
					if (translated is DefaultValueExpression && _buildPurpose is BuildPurpose.Expression)
					{
						// skip DefaultValueExpression in expression mode, it should be result from SelectContext that projection is wrong.
					}
					else
					{
						translated = RegisterTranslatedSql(translated, node);

						translated = Visit(translated);
						return translated;
					}
				}

				if (_buildPurpose is BuildPurpose.Traverse)
					return node;

				if (Builder.IsAssociation(node, out _))
				{
					result = HandleAssociation(node, context);
					if (result != null)
						return result;
				}

				if (_buildPurpose is BuildPurpose.Sql && translated is SqlErrorExpression)
					return translated;
			}

			if (BuildContext != null && _buildPurpose is BuildPurpose.Sql or BuildPurpose.Expression)
			{
				result = HandleMember(node, context);
				if (result != null)
					return result;
			}

			if (_buildPurpose is BuildPurpose.Expression && node.Expression != null)
			{
				return base.VisitMember(node);
			}

			return node;

			Expression? HandleNullableT(MemberExpression node)
			{
				if (node.Member.IsNullableValueMember())
				{
					var translatedExpr = Visit(node.Expression);

					if (translatedExpr is SqlPlaceholderExpression placeholder)
					{
						return Visit(placeholder.WithType(node.Type));
					}

					return node.Update(translatedExpr);
				}

				if (node.Member.IsNullableHasValueMember())
				{
					var translatedExpr = Visit(node.Expression);

					if (translatedExpr is SqlPlaceholderExpression)
					{
						var hasValue = SequenceHelper.MakeNotNullCondition(translatedExpr);
						return Visit(hasValue);
					}
				}

				return null;
			}

			Expression? HandleAssociation(MemberExpression node, ContextRefExpression context)
			{
				Expression associationRoot;
				using (UsingBuildPurpose(BuildPurpose.AssociationRoot))
				{
					associationRoot = MakeWithCache(context.BuildContext, node);
				}

				if (!IsSame(associationRoot, node) && associationRoot is not SqlErrorExpression)
				{
					return Visit(associationRoot);
				}

				if (node.Expression != null)
				{
					var testExpression = UnwrapExpressionForAssociation(node.Expression);

					if (testExpression is ContextRefExpression { BuildContext.AutomaticAssociations: true })
					{
						var association = TryCreateAssociation(node, context, BuildContext);
						if (!IsSame(association, node))
							return Visit(association);
					}
				}

				return null;
			}

			Expression? HandleRoot(MemberExpression node)
			{
				Expression root;
				using (CombineBuildFlags(BuildFlags.ForMemberRoot))
				{
					root = _buildPurpose is BuildPurpose.Expression or BuildPurpose.Sql or BuildPurpose.SubQuery or BuildPurpose.Extract or BuildPurpose.Table
						? BuildRoot(node.Expression!)
						: Visit(node.Expression!);

					if (root is ParameterExpression)
						return node;
				}

				if (!IsSame(root, node.Expression!))
				{
					if (root is not (SqlErrorExpression or MethodCallExpression or SqlGenericConstructorExpression or SqlPlaceholderExpression))
					{
						if (root.Type != node.Expression!.Type && _buildPurpose is BuildPurpose.Table or BuildPurpose.AggregationRoot or BuildPurpose.AssociationRoot)
							return Visit(root);

						var updated = node.Update(root);
						var result   = Visit(updated);
						if (result is SqlPlaceholderExpression placeholder)
						{
							result = placeholder.WithTrackingPath(updated);
						}

						return result;
					}

					if (root is SqlErrorExpression error)
					{
						FoundRoot = null;
					}
				}

				return null;
			}

			Expression? HandleMember(MemberExpression node, ContextRefExpression? context)
			{
				var rootContext = context?.BuildContext ?? BuildContext!;
				var cacheKey = new ExprCacheKey(node, null, CurrentDescriptor, rootContext.SelectQuery, ProjectFlags.SQL);

				if (GetAlreadyTranslated(cacheKey, out var translatedLocal))
					return translatedLocal;

				if (TranslateMember(BuildContext, memberExpression: node, translated: out translatedLocal))
				{
					translatedLocal = RegisterTranslatedSql(translatedLocal, node);

					if (!_translationCache.ContainsKey(cacheKey))
						_translationCache[cacheKey] = translatedLocal;

					return Visit(translatedLocal);
				}

				if (HandleValue(node, out var translated))
					return Visit(translated);

				if (HandleExtension(BuildContext, node, out translated))
					return Visit(translated);

				var exposed = Builder.ConvertSingleExpression(node);

				if (!IsSame(exposed, node))
				{
					var translatedExposed = Visit(exposed);
					if (SequenceHelper.HasError(translatedExposed))
						return node;
					return translatedExposed;
				}

				if (HandleSubquery(node, out translated))
					return Visit(translated);

				if (node.Expression is ContextRefExpression contextRef)
				{
					// Handling case when implementation of interface refers to ExpressionMethod
					if (contextRef is { ElementType.IsInterface: true, BuildContext: ITableContext tableContext } && tableContext.ObjectType != contextRef.ElementType)
					{
						var newMember = tableContext.ObjectType.GetImplementation(node.Member);
						if (newMember != null)
						{
							var newMemberAccess = Expression.MakeMemberAccess(contextRef.WithType(tableContext.ObjectType), newMember);
							return Visit(newMemberAccess);
						}
					}
				}

				return null;
			}
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			FoundRoot = null;
			return base.VisitParameter(node);
		}

		static Expression UnwrapExpressionForAssociation(Expression expression)
		{
			var testExpression = expression.UnwrapConvert();
			if (testExpression.NodeType is ExpressionType.TypeAs or ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				var operand = ((UnaryExpression)testExpression).Operand;
				testExpression = UnwrapExpressionForAssociation(operand);
			}
			else if (testExpression is SqlDefaultIfEmptyExpression defaultIfEmpty)
			{
				testExpression = UnwrapExpressionForAssociation(defaultIfEmpty.InnerExpression);
			}

			return testExpression;
		}

		public CacheSnapshot CreateSnapshot()
		{
			return new CacheSnapshot(this);
		}

		public Expression TryCreateAssociation(Expression expression, ContextRefExpression rootContext, IBuildContext? forContext)
		{
			var associationDescriptor = Builder.GetAssociationDescriptor(expression, out var memberInfo);

			if (associationDescriptor == null || memberInfo == null)
				return expression;

			var associationRoot = rootContext;

			_associations ??= new(ExprCacheKey.SqlCacheKeyComparer);

			var cacheFlags = ProjectFlags.Root;

			var key = new ExprCacheKey(
				Builder.AssociationToRealization(expression),
				associationRoot.BuildContext,
				columnDescriptor: null,
				selectQuery: null,
				cacheFlags
			);

			if (_associations.TryGetValue(key, out var associationExpression))
			{
				return associationExpression;
			}

			LoadWithEntity? loadWith     = null;

			var   prevIsOuter = _buildFlags.HasFlag(BuildFlags.ForceOuter);
			bool? isOptional  = prevIsOuter ? true : null;

			if (rootContext.BuildContext.IsOptional)
				isOptional = true;

			var table = SequenceHelper.GetTableOrCteContext(rootContext.BuildContext);

			if (table != null)
			{
				table.LoadWithRoot ??= new();
				loadWith = table.LoadWithRoot;
				if (table.IsOptional)
					isOptional = true;
			}

			if (forContext?.IsOptional == true)
				isOptional = true;

			if (!associationDescriptor.IsList)
			{
				isOptional = isOptional == true || associationDescriptor.CanBeNull;
			}

			Expression? notNullCheck = null;
			if (associationDescriptor.IsList && (isOptional == true && _buildPurpose is BuildPurpose.SubQuery))
			{
				var keys = BuildExpression(forContext, rootContext, BuildPurpose.Sql, BuildFlags.ForKeys);
				if (forContext != null)
				{
					notNullCheck = ExtractNotNullCheck(forContext, keys);
				}
			}

			var modifier = associationRoot.BuildContext.TranslationModifier;

			var association = AssociationHelper.BuildAssociationQuery(Builder, rootContext, memberInfo,
				associationDescriptor, notNullCheck, !associationDescriptor.IsList, modifier, loadWith, ref isOptional);

			associationExpression = association;

			var doNotBuild =
				associationDescriptor.IsList
				|| _buildPurpose is BuildPurpose.SubQuery or BuildPurpose.Extract or BuildPurpose.Expand or BuildPurpose.AggregationRoot
				|| _buildFlags.HasFlag(BuildFlags.ForExpanding);

			if (!doNotBuild)
			{
				// IsAssociation will force to create OuterApply instead of subquery. Handled in FirstSingleContext
				//
				var buildInfo = new BuildInfo(rootContext.BuildContext, association, new SelectQuery())
				{
					SourceCardinality = isOptional == true ? SourceCardinality.ZeroOrOne : SourceCardinality.One,
					IsAssociation = true
				};

				using var snapshot = CreateSnapshot();
				var       sequence = Builder.BuildSequence(buildInfo);

				if (!Builder.IsSupportedSubquery(rootContext.BuildContext, sequence, out var errorMessage))
				{
					sequence.Detach();
					return new SqlErrorExpression(expression, errorMessage, expression.Type, true);
				}

				snapshot.Accept();
				sequence.SetAlias(associationDescriptor.GenerateAlias());

				/*
				if (forContext != null)
					sequence = new ScopeContext(sequence, forContext);
					*/

				associationExpression = new ContextRefExpression(association.Type, sequence);
			}
			else
			{
				associationExpression = SqlAdjustTypeExpression.AdjustType(associationExpression, expression.Type, MappingSchema);
			}

			if (!doNotBuild)
				_associations.Add(key, associationExpression);

			return associationExpression;
		}

		Expression? ExtractNotNullCheck(IBuildContext context, Expression expr)
		{
			SqlPlaceholderExpression? notNull = null;

			if (expr is SqlPlaceholderExpression placeholder)
			{
				notNull = placeholder.MakeNullable();
			}

			if (notNull == null)
			{
				List<Expression> expressions = new();
				if (!Builder.CollectNullCompareExpressions(context, expr, expressions) || expressions.Count == 0)
					return null;

				List<SqlPlaceholderExpression> placeholders = new(expressions.Count);

				foreach (var expression in expressions)
				{
					var predicateExpr = BuildExpression(context, expression, BuildPurpose.Sql);
					if (predicateExpr is SqlPlaceholderExpression current)
					{
						placeholders.Add(current);
					}
				}

				notNull = placeholders
					.FirstOrDefault(pl => !pl.Sql.CanBeNullable(NullabilityContext.NonQuery));
			}

			if (notNull == null)
			{
				return null;
			}

			var notNullPath = notNull.Path;

			if (!notNullPath.Type.IsNullableOrReferenceType())
			{
				notNullPath = Expression.Convert(notNullPath, typeof(Nullable<>).MakeGenericType(notNullPath.Type));
			}

			var notNullExpression = Expression.NotEqual(notNullPath, Expression.Constant(null, notNullPath.Type));

			return notNullExpression;
		}

		public override Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
		{
			var innerExpression = Visit(node.InnerExpression);

			using var saveAlias = UsingAlias(null);

			if (_buildPurpose is BuildPurpose.Sql)
			{
				if (innerExpression is SqlPlaceholderExpression)
					return innerExpression;
			}

			if (innerExpression is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				var newNode = node.Update(defaultIfEmptyExpression.InnerExpression, defaultIfEmptyExpression.NotNullExpressions);
				return Visit(newNode);
			}

			if (_buildPurpose is BuildPurpose.Expression && _buildFlags.HasFlag(BuildFlags.ForceDefaultIfEmpty))
			{
				var notNull = node.NotNullExpressions.Select(n => Visit(n)).ToList();

				Expression testCondition;

				testCondition = notNull.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);
				testCondition = MarkerExpression.PreferClientSide(testCondition);

				var defaultValue  = new DefaultValueExpression(MappingSchema, innerExpression.Type, true);

				var condition = Expression.Condition(testCondition, innerExpression, defaultValue);

				return Visit(condition);
			}

			if ((_buildFlags.HasFlag(BuildFlags.ForMemberRoot) && _buildFlags.HasFlag(BuildFlags.ForExpanding)))
			{
				if (innerExpression is ContextRefExpression contextRef)
				{
					return Visit(contextRef);
				}
			}

			return node.Update(innerExpression, node.NotNullExpressions);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsSqlOrExpression()
		{
			return _buildPurpose is BuildPurpose.Sql or BuildPurpose.Expression;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsRootExpression()
		{
			return _buildPurpose is BuildPurpose.Root or BuildPurpose.Table or BuildPurpose.AggregationRoot or BuildPurpose.AssociationRoot;
		}

		protected override Expression VisitConditional(ConditionalExpression node)
		{
#if DEBUG
			Debug.WriteLine($"VisitConditional {_buildPurpose}, {_buildFlags}, {node}");
#endif

			if (_buildPurpose is BuildPurpose.Root or BuildPurpose.AssociationRoot or BuildPurpose.AggregationRoot)
				return node;

			if (_buildPurpose is not BuildPurpose.Sql && _buildFlags.HasFlag(BuildFlags.ForExpanding))
			{
				if (!HasContextReferenceOrSql(node))
				{
					return node;
				}
			}

			if (!IsSqlOrExpression() || BuildContext == null)
			{
				var newNode = base.VisitConditional(node);
				FoundRoot = null;
				return newNode;
			}

			var optimized = OptimizeExpression(node);
			if (!IsSame(optimized, node))
				return Visit(optimized);

			if (TryConvertToSql(node, out var sqlResult) && sqlResult is SqlPlaceholderExpression)
			{
				return sqlResult;
			}

			{
				Expression test;

				// is scope correct here?
				using var saveFlags = CombineBuildFlags(BuildFlags.ForceOuter);

				using (UsingColumnDescriptor(null))
				using (UsingAlias("test"))
				{
					test = Visit(node.Test);
				}

				if (test.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
				{
					var binary = (BinaryExpression)test;
					if (HandleDefaultIfEmptyInBinary(binary.Left, binary.Right, out var newTest)
						|| HandleDefaultIfEmptyInBinary(binary.Right, binary.Left, out newTest))
					{
						if (binary.NodeType == ExpressionType.Equal)
						{
							newTest = Expression.Not(newTest);
						}

						var newCondition = Expression.Condition(newTest, node.IfTrue, node.IfFalse);

						return Visit(newCondition);
					}
				}

				var ifTrue  = Visit(node.IfTrue);
				var ifFalse = Visit(node.IfFalse);

				if (test is ConstantExpression { Value: bool boolValue })
				{
					return boolValue ? ifTrue : ifFalse;
				}

				if (_buildPurpose is BuildPurpose.Sql)
				{
					if (test is SqlPlaceholderExpression testPlaceholder
						&& ifTrue is SqlPlaceholderExpression truePlaceholder
						&& ifFalse is SqlPlaceholderExpression falsePlaceholder)
					{
						testPlaceholder = UpdateNesting(testPlaceholder);
						truePlaceholder = UpdateNesting(truePlaceholder);
						falsePlaceholder = UpdateNesting(falsePlaceholder);

						return Visit(CreatePlaceholder(new SqlConditionExpression(ConvertExpressionToPredicate(testPlaceholder.Sql), truePlaceholder.Sql, falsePlaceholder.Sql), node));
					}
				}

				var newNode = node.Update(test, ifTrue, ifFalse);
				if (!IsSame(newNode, node))
					return Visit(newNode);
			}

			return node;
		}

		bool HandleDefaultIfEmptyInBinary(Expression left, Expression right, [NotNullWhen(true)] out Expression? newCondition)
		{
			if (left is SqlDefaultIfEmptyExpression { InnerExpression: SqlGenericConstructorExpression } defaultIfEmpty && right.IsNullValue())
			{
				var notNullExpressions = defaultIfEmpty.NotNullExpressions;

				newCondition = notNullExpressions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);

				return true;
			}

			newCondition = null;
			return false;
		}

		public Expression RemoveNullPropagation(Expression expr, bool toSql)
		{
			static bool? IsNull(Expression sqlExpr)
			{
				if (sqlExpr.IsNullValue())
					return true;

				if (sqlExpr is not SqlPlaceholderExpression placeholder)
					return null;

				return QueryHelper.IsNullValue(placeholder.Sql);
			}

			if (expr.NodeType == ExpressionType.Equal || expr.NodeType == ExpressionType.NotEqual)
			{
				var binary = (BinaryExpression)expr;

				var left  = RemoveNullPropagation(binary.Left, toSql: true);
				var right = RemoveNullPropagation(binary.Right, toSql: true);

				if (toSql)
				{
					binary = binary.Update(
						left,
						binary.Conversion,
						right);
				}

				return binary;
			}

			if (expr.NodeType == ExpressionType.Conditional)
			{
				var cond = (ConditionalExpression)expr;

				var test    = RemoveNullPropagation(cond.Test, toSql: true);
				var ifTrue  = RemoveNullPropagation(cond.IfTrue, toSql: true);
				var ifFalse = RemoveNullPropagation(cond.IfFalse, toSql: true);

				if (test.NodeType == ExpressionType.Equal || test.NodeType == ExpressionType.NotEqual)
				{
					var testLeft  = ((BinaryExpression)test).Left;
					var testRight = ((BinaryExpression)test).Right;

					var nullLeft  = IsNull(testLeft);
					var nullRight = IsNull(testRight);

					if (nullRight == true && nullLeft == true)
					{
						return test.NodeType == ExpressionType.Equal ? cond.IfTrue : cond.IfFalse;
					}

					if (test.NodeType == ExpressionType.Equal)
					{
						if (IsNull(ifTrue) == true && (nullRight == true || nullRight == true))
						{
							return toSql ? ifFalse : cond.IfFalse;
						}
					}
					else
					{
						if (IsNull(ifFalse) == true && (nullLeft == true || nullRight == true))
						{
							return toSql ? ifTrue : cond.IfTrue;
						}
					}
				}

				if (toSql)
				{
					cond = cond.Update(test, ifTrue, ifFalse);
				}

				return cond;
			}

			var doNotConvert =
				expr.NodeType is ExpressionType.Equal
							  or ExpressionType.NotEqual
							  or ExpressionType.GreaterThan
							  or ExpressionType.GreaterThanOrEqual
							  or ExpressionType.LessThan
							  or ExpressionType.LessThanOrEqual
							  or ExpressionType.Convert;

			if (!doNotConvert && toSql)
			{
				var sql = BuildExpression(expr, BuildPurpose.Sql);
				if (sql is SqlPlaceholderExpression or SqlGenericConstructorExpression)
					return sql;
			}

			return expr;
		}

		Expression OptimizeExpression(Expression expression)
		{
			var visitor = new ExpressionTreeOptimizerVisitor();
			var result  = visitor.Visit(expression);
			return result;
		}

		bool TryConvertToSql(Expression node, out Expression translated)
		{
			if (_preferClientSide && !_buildFlags.HasFlag(BuildFlags.ForSetProjection))
			{
				translated = node;
				return false;
			}

			if (node is SqlPlaceholderExpression)
			{
				translated = node;
				return true;
			}

			// Trying to convert whole expression
			if (_buildPurpose is not BuildPurpose.Sql && node is BinaryExpression or UnaryExpression or ConditionalExpression)
			{
				translated = BuildSqlExpression(node);
				//if (!SequenceHelper.HasError(translated))
				if (translated is SqlPlaceholderExpression)
				{
					return true;
				}
			}

			translated = node;
			return false;
		}

		internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			if (_buildPurpose is BuildPurpose.Extract)
			{
				return base.VisitSqlEagerLoadExpression(node);
			}

			return node;
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			if (_buildPurpose is BuildPurpose.Table)
			{
				if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					var converted = Visit(node.Operand);
					return converted;
				}
			}

			if (_buildPurpose is not (BuildPurpose.Traverse or BuildPurpose.Sql or BuildPurpose.Expression))
			{
				return base.VisitUnary(node);
			}

			switch (node.NodeType)
			{
				case ExpressionType.Not:
				{
					if (_buildPurpose is BuildPurpose.Sql)
					{
						var predicateExpr = Visit(node.Operand);

						if (predicateExpr is SqlPlaceholderExpression placeholder)
						{
							var predicate = placeholder.Sql as ISqlPredicate;
							if (predicate is null)
							{
								var withNull = !node.Operand.Type.IsNullableType;

								predicate = ConvertExpressionToPredicate(
									placeholder.Sql,
									withNull: withNull,
									forceEquality: withNull && placeholder.Sql.CanBeNullableOrUnknown(GetNullabilityContext(), false));
							}

							var condition = new SqlSearchCondition();
							condition.Add(predicate.MakeNot());
							return CreatePlaceholder(condition, node);
						}
					}

					break;
				}

				case ExpressionType.UnaryPlus:
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
				{
					var translated = Visit(node.Operand);

					if (translated is SqlPlaceholderExpression placeholder)
					{
						placeholder = UpdateNesting(placeholder);

						var t = node.Type;

						switch (node.NodeType)
						{
							case ExpressionType.UnaryPlus: return CreatePlaceholder(placeholder.Sql, node);
							case ExpressionType.Negate:
							case ExpressionType.NegateChecked:
								return CreatePlaceholder(new SqlBinaryExpression(t, new SqlValue(-1), "*", placeholder.Sql, Precedence.Multiplicative), node);
						}
					}

					break;
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					if (node.Type != typeof(object) &&
						(HandleSqlRelated(node, out var translated) || HandleValue(node, out translated)))
					{
						return Visit(translated);
					}

					if ((_buildPurpose is BuildPurpose.Root || _buildFlags.HasFlag(BuildFlags.ForMemberRoot)) && node.Operand is ContextRefExpression contextRef)
					{
						return Visit(contextRef.WithType(node.Type));
					}

					if (_buildPurpose is BuildPurpose.Expression && !_buildFlags.HasFlag(BuildFlags.ForSetProjection))
					{
						return base.VisitUnary(node);
					}

					var operandExpr = Visit(node.Operand);

					if (_buildPurpose is BuildPurpose.Sql && operandExpr is SqlDefaultIfEmptyExpression defaultIfEmpty)
					{
						if (defaultIfEmpty.InnerExpression is SqlPlaceholderExpression)
							operandExpr = defaultIfEmpty.InnerExpression;
					}

					if (SequenceHelper.IsSqlReady(operandExpr))
					{
						operandExpr = UpdateNesting(operandExpr);
						var placeholders = CollectPlaceholdersStraight(operandExpr);

						if (placeholders.Count == 1)
						{
							var placeholder = placeholders[0];

							if (_buildPurpose is BuildPurpose.Expression && node.Type == typeof(object))
								return base.VisitUnary(node);

							if (node.Method == null && (node.IsLifted || node.Type == typeof(object)))
								return Visit(placeholder.WithType(node.Type));

							if (node.Method == null && operandExpr is not SqlPlaceholderExpression)
								return base.VisitUnary(node);

							if (node.Type == typeof(bool) && node.Operand.Type == typeof(SqlBoolean))
								return Visit(placeholder.WithType(node.Type));

							if (node.Type == typeof(Enum) && node.Operand.Type.IsEnum)
								return base.VisitUnary(node);

							var t = node.Operand.Type;
							var s = MappingSchema.GetDataType(t);

							if (placeholder.Sql.SystemType != null && s.Type.SystemType == typeof(object))
							{
								t = placeholder.Sql.SystemType;
								s = MappingSchema.GetDataType(t);
							}

							if (node.Type == t ||
								t.IsEnum && Enum.GetUnderlyingType(t) == node.Type ||
								node.Type.IsEnum && Enum.GetUnderlyingType(node.Type) == t)
							{
								return Visit(placeholder.WithType(node.Type));
							}

							return Visit(CreatePlaceholder(PseudoFunctions.MakeCast(placeholder.Sql, MappingSchema.GetDbDataType(node.Type), s), node));
						}
					}

					if (HandleValue(node, out var translatedValue))
					{
						return Visit(translatedValue);
					}

					break;
				}
			}

			return base.VisitUnary(node);
		}

		protected override Expression VisitDefault(DefaultExpression node)
		{
			if (_buildPurpose is BuildPurpose.Sql)
			{
				if (HandleValue(node, out var translated))
					return Visit(translated);
			}

			return base.VisitDefault(node);
		}

		public override Expression VisitDefaultValueExpression(DefaultValueExpression node)
		{
			if (_buildPurpose is BuildPurpose.Sql && BuildContext is not null)
			{
				if (node.IsNull)
				{
					var dataType = (node.MappingSchema ?? MappingSchema).GetDbDataType(node.Type);
					var value    = new SqlValue(dataType, null);
					return CreatePlaceholder(value, node);
				}

				if (HandleValue(node, out var translated))
					return Visit(translated);
			}

			return base.VisitDefaultValueExpression(node);
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (node.Type == typeof(MemberInfo[]) || node.Type == typeof(LoadWithEntity))
				return node;

			if (IsSqlOrExpression())
			{
				if (HandleSqlRelated(node, out var translated))
					return Visit(translated);

				if (_buildPurpose is BuildPurpose.Expression && !_buildFlags.HasFlag(BuildFlags.ForSetProjection))
					return node;

				if (HandleValue(node, out translated))
					return Visit(translated);
			}

			return base.VisitConstant(node);
		}

		public override Expression VisitMarkerExpression(MarkerExpression node)
		{
			if (node.MarkerType == MarkerType.PreferClientSide)
			{
				Expression inner;
				using (UsingPreferClientSide(true))
				{
					inner = Visit(node.InnerExpression);
				}

				if (inner is SqlPlaceholderExpression or SqlErrorExpression)
					return inner;

				return node.Update(inner);
			}

			// MarkerType.AggregationFallback or MarkerType.None
			return node;
		}

		public bool HandleAsParameter(Expression node, [NotNullWhen(true)] out Expression? translated)
		{
			if (_buildPurpose is BuildPurpose.Sql)
			{
				var sqlParam = Builder.ParametersContext.BuildParameter(BuildContext, node, CurrentDescriptor, alias: Alias);

				if (sqlParam != null)
				{
					translated = CreatePlaceholder(sqlParam, node);
					return true;
				}
			}

			translated = null;
			return false;
		}

		public bool HandleStringFormat(MethodCallExpression node, [NotNullWhen(true)] out Expression? translated)
		{
			translated = null;

			if (node.Method.DeclaringType == typeof(string))
			{
				if (node.Method.Name == "Format")
				{
					var format = node.Arguments[0].EvaluateExpression<string>();
					if (format == null)
						return false;

					var (inputArguments, startIndex) = node.Arguments switch
					{
						[_, NewArrayExpression arrayExpr] => (arrayExpr.Expressions, 0),
						_ => (node.Arguments, 1),
					};

					var arguments = new ISqlExpression[inputArguments.Count - startIndex];

					DbDataType stringType;

					if (CurrentDescriptor?.MemberType == typeof(string))
						stringType = CurrentDescriptor.GetDbDataType(true);
					else
						stringType = MappingSchema.GetDbDataType(typeof(string));

					var formatAsExpression = _buildFlags.HasFlag(BuildFlags.FormatAsExpression);

					for (var i = startIndex; i < inputArguments.Count; i++)
					{
						var expr = BuildSqlExpression(inputArguments[i]);
						if (expr is not SqlPlaceholderExpression sqlPlaceholder)
							return false;

						var sql = sqlPlaceholder.Sql;

						if (!formatAsExpression)
						{
							sql = new SqlCastExpression(sql, stringType, null);
							sql = new SqlCoalesceExpression(sql, new SqlValue(stringType, ""));
						}

						arguments[i - startIndex] = sql;
					}

					ISqlExpression result;
					if (formatAsExpression)
					{
						result = new SqlExpression(MappingSchema.GetDbDataType(node.Type), format, Precedence.Primary, arguments);
					}
					else
					{
						result = QueryHelper.ConvertFormatToConcatenation(format, arguments);
					}

					translated = CreatePlaceholder(result, node);
					return true;
				}
			}

			return false;
		}

		[MemberNotNullWhen(true, nameof(BuildContext))]
		private bool CanTryHandleValue(Expression node)
		{
			if (_buildPurpose is not (BuildPurpose.Sql or BuildPurpose.Expression))
			{
				return false;
			}

			if (BuildContext != null && Builder.CanBeEvaluatedOnClient(node))
			{
				if (!Builder.PreferServerSide(node, false))
				{
					var preferConvert = _buildPurpose is BuildPurpose.Sql || (_buildPurpose is BuildPurpose.Expression && _buildFlags.HasFlag(BuildFlags.ForSetProjection));

					if (!preferConvert)
					{
						return false;
					}

					return true;
				}
			}

			return false;
		}

		public bool HandleValue(Expression node, [NotNullWhen(true)] out Expression? translated)
		{
			if (CanTryHandleValue(node))
			{
				ISqlExpression? sql = null;

				if (CurrentDescriptor?.ValueConverter == null && Builder.CanBeConstant(node) && Builder.CanBeEvaluatedOnClient(node) && !_buildFlags.HasFlag(BuildFlags.ForceParameter))
				{
					sql = Builder.BuildConstant(MappingSchema, node, CurrentDescriptor);
				}

				var needParameter = sql == null;
				if (!needParameter)
				{
					if (null != node.Find(x => ReferenceEquals(x, ExpressionBuilder.ParametersParam)))
						needParameter = true;
				}

				if (needParameter)
				{
					var toTranslate = node;
					if (_buildFlags.HasFlag(BuildFlags.ForKeys))
						toTranslate = Builder.ParseGenericConstructor(node, ProjectFlags.SQL | ProjectFlags.Expression, CurrentDescriptor);

					if (toTranslate is not SqlGenericConstructorExpression)
					{
						sql = Builder.ParametersContext.BuildParameter(BuildContext, toTranslate, CurrentDescriptor, alias: Alias);
					}
				}

				if (sql != null)
				{
					var path = new SqlPathExpression([SequenceHelper.CreateRef(BuildContext), node], node.Type);
					translated = CreatePlaceholder(sql, path).WithAlias(Alias);
					return true;
				}
			}

			translated = null;
			return false;
		}

		public bool HandleSubquery(Expression node, [NotNullWhen(true)] out Expression? subqueryExpression)
		{
			subqueryExpression = null;

			if (BuildContext == null || _buildPurpose is BuildPurpose.SubQuery or BuildPurpose.Traverse)
				return false;

			if (null != subqueryExpression.Find(e => e is SqlEagerLoadExpression or SqlErrorExpression))
				return false;

			if (_disableSubqueries.Contains(node, ExpressionEqualityComparer.Instance))
				return false;

			if (Builder.CanBeEvaluatedOnClient(node))
				return false;

			LogVisit(node);

			var calculatedContext = BuildContext;
			if (node is ContextRefExpression contextRef)
				calculatedContext = contextRef.BuildContext;

			var onContext = calculatedContext;

			var traversed = BuildExpression(node, BuildPurpose.Traverse);

			if (_disableSubqueries.Contains(traversed, ExpressionEqualityComparer.Instance))
				return false;

			var cacheRoot = GetCacheRootContext(traversed);

			if (cacheRoot != null)
			{
				calculatedContext = cacheRoot.BuildContext;
				onContext = calculatedContext;
			}
			else
			{
				var root = BuildAggregationRoot(new ContextRefExpression(calculatedContext.ElementType, calculatedContext)) as ContextRefExpression;
				if (root != null)
				{
					calculatedContext = root.BuildContext;
					if (calculatedContext is GroupByBuilder.GroupByContext)
					{
						onContext = calculatedContext;
					}
				}
			}

			var cacheKey = new ExprCacheKey(traversed, null, null, calculatedContext.SelectQuery, ProjectFlags.SQL | ProjectFlags.Subquery);

			if (GetAlreadyTranslated(cacheKey, out var alreadyTranslated))
			{
				subqueryExpression = alreadyTranslated;
				return !IsSame(node, alreadyTranslated);
			}

			_disableSubqueries.Push(traversed);
			_disableSubqueries.Push(node);
			var ctx = GetSubQuery(node, onContext, out var isSequence, out var errorMessage);
			_disableSubqueries.Pop();
			_disableSubqueries.Pop();

			if (ctx is null || errorMessage is not null)
			{
				if (isSequence)
				{
					ctx?.Detach();

					if (_buildPurpose is BuildPurpose.Expression)
					{
						// Trying to relax eager for First[OrDefault](predicate)
						var prepared = PrepareSubqueryExpression(node);
						if (!IsSame(prepared, node))
						{
							subqueryExpression = prepared;
							return true;
						}

						if (ctx?.IsSingleElement == true)
						{
							subqueryExpression = new SqlErrorExpression(node, errorMessage, node.Type);
							return true;
						}

						return false;
					}

					if (_buildPurpose is BuildPurpose.Sql)
					{
						if (ctx?.IsSingleElement == true)
						{
							subqueryExpression = new SqlErrorExpression(node, errorMessage, node.Type);
							return true;
						}
					}
				}

				return false;
			}

			var isCollection = !ctx.IsSingleElement;
			if (isCollection && _buildPurpose is BuildPurpose.Expression)
			{
				var eager = new SqlEagerLoadExpression(node);
				subqueryExpression = SqlAdjustTypeExpression.AdjustType(eager, node.Type, MappingSchema);
				ctx.Detach();
			}
			else if (isCollection)
			{
				return false;
			}
			else
			{
				if (calculatedContext.SelectQuery != ctx.SelectQuery)
				{
					ctx = new ScopeContext(ctx, calculatedContext);
				}

				subqueryExpression = new ContextRefExpression(node.Type, ctx, alias: Alias);

				if (_buildFlags.HasFlag(BuildFlags.ForExpanding))
				{
					Expression testExpression;

					// Translate subqueries only if they are SQL
					using (UsingBuildFlags(_buildFlags & ~BuildFlags.ForExpanding))
					{
						testExpression = BuildSqlExpression(subqueryExpression);
					}

					if (testExpression is SqlPlaceholderExpression placeholder)
					{
						//snapshot.Accept();
						subqueryExpression = placeholder;
						return true;
					}

					ctx.Detach();

					return false;
				}
			}

			if (!isCollection)
				_translationCache[cacheKey] = subqueryExpression;

			return true;
		}

		public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
		{
			if (BuildContext != null && node.SelectQuery != BuildContext?.SelectQuery)
			{
				if (Alias != null)
					node = node.WithAlias(Alias);

				//return Builder.UpdateNesting(BuildContext!, node);
				return node;
			}

			return base.VisitSqlPlaceholderExpression(node);
		}

		internal override Expression VisitContextRefExpression(ContextRefExpression node)
		{
			LogVisit(node);

			var newNode = MakeWithCache(node.BuildContext, node);

			if (!IsSame(newNode, node))
			{
				if (_buildPurpose is BuildPurpose.Root or BuildPurpose.AssociationRoot or BuildPurpose.AggregationRoot)
				{
					FoundRoot = node;

					if (newNode is ContextRefExpression newRef)
						FoundRoot = newRef;
					else
						return node;

					if (_buildPurpose is BuildPurpose.Root)
					{
						if (newNode.Type != node.Type && !(node.Type.IsSameOrParentOf(newNode.Type) || newNode.Type.IsSameOrParentOf(node.Type)))
						{
							FoundRoot = node;
							return node;
						}
					}
				}

				if (newNode is ContextRefExpression && _buildPurpose is BuildPurpose.SubQuery)
				{
					return newNode;
				}

				return Visit(newNode);
			}
			else
			{
				var check = _buildPurpose is BuildPurpose.Expression or BuildPurpose.Sql;

				if (check)
				{
					if (HandleSubquery(node, out var transformed))
					{
						if (!IsSame(transformed, node))
							transformed = Visit(transformed);
						return transformed;
					}
				}
			}

			FoundRoot = node;

			return base.VisitContextRefExpression(node);
		}

		public ColumnDescriptor? SuggestColumnDescriptor(Expression expr)
		{
			expr = expr.Unwrap();

			Expression converted;

			using (UsingColumnDescriptor(null))
			{
				converted = Visit(expr);
			}

			if (converted is not SqlPlaceholderExpression placeholderTest)
				return null;

			var descriptor = QueryHelper.GetColumnDescriptor(placeholderTest.Sql);
			return descriptor;
		}

		public ColumnDescriptor? SuggestColumnDescriptor(Expression expr1, Expression expr2)
		{
			return SuggestColumnDescriptor(expr1) ?? SuggestColumnDescriptor(expr2);
		}

		public SqlPlaceholderExpression CreatePlaceholder(ISqlExpression sqlExpression, Expression path)
		{
			if (BuildContext == null)
				throw new InvalidOperationException("BuildContext is not initialized");

			var placeholder = ExpressionBuilder.CreatePlaceholder(BuildContext, sqlExpression, path, alias: Alias);
			return placeholder;
		}

		static bool HasContextReferenceOrSql(Expression expression)
		{
			return expression.Find(n => n is ContextRefExpression or SqlPlaceholderExpression) != null;
		}

		static bool IsPrimitiveConstant(Expression expression)
		{
			return expression.NodeType == ExpressionType.Constant && (expression.Type == typeof(int) || expression.Type == typeof(bool));
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			if (BuildContext == null || _buildPurpose is not (BuildPurpose.Sql or BuildPurpose.Expression or BuildPurpose.Expand))
				return base.VisitBinary(node);

			var shouldSkipSqlConversion = false;
			if (_buildPurpose is BuildPurpose.Expression)
			{
				if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
				{
					// Small tuning of final Expression generation
					//
					if (node.Left.IsNullValue() || node.Right.IsNullValue())
						shouldSkipSqlConversion = true;
					else if (SequenceHelper.IsSpecialProperty(node.Left, out _, out _) || SequenceHelper.IsSpecialProperty(node.Right, out _, out _))
						shouldSkipSqlConversion = true;
					else if (IsPrimitiveConstant(node.Left) || IsPrimitiveConstant(node.Right))
						shouldSkipSqlConversion = true;
				}
			}

			if (node.NodeType == ExpressionType.ArrayIndex && node.Left == ExpressionBuilder.ParametersParam)
			{
				return node;
			}

			// Handle client-side coalesce
			if (_buildPurpose is BuildPurpose.Expression && node.NodeType == ExpressionType.Coalesce && !_buildFlags.HasFlag(BuildFlags.ForSetProjection))
			{
				var right = Visit(node.Right);
				if (right is not SqlPlaceholderExpression)
				{
					return base.VisitBinary(node);
				}
			}

			if (!shouldSkipSqlConversion && TryConvertToSql(node, out var sqlResult))
			{
				return sqlResult;
			}

			if (HandleValue(node, out var sqlValue))
				return Visit(sqlValue);

			if (_buildPurpose is BuildPurpose.Expression)
				return base.VisitBinary(node);

			if (HandleBinary(node, out var translated))
				return translated; // Do not Visit again

			var exposed = Builder.ConvertSingleExpression(node);

			if (!IsSame(exposed, node))
				return Visit(exposed);

			return base.VisitBinary(node);
		}

		bool HandleBinary(BinaryExpression node, out Expression translated)
		{
			switch (node.NodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				{
					return HandleBinaryComparison(node, out translated);
				}

				case ExpressionType.AndAlso:
				case ExpressionType.OrElse:
				{
					return HandleBinaryLogical(node, out translated);
				}

				case ExpressionType.ArrayIndex:
				{
					if (HandleSqlRelated(node, out translated!))
					{
						translated = Visit(translated);
						return true;
					}

					if (HandleValue(node, out translated!))
					{
						translated = Visit(translated);
						return true;
					}

					break;
				}

				case ExpressionType.And:
				case ExpressionType.Or:
				{
					if (node.Type == typeof(bool))
						goto case ExpressionType.AndAlso;
					goto case ExpressionType.Add;
				}

				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Divide:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Power:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Coalesce:
				{
					return HandleBinaryMath(node, out translated);
				}
			}

			translated = node;
			return false;
		}

		bool HandleBinaryComparison(BinaryExpression node, out Expression translated)
		{
			translated = node;

			Expression left;
			Expression right;
			using (UsingAlias(null))
			using (UsingBuildFlags(_buildFlags | BuildFlags.ForKeys))
			using (UsingColumnDescriptor(null))
			{
				left = Visit(node.Left);
				right = Visit(node.Right);
			}

			if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
			{
				if (HandleEquality(node.NodeType is ExpressionType.NotEqual, left, right, out var optimized)
					|| HandleEquality(node.NodeType is ExpressionType.NotEqual, right, left, out optimized))

				{
					optimized = Visit(optimized);
					if (_buildPurpose is BuildPurpose.Sql && optimized is not SqlPlaceholderExpression)
						return true;

					translated = optimized;
					return true;
				}
			}

			Expression compareExpr;
			using (UsingAlias("cond"))
			using (UsingColumnDescriptor(null))
			{
				compareExpr = ConvertCompareExpression(node.NodeType, node.Left, node.Right, node);
			}

			if (!IsSame(compareExpr, node))
			{
				if (compareExpr is SqlErrorExpression error)
				{
					if (_buildPurpose is BuildPurpose.Expand || _buildFlags.HasFlag(BuildFlags.ForExpanding))
					{
						translated = base.VisitBinary(node);
						return true;
					}

					if (_buildPurpose is BuildPurpose.Sql && error.Message is null)
					{
						return true;
					}
				}

				translated = Visit(compareExpr);
				return true;
			}

			return false;
		}

		bool HandleBinaryLogical(BinaryExpression node, out Expression translated)
		{
			translated = node;

			var stack = new Stack<Expression>();

			List<Expression>? clientItems = null;
			List<Expression>? allItems    = null;

			var items        = new List<Expression>();
			var binary       = node;

			stack.Push(binary.Right);
			stack.Push(binary.Left);
			while (stack.Count > 0)
			{
				var item = stack.Pop();
				if (item.NodeType == node.NodeType)
				{
					binary = (BinaryExpression)item;
					stack.Push(binary.Right);
					stack.Push(binary.Left);
				}
				else
				{
					if (!CanTryHandleValue(item))
					{
						items.Add(item);
					}
					else
					{
						(clientItems ??= []).Add(item);
						allItems ??= [.. items];
					}

					allItems?.Add(item);
				}
			}

			var predicates = new List<ISqlPredicate?>(items.Count + clientItems?.Count > 0 ? 1 : 0);
			var hasError   = false;

			using var saveAlias            = UsingAlias("cond");
			using var saveColumnDescriptor = UsingColumnDescriptor(null);

			var errorOffset = 0;

			if (clientItems?.Count > 1)
			{
				var clientCondition = clientItems.Aggregate(node.NodeType == ExpressionType.AndAlso ? Expression.AndAlso : Expression.OrElse);

				if (HandleValue(clientCondition, out var translatedValue))
				{
					translatedValue = Visit(translatedValue);

					if (translatedValue is SqlPlaceholderExpression valuePlaceholder)
					{
						var valuePredicateSql = ConvertExpressionToPredicate(valuePlaceholder.Sql);
						if (valuePredicateSql != null)
						{
							predicates.Add(valuePredicateSql);
							errorOffset = 1;
						}
					}
				}
			}

			if (predicates.Count == 0 && clientItems != null)
			{
				items = allItems!;
			}

			foreach (var predicateExpr in items)
			{
				var            translatedPredicate = Visit(predicateExpr);
				ISqlPredicate? predicateSql        = null;

				if (translatedPredicate is SqlPlaceholderExpression placeholder)
				{
					predicateSql = ConvertExpressionToPredicate(placeholder.Sql);
				}

				if (predicateSql is null)
				{
					if (_buildPurpose is BuildPurpose.Sql)
					{
						if (translatedPredicate is SqlErrorExpression)
							translated = SqlErrorExpression.EnsureError(translatedPredicate, typeof(bool));
						else
							translated = SqlErrorExpression.EnsureError(predicateExpr, typeof(bool));
						return true;
					}

					hasError = true;
				}

				predicates.Add(predicateSql);
			}

			translated = node;

			if (hasError)
			{
				// replace translated nodes
				for (var index = errorOffset; index < predicates.Count; index++)
				{
					var predicateSql = predicates[index];
					var itemNode     = items[index - errorOffset];
					if (predicateSql is not null)
					{
						if (predicateSql is not ISqlExpression sqlExpr)
						{
							sqlExpr = new SqlSearchCondition(false, canBeUnknown: null, predicateSql);
						}

						var placeholder = CreatePlaceholder(sqlExpr, itemNode);
						translated = translated.Replace(itemNode, placeholder);
					}
					else
					{
						var translatedNode = Visit(itemNode);
						if (!ReferenceEquals(itemNode, translatedNode))
						{
							translated = translated.Replace(itemNode, translatedNode);
						}
					}
				}

				return true;
			}

			var condition = new SqlSearchCondition(node.NodeType is ExpressionType.OrElse or ExpressionType.Or, canBeUnknown: null, predicates!);
			translated = CreatePlaceholder(condition, node);

			return true;
		}

		bool HandleBinaryMath(BinaryExpression node, out Expression translated)
		{
			translated = node;

			var left  = node.Left;
			var right = node.Right;

			var shouldCheckColumn = node.Left.Type.UnwrapNullableType() == node.Right.Type.UnwrapNullableType();

			if (shouldCheckColumn)
			{
				right = right.Unwrap();
			}
			else
			{
				left = left.Unwrap();
				right = right.Unwrap();
			}

			ColumnDescriptor? columnDescriptor = null;
			switch (node.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.Divide:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Or:
				case ExpressionType.Power:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Coalesce:

				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				{
					columnDescriptor = SuggestColumnDescriptor(left) ?? SuggestColumnDescriptor(right);
					break;
				}
			}

			if (left.Type != right.Type)
			{
				if (!left.Type.IsEnum && right.Type.IsEnum)
				{
					// do nothing
				}
				else if (left.Type.UnwrapNullableType() != right.Type.UnwrapNullableType())
					columnDescriptor = null;
			}

			Expression leftExpr;
			Expression rightExpr;

			using (UsingColumnDescriptor(columnDescriptor))
			{
				leftExpr = UpdateNesting(Visit(left));
				rightExpr = UpdateNesting(Visit(right));
			}

			if (leftExpr is not SqlPlaceholderExpression leftPlaceholder || rightExpr is not SqlPlaceholderExpression rightPlaceholder)
			{
				if (leftExpr is SqlErrorExpression leftError)
					translated = leftError.WithType(node.Type);
				else if (rightExpr is SqlErrorExpression rightError)
					translated = rightError.WithType(node.Type);
				else
					translated = base.VisitBinary(node);

				return true;
			}

			var l = leftPlaceholder.Sql;
			var r = rightPlaceholder.Sql;
			var t = node.Type;

			switch (node.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "+", r, Precedence.Additive), node); break;
				case ExpressionType.And: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "&", r, Precedence.Bitwise), node); break;
				case ExpressionType.Divide: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "/", r, Precedence.Multiplicative), node); break;
				case ExpressionType.ExclusiveOr: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "^", r, Precedence.Bitwise), node); break;
				case ExpressionType.Modulo: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "%", r, Precedence.Multiplicative), node); break;
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "*", r, Precedence.Multiplicative), node); break;
				case ExpressionType.Or: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "|", r, Precedence.Bitwise), node); break;
				case ExpressionType.Power: translated = CreatePlaceholder(new SqlFunction(MappingSchema.GetDbDataType(t), "Power", l, r), node); break;
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked: translated = CreatePlaceholder(new SqlBinaryExpression(t, l, "-", r, Precedence.Subtraction), node); break;
				case ExpressionType.Coalesce: translated = CreatePlaceholder(new SqlCoalesceExpression(l, r), node); break;
				default:
					return false;
			}

			return true;
		}

		public override Expression VisitSqlValidateExpression(SqlValidateExpression node)
		{
			if (_buildPurpose == BuildPurpose.Sql && _buildFlags.HasFlag(BuildFlags.ForKeys))
			{
				if (node.InnerExpression is SqlPlaceholderExpression)
					return node.InnerExpression;
			}

			return base.VisitSqlValidateExpression(node);
		}

		static Expression SimplifyConvert(Expression expression)
		{
			expression = expression.UnwrapConvert();
			if (expression.NodeType == ExpressionType.TypeAs)
			{
				var unary = (UnaryExpression)expression;
				if (unary.Operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
					return SimplifyConvert(unary.Operand);
			}

			return expression;
		}

		bool HandleEquality(bool isNot, Expression expr1, Expression expr2, [NotNullWhen(true)] out Expression? result)
		{
			if (IsNull(expr1) == true)
			{
				if (expr2 is SqlAdjustTypeExpression)
				{
					// Usually SqlAdjustTypeExpression is created during collection navigation property translation or EagerLoading, so we can transform null equality to Any
					var elementType = expr2.Type.TryGetElementType(typeof(IEnumerable<>));
					if (elementType != null)
					{
						result = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [elementType], expr2);
						if (!isNot)
							result = Expression.Not(result);

						return true;
					}
				}
				else
				{
					var unwrapped2 = SimplifyConvert(expr2);
					if (unwrapped2 is SqlGenericConstructorExpression)
					{
						result = ExpressionInstances.Boolean(isNot);
						return true;
					}

					if (unwrapped2 is SqlDefaultIfEmptyExpression defaultIfEmpty)
					{
						var testCondition = defaultIfEmpty.NotNullExpressions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);
						if (!isNot)
							testCondition = Expression.Not(testCondition);
						result = testCondition;
						return true;
					}

					if (unwrapped2 is ConditionalExpression conditional)
					{
						if (IsNull(conditional.IfTrue) == true)
						{
							if (!isNot)
							{
								result = conditional.Test;
							}
							else
							{
								result = Expression.Not(conditional.Test);
							}

							return true;
						}
					}
				}
			}

			result = null;
			return false;
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression node)
		{
			if (IsSqlOrExpression())
			{
				var tableContext = SequenceHelper.GetTableContext(Builder, node.Expression);

				if (tableContext != null)
				{
					return Visit(MakeIsPredicateExpression(tableContext, node));
				}
			}

			return base.VisitTypeBinary(node);
		}

		static bool? IsNull(Expression sqlExpr)
		{
			if (sqlExpr.IsNullValue())
				return true;

			if (sqlExpr is SqlGenericConstructorExpression or MemberInitExpression or NewExpression)
				return false;

			if (sqlExpr is not SqlPlaceholderExpression placeholder)
				return null;

			return QueryHelper.IsNullValue(placeholder.Sql);
		}

		#region SearchCondition

		public void BuildSearchCondition(IBuildContext? context, Expression expression, SqlSearchCondition searchCondition)
		{
			if (!BuildSearchCondition(context, expression, searchCondition, out var error))
			{
				throw error.CreateException();
			}
		}

		static Expression? GetSearchConditionError(Expression expression)
		{
			var found = SequenceHelper.FindError(expression);

			if (found != null)
				return found;

			return FindErrorExpression(expression);

			static Expression? FindErrorExpression(Expression expression)
			{
				if (expression is BinaryExpression binary)
				{
					if (binary.Left is not SqlPlaceholderExpression && binary.Right is not SqlPlaceholderExpression)
						return expression;

					return FindErrorExpression(binary.Left) ?? FindErrorExpression(binary.Right);
				}

				if (expression is SqlPlaceholderExpression)
					return null;

				return expression;
			}
		}

		public bool BuildSearchCondition(IBuildContext? context, Expression expression, SqlSearchCondition searchCondition, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			using (UsingBuildContext(context ?? BuildContext))
			using (UsingBuildPurpose(BuildPurpose.Sql))
			{
				var result = Visit(expression);

				if (result is SqlPlaceholderExpression placeholder)
				{
					searchCondition.Add(ConvertExpressionToPredicate(placeholder.Sql));
					error = null;
					return true;
				}

				var errorExpr = GetSearchConditionError(result);
				if (errorExpr != null)
					error = SqlErrorExpression.EnsureError(errorExpr, expression.Type);
				else
					error = SqlErrorExpression.EnsureError(result, expression.Type);
				return false;
			}
		}

		#endregion

		Expression ConvertPredicateMethod(MethodCallExpression node)
		{
			ISqlExpression? IsCaseSensitive(MethodCallExpression mc)
			{
				if (mc.Arguments.Count <= 1)
					return new SqlValue(typeof(bool?), null);

				if (!typeof(StringComparison).IsSameOrParentOf(mc.Arguments[1].Type))
					return new SqlValue(typeof(bool?), null);

				var arg = mc.Arguments[1];

				if (arg.NodeType == ExpressionType.Constant || arg.NodeType == ExpressionType.Default)
				{
					var comparison = (StringComparison)(Builder.EvaluateExpression(arg) ?? throw new InvalidOperationException());
					return new SqlValue(comparison is StringComparison.CurrentCulture
						or StringComparison.InvariantCulture
						or StringComparison.Ordinal);
				}

				var variable   = Expression.Variable(typeof(StringComparison), "c");
				var assignment = Expression.Assign(variable, arg);
				var expr       = (Expression)Expression.Equal(variable, Expression.Constant(StringComparison.CurrentCulture));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.InvariantCulture)));
				expr = Expression.OrElse(expr, Expression.Equal(variable, Expression.Constant(StringComparison.Ordinal)));
				expr = Expression.Block(new[] { variable }, assignment, expr);

				var parameter = Builder.ParametersContext.BuildParameter(BuildContext, expr, columnDescriptor : null);
				if (parameter != null)
				{
					parameter.IsQueryParameter = false;
				}

				return parameter;
			}

			ISqlPredicate? predicate = null;

			if (node is { Method.Name: "Equals", Object: { }, Arguments.Count: 1 })
				return ConvertCompareExpression(ExpressionType.Equal, node.Object, node.Arguments[0]);

			using (UsingBuildFlags((_buildFlags | BuildFlags.ForKeys) & ~BuildFlags.ForMemberRoot))
			{
				switch (node)
				{
					case { Method: { DeclaringType.IsStringType: true, Name: nameof(string.Contains) } }:
						predicate = CreateStringPredicate(node, SqlPredicate.SearchString.SearchKind.Contains, IsCaseSensitive(node));
						break;

					case { Method: { DeclaringType.IsStringType: true, Name: nameof(string.StartsWith) } }:
						predicate = CreateStringPredicate(node, SqlPredicate.SearchString.SearchKind.StartsWith, IsCaseSensitive(node));
						break;

					case { Method: { DeclaringType.IsStringType: true, Name: nameof(string.EndsWith) } }:
						predicate = CreateStringPredicate(node, SqlPredicate.SearchString.SearchKind.EndsWith, IsCaseSensitive(node));
						break;

					// static Contains(this src, item) extension methods
					case { Method: { DeclaringType: { } type, Name: nameof(Enumerable.Contains) } } when (
						type == typeof(Enumerable) ||
						(type == typeof(Queryable) && node.Arguments.Count == 2 && Builder.CanBeEvaluatedOnClient(node.Arguments[0]))
					):
						predicate = ConvertInPredicate(node.Arguments[1], node.Arguments[0]);
						break;

					// src.Contains(item) instance methods
					case { Method: { DeclaringType: { } type, Name: nameof(IList.Contains) } } when (
						typeof(IList).IsSameOrParentOf(type) ||
						typeof(ICollection<>).IsSameOrParentOf(type) ||
						// IReadOnlyCollection<> doesn't declare Contains(), but derived (readonly) collection classes could.
						typeof(IReadOnlyCollection<>).IsSameOrParentOf(type)
					):
						predicate = ConvertInPredicate(node.Arguments[0], node.Object!);
						break;

#if NET8_0_OR_GREATER
					case
					{
						Method: { DeclaringType.IsMemoryExtensionsType: true, Name: nameof(MemoryExtensions.Contains) },
						Arguments:
						[
							MethodCallExpression
						{
							Method.Name: "op_Implicit",
							Type.Name: "ReadOnlySpan`1" or "Span`1",
							Arguments: [var spanSource],
						},
							var value,
							..
						]
					}:
						predicate = ConvertInPredicate(value, spanSource!.UnwrapConvertToSelf());
						break;
#endif

					case { Method: { DeclaringType: { } type, Name: nameof(Dictionary<,>.ContainsValue) } } when (
						typeof(Dictionary<,>).IsSameOrParentOf(type)
					):
						predicate = ConvertInPredicate(node.Arguments[0], ExpressionHelper.PropertyOrField(node.Object!, "Values"));
						break;

					case { Method: { DeclaringType: { } type, Name: nameof(IDictionary<,>.ContainsKey) } } when (
						typeof(IDictionary<,>).IsSameOrParentOf(type) ||
						typeof(IReadOnlyDictionary<,>).IsSameOrParentOf(type)
					):
						predicate = ConvertInPredicate(node.Arguments[0], ExpressionHelper.PropertyOrField(node.Object!, "Keys"));
						break;
				}
			}

			if (predicate != null)
			{
				var condition = new SqlSearchCondition(false).Add(predicate);
				return CreatePlaceholder(condition, node);
			}

			return node;
		}

		public TExpression UpdateNesting<TExpression>(TExpression expression)
			where TExpression : Expression
		{
			if (BuildContext == null)
				return expression;

			var corrected = Builder.UpdateNesting(BuildContext.SelectQuery, expression);

			return corrected;
		}

		ISqlPredicate ConvertExpressionToPredicate(ISqlExpression sqlExpression, bool withNull = false, bool forceEquality = false)
		{
			if (sqlExpression is ISqlPredicate predicate)
				return predicate;

			if (sqlExpression is SqlParameterizedExpressionBase { IsPredicate: true })
				return new SqlPredicate.Expr(sqlExpression);

			var columnDescriptor = QueryHelper.GetColumnDescriptor(sqlExpression);
			var valueConverter   = columnDescriptor?.ValueConverter;

			if (!Builder.DataContext.SqlProviderFlags.SupportsBooleanType
				|| forceEquality
				|| valueConverter != null
				|| (columnDescriptor != null && columnDescriptor.GetDbDataType(true).DataType is not DataType.Boolean))
			{
				using (UsingColumnDescriptor(columnDescriptor))
				{
					SqlPlaceholderExpression? trueValue;
					SqlPlaceholderExpression? falseValue;

					using (UsingAlias("true_value"))
					{
						trueValue = Visit(ExpressionInstances.True) as SqlPlaceholderExpression;
					}

					using (UsingAlias("false_value"))
					{
						falseValue = Visit(ExpressionInstances.False) as SqlPlaceholderExpression;
					}

					if (trueValue != null && falseValue != null)
					{
						predicate = new SqlPredicate.IsTrue(sqlExpression, trueValue.Sql, falseValue.Sql, withNull && DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null, false);
						return predicate;
					}
				}
			}

			predicate = new SqlPredicate.Expr(sqlExpression);

			return predicate;
		}

		ISqlPredicate? MakeIsTrueCheck(ISqlExpression sqlExpression)
		{
			var descriptor = QueryHelper.GetColumnDescriptor(sqlExpression);

			using (UsingColumnDescriptor(descriptor))
			{
				var trueValue  = UpdateNesting(Visit(ExpressionInstances.True));
				var falseValue = UpdateNesting(Visit(ExpressionInstances.False));

				if (trueValue is not SqlPlaceholderExpression trueSql || falseValue is not SqlPlaceholderExpression falseSql)
					return null;

				return new SqlPredicate.IsTrue(sqlExpression, trueSql.Sql, falseSql.Sql, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null, false);
			}
		}

		static bool IsNullExpression(Expression expression)
		{
			if (expression.IsNullValue())
				return true;
			if (expression is SqlPlaceholderExpression placeholder)
				return placeholder.Sql.IsNullValue();
			return false;
		}

		#region ConvertCompare

		public bool TryGenerateComparison(
			IBuildContext? context,
			Expression left,
			Expression right,
			[NotNullWhen(true)] out SqlSearchCondition? searchCondition,
			[NotNullWhen(false)] out SqlErrorExpression? error,
			BuildPurpose? buildPurpose = default)
		{
			using (UsingBuildContext(context))
			using (UsingBuildPurpose(buildPurpose ?? _buildPurpose))
			{
				var expr = ConvertCompareExpression(ExpressionType.Equal, left, right);

				if (expr is SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
				{
					searchCondition = sc;
					error = null;
					return true;
				}

				searchCondition = null;
				error = SqlErrorExpression.EnsureError(expr, typeof(bool));

				return false;
			}
		}

		public SqlSearchCondition GenerateComparison(
			IBuildContext? context,
			Expression left,
			Expression right,
			BuildPurpose? buildPurpose = default)
		{
			using (UsingBuildContext(context))
			using (UsingBuildPurpose(buildPurpose ?? _buildPurpose))
			{
				var expr = ConvertCompareExpression(ExpressionType.Equal, left, right);

				if (expr is SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
					return sc;

				if (expr is SqlErrorExpression error)
					throw error.CreateException();

				throw new SqlErrorExpression($"Could not compare '{SqlErrorExpression.PrepareExpressionString(left)}' with {SqlErrorExpression.PrepareExpressionString(right)}", typeof(bool)).CreateException();
			}
		}

		Expression ConvertCompareExpression(ExpressionType nodeType, Expression left, Expression right, Expression? originalExpression = null)
		{
			Expression GetOriginalExpression()
			{
				if (originalExpression != null)
					return originalExpression;

				var rightExpr = right;
				var leftExpr  = left;
				if (rightExpr.Type != leftExpr.Type)
				{
					if (rightExpr.Type.CanConvertTo(leftExpr.Type))
						rightExpr = Expression.Convert(rightExpr, leftExpr.Type);
					else if (left.Type.CanConvertTo(leftExpr.Type))
						leftExpr = Expression.Convert(leftExpr, right.Type);
				}
				else
				{
					if (nodeType == ExpressionType.Equal || nodeType == ExpressionType.NotEqual)
					{
						// Fore generating Path for SqlPlaceholderExpression
						if (!rightExpr.Type.IsPrimitive)
						{
							return new SqlPathExpression(
								new[] { leftExpr, Expression.Constant(nodeType), rightExpr },
								typeof(bool));
						}
					}
				}

				return Expression.MakeBinary(nodeType, leftExpr, rightExpr);
			}

			Expression GenerateNullComparison(Expression placeholdersExpression, bool isNot)
			{
				var condition = CollectNullCompareExpressionExpression(placeholdersExpression);

				if (condition == null)
					return GetOriginalExpression();

				if (isNot)
					condition = Expression.Not(condition);

				condition = OptimizeExpression(condition);

				var converted = Visit(condition);

				if (converted is not SqlPlaceholderExpression)
					return GetOriginalExpression();

				return converted;
			}

			Expression? CollectNullCompareExpressionExpression(Expression current)
			{
				if (IsNullExpression(current))
				{
					return ExpressionInstances.True;
				}

				switch (current.NodeType)
				{
					case ExpressionType.Constant:
					case ExpressionType.Default:
					{
						if (current.Type.IsValueType)
							return null;

						return Expression.Equal(current, Expression.Constant(null, current.Type));
					}
				}

				if (current is SqlPlaceholderExpression)
				{
					if (current.Type.IsValueType)
						return null;

					return Expression.Equal(current, Expression.Constant(null, current.Type));
				}

				if (current is SqlGenericConstructorExpression generic)
				{
					return ExpressionInstances.False;
				}

				if (current is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
				{
					var testCondition = Expression.Not(defaultIfEmptyExpression.NotNullExpressions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.OrElse));
					return testCondition;
				}

				if (current is ConditionalExpression conditionalExpression)
				{
					var trueCondition  = CollectNullCompareExpressionExpression(conditionalExpression.IfTrue);
					if (trueCondition == null)
						return null;

					var falseCondition = CollectNullCompareExpressionExpression(conditionalExpression.IfFalse);
					if (falseCondition == null)
						return null;

					return Expression.OrElse(
						Expression.AndAlso(conditionalExpression.Test, trueCondition),
						Expression.AndAlso(Expression.Not(conditionalExpression.Test), falseCondition));
				}

				if (current is ContextRefExpression { BuildContext: IBuildProxy proxy })
				{
					return CollectNullCompareExpressionExpression(proxy.InnerExpression);
				}

				return null;
			}

			Expression GeneratePathComparison(Expression leftOriginal, Expression leftParsed, Expression rightOriginal, Expression rightParsed)
			{
				var predicateExpr = GeneratePredicate(leftOriginal, leftParsed, rightOriginal, rightParsed);
				if (predicateExpr == null)
					return GetOriginalExpression();

				var converted = Visit(predicateExpr);
				if (converted is not SqlPlaceholderExpression)
					converted = GetOriginalExpression();

				return converted;
			}

			Expression? GeneratePredicate(Expression leftOriginal, Expression leftParsed, Expression rightOriginal, Expression rightParsed)
			{
				Expression? predicateExpr = null;

				if (leftParsed is SqlGenericConstructorExpression genericLeft)
				{
					predicateExpr = BuildPredicateExpression(genericLeft, null, rightOriginal);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (rightParsed is SqlGenericConstructorExpression genericRight)
				{
					predicateExpr = BuildPredicateExpression(genericRight, null, leftOriginal);
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (leftParsed is ConditionalExpression condLeft)
				{
					if (condLeft.IfTrue is SqlGenericConstructorExpression genericTrue)
					{
						predicateExpr = BuildPredicateExpression(genericTrue, leftOriginal, rightOriginal);
					}
					else if (condLeft.IfFalse is SqlGenericConstructorExpression genericFalse)
					{
						predicateExpr = BuildPredicateExpression(genericFalse, leftOriginal, rightOriginal);
					}

					/*if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, condLeft.IfTrue, rightOriginal, rightParsed);
					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, condLeft.IfFalse, rightOriginal, rightParsed);*/
				}

				if (predicateExpr != null)
					return predicateExpr;

				if (rightParsed is ConditionalExpression condRight)
				{
					if (condRight.IfTrue is SqlGenericConstructorExpression genericTrue)
					{
						predicateExpr = BuildPredicateExpression(genericTrue, leftOriginal, rightOriginal);
					}
					else if (condRight.IfFalse is SqlGenericConstructorExpression genericFalse)
					{
						predicateExpr = BuildPredicateExpression(genericFalse, leftOriginal, rightOriginal);
					}

					/*if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, leftParsed, condRight.IfTrue, rightParsed);
					if (predicateExpr == null)
						predicateExpr = GeneratePredicate(leftOriginal, leftParsed, condRight.IfFalse, rightParsed);*/
				}

				if (predicateExpr != null)
					return predicateExpr;

				return predicateExpr;
			}

			Expression? BuildPredicateExpression(SqlGenericConstructorExpression genericConstructor, Expression? rootLeft, Expression rootRight)
			{
				if (genericConstructor.Assignments.Count == 0)
					return null;

				var operations = genericConstructor.Assignments
					.Select(a => Expression.Equal(
						rootLeft == null ? a.Expression : Expression.MakeMemberAccess(rootLeft, a.MemberInfo),
						Expression.MakeMemberAccess(rootRight, a.MemberInfo))
					);

				var result = (Expression)operations.Aggregate(Expression.AndAlso);
				if (nodeType == ExpressionType.NotEqual)
					result = Expression.Not(result);

				return result;
			}

			Expression GenerateConstructorComparison(SqlGenericConstructorExpression leftConstructor, SqlGenericConstructorExpression rightConstructor)
			{
				var strict = leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.Full &&
							 rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.Full ||
							 (leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.New &&
							  rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.New) ||
							 (leftConstructor.ConstructType  == SqlGenericConstructorExpression.CreateType.MemberInit &&
							  rightConstructor.ConstructType == SqlGenericConstructorExpression.CreateType.MemberInit);

				var isNot           = nodeType == ExpressionType.NotEqual;
				var searchCondition = new SqlSearchCondition(isNot);
				var usedMembers     = new HashSet<MemberInfo>(MemberInfoEqualityComparer.Default);

				foreach (var leftAssignment in leftConstructor.Assignments)
				{
					var found = rightConstructor.Assignments.FirstOrDefault(a =>
						MemberInfoEqualityComparer.Default.Equals(a.MemberInfo, leftAssignment.MemberInfo));

					if (found == null && strict)
					{
						// fail fast and prepare correct error expression
						return new SqlErrorExpression(Expression.MakeMemberAccess(right, leftAssignment.MemberInfo));
					}

					var rightExpression = found?.Expression;
					if (rightExpression == null)
					{
						rightExpression = Expression.Default(leftAssignment.Expression.Type);
					}
					else
					{
						usedMembers.Add(found!.MemberInfo);
					}

					var predicateExpr = ConvertCompareExpression(nodeType, leftAssignment.Expression, rightExpression);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
					{
						if (strict)
						{
							if (leftAssignment.Expression is SqlPlaceholderExpression && rightExpression is not SqlPlaceholderExpression)
								return SqlErrorExpression.EnsureError(rightExpression, typeof(bool));
							if (leftAssignment.Expression is not SqlPlaceholderExpression && rightExpression is SqlPlaceholderExpression)
								return SqlErrorExpression.EnsureError(leftAssignment.Expression, typeof(bool));
							return GetOriginalExpression();
						}

						continue;
					}

					searchCondition.Predicates.Add(sc.MakeNot(isNot));
				}

				foreach (var rightAssignment in rightConstructor.Assignments)
				{
					if (usedMembers.Contains(rightAssignment.MemberInfo))
						continue;

					if (strict)
					{
						// fail fast and prepare correct error expression
						return new SqlErrorExpression(Expression.MakeMemberAccess(left, rightAssignment.MemberInfo));
					}

					var leftExpression = Expression.Default(rightAssignment.Expression.Type);

					var predicateExpr = ConvertCompareExpression(nodeType, leftExpression, rightAssignment.Expression);
					if (predicateExpr is not SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
					{
						if (strict)
							return predicateExpr;
						continue;
					}

					searchCondition.Predicates.Add(sc.MakeNot(isNot));
				}

				if (usedMembers.Count == 0)
				{
					if (leftConstructor.Parameters.Count > 0 && leftConstructor.Parameters.Count == rightConstructor.Parameters.Count)
					{
						for (var index = 0; index < leftConstructor.Parameters.Count; index++)
						{
							var leftParam  = leftConstructor.Parameters[index];
							var rightParam = rightConstructor.Parameters[index];

							var predicateExpr = ConvertCompareExpression(nodeType, leftParam.Expression, rightParam.Expression);
							if (predicateExpr is not SqlPlaceholderExpression { Sql: SqlSearchCondition sc })
							{
								if (strict)
									return GetOriginalExpression();
								continue;
							}

							searchCondition.Predicates.Add(sc.MakeNot(isNot));
						}

					}
					else
						return GetOriginalExpression();
				}

				return CreatePlaceholder(searchCondition, GetOriginalExpression());
			}

			if (!RestoreCompare(ref left, ref right))
				RestoreCompare(ref right, ref left);

			if (BuildContext == null)
				throw new InvalidOperationException();

			ISqlExpression? l = null;
			ISqlExpression? r = null;

			var nullability = NullabilityContext.GetContext(BuildContext.SelectQuery);

			using var saveFlags      = UsingBuildFlags((_buildFlags | BuildFlags.ForKeys) & ~BuildFlags.ForMemberRoot);
			using var saveDescriptor = UsingColumnDescriptor(SuggestColumnDescriptor(left, right));

			var leftExpr = Visit(left);
			if (leftExpr is SqlErrorExpression errorLeft)
				return errorLeft.WithType(typeof(bool));

			var rightExpr = Visit(right);
			if (rightExpr is SqlErrorExpression errorRight)
				return errorRight.WithType(typeof(bool));

			leftExpr = Builder.UpdateNesting(BuildContext, leftExpr);
			rightExpr = Builder.UpdateNesting(BuildContext, rightExpr);

			var compareNullsAsValues = Builder.CompareNulls is CompareNulls.LikeClr or CompareNulls.LikeSqlExceptParameters;

			//SQLRow case when needs to add Single
			//
			if (leftExpr is SqlPlaceholderExpression { Sql: SqlRowExpression } && rightExpr is not SqlPlaceholderExpression)
			{
				var elementType = TypeHelper.GetEnumerableElementType(rightExpr.Type);
				var singleCall  = Expression.Call(Methods.Enumerable.Single.MakeGenericMethod(elementType), right);
				rightExpr = Visit(singleCall);
			}
			else if (rightExpr is SqlPlaceholderExpression { Sql: SqlRowExpression } &&
					 leftExpr is not SqlPlaceholderExpression)
			{
				var elementType = TypeHelper.GetEnumerableElementType(leftExpr.Type);
				var singleCall  = Expression.Call(Methods.Enumerable.Single.MakeGenericMethod(elementType), left);
				leftExpr = Visit(singleCall);
			}

			leftExpr = RemoveNullPropagation(leftExpr, toSql: true);
			rightExpr = RemoveNullPropagation(rightExpr, toSql: true);

			if (leftExpr is SqlErrorExpression leftError)
				return leftError.WithType(typeof(bool));

			if (rightExpr is SqlErrorExpression rightError)
				return rightError.WithType(typeof(bool));

			if (leftExpr is SqlPlaceholderExpression placeholderLeft)
			{
				l = placeholderLeft.Sql;
			}

			if (rightExpr is SqlPlaceholderExpression placeholderRight)
			{
				r = placeholderRight.Sql;
			}

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					var isNot = nodeType == ExpressionType.NotEqual;

					if (l != null && r != null)
						break;

					leftExpr  = Builder.ParseGenericConstructor(leftExpr.UnwrapAdjustType(),  ProjectFlags.SQL | ProjectFlags.Keys, CurrentDescriptor);
					rightExpr = Builder.ParseGenericConstructor(rightExpr.UnwrapAdjustType(), ProjectFlags.SQL | ProjectFlags.Keys, CurrentDescriptor);

					if (SequenceHelper.UnwrapDefaultIfEmpty(leftExpr) is SqlGenericConstructorExpression leftGenericConstructor &&
						SequenceHelper.UnwrapDefaultIfEmpty(rightExpr) is SqlGenericConstructorExpression rightGenericConstructor)
					{
						return GenerateConstructorComparison(leftGenericConstructor, rightGenericConstructor);
					}

					if (IsNullExpression(left))
					{
						rightExpr = Visit(rightExpr);

						if (rightExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SqlSearchCondition rightSearchCond } } && rightSearchCond.Predicates.Count == 1)
						{
							var rightPredicate  = rightSearchCond.Predicates[0];
							var localIsNot = isNot;

							if (rightPredicate is SqlPredicate.IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(new SqlSearchCondition(false, canBeUnknown: null, isnull), GetOriginalExpression());

								return CreatePlaceholder(new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.IsNull(isnull.Expr1, !isnull.IsNot)), GetOriginalExpression());
							}
						}

						return GenerateNullComparison(rightExpr, isNot);
					}

					if (IsNullExpression(right))
					{
						leftExpr = Visit(leftExpr);

						if (leftExpr is ConditionalExpression { Test: SqlPlaceholderExpression { Sql: SqlSearchCondition leftSearchCond } } && leftSearchCond.Predicates.Count == 1)
						{
							var leftPredicate  = leftSearchCond.Predicates[0];
							var localIsNot = isNot;

							if (leftPredicate is SqlPredicate.IsNull isnull)
							{
								if (isnull.IsNot == localIsNot)
									return CreatePlaceholder(new SqlSearchCondition(false, canBeUnknown: null, isnull), GetOriginalExpression());

								return CreatePlaceholder(new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.IsNull(isnull.Expr1, !isnull.IsNot)), GetOriginalExpression());
							}
						}

						return GenerateNullComparison(leftExpr, isNot);
					}

					if (l == null || r == null)
					{
						var pathComparison = GeneratePathComparison(left, SequenceHelper.UnwrapDefaultIfEmpty(leftExpr), right, SequenceHelper.UnwrapDefaultIfEmpty(rightExpr));

						return pathComparison;
					}

					break;
			}

			var op = nodeType switch
			{
				ExpressionType.Equal              => SqlPredicate.Operator.Equal,
				ExpressionType.NotEqual           => SqlPredicate.Operator.NotEqual,
				ExpressionType.GreaterThan        => SqlPredicate.Operator.Greater,
				ExpressionType.GreaterThanOrEqual => SqlPredicate.Operator.GreaterOrEqual,
				ExpressionType.LessThan           => SqlPredicate.Operator.Less,
				ExpressionType.LessThanOrEqual    => SqlPredicate.Operator.LessOrEqual,
				_                                 => throw new InvalidOperationException(),
			};

			if ((left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked || right.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked) && (op == SqlPredicate.Operator.Equal || op == SqlPredicate.Operator.NotEqual))
			{
				var p = ConvertEnumConversion(left, op, right);
				if (p != null)
					return CreatePlaceholder(new SqlSearchCondition(false, canBeUnknown: null, p), GetOriginalExpression());
			}

			if (l is null)
			{
				if (Visit(left) is not SqlPlaceholderExpression leftPlaceholder)
					return GetOriginalExpression();
				l = leftPlaceholder.Sql;
			}

			if (r is null)
			{
				if (Visit(right) is not SqlPlaceholderExpression rightPlaceholder)
					return GetOriginalExpression();
				r = rightPlaceholder.Sql;
			}

			var lOriginal = l;
			var rOriginal = r;

			l = QueryHelper.UnwrapExpression(l, checkNullability: true);
			r = QueryHelper.UnwrapExpression(r, checkNullability: true);

			if (l is SqlValue lValue)
				lValue.ValueType = GetDataType(r, lValue.ValueType, MappingSchema);

			if (r is SqlValue rValue)
				rValue.ValueType = GetDataType(l, rValue.ValueType, MappingSchema);

			/*switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:

					if (!BuildContext!.SelectQuery.IsParameterDependent &&
						(l is SqlParameter && lOriginal.CanBeNullable(nullability) || r is SqlParameter && r.CanBeNullable(nullability)))
					{
						BuildContext.SelectQuery.IsParameterDependent = true;
					}

					break;
			}*/

			ISqlPredicate? predicate = null;

			var isEquality = op == SqlPredicate.Operator.Equal || op == SqlPredicate.Operator.NotEqual
				? op == SqlPredicate.Operator.Equal
				: (bool?)null;

			// TODO: maybe remove
			if (l is SqlSearchCondition lsc)
			{
				if (isEquality != null & IsBooleanConstant(rightExpr, out var boolRight) && boolRight != null)
				{
					predicate = lsc.MakeNot(boolRight != isEquality);
				}
			}

			// TODO: maybe remove
			if (r is SqlSearchCondition rsc)
			{
				if (isEquality != null & IsBooleanConstant(rightExpr, out var boolLeft) && boolLeft != null)
				{
					predicate = rsc.MakeNot(boolLeft != isEquality);
				}
			}

			if (predicate == null)
			{
				if (isEquality != null)
				{
					bool?           value;
					ISqlExpression? expression  = null;

					if (IsBooleanConstant(left, out value))
					{
						if (l.ElementType != QueryElementType.SqlParameter)
						{
							expression = rOriginal;
						}
					}
					else if (IsBooleanConstant(right, out value))
					{
						if (r.ElementType != QueryElementType.SqlParameter)
						{
							expression = lOriginal;
						}
					}

					if (value != null
						&& expression != null
						&& !(expression.ElementType == QueryElementType.SqlValue && ((SqlValue)expression).Value == null))
					{
						var isNot = !value.Value;
						var withNull = false;
						if (op == SqlPredicate.Operator.NotEqual)
						{
							isNot = !isNot;
							withNull = true;
						}

						using (UsingColumnDescriptor(QueryHelper.GetColumnDescriptor(expression)))
						{
							var trueValue  = ((SqlPlaceholderExpression)Visit(ExpressionInstances.True)).Sql;
							var falseValue = ((SqlPlaceholderExpression)Visit(ExpressionInstances.False)).Sql;

							if (trueValue.ElementType == QueryElementType.SqlValue &&
								falseValue.ElementType == QueryElementType.SqlValue)
							{
								if (expression is SqlParameterizedExpressionBase { IsPredicate: true } predicateExpr)
								{
									predicate = new SqlPredicate.Expr(predicateExpr);
									if (isNot)
										predicate = new SqlPredicate.Not(predicate);
								}
								else
								{
									var withNullValue = compareNullsAsValues
										? withNull
										: (bool?)null;
									predicate = new SqlPredicate.IsTrue(expression, trueValue, falseValue, withNullValue, isNot);
								}
							}
						}
					}
				}

				if (predicate == null)
				{
					predicate = new SqlPredicate.ExprExpr(lOriginal, op, rOriginal,
						compareNullsAsValues && (lOriginal.CanBeNullable(nullability) || rOriginal.CanBeNullable(nullability))
							? op == SqlPredicate.Operator.Equal
							: null);
				}
			}

			return CreatePlaceholder(new SqlSearchCondition(false, canBeUnknown: null, predicate), GetOriginalExpression());
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

		public static List<SqlPlaceholderExpression> CollectPlaceholdersStraight(Expression expression)
		{
			var result = new List<SqlPlaceholderExpression>();

			Collect(expression);

			return result;

			void Collect(Expression expr)
			{
				if (expr is SqlPlaceholderExpression placeholder)
				{
					result.Add(placeholder);
				}
				else if (expr is SqlGenericConstructorExpression generic)
				{
					foreach (var assignment in generic.Assignments)
					{
						Collect(assignment.Expression);
					}

					foreach (var parameter in generic.Parameters)
					{
						Collect(parameter.Expression);
					}
				}
				else if (expr is MemberInitExpression memberInit)
				{
					foreach (var binding in memberInit.Bindings)
					{
						if (binding is MemberAssignment assignment)
						{
							Collect(assignment.Expression);
						}
					}
				}
				else if (expr is NewExpression newExpr)
				{
					foreach (var argument in newExpr.Arguments)
					{
						Collect(argument);
					}
				}
			}
		}

		public static List<SqlPlaceholderExpression> CollectPlaceholdersStraightWithPath(Expression expression, Expression path, out Expression correctedExpression)
		{
			var replacement = new Dictionary<Expression, SqlPlaceholderExpression>();

			Collect(expression, path);

			correctedExpression = expression.Transform(replacement, (r, e) =>
			{
				if (r.TryGetValue(e, out var newExpr))
					return newExpr;
				return e;
			});

			return replacement.Values.ToList();

			void Collect(Expression expr, Expression localPath)
			{
				if (expr is SqlPlaceholderExpression placeholder)
				{
					if (!replacement.ContainsKey(placeholder))
					{
						replacement.Add(placeholder, placeholder.WithPath(localPath));
					}
				}
				else if (expr is SqlGenericConstructorExpression generic)
				{
					foreach (var assignment in generic.Assignments)
					{
						var currentPath = Expression.MakeMemberAccess(localPath, assignment.MemberInfo);
						Collect(assignment.Expression, currentPath);
					}

					foreach (var parameter in generic.Parameters)
					{
						var currentPath = new SqlGenericParamAccessExpression(generic, parameter.ParameterInfo);
						Collect(parameter.Expression, currentPath);
					}
				}
				else if (expr is MemberInitExpression memberInit)
				{
					foreach (var binding in memberInit.Bindings)
					{
						if (binding is MemberAssignment assignment)
						{
							var currentPath = Expression.MakeMemberAccess(localPath, binding.Member);
							Collect(assignment.Expression, currentPath);
						}
					}
				}
				else if (expr is NewExpression newExpr)
				{
					if (newExpr.Members != null)
					{
						for (var i = 0; i < newExpr.Arguments.Count; i++)
						{
							var currentPath = Expression.MakeMemberAccess(localPath, newExpr.Members[i]);
							Collect(newExpr.Arguments[i], currentPath);
						}
					}
				}
				else if (expr is SqlDefaultIfEmptyExpression defaultIfEmpty)
					Collect(defaultIfEmpty.InnerExpression, localPath);
			}
		}

		public bool CollectNullCompareExpressions(Expression expression, List<Expression> result, ref List<Expression>? testExpressions)
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
				testExpressions ??= [];
				testExpressions.Add(Expression.Constant(false));

				return true;
			}

			if (expression is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
			{
				result.AddRange(defaultIfEmptyExpression.NotNullExpressions);
				return true;
			}

			if (expression is ConditionalExpression conditionalExpression)
			{
				var trueResult = new List<Expression>();

				if (conditionalExpression.IfTrue is SqlGenericConstructorExpression)
				{
					testExpressions ??= [];

					if (conditionalExpression.IfFalse is SqlGenericConstructorExpression)
					{
						testExpressions.Add(ExpressionInstances.False);
						return true;
					}

					testExpressions.Add(Expression.Not(conditionalExpression.Test));

					if (IsNullExpression(conditionalExpression.IfFalse))
					{
						return true;
					}
				}

				if (conditionalExpression.IfFalse is SqlGenericConstructorExpression)
				{
					testExpressions ??= [];

					testExpressions.Add(conditionalExpression.Test);

					if (IsNullExpression(conditionalExpression.IfTrue))
					{
						return true;
					}
				}

				List<Expression>? ifTrueTestExpression = null;

				if (conditionalExpression.IfTrue is not SqlGenericConstructorExpression)
				{
					if (!CollectNullCompareExpressions(conditionalExpression.IfTrue, trueResult, ref ifTrueTestExpression))
						return false;
				}

				List<Expression>? ifFalseTestExpression = null;

				var falseResult = new List<Expression>();

				if (conditionalExpression.IfFalse is not SqlGenericConstructorExpression)
				{
					if (!CollectNullCompareExpressions(conditionalExpression.IfFalse, falseResult, ref ifFalseTestExpression))
						return false;
				}

				foreach (var expr in trueResult)
				{
					result.Add(Expression.Condition(conditionalExpression.Test, expr, new DefaultValueExpression(MappingSchema, expr.Type)));
				}

				foreach (var expr in falseResult)
				{
					result.Add(Expression.Condition(Expression.Not(conditionalExpression.Test), expr, new DefaultValueExpression(MappingSchema, expr.Type)));
				}

				if (ifTrueTestExpression != null)
				{
					foreach (var te in ifTrueTestExpression)
					{
						testExpressions ??= [];
						testExpressions.Add(Expression.AndAlso(conditionalExpression.Test, te));
					}
				}

				if (ifFalseTestExpression != null)
				{
					foreach (var te in ifFalseTestExpression)
					{
						testExpressions ??= [];
						testExpressions.Add(Expression.AndAlso(Expression.Not(conditionalExpression.Test), te));
					}
				}

				return true;
			}

			if (expression is SqlEagerLoadExpression)
				return true;

			return false;
		}

		private static bool IsBooleanConstant(Expression expr, out bool? value)
		{
			value = null;
			if (expr.Type == typeof(bool) || expr.Type == typeof(bool?))
			{
				expr = expr.Unwrap();
				if (expr is ConstantExpression c)
				{
					value = c.Value as bool?;
					return true;
				}
				else if (expr is DefaultExpression)
				{
					value = expr.Type == typeof(bool) ? false : null;
					return true;
				}
				else if (expr is SqlPlaceholderExpression palacehoder)
				{
					if (palacehoder.Sql is SqlValue sqlValue)
					{
						value = sqlValue.Value as bool?;
						return true;
					}

					return false;
				}
			}

			return false;
		}

		// restores original types, lost due to C# compiler optimizations
		// e.g. see https://github.com/linq2db/linq2db/issues/2041
		private static bool RestoreCompare(ref Expression op1, ref Expression op2)
		{
			if (op1.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				var op1conv = (UnaryExpression)op1;

				// handle char replaced with int
				// (int)chr op CONST
				if (op1.Type == typeof(int) && op1conv.Operand.Type == typeof(char)
					&& (op2.NodeType is ExpressionType.Constant or ExpressionType.Convert or ExpressionType.ConvertChecked))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)op2).Operand;
					return true;
				}
				// (int?)chr? op CONST
				else if (op1.Type == typeof(int?) && op1conv.Operand.Type == typeof(char?)
					&& (op2.NodeType == ExpressionType.Constant
						|| (op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)op2).Operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)))
				{
					op1 = op1conv.Operand;
					op2 = op2.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)op2).Value))
						: ((UnaryExpression)((UnaryExpression)op2).Operand).Operand;
					return true;
				}
				// handle enum replaced with integer
				// here byte/short values replaced with int, int+ values replaced with actual underlying type
				// (int)enum op const
				else if (op1conv.Operand.Type.IsEnum
					&& op2.NodeType == ExpressionType.Constant
						&& (op2.Type == Enum.GetUnderlyingType(op1conv.Operand.Type) || op2.Type == typeof(int)))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(Enum.ToObject(op1conv.Operand.Type, ((ConstantExpression)op2).Value!), op1conv.Operand.Type);
					return true;
				}
				// here underlying type used
				// (int?)enum? op (int?)enum
				else if (op1conv.Operand.Type.IsNullableType && Nullable.GetUnderlyingType(op1conv.Operand.Type)!.IsEnum
					&& op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
					&& op2 is UnaryExpression op2conv2
					&& op2conv2.Operand.NodeType == ExpressionType.Constant
					&& op2conv2.Operand.Type == Nullable.GetUnderlyingType(op1conv.Operand.Type))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Convert(op2conv2.Operand, op1conv.Operand.Type);
					return true;
				}
				// https://github.com/linq2db/linq2db/issues/2039
				// byte, sbyte and ushort comparison operands upcasted to int
				else if (op2.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
					&& op2 is UnaryExpression op2conv1)
				{
					if (op1conv.Operand.Type == op2conv1.Operand.Type && op1conv.Operand.Type != typeof(object))
					{
						op1 = op1conv.Operand;
						op2 = op2conv1.Operand;
						return true;
					}
					else if (op1conv.Operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
					{
						// double conversion (:> int :> int?)
						var op1convNested = (UnaryExpression)op1conv.Operand;
						if (op1convNested.Operand.Type == op2conv1.Operand.Type && op1convNested.Operand.Type != typeof(object))
						{
							op1 = op1convNested.Operand;
							op2 = op2conv1.Operand;
							return true;
						}
						else if (op1convNested.Operand.Type == op2conv1.Operand.Type.UnwrapNullableType() && op1convNested.Operand.Type != typeof(object))
						{
							op1 = Expression.Convert(op1convNested.Operand, op2conv1.Operand.Type);
							op2 = op2conv1.Operand;
							return true;
						}
					}
				}

				// https://github.com/linq2db/linq2db/issues/2166
				// generates expression:
				// Convert(member, int) == const(value, int)
				// we must replace it with:
				// member == const(value, member_type)
				if (op2 is ConstantExpression const2
					&& const2.Type == typeof(int)
					&& ConvertUtils.TryConvert(const2.Value, op1conv.Operand.Type, out var convertedValue))
				{
					op1 = op1conv.Operand;
					op2 = Expression.Constant(convertedValue, op1conv.Operand.Type);
					return true;
				}
			}

			return false;
		}

		#endregion

		#region ConvertEnumConversion

		ISqlPredicate? ConvertEnumConversion(Expression left, SqlPredicate.Operator op, Expression right)
		{
			Expression value;
			Expression operand;

			if (left is MemberExpression)
			{
				operand = left;
				value = right;
			}
			else if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)left).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)left).Operand;
				value = right;
			}
			else if (right is MemberExpression)
			{
				operand = right;
				value = left;
			}
			else if (right.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)right).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)right).Operand;
				value = left;
			}
			else if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				operand = ((UnaryExpression)left).Operand;
				value = right;
			}
			else
			{
				operand = ((UnaryExpression)right).Operand;
				value = left;
			}

			var type = operand.Type;

			if (!type.UnwrapNullableType().IsEnum)
				return null;

			var dic = new Dictionary<object, object?>();

			var mapValues = MappingSchema.GetMapValues(type);

			if (mapValues != null)
				foreach (var mv in mapValues)
					if (!dic.ContainsKey(mv.OrigValue))
						dic.Add(mv.OrigValue, mv.MapValues[0].Value);

			switch (value.NodeType)
			{
				case ExpressionType.Constant:
				{
					var name = Enum.GetName(type, ((ConstantExpression)value).Value!);

					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					// ReSharper disable HeuristicUnreachableCode
					if (name == null)
						return null;
					// ReSharper restore HeuristicUnreachableCode
					// ReSharper restore ConditionIsAlwaysTrueOrFalse

					var origValue = Enum.Parse(type, name, false);

					if (!dic.TryGetValue(origValue, out var mapValue))
						mapValue = origValue;

					SqlValue sqlvalue;
					var ce = MappingSchema.GetConverter(new DbDataType(type), new DbDataType(typeof(DataParameter)), false, ConversionType.Common);

					if (ce != null)
					{
						sqlvalue = new SqlValue(ce.ConvertValueToParameter(origValue).Value!);
					}
					else
					{
						// TODO: pass column type to type mapValue=null cases?
						sqlvalue = MappingSchema.GetSqlValue(type, mapValue, null);
					}

					ISqlExpression? l, r;

					if (left.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
					{
						l = (Visit(operand) as SqlPlaceholderExpression)?.Sql;
						r = sqlvalue;
					}
					else
					{
						r = (Visit(operand) as SqlPlaceholderExpression)?.Sql;
						l = sqlvalue;
					}

					if (r == null || l == null)
						return null;

					return new SqlPredicate.ExprExpr(l, op, r, true);
				}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					value = ((UnaryExpression)value).Operand;

					SqlPlaceholderExpression? leftPlaceholder;
					SqlPlaceholderExpression? rightPlaceholder;

					using (UsingColumnDescriptor(SuggestColumnDescriptor(operand, value)))
					{
						leftPlaceholder = Visit(operand) as SqlPlaceholderExpression;
						rightPlaceholder = Visit(value) as SqlPlaceholderExpression;
					}

					if (leftPlaceholder == null || rightPlaceholder == null)
						return null;

					return new SqlPredicate.ExprExpr(leftPlaceholder.Sql, op, rightPlaceholder.Sql, true);
				}
			}

			return null;
		}

		#endregion

		#region Parameters

		private sealed class GetDataTypeContext
		{
			public GetDataTypeContext(DbDataType baseType, MappingSchema mappingSchema)
			{
				DataType = baseType.DataType;
				DbType = baseType.DbType;
				Length = baseType.Length;
				Precision = baseType.Precision;
				Scale = baseType.Scale;

				MappingSchema = mappingSchema;
			}

			public DataType      DataType;
			public string?       DbType;
			public int?          Length;
			public int?          Precision;
			public int?          Scale;

			public MappingSchema MappingSchema { get; }
		}

		static DbDataType GetDataType(ISqlExpression expr, DbDataType baseType, MappingSchema mappingSchema)
		{
			var ctx = new GetDataTypeContext(baseType, mappingSchema);

			expr.Find(ctx, static (context, e) =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField:
					{
						var fld = (SqlField)e;
						context.DataType = fld.Type.DataType;
						context.DbType = fld.Type.DbType;
						context.Length = fld.Type.Length;
						context.Precision = fld.Type.Precision;
						context.Scale = fld.Type.Scale;
						return true;
					}
					case QueryElementType.SqlParameter:
					{
						var type             = ((SqlParameter)e).Type;
						context.DataType = type.DataType;
						context.DbType = type.DbType;
						context.Length = type.Length;
						context.Precision = type.Precision;
						context.Scale = type.Scale;
						return true;
					}
					case QueryElementType.SqlDataType:
					{
						var type             = ((SqlDataType)e).Type;
						context.DataType = type.DataType;
						context.DbType = type.DbType;
						context.Length = type.Length;
						context.Precision = type.Precision;
						context.Scale = type.Scale;
						return true;
					}
					case QueryElementType.SqlValue:
					{
						var valueType        = ((SqlValue)e).ValueType;
						context.DataType = valueType.DataType;
						context.DbType = valueType.DbType;
						context.Length = valueType.Length;
						context.Precision = valueType.Precision;
						context.Scale = valueType.Scale;
						return true;
					}
					default:
					{
						if (e is ISqlExpression expr)
						{
							var type = QueryHelper.GetDbDataType(expr, context.MappingSchema);
							context.DataType = type.DataType;
							context.DbType = type.DbType;
							context.Length = type.Length;
							context.Precision = type.Precision;
							context.Scale = type.Scale;
							return true;
						}

						return false;
					}
				}
			});

			return new DbDataType(
				baseType.SystemType,
				ctx.DataType == DataType.Undefined ? baseType.DataType : ctx.DataType,
				string.IsNullOrEmpty(ctx.DbType) ? baseType.DbType : ctx.DbType,
				ctx.Length ?? baseType.Length,
				ctx.Precision ?? baseType.Precision,
				ctx.Scale ?? baseType.Scale
			);
		}

		#endregion

		#region ConvertInPredicate

		void BuildObjectGetters(SqlGenericConstructorExpression generic, ParameterExpression rootParam, Expression root, List<SqlGetValue> getters)
		{
			for (int i = 0; i < generic.Assignments.Count; i++)
			{
				var assignment = generic.Assignments[i];

				if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
				{
					BuildObjectGetters(subGeneric, rootParam, Expression.MakeMemberAccess(root, assignment.MemberInfo), getters);
				}
				else if (assignment.Expression is SqlPlaceholderExpression placeholder)
				{
					var access = Expression.MakeMemberAccess(root, assignment.MemberInfo);
					var body   = Expression.Convert(access, typeof(object));

					var lambda = Expression.Lambda<Func<object, object>>(body, rootParam);

					getters.Add(new SqlGetValue(placeholder.Sql, placeholder.Type, null, lambda.CompileExpression()));
				}
			}
		}

		private ISqlPredicate? ConvertInPredicate(Expression value, Expression arr)
		{
			ISqlExpression? expr = null;

			Expression builtExpr;

			using (UsingBuildFlags(_buildFlags | BuildFlags.ForKeys))
			{
				builtExpr = Visit(value);
			}

			if (builtExpr is SqlPlaceholderExpression placeholder)
			{
				expr = placeholder.Sql;
			}
			else if (SequenceHelper.UnwrapDefaultIfEmpty(builtExpr) is SqlGenericConstructorExpression constructor)
			{
				var objParam = Expression.Parameter(typeof(object));

				var getters = new List<SqlGetValue>();
				BuildObjectGetters(constructor, objParam, Expression.Convert(objParam, constructor.ObjectType),
					getters);

				expr = new SqlObjectExpression(MappingSchema, getters.ToArray());
			}

			if (expr == null)
				return null;

			using (UsingColumnDescriptor(QueryHelper.GetColumnDescriptor(expr)))
			{
				switch (arr.NodeType)
				{
					case ExpressionType.NewArrayInit:
					{
						var newArr = (NewArrayExpression)arr;

						if (newArr.Expressions.Count == 0)
							return SqlPredicate.False;

						var exprs  = new ISqlExpression[newArr.Expressions.Count];

						for (var i = 0; i < newArr.Expressions.Count; i++)
						{
							if (Visit(newArr.Expressions[i]) is not SqlPlaceholderExpression exprPlaceholder)
								return null;

							exprs[i] = exprPlaceholder.Sql;
						}

						return new SqlPredicate.InList(expr, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null, false, exprs);
					}

					default:

						if (Builder.CanBeEvaluatedOnClient(arr))
						{
							var parameter = Builder.ParametersContext.BuildParameter(BuildContext, arr, CurrentDescriptor, buildParameterType : ParametersContext.BuildParameterType.InPredicate);

							if (parameter != null)
							{
								parameter.IsQueryParameter = false;
								return new SqlPredicate.InList(expr, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null, false, parameter);
							}
						}

						break;
				}
			}

			return null;
		}

		#endregion

		#region LIKE predicate

		ISqlPredicate? CreateStringPredicate(MethodCallExpression expression, SqlPredicate.SearchString.SearchKind kind, ISqlExpression? caseSensitive)
		{
			var e = expression;

			if (e.Object == null || caseSensitive == null)
				return null;

			var descriptor = SuggestColumnDescriptor(e.Object, e.Arguments[0]);

			SqlPlaceholderExpression? objExpr;
			SqlPlaceholderExpression? argExpr;

			using (UsingColumnDescriptor(descriptor))
			{
				objExpr = Visit(e.Object) as SqlPlaceholderExpression;
				argExpr = Visit(e.Arguments[0]) as SqlPlaceholderExpression;
			}

			if (objExpr == null || argExpr == null)
				return null;

			return new SqlPredicate.SearchString(objExpr.Sql, false, argExpr.Sql, kind, caseSensitive);
		}

		#endregion

		#region MakeIsPredicate

		Expression MakeIsPredicateExpression(TableBuilder.TableContext tableContext, TypeBinaryExpression expression)
		{
			var typeOperand = expression.TypeOperand;

			if (typeOperand == tableContext.ObjectType)
			{
				var all = true;
				foreach (var m in tableContext.InheritanceMapping)
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

			var mapping = new List<(InheritanceMapping m, int i)>(tableContext.InheritanceMapping.Count);

			for (var i = 0; i < tableContext.InheritanceMapping.Count; i++)
			{
				var m = tableContext.InheritanceMapping[i];
				if (typeOperand.IsAssignableFrom(m.Type) && !m.IsDefault)
					mapping.Add((m, i));
			}

			var isEqual = true;

			if (mapping.Count == 0)
			{
				for (var i = 0; i < tableContext.InheritanceMapping.Count; i++)
				{
					var m = tableContext.InheritanceMapping[i];
					if (!m.IsDefault)
						mapping.Add((m, i));
				}

				isEqual = false;
			}

			Expression? expr = null;

			foreach (var m in mapping)
			{
				var field = tableContext.SqlTable.FindFieldByMemberName(tableContext.InheritanceMapping[m.i].DiscriminatorName) ?? throw new LinqToDBException($"Field {tableContext.InheritanceMapping[m.i].DiscriminatorName} not found in table {tableContext.SqlTable}");
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

				expr = expr == null ? e :
					isEqual
						? Expression.OrElse(expr, e)
						: Expression.AndAlso(expr, e);
			}

			return expr!;
		}

		#endregion

		#region BuildExpression

		public Expression CorrectRoot(Expression expr)
		{
			if (expr is MethodCallExpression mc && mc.IsQueryable())
			{
				var firstArg = CorrectRoot(mc.Arguments[0]);
				if (!ReferenceEquals(firstArg, mc.Arguments[0]))
				{
					var args = mc.Arguments.ToArray();
					args[0] = firstArg;
					return mc.Update(null, args);
				}
			}
			else if (expr is ContextRefExpression { BuildContext: DefaultIfEmptyBuilder.DefaultIfEmptyContext di })
			{
				return CorrectRoot(new ContextRefExpression(expr.Type, di.Sequence));
			}

			var newExpr = BuildExpression(expr, BuildPurpose.Traverse);
			if (!ExpressionEqualityComparer.Instance.Equals(newExpr, expr))
			{
				newExpr = CorrectRoot(newExpr);
			}

			return newExpr;
		}

		int _gettingSubquery;

		public IBuildContext? GetSubQuery(Expression expr, IBuildContext onContext, out bool isSequence, out string? errorMessage)
		{
			if (onContext is GroupByBuilder.GroupByContext groupBy)
			{
			}

			var info = new BuildInfo(onContext, expr, new SelectQuery())
			{
				CreateSubQuery = true,
				IsSubqueryExpression = true
			};

			if (_buildFlags.HasFlag(BuildFlags.ForceOuter))
			{
				info.SourceCardinality = SourceCardinality.ZeroOrMany;
			}

			using var snapshot = _gettingSubquery == 0 && Builder.ValidateSubqueries ? CreateSnapshot() : null;

			++_gettingSubquery;
			var buildResult = Builder.TryBuildSequence(info);
			--_gettingSubquery;

			if (expr is ContextRefExpression contextRef && ReferenceEquals(contextRef.BuildContext, buildResult.BuildContext))
			{
				errorMessage = null;
				isSequence = false;
				return null;
			}

			isSequence = buildResult.IsSequence;

			if (buildResult.BuildContext != null)
			{
				if (_gettingSubquery == 0)
				{
					++_gettingSubquery;
					var isSupported = Builder.IsSupportedSubquery(onContext, buildResult.BuildContext, out errorMessage);
					--_gettingSubquery;
					if (!isSupported)
					{
						buildResult.BuildContext.Detach();
						return buildResult.BuildContext;
					}
				}
			}

			snapshot?.Accept();

			errorMessage = buildResult.AdditionalDetails;
			return buildResult.BuildContext;
		}

		static string [] _singleElementMethods =
		{
			nameof(Enumerable.FirstOrDefault),
			nameof(Enumerable.First),
			nameof(Enumerable.Single),
			nameof(Enumerable.SingleOrDefault),
		};

		public Expression PrepareSubqueryExpression(Expression expr)
		{
			var newExpr = expr;

			if (expr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expr;
				if (mc.IsQueryable(_singleElementMethods))
				{
					if (mc.Arguments is [var a0, var a1])
					{
						Expression whereMethod;

						var typeArguments = mc.Method.GetGenericArguments();
						if (mc.Method.DeclaringType == typeof(Queryable))
						{
							var methodInfo = Methods.Queryable.Where.MakeGenericMethod(typeArguments);
							whereMethod = Expression.Call(methodInfo, a0, a1);
							var limitCall = Expression.Call(typeof(Queryable), mc.Method.Name, typeArguments, whereMethod);

							newExpr = limitCall;
						}
						else
						{
							var methodInfo = Methods.Enumerable.Where.MakeGenericMethod(typeArguments);
							whereMethod = Expression.Call(methodInfo, a0, a1);
							var limitCall = Expression.Call(typeof(Enumerable), mc.Method.Name, typeArguments, whereMethod);

							newExpr = limitCall;
						}
					}
				}
			}

			return newExpr;
		}

		#endregion

		sealed class TranslationContext : ITranslationContext
		{
			sealed class SqlExpressionFactory : ISqlExpressionFactory
			{
				readonly ITranslationContext _translationContext;

				public SqlExpressionFactory(ITranslationContext translationContext)
				{
					_translationContext = translationContext;
				}

				public DataOptions DataOptions => _translationContext.DataOptions;
				public DbDataType GetDbDataType(ISqlExpression expression) => _translationContext.GetDbDataType(expression);
				public DbDataType GetDbDataType(Type type) => _translationContext.MappingSchema.GetDbDataType(type);
			}

			public void Init(ExpressionBuildVisitor visitor, IBuildContext? currentContext, string? currentAlias)
			{
				Visitor        = visitor;
				CurrentContext = currentContext;
				CurrentAlias   = currentAlias;
			}

			public void Cleanup()
			{
				Visitor        = default!;
				CurrentContext = default!;
				CurrentAlias   = default!;
			}

			public TranslationContext()
			{
				ExpressionFactory = new SqlExpressionFactory(this);
			}

			public ISqlExpressionFactory ExpressionFactory { get; }

			public ExpressionBuildVisitor Visitor                 { get; private set; } = default!;
			public ExpressionBuilder      Builder                 => Visitor.Builder;
			public IBuildContext?         CurrentContext          { get; private set; }
			public ColumnDescriptor?      CurrentColumnDescriptor => Visitor.CurrentDescriptor;
			public string?                CurrentAlias            { get; private set; }

			static BuildPurpose GetBuildPurpose(TranslationFlags translationFlags)
			{
				var result = BuildPurpose.None;

				if (translationFlags.HasFlag(TranslationFlags.Expression))
				{
					result = BuildPurpose.Expression;
				}

				if (translationFlags.HasFlag(TranslationFlags.Sql))
				{
					result = BuildPurpose.Sql;
				}

				if (translationFlags.HasFlag(TranslationFlags.Expand))
				{
					result = BuildPurpose.Expand;
				}

				if (translationFlags.HasFlag(TranslationFlags.Traverse))
				{
					result = BuildPurpose.Traverse;
				}

				return result;
			}

			public Expression Translate(Expression expression, TranslationFlags translationFlags)
			{
				var buildPurpose = GetBuildPurpose(translationFlags);
				if (CurrentContext == null)
					throw new InvalidOperationException("CurrentContext not initialized");
				return Builder.BuildSqlExpression(CurrentContext, expression, buildPurpose, BuildFlags.None, alias: CurrentAlias);
			}

			public bool TranslateExpression(Expression expression, [NotNullWhen(true)] out ISqlExpression? sql, [NotNullWhen(false)] out SqlErrorExpression? error)
			{
				var translated = Translate(expression, TranslationFlags.Sql);
				if (translated is SqlPlaceholderExpression placeholder)
				{
					sql = placeholder.Sql;
					error = null;
					return true;
				}

				if (translated is SqlErrorExpression sqlError)
				{
					sql = null;
					error = sqlError;
					return false;
				}

				sql = null;
				error = CreateErrorExpression(expression);
				return false;
			}

			public MappingSchema MappingSchema => CurrentContext?.MappingSchema ?? throw new InvalidOperationException();
			public DataOptions DataOptions => Builder.DataOptions;

			public SelectQuery CurrentSelectQuery => CurrentContext?.SelectQuery ?? throw new InvalidOperationException();

			public SqlPlaceholderExpression CreatePlaceholder(SelectQuery selectQuery, ISqlExpression sqlExpression, Expression basedOn)
			{
				return new SqlPlaceholderExpression(selectQuery, sqlExpression, basedOn);
			}

			public SqlErrorExpression CreateErrorExpression(Expression basedOn, string? message = null, Type? type = null)
			{
				return new SqlErrorExpression(basedOn, message, type ?? basedOn.Type);
			}

			public Expression? GetAggregationContext(Expression expression)
			{
				var result = Builder.BuildTraverseExpression(expression);
				if (result is ContextRefExpression contextRef)
				{
					if (contextRef.BuildContext is AggregateExecuteBuilder.AggregateRootContext)
						return result;
					if (contextRef.BuildContext is GroupByBuilder.GroupByContext groupByContext)
						return SequenceHelper.CreateRef(groupByContext.SubQuery);
				}

				var elementType = TypeHelper.GetEnumerableElementType(expression.Type);
				var param       = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "source");
				var emptyLambda = Expression.Lambda(Expression.Constant(1, typeof(int)), param);
				var queryable   = expression;

				if (!typeof(IQueryable<>).IsSameOrParentOf(queryable.Type))
				{
					queryable = Expression.Call(typeof(Queryable), nameof(Queryable.AsQueryable), [elementType], queryable);
				}

				var executorCall = Expression.Call(typeof(LinqExtensions), nameof(LinqExtensions.AggregateExecute), [elementType, typeof(int)], queryable, emptyLambda);

				var buildResult = Builder.TryBuildSequence(new BuildInfo(CurrentContext, executorCall, CurrentSelectQuery) { CreateSubQuery = true, IsAggregation = true });
				if (buildResult.BuildContext is not null)
				{
					return SequenceHelper.CreateRef(buildResult.BuildContext).WithType(expression.Type);
				}

				return null;
			}

			public SelectQuery GetAggregationSelectQuery(Expression enumerableContext)
			{
				if (enumerableContext is ContextRefExpression contextRef)
				{
					if (contextRef.BuildContext is GroupByBuilder.GroupByContext groupByContext)
						return groupByContext.SubQuery.SelectQuery;
					return contextRef.BuildContext.SelectQuery;
				}

				throw new InvalidOperationException("Invalid enumerable context");
			}

			public Expression? BuildArrayAggregationFunction(
				int                                                       sequenceExpressionIndex,
				Expression                                                functionExpression,
				AllowedAggregationOperators                               allowedOperations,
				Func<IAggregationContext, BuildAggregationFunctionResult> functionFactory
				)
			{
				return Builder.BuildArrayAggregationFunction(sequenceExpressionIndex, functionExpression, allowedOperations, functionFactory);
			}

			public Expression? BuildAggregationFunction(
				int                                                       sequenceExpressionIndex,
				Expression                                                functionExpression,
				AllowedAggregationOperators                               allowedOperations,
				Func<IAggregationContext, BuildAggregationFunctionResult> functionFactory
				)
			{
				return Builder.BuildAggregationFunction(sequenceExpressionIndex, functionExpression, allowedOperations, functionFactory);
			}

			public bool CanBeEvaluatedOnClient(Expression expression)
			{
				return Builder.CanBeEvaluatedOnClient(expression);
			}

			public bool CanBeEvaluated(Expression expression)
			{
				return Builder.CanBeEvaluated(expression);
			}

			public object? Evaluate(Expression expression)
			{
				return Builder.Evaluate(expression);
			}

			public bool TryEvaluate(ISqlExpression expression, out object? result)
			{
				var context = new EvaluationContext();
				return expression.TryEvaluateExpression(context, out result);
			}

			public void MarkAsNonParameter(Expression expression, object? currentValue)
			{
				Builder.ParametersContext.MarkAsValue(expression, currentValue);
			}

			public IDisposable UsingColumnDescriptor(ColumnDescriptor? columnDescriptor)
			{
				return Visitor.UsingColumnDescriptor(columnDescriptor);
			}

			public IDisposable UsingCurrentAggregationContext(Expression basedOn)
			{
				if (basedOn is not ContextRefExpression contextRef)
					throw new InvalidOperationException("Invalid context reference");

				return new CurrentContextScope(this, contextRef.BuildContext);
			}

			sealed class CurrentContextScope : IDisposable
			{
				readonly IBuildContext?     _oldContext;
				readonly IDisposable        _disposable;
				readonly TranslationContext _translationContext;

				public CurrentContextScope(TranslationContext translationContext, IBuildContext newContext)
				{
					_translationContext               = translationContext;
					_oldContext                       = translationContext.CurrentContext;
					_disposable                       = translationContext.Visitor.UsingBuildContext(newContext);
					translationContext.CurrentContext = newContext;
				}

				public void Dispose()
				{
					_translationContext.CurrentContext = _oldContext;
					_disposable.Dispose();
				}
			}
		}

		static ObjectPool<TranslationContext> _translationContexts = new(() => new TranslationContext(), c => c.Cleanup(), 100);

		TranslationFlags GetTranslationFlags()
		{
			var result = TranslationFlags.None;

			if (_buildPurpose is BuildPurpose.Sql)
				result |= TranslationFlags.Sql;

			if (_buildPurpose is BuildPurpose.Expression)
			{
				if (_buildFlags.HasFlag(BuildFlags.ForSetProjection))
					result |= TranslationFlags.Sql;
				else
					result |= TranslationFlags.Expression;
			}

			if (result == TranslationFlags.None)
				result = TranslationFlags.Sql;

			return result;
		}

		public bool TranslateMember(IBuildContext? context, Expression memberExpression, [NotNullWhen(true)] out Expression? translated)
		{
			translated = null;

			if (memberExpression is MethodCallExpression || memberExpression is MemberExpression || memberExpression is NewExpression)
			{
				// Skip translation if there is a placeholder in the expression. It means that we already tried to translate, but it is failed.
				if (null != memberExpression.Find(e => e is SqlPlaceholderExpression))
				{
					translated = null;
					return false;
				}

				if (context?.SelectQuery != null)
				{
					if (GetAlreadyTranslated(context.SelectQuery, memberExpression, out translated))
						return true;
				}

				using var translationContext = _translationContexts.Allocate();

				translationContext.Value.Init(this, context, Alias);

				translated = Builder._memberTranslator.Translate(translationContext.Value, memberExpression, GetTranslationFlags());

				if (translated == null)
					return false;

				if (!IsSame(translated, memberExpression))
				{
					if (context?.SelectQuery != null)
						RegisterTranslatedSql(context.SelectQuery, translated, memberExpression);
					return true;
				}
			}

			return false;
		}

		public SqlPlaceholderExpression MakeColumn(SelectQuery? parentQuery, SqlPlaceholderExpression sqlPlaceholder, bool asNew = false)
		{
			if (parentQuery == sqlPlaceholder.SelectQuery)
				throw new InvalidOperationException();

			var placeholderType = sqlPlaceholder.Type;
			if (placeholderType.IsNullableType)
				placeholderType = placeholderType.UnwrapNullableType();

			if (sqlPlaceholder.SelectQuery == null)
				throw new InvalidOperationException($"Placeholder with path '{sqlPlaceholder.Path}' and SQL '{sqlPlaceholder.Sql}' has no SelectQuery defined.");

			var key = new ColumnCacheKey(sqlPlaceholder.Path, placeholderType, sqlPlaceholder.SelectQuery, parentQuery);

			if (!asNew && _columnCache.TryGetValue(key, out var placeholder))
			{
				return placeholder.WithType(sqlPlaceholder.Type);
			}

			var alias = sqlPlaceholder.Alias;

			if (string.IsNullOrEmpty(alias))
			{
				if (sqlPlaceholder.TrackingPath is MemberExpression tme)
					alias = tme.Member.Name;
				else if (sqlPlaceholder.Path is MemberExpression me)
					alias = me.Member.Name;
			}

			/*

			// Left here for simplifying debugging

			var findStr = "Ref(TableContext[ID:1](13)(T: 14)::ElementTest).Id";
			if (sqlPlaceholder.Path.ToString().Contains(findStr))
			{
				var found = _columnCache.Keys.FirstOrDefault(c => c.Expression?.ToString().Contains(findStr) == true);
				if (found.Expression != null)
				{
					if (_columnCache.TryGetValue(found, out var current))
					{
						var fh = ExpressionEqualityComparer.Instance.GetHashCode(found.Expression);
						var kh = ExpressionEqualityComparer.Instance.GetHashCode(key.Expression);

						var foundHash = ColumnCacheKey.ColumnCacheKeyComparer.GetHashCode(found);
						var KeyHash   = ColumnCacheKey.ColumnCacheKeyComparer.GetHashCode(key);
					}
				}
			}

			*/

			var sql    = sqlPlaceholder.Sql;
			var idx    = sqlPlaceholder.SelectQuery.Select.AddNew(sql);
			var column = sqlPlaceholder.SelectQuery.Select.Columns[idx];

			if (!string.IsNullOrEmpty(alias))
			{
				column.RawAlias = alias;
			}

			placeholder = ExpressionBuilder.CreatePlaceholder(parentQuery, column, sqlPlaceholder.Path, sqlPlaceholder.ConvertType, alias, idx, trackingPath: sqlPlaceholder.TrackingPath);

			if (!asNew)
				_columnCache.Add(key, placeholder);

			return placeholder;
		}

		NullabilityContext GetNullabilityContext()
		{
			_nullabilityContext ??= NullabilityContext.GetContext(BuildContext?.SelectQuery);
			return _nullabilityContext;
		}

		[DebuggerDisplay("S: {SelectQuery?.SourceID}, E: {Expression}")]
		readonly struct ColumnCacheKey
		{
			public ColumnCacheKey(Expression? expression, Type resultType, SelectQuery selectQuery, SelectQuery? parentQuery)
			{
				Expression  = expression;
				ResultType  = resultType;
				SelectQuery = selectQuery;
				ParentQuery = parentQuery;
			}

			public Expression?  Expression  { get; }
			public Type         ResultType  { get; }
			public SelectQuery  SelectQuery { get; }
			public SelectQuery? ParentQuery { get; }

			private sealed class ColumnCacheKeyEqualityComparer : IEqualityComparer<ColumnCacheKey>
			{
				public bool Equals(ColumnCacheKey x, ColumnCacheKey y)
				{
					return x.ResultType == y.ResultType                                           &&
						   ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression) &&
						   ReferenceEquals(x.SelectQuery, y.SelectQuery)                          &&
						   ReferenceEquals(x.ParentQuery, y.ParentQuery);
				}

				public int GetHashCode(ColumnCacheKey obj)
				{
					return HashCode.Combine(
						obj.ResultType,
						ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression),
						obj.SelectQuery,
						obj.ParentQuery
					);
				}
			}

			public static IEqualityComparer<ColumnCacheKey> ColumnCacheKeyComparer { get; } = new ColumnCacheKeyEqualityComparer();
		}

		[DebuggerDisplay("S: {SelectQuery?.SourceID} F: {Flags}, E: {Expression}, C: {Context}, CD: {ColumnDescriptor}")]
		readonly struct ExprCacheKey
		{
			public ExprCacheKey(Expression expression, IBuildContext? context, ColumnDescriptor? columnDescriptor, SelectQuery? selectQuery, ProjectFlags flags)
			{
				Expression       = expression;
				Context          = context;
				ColumnDescriptor = columnDescriptor;
				SelectQuery      = selectQuery;
				Flags            = flags;
			}

			public Expression        Expression       { get; }
			public IBuildContext?    Context          { get; }
			public ColumnDescriptor? ColumnDescriptor { get; }
			public SelectQuery?      SelectQuery      { get; }
			public ProjectFlags      Flags            { get; }

			sealed class ExprCacheKeyEqualityComparer : IEqualityComparer<ExprCacheKey>
			{
				public bool Equals(ExprCacheKey x, ExprCacheKey y)
				{
					return ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression) &&
						   Equals(x.Context, y.Context)                                           &&
						   Equals(x.SelectQuery, y.SelectQuery)                                   &&
						   Equals(x.ColumnDescriptor, y.ColumnDescriptor)                         &&
						   x.Flags == y.Flags;
				}

				public int GetHashCode(ExprCacheKey obj)
				{
					return HashCode.Combine(
						ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression),
						obj.Context,
						obj.SelectQuery,
						obj.ColumnDescriptor,
						obj.Flags
					);
				}
			}

			public static IEqualityComparer<ExprCacheKey> SqlCacheKeyComparer { get; } = new ExprCacheKeyEqualityComparer();
		}
	}
}
