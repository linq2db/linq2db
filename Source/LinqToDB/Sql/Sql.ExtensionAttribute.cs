﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using LinqToDB.Mapping;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Common;
	using Expressions;
	using Extensions;
	using SqlQuery;

	[AttributeUsage(AttributeTargets.Parameter)]
	[MeansImplicitUse]
	public class ExprParameterAttribute : Attribute
	{
		public string? Name { get; set; }

		public ExprParameterAttribute(string name)
		{
			Name = name;
		}

		public ExprParameterAttribute()
		{
		}
	}

	public static class ExtensionlBuilderExtensions
	{
		public static Sql.SqlExtensionParam AddParameter(this Sql.ISqExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new SqlValue(value));
		}

		public static Sql.SqlExtensionParam AddExpression(this Sql.ISqExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new SqlExpression(expr, Precedence.Primary));
		}

		public static ISqlExpression Add(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "+", right, Precedence.Additive);
		}
		public static ISqlExpression Add<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Add(left, right, typeof(T));
		}

		public static ISqlExpression Add(this Sql.ISqExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Add<int>(left, new SqlValue(value));
		}

		public static ISqlExpression Inc(this Sql.ISqExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Add(expr, 1);
		}

		public static ISqlExpression Sub(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "-", right, Precedence.Subtraction);
		}

		public static ISqlExpression Sub<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Sub(left, right, typeof(T));
		}

		public static ISqlExpression Sub(this Sql.ISqExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Sub<int>(left, new SqlValue(value));
		}

		public static ISqlExpression Dec(this Sql.ISqExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Sub(expr, 1);
		}

		public static ISqlExpression Mul(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "*", right, Precedence.Multiplicative);
		}

		public static ISqlExpression Mul<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Mul(left, right, typeof(T));
		}

		public static ISqlExpression Mul(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Mul<int>(expr1, new SqlValue(value));
		}

		public static ISqlExpression Div(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative);
		}

		public static ISqlExpression Div<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2)
		{
			return builder.Div(expr1, expr2, typeof(T));
		}

		public static ISqlExpression Div(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Div<int>(expr1, new SqlValue(value));
		}
	}

	public partial class Sql
	{
		public interface ISqlExtension
		{
		}

		public static ISqlExtension? Ext => null;

		public interface IExtensionCallBuilder
		{
			void Build(ISqExtensionBuilder builder);
		}

		public interface IQueryableContainer
		{
			[EditorBrowsable(EditorBrowsableState.Never)]
			IQueryable Query { get; }
		}

		public interface ISqExtensionBuilder
		{
			string?         Configuration    { get; }
			object?         BuilderValue     { get; }
			IDataContext    DataContext      { get; }
			MappingSchema   Mapping          { get; }
			SelectQuery     Query            { get; }
			MemberInfo      Member           { get; }
			SqlExtension    Extension        { get; }
			ISqlExpression? ResultExpression { get; set; }
			string          Expression       { get; set; }
			Expression[]    Arguments        { get; }

			T GetValue<T>(int index);
			T GetValue<T>(string argName);

			ISqlExpression GetExpression(int index, bool unwrap = false);
			ISqlExpression GetExpression(string argName, bool unwrap = false);
			ISqlExpression ConvertToSqlExpression();
			ISqlExpression ConvertToSqlExpression(int precedence);
			ISqlExpression ConvertExpressionToSql(Expression expression, bool unwrap = false);

			SqlExtensionParam AddParameter(string name, ISqlExpression expr);
		}

		public class SqlExtension
		{
			public Dictionary<string, List<SqlExtensionParam>> NamedParameters { get; }

			public int ChainPrecedence { get; set; }

			public SqlExtension(Type? systemType, string expr, int precedence, int chainPrecedence, 
				bool isAggregate, 
				bool isWindowFunction,
				bool isPure, 
				bool isPredicate,
				bool? canBeNull, 
				params SqlExtensionParam[] parameters)
			{
				if (parameters == null) throw new ArgumentNullException(nameof(parameters));

				foreach (var value in parameters)
					if (value == null) throw new ArgumentNullException(nameof(parameters));

				SystemType       = systemType;
				Expr             = expr;
				Precedence       = precedence;
				ChainPrecedence  = chainPrecedence;
				IsAggregate      = isAggregate;
				IsWindowFunction = isWindowFunction;
				IsPure           = isPure;
				IsPredicate      = isPredicate;
				CanBeNull        = canBeNull;
				NamedParameters  = parameters.ToLookup(p => p.Name ?? string.Empty).ToDictionary(p => p.Key, p => p.ToList());
			}

			public SqlExtension(string expr, params SqlExtensionParam[] parameters)
				: this(null, expr, SqlQuery.Precedence.Unknown, 0, false, false, true, true, false, parameters)
			{
			}

			public Type?  SystemType       { get; set; }
			public string Expr             { get; set; }
			public int    Precedence       { get; set; }
			public bool   IsAggregate      { get; set; }
			public bool   IsWindowFunction { get; set; }
			public bool   IsPure           { get; set; }
			public bool   IsPredicate      { get; set; }
			public bool?  CanBeNull        { get; set; }

			public SqlExtensionParam AddParameter(string name, ISqlExpression sqlExpression)
			{
				return AddParameter(new SqlExtensionParam(name ?? string.Empty, sqlExpression));
			}

			public SqlExtensionParam AddParameter(SqlExtensionParam param)
			{
				var key = param.Name ?? string.Empty;

				if (!NamedParameters.TryGetValue(key, out var list))
				{
					list = new List<SqlExtensionParam>();
					NamedParameters.Add(key, list);
				}

				list.Add(param);
				return param;
			}

			public IEnumerable<SqlExtensionParam> GetParametersByName(string name)
			{
				if (NamedParameters.TryGetValue(name, out var list))
					return list;
				return Enumerable.Empty<SqlExtensionParam>();
			}

			public SqlExtensionParam[] GetParameters()
			{
				return NamedParameters.Values.SelectMany(_ => _).ToArray();
			}
		}

		[DebuggerDisplay("{ToDebugString()}")]
		public class SqlExtensionParam
		{
#if DEBUG
			private static int _paramCounter;
			private readonly int _paramNumber;
			public int ParamNumber => _paramNumber;
#endif

			public SqlExtensionParam(string? name, ISqlExpression expression)
			{
				Name       = name;
				Expression = expression;
#if DEBUG
				_paramNumber = Interlocked.Add(ref _paramCounter, 1);
#endif
			}

			public SqlExtensionParam(string? name, SqlExtension extension)
			{
				Name      = name;
				Extension = extension;
#if DEBUG
				_paramNumber = Interlocked.Add(ref _paramCounter, 1);
#endif
			}

			public string ToDebugString()
			{
				string str;

#if DEBUG
				var paramPrefix = $"Param[{ParamNumber}]";
#else
				var paramPrefix = $"Param";
#endif

				if (Extension != null)
				{
					str = $"{paramPrefix}('{Name ?? ""}', {Extension.ChainPrecedence}): {Extension.Expr}";
				}
				else if (Expression != null)
				{
					var sb = new StringBuilder();
					Expression.ToString(sb, new Dictionary<IQueryElement, IQueryElement>());
					str = $"{paramPrefix}('{Name ?? ""}'): {sb}";
				}
				else
					str = $"{paramPrefix}('{Name ?? ""}')";

				return str;
			}

			public string?         Name       { get; set; }
			public SqlExtension?   Extension  { get; set; }
			public ISqlExpression? Expression { get; set; }
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class ExtensionAttribute : ExpressionAttribute
		{
			private static readonly ConcurrentDictionary<Type, IExtensionCallBuilder> _builders = new ();

			public string? TokenName { get; set; }

			protected class ExtensionBuilder: ISqExtensionBuilder
			{
				readonly ConvertHelper _convert;

				public ExtensionBuilder(
					string?       configuration,
					object?       builderValue,
					IDataContext  dataContext,
					SelectQuery   query,
					SqlExtension  extension,
					ConvertHelper convertHeper,
					MemberInfo    member,
					Expression[]  arguments)
				{
					Configuration = configuration;
					BuilderValue  = builderValue;
					DataContext   = dataContext  ?? throw new ArgumentNullException(nameof(dataContext));
					Query         = query        ?? throw new ArgumentNullException(nameof(query));
					Extension     = extension    ?? throw new ArgumentNullException(nameof(extension));
					_convert      = convertHeper ?? throw new ArgumentNullException(nameof(convertHeper));
					Member        = member;
					Method        = member as MethodInfo;
					Arguments     = arguments    ?? throw new ArgumentNullException(nameof(arguments));
				}

				public MethodInfo?  Method { get; }

				public ISqlExpression ConvertExpression(Expression expr, bool unwrap, ColumnDescriptor? columnDescriptor)
				{
					if (unwrap)
						expr = expr.UnwrapConvert();

					return _convert.Convert(expr, columnDescriptor);
				}

				#region ISqExtensionBuilder Members

				public string?         Configuration    { get; }
				public object?         BuilderValue     { get; }
				public IDataContext    DataContext      { get; }
				public MappingSchema   Mapping          => DataContext.MappingSchema;
				public SelectQuery     Query            { get; }
				public MemberInfo      Member           { get; }
				public SqlExtension    Extension        { get; }
				public ISqlExpression? ResultExpression { get; set; }
				public Expression[]    Arguments        { get; }

				public string Expression
				{
					get => Extension.Expr;
					set => Extension.Expr = value;
				}

				public T GetValue<T>(int index)
				{
					var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(Arguments[index]);
					return lambda.CompileExpression()();
				}

				public T GetValue<T>(string argName)
				{
					if (Method != null)
					{
						var parameters = Method.GetParameters();
						for (int i = 0; i < parameters.Length; i++)
						{
							if (parameters[i].Name == argName)
							{
								return GetValue<T>(i);
							}
						}
					}

					throw new InvalidOperationException(string.Format("Argument '{0}' not found", argName));
				}

				public ISqlExpression GetExpression(int index, bool unwrap)
				{
					return ConvertExpression(Arguments[index], unwrap, null);
				}

				public ISqlExpression GetExpression(string argName, bool unwrap)
				{
					if (Method != null)
					{
						var parameters = Method.GetParameters();
						for (int i = 0; i < parameters.Length; i++)
						{
							if (parameters[i].Name == argName)
							{
								return GetExpression(i, unwrap);
							}
						}
					}

					throw new InvalidOperationException(string.Format("Argument '{0}' not found", argName));
				}

				public ISqlExpression ConvertToSqlExpression()
				{
					return ConvertToSqlExpression(Extension.Precedence);
				}

				public ISqlExpression ConvertToSqlExpression(int precedence)
				{
					return BuildSqlExpression(Extension, Extension.SystemType, precedence,
						(Extension.IsAggregate      ? SqlFlags.IsAggregate      : SqlFlags.None) |
						(Extension.IsPure           ? SqlFlags.IsPure           : SqlFlags.None) |
						(Extension.IsPredicate      ? SqlFlags.IsPredicate      : SqlFlags.None) | 
						(Extension.IsWindowFunction ? SqlFlags.IsWindowFunction : SqlFlags.None), 
						Extension.CanBeNull, IsNullableType.Undefined);
				}

				public ISqlExpression ConvertExpressionToSql(Expression expression, bool unwrap)
				{
					return ConvertExpression(expression, unwrap, null);
				}

				public SqlExtensionParam AddParameter(string name, ISqlExpression expr)
				{
					return Extension.AddParameter(name, expr);
				}

				#endregion

			}

			public Type?     BuilderType     { get; set; }
			public object?   BuilderValue    { get; set; }

			/// <summary>
			/// Defines in which order process extensions. Items will be ordered Descending.
			/// </summary>
			public int       ChainPrecedence { get; set; }

			public ExtensionAttribute(string expression): this(string.Empty, expression)
			{
			}

			public ExtensionAttribute(string configuration, string expression) : base(configuration, expression)
			{
				ExpectExpression = true;
				ServerSideOnly   = true;
				PreferServerSide = true;
				ChainPrecedence  = -1;
			}

			public ExtensionAttribute(Type builderType): this(string.Empty, builderType)
			{
			}

			public ExtensionAttribute(string configuration, Type builderType) : this(configuration, string.Empty)
			{
				BuilderType = builderType;
			}

			static T GetExpressionValue<T>(Expression expr)
			{
				var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(expr);
				return lambda.CompileExpression()();
			}

			public static ExtensionAttribute[] GetExtensionAttributes(Expression expression, MappingSchema mapping)
			{
				MemberInfo memberInfo;

				switch (expression.NodeType)
				{
					case ExpressionType.MemberAccess:
						memberInfo = ((MemberExpression) expression).Member;
						break;
					case ExpressionType.Call:
						memberInfo = ((MethodCallExpression) expression).Method;
						break;
					default:
						return Array<ExtensionAttribute>.Empty;
				}

				var attributes =
						mapping.GetAttributes<ExtensionAttribute>(memberInfo.ReflectedType!, memberInfo,
							a => a.Configuration, inherit: true, exactForConfiguration: true);

				if (attributes.Length == 0)
				{
					// notify if there is method that has no defined attribute for specific configuration
					attributes = mapping.GetAttributes<ExtensionAttribute>(memberInfo.ReflectedType!, memberInfo);
					if (attributes.Length > 0)
						throw new LinqToDBException($"Expression {expression}, unsupported for configuration(s) '{string.Join(", ", mapping.ConfigurationList)}'.");
				}

				return attributes;
			}

			public static Expression ExcludeExtensionChain(MappingSchema mapping, Expression expr)
			{
				var current = expr;

				while (true)
				{
					var attributes = GetExtensionAttributes(current, mapping);

					if (attributes.Length == 0)
						break;

					switch (current.NodeType)
					{
						case ExpressionType.MemberAccess :
							{
								var memberExpr = (MemberExpression)current;
								current        = memberExpr.Expression;

								break;
							}

						case ExpressionType.Call :
							{
								var call = (MethodCallExpression) current;

								if (call.Method.IsStatic)
								{
									if (call.Arguments.Count > 0)
										current = call.Arguments[0];
									else
										return current;
								}
								else
									current = call.Object;

								break;
							}
						default:
							{
								return current;
							}
					}
				}

				return current;
			}

			protected List<SqlExtensionParam> BuildFunctionsChain(IDataContext dataContext, SelectQuery query, Expression expr, ConvertHelper convertHelper)
			{
				var chains           = new List<SqlExtensionParam>();
				Expression? current  = expr;

				while (current != null)
				{
					MemberInfo?   memberInfo = null;
					Expression[]? arguments  = null;
					Expression?   next       = null;

					switch (current.NodeType)
					{
						case ExpressionType.MemberAccess :
							{
								var memberExpr = (MemberExpression)current;

								memberInfo = memberExpr.Member;
								arguments  = Array<Expression>.Empty;
								next       = memberExpr.Expression;

								break;
							}

						case ExpressionType.Call :
							{
								var call = (MethodCallExpression) current;

								memberInfo = call.Method;
								arguments  = call.Arguments.ToArray();

								if (call.Method.IsStatic)
									next = call.Arguments.FirstOrDefault();
								else
									next = call.Object;

								break;
							}

						case ExpressionType.Constant:
							{
								if (typeof(IQueryableContainer).IsSameOrParentOf(current.Type))
								{
									next = ((IQueryableContainer)current.EvaluateExpression()!).Query.Expression;
								}
								break;
							}
					}

					if (memberInfo != null)
					{
						var attributes = GetExtensionAttributes(current, dataContext.MappingSchema);
						var continueChain = false;

						foreach (var attr in attributes)
						{
							var param = attr.BuildExtensionParam(dataContext, query, memberInfo, arguments!, convertHelper);
							continueChain = continueChain || !string.IsNullOrEmpty(param.Name) ||
							                param.Extension != null && param.Extension.ChainPrecedence != -1;
							chains.Add(param);
						}

						if (!continueChain)
							break;
					}

					current = next;
				}

				return chains;
			}

			SqlExtensionParam BuildExtensionParam(IDataContext dataContext, SelectQuery query, MemberInfo member, Expression[] arguments, ConvertHelper convertHelper)
			{
				var method = member as MethodInfo;
				var type   = member.GetMemberType();
				if (method != null)
					type = method.ReturnType ?? type;
				else if (member is PropertyInfo)
					type = ((PropertyInfo)member).PropertyType;

				var extension = new SqlExtension(type, Expression!, Precedence, ChainPrecedence, IsAggregate, IsWindowFunction, IsPure, IsPredicate, _canBeNull);

				SqlExtensionParam? result = null;

				if (method != null)
				{
					var parameters = method.GetParameters();

					var genericDefinition        = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
					var templateParameters       = genericDefinition.GetParameters();
					var templateGenericArguments = genericDefinition.GetGenericArguments();
					var descriptorMapping        = new Dictionary<Type, ColumnDescriptor?>();

					for (var i = 0; i < parameters.Length; i++)
					{
						var arg   = arguments[i];
						var param = parameters[i];
						var names = param.GetCustomAttributes(true).OfType<ExprParameterAttribute>()
							.Select(a => a.Name ?? param.Name)
							.Distinct()
							.ToArray()!;

						ColumnDescriptor? descriptor;
						if (names.Length > 0)
						{
							if (method.IsGenericMethod)
							{
								var templateParam  = templateParameters[i];
								var elementType    = templateParam.ParameterType!;
								var argElementType = param.ParameterType;
								if (elementType.IsArray && argElementType.IsArray)
								{
									elementType    = elementType.GetElementType()!;
									argElementType = argElementType.GetElementType()!;
								}
								descriptorMapping.TryGetValue(elementType, out descriptor);

								ISqlExpression[] sqlExpressions;
								if (arg is NewArrayExpression arrayInit)
								{
									sqlExpressions = arrayInit.Expressions.Select(e => convertHelper.Convert(e, descriptor)).ToArray();
								}	
								else
								{
									var sqlExpression = convertHelper.Convert(arg, descriptor);
									sqlExpressions = new[] { sqlExpression };
								}


								if (descriptor == null)
								{
									descriptor = sqlExpressions.Select(e => QueryHelper.GetColumnDescriptor(e)).FirstOrDefault(d => d != null);
									if (descriptor != null)
									{
										foreach (var pair
											in TypeHelper.EnumTypeRemapping(elementType, argElementType, templateGenericArguments))
										{
											if (!descriptorMapping.ContainsKey(pair.Item1))
												descriptorMapping.Add(pair.Item1, descriptor);
										}
									}
								}

								foreach (var name in names)
								{
									foreach (var sqlExpr in sqlExpressions)
									{
										extension.AddParameter(name!, sqlExpr);
									}
								}
							}
							else
							{
								var sqlExpressions = arg is NewArrayExpression arrayInit
									? arrayInit.Expressions.Select(e => convertHelper.Convert(e, null)).ToArray()
									: new[] { convertHelper.Convert(arg, null) };

								foreach (var name in names)
								{
									foreach (var sqlExpr in sqlExpressions)
									{
										extension.AddParameter(name!, sqlExpr);
									}
								}
							}
						}
					}
				}

				if (BuilderType != null)
				{
					var callBuilder = _builders.GetOrAdd(BuilderType, t =>
						{
							if (Activator.CreateInstance(BuilderType)! is IExtensionCallBuilder res)
								return res;

							throw new ArgumentException(
								$"Type '{BuilderType}' does not implement {nameof(IExtensionCallBuilder)} interface.");
						}
					);

					var builder = new ExtensionBuilder(Configuration, BuilderValue, dataContext, query, extension, convertHelper, member, arguments);
					callBuilder.Build(builder);

					result = builder.ResultExpression != null ?
						new SqlExtensionParam(TokenName, builder.ResultExpression) :
						new SqlExtensionParam(TokenName, builder.Extension);
				}

				if (!extension.CanBeNull.HasValue)
					extension.CanBeNull = CalcCanBeNull(IsNullable,
						extension.GetParameters().Select(p => p.Expression?.CanBeNull ?? p.Extension?.CanBeNull ?? true));

				result ??= new SqlExtensionParam(TokenName, extension);

				return result;
			}

			static IEnumerable<Expression> ExtractArray(Expression expression)
			{
				var array = (NewArrayExpression) expression;
				return array.Expressions;
			}

			protected class ConvertHelper
			{
				readonly Func<Expression, ColumnDescriptor?, ISqlExpression> _converter;

				public ConvertHelper(Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
				{
					_converter = converter ?? throw new ArgumentNullException(nameof(converter));
				}

				public ISqlExpression Convert(Expression exp, ColumnDescriptor? descriptor)
				{
					return _converter(exp, descriptor);
				}
			}

			public static SqlExpression BuildSqlExpression(SqlExtension root, Type? systemType, int precedence,
				SqlFlags flags, bool? canBeNull, IsNullableType isNullable)
			{
				var sb             = new StringBuilder();
				var resolvedParams = new Dictionary<SqlExtensionParam, string?>();
				var resolving      = new HashSet<SqlExtensionParam>();
				var newParams      = new List<ISqlExpression>();

				Func<string, string?, string?>? valueProvider = null;
				Stack<SqlExtension> current                   = new Stack<SqlExtension>();

				valueProvider = (name, delimiter) =>
				{
					var found = root.GetParametersByName(name);
					if (current.Count != 0)
						found = current.Peek().GetParametersByName(name).Concat(found);
					string? result = null;
					foreach (var p in found)
					{
						if (resolvedParams.TryGetValue(p, out var paramValue))
						{
							result = paramValue;
						}
						else
						{
							if (resolving.Contains(p))
								throw new InvalidOperationException("Circular reference");

							resolving.Add(p);
							var ext = p.Extension;
							if (ext != null)
							{
								current.Push(ext);
								paramValue = ResolveExpressionValues(ext.Expr, valueProvider!);
								current.Pop();
							}
							else
							{
								sb.Length = 0;
								if (p.Expression != null)
								{
									paramValue = string.Format("{{{0}}}", newParams.Count);
									newParams.Add(p.Expression);
								}
							}

							resolvedParams.Add(p, paramValue);

							if (string.IsNullOrEmpty(paramValue))
								continue;

							if (!string.IsNullOrEmpty(result))
								result += delimiter;
							result += paramValue;
						}

						if (delimiter == null && !string.IsNullOrEmpty(result))
							break;
					}

					return result;
				};

				var expr          = ResolveExpressionValues(root.Expr, valueProvider);
				var sqlExpression = new SqlExpression(systemType, expr, precedence, flags, newParams.ToArray());
				if (!canBeNull.HasValue)
					canBeNull = root.CanBeNull;

				if (!canBeNull.HasValue)
					canBeNull = CalcCanBeNull(isNullable, sqlExpression.Parameters.Select(p => p.CanBeNull));

				if (canBeNull.HasValue)
					sqlExpression.CanBeNull = canBeNull.Value;

				return sqlExpression;
			}

			public override ISqlExpression GetExpression(IDataContext dataContext, SelectQuery query, Expression expression, Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				var helper = new ConvertHelper(converter);

				// chain starts from the tail
				var chain  = BuildFunctionsChain(dataContext, query, expression, helper);

				if (chain.Count == 0)
					throw new InvalidOperationException("No sequence found for expression '{expression}'");

				var ordered = chain
					.Select((c, i) => Tuple.Create(c, i))
					.OrderByDescending(t => t.Item1.Extension?.ChainPrecedence ?? int.MinValue)
					.ThenByDescending(t => t.Item2)
					.Select(t => t.Item1)
					.ToArray();

				var main    = ordered.FirstOrDefault(c => c.Extension != null);

				if (main == null)
				{
					var replaced = chain.Where(c => c.Expression != null).ToArray();
					if (replaced.Length == 0)
						throw new InvalidOperationException($"Can not find root sequence for expression '{expression}'");
					else if (replaced.Length > 1)
						throw new InvalidOperationException($"Multiple root sequences found for expression '{expression}'");

					return replaced[0].Expression!;
				}

				var mainExtension = main.Extension!;

				// suggesting type
				var type = ordered
					.Select(c => c.Extension?.SystemType)
					.FirstOrDefault(t => t != null && dataContext.MappingSchema.IsScalarType(t));

				if (type == null)
					type = ordered
						.Select(c => c.Expression?.SystemType)
						.FirstOrDefault(t => t != null && dataContext.MappingSchema.IsScalarType(t));

				mainExtension.SystemType = type ?? expression.Type;

				// calculating that extension is aggregate
				var isAggregate  = ordered.Any(c => c.Extension?.IsAggregate == true);
				var isPure       = ordered.All(c => c.Extension?.IsPure != false);
				var isWindowFunc = ordered.Any(c => c.Extension?.IsWindowFunction == true);
				var isPredicate  = mainExtension.IsPredicate;

				// calculating replacements
				var replacementMap = ordered
					.Where(c => c.Extension != mainExtension)
					.Select((c, i) => Tuple.Create(c, i))
					.GroupBy(e => e.Item1.Name ?? "")
					.Select(g => new
					{
						Name = g.Key,
						UnderName = g
							.OrderByDescending(e => e.Item1.Extension?.ChainPrecedence ?? int.MinValue)
							.ThenBy(e => e.Item2)
							.Select(e => e.Item1)
							.ToArray()
					})
					.ToArray();

				foreach (var c in replacementMap)
				{
					var first = c.UnderName[0];
					if (c.Name == "" || first.Extension == null)
					{
						for (var i = 0; i < c.UnderName.Length; i++)
						{
							var e = c.UnderName[i];
							mainExtension.AddParameter(e);
						}
					}	
					else
					{
						var firstPrecedence = first.Extension.ChainPrecedence;
						mainExtension.AddParameter(first);
						// append all replaced under superior
						for (int i = 1; i < c.UnderName.Length; i++)
						{
							var item = c.UnderName[i];
							if (firstPrecedence > (item.Extension?.ChainPrecedence ?? int.MinValue))
								first.Extension.AddParameter(item);
							else
								mainExtension.AddParameter(item);
						}
					}
				}

				//TODO: Precedence calculation
				var res = BuildSqlExpression(mainExtension, mainExtension.SystemType, 
					mainExtension.Precedence,
					(isAggregate  ? SqlFlags.IsAggregate      : SqlFlags.None) |
					(isPure       ? SqlFlags.IsPure           : SqlFlags.None) |
					(isPredicate  ? SqlFlags.IsPredicate      : SqlFlags.None) |
					(isWindowFunc ? SqlFlags.IsWindowFunction : SqlFlags.None),
					mainExtension.CanBeNull, IsNullable);

				return res;
			}
		}
	}
}
