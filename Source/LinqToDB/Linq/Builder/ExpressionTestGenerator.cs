using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	sealed class ExpressionTestGenerator
	{
		readonly bool          _mangleNames; 
		readonly StringBuilder _exprBuilder = new ();
		readonly IDataContext  _dataContext;

		string _indent = "\t\t\t\t";

		public ExpressionTestGenerator(IDataContext dataContext): this(true, dataContext)
		{
		}

		public ExpressionTestGenerator(bool mangleNames, IDataContext dataContext)
		{
			_mangleNames = mangleNames;
			_dataContext = dataContext;
		}

		void PushIndent() { _indent += '\t'; }
		void PopIndent () { _indent = _indent.Substring(1); }

		readonly HashSet<Expression> _visitedExprs = new ();

		private VisitFuncVisitor<ExpressionTestGenerator>? _buildExpressionVisitor;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Build(Expression expr)
		{
			(_buildExpressionVisitor ??= VisitFuncVisitor<ExpressionTestGenerator>.Create(this, static (ctx, e) => ctx.BuildExpression(e))).Visit(expr);
		}

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

						_exprBuilder.Append('(');

						Build(e.Left);

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

						Build(e.Right);

						_exprBuilder.Append(')');

						return false;
					}

				case ExpressionType.ArrayLength:
					{
						var e = (UnaryExpression)expr;

						Build(e.Operand);
						_exprBuilder.Append(".Length");

						return false;
					}

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					{
						var e = (UnaryExpression)expr;

						_exprBuilder.Append(CultureInfo.InvariantCulture, $"({GetTypeName(e.Type)})");
						Build(e.Operand);

						return false;
					}

				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
					{
						_exprBuilder.Append('-');
						return true;
					}

				case ExpressionType.Not:
					{
						_exprBuilder.Append('!');
						return true;
					}

				case ExpressionType.Quote:
					return true;

				case ExpressionType.TypeAs:
					{
						var e = (UnaryExpression)expr;

						_exprBuilder.Append('(');
						Build(e.Operand);
						_exprBuilder.Append(CultureInfo.InvariantCulture, $" as {GetTypeName(e.Type)})");

						return false;
					}

				case ExpressionType.UnaryPlus:
					{
						_exprBuilder.Append('+');
						return true;
					}

				case ExpressionType.ArrayIndex:
					{
						var e = (BinaryExpression)expr;

						Build(e.Left);
						_exprBuilder.Append('[');
						Build(e.Right);
						_exprBuilder.Append(']');

						return false;
					}

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expr;

						Build(e.Expression!);
						_exprBuilder.Append(CultureInfo.InvariantCulture, $".{MangleName(e.Member.DeclaringType!, e.Member.Name, "P")}");

						return false;
					}

				case ExpressionType.Parameter :
					{
						var e = (ParameterExpression)expr;
						_exprBuilder.Append(MangleName(e.Name, "p"));
						return false;
					}

				case ExpressionType.Call :
					{
						var ex = (MethodCallExpression)expr;
						var mi = ex.Method;

						var isExtension = mi.HasAttribute<ExtensionAttribute>(false);

						if (isExtension)
						{
							Build(ex.Arguments[0]);
							PushIndent();
							_exprBuilder.AppendLine().Append(_indent);
						}
						else if (ex.Object != null)
							Build(ex.Object);
						else
							_exprBuilder.Append(GetTypeName(mi.DeclaringType!));

						_exprBuilder.Append('.').Append(MangleName(mi.DeclaringType!, mi.Name, "M"));

					if ((!ex.IsQueryable() || ex.Method.DeclaringType == typeof(DataExtensions))
						&& mi.IsGenericMethod && mi.GetGenericArguments().Select(GetTypeName).All(t => t != null))
						{
							_exprBuilder
								.Append('<')
								.Append(GetTypeNames(mi.GetGenericArguments(), ","))
								.Append('>');
						}

						_exprBuilder.Append('(');

						PushIndent();

						var n = isExtension ? 1 : 0;

						for (var i = n; i < ex.Arguments.Count; i++)
						{
							if (i != n)
								_exprBuilder.Append(',');

							_exprBuilder.AppendLine().Append(_indent);

							Build(ex.Arguments[i]);
						}

						PopIndent();

						_exprBuilder.Append(')');

						if (isExtension)
						{
							PopIndent();
						}

						return false;
					}

				case ExpressionType.Constant:
					{
						var c = (ConstantExpression)expr;

						if (c.Value is IQueryable queryable)
						{
							var e = queryable.Expression;

							if (_visitedExprs.Add(e))
							{
								Build(e);
								return false;
							}
						}

						if (typeof(Table<>).IsSameOrParentOf(expr.Type))
							_exprBuilder.Append(CultureInfo.InvariantCulture, $"db.GetTable<{GetTypeName(expr.Type.GetGenericArguments()[0])}>()");
						else if (c.Value == _dataContext || c.Value == null && typeof(IDataContext).IsSameOrParentOf(c.Type))
							_exprBuilder.Append("db");
						else if (expr.ToString() == "value(" + expr.Type + ")")
							_exprBuilder.Append("value(").Append(GetTypeName(expr.Type)).Append(')');
						else
							_exprBuilder.Append(CultureInfo.InvariantCulture, $"{expr}");

						return true;
					}

				case ExpressionType.Lambda:
					{
						var le = (LambdaExpression)expr;
						var ps = string.Join(", ", le.Parameters.Select(p => MangleName(p.Name, "p")));

						if (le.Parameters.Count == 1)
							_exprBuilder.Append(ps);
						else
							_exprBuilder.Append('(').Append(ps).Append(')');
						_exprBuilder.Append(" => ");

						Build(le.Body);
						return false;
					}

				case ExpressionType.Conditional:
					{
						var e = (ConditionalExpression)expr;

						_exprBuilder.Append('(');
						Build(e.Test);
						_exprBuilder.Append(" ? ");
						Build(e.IfTrue);
						_exprBuilder.Append(" : ");
						Build(e.IfFalse);
						_exprBuilder.Append(')');

						return false;
					}

				case ExpressionType.New:
					{
						var ne = (NewExpression)expr;

						if (IsAnonymous(ne.Type))
						{
							if (ne.Members!.Count == 1)
							{
								_exprBuilder.Append(CultureInfo.InvariantCulture, $"new {{ {MangleName(ne.Members[0].DeclaringType!, ne.Members[0].Name, "P")} = ");
								Build(ne.Arguments[0]);
								_exprBuilder.Append(" }}");
							}
							else
							{
								_exprBuilder.AppendLine("new").Append(_indent).Append('{');

								PushIndent();

								for (var i = 0; i < ne.Members.Count; i++)
								{
									_exprBuilder
										.AppendLine()
										.Append(CultureInfo.InvariantCulture, $"{_indent}{MangleName(ne.Members[i].DeclaringType!, ne.Members[i].Name, "P")} = ");
									Build(ne.Arguments[i]);

									if (i + 1 < ne.Members.Count)
										_exprBuilder.Append(',');
								}

								PopIndent();
								_exprBuilder.AppendLine().Append(_indent).Append('}');
							}
						}
						else
						{
							_exprBuilder.Append(CultureInfo.InvariantCulture, $"new {GetTypeName(ne.Type)}(");

							for (var i = 0; i < ne.Arguments.Count; i++)
							{
								Build(ne.Arguments[i]);
								if (i + 1 < ne.Arguments.Count)
									_exprBuilder.Append(", ");
							}

							_exprBuilder.Append(')');
						}

						return false;
					}

				case ExpressionType.MemberInit:
					{
						void Modify(MemberBinding b)
						{
							switch (b.BindingType)
							{
								case MemberBindingType.Assignment:
									var ma = (MemberAssignment) b;
									_exprBuilder.Append(CultureInfo.InvariantCulture, $"{MangleName(ma.Member.DeclaringType!, ma.Member.Name, "P")} = ");
									Build(ma.Expression);
									break;
								default:
									_exprBuilder.Append(CultureInfo.InvariantCulture, $"{b}");
									break;
							}
						}

						var e = (MemberInitExpression)expr;

						Build(e.NewExpression);

						if (e.Bindings.Count == 1)
						{
							_exprBuilder.Append(" { ");
							Modify(e.Bindings[0]);
							_exprBuilder.Append(" }");
						}
						else
						{
							_exprBuilder.AppendLine().Append(_indent).Append('{');

							PushIndent();

							for (var i = 0; i < e.Bindings.Count; i++)
							{
								_exprBuilder.AppendLine().Append(_indent);
								Modify(e.Bindings[i]);
								if (i + 1 < e.Bindings.Count)
									_exprBuilder.Append(',');
							}

							PopIndent();
							_exprBuilder.AppendLine().Append(_indent).Append('}');
						}

						return false;
					}

				case ExpressionType.NewArrayInit:
					{
						var e = (NewArrayExpression)expr;

						_exprBuilder.Append(CultureInfo.InvariantCulture, $"new {GetTypeName(e.Type.GetElementType()!)}[]");

						if (e.Expressions.Count == 1)
						{
							_exprBuilder.Append(" { ");
							Build(e.Expressions[0]);
							_exprBuilder.Append(" }");
						}
						else
						{
							_exprBuilder.AppendLine().Append(_indent).Append('{');

							PushIndent();

							for (var i = 0; i < e.Expressions.Count; i++)
							{
								_exprBuilder.AppendLine().Append(_indent);
								Build(e.Expressions[i]);
								if (i + 1 < e.Expressions.Count)
									_exprBuilder.Append(',');
							}

							PopIndent();
							_exprBuilder.AppendLine().Append(_indent).Append('}');
						}

						return false;
					}

				case ExpressionType.TypeIs:
					{
						var e = (TypeBinaryExpression)expr;

						_exprBuilder.Append('(');
						Build(e.Expression);
						_exprBuilder.Append(CultureInfo.InvariantCulture, $" is {e.TypeOperand})");

						return false;
					}

				case ExpressionType.ListInit:
					{
						var e = (ListInitExpression)expr;

						Build(e.NewExpression);

						if (e.Initializers.Count == 1)
						{
							_exprBuilder.Append(" { ");
							Build(e.Initializers[0].Arguments[0]);
							_exprBuilder.Append(" }");
						}
						else
						{
							_exprBuilder.AppendLine().Append(_indent).Append('{');

							PushIndent();

							for (var i = 0; i < e.Initializers.Count; i++)
							{
								_exprBuilder.AppendLine().Append(_indent);
								Build(e.Initializers[i].Arguments[0]);
								if (i + 1 < e.Initializers.Count)
									_exprBuilder.Append(',');
							}

							PopIndent();
							_exprBuilder.AppendLine().Append(_indent).Append('}');
						}

						return false;
					}

				case ExpressionType.Invoke:
					{
						var e = (InvocationExpression)expr;

						_exprBuilder.Append("Expression.Invoke(");
						Build(e.Expression);
						_exprBuilder.Append(", (");

						for (var i = 0; i < e.Arguments.Count; i++)
						{
							Build(e.Arguments[i]);
							if (i + 1 < e.Arguments.Count)
								_exprBuilder.Append(", ");
						}
						_exprBuilder.Append("))");

						return false;
					}

				default:
					_exprBuilder
						.AppendLine("// Unknown expression.")
						.Append(CultureInfo.InvariantCulture, $"{_indent}{expr}");
					return false;
			}
		}

		readonly Dictionary<Type,string?> _typeNames = new ()
		{
			{ typeof(object), "object" },
			{ typeof(bool),   "bool"   },
			{ typeof(int),    "int"    },
			{ typeof(string), "string" },
		};

		readonly StringBuilder _typeBuilder = new ();

		void BuildType(Type type, MappingSchema mappingSchema, DataOptions dataOptions)
		{
			if (!IsUserType(type) ||
				IsAnonymous(type) ||
				type.Assembly == GetType().Assembly ||
				type.IsGenericType && type.GetGenericTypeDefinition() != type)
				return;

			var isUserName = IsUserType(type);
			var name       = MangleName(isUserName, type.Name, "T");
			var idx        = name.LastIndexOf("`");

			if (idx > 0)
				name = name.Substring(0, idx);

			if (type.IsGenericType)
				type = type.GetGenericTypeDefinition();

			if (type.IsEnum)
			{
				// var ed = mappingSchema.GetEntityDescriptor(type); -> todo entity descriptor should contain enum mappings, otherwise fluent mappings will not be included
				_typeBuilder.AppendLine("\tenum " + MangleName(isUserName, type.Name, "T") + " {");
				foreach (var nm in Enum.GetNames(type))
				{
					var attr = "";
					var valueAttribute = mappingSchema.GetAttribute<MapValueAttribute>(type, type.GetField(nm)!);
					if (valueAttribute != null)
					{
						attr = "[MapValue(\"" + valueAttribute.Value + "\")] ";
					}
					_typeBuilder.AppendLine(CultureInfo.InvariantCulture, $"\t\t{attr}{nm} = {Convert.ToInt64(Enum.Parse(type, nm), CultureInfo.InvariantCulture)},");
				}
				_typeBuilder.Remove(_typeBuilder.Length - 1, 1);
				_typeBuilder.AppendLine("\t}");
				return;
			}

			var baseClasses = CollectBaseTypes(type);

			var ctors = type.GetConstructors().Select(c =>
			{
				var attrs = c.GetCustomAttributesData();
				var attr  = string.Concat(attrs.Select(a => "\r\n\t\t" + a.ToString()));
				var ps    = c.GetParameters().Select(p => GetTypeName(p.ParameterType) + " " + MangleName(p.Name, "p"));

				return string.Format(CultureInfo.InvariantCulture, @"{0}
		public {1}({2})
		{{
			// throw new NotImplementedException();
		}}",
					attr,
					name,
					string.Join(", ", ps));
			}).ToList();

			if (ctors.Count == 1 && ctors[0].IndexOf("()") >= 0)
				ctors.Clear();

			var members = type.GetFields().Intersect(_usedMembers.OfType<FieldInfo>()).Select(f =>
			{
				var attr = "";
				var ed = mappingSchema.GetEntityDescriptor(type, dataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				if (ed != null)
				{
					var colum = ed.Columns.FirstOrDefault(x => x.MemberInfo == f);
					if (colum != null)
					{
						attr += "\t\t[Column(" + (string.IsNullOrEmpty(colum.ColumnName) ? "" : "\"" + colum.ColumnName + "\"") + ")]" + Environment.NewLine;
					}
					else
					{
						attr += "\t\t[NotColumn]" + Environment.NewLine;
					}
					if (colum != null && colum.IsPrimaryKey)
					{
						attr += "\t\t[PrimaryKey]" + Environment.NewLine;
					}
				}
				return string.Format(CultureInfo.InvariantCulture, @"
{0}		public {1} {2};",
					attr,
					GetTypeName(f.FieldType),
					MangleName(isUserName, f.Name, "P"));
			})
			.Concat(
				type.GetPropertiesEx().Intersect(_usedMembers.OfType<PropertyInfo>()).Select(p =>
				{
					var attr = "";
					var ed = mappingSchema.GetEntityDescriptor(type, dataOptions.ConnectionOptions.OnEntityDescriptorCreated);
					if (ed != null)
					{
						var colum = ed.Columns.FirstOrDefault(x => x.MemberInfo == p);
						if (colum != null)
						{
							attr += "\t\t[Column(" + (string.IsNullOrEmpty(colum.ColumnName) ? "" : "\"" + colum.ColumnName + "\"") + ")]" + Environment.NewLine;
						}
						else
						{
							attr += "\t\t[NotColumn]" + Environment.NewLine;
						}
						if (colum != null && colum.IsPrimaryKey)
						{
							attr += "\t\t[PrimaryKey]" + Environment.NewLine;
						}
					}
					return string.Format(CultureInfo.InvariantCulture, @"
{0}		{3}{1} {2} {{ get; set; }}",
						attr,
						GetTypeName(p.PropertyType),
						MangleName(isUserName, p.Name, "P"),
						type.IsInterface ? "" : "public ");
				}))
			.Concat(
				type.GetMethods().Intersect(_usedMembers.OfType<MethodInfo>()).Select(m =>
				{
					var attrs = m.GetCustomAttributesData();
					var ps    = m.GetParameters().Select(p => GetTypeName(p.ParameterType) + " " + MangleName(p.Name, "p"));
					return string.Format(CultureInfo.InvariantCulture, @"{0}
		{5}{4}{1} {2}({3})
		{{
			throw new NotImplementedException();
		}}",
						string.Concat(attrs.Select(a => "\r\n\t\t" + a.ToString())),
						GetTypeName(m.ReturnType),
						MangleName(isUserName, m.Name, "M"),
						string.Join(", ", ps),
						m.IsStatic   ? "static "   :
						m.IsVirtual  ? "virtual "  :
						m.IsAbstract ? "abstract " :
						               "",
						type.IsInterface ? "" : "public ");
				}))
			.ToArray();

			{
				var attr = "";
				var ed = mappingSchema.GetEntityDescriptor(type, dataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				if (ed != null && !type.IsInterface)
				{
					attr += "\t[Table(" + (string.IsNullOrEmpty(ed.Name.Name) ? "" : "\"" + ed.Name.Name + "\"") + ")]" + Environment.NewLine;
				}

				_typeBuilder.AppendFormat(
					CultureInfo.InvariantCulture,
					type.IsGenericType ?
@"
{0}	{1}{2}{3} {4}<{8}>{5}
	{{{6}{7}
	}}
"
:
@"
{0}	{1}{2}{3} {4}{5}
	{{{6}{7}
	}}
",
					attr,
					type.IsPublic ? "public " : string.Empty,
					type.IsAbstract && !type.IsInterface ? "abstract " : string.Empty,
					type.IsInterface ? "interface" : type.IsClass ? "class" : "struct",
					name,
					baseClasses.Length == 0 ? string.Empty : " : " + GetTypeNames(baseClasses),
					string.Join("\r\n", ctors),
					members.Length > 0 ? (ctors.Count != 0 ? "\r\n" : string.Empty) + string.Join("\r\n", members) : string.Empty,
					type.IsGenericType ? GetTypeNames(type.GetGenericArguments(), ",") : string.Empty);
			}
		}

		private static Type[] CollectBaseTypes(Type type)
		{
			var types = new List<Type>();
			var duplicateInterfaces = new HashSet<Type>();

			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				types.Add(type.BaseType);

				populateBaseInterfaces(type.BaseType, duplicateInterfaces);
			}

			foreach (var iface in type.GetInterfaces())
				if (duplicateInterfaces.Add(iface))
				{
					types.Add(iface);
					populateBaseInterfaces(iface, duplicateInterfaces);
				}

			return types.ToArray();

			static void populateBaseInterfaces(Type type, HashSet<Type> duplicateInterfaces)
			{
				foreach (var iface in type.GetInterfaces())
					duplicateInterfaces.Add(iface);
			}
		}

		string GetTypeNames(IEnumerable<Type> types, string separator = ", ")
		{
			return string.Join(separator, types.Select(GetTypeName));
		}

		static bool IsAnonymous(Type type)
		{
			return type.Name.StartsWith("<>");
		}

		readonly Dictionary<string,string> _nameDic = new ();

		string MangleName(Type type, string? name, string prefix)
		{
			return IsUserType(type) ? MangleName(name, prefix) : name ?? prefix;
		}

		string MangleName(bool isUserType, string? name, string prefix)
		{
			return isUserType ? MangleName(name, prefix) : name ?? prefix;
		}

		string MangleName(string? name, string prefix)
		{
			name ??= "";
			if (!_mangleNames)
				return name;

			var oldNames = name.Split('.');
			var newNames = new string[oldNames.Length];

			for (var i = 0; i < oldNames.Length; i++)
			{
				if (_nameDic.TryGetValue(prefix + oldNames[i], out var mangledName))
					newNames[i] = mangledName;
				else
					newNames[i] = _nameDic[prefix + oldNames[i]] = FormattableString.Invariant($"{prefix}{_nameDic.Count}");
			}

			return string.Join(".", newNames);
		}

		public static List<string> SystemNamespaces = new ()
		{
			"System", "LinqToDB", "Microsoft"
		};

		bool IsUserType(Type type)
		{
			return IsUserNamespace(type.Namespace);
		}

		static bool IsUserNamespace(string? @namespace)
		{
			return @namespace == null || SystemNamespaces.All(ns => @namespace != ns && !@namespace.StartsWith(ns + '.'));
		}

		string? GetTypeName(Type type)
		{
			if (type == null || type == typeof(object))
				return null;

			if (type.IsGenericParameter)
				return type.ToString();

			if (_typeNames.TryGetValue(type, out var name))
				return name;

			if (IsAnonymous(type))
			{
				_typeNames[type] = null;
				return null;
			}

			if (type.IsGenericType)
			{
				var args = type.GetGenericArguments();

				name = "";

				if (type.Namespace != "System")
					name = type.Namespace + ".";

				name += type.Name;

				var idx = name.LastIndexOf("`");

				if (idx > 0)
					name = name.Substring(0, idx);

				if (type.IsNullable())
				{
					name = $"{GetTypeName(args[0])}?";
				}
				else
				{
					name = string.Format(
						CultureInfo.InvariantCulture,
						"{0}<{1}>",
						name,
						string.Join(", ", args.Select(GetTypeName)));
				}

				_typeNames[type] = name;

				return name;
			}

			if (type.Namespace == "System")
				return type.Name;

			return MangleName(type, type.ToString(), "T");
		}

		readonly HashSet<object> _usedMembers = new ();

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

						void Visit(IEnumerable<MemberBinding> bs)
						{
							foreach (var b in bs)
							{
								_usedMembers.Add(b.Member);

								switch (b.BindingType)
								{
									case MemberBindingType.MemberBinding:
										Visit(((MemberMemberBinding) b).Bindings);
										break;
								}
							}
						}

						Visit(ex.Bindings);
						break;
					}
			}
		}

		readonly HashSet<Type> _usedTypes = new ();

		void AddType(Type? type)
		{
			if (type == null || type == typeof(object) || type.IsGenericParameter || _usedTypes.Contains(type))
				return;

			_usedTypes.Add(type);

			if (type.IsGenericType)
				foreach (var arg in type.GetGenericArguments())
					AddType(arg);

			if (type.IsGenericType && type.GetGenericTypeDefinition() != type)
				AddType(type.GetGenericTypeDefinition());

			AddType(type.BaseType);

			foreach (var i in type.GetInterfaces())
				AddType(i);
		}

		private VisitActionVisitor<ExpressionTestGenerator>? _typesVisitor;
		private VisitActionVisitor<ExpressionTestGenerator>? _membersVisitor;

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

		public string? GenerateSource(Expression expr)
		{
			string? fileName = null;
			StreamWriter? sw = null;

			try
			{
				var dir = Path.Combine(Path.GetTempPath(), "linq2db\\");

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				var number = 0;//DateTime.Now.Ticks;

				fileName = Path.Combine(dir, FormattableString.Invariant($"ExpressionTest.{number}.cs"));

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
				sw?.Dispose();
			}

			return fileName;
		}

		public string GenerateSourceString(Expression expr)
		{
			(_membersVisitor ??= VisitActionVisitor<ExpressionTestGenerator>.Create(this, static (ctx, e) => ctx.VisitMembers(e))).Visit(expr);
			(_typesVisitor   ??= VisitActionVisitor<ExpressionTestGenerator>.Create(this, static (ctx, e) => ctx.VisitTypes(e))).Visit(expr);

			foreach (var typeNamespaceList in _usedTypes.OrderBy(t => t.Namespace).GroupBy(x => x.Namespace))
			{
				if (typeNamespaceList.All(type =>
				{
					return (!IsUserType(type) ||
							IsAnonymous(type) ||
							type.Assembly == GetType().Assembly ||
							type.IsGenericType && type.GetGenericTypeDefinition() != type);
				}))
					continue;
				_typeBuilder.AppendLine("namespace " + MangleName(IsUserNamespace(typeNamespaceList.Key), typeNamespaceList.Key, "T"));
				_typeBuilder.AppendLine("{");
				foreach (var type in typeNamespaceList.OrderBy(t => t.Name))
				{
					BuildType(type, _dataContext.MappingSchema, _dataContext.Options);
				}
				_typeBuilder.AppendLine("}");
			}

			Build(expr);

			_exprBuilder.Replace("<>h__TransparentIdentifier", "tp");
			_exprBuilder.Insert(0, "var query = ");

			var result = string.Format(CultureInfo.InvariantCulture,
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
		[Test]
		public void Test([DataSources(ProviderName.SQLite)] string context)
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
