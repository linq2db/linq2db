using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder.Visitors
{
	sealed class ExposeExpressionVisitor : ExpressionVisitorBase, IExpressionEvaluator
	{
		static ObjectPool<IsCompilableVisitor> _isCompilableVisitorPool = new(() => new IsCompilableVisitor(), v => v.Cleanup(), 100);

		IDataContext                      _dataContext         = default!;
		IMemberConverter                  _memberConverter     = default!;
		ExpressionTreeOptimizationContext _optimizationContext = default!;
		object?[]?                        _parameterValues;
		bool                              _includeConvert;
		bool                              _optimizeConditions;
		bool                              _compactBinary;
		bool                              _isSingleConvert;

		public IDataContext  DataContext   => _dataContext;
		public MappingSchema MappingSchema => _dataContext.MappingSchema;

		Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;

		public Expression ExposeExpression(IDataContext dataContext,
			ExpressionTreeOptimizationContext           optimizationContext,
			object?[]?                                  parameterValues,
			Expression                                  expression,
			bool                                        includeConvert,
			bool                                        optimizeConditions,
			bool                                        compactBinary,
			bool                                        isSingleConvert)
		{
			_dataContext         = dataContext;
			_includeConvert      = includeConvert;
			_optimizationContext = optimizationContext;
			_parameterValues     = parameterValues;
			_optimizeConditions  = optimizeConditions;
			_compactBinary       = compactBinary;
			_isSingleConvert     = isSingleConvert;
			_memberConverter     = ((IInfrastructure<IServiceProvider>)dataContext).Instance.GetRequiredService<IMemberConverter>();

			return Visit(expression);
		}

		public override void Cleanup()
		{
			_dataContext         = default!;
			_includeConvert      = default;
			_memberConverter     = default!;
			_optimizationContext = default!;
			_optimizeConditions  = default;
			_compactBinary       = false;
			_isSingleConvert     = false;

			_allowedParameters?.Clear();

			base.Cleanup();
		}

#if DEBUG
		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null)
				return null;

			return base.Visit(node);
		}
