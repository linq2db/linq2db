using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common.Internal;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	class ProjectionVisitor : ExpressionVisitorBase
	{
		readonly IBuildContext _context;

		public ExpressionBuilder Builder => _context.Builder;

		public ProjectionVisitor(IBuildContext context)
		{
			_context = context;
		}

		Expression ParseGenericConstructor(Expression expression)
		{
			return Builder.ParseGenericConstructor(expression, ProjectFlags.ExtractProjection, null);
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			return Visit(ParseGenericConstructor(node));
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			var newNode = Builder.MakeExpression(_context, node, ProjectFlags.ExtractProjection);
			if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
				return Visit(newNode);
			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			var newNode = Builder.MakeExpression(_context, node, ProjectFlags.ExtractProjection);

			if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
				return Visit(newNode);

			var parsed = ParseGenericConstructor(node);

			if (!ReferenceEquals(parsed, node))
				return Visit(parsed);

			return base.VisitMethodCall(node);
		}

		internal override Expression VisitContextRefExpression(ContextRefExpression node)
		{
			var newNode = Builder.MakeExpression(_context, node, ProjectFlags.ExtractProjection);
			if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
				return Visit(newNode);
			return base.VisitContextRefExpression(node);
		}

		public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
		{
			var newNode = base.VisitSqlGenericConstructorExpression(node);
			return newNode;
		}
	}

	class LambdaResolveVisitor : ExpressionVisitorBase
	{
		readonly IBuildContext _context;
		bool _inLambda;

		public ExpressionBuilder Builder => _context.Builder;

		public LambdaResolveVisitor(IBuildContext context)
		{
			_context = context;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (_inLambda)
			{
				if (null != node.Find(1, (_, e) => e is ContextRefExpression))
				{
					var expr = Builder.BuildSqlExpression(_context, node, ProjectFlags.SQL,
						buildFlags : ExpressionBuilder.BuildFlags.ForceAssignments);

					if (expr is SqlPlaceholderExpression)
						return expr;
				}

				return node;
			}

			return base.VisitMember(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			var save = _inLambda;
			_inLambda = true;

			var newNode = base.VisitLambda(node);

			_inLambda = save;

			return newNode;
		}
	}

	partial class ExpressionBuilder
	{
		static ObjectPool<BuildVisitor> _buildVisitorPool = new(() => new BuildVisitor(), v => v.Cleanup(), 100);

		static ObjectPool<FinalizeExpressionVisitor> _finalizeVisitorPool = new(() => new FinalizeExpressionVisitor(), v => v.Cleanup(), 100);

		sealed class BuildVisitor : ExpressionVisitorBase
		{
			ProjectFlags      _flags;
			IBuildContext     _context = default!;
			BuildFlags        _buildFlags;
			bool              _forceSql;
			bool              _disableParseNew;
			string?           _alias;
			ColumnDescriptor? _columnDescriptor;
			bool              _disableClosureHandling;
			Expression?       _root;

			ExpressionBuilder Builder       => _context.Builder;
			MappingSchema     MappingSchema => _context.MappingSchema;

			[return:NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				if (node == null)
					return null;

				if (_buildFlags.HasFlag(BuildFlags.IgnoreRoot))
				{
					if (ReferenceEquals(_root, node))
						return base.Visit(node);
				}

				if (node is SqlPlaceholderExpression)
					return node;

				if (HandleParameterized(node, out var parameterized))
					return parameterized;

				if (node.NodeType is ExpressionType.Conditional
								  or ExpressionType.Call
								  or ExpressionType.New
								  or ExpressionType.MemberInit
								  or ExpressionType.MemberAccess
								  or ExpressionType.ListInit
								  or ExpressionType.Default
								  or ExpressionType.Convert
								  or ExpressionType.ConvertChecked
								  or ExpressionType.Constant
								  or ExpressionType.Parameter
								  or ExpressionType.Not
					|| node is SqlGenericConstructorExpression
							or SqlEagerLoadExpression
							or BinaryExpression
							or SqlErrorExpression)
				{
					return base.Visit(node);
				}

				var newNode = TranslateExpression(node);

				if (newNode is SqlErrorExpression errorExpression)
				{
					if (errorExpression.IsCritical)
						return errorExpression;
					return base.Visit(node);
				}

				if (newNode is SqlPlaceholderExpression)
					return newNode;

				if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
					return Visit(newNode);

				if ((node.NodeType == ExpressionType.MemberAccess || node.NodeType == ExpressionType.Conditional) && _flags.IsExtractProjection())
					return newNode;

				return base.Visit(newNode);
			}

			readonly struct NeedForceScope : IDisposable
			{
				readonly BuildVisitor _visitor;
				readonly bool         _saveValue;

				public NeedForceScope(BuildVisitor visitor, bool needForce)
				{
					_visitor          = visitor;
					_saveValue        = visitor._forceSql;
					visitor._forceSql = needForce;
				}

				public void Dispose()
				{
					_visitor._forceSql = _saveValue;
				}
			}

			NeedForceScope NeedForce(bool needForce)
			{
				return new NeedForceScope(this, needForce);
			}

			public Expression Build(IBuildContext context, Expression expression, ProjectFlags flags, BuildFlags buildFlags)
			{
				_flags      = flags;
				_context    = context;
				_buildFlags = buildFlags;
				_root       = expression;

				using var _      = NeedForce((buildFlags & BuildFlags.ForceAssignments) != 0);
				var       result = Visit(expression);
				return result;
			}

			public override void Cleanup()
			{
				_flags                  = default;
				_context                = default!;
				_buildFlags             = default;
				_root                   = default;
				_forceSql               = default;
				_disableParseNew        = default;
				_alias                  = default;
				_columnDescriptor       = default;
				_disableClosureHandling = default;

				base.Cleanup();
			}

			public override Expression VisitDefaultValueExpression(DefaultValueExpression node)
			{
				if (_flags.IsExpression())
					return node;

				return TranslateExpression(node);
			}

			Expression TranslateExpression(Expression expression, string? alias = null, bool useSql = false, bool doNotSuppressErrors = false)
			{
				var asSql = _flags.IsSql() || _forceSql || useSql;

				/*
				if (!asSql)
				{
					if (Builder.TryGetAlreadyTranslated(_context, expression, _columnDescriptor, out var alreadyTranslated))
					{
						return alreadyTranslated;
					}
				}*/

				var localFlags = _flags;
				if (asSql)
					localFlags = localFlags.SqlFlag();

				var translated = asSql
					? Builder.ConvertToSqlExpr(_context, expression, localFlags, columnDescriptor : _columnDescriptor, alias : alias)
					: Builder.MakeExpression(_context, expression, localFlags);

				// Handling GroupBy by group case. Maybe wrong decision, we can do such correction during Group building.
				//
				if (!expression.Type.IsValueType && !expression.Type.IsSameOrParentOf(translated.Type) && !translated.Type.IsSameOrParentOf(expression.Type))
					return expression;

				if (SequenceHelper.HasError(translated))
				{
					if (translated is SqlErrorExpression errorExpression && (errorExpression.IsCritical || doNotSuppressErrors))
						return translated;
					return expression;
				}

				if (asSql && translated is SqlPlaceholderExpression placeholder &&
				    QueryHelper.IsNullValue(placeholder.Sql))
				{
					if (!Builder.MappingSchema.IsScalarType(expression.Type))
					{
						return Expression.Default(expression.Type);
					}
				}

				if (!_flags.IsTest())
				{
					translated = Builder.UpdateNesting(_context, translated);
				}

				if (!ExpressionEqualityComparer.Instance.Equals(translated, expression))
				{
					var result = Visit(translated);
					return result;
				}

				return translated;
			}

			protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
			{
				var saveDescriptor = _columnDescriptor;
				var saveAlias      = _alias;

				if (node.Member.DeclaringType != null)
				{
					_columnDescriptor = MappingSchema.GetEntityDescriptor(node.Member.DeclaringType).FindColumnDescriptor(node.Member);
				}

				_alias = node.Member.Name;

				var newNode = base.VisitMemberAssignment(node);

				_alias            = saveAlias;
				_columnDescriptor = saveDescriptor;

				return newNode;
			}

			internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
			{
				var saveDescriptor = _columnDescriptor;
				var saveAlias      = _alias;

				if (assignment.MemberInfo.DeclaringType != null)
				{
					_columnDescriptor = MappingSchema.GetEntityDescriptor(assignment.MemberInfo.DeclaringType).FindColumnDescriptor(assignment.MemberInfo);
				}

				using var _ = NeedForce((_buildFlags & BuildFlags.ForceAssignments) != 0);

				_alias = assignment.MemberInfo.Name;

				var newNode = base.VisitSqlGenericAssignment(assignment);

				_alias            = saveAlias;
				_columnDescriptor = saveDescriptor;

				return newNode;

			}

			internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
			{
				var saveDescriptor = _columnDescriptor;
				var saveAlias      = _alias;

				if (parameter.MemberInfo?.DeclaringType != null)
				{
					_columnDescriptor = MappingSchema.GetEntityDescriptor(parameter.MemberInfo.DeclaringType).FindColumnDescriptor(parameter.MemberInfo);
				}

				_alias = parameter.MemberInfo?.Name ?? parameter.ParameterInfo.Name;

				var newNode = base.VisitSqlGenericParameter(parameter);

				_alias            = saveAlias;
				_columnDescriptor = saveDescriptor;

				return newNode;

			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node == ExpressionConstants.DataContextParam)
					return node;

				return TranslateExpression(node);
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				// Do nothing with SequenceExpression
				var save = _disableClosureHandling;

				_disableClosureHandling = true;
				var newNode = node.Update(node.SequenceExpression, Visit(node.Predicate));
				_disableClosureHandling = save;

				return newNode;
			}

			bool IsForcedToConvert(Expression expression)
			{
				return _forceSql;
			}

			protected override Expression VisitConstant(ConstantExpression node)
			{
				if (IsForcedToConvert(node))
					return TranslateExpression(node);

				return node;
			}

			protected override Expression VisitUnary(UnaryExpression node)
			{
				if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					if (_flags.IsExpression())
					{
						var operand = Visit(node.Operand);
						var newNode = node.Update(operand);

						if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
							return Visit(newNode);

						return node;
					}

					var translated = TranslateExpression(node);
					if (!SequenceHelper.HasError(translated) && !ExpressionEqualityComparer.Instance.Equals(translated, node))
						return Visit(translated);
				}
				else if (node.NodeType == ExpressionType.Not)
				{
					if (node.Operand.NodeType == ExpressionType.Equal)
					{
						var binary = (BinaryExpression)node.Operand;
						return Visit(Expression.NotEqual(binary.Left, binary.Right));
					}

					if (node.Operand.NodeType == ExpressionType.NotEqual)
					{
						var binary = (BinaryExpression)node.Operand;
						return Visit(Expression.Equal(binary.Left, binary.Right));
					}
				}

				if (IsForcedToConvert(node) || Builder.IsServerSideOnly(node, _flags.IsExpression()) || Builder.PreferServerSide(node, true))
				{
					var translated = TranslateExpression(node, useSql: true);
					if (!ExpressionEqualityComparer.Instance.Equals(translated, node))
						return Visit(translated);
				}

				return base.VisitUnary(node);
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				if (node.NodeType == ExpressionType.Assign)
				{
					return base.VisitBinary(node);
				}

				var doNotForce = !IsForcedToConvert(node) && _flags.IsExpression();
				if (doNotForce)
				{
					if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
					{
						var left  = Visit(node.Left)!;
						var right = Visit(node.Right)!;

						if (left is ConditionalExpression condLeft && Builder.IsNullConstant(node.Right))
						{
							if (Builder.IsNullConstant(condLeft.IfFalse))
							{
								var test = condLeft.Test;
								if (node.NodeType == ExpressionType.Equal)
									test = Expression.Not(test);
								return Visit(test);
							}
						}

						if (right is ConditionalExpression condRight && Builder.IsNullConstant(node.Left))
						{
							if (Builder.IsNullConstant(condRight.IfFalse))
							{
								var test = condRight.Test;
								if (node.NodeType == ExpressionType.Equal)
									test = Expression.Not(test);
								return Visit(test);
							}
						}

						if (left is SqlGenericConstructorExpression && Builder.IsNullConstant(node.Right))
						{
							return Visit(Expression.Constant(node.NodeType == ExpressionType.NotEqual));
						}

						if (right is SqlGenericConstructorExpression && Builder.IsNullConstant(node.Left))
						{
							return Visit(Expression.Constant(node.NodeType == ExpressionType.NotEqual));
						}

						if (left is ConditionalExpression || left is SqlGenericConstructorExpression &&
						    Builder.IsNullConstant(node.Right))
						{
							return TranslateExpression(node, useSql : true);
						}

						if (right is ConditionalExpression || right is SqlGenericConstructorExpression &&
						    Builder.IsNullConstant(node.Left))
						{
							return TranslateExpression(node, useSql : true);
						}

						if (Builder.IsNullConstant(node.Left))
						{
							return node.Update(node.Left, node.Conversion, right);
						}

						if (Builder.IsNullConstant(node.Right))
						{
							return node.Update(left, node.Conversion, node.Right);
						}

						node = node.Update(left, node.Conversion, right);

						// Maybe we can relax this condition
						if (node.Left is SqlPlaceholderExpression  && Builder.IsConstantOrNullValue(node.Right) ||
						    node.Right is SqlPlaceholderExpression && Builder.IsConstantOrNullValue(node.Left))
						{
							return node;
						}
					}
					else if (node.NodeType == ExpressionType.Coalesce)
					{
						var left  = Visit(node.Left)!;
						var right = Visit(node.Right)!;

						node = node.Update(left, node.Conversion, right);

						if (left is not SqlPlaceholderExpression || right is not SqlPlaceholderExpression)
						{
							return node;
						}
						else
						{
							//TODO: strange Oracle limitation
							if (right.Type == typeof(string))
								return node;
						}
					}
				}

				var newNode = TranslateExpression(node, useSql: true);

				if (SequenceHelper.HasError(newNode))
					return base.VisitBinary(node);

				if (ExpressionEqualityComparer.Instance.Equals(newNode, node))
				{
					if (!doNotForce)
					{
						var left  = Visit(node.Left)!;
						var right = Visit(node.Right)!;

						var updatedNode = node.Update(left, node.Conversion, right);

						if (!ExpressionEqualityComparer.Instance.Equals(updatedNode, node))
							return Visit(updatedNode);

					}

					return base.VisitBinary(node);
				}

				return Visit(newNode);
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				var translatedMemberExpression = Builder.TranslateMember(_context, _flags, _columnDescriptor, _alias, node);

				if (translatedMemberExpression != null)
					return Visit(translatedMemberExpression);

				var attr = node.Member.GetExpressionAttribute(MappingSchema);

				if (attr != null)
				{
					if (attr.ServerSideOnly || _flags.IsSql() || attr.PreferServerSide)
					{
						var converted = Builder.ConvertExtension(attr, _context, node, _flags.SqlFlag());
						if (!ReferenceEquals(converted, node))
							return Visit(converted);
					}
				}

				if (Builder.IsServerSideOnly(node, _flags.IsExpression()) || Builder.PreferServerSide(node, true))
				{
					var translatedForced = TranslateExpression(node, alias: node.Member.Name, useSql : true);

					if (!SequenceHelper.HasError(translatedForced) && !ExpressionEqualityComparer.Instance.Equals(translatedForced, node))
						return translatedForced;

					return base.VisitMember(node);
				}

				var useSql = IsForcedToConvert(node) || _flags.IsSql();

				var translated = TranslateExpression(node, alias: node.Member.Name, useSql : useSql);

				if (!ExpressionEqualityComparer.Instance.Equals(translated, node))
					return Visit(translated);

				if (_flags.IsExpression())
				{
					var expr = Visit(node.Expression!);
					if (!ExpressionEqualityComparer.Instance.Equals(expr, node.Expression))
					{
						return Expression.Condition(Expression.NotEqual(expr, Expression.Default(expr.Type)),
							node.Update(expr), Expression.Default(node.Type));
					}
				}
				else
				{
					return node;
				}

				return base.VisitMember(node);
			}

			Expression ParseGenericConstructor(Expression expression)
			{
				return Builder.ParseGenericConstructor(expression, _flags, _columnDescriptor);
			}

			protected override Expression VisitNew(NewExpression node)
			{
				if (!_disableParseNew && _flags.IsSql())
				{
					var newNode = ParseGenericConstructor(node);
					if (!ReferenceEquals(newNode, node))
						return Visit(newNode);
				}

				return base.VisitNew(node);
			}

			protected override Expression VisitMemberInit(MemberInitExpression node)
			{
				if (_flags.IsSql() && !Builder.CanBeEvaluated(node))
				{
					var parsedNode = ParseGenericConstructor(node);
					if (!ReferenceEquals(parsedNode, node))
						return Visit(parsedNode);
				}

				var save = _disableParseNew;
				_disableParseNew = true;

				var newNode = base.VisitMemberInit(node);

				_disableParseNew = save;

				return newNode;
			}

			protected override Expression VisitListInit(ListInitExpression node)
			{
				var saveDisable = _disableParseNew;
				_disableParseNew = true;

				using var _ = NeedForce(false);

				var newNode = base.VisitListInit(node);

				_disableParseNew = saveDisable;

				return newNode;
			}

			public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				using var _ = NeedForce((_buildFlags & BuildFlags.ForceAssignments) != 0);
				return base.VisitSqlGenericConstructorExpression(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (node.Method.DeclaringType == typeof(Sql))
				{
					if (node.Method.Name == nameof(Sql.Alias))
					{
						var saveAlias = _alias;

						_alias = node.Arguments[1].EvaluateExpression() as string;

						var aliasedNode = Visit(node.Arguments[0]);

						if (!string.IsNullOrEmpty(_alias) && aliasedNode is SqlPlaceholderExpression placeholder)
							aliasedNode = placeholder.WithAlias(_alias);

						_alias = saveAlias;

						return aliasedNode;
					}
				}

				var localFlags = _flags;
				if (IsForcedToConvert(node))
					localFlags = _flags.SqlFlag();

				if (Builder.IsServerSideOnly(node, _flags.IsExpression()) || Builder.PreferServerSide(node, false))
					localFlags = _flags.SqlFlag();

				var method = Builder.MakeExpression(_context, node, localFlags);

				if (method is SqlErrorExpression)
				{
					if (_flags.IsSql())
						return method;
					return base.VisitMethodCall(node);
				}

				if (!_flags.IsTest())
					method = Builder.UpdateNesting(_context, method);

				if (!ReferenceEquals(method, node))
					return Visit(method);

				var translatedMethodCall = Builder.TranslateMember(_context, localFlags, _columnDescriptor, _alias, node);

				if (translatedMethodCall != null)
					return Visit(translatedMethodCall);

				var attr = node.Method.GetExpressionAttribute(MappingSchema);

				if (attr != null)
				{
					// Handling AsNullable<T>, AsNotNull<T>, AsNotNullable<T>, ToNullable<T>, ToNotNull<T>, ToNotNullable<T>
					if (!_flags.IsSql() && !attr.ServerSideOnly && attr.Expression == "{0}" && node.Method.DeclaringType == typeof(Sql))
					{
						var converted = Visit(node.Arguments[0]);
						if (converted.Type != node.Type)
							converted = Expression.Convert(converted, node.Type);
						return converted;
					}

					if (attr.ServerSideOnly || _flags.IsSql() || attr.PreferServerSide || attr.IsWindowFunction || attr.IsAggregate)
					{
						var converted = Builder.ConvertExtension(attr, _context, node, _flags.SqlFlag());
						if (!ReferenceEquals(converted, node))
							return Visit(converted);
					}
				}

				var newNode = Builder.ConvertExpression(node);
				if (!ReferenceEquals(newNode, node))
				{
					if (!_flags.IsSql() && !Builder.PreferServerSide(node, false))
					{
						var translatedWithArguments = base.VisitMethodCall(node);
						if (Builder.CanBeCompiled(translatedWithArguments, true))
							return translatedWithArguments;
					}

					return Visit(newNode);
				}

				if ((_buildFlags & BuildFlags.ForceAssignments) != 0 && _flags.IsSql())
				{
					var parsed = ParseGenericConstructor(node);

					if (!ReferenceEquals(parsed, node))
						return Visit(parsed);
				}

				if (localFlags.IsSql())
				{
					var translated = TranslateExpression(method, useSql: true);
					if (!ReferenceEquals(translated, method))
						return translated;
				}

				var saveDescriptor = _columnDescriptor;
				_columnDescriptor = null;

				if (Builder.CanBeCompiled(node, true))
					return node;

				if (!_flags.IsSql())
					newNode = base.VisitMethodCall(node);

				_columnDescriptor = saveDescriptor;

				return newNode;
			}

			Expression ApplyAccessors(Expression expression)
			{
				var result = Builder.ParametersContext.ApplyAccessors(expression, true);
				return result;
			}

			bool HandleParameterized(Expression expr, [NotNullWhen(true)] out Expression? transformed)
			{
				if (expr is PlaceholderExpression { PlaceholderType: PlaceholderType.Closure })
				{
					transformed = expr;
					return true;
				}

				transformed = null;

				if (_disableClosureHandling)
				{
					return false;
				}

				// Do not select from database simple constants
				if (_flags.IsExpression() 
				    && expr is ConstantExpression constantExpression 
				    && (constantExpression.Value is null || constantExpression.Type.IsValueType))
				{
					return false;
				}

				var canBeCompiled = Builder.CanBeCompiled(expr, false);

				// some types has custom converter, we have to handle them
				//
				if (canBeCompiled && Builder.IsForceParameter(expr, _columnDescriptor))
				{
					transformed = TranslateExpression(expr, _alias, true);
					return !ExpressionEqualityComparer.Instance.Equals(expr, transformed);
				}

				// Shortcut: if expression can be compiled we can live it as is but inject accessors
				//
				if ((_flags.IsExpression()      &&
				     expr.NodeType != ExpressionType.New           &&
				     expr.NodeType != ExpressionType.Default       &&
				     expr is not DefaultValueExpression            &&
				     !(expr is ConstantExpression { Value: null }) &&
				     expr != ExpressionConstants.DataContextParam  &&
				     !Builder.CanBeConstant(expr)                  &&
				     canBeCompiled)
				   )
				{
					if (expr.NodeType == ExpressionType.MemberAccess || expr.NodeType == ExpressionType.Call)
					{
						transformed = Builder.MakeExpression(_context, expr, _flags);
						if (!ExpressionEqualityComparer.Instance.Equals(transformed, expr))
						{
							transformed = Visit(transformed);
							return true;
						}
					}

					// correct expression based on accessors

					var valueExpr = ApplyAccessors(expr);

					if (valueExpr.Type != expr.Type)
					{
						valueExpr = Expression.Convert(valueExpr.UnwrapConvert(), expr.Type);
					}

					transformed = new PlaceholderExpression(valueExpr, PlaceholderType.Closure);
					return true;
				}

				return false;
			}

			public override Expression VisitPlaceholderExpression(PlaceholderExpression node)
			{
				return node;
			}

			protected override Expression VisitConditional(ConditionalExpression node)
			{
				var saveFlags = _flags;

				_flags |= ProjectFlags.ForceOuterAssociation;
				try
				{
					var translated = TranslateExpression(node, useSql: true);

					if (translated is SqlPlaceholderExpression)
						return translated;

					if (IsForcedToConvert(node))
					{
						return TranslateExpression(node);
					}

					var saveDescriptor = _columnDescriptor;
					_columnDescriptor = null;
					var test           = Visit(node.Test);
					_columnDescriptor = saveDescriptor;

					var ifTrue  = Visit(node.IfTrue);
					var ifFalse = Visit(node.IfFalse);

					if (test is ConstantExpression { Value: bool boolValue })
					{
						return boolValue ? ifTrue : ifFalse;
					}

					if (ifTrue is SqlGenericConstructorExpression && ifFalse is SqlPlaceholderExpression)
						ifFalse = node.IfFalse;
					else if (ifFalse is SqlGenericConstructorExpression && ifTrue is SqlPlaceholderExpression)
						ifTrue = node.IfTrue;

					if (test is SqlPlaceholderExpression   &&
					    ifTrue is SqlPlaceholderExpression &&
					    ifFalse is SqlPlaceholderExpression)
					{
						return TranslateExpression(node, useSql: true);
					}

					return node.Update(test, ifTrue, ifFalse);
				}
				finally
				{
					_flags = saveFlags;
				}
			}

			public override Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
			{
				var innerExpression = Visit(node.InnerExpression);

				if (_flags.IsExpression() && _buildFlags.HasFlag(BuildFlags.ForceDefaultIfEmpty))
				{
					var testCondition = node.NotNullExpressions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);
					var defaultValue = new DefaultValueExpression(MappingSchema, innerExpression.Type);

					var condition = Expression.Condition(testCondition, innerExpression, defaultValue);

					return Visit(condition);
				}

				return node.Update(innerExpression, node.NotNullExpressions);
			}
		}

		[Flags]
		public enum BuildFlags
		{
			None = 0,
			ForceAssignments = 0x1,
			ForceDefaultIfEmpty = 0x2,
			IgnoreRoot = 0x4,
		}

		public Expression BuildSqlExpression(IBuildContext context, Expression expression, ProjectFlags flags, string? alias = null, BuildFlags buildFlags = BuildFlags.None)
		{
			using var visitor =  _buildVisitorPool.Allocate();

			var result = visitor.Value.Build(context, expression, flags, buildFlags);
			return result;
		}

		public Expression ExtractProjection(IBuildContext context, Expression expression)
		{
			var projectVisitor = new ProjectionVisitor(context);
			var projected      = projectVisitor.Visit(expression);

			return projected;
		}

		bool _handlingAlias;

		Expression CheckForAlias(IBuildContext context, MemberExpression memberExpression, EntityDescriptor entityDescriptor, string alias, ProjectFlags flags)
		{
			if (_handlingAlias)
				return memberExpression;

			var otherProp = entityDescriptor.TypeAccessor.GetMemberByName(alias);

			if (otherProp == null)
				return memberExpression;

			var newPath     = Expression.MakeMemberAccess(memberExpression.Expression, otherProp.MemberInfo);

			_handlingAlias = true;
			var aliasResult = MakeExpression(context, newPath, flags);
			_handlingAlias = false;

			if (aliasResult is not SqlErrorExpression && aliasResult is not DefaultValueExpression)
			{
				return aliasResult;
			}

			return memberExpression;
		}

		public bool HandleAlias(IBuildContext context, Expression expression, ProjectFlags flags, [NotNullWhen(true)] out Expression? result)
		{
			result = null;

			if (expression is not MemberExpression memberExpression)
				return false;

			var ed = MappingSchema.GetEntityDescriptor(memberExpression.Expression!.Type);

			if (ed.Aliases == null)
				return false;

			var testedColumn = ed.Columns.FirstOrDefault(c =>
				MemberInfoComparer.Instance.Equals(c.MemberInfo, memberExpression.Member));

			if (testedColumn != null)
			{
				var otherColumns = ed.Aliases.Where(a =>
					a.Value == testedColumn.MemberName);

				foreach (var other in otherColumns)
				{
					var newResult = CheckForAlias(context, memberExpression, ed, other.Key, flags);
					if (!ReferenceEquals(newResult, memberExpression))
					{
						result = newResult;
						return true;
					}
				}
			}
			else
			{
				if (ed.Aliases.TryGetValue(memberExpression.Member.Name, out var alias))
				{
					var newResult = CheckForAlias(context, memberExpression, ed, alias, flags);
					if (!ReferenceEquals(newResult, memberExpression))
					{
						result = newResult;
						return true;
					}
				}
			}

			return false;
		}
	}
}
