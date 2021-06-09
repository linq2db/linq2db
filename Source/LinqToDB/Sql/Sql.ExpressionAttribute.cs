using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace LinqToDB
{
	using Mapping;
	using SqlQuery;

	
	partial class Sql
	{
		/// <summary>
		/// An Attribute that allows custom Expressions to be defined
		/// for a Method used within a Linq Expression. 
		/// </summary>
		[PublicAPI]
		[Serializable]
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
		public class ExpressionAttribute : Attribute
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
			/// Refer to <see cref="LinqToDB.SqlQuery.Precedence"/>.
			/// </summary>
			public int            Precedence       { get; set; }
			/// <summary>
			/// If <c>null</c>, this will be treated as the default
			/// evaluation for the expression. If set to a <see cref="ProviderName"/>,
			/// It will only be used for that provider configuration.
			/// </summary>
			public string?        Configuration    { get; set; }
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
			/// But expression <see cref="Sql.CurrentTimestamp"/> is Pure in case of executed query.
			/// <see cref="Sql.DateAdd(LinqToDB.Sql.DateParts,System.Nullable{double},System.Nullable{System.DateTime})"/> is also Pure function because it returns the same result with the same parameters.  
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

			internal  bool? _canBeNull;
			/// <summary>
			/// If <c>true</c>, result can be null
			/// </summary>
			public    bool   CanBeNull
			{
				get => _canBeNull ?? true;
				set => _canBeNull = value;
			}

			protected bool GetCanBeNull(ISqlExpression[] parameters)
			{
				if (_canBeNull != null)
					return _canBeNull.Value;

				return CalcCanBeNull(IsNullable, parameters.Select(p => p.CanBeNull)) ?? true;
			}

			public static bool? CalcCanBeNull(IsNullableType isNullable, IEnumerable<bool> nullInfo)
			{
				switch (isNullable)
				{
					case IsNullableType.Undefined              : return null;
					case IsNullableType.Nullable               : return true;
					case IsNullableType.NotNullable            : return false;
				}

				var parameters = nullInfo.ToArray();

				switch (isNullable)
				{
					case IsNullableType.SameAsFirstParameter   : return SameAs(0);
					case IsNullableType.SameAsSecondParameter  : return SameAs(1);
					case IsNullableType.SameAsThirdParameter   : return SameAs(2);
					case IsNullableType.SameAsLastParameter    : return SameAs(parameters.Length - 1);
					case IsNullableType.IfAnyParameterNullable : return parameters.Any(p => p);
				}

				bool SameAs(int parameterNumber)
				{
					if (parameterNumber >= 0 && parameters.Length > parameterNumber)
						return parameters[parameterNumber];
					return true;
				}

				return null;
			}

			const  string MatchParamPattern = @"{([0-9a-z_A-Z?]*)(,\s'(.*)')?}";
			static Regex  _matchParamRegEx  = new Regex(MatchParamPattern, RegexOptions.Compiled);

			public static string ResolveExpressionValues(string expression, Func<string, string?, string?> valueProvider)
			{
				if (expression    == null) throw new ArgumentNullException(nameof(expression));
				if (valueProvider == null) throw new ArgumentNullException(nameof(valueProvider));

				int  prevMatch         = -1;
				int  prevNotEmptyMatch = -1;
				bool spaceNeeded       = false;

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
					var calculated = valueProvider(paramName, delimiter);

					if (string.IsNullOrEmpty(calculated) && !canBeOptional)
						throw new InvalidOperationException($"Non optional parameter '{paramName}' not found");

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

					return res;
				});

				return str;
			}

			public static readonly SqlExpression UnknownExpression = new SqlExpression("!!!");

			public static void PrepareParameterValues(Expression expression, ref string? expressionStr, bool includeInstance, out List<Expression?> knownExpressions, out List<ISqlExpression>? genericTypes)
			{
				knownExpressions = new List<Expression?>();
				genericTypes     = null;

				if (expression.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression) expression;
					expressionStr ??= mc.Method.Name;

					if (includeInstance && !mc.Method.IsStatic)
						knownExpressions.Add(mc.Object);

					ParameterInfo[]? pis = null;

					for (var i = 0; i < mc.Arguments.Count; i++)
					{
						var arg = mc.Arguments[i];

						if (arg is NewArrayExpression nae)
						{
							if (pis == null)
								pis = mc.Method.GetParameters();

							var p = pis[i];

							if (p.GetCustomAttributes(true).OfType<ParamArrayAttribute>().Any())
							{
								knownExpressions.AddRange(nae.Expressions);
							}
							else
							{
								knownExpressions.Add(nae);
							}
						}
						else
						{
							knownExpressions.Add(arg);
						}
					}

					if (mc.Method.DeclaringType!.IsGenericType)
					{
						genericTypes ??= new List<ISqlExpression>();
						genericTypes.AddRange(mc.Method.DeclaringType.GetGenericArguments()
							.Select(t => (ISqlExpression)SqlDataType.GetDataType(t)));
					}

					if (mc.Method.IsGenericMethod)
					{
						genericTypes ??= new List<ISqlExpression>();
						genericTypes.AddRange(mc.Method.GetGenericArguments()
							.Select(t => (ISqlExpression)SqlDataType.GetDataType(t)));
					}
				}
				else
				{
					var me = (MemberExpression) expression;
					expressionStr ??= me.Member.Name;
					if (me.Expression != null)
						knownExpressions.Add(me.Expression);
				}
			}

			public static ISqlExpression[] PrepareArguments(string expressionStr, int[]? argIndices, bool addDefault, List<Expression?> knownExpressions, List<ISqlExpression>? genericTypes, Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				var parms = new List<ISqlExpression?>();

				var foundPosition = false;
				_ = ResolveExpressionValues(expressionStr!,
					(v, d) =>
					{
						foundPosition = true;

						var argIdx = int.Parse(v);
						var idx    = argIdx;

						if (argIndices != null)
						{
							if (idx < 0 || idx >= argIndices.Length)
								throw new LinqToDBException($"Expression '{expressionStr}' has wrong ArgIndices mapping. Index '{idx}' do not fit in range.");

							idx = argIndices[idx];
						}

						if (idx < 0)
							throw new LinqToDBException($"Expression '{expressionStr}' has wrong param index mapping. Index '{idx}' do not fit in range.");

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
									throw new LinqToDBException($"Expression '{expressionStr}' has wrong param index mapping. Index '{argIdx}' do not fit in parameters range.");
								}

								paramExpr = genericTypes[typeIndex];
							}
							else
							{
								var expr = knownExpressions[argIdx];
								if (expr != null)
									paramExpr = converter(expr, null);
							}

							parms[idx] = paramExpr;
						}

						return v;
					});

				if (!foundPosition)
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
										throw new LinqToDBException($"Function '{expressionStr}' has wrong param index mapping. Index '{argIdx}' do not fit in parameters range.");
									}

									paramExpr = genericTypes[typeIndex];
								}
								else
								{
									var expr = knownExpressions[argIdx];
									if (expr != null)
										paramExpr = converter(expr, null);
								}

								parms[idx] = paramExpr;
							}
						}
					}
					else
					{
						if (addDefault)
						{
							parms.AddRange(knownExpressions.Select(e => e == null ? null : converter(e, null)));
							if (genericTypes != null)
								parms.AddRange(genericTypes);
						}
					}
				}

				return parms.Select(p => p ?? UnknownExpression).ToArray();
			}

			public virtual ISqlExpression? GetExpression(IDataContext dataContext, SelectQuery query,
				Expression expression, Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				var expressionStr = Expression;
				PrepareParameterValues(expression, ref expressionStr, true, out var knownExpressions, out var genericTypes);

				if (string.IsNullOrEmpty(expressionStr))
					throw new LinqToDBException($"Cannot retrieve SQL Expression body from expression '{expression}'.");

				var parameters = PrepareArguments(expressionStr!, ArgIndices, addDefault: false, knownExpressions, genericTypes, converter);

				return new SqlExpression(expression.Type, expressionStr!, Precedence,
					(IsAggregate      ? SqlFlags.IsAggregate      : SqlFlags.None) | 
					(IsPure           ? SqlFlags.IsPure           : SqlFlags.None) |
					(IsPredicate      ? SqlFlags.IsPredicate      : SqlFlags.None) | 
					(IsWindowFunction ? SqlFlags.IsWindowFunction : SqlFlags.None), 
					parameters)
				{
					CanBeNull = GetCanBeNull(parameters)
				};
			}

			public virtual bool GetIsPredicate(Expression expression) => IsPredicate;
		}
	}
}