#endif

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			var convertedMember = _memberConverter.Convert(node, out var handled);
			if (handled && !ReferenceEquals(node, convertedMember))
			{
				return Visit(convertedMember);
			}

			var l = ConvertExpressionMethodAttribute(node.Object?.Type ?? node.Method.ReflectedType!, node.Method, out var alias);

			if (l != null)
			{
				var converted = ConvertMethod(node, l);
				converted = Visit(converted);
				return AliasCall(converted, alias);
			}

			if (node.Method.IsSqlPropertyMethodEx())
			{
				return HandleSqlProperty(node);
			}

			if (node.Method.Name == "Compile" &&
				typeof(LambdaExpression).IsSameOrParentOf(node.Method.DeclaringType!))
			{
				if (node.Object.EvaluateExpression() is LambdaExpression lambda)
				{
					return Visit(lambda);
				}
			}

			if (node.Method.Name == "Invoke" && node.Object is LambdaExpression invokeLambda)
			{
				return HandleInvoke(node, invokeLambda);
			}

			if (node.Method.Name == nameof(DataExtensions.QueryFromExpression) &&
				node.Method.DeclaringType == typeof(DataExtensions))
			{
				if (node.Arguments[1].EvaluateExpression() is LambdaExpression lambda)
				{
					return Visit(lambda.Body);
				}
			}

			if (TryConvertIQueryable(node, out var convertedQuery))
			{
				var save = _compactBinary;
				_compactBinary = true;
				convertedQuery = Visit(convertedQuery);
				_compactBinary = save;

				return convertedQuery;
			}

			if (_includeConvert)
			{
				var newNode = ConvertMethod(node);
				if (newNode != null)
				{
					return Visit(newNode);
				}
			}

			var dependentParameters = SqlQueryDependentAttributeHelper.GetQueryDependentAttributes(node.Method);

			if (dependentParameters != null)
			{
				node = HandleSqlDependentParameters(node, dependentParameters);
			}

			if (_isSingleConvert)
				return node;

			var newEvaluatedArguments = TryEvaluateArguments(node);

			if (newEvaluatedArguments != null)
			{
				return Visit(node.Update(node.Object, newEvaluatedArguments));
			}

			var result = base.VisitMethodCall(node);
			return result;

			MethodCallExpression HandleSqlDependentParameters(MethodCallExpression node, IList<SqlQueryDependentAttribute?> dependentParameters)
			{
				var           arguments    = node.Arguments;
				Expression[]? newArguments = null;

				for (var i = 0; i < arguments.Count; i++)
				{
					var attr = dependentParameters[i];
					if (attr != null)
					{
						var argument = arguments[i];
						if (argument.NodeType != ExpressionType.Constant)
						{
							var newArgument = attr.PrepareForCache(argument, this);
							if (newArgument.Type != argument.Type)
								newArgument = Expression.Convert(newArgument, argument.Type);

							if (!ReferenceEquals(newArgument, argument))
							{
								newArguments ??= arguments.ToArray();
								newArguments[i] = newArgument;
							}
						}
					}
				}

				if (newArguments != null)
				{
					node = node.Update(node.Object, newArguments);
				}

				return node;
			}

			Expression HandleSqlProperty(MethodCallExpression node)
			{
				// transform Sql.Property into member access
				if (node.Arguments[1].Type != typeof(string))
					throw new ArgumentException("Only strings are allowed for member name in Sql.Property expressions.");

				var entity               = Visit(node.Arguments[0].UnwrapConvertToObject());
				var memberNameExpression = Visit(node.Arguments[1]);
				var memberName           = memberNameExpression.EvaluateExpression<string>();
				if (memberName == null)
					throw new InvalidOperationException(
						$"Could not retrieve member name from expression '{memberNameExpression}'");

				var entityDescriptor = MappingSchema.GetEntityDescriptor(entity.Type, DataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

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

			Expression HandleInvoke(MethodCallExpression node, LambdaExpression invokeLambda)
			{
				var body = invokeLambda.Body;

				if (node.Arguments.Count == 1)
					body = body.Replace(invokeLambda.Parameters[0], node.Arguments[0]);
				else if (node.Arguments.Count > 1)
				{
					var dict = invokeLambda.Parameters.Select((p, i) => (p, i)).ToDictionary(p => (Expression)p.p, p => node.Arguments[p.i]);
					body = body.Replace(dict);
				}

				return Visit(body);
			}

			Expression[]? TryEvaluateArguments(MethodCallExpression node)
			{
				Expression[]? newEvaluatedArguments = null;

				for (var i = 0; i < node.Arguments.Count; i++)
				{
					var argument = node.Arguments[i];

					if (argument.NodeType != ExpressionType.Quote && typeof(Expression<>).IsSameOrParentOf(argument.Type))
					{
						if (IsCompilable(argument))
						{
							var evaluated = EvaluateExpression(argument);
							if (evaluated is Expression evaluatedExpr)
							{
								if (newEvaluatedArguments == null)
								{
									newEvaluatedArguments ??= node.Arguments.ToArray();
									newEvaluatedArguments[i] = evaluatedExpr;
								}
							}
						}
					}
				}

				return newEvaluatedArguments;
			}
		}

		Expression? ConvertMethod(MethodCallExpression pi)
		{
			LambdaExpression? lambda = null;

			if (!pi.Method.IsStatic && pi.Object != null && pi.Object.Type != pi.Method.DeclaringType)
			{
				var concreteTypeMemberInfo = pi.Object.Type.GetMemberEx(pi.Method);
				if (concreteTypeMemberInfo != null)
					lambda = LinqToDB.Linq.Expressions.ConvertMember(MappingSchema, pi.Object.Type, concreteTypeMemberInfo);
			}

			lambda ??= LinqToDB.Linq.Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

			Expression? result = null;

			if (lambda != null)
				result = ConvertMethod(pi, lambda);

			return result;
		}

		Expression? ConvertBinary(BinaryExpression node)
		{
			var l = LinqToDB.Linq.Expressions.ConvertBinary(MappingSchema, node);
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

		object? EvaluateExpression(Expression? expression)
		{
			if (expression == null)
				return null;

			// Shortcut for constants
			if (expression.NodeType == ExpressionType.Constant)
				return ((ConstantExpression)expression).Value;

			var expr = expression.Transform(e =>
			{
				if (e is SqlQueryRootExpression root)
				{
					if (((IConfigurationID)root.MappingSchema).ConfigurationID ==
					    ((IConfigurationID)DataContext.MappingSchema).ConfigurationID)
					{
						return Expression.Constant(DataContext, e.Type);
					}
				}
				else if (e.NodeType == ExpressionType.Parameter)
				{
					if (e == ExpressionConstants.DataContextParam)
					{
						return Expression.Constant(DataContext, e.Type);
					}

					if (e == ExpressionBuilder.ParametersParam && _parameterValues != null)
					{
						return Expression.Constant(_parameterValues, e.Type);
					}
				}

				return e;
			});

			return expr.EvaluateExpression();
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
							if (IsCompilable(mc))
							{
								var evaluated = EvaluateExpression(mc.Arguments[0]);
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

										converted = ConvertIQueryable(node);
										return !ExpressionEqualityComparer.Instance.Equals(converted, node);
									}
								}
							}
						}

						converted = mc;
						return false;
					}
				}

				if (IsCompilable(node))
				{
					converted = ConvertIQueryable(node);

					return !ExpressionEqualityComparer.Instance.Equals(converted, node);
				}
			}

			converted = node;
			return false;
		}

		Expression ConvertIQueryable(Expression expression)
		{
			if (expression.NodeType is ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expression;
				if (mc.Method.DeclaringType != null && MappingSchema.HasAttribute<Sql.QueryExtensionAttribute>(mc.Method.DeclaringType, mc.Method))
					return mc;
			}

			if (expression.NodeType is ExpressionType.MemberAccess or ExpressionType.Call)
			{
				if (EvaluateExpression(expression) is not IQueryable newQuery)
					return expression;

				return newQuery.Expression;
			}

			throw new InvalidOperationException();
		}

		protected override Expression VisitConditional(ConditionalExpression node)
		{
			if (_optimizeConditions)
			{
				var test    = Visit(node.Test);
				var ifTrue  = Visit(node.IfTrue);
				var ifFalse = Visit(node.IfFalse);

				if (IsCompilable(test))
				{
					if (EvaluateExpression(test) is bool testValue)
					{
						return Visit(testValue ? ifTrue : ifFalse);
					}
				}

				return node.Update(test, ifTrue, ifFalse);
			}

			return base.VisitConditional(node);
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			var convertedMember = _memberConverter.Convert(node, out var handled);
			if (handled && !ReferenceEquals(node, convertedMember))
			{
				return Visit(convertedMember);
			}

			if (!IsCompilable(node))
			{
				var l = ConvertExpressionMethodAttribute(node.Expression?.Type ?? node.Member.ReflectedType!,
					node.Member, out var alias);

				if (l != null)
				{
					var converted = ConvertMemberExpression(node, MappingSchema, node.Expression!, l);
					converted = Visit(converted);
					return AliasCall(converted, alias);
				}
			}

			if (node.Expression != null)
			{
				if (typeof(IQueryable).IsSameOrParentOf(node.Type) && typeof(IDataContext).IsSameOrParentOf(node.Expression.Type))
				{
					// Handling case when CompiledQuery replaced DataContext with ParameterValues access.

					var unwrapped = node.Expression.UnwrapConvert();
					if (unwrapped.NodeType == ExpressionType.ArrayIndex)
					{
						var arrayIndex = (BinaryExpression)unwrapped;
						if (arrayIndex.Left == ExpressionBuilder.ParametersParam && _parameterValues != null)
						{
							var evaluated = EvaluateExpression(node);
							if (evaluated is IQueryable query)
								return Visit(query.Expression);
						}
					}
					else
					{
						if (node.Expression.UnwrapConvert() is SqlQueryRootExpression)
						{
							var evaluated = EvaluateExpression(node);
							if (evaluated is IQueryable query)
								return Visit(query.Expression);
						}
					}
				}
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
					var mi = ExpressionBuilder.EnumerableMethods["Count"]
						.First(static m => m.GetParameters().Length == 1)
						.MakeGenericMethod(node.Expression!.Type.GetItemType()!);

					return Visit(Expression.Call(null, mi, node.Expression));
				}
			}

			if (TryConvertIQueryable(node, out var convertedQuery))
			{
				var save = _compactBinary;
				_compactBinary = true;
				convertedQuery = Visit(convertedQuery);
				_compactBinary = save;

				return convertedQuery;
			}

			if (_includeConvert)
			{
				var converted = ConvertMemberAccess(node);
				if (converted != null)
					return Visit(converted);
			}

			if (typeof(IDataContext).IsSameOrParentOf(node.Type))
			{
				if (_dataContext.GetType().IsSameOrParentOf(node.Type) || node.Type.IsSameOrParentOf(_dataContext.GetType()))
				{
					var dc = EvaluateExpression(node) as IDataContext;
					if ((dc?.MappingSchema as IConfigurationID)?.ConfigurationID ==
					    ((IConfigurationID)_dataContext.MappingSchema).ConfigurationID)
					{
						return new SqlQueryRootExpression(MappingSchema, node.Type);
					}
				}
			}

			return base.VisitMember(node);
		}

		bool IsCompilable(Expression expression)
		{
			using var visitor = _isCompilableVisitorPool.Allocate();
			return visitor.Value.CanBeEvaluatedOnClient(expression, _optimizationContext);
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
			var l = LinqToDB.Linq.Expressions.ConvertMember(MappingSchema, node.Expression?.Type, node.Member);

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

			if (!DataContext.Options.SqlOptions.DisableBuiltInTimeSpanConversion)
			{
				if (node.Member.DeclaringType == typeof(TimeSpan) && node.Expression != null)
				{
					switch (node.Expression.NodeType)
					{
						case ExpressionType.Subtract:
						case ExpressionType.SubtractChecked:

							Sql.DateParts datePart;

							switch (node.Member.Name)
							{
								case "TotalMilliseconds": datePart = Sql.DateParts.Millisecond; break;
								case "TotalSeconds": datePart = Sql.DateParts.Second; break;
								case "TotalMinutes": datePart = Sql.DateParts.Minute; break;
								case "TotalHours": datePart = Sql.DateParts.Hour; break;
								case "TotalDays": datePart = Sql.DateParts.Day; break;
								default: return null;
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
					if (mc.Object.EvaluateExpression() is LambdaExpression lambda)
					{
						var newBody = lambda.GetBody(node.Arguments);
						return Visit(newBody);
					}
				}
			}
			else if (node.Expression.NodeType == ExpressionType.Lambda)
			{
				var newBody = ((LambdaExpression)node.Expression).GetBody(node.Arguments);
				return Visit(newBody);
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
			if (node.Value != null)
			{
				if (node.Value is IQueryable queryable)
				{
					if (!ExpressionEqualityComparer.Instance.Equals(queryable.Expression, node))
						return Visit(queryable.Expression);
				}
				else if (node.Value is Sql.IQueryableContainer queryableContainer)
				{
					return Visit(queryableContainer.Query.Expression);
				}
				/*else if (node.Value is Sql.ISqlExtension)
				{
					return Expression.Constant(null, node.Type);
				}*/
			}

			return base.VisitConstant(node);
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			if (node.Method != null)
			{
				var convertedMember = _memberConverter.Convert(node, out var handled);
				if (handled && !ReferenceEquals(node, convertedMember))
				{
					return Visit(convertedMember);
				}

				var l = ConvertExpressionMethodAttribute(node.Method.ReflectedType!, node.Method, out var alias);

				if (l != null)
				{
					var converted = ConvertUnary(node, l);
					converted = Visit(converted);
					return AliasCall(converted, alias);
				}
			}

			switch (node.NodeType)
			{
				case ExpressionType.ArrayLength:
				{
					var ll = LinqToDB.Linq.Expressions.ConvertMember(MappingSchema, node.Operand?.Type, node.Operand!.Type.GetProperty(nameof(Array.Length))!);
					if (ll != null)
					{
						var exposed = ConvertMemberExpression(node, MappingSchema, node.Operand!, ll);
				
						return Visit(exposed);
					}
				
					break;
				}
			}

			return base.VisitUnary(node);
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			if (node.Method != null)
			{
				var convertedMember = _memberConverter.Convert(node, out var handled);
				if (handled && !ReferenceEquals(node, convertedMember))
				{
					return Visit(convertedMember);
				}

				var l = ConvertExpressionMethodAttribute(node.Method.ReflectedType!, node.Method, out var alias);

				if (l != null)
				{
					var converted = ConvertBinary(node, l);
					converted = Visit(converted);
					return AliasCall(converted, alias);
				}
			}

			switch (node.NodeType)
			{
				//This is to handle VB's weird expression generation when dealing with nullable properties.
				case ExpressionType.Coalesce:
				{
					if (node.Left is BinaryExpression equalityLeft && node.Right is ConstantExpression constantRight)
						if (equalityLeft.Type.IsNullableType)
							if (equalityLeft.NodeType == ExpressionType.Equal && equalityLeft.Left.Type == equalityLeft.Right.Type)
								if (constantRight.Value is bool val && val == false)
								{
									var result = Visit(equalityLeft);
									if (result.Type != node.Type)
										result = Expression.Convert(result, node.Type);
									return result;
								}

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

			if (_compactBinary)
			{
				var compacted = BinaryExpressionAggregatorVisitor.Instance.Visit(node);
				if (!ReferenceEquals(compacted, node))
				{
					node = (BinaryExpression)compacted;
				}
			}

			var save = _compactBinary;
			_compactBinary = false;

			var newNode = base.VisitBinary(node);

			_compactBinary = save;

			return newNode;
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

			var lambda = LinqToDB.Linq.Expressions.ConvertMember(MappingSchema, pi.Type, pi.Constructor);

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

		sealed class IsCompilableVisitor : CanBeEvaluatedOnClientCheckVisitorBase
		{
			public bool CanBeEvaluatedOnClient(Expression expression, ExpressionTreeOptimizationContext optimizationContext)
			{
				Cleanup();

				OptimizationContext = optimizationContext;

				_ = Visit(expression);

				return CanBeEvaluated;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node == ExpressionBuilder.ParametersParam)
				{
					CanBeEvaluated = false;
					return node;
				}

				return base.VisitParameter(node);
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

			return newNode;
		}

		public Expression ConvertUnary(UnaryExpression node, LambdaExpression replacementLambda)
		{
			var replacementBody = replacementLambda.Body.Unwrap();
			var parms           = new Dictionary<ParameterExpression,int>(replacementLambda.Parameters.Count);
			var pn              = node.Method!.IsStatic ? 0 : -1;

			foreach (var p in replacementLambda.Parameters)
				parms.Add(p, pn++);

			var newNode = replacementBody.Transform((node, parms, MappingSchema), static (context, wpi) =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (context.parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						var result = context.node.Operand;

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

			return newNode;
		}

		public Expression ConvertBinary(BinaryExpression node, LambdaExpression replacementLambda)
		{
			var replacementBody = replacementLambda.Body.Unwrap();
			var parms           = new Dictionary<ParameterExpression,int>(replacementLambda.Parameters.Count);
			var pn              = node.Method!.IsStatic ? 0 : -1;

			foreach (var p in replacementLambda.Parameters)
				parms.Add(p, pn++);

			var newNode = replacementBody.Transform((node, parms, MappingSchema), static (context, wpi) =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (context.parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						var result = n == 0 ? context.node.Left : context.node.Right;

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

			if (attr == null && type != mi.ReflectedType && mi.DeclaringType?.IsInterface == true && type.IsClass)
			{
				var newInfo = type.GetImplementation(mi);
				if (newInfo != null)
				{
					attr = MappingSchema.GetAttribute<ExpressionMethodAttribute>(type, newInfo);
					mi   = newInfo;
				}
			}

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
						var name  = string.Format(CultureInfo.InvariantCulture, attr.MethodName!, names);

						expr = Expression.Call(
							mi.DeclaringType!,
							name,
							name != attr.MethodName ? [] : args);
					}
					else
					{
						expr = Expression.Call(mi.DeclaringType!, attr.MethodName!, []);
					}

					var evaluated = (LambdaExpression?)expr.EvaluateExpression();
					return evaluated;
				}
			}

			alias = null;
			return null;
		}

		#endregion

		#region IExpressionEvaluator

		bool IExpressionEvaluator.CanBeEvaluated(Expression expression)
		{
			return IsCompilable(expression);
		}

		object? IExpressionEvaluator.Evaluate(Expression expression)
		{
			return EvaluateExpression(expression);
		}

		#endregion
	}
}
