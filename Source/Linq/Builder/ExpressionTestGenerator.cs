﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using LinqToDB.Expressions;

	class ExpressionTestGenerator
	{
		readonly bool          _encryptNames;
		readonly StringBuilder _exprBuilder = new StringBuilder();

		string _indent = "\t\t\t\t";

		public ExpressionTestGenerator(): this(true)
		{
		}

		public ExpressionTestGenerator(bool encryptNames)
		{
			_encryptNames = encryptNames;
		}

		void PushIndent() { _indent += '\t'; }
		void PopIndent () { _indent = _indent.Substring(1); }

		readonly HashSet<Expression> _visitedExprs = new HashSet<Expression>();

		bool BuildExpression(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Assign:
				case ExpressionType.Coalesce:
				case ExpressionType.Divide:
				case ExpressionType.Equal:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.NotEqual:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.Power:
				case ExpressionType.RightShift:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
					{
						var e = (BinaryExpression)expr;

						_exprBuilder.Append("(");

						e.Left.Visit(new Func<Expression,bool>(BuildExpression));

						switch (expr.NodeType)
						{
							case ExpressionType.Add                :
							case ExpressionType.AddChecked         : _exprBuilder.Append(" + ");  break;
							case ExpressionType.And                : _exprBuilder.Append(" & ");  break;
							case ExpressionType.AndAlso            : _exprBuilder.Append(" && "); break;
							case ExpressionType.Assign             : _exprBuilder.Append(" = ");  break;
							case ExpressionType.Coalesce           : _exprBuilder.Append(" ?? "); break;
							case ExpressionType.Divide             : _exprBuilder.Append(" / ");  break;
							case ExpressionType.Equal              : _exprBuilder.Append(" == "); break;
							case ExpressionType.ExclusiveOr        : _exprBuilder.Append(" ^ ");  break;
							case ExpressionType.GreaterThan        : _exprBuilder.Append(" > ");  break;
							case ExpressionType.GreaterThanOrEqual : _exprBuilder.Append(" >= "); break;
							case ExpressionType.LeftShift          : _exprBuilder.Append(" << "); break;
							case ExpressionType.LessThan           : _exprBuilder.Append(" < ");  break;
							case ExpressionType.LessThanOrEqual    : _exprBuilder.Append(" <= "); break;
							case ExpressionType.Modulo             : _exprBuilder.Append(" % ");  break;
							case ExpressionType.Multiply           :
							case ExpressionType.MultiplyChecked    : _exprBuilder.Append(" * ");  break;
							case ExpressionType.NotEqual           : _exprBuilder.Append(" != "); break;
							case ExpressionType.Or                 : _exprBuilder.Append(" | ");  break;
							case ExpressionType.OrElse             : _exprBuilder.Append(" || "); break;
							case ExpressionType.Power              : _exprBuilder.Append(" ** "); break;
							case ExpressionType.RightShift         : _exprBuilder.Append(" >> "); break;
							case ExpressionType.Subtract           :
							case ExpressionType.SubtractChecked    : _exprBuilder.Append(" - ");  break;
						}

						e.Right.Visit(new Func<Expression,bool>(BuildExpression));

						_exprBuilder.Append(")");

						return false;
					}

				case ExpressionType.ArrayLength:
					{
						var e = (UnaryExpression)expr;

						e.Operand.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.Append(".Length");

						return false;
					}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					{
						var e = (UnaryExpression)expr;

						_exprBuilder.AppendFormat("({0})", GetTypeName(e.Type));
						e.Operand.Visit(new Func<Expression,bool>(BuildExpression));

						return false;
					}

				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
					{
						_exprBuilder.Append("-");
						return true;
					}

				case ExpressionType.Not:
					{
						_exprBuilder.Append("!");
						return true;
					}

				case ExpressionType.Quote:
					return true;

				case ExpressionType.TypeAs:
					{
						var e = (UnaryExpression)expr;

						_exprBuilder.Append("(");
						e.Operand.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.AppendFormat(" as {0})", GetTypeName(e.Type));

						return false;
					}

				case ExpressionType.UnaryPlus:
					{
						_exprBuilder.Append("+");
						return true;
					}

				case ExpressionType.ArrayIndex:
					{
						var e = (BinaryExpression)expr;

						e.Left.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.Append("[");
						e.Right.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.Append("]");

						return false;
					}

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expr;

						e.Expression.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.AppendFormat(".{0}", EncryptName(e.Member.DeclaringType, e.Member.Name, "P"));

						return false;
					}

				case ExpressionType.Parameter :
					{
						var e = (ParameterExpression)expr;
						_exprBuilder.Append(EncryptName(e.Name, "p"));
						return false;
					}

				case ExpressionType.Call :
					{
						var ex = (MethodCallExpression)expr;
						var mi = ex.Method;

						var attrs = mi.GetCustomAttributesEx(typeof(ExtensionAttribute), false);

						if (attrs.Length != 0)
						{
							ex.Arguments[0].Visit(new Func<Expression,bool>(BuildExpression));
							PushIndent();
							_exprBuilder.AppendLine().Append(_indent);
						}
						else if (ex.Object != null)
							ex.Object.Visit(new Func<Expression,bool>(BuildExpression));
						else
							_exprBuilder.Append(GetTypeName(mi.DeclaringType));

						_exprBuilder.Append(".").Append(EncryptName(mi.DeclaringType, mi.Name, "M"));

						if (!ex.IsQueryable() && mi.IsGenericMethod && mi.GetGenericArguments().Select(GetTypeName).All(t => t != null))
						{
							_exprBuilder
								.Append("<")
								.Append(GetTypeNames(mi.GetGenericArguments(), ","))
								.Append(">");
						}

						_exprBuilder.Append("(");

						PushIndent();

						var n = attrs.Length != 0 ? 1 : 0;

						for (var i = n; i < ex.Arguments.Count; i++)
						{
							if (i != n)
								_exprBuilder.Append(",");

							_exprBuilder.AppendLine().Append(_indent);

							ex.Arguments[i].Visit(new Func<Expression,bool>(BuildExpression));
						}

						PopIndent();

						_exprBuilder.Append(")");

						if (attrs.Length != 0)
						{
							PopIndent();
						}

						return false;
					}

				case ExpressionType.Constant:
					{
						var c = (ConstantExpression)expr;

						if (c.Value is IQueryable)
						{
							var e = ((IQueryable)c.Value).Expression;

							if (_visitedExprs.Add(e))
							{
								e.Visit(new Func<Expression,bool>(BuildExpression));
								return false;
							}
						}

						if (typeof(Table<>).IsSameOrParentOf(expr.Type))
						{
							_exprBuilder.AppendFormat("db.GetTable<{0}>()", GetTypeName(expr.Type.GetGenericArgumentsEx()[0]));
						}
						else if (expr.ToString() == "value(" + expr.Type + ")")
							_exprBuilder.Append("value(").Append(GetTypeName(expr.Type)).Append(")");
						else
							_exprBuilder.Append(expr);

						return true;
					}

				case ExpressionType.Lambda:
					{
						var le = (LambdaExpression)expr;
						var ps = le.Parameters
							.Select(p => (/*GetTypeName(p.Type) + " " + */ EncryptName(p.Name, "p")).TrimStart())
							.Aggregate("", (p1, p2) => p1 + ", " + p2, p => p.TrimStart(',', ' '));

						if (le.Parameters.Count == 1)
							_exprBuilder.Append(ps);
						else
							_exprBuilder.Append("(").Append(ps).Append(")");
						_exprBuilder.Append(" => ");

						le.Body.Visit(new Func<Expression,bool>(BuildExpression));
						return false;
					}

				case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						_exprBuilder.Append("(");
						e.Test.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.Append(" ? ");
						e.IfTrue.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.Append(" : ");
						e.IfFalse.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.Append(")");

						return false;
					}

				case ExpressionType.New:
					{
						var ne = (NewExpression)expr;

						if (IsAnonymous(ne.Type))
						{
							if (ne.Members.Count == 1)
							{
								_exprBuilder.AppendFormat("new {{ {0} = ", EncryptName(ne.Members[0].DeclaringType, ne.Members[0].Name, "P"));
								ne.Arguments[0].Visit(new Func<Expression,bool>(BuildExpression));
								_exprBuilder.Append(" }}");
							}
							else
							{
								_exprBuilder.AppendLine("new").Append(_indent).Append("{");

								PushIndent();

								for (var i = 0; i < ne.Members.Count; i++)
								{
									_exprBuilder.AppendLine().Append(_indent).AppendFormat("{0} = ", EncryptName(ne.Members[i].DeclaringType, ne.Members[i].Name, "P"));
									ne.Arguments[i].Visit(new Func<Expression,bool>(BuildExpression));

									if (i + 1 < ne.Members.Count)
										_exprBuilder.Append(",");
								}

								PopIndent();
								_exprBuilder.AppendLine().Append(_indent).Append("}");
							}
						}
						else
						{
							_exprBuilder.AppendFormat("new {0}(", GetTypeName(ne.Type));

							for (var i = 0; i < ne.Arguments.Count; i++)
							{
								ne.Arguments[i].Visit(new Func<Expression,bool>(BuildExpression));
								if (i + 1 < ne.Arguments.Count)
									_exprBuilder.Append(", ");
							}

							_exprBuilder.Append(")");
						}

						return false;
					}

				case ExpressionType.MemberInit:
					{
						Func<MemberBinding,bool> modify = b =>
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment    :
									var ma = (MemberAssignment)b;
									_exprBuilder.AppendFormat("{0} = ", EncryptName(ma.Member.DeclaringType, ma.Member.Name, "P"));
									ma.Expression.Visit(new Func<Expression,bool>(BuildExpression));
									break;
								default:
									_exprBuilder.Append(b.ToString());
									break;
							}

							return true;
						};

						var e = (MemberInitExpression)expr;

						e.NewExpression.Visit(new Func<Expression,bool>(BuildExpression));

						if (e.Bindings.Count == 1)
						{
							_exprBuilder.Append(" { ");
							modify(e.Bindings[0]);
							_exprBuilder.Append(" }");
						}
						else
						{
							_exprBuilder.AppendLine().Append(_indent).Append("{");

							PushIndent();

							for (var i = 0; i < e.Bindings.Count; i++)
							{
								_exprBuilder.AppendLine().Append(_indent);
								modify(e.Bindings[i]);
								if (i + 1 < e.Bindings.Count)
									_exprBuilder.Append(",");
							}

							PopIndent();
							_exprBuilder.AppendLine().Append(_indent).Append("}");
						}

						return false;
					}

				case ExpressionType.NewArrayInit:
					{
						var e = (NewArrayExpression)expr;

						_exprBuilder.AppendFormat("new {0}[]", GetTypeName(e.Type.GetElementType()));

						if (e.Expressions.Count == 1)
						{
							_exprBuilder.Append(" { ");
							e.Expressions[0].Visit(new Func<Expression,bool>(BuildExpression));
							_exprBuilder.Append(" }");
						}
						else
						{
							_exprBuilder.AppendLine().Append(_indent).Append("{");

							PushIndent();

							for (var i = 0; i < e.Expressions.Count; i++)
							{
								_exprBuilder.AppendLine().Append(_indent);
								e.Expressions[i].Visit(new Func<Expression,bool>(BuildExpression));
								if (i + 1 < e.Expressions.Count)
									_exprBuilder.Append(",");
							}

							PopIndent();
							_exprBuilder.AppendLine().Append(_indent).Append("}");
						}

						return false;
					}

				case ExpressionType.TypeIs:
					{
						var e = (TypeBinaryExpression)expr;

						_exprBuilder.Append("(");
						e.Expression.Visit(new Func<Expression, bool>(BuildExpression));
						_exprBuilder.AppendFormat(" is {0})", e.TypeOperand);

						return false;
					}

				case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						e.NewExpression.Visit(new Func<Expression,bool>(BuildExpression));

						if (e.Initializers.Count == 1)
						{
							_exprBuilder.Append(" { ");
							e.Initializers[0].Arguments[0].Visit(new Func<Expression, bool>(BuildExpression));
							_exprBuilder.Append(" }");
						}
						else
						{
							_exprBuilder.AppendLine().Append(_indent).Append("{");

							PushIndent();

							for (var i = 0; i < e.Initializers.Count; i++)
							{
								_exprBuilder.AppendLine().Append(_indent);
								e.Initializers[i].Arguments[0].Visit(new Func<Expression,bool>(BuildExpression));
								if (i + 1 < e.Initializers.Count)
									_exprBuilder.Append(",");
							}

							PopIndent();
							_exprBuilder.AppendLine().Append(_indent).Append("}");
						}

						return false;
					}

				case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						_exprBuilder.Append("Expression.Invoke(");
						e.Expression.Visit(new Func<Expression,bool>(BuildExpression));
						_exprBuilder.Append(", (");

						for (var i = 0; i < e.Arguments.Count; i++)
						{
							e.Arguments[i].Visit(new Func<Expression,bool>(BuildExpression));
							if (i + 1 < e.Arguments.Count)
								_exprBuilder.Append(", ");
						}
						_exprBuilder.Append("))");

						return false;
					}

				default:
					_exprBuilder.AppendLine("// Unknown expression.").Append(_indent).Append(expr);
					return false;
			}
		}

		readonly Dictionary<Type,string> _typeNames = new Dictionary<Type,string>
		{
			{ typeof(object), "object" },
			{ typeof(bool),   "bool"   },
			{ typeof(int),    "int"    },
			{ typeof(string), "string" },
		};

		readonly StringBuilder _typeBuilder = new StringBuilder();

		void BuildType(Type type)
		{
			if (!IsUserType(type) ||
				IsAnonymous(type) ||
				type.AssemblyEx() == GetType().AssemblyEx() ||
				type.IsGenericTypeEx() && type.GetGenericTypeDefinition() != type)
				return;

			var isUserName = IsUserType(type);
			var name       = EncryptName(isUserName, type.Name, "T");
			var idx        = name.LastIndexOf("`");

			if (idx > 0)
				name = name.Substring(0, idx);

			if (type.IsGenericTypeEx())
				type = type.GetGenericTypeDefinition();

			var baseClasses = new[] { type.BaseTypeEx() }
				.Where(t => t != null && t != typeof(object))
				.Concat(type.GetInterfacesEx()).ToArray();

			var ctors = type.GetConstructorsEx().Select(c =>
			{
#if SILVERLIGHT || NETFX_CORE || NETSTANDARD
				var attrs = c.GetCustomAttributes(false).ToList();
#else
				var attrs = c.GetCustomAttributesData();
#endif
				var ps    = c.GetParameters().Select(p => GetTypeName(p.ParameterType) + " " + EncryptName(p.Name, "p")).ToArray();
				return @"{0}
		public {1}({2})
		{{
			throw new NotImplementedException();
		}}".Args(
					attrs.Count > 0 ? attrs.Select(a => "\r\n\t\t" + a.ToString()).Aggregate((a1,a2) => a1 + a2) : "",
					name,
					ps.Length == 0 ? "" : ps.Aggregate((s,t) => s + ", " + t));
			}).ToList();

			if (ctors.Count == 1 && ctors[0].IndexOf("()") >= 0)
				ctors.Clear();

			var members = type.GetFieldsEx().Intersect(_usedMembers.OfType<FieldInfo>()).Select(f =>
			{
#if SILVERLIGHT || NETFX_CORE || NETSTANDARD
				var attrs = f.GetCustomAttributes(false).ToList();
#else
				var attrs = f.GetCustomAttributesData();
#endif
				return @"{0}
		public {1} {2};".Args(
					attrs.Count > 0 ? attrs.Select(a => "\r\n\t\t" + a.ToString()).Aggregate((a1,a2) => a1 + a2) : "",
					GetTypeName(f.FieldType),
					EncryptName(isUserName, f.Name, "P"));
			})
			.Concat(
				type.GetPropertiesEx().Intersect(_usedMembers.OfType<PropertyInfo>()).Select(p =>
				{
#if SILVERLIGHT || NETFX_CORE || NETSTANDARD
					var attrs = p.GetCustomAttributes(false).ToList();
#else
					var attrs = p.GetCustomAttributesData();
#endif
					return string.Format(@"{0}
		{3}{1} {2} {{ get; set; }}",
						attrs.Count > 0 ? attrs.Select(a => "\r\n\t\t" + a.ToString()).Aggregate((a1,a2) => a1 + a2) : "",
						GetTypeName(p.PropertyType),
						EncryptName(isUserName, p.Name, "P"),
						type.IsInterfaceEx() ? "" : "public ");
				}))
			.Concat(
				type.GetMethodsEx().Intersect(_usedMembers.OfType<MethodInfo>()).Select(m =>
				{
#if SILVERLIGHT || NETFX_CORE || NETSTANDARD
					var attrs = m.GetCustomAttributes(false).ToList();
#else
					var attrs = m.GetCustomAttributesData();
#endif
					var ps    = m.GetParameters().Select(p => GetTypeName(p.ParameterType) + " " + EncryptName(p.Name, "p")).ToArray();
					return string.Format(@"{0}
		{5}{4}{1} {2}({3})
		{{
			throw new NotImplementedException();
		}}",
						attrs.Count > 0 ? attrs.Select(a => "\r\n\t\t" + a.ToString()).Aggregate((a1,a2) => a1 + a2) : "",
						GetTypeName(m.ReturnType),
						EncryptName(isUserName, m.Name, "M"),
						ps.Length == 0 ? "" : ps.Aggregate((s,t) => s + ", " + t),
						m.IsStatic   ? "static "   :
						m.IsVirtual  ? "virtual "  :
						m.IsAbstract ? "abstract " :
						               "",
						type.IsInterfaceEx() ? "" : "public ");
				}))
			.ToArray();

			{
#if SILVERLIGHT || NETFX_CORE || NETSTANDARD
				var attrs = type.GetCustomAttributesEx(false).ToList();
#else
				var attrs = type.GetCustomAttributesData();
#endif

				_typeBuilder.AppendFormat(
					type.IsGenericTypeEx() ?
@"
namespace {0}
{{{8}
	{6}{7}{1} {2}<{3}>{5}
	{{{4}{9}
	}}
}}
"
:
@"
namespace {0}
{{{8}
	{6}{7}{1} {2}{5}
	{{{4}{9}
	}}
}}
",
					EncryptName(isUserName, type.Namespace, "T"),
					type.IsInterfaceEx() ? "interface" : type.IsClassEx() ? "class" : "struct",
					name,
					type.IsGenericTypeEx() ? GetTypeNames(type.GetGenericArgumentsEx(), ",") : null,
					ctors.Count == 0 ? "" : ctors.Aggregate((s,t) => s + "\r\n" + t),
					baseClasses.Length == 0 ? "" : " : " + GetTypeNames(baseClasses),
					type.IsPublicEx() ? "public " : "",
					type.IsAbstractEx() && !type.IsInterfaceEx() ? "abstract " : "",
					attrs.Count > 0 ? attrs.Select(a => "\r\n\t" + a.ToString()).Aggregate((a1,a2) => a1 + a2) : "",
					members.Length > 0 ?
						(ctors.Count != 0 ? "\r\n" : "") + members.Aggregate((f1,f2) => f1 + "\r\n" + f2) :
						"");
			}
		}

		string GetTypeNames(IEnumerable<Type> types, string separator = ", ")
		{
			return types.Select(GetTypeName).Aggregate(
				"",
				(t1,t2) => t1 + separator + t2,
				p => p.TrimStart(separator.ToCharArray()));
		}

		bool IsAnonymous(Type type)
		{
			return type.Name.StartsWith("<>f__AnonymousType");
		}

		readonly Dictionary<string,string> _nameDic = new Dictionary<string,string>();

		string EncryptName(Type type, string name, string prefix)
		{
			return IsUserType(type) ? EncryptName(name, prefix) : name;
		}

		string EncryptName(bool isUserType, string name, string prefix)
		{
			return isUserType ? EncryptName(name, prefix) : name;
		}

		string EncryptName(string name, string prefix)
		{
			if (!_encryptNames)
				return name;

			var oldNames = name.Split('.');
			var newNames = new string[oldNames.Length];

			for (var i = 0; i < oldNames.Length; i++)
			{
				string encryptedName;

				if (_nameDic.TryGetValue(prefix + oldNames[i], out encryptedName))
					newNames[i] = encryptedName;
				else
					newNames[i] = _nameDic[prefix + oldNames[i]] = prefix + _nameDic.Count;
			}

			return string.Join(".", newNames);
		}

		public static List<string> SystemNamespaces = new List<string>
		{
			"System", "LinqToDB", "Microsoft"
		};

		bool IsUserType(Type type)
		{
			return type.Namespace == null || SystemNamespaces.All(ns => type.Namespace != ns && !type.Namespace.StartsWith(ns + '.'));
		}

		string GetTypeName(Type type)
		{
			if (type == null || type == typeof(object))
				return null;

			if (type.IsGenericParameter)
				return type.ToString();

			string name;

			if (_typeNames.TryGetValue(type, out name))
				return name;

			if (IsAnonymous(type))
			{
				_typeNames[type] = null;
				return null;
			}

			if (type.IsGenericTypeEx())
			{
				var args = type.GetGenericArgumentsEx();

				name = "";

				if (type.Namespace != "System")
					name = type.Namespace + ".";

				name += type.Name;

				var idx = name.LastIndexOf("`");

				if (idx > 0)
					name = name.Substring(0, idx);

				if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					name = "{0}?".Args(GetTypeName(args[0]));
				}
				else
				{
					name = string.Format("{0}<{1}>",
						name,
						args.Select(GetTypeName).Aggregate("", (s,t) => s + "," + t, p => p.TrimStart(',')));
				}

				_typeNames[type] = name;

				return name;
			}

			if (type.Namespace == "System")
				return type.Name;

			return EncryptName(type, type.ToString(), "T");
		}

		readonly HashSet<object> _usedMembers = new HashSet<object>();

		void VisitMembers(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.Call :
					{
						var ex = (MethodCallExpression)expr;
						_usedMembers.Add(ex.Method);

						if (ex.Method.IsGenericMethod)
						{
							var gmd = ex.Method.GetGenericMethodDefinition();

							if (gmd != ex.Method)
								_usedMembers.Add(gmd);

							var ga = ex.Method.GetGenericArguments();

							foreach (var type in ga)
								_usedMembers.Add(type);
						}

						break;
					}

				case ExpressionType.MemberAccess :
					{
						var ex = (MemberExpression)expr;
						_usedMembers.Add(ex.Member);
						break;
					}

				case ExpressionType.MemberInit :
					{
						var ex = (MemberInitExpression)expr;

						Action<IEnumerable<MemberBinding>> visit = null; visit = bs =>
						{
							foreach (var b in bs)
							{
								_usedMembers.Add(b.Member);

								switch (b.BindingType)
								{
									case MemberBindingType.MemberBinding :
										visit(((MemberMemberBinding)b).Bindings);
										break;
								}}
						};

						visit(ex.Bindings);
						break;
					}
			}
		}

		readonly HashSet<Type> _usedTypes = new HashSet<Type>();

		void AddType(Type type)
		{
			if (type == null || type == typeof(object) || type.IsGenericParameter || _usedTypes.Contains(type))
				return;

			_usedTypes.Add(type);

			if (type.IsGenericTypeEx())
				foreach (var arg in type.GetGenericArgumentsEx())
					AddType(arg);

			if (type.IsGenericTypeEx() && type.GetGenericTypeDefinition() != type)
				AddType(type.GetGenericTypeDefinition());

			AddType(type.BaseTypeEx());

			foreach (var i in type.GetInterfacesEx())
				AddType(i);
		}

		void VisitTypes(Expression expr)
		{
			AddType(expr.Type);

			switch (expr.NodeType)
			{
				case ExpressionType.Call :
					{
						var ex = (MethodCallExpression)expr;
						var mi = ex.Method;

						AddType(mi.DeclaringType);
						AddType(mi.ReturnType);

						foreach (var arg in mi.GetGenericArguments())
							AddType(arg);

						break;
					}
			}
		}


