using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

using LinqToDB.Common.Internal;
using LinqToDB.Expressions;
using LinqToDB.Expressions.ExpressionVisitors;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	partial class Sql
	{
		/// <summary>
		/// An Attribute that allows custom Expressions to be defined
		/// for a Method used within a Linq Expression.
		/// </summary>
		[PublicAPI]
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class ExpressionAttribute : MappingAttribute
		{
			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// in place of the method call decorated by this attribute.
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			public ExpressionAttribute(string? expression)
			{
				Expression = expression;
				Precedence = SqlQuery.Precedence.Primary;
				IsPure     = true;
			}

			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// in place of the method call decorated by this attribute.
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			/// <param name="argIndices">Used for setting the order of the method arguments
			/// being passed into the function.</param>
			public ExpressionAttribute(string expression, params int[] argIndices)
			{
				Expression = expression;
				ArgIndices = argIndices;
				Precedence = SqlQuery.Precedence.Primary;
				IsPure     = true;
			}

			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// for the <see cref="ProviderName"/> specified,
			/// in place of the method call decorated by this attribute.
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			/// <param name="configuration">The Database configuration for which this Expression will be used.</param>
			public ExpressionAttribute(string configuration, string expression)
			{
				Configuration = configuration;
				Expression    = expression;
				Precedence    = SqlQuery.Precedence.Primary;
				IsPure        = true;
			}

			/// <summary>
			/// Creates an Expression that will be used in SQL,
			/// for the <see cref="ProviderName"/> specified,
			/// in place of the method call decorated by this attribute.
			/// </summary>
			/// <param name="expression">The SQL expression. Use {0},{1}... for parameters given to the method call.</param>
			/// <param name="configuration">The Database configuration for which this Expression will be used.</param>
			/// <param name="argIndices">Used for setting the order of the method arguments
			/// being passed into the function.</param>
			public ExpressionAttribute(string configuration, string expression, params int[] argIndices)
			{
				Configuration = configuration;
				Expression    = expression;
				ArgIndices    = argIndices;
				Precedence    = SqlQuery.Precedence.Primary;
				IsPure        = true;
			}

			/// <summary>
			/// The expression to be used in building the SQL.
			/// </summary>
			public string?        Expression       { get; set; }
			/// <summary>
			/// The order of Arguments to be passed
			/// into the function from the method call.
			/// </summary>
			public int[]?         ArgIndices       { get; set; }
			/// <summary>
			/// Determines the priority of the expression in evaluation.
			/// Refer to <see cref="SqlQuery.Precedence"/>.
			/// </summary>
			public int            Precedence       { get; set; }
			/// <summary>
			/// If <c>true</c> The expression will only be evaluated on the
			/// database server. If it cannot, an exception will
			/// be thrown.
			/// </summary>
			public bool           ServerSideOnly   { get; set; }
			/// <summary>
			/// If <c>true</c> a greater effort will be made to execute
			/// the expression on the DB server instead of in .NET.
			/// </summary>
			public bool           PreferServerSide { get; set; }
			/// <summary>
			/// If <c>true</c> inline all parameters passed into the expression.
			/// </summary>
			public bool           InlineParameters { get; set; }
			/// <summary>
			/// Used internally by <see cref="ExtensionAttribute"/>.
			/// </summary>
			public bool           ExpectExpression { get; set; }
			/// <summary>
			/// If <c>true</c> the expression is treated as a Predicate
			/// And when used in a Where clause will not have
			/// an added comparison to 'true' in the database.
			/// </summary>
			public bool           IsPredicate      { get; set; }
			/// <summary>
			/// If <c>true</c>, this expression represents an aggregate result
			/// Examples would be SUM(),COUNT().
			/// </summary>
			public bool           IsAggregate      { get; set; }
			/// <summary>
			/// If <c>true</c>, this expression represents a Window Function
			/// Examples would be SUM() OVER(), COUNT() OVER().
			/// </summary>
			public bool           IsWindowFunction { get; set; }
			/// <summary>
			/// If <c>true</c>, it notifies SQL Optimizer that expression returns same result if the same values/parameters are used. It gives optimizer additional information how to simplify query.
			/// For example ORDER BY PureFunction("Str") can be removed because PureFunction function uses constant value.
			/// <example>
			/// For example Random function is NOT Pure function because it returns different result all time.
			/// But expression <see cref="CurrentTimestamp"/> is Pure in case of executed query.
			/// <see cref="DateAdd(DateParts,double?,System.DateTime?)"/> is also Pure function because it returns the same result with the same parameters.
			/// </example>
			/// </summary>
			public bool           IsPure          { get; set; }
			/// <summary>
			/// Used to determine whether the return type should be treated as
			/// something that can be null If CanBeNull is not explicitly set.
			/// <para>Default is <see cref="IsNullableType.Undefined"/>,
			/// which will be treated as <c>true</c></para>
			/// </summary>
			public IsNullableType IsNullable       { get; set; }

			/// <summary>
			/// if <c>true</c>, do not generate generic parameters.
			/// </summary>
			public bool IgnoreGenericParameters { get; set; }

			protected bool? СonfiguredCanBeNull { get; private set; }
			/// <summary>
			/// If <c>true</c>, result can be null.
			/// If value is not set explicitly, nullability calculated based on return type and <see cref="IsNullable"/> value.
			/// </summary>
			public    bool   CanBeNull
			{
				get => СonfiguredCanBeNull ?? true;
				set => СonfiguredCanBeNull = value;
			}

			const  string MatchParamPattern = @"{([0-9a-z_A-Z?]*)(,\s'(.*)')?}";
			static Regex  _matchParamRegEx  = new (MatchParamPattern, RegexOptions.Compiled);

			public static string ResolveExpressionValues<TContext>(TContext context, string expression, Func<TContext, string, string?, string?> valueProvider, out Expression? error)
			{
				if (expression    == null) throw new ArgumentNullException(nameof(expression));
				if (valueProvider == null) throw new ArgumentNullException(nameof(valueProvider));

				int  prevMatch         = -1;
				int  prevNotEmptyMatch = -1;
				bool spaceNeeded       = false;

				Expression? errorExpr = null;

				var str = _matchParamRegEx.Replace(expression, match =>
				{
					var paramName     = match.Groups[1].Value;
					var canBeOptional = paramName.EndsWith("?");
					if (canBeOptional)
						paramName = paramName.TrimEnd('?');

					if (paramName == "_")
					{
						spaceNeeded = true;
						prevMatch   = match.Index + match.Length;
						return string.Empty;
					}

					var delimiter  = match.Groups[3].Success ? match.Groups[3].Value : null;
					var calculated = valueProvider(context, paramName, delimiter);

					if (string.IsNullOrEmpty(calculated) && !canBeOptional)
					{
						errorExpr = new SqlErrorExpression($"Non-optional parameter '{paramName}' not found", typeof(string));
						return "error";
					}

					var res = calculated;
					if (spaceNeeded)
					{
						if (!string.IsNullOrEmpty(calculated))
						{
							var e = expression;
							if (prevMatch == match.Index && prevNotEmptyMatch == match.Index - 3 || (prevNotEmptyMatch >= 0 && e[prevNotEmptyMatch] != ' '))
								res = " " + calculated;
						}

						spaceNeeded = false;
					}

					if (!string.IsNullOrEmpty(calculated))
					{
						prevNotEmptyMatch = match.Index + match.Length;
					}

					return res ?? string.Empty;
				});

				error = errorExpr;

				return str;
			}

			public static readonly ISqlExpression UnknownExpression = new SqlFragment("!!!");

			public static void PrepareParameterValues<TContext>(
				TContext                                                              context,
				MappingSchema                                                         mappingSchema,
				Expression                                                            expression,
				ref string?                                                           expressionStr,
				bool                                                                  includeInstance,
				out List<(Expression? expression, ExprParameterAttribute? parameter)> knownExpressions,
				bool                                                                  ignoreGenericParameters,
				bool                                                                  forceInlineParameters,
				out List<SqlDataType>?                                                genericTypes,
				ConvertFunc<TContext>                                                 converter)
			{
				knownExpressions = new List<(Expression?, ExprParameterAttribute?)>();
				genericTypes     = null;

				if (expression.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression) expression;
					expressionStr ??= mc.Method.Name;

					if (includeInstance && !mc.Method.IsStatic)
						knownExpressions.Add((mc.Object, null));

					ParameterInfo[]? pis = null;

					for (var i = 0; i < mc.Arguments.Count; i++)
					{
						var arg = mc.Arguments[i];

						pis ??= mc.Method.GetParameters();
						var p              = pis[i];
						var paramAttribute = p.GetAttribute<ExprParameterAttribute>();

						if (arg is NewArrayExpression nae)
						{
							if (p.HasAttribute<ParamArrayAttribute>())
							{
								foreach (var e in nae.Expressions)
								{
									knownExpressions.Add((e, paramAttribute));
								}
							}
							else
							{
								knownExpressions.Add((nae, paramAttribute));
							}
						}
						else
						{
							knownExpressions.Add((arg, paramAttribute));
						}
					}

					if (!ignoreGenericParameters)
					{
						ParameterInfo[]? pi = null;

						if (mc.Method.DeclaringType!.IsGenericType)
						{
							genericTypes = new List<SqlDataType>();
							foreach (var t in mc.Method.DeclaringType.GetGenericArguments())
							{
								var type = mappingSchema.GetDataType(t);
								if (type.Type.DataType == DataType.Undefined)
								{
									pi ??= mc.Method.GetParameters();
									for (var i = 0; i < pi.Length; i++)
									{
										if (pi[i].ParameterType == t)
										{
											var paramAttribute = pi[i].GetAttribute<ExprParameterAttribute>();

											var converted      = converter(context, mc.Arguments[i], null, forceInlineParameters || paramAttribute?.DoNotParameterize == true);
											if (converted is SqlPlaceholderExpression placeholder)
											{
												var dbType = QueryHelper.GetDbDataType(placeholder.Sql, mappingSchema);
												if (dbType.DataType != DataType.Undefined)
													type = new SqlDataType(dbType);
											}
										}
									}
								}

								genericTypes.Add(type);
							}
						}

						if (mc.Method.IsGenericMethod)
						{
							genericTypes ??= new List<SqlDataType>();
							foreach (var t in mc.Method.GetGenericArguments())
							{
								var type = mappingSchema.GetDataType(t);
								if (type.Type.DataType == DataType.Undefined)
								{
									pi ??= mc.Method.GetParameters();
									for (var i = 0; i < pi.Length; i++)
									{
										if (pi[i].ParameterType == t)
										{
											var paramAttribute = pi[i].GetAttribute<ExprParameterAttribute>();

											var converted = converter(context, mc.Arguments[i], null, forceInlineParameters || paramAttribute?.DoNotParameterize == true);
											if (converted is SqlPlaceholderExpression placeholder)
											{
												var dbType = QueryHelper.GetDbDataType(placeholder.Sql, mappingSchema);
												if (dbType.DataType != DataType.Undefined)
													type = new SqlDataType(dbType);
											}
										}
									}
								}

								genericTypes.Add(type);
							}
						}
					}
				}
				else
				{
					var me = (MemberExpression) expression;
					expressionStr ??= me.Member.Name;
					if (me.Expression != null)
						knownExpressions.Add((me.Expression, null));
				}
			}

			public delegate Expression ConvertFunc<TContext>(TContext context, Expression expression, ColumnDescriptor? columnDescriptor, bool? inlineParameters);

			public static ISqlExpression?[] PrepareArguments<TContext>(TContext   context,
				string                                                            expressionStr,
				int[]?                                                            argIndices,
				bool                                                              addDefault,
				List<(Expression? expression, ExprParameterAttribute? parameter)> knownExpressions,
				List<SqlDataType>?                                                genericTypes,
				ConvertFunc<TContext>                                             converter,
				bool                                                              forceInlineParameters,
				out Expression?                                                   error)
			{
				var parms = new List<ISqlExpression?>();
				var ctx   = WritableContext.Create((found: false, error: (Expression?)null), (context, expressionStr, argIndices, knownExpressions, genericTypes, converter, parms, forceInlineParameters));

				ResolveExpressionValues(
					ctx,
					expressionStr!,
					static (ctx, v, d) =>
					{
						ctx.WriteableValue = (true, ctx.WriteableValue.error);

						var idxInExpr   = int.Parse(v, NumberFormatInfo.InvariantInfo);
						var idxInMethod = idxInExpr;

						if (ctx.StaticValue.argIndices != null)
						{
							if (idxInMethod < 0 || idxInMethod >= ctx.StaticValue.argIndices.Length)
								throw new LinqToDBException(FormattableString.Invariant($"Expression '{ctx.StaticValue.expressionStr}' has wrong ArgIndices mapping. Index '{idxInMethod}' do not fit in range."));

							idxInMethod = ctx.StaticValue.argIndices[idxInMethod];
						}

						if (idxInMethod < 0)
							throw new LinqToDBException(FormattableString.Invariant($"Expression '{ctx.StaticValue.expressionStr}' has wrong param index mapping. Index '{idxInMethod}' do not fit in range."));

						while (idxInExpr >= ctx.StaticValue.parms.Count)
						{
							ctx.StaticValue.parms.Add(null);
						}

						if (ctx.StaticValue.parms[idxInExpr] == null)
						{
							ISqlExpression? paramExpr = null;
							if (idxInExpr >= ctx.StaticValue.knownExpressions.Count)
							{
								var typeIndex = idxInExpr - ctx.StaticValue.knownExpressions.Count;
								if (ctx.StaticValue.genericTypes == null || typeIndex >= ctx.StaticValue.genericTypes.Count || typeIndex < 0)
								{
									throw new LinqToDBException(FormattableString.Invariant($"Expression '{ctx.StaticValue.expressionStr}' has wrong param index mapping. Index '{idxInExpr}' do not fit in parameters range."));
								}

								paramExpr = ctx.StaticValue.genericTypes[typeIndex];
							}
							else
							{
								var (expression, parameter) = ctx.StaticValue.knownExpressions[idxInMethod];
								if (expression != null)
								{
									var converted = ctx.StaticValue.converter(ctx.StaticValue.context, expression, null, ctx.StaticValue.forceInlineParameters || parameter?.DoNotParameterize == true);
									if (converted is SqlPlaceholderExpression placeholder)
									{
										paramExpr = placeholder.Sql;
									}
									else
									{
										paramExpr          = null;
										ctx.WriteableValue = (true, converted);
									}
								}
							}

							ctx.StaticValue.parms[idxInExpr] = paramExpr;
						}

						return v;
					}, out error);

				if (error != null)
					return [];

				if (!ctx.WriteableValue.found)
				{
					// It means that we have to prepare parameters for function
					if (argIndices != null)
					{
						for (var idx = 0; idx < argIndices.Length; idx++)
						{
							var argIdx = argIndices[idx];

							while (idx >= parms.Count)
							{
								parms.Add(null);
							}

							if (parms[idx] == null)
							{
								ISqlExpression? paramExpr = null;
								if (argIdx >= knownExpressions.Count)
								{
									var typeIndex = argIdx - knownExpressions.Count;
									if (genericTypes == null || typeIndex >= genericTypes.Count || typeIndex < 0)
									{
										throw new LinqToDBException(FormattableString.Invariant($"Function '{expressionStr}' has wrong param index mapping. Index '{argIdx}' do not fit in parameters range."));
									}

									paramExpr = genericTypes[typeIndex];
								}
								else
								{
									var (expression, parameter) = knownExpressions[argIdx];
									if (expression != null)
									{
										var converted = converter(context, expression, null, ctx.StaticValue.forceInlineParameters || parameter?.DoNotParameterize == true);
										if (converted is SqlPlaceholderExpression placeholder)
										{
											paramExpr = placeholder.Sql;
										}
										else
										{
											// do not allow overriding first error
											if (ctx.WriteableValue.error == null)
												ctx.WriteableValue = (true, error);
											paramExpr = null;
										}
					
									}
								}

								parms[idx] = paramExpr;
							}
						}
					}
					else
					{
						if (addDefault)
						{
							foreach (var (expression, parameter) in knownExpressions)
							{
								if (expression == null)
									parms.Add(null);
								else
								{
									var converted = converter(context, expression, null, ctx.StaticValue.forceInlineParameters || parameter?.DoNotParameterize == true);

									if (converted is SqlPlaceholderExpression placeholder)
										parms.Add(placeholder.Sql);
									else
									{
										error = expression;
									}
								}
							}

							if (genericTypes != null)
								parms.AddRange(genericTypes);
						}
					}
				}

				if (ctx.WriteableValue.error != null)
				{
					error = ctx.WriteableValue.error;
					return parms.Select(static p => p).ToArray();
				}

				return parms.Select(static p => p ?? UnknownExpression).ToArray();
			}

			public virtual Expression GetExpression<TContext>(
				TContext              context,
				IDataContext          dataContext,
				IExpressionEvaluator  evaluator,
				SelectQuery           query,
				Expression            expression,
				ConvertFunc<TContext> converter)
			{
				var expressionStr = Expression;
				PrepareParameterValues(context, dataContext.MappingSchema, expression, ref expressionStr, true, out var knownExpressions, IgnoreGenericParameters, InlineParameters, out var genericTypes, converter);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve SQL Expression body from expression '{expression}'.");

				var parameters = PrepareArguments(context, expressionStr!, ArgIndices, false, knownExpressions, genericTypes, converter, InlineParameters, out var error);

				if (error != null)
					return SqlErrorExpression.EnsureError(error, expression.Type);

				var sqlExpression = new SqlExpression(dataContext.MappingSchema.GetDbDataType(expression.Type), expressionStr!, Precedence,
					(IsAggregate      ? SqlFlags.IsAggregate      : SqlFlags.None) |
					(IsPure           ? SqlFlags.IsPure           : SqlFlags.None) |
					(IsPredicate      ? SqlFlags.IsPredicate      : SqlFlags.None) |
					(IsWindowFunction ? SqlFlags.IsWindowFunction : SqlFlags.None),
					ToParametersNullabilityType(IsNullable),
					СonfiguredCanBeNull,
					parameters!);

				if (СonfiguredCanBeNull != null)
					sqlExpression.CanBeNull = СonfiguredCanBeNull.Value;

				// placeholder will be updated later by concrete path
				return ExpressionBuilder.CreatePlaceholder(query, sqlExpression, expression);
			}

			public static ParametersNullabilityType ToParametersNullabilityType(IsNullableType nullableType)
			{
				return (ParametersNullabilityType)nullableType;
			}

			public virtual bool GetIsPredicate(Expression expression) => IsPredicate;

			public override string GetObjectID()
			{
				return FormattableString.Invariant($".{Configuration}.{Expression}.{IdentifierBuilder.GetObjectID(ArgIndices)}.{Precedence}.{(ServerSideOnly ? 1 : 0)}.{(PreferServerSide ? 1 : 0)}.{(InlineParameters ? 1 : 0)}.{(ExpectExpression ? 1 : 0)}.{(IsPredicate ? 1 : 0)}.{(IsAggregate ? 1 : 0)}.{(IsWindowFunction ? 1 : 0)}.{(IsPure ? 1 : 0)}.{(int)IsNullable}.{(IgnoreGenericParameters ? 1 : 0)}.{(СonfiguredCanBeNull switch { null => -1, true => 1, _ => 0 })}.");
			}
		}
	}
}
