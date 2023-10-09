using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder.Visitors
{
	using Common;
	using Extensions;
	using Mapping;
	using Reflection;
	using LinqToDB.Expressions;

	class ExposeExpressionVisitor : ExpressionVisitorBase
	{
		ExpressionBuilder _builder       = default!;
		MappingSchema     _mappingSchema = default!;
		bool              _includeConvert;

		public ExpressionBuilder Builder       => _builder;
		public MappingSchema     MappingSchema => _mappingSchema;

		Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;

		public Expression ExposeExpression(
			Expression        expression,    
			ExpressionBuilder builder,
			MappingSchema     mappingSchema, 
			bool              includeConvert)
		{
			_builder        = builder;
			_mappingSchema  = mappingSchema;
			_includeConvert = includeConvert;

			return Visit(expression);
		}

		public override void Cleanup()
		{
			_builder        = default!;
			_mappingSchema  = default!;
			_includeConvert = false;

			_allowedParameters?.Clear();

			base.Cleanup();
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			var l = ConvertExpressionMethodAttribute(node.Object?.Type ?? node.Method.ReflectedType!, node.Method, out var alias);

			if (l != null)
			{
				var converted = ConvertMethod(node, l);
				converted = Visit(converted);
				return AliasCall(converted, alias);
			}

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

			if (TryConvertIQueryable(node, out var convertedQuery))
				return Visit(convertedQuery);

			if (_includeConvert)
			{
				var newNode = ConvertMethod(node);
				if (newNode != null)
					return Visit(newNode);
			}

			var result = base.VisitMethodCall(node);
			return result;
		}

		Expression? ConvertMethod(MethodCallExpression pi)
		{
			LambdaExpression? lambda = null;

			if (!pi.Method.IsStatic && pi.Object != null && pi.Object.Type != pi.Method.DeclaringType)
			{
				var concreteTypeMemberInfo = pi.Object.Type.GetMemberEx(pi.Method);
				if (concreteTypeMemberInfo != null)
					lambda = Expressions.ConvertMember(MappingSchema, pi.Object.Type, concreteTypeMemberInfo);
			}

			lambda ??= Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

			return lambda == null ? null : ConvertMethod(pi, lambda);
		}

		Expression? ConvertBinary(BinaryExpression node)
		{
			var l = Expressions.ConvertBinary(MappingSchema, node);
			if (l != null)
			{
				var body = l.Body.Unwrap();
				var expr = body.Transform((l, node), static (context, wpi) =>
				{
					if (wpi.NodeType == ExpressionType.Parameter)
					{
						if (context.l.Parameters[0] == wpi)
							return context.node.Left;
						if (context.l.Parameters[1] == wpi)
							return context.node.Right;
					}

					return wpi;
				});

				if (expr.Type != node.Type)
					expr = new ChangeTypeExpression(expr, node.Type);

				return expr;
			}

			return null;
		}

		bool TryConvertIQueryable(Expression node, out Expression converted)
		{
			if (typeof(IQueryable).IsSameOrParentOf(node.Type) && !typeof(Sql.IQueryableContainer).IsSameOrParentOf(node.Type))
			{
				if (node is MethodCallExpression mc)
				{
					var attr = mc.Method.GetTableFunctionAttribute(MappingSchema);
					if (attr != null)
					{
						converted = mc;
						return false;
					}

					if (mc.IsQueryable())
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
											var args = mc.Arguments.ToArray();
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

					return Visit(Expression.Call(null, mi, node.Expression));
				}
			}

			if (TryConvertIQueryable(node, out var convertedQuery))
				return Visit(convertedQuery);

			if (_includeConvert)
			{
				var converted = ConvertMemberAccess(node);
				if (converted != null)
					return Visit(converted);
			}

			return base.VisitMember(node);
		}

		interface IConvertHelper
		{
			Expression ConvertNull(MemberExpression expression);
		}

		sealed class ConvertHelper<T> : IConvertHelper
			where T : struct
		{
			public Expression ConvertNull(MemberExpression expression)
			{
				return Expression.Call(
					null,
					MemberHelper.MethodOf<T?>(p => Sql.ToNotNull(p)),
					expression.Expression!);
			}
		}

		Expression? ConvertMemberAccess(MemberExpression node)
		{
			var l = Expressions.ConvertMember(MappingSchema, node.Expression?.Type, node.Member);

			if (l != null)
			{
				var body = l.Body.Unwrap();
				var expr = body.Transform(node, static (ma, wpi) => wpi.NodeType == ExpressionType.Parameter ? ma.Expression! : wpi);

				if (expr.Type != node.Type)
				{
					//expr = new ChangeTypeExpression(expr, e.Type);
					expr = Expression.Convert(expr, node.Type);
				}

				return expr;
			}

			if (node.Member.IsNullableValueMember())
			{
				var ntype  = typeof(ConvertHelper<>).MakeGenericType(node.Type);
				var helper = (IConvertHelper)Activator.CreateInstance(ntype)!;
				var expr   = helper.ConvertNull(node);

				return expr;
			}

			if (node.Member.DeclaringType == typeof(TimeSpan))
			{
				switch (node.Expression!.NodeType)
				{
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:

						Sql.DateParts datePart;

						switch (node.Member.Name)
						{
							case "TotalMilliseconds": datePart = Sql.DateParts.Millisecond; break;
							case "TotalSeconds"     : datePart = Sql.DateParts.Second;      break;
							case "TotalMinutes"     : datePart = Sql.DateParts.Minute;      break;
							case "TotalHours"       : datePart = Sql.DateParts.Hour;        break;
							case "TotalDays"        : datePart = Sql.DateParts.Day;         break;
							default                 : return null;
						}

						var ex = (BinaryExpression)node.Expression;
						if (ex.Left.Type == typeof(DateTime)
							&& ex.Right.Type == typeof(DateTime))
						{
							var method = MemberHelper.MethodOf(
										() => Sql.DateDiff(Sql.DateParts.Day, DateTime.MinValue, DateTime.MinValue));

							var call   =
										Expression.Convert(
											Expression.Call(
												null,
												method,
												Expression.Constant(datePart),
												Expression.Convert(ex.Right, typeof(DateTime?)),
												Expression.Convert(ex.Left,  typeof(DateTime?))),
											typeof(double));

							return call;
						}
						else
						{
							var method = MemberHelper.MethodOf(
										() => Sql.DateDiff(Sql.DateParts.Day, DateTimeOffset.MinValue, DateTimeOffset.MinValue));

							var call =
								Expression.Convert(
									Expression.Call(
										null,
										method,
										Expression.Constant(datePart),
										Expression.Convert(ex.Right, typeof(DateTimeOffset?)),
										Expression.Convert(ex.Left, typeof(DateTimeOffset?))),
									typeof(double));

							return call;
						}
				}
			}

			return null;
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

		protected override Expression VisitBinary(BinaryExpression node)
		{
			switch (node.NodeType)
			{
				//This is to handle VB's weird expression generation when dealing with nullable properties.
				case ExpressionType.Coalesce:
				{
					if (node.Left is BinaryExpression equalityLeft && node.Right is ConstantExpression constantRight)
						if (equalityLeft.Type.IsNullable())
							if (equalityLeft.NodeType == ExpressionType.Equal && equalityLeft.Left.Type == equalityLeft.Right.Type)
								if (constantRight.Value is bool val && val == false)
									return Visit(equalityLeft);

					break;
				}
			}

			if (_includeConvert)
			{
				var converted = ConvertBinary(node);
				if (converted != null)
				{
					return Visit(converted);
				}
			}

			return base.VisitBinary(node);
		}

		protected override Expression VisitNew(NewExpression node)
		{
			if (_includeConvert)
			{
				var newNode = ConvertNew(node);
				if (newNode != null)
					return Visit(newNode);
			}

			return base.VisitNew(node);
		}

		Expression? ConvertNew(NewExpression pi)
		{
			if (pi.Constructor == null)
				return null;

			var lambda = Expressions.ConvertMember(MappingSchema, pi.Type, pi.Constructor);

			if (lambda != null)
			{
				var ef    = lambda.Body.Unwrap();
				var parms = new Dictionary<string,int>(lambda.Parameters.Count);
				var pn    = 0;

				foreach (var p in lambda.Parameters)
					parms.Add(p.Name!, pn++);

				return ef.Transform((pi, parms), static (context, wpi) =>
				{
					if (wpi.NodeType == ExpressionType.Parameter)
					{
						var pe = (ParameterExpression)wpi;
						var n  = context.parms[pe.Name!];
						return context.pi.Arguments[n];
					}

					return wpi;
				});
			}

			return null;
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
				{
					_canBeCompiled = false;
					return node;
				}

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

		public Expression ConvertMethod(MethodCallExpression node, LambdaExpression replacementLambda)
		{
			var replacementBody = replacementLambda.Body.Unwrap();
			var parms           = new Dictionary<ParameterExpression,int>(replacementLambda.Parameters.Count);
			var pn              = node.Method.IsStatic ? 0 : -1;

			foreach (var p in replacementLambda.Parameters)
				parms.Add(p, pn++);

			var newNode = replacementBody.Transform((node, parms, MappingSchema), static (context, wpi) =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (context.parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						if (n >= context.node.Arguments.Count)
						{
							if (typeof(IDataContext).IsSameOrParentOf(wpi.Type))
							{
								return SqlQueryRootExpression.Create(context.MappingSchema, wpi.Type);
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
