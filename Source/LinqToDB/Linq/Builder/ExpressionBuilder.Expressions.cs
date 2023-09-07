using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class ProjectionVisitor : ExpressionVisitorBase
	{
		readonly IBuildContext _context;

		public ExpressionBuilder Builder => _context.Builder;

		public ProjectionVisitor(IBuildContext context)
		{
			_context = context;
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			return Visit(SqlGenericConstructorExpression.Parse(node));
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

			var parsed = SqlGenericConstructorExpression.Parse(node);

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

		internal override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
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

		class BuildVisitor : ExpressionVisitorBase
		{
			ProjectFlags      _flags;
			IBuildContext     _context = default!;
			BuildFlags        _buildFlags;
			bool              _forceSql;
			bool              _disableParseNew;
			string?           _alias;
			ColumnDescriptor? _columnDescriptor;
			bool              _disableClosureHandling;
			
			ExpressionBuilder Builder       => _context.Builder;
			MappingSchema     MappingSchema => _context.MappingSchema;

			[return:NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				if (node == null)
					return null;

				if (node is SqlPlaceholderExpression)
					return node;

				if (HandleParametrized(node, out var parametrized))
					return parametrized;

				if (node.NodeType == ExpressionType.Conditional  || 
				    node.NodeType == ExpressionType.Call         || 
				    node.NodeType == ExpressionType.New          || 
				    node.NodeType == ExpressionType.MemberInit   || 
				    node.NodeType == ExpressionType.MemberAccess || 
				    node.NodeType == ExpressionType.ListInit     || 
				    node.NodeType == ExpressionType.Default      || 
				    node.NodeType == ExpressionType.Convert      ||
				    node.NodeType == ExpressionType.Constant     ||
				    node.NodeType == ExpressionType.Parameter    ||
				    node.NodeType == ExpressionType.Not          ||
				    node is SqlGenericConstructorExpression      ||
				    node is BinaryExpression)
				{
					return base.Visit(node);
				}

				var newNode = TranslateExpression(node);

				if (newNode is SqlErrorExpression)
					return base.Visit(node);

				if (newNode is SqlPlaceholderExpression)
					return newNode;

				if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
					return Visit(newNode);

				if ((node.NodeType == ExpressionType.MemberAccess || node.NodeType == ExpressionType.Conditional) && _flags.IsExtractProjection())
					return newNode;

				if (_flags.IsExtractProjection())
				{

				}

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

				using var _      = NeedForce((buildFlags & BuildFlags.ForceAssignments) != 0);
				var       result = Visit(expression);
				return result;
			}

			public override Expression VisitDefaultValueExpression(DefaultValueExpression node)
			{
				if (_flags.IsExpression())
					return node;

				return TranslateExpression(node);
			}

			protected Expression TranslateExpression(Expression expression, string? alias = null, bool useSql = false)
			{
				var asSql = _flags.IsSql() || _forceSql || useSql;

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

				if (translated is SqlErrorExpression)
				{
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
					return Visit(translated);

				return translated;
			}

			protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
			{
				var save = _columnDescriptor;

				if (node.Member.DeclaringType != null)
				{
					_columnDescriptor = MappingSchema.GetEntityDescriptor(node.Member.DeclaringType).FindColumnDescriptor(node.Member);
				}

				var newNode = base.VisitMemberAssignment(node);

				_columnDescriptor = save;

				return newNode;
			}

			internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
			{
				var save = _columnDescriptor;

				if (assignment.MemberInfo.DeclaringType != null)
				{
					_columnDescriptor = MappingSchema.GetEntityDescriptor(assignment.MemberInfo.DeclaringType).FindColumnDescriptor(assignment.MemberInfo);
				}

				var newNode = base.VisitSqlGenericAssignment(assignment);

				_columnDescriptor = save;

				return newNode;

			}

			internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
			{
				var save = _columnDescriptor;

				if (parameter.MemberInfo?.DeclaringType != null)
				{
					_columnDescriptor = MappingSchema.GetEntityDescriptor(parameter.MemberInfo.DeclaringType).FindColumnDescriptor(parameter.MemberInfo);
				}

				var newNode = base.VisitSqlGenericParameter(parameter);

				_columnDescriptor = save;

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
				if (_flags.IsExpression() && !IsForcedToConvert(node))
					return node;

				using var _ = NeedForce(true);
				return TranslateExpression(node);
			}

			protected override Expression VisitUnary(UnaryExpression node)
			{
				if (IsForcedToConvert(node))
					return TranslateExpression(node);

				if (node.NodeType == ExpressionType.Convert)
				{
					if (_flags.IsExpression())
					{
						var operand = Visit(node.Operand);
						var newNode = node.Update(operand);
						if (!ExpressionEqualityComparer.Instance.Equals(newNode, node))
							return Visit(newNode);
					}

					return TranslateExpression(node);
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

				return base.VisitUnary(node);
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
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
						
					} else if (node.NodeType == ExpressionType.Coalesce)
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
					else if (node.NodeType != ExpressionType.ArrayIndex && node.NodeType != ExpressionType.Assign)
					{
						var left  = Visit(node.Left)!;
						var right = Visit(node.Right)!;

						var updatedNode = node.Update(left, node.Conversion, right);
						if (!ExpressionEqualityComparer.Instance.Equals(updatedNode, node))
							return Visit(updatedNode);
					}
				}
				
				var newNode = TranslateExpression(node, useSql: true);

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
					return node;
				}

				return Visit(newNode);
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				if (Builder.IsServerSideOnly(node, _flags.IsExpression()) || Builder.PreferServerSide(node, true))
				{
					return TranslateExpression(node, useSql : true);
				}

				var handled = Builder.HandleExtension(_context, node, _flags);

				if (!ExpressionEqualityComparer.Instance.Equals(handled, node))
					return Visit(handled);

				var useSql = IsForcedToConvert(node);

				if (!useSql)
				{
					var expanded = Builder.ExposeExpression(node);

					if (!ReferenceEquals(expanded, node))
					{
						useSql = true;
					}
				}

				var translated = TranslateExpression(node, useSql : useSql);

				if (!ExpressionEqualityComparer.Instance.Equals(translated, node))
					return translated;

				if (_flags.IsExpression())
				{
					var expr = Visit(node.Expression);
					if (!ExpressionEqualityComparer.Instance.Equals(expr, node.Expression))
					{
						return Expression.Condition(Expression.NotEqual(expr, Expression.Default(expr.Type)),
							node.Update(expr), Expression.Default(node.Type));
					}
				}

				return base.VisitMember(node);
			}

			protected override Expression VisitNew(NewExpression node)
			{
				if (!_disableParseNew)
				{
					var newNode = SqlGenericConstructorExpression.Parse(node);
					if (!ReferenceEquals(newNode, node))
						return Visit(newNode);
				}

				using var _ = NeedForce(true);

				return base.VisitNew(node);
			}

			protected override Expression VisitMemberInit(MemberInitExpression node)
			{
				var newNode = SqlGenericConstructorExpression.Parse(node);
				if (!ReferenceEquals(newNode, node))
					return Visit(newNode);

				return base.VisitMemberInit(node);
			}

			protected override Expression VisitListInit(ListInitExpression node)
			{
				var saveDisable = _disableParseNew;
				_disableParseNew = true;

				using var _ = NeedForce(true);

				var newNode = base.VisitListInit(node);

				_disableParseNew = saveDisable;

				return newNode;
			}

			internal override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				using var _ = NeedForce((_buildFlags & BuildFlags.ForceAssignments) != 0);
				return base.VisitSqlGenericConstructorExpression(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				var localFlags = _flags;
				if (IsForcedToConvert(node))
					localFlags = _flags.SqlFlag();

				if (Builder.IsServerSideOnly(node, _flags.IsExpression()) || node.Method.IsSqlPropertyMethodEx())
					localFlags = _flags.SqlFlag();

				var method = Builder.MakeExpression(_context, node, localFlags);

				if (!_flags.IsTest())
					method = Builder.UpdateNesting(_context, method);

				if (!ReferenceEquals(method, node))
					return Visit(method);

				var newNode = Builder.HandleExtension(_context, node, localFlags);
				if (!ReferenceEquals(newNode, node))
					return Visit(newNode);

				var converted = Builder.ConvertSingleExpression(node, _flags.IsExpression());
				if (!ReferenceEquals(converted, node))
				{
					return TranslateExpression(converted, useSql : true);
				}

				if ((_buildFlags & BuildFlags.ForceAssignments) != 0)
				{
					var parsed = SqlGenericConstructorExpression.Parse(node);

					if (!ReferenceEquals(parsed, node))
						return Visit(parsed);
				}

				if (localFlags.IsSql())
				{
					var translated = TranslateExpression(method);
					if (!ReferenceEquals(translated, method))
						return translated;
				}

				return base.VisitMethodCall(node);
			}

			bool HandleParametrized(Expression expr, [NotNullWhen(true)] out Expression? transformed)
			{
				if (expr is ClosurePlaceholderExpression)
				{
					transformed = expr;
					return true;
				}

				transformed = null;

				if (_disableClosureHandling)
				{
					return false;
				}

				// Shortcut: if expression can be compiled we can live it as is but inject accessors 
				//
				if (_flags.IsExpression()                         &&
				    expr.NodeType != ExpressionType.New           &&
				    expr.NodeType != ExpressionType.Default       &&
				    expr is not DefaultValueExpression            &&
					!(expr is ConstantExpression { Value: null }) &&
					expr != ExpressionConstants.DataContextParam  &&
				    Builder.CanBeCompiled(expr, false))
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

					var valueAccessor = Builder.ParametersContext.ReplaceParameter(
						Builder.ParametersContext._expressionAccessors, expr, false, s => { });

					var valueExpr = valueAccessor.ValueExpression;

					if (valueExpr.Type != expr.Type)
					{
						valueExpr = Expression.Convert(valueExpr.UnwrapConvert(), expr.Type);
					}

					transformed = new ClosurePlaceholderExpression(valueExpr);
					return true;
				}

				return false;
			}

			public override Expression VisitClosurePlaceholderExpression(ClosurePlaceholderExpression node)
			{
				return node;
			}

			protected override Expression VisitConditional(ConditionalExpression node)
			{
				if (_flags.IsSql())
				{
					var translated = TranslateExpression(node);

					if (translated is SqlPlaceholderExpression)
						return translated;
				}


				if (IsForcedToConvert(node))
				{
					return TranslateExpression(node);
				}

				var test    = Visit(node.Test);

				var ifTrue  = Visit(node.IfTrue);
				var ifFalse = Visit(node.IfFalse);

				if (ifTrue is SqlGenericConstructorExpression && ifFalse is SqlPlaceholderExpression)
					ifFalse = node.IfFalse;
				else if (ifFalse is SqlGenericConstructorExpression && ifTrue is SqlPlaceholderExpression)
					ifTrue = node.IfTrue;

				if (test is SqlPlaceholderExpression   && 
				    ifTrue is SqlPlaceholderExpression &&
					ifFalse is SqlPlaceholderExpression)
				{
					return TranslateExpression(node, useSql : true);
				}
				
				return node.Update(test, ifTrue, ifFalse);
			}
		}


		[Flags]
		public enum BuildFlags
		{
			None = 0,
			ForceAssignments = 0x1,
			IgnoreNullComparison = 0x2
		}

		public Expression BuildSqlExpression(IBuildContext context, Expression expression, ProjectFlags flags, string? alias = null, BuildFlags buildFlags = BuildFlags.None)
		{
			using var visitor =  _buildVisitorPool.Allocate();

			var result = visitor.Value.Build(context, expression, flags, buildFlags);
			return result;
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

			var ed = MappingSchema.GetEntityDescriptor(memberExpression.Expression.Type);

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
