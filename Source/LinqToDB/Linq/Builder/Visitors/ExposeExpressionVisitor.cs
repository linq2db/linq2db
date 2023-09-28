using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder.Visitors
{
	class ExposeExpressionVisitor : ExpressionVisitorBase
	{
		ExpressionBuilder _builder       = default!;
		MappingSchema     _mappingSchema = default!;

		public ExpressionBuilder Builder       => _builder;
		public MappingSchema     MappingSchema => _mappingSchema;

		Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;

		public Expression ExposeExpression(Expression expression, ExpressionBuilder builder, MappingSchema mappingSchema)
		{
			_builder       = builder;
			_mappingSchema = mappingSchema;

			return Visit(expression);
		}

		public override void Cleanup()
		{
			_builder       = default!;
			_mappingSchema = default!;

			_allowedParameters?.Clear();

			base.Cleanup();
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			var l = ConvertExpressionMethodAttribute(node.Object?.Type ?? node.Method.ReflectedType!, node.Method, out var alias);

			if (l != null)
			{
				var converted = ConvertMethod(node, MappingSchema, l);
				converted = Visit(converted);
				return AliasCall(converted, alias);
			}

			if (TryConvertIQueryable(node, out var convertedQuery))
				return Visit(convertedQuery);

			if (node.Method.IsSqlPropertyMethodEx())
			{
				// transform Sql.Property into member access
				if (node.Arguments[1].Type != typeof(string))
					throw new ArgumentException("Only strings are allowed for member name in Sql.Property expressions.");

				var entity               = Visit(node.Arguments[0].UnwrapConvertToObject());
				var memberNameExpression = Visit(node.Arguments[1]);
				var memberName           = Builder.EvaluateExpression<string>(memberNameExpression);
				if (memberName == null)
					throw new InvalidOperationException(
						$"Could not retrieve member mane from expression '{memberNameExpression}'");

				var entityDescriptor = MappingSchema.GetEntityDescriptor(entity.Type, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

				var memberInfo = entityDescriptor[memberName]?.MemberInfo;
				if (memberInfo == null)
				{
					foreach (var a in entityDescriptor.Associations)
					{
						if (a.MemberInfo.Name == memberName)
						{
							if (memberInfo != null)
								throw new InvalidOperationException("Sequence contains more than one element");
							memberInfo = a.MemberInfo;
						}
					}
				}

				if (memberInfo == null)
					memberInfo = MemberHelper.GetMemberInfo(node);

				return Expression.MakeMemberAccess(entity, memberInfo);
			}

			var result = base.VisitMethodCall(node);
			return result;
		}

		bool TryConvertIQueryable(Expression node, out Expression converted)
		{
			if (typeof(IQueryable).IsSameOrParentOf(node.Type) && !typeof(Sql.IQueryableContainer).IsSameOrParentOf(node.Type))
			{
				if (node is MethodCallExpression mc && mc.IsQueryable())
				{
					if (mc.Arguments[0] is MemberExpression or ConstantExpression)
					{
						var visitorM = new IsCompilableVisitor();

						if (visitorM.CanBeCompiled(mc, Builder.OptimizationContext))
						{
							var evaluated = Builder.EvaluateExpression(mc.Arguments[0]);
							if (evaluated != null)
							{
								var evaluatedType = evaluated.GetType();
								if (!typeof(CteTable<>).IsSameOrParentOf(evaluatedType))
								{
									if (evaluated is IDataContext dc)
									{
										var args  = mc.Arguments.ToArray();
										args[0]   = SqlQueryRootExpression.Create(dc, evaluatedType);
										mc        = mc.Update(mc.Object, args);
										converted = mc;
										return true;
									}

									converted = Builder.ConvertIQueryable(node);
									return !ExpressionEqualityComparer.Instance.Equals(converted, node);
								}
							}
						}
					}
					converted = mc;
					return false;
				}

				var visitor = new IsCompilableVisitor();

				if (visitor.CanBeCompiled(node, Builder.OptimizationContext))
				{
					converted = Builder.ConvertIQueryable(node);
					return !ExpressionEqualityComparer.Instance.Equals(converted, node);
				}
			}

			converted = node;
			return false;
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			if (Builder.CompiledParameters != null)
			{
				var idx = Array.IndexOf(Builder.CompiledParameters, node);
				if (idx >= 0)
				{
					return Expression.Convert(Expression.ArrayIndex(ExpressionBuilder.ParametersParam, ExpressionInstances.Int32(idx)), node.Type);
				}
			}

			return base.VisitParameter(node);
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			var l = ConvertExpressionMethodAttribute(node.Expression?.Type ?? node.Member.ReflectedType!, node.Member, out var alias);

			if (l != null)
			{
				var converted = ConvertMemberExpression(node, MappingSchema, node.Expression!, l);
				converted = Visit(converted);
				return AliasCall(converted, alias);
			}

			// Replace Count with Count()
			//
			if (node.Member.Name == "Count")
			{
				var isList = typeof(System.Collections.ICollection).IsAssignableFrom(node.Member.DeclaringType);

				if (!isList)
				{
					isList =
						node.Member.DeclaringType!.IsGenericType &&
						node.Member.DeclaringType.GetGenericTypeDefinition() == typeof(ICollection<>);
				}

				if (!isList)
				{
					isList = node.Member.DeclaringType!.GetInterfaces()
						.Any(static t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
				}

				if (isList)
				{
					var mi = ExpressionBuilder.EnumerableMethods
						.First(static m => m.Name == "Count" && m.GetParameters().Length == 1)
						.MakeGenericMethod(node.Expression!.Type.GetItemType()!);

					return Expression.Call(null, mi, node.Expression);
				}
			}

			if (TryConvertIQueryable(node, out var convertedQuery))
				return Visit(convertedQuery);

			return base.VisitMember(node);
		}

		protected override Expression VisitInvocation(InvocationExpression node)
		{
			if (node.Expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)node.Expression;
				if (mc.Method.Name == "Compile" &&
				    typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType!))
				{
					if (mc.Object.EvaluateExpression() is LambdaExpression lamda)
					{
						var newBody = lamda.Body;
						if (node.Arguments.Count > 0)
						{
							var map = new Dictionary<Expression, Expression>();
							for (int i = 0; i < node.Arguments.Count; i++)
								map.Add(lamda.Parameters[i], node.Arguments[i]);

							newBody = lamda.Body.Transform(map, static (map, se) =>
							{
								if (se.NodeType == ExpressionType.Parameter &&
								    map.TryGetValue(se, out var newExpr))
									return newExpr;
								return se;
							});
						}

						return Visit(newBody);
					}
				}
			}

			return base.VisitInvocation(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			_allowedParameters ??= new();

			_allowedParameters.Push(node.Parameters);

			var newNode = base.VisitLambda(node);

			_allowedParameters.Pop();

			return newNode;
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (node.Value is IQueryable queryable/* && !(queryable is ITable)*/)
			{
				if (!ExpressionEqualityComparer.Instance.Equals(queryable.Expression, node))
					return Visit(queryable.Expression);
			}

			return base.VisitConstant(node);
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			switch (node.NodeType)
			{
				case ExpressionType.ArrayLength:
				{
					//TODO: WTF?
					throw new NotImplementedException();
					var ll = Expressions.ConvertMember(MappingSchema, node.Operand?.Type, node.Operand!.Type.GetProperty(nameof(Array.Length))!);
					if (ll != null)
					{
						var exposed = ConvertMemberExpression(node, MappingSchema, node.Operand!, ll);

						return Visit(exposed);
					}

					break;
				}

				case ExpressionType.Convert:
				{
					if (node.Method != null)
					{
						var l = ConvertExpressionMethodAttribute(node.Method.DeclaringType!, node.Method, out var alias);
						if (l != null)
						{
							var exposed = l.GetBody(node.Operand);
							return Visit(exposed);
						}
					}

					break;
				}
			}

			return base.VisitUnary(node);
		}

		#region Helper methods

		class IsCompilableVisitor : ExpressionVisitorBase
		{
			bool _canBeCompiled;

			bool _inMethod;

			Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;

			ExpressionTreeOptimizationContext _optimizationContext = default!;

			bool CanBeCompiledFlag
			{
				get => _canBeCompiled;
				set
				{
					_canBeCompiled = value;
				}
			}

			public bool CanBeCompiled(Expression  expression,
				ExpressionTreeOptimizationContext optimizationContext)
			{
				Cleanup();

				_optimizationContext = optimizationContext;

				_ = Visit(expression);

				return _canBeCompiled;
			}

			public override void Cleanup()
			{
				_canBeCompiled       = true;
				_inMethod            = false;
				_optimizationContext = default!;

				_allowedParameters?.Clear();

				base.Cleanup();
			}

			public override Expression? Visit(Expression? node)
			{
				if (!_canBeCompiled)
					return node;

				return base.Visit(node);
			}

			protected override Expression VisitLambda<T>(Expression<T> node)
			{
				if (!_inMethod)
				{
					CanBeCompiledFlag = false;
					return node;
				}

				_allowedParameters ??= new();

				_allowedParameters.Push(node.Parameters);

				_ = base.VisitLambda(node);

				_allowedParameters.Pop();

				return node;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node == ExpressionBuilder.ParametersParam)
					return node;

				if (_allowedParameters == null || !_allowedParameters.Any(ps => ps.Contains(node)))
					CanBeCompiledFlag = false;

				return node;
			}

			internal override Expression VisitContextRefExpression(ContextRefExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			internal override Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (!CanBeCompiledFlag)
				{
					return node;
				}

				if (_optimizationContext.IsServerSideOnly(node, false))
					CanBeCompiledFlag = false;

				var save = _inMethod;
				_inMethod = true;

				base.VisitMethodCall(node);

				_inMethod = save;

				return node;
			}

			internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
			{
				CanBeCompiledFlag = false;
				return assignment;
			}

			internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
			{
				CanBeCompiledFlag = false;
				return parameter;
			}

			internal override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}
		}

		static Expression AliasCall(Expression expression, string? alias)
		{
			if (string.IsNullOrEmpty(alias))
				return expression;

			return Expression.Call(Methods.LinqToDB.SqlExt.Alias.MakeGenericMethod(expression.Type), expression,
				Expression.Constant(alias));
		}

		public Expression ConvertMethod(MethodCallExpression node, MappingSchema mappingSchema, LambdaExpression replacementLambda)
		{
			var replacementBody = replacementLambda.Body.Unwrap();
			var parms           = new Dictionary<ParameterExpression,int>(replacementLambda.Parameters.Count);
			var pn              = node.Method.IsStatic ? 0 : -1;

			foreach (var p in replacementLambda.Parameters)
				parms.Add(p, pn++);

			var newNode = replacementBody.Transform((node, parms, mappingSchema), static (context, wpi) =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (context.parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						if (n >= context.node.Arguments.Count)
						{
							if (typeof(IDataContext).IsSameOrParentOf(wpi.Type))
							{
								return SqlQueryRootExpression.Create(context.mappingSchema, wpi.Type);
							}

							throw new LinqToDBException($"Can't convert {wpi} to expression.");
						}

						var result = n < 0 ? context.node.Object! : context.node.Arguments[n];

						if (result.Type != wpi.Type)
						{
							var noConvert = result.UnwrapConvert();
							if (noConvert.Type == wpi.Type)
							{
								result = noConvert;
							}
							else
							{
								if (noConvert.Type.IsValueType)
									result = Expression.Convert(noConvert, wpi.Type);
							}
						}

						return result;
					}
				}

				return wpi;
			});

			if (node.Method.ReturnType != newNode.Type)
			{
				newNode = newNode.UnwrapConvert();
				if (node.Method.ReturnType != newNode.Type)
				{
					newNode = Expression.Convert(newNode, node.Method.ReturnType);
				}
			}

			Builder.ParametersContext.AddExpressionAccessors(newNode.GetExpressionAccessors(ExpressionBuilder.ExpressionParam));

			return newNode;
		}

		static Expression ConvertMemberExpression(Expression expr, MappingSchema mappingSchema, Expression root, LambdaExpression l)
		{
			var body  = l.Body.Unwrap();
			var parms = l.Parameters.ToDictionary(p => p);
			var ex = body.Transform(
				(parms, root, mappingSchema),
				static (context, wpi) =>
				{
					if (wpi.NodeType == ExpressionType.Parameter && context.parms.ContainsKey((ParameterExpression)wpi))
					{
						if (wpi.Type.IsSameOrParentOf(context.root.Type))
						{
							return context.root;
						}

						if (typeof(IDataContext).IsSameOrParentOf(wpi.Type))
						{
							return SqlQueryRootExpression.Create(context.mappingSchema, wpi.Type);
						}

						throw new LinqToDBException($"Can't convert {wpi} to expression.");
					}

					return wpi;
				});

			if (ex.Type != expr.Type)
			{
				ex = Expression.Convert(ex, expr.Type);
			}

			return ex;
		}

		public LambdaExpression? ConvertExpressionMethodAttribute(Type type, MemberInfo mi, out string? alias)
		{
			mi = type.GetMemberOverride(mi);

			var attr = MappingSchema.GetAttribute<ExpressionMethodAttribute>(type, mi);

			if (attr != null)
			{
				alias = attr.Alias ?? mi.Name;
				if (attr.Expression != null)
					return attr.Expression;

				if (!string.IsNullOrEmpty(attr.MethodName))
				{
					Expression expr;

					if (mi is MethodInfo method && method.IsGenericMethod)
					{
						var args  = method.GetGenericArguments();
						var names = args.Select(t => (object)t.Name).ToArray();
						var name  = string.Format(attr.MethodName!, names);

						expr = Expression.Call(
							mi.DeclaringType!,
							name,
							name != attr.MethodName ? Array<Type>.Empty : args);
					}
					else
					{
						expr = Expression.Call(mi.DeclaringType!, attr.MethodName!, Array<Type>.Empty);
					}

					var evaluated = (LambdaExpression?)expr.EvaluateExpression();
					return evaluated;
				}
			}

			alias = null;
			return null;
		}

		#endregion

	}
}
