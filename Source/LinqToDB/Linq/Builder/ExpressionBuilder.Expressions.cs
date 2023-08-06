using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using static LinqToDB.Linq.Builder.ContextParser;

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

	partial class ExpressionBuilder
	{
		class BuildVisitor : ExpressionVisitorBase
		{
			ProjectFlags  _flags;
			IBuildContext _context = default!;
			BuildFlags    _buildFlags;
			bool          _forceSql;
			bool          _disableParseNew;
			string?       _alias;
			
			ExpressionBuilder Builder => _context.Builder;
			MappingSchema MappingSchema => Builder.MappingSchema;

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
					? Builder.ConvertToSqlExpr(_context, expression, localFlags, alias : alias)
					: Builder.MakeExpression(_context, expression, localFlags);

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

			[return:NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				if (node == null)
					return null;

				if (node is SqlPlaceholderExpression)
					return node;

				/*
				if (node is BinaryExpression binary && HandleBinary(binary, out var newBinary))
					return newBinary;
					*/

				if (HandleParametrized(node, out var parametrized))
					return parametrized;

				if (node.NodeType == ExpressionType.Conditional || 
				    node.NodeType == ExpressionType.Call        || 
				    node.NodeType == ExpressionType.New         || 
				    node.NodeType == ExpressionType.MemberInit  || 
				    node.NodeType == ExpressionType.ListInit    || 
				    node.NodeType == ExpressionType.Default     || 
				    node.NodeType == ExpressionType.Convert     ||
				    node.NodeType == ExpressionType.Constant    ||
				    node.NodeType == ExpressionType.Parameter   ||
				    node is SqlGenericConstructorExpression     ||
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



			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node == ExpressionConstants.DataContextParam)
					return node;

				return TranslateExpression(node);
			}

			protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
			{
				return base.VisitMemberAssignment(node);
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				// Do nothing with SequenceExpression
				return node.Update(node.SequenceExpression, Visit(node.Predicate));
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
						return node.Update(operand);
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
					else
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

				var method = Builder.MakeExpression(_context, node, localFlags);

				if (!_flags.IsTest())
					method = Builder.UpdateNesting(_context, method);

				if (!ReferenceEquals(method, node))
					return Visit(method);

				var attr = node.Method.GetExpressionAttribute(MappingSchema);
				if (attr != null)
				{
					return CreatePlaceholder(_context, Builder.ConvertExtensionToSql(_context, localFlags, attr, node), node, alias: _alias);
				}

				var newNode = Builder.HandleExtension(_context, node, _flags);
				if (!ReferenceEquals(newNode, node))
					return Visit(newNode);

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
				transformed = null;

				// Shortcut: if expression can be compiled we can live it as is but inject accessors 
				//
				if (_flags.IsExpression()             &&
				    expr.NodeType != ExpressionType.New      &&
				    //expr.NodeType != ExpressionType.Constant &&
				    expr.NodeType != ExpressionType.Default  &&
				    expr is not DefaultValueExpression       &&
					expr != ExpressionConstants.DataContextParam &&
				    Builder.CanBeCompiled(expr, false))
				{
					// correct expression based on accessors

					var valueAccessor = Builder.ParametersContext.ReplaceParameter(
						Builder.ParametersContext._expressionAccessors, expr, false, s => { });

					var valueExpr = valueAccessor.ValueExpression;

					if (valueExpr.Type != expr.Type)
					{
						valueExpr = Expression.Convert(valueExpr.UnwrapConvert(), expr.Type);
					}

					transformed = valueExpr;
					return true;
				}

				return false;
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
			/*
			if (flags.IsExtractProjection())
			{
				var projectVisitor = new ProjectionVisitor(context);
				return projectVisitor.Visit(expression);
			}
			*/

			var visitor =  new BuildVisitor();

			var result = visitor.Build(context, expression, flags, buildFlags);
			return result;
		}

	}
}