#if NETFX_CORE && !NETSTANDARD

		public string GenerateSource(Expression expr)
		{
			string fileName = null;

			try
			{
				var tf  = Windows.Storage.ApplicationData.Current.TemporaryFolder;
				var dir = tf.CreateFolderAsync("linq2db", Windows.Storage.CreationCollisionOption.OpenIfExists).AsTask().Result;

				var number = 0;//DateTime.Now.Ticks;

				fileName = Path.Combine("ExpressionTest." + number  + ".cs");

				var file = dir.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting).AsTask().Result;

				var source = GenerateSourceString(expr);

				Windows.Storage.FileIO.WriteTextAsync(
					file,
					source).AsTask().RunSynchronously();

				fileName = Path.Combine(dir.Name, fileName);
			}
			catch (Exception)
			{
			}

			return fileName;
		}

#else

		public string GenerateSource(Expression expr)
		{
			string fileName = null;
			StreamWriter sw = null;

			try
			{
				var dir = Path.Combine(Path.GetTempPath(), "linq2db\\");

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				var number = 0;//DateTime.Now.Ticks;

				fileName = Path.Combine(dir, "ExpressionTest." + number + ".cs");

				sw = File.CreateText(fileName);

				var source = GenerateSourceString(expr);
				sw.WriteLine(source);
			}
			catch (Exception ex)
			{
				if (sw != null)
				{
					sw.WriteLine();
					sw.WriteLine(ex.GetType());
					sw.WriteLine(ex.Message);
					sw.WriteLine(ex.StackTrace);
				}
			}
			finally
			{
				if (sw != null)
					sw.Dispose();
			}

			return fileName;
		}

#endif

		public string GenerateSourceString(Expression expr)
		{
			expr.Visit(new Action<Expression>(VisitMembers));
			expr.Visit(new Action<Expression>(VisitTypes));

			foreach (var type in _usedTypes.OrderBy(t => t.Namespace).ThenBy(t => t.Name))
				BuildType(type);

			expr.Visit(new Func<Expression,bool>(BuildExpression));

			_exprBuilder.Replace("<>h__TransparentIdentifier", "tp");
			_exprBuilder.Insert(0, "var quey = ");

			var result = string.Format(
@"//---------------------------------------------------------------------------------------------------
// This code was generated by LinqToDB.
//---------------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;

using NUnit.Framework;
{0}
namespace Tests.UserTests
{{
	[TestFixture]
	public class UserTest : TestBase
	{{
		[Test, DataContextSource]
		public void Test(string context)
		{{
			// {1}
			using (var db = GetDataContext(context))
			{{
				{2};
			}}
		}}
	}}
}}
",
				_typeBuilder,
				_nameDic.Aggregate(expr.ToString(), (current,item) => current.Replace(item.Key.Substring(1), item.Value)),
				_exprBuilder);

			return result;
		}
	}
}
