using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LinqToDB.CodeGen.CodeGeneration;

namespace LinqToDB.CodeGen.CodeModel
{

	public class CSharpCodeGenerator : CodeGenerationVisitor
	{
		private static readonly IDictionary<BinaryOperation, string> _operators = new Dictionary<BinaryOperation, string>()
		{
			{ BinaryOperation.Equal, " == " },
			{ BinaryOperation.And, " && " },
		};

		// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#line-terminators
		private static readonly string[] _newLines = new []{ "\x000D\x000A", "\x000A", "\x000D", "\x0085", "\x2028", "\x2029" };

		private readonly bool _useNRT;
		private IType? _currentType;
		private List<CollectMemberNamesVisitor> _contextNamesStack = new();

		private readonly ILanguageServices _langServices;
		private readonly ISet<CodeIdentifier[]> _knownNamespaces;
		private CodeIdentifier[]? _currentNamespace;

		public CSharpCodeGenerator(ILanguageServices langServices, CodeGenerationSettings settings, ISet<CodeIdentifier[]> knownNamespaces)
			: base(settings)
		{
			_useNRT = Settings.NullableReferenceTypes;
			_langServices = langServices;
			_knownNamespaces = knownNamespaces;
		}

		protected override string[] NewLineSequences => _newLines;

		protected override void Visit(CodeElementImport import)
		{
			Write("using ");
			WriteDelimitedList(import.Parts, ".", false);
			WriteLine(";");
		}

		protected override void Visit(CodeElementPragma pragma)
		{
			switch (pragma.PragmaType)
			{
				case PragmaType.NullableEnable:
					WriteUnindentedLine("#nullable enable");
					break;
				case PragmaType.DisableWarning:
					WriteUnindented("#pragma warning disable");
					foreach (var warn in pragma.Parameters)
					{
						Write(' ');
						Write(string.Join(string.Empty, SplitByNewLine(warn)));
					}
					WriteLine();
					break;
				case PragmaType.Error:
					WriteUnindented("#error ");
					WriteLine(string.Join(string.Empty, SplitByNewLine(pragma.Parameters[0])));
					break;
				default:
					throw new NotImplementedException();
			}
		}

		protected override void Visit(CodeFile file)
		{
			VisitList(file);
		}

		protected override void Visit(CodeBinaryExpression expression)
		{
			Visit(expression.Left);
			Write(_operators[expression.Operation]);
			Visit(expression.Right);
		}

		protected override void Visit(LambdaMethod method)
		{
			if (method.XmlDoc != null)
				Visit(method.XmlDoc);

			WriteCustomAttributes(method.CustomAttributes, false);

			WriteAttributes(method.Attributes);

			var encloseParameters = method.Parameters.Count != 1 || method.Parameters[0].Type != null;
			if (encloseParameters)
				Write('(');
			WriteDelimitedList(method.Parameters, ", ", false);
			if (encloseParameters)
				Write(')');

			Write(" =>");

			WriteMethodBody(method.Body!, true, true, false, false);
		}

		protected override void Visit(CodeMemberExpression expression)
		{
			if (expression.Type != null)
			{
				Visit(expression.Type);
				Write('.');
			}
			// TODO: check we can ommit this
			else if (expression.Object != null && expression.Object.ElementType != CodeElementType.This)
			{
				Visit(expression.Object);
				Write('.');
			}

			Visit(expression.Member);
		}

		protected override void Visit(NameOfExpression nameOf)
		{
			Write("nameof(");
			Visit(nameOf.Expression);
			Write(")");
		}

		protected override void Visit(CodeRegion region)
		{
			if (region.IsEmpty())
				return;

			Write("#region ");
			WriteLine(string.Join(string.Empty, SplitByNewLine(region.Name!)));

			WriteMemberGroups(region.Members);

			WriteLine("#endregion");
		}

		private string[] BuildAttributesAsTable<T>(List<T> owners)
			where T: AttributeOwner
		{
			// I don't care enough about table layout to justify this abomination...
			var generatedParts = new List<(string attr, List<string>? parameters, List<(string name, string value)>? namedParameters)>?[owners.Count];

			for (var i = 0; i < owners.Count; i++)
			{
				var attrs = owners[i].CustomAttributes;
				if (attrs.Count > 0)
				{
					var list = generatedParts[i] = new List<(string attr, List<string>? parameters, List<(string name, string value)>? namedParameters)>();
					foreach (var attr in attrs)
					{
						List<string>? parameters = null;
						List<(string name, string value)>? namedParameters = null;

						var builtName = BuildFragment<CSharpCodeGenerator>(b => WriteAttributeType(attr, false));

						if (attr.Parameters.Count > 0)
						{
							parameters = new();
							foreach (var param in attr.Parameters)
								parameters.Add(BuildFragment<CSharpCodeGenerator>(b => b.Visit(param)));
						}

						if (attr.NamedParameters.Count > 0)
						{
							namedParameters = new();
							foreach (var (property, value) in attr.NamedParameters)
								namedParameters.Add((
									BuildFragment<CSharpCodeGenerator>(b => b.Visit(property)),
									BuildFragment<CSharpCodeGenerator>(b => b.Visit(value))
									));
						}

						list.Add((builtName, parameters, namedParameters));
					}
				}
			}

			var results = new string[owners.Count];

			var maxAttributesCount = generatedParts.Max(p => p?.Count ?? 0);

			// magic
			var maxLengths = generatedParts.Where(p => p != null)
				.SelectMany(p => p.Select((a, i) => (a, i)))
				.GroupBy(a => a.i)
				.OrderBy(g => g.Key)
				.Select(g => (
					attrNameLength: g.Max(r => r.a.attr.Length),
					parametersLengths: g.Where(r => r.a.parameters != null)
											.SelectMany(r => r.a.parameters.Select((p, i) => (p, i) ))
											.GroupBy(a => a.i)
											.OrderBy(g => g.Key)
											.Select(g => g.Max(g => g.p.Length))
											.ToArray(),
					namedParametersLengths: g.Where(r => r.a.namedParameters != null)
											.SelectMany(r => r.a.namedParameters.Select((p, i) => (p, i) ))
											.GroupBy(a => a.i)
											.OrderBy(g => g.Key)
											.Select(g => (
												name: g.Max(g => g.p.name.Length),
												value: g.Max(g => g.p.value.Length)
											))
											.ToArray()
					))
				.ToList();


			var totalWidth = maxAttributesCount == 0 ? 0 : 3; // "[] "
			for (var i = 0; i < maxLengths.Count; i++)
			{
				if (i > 0)
					totalWidth += 2; // ", "

				var (attrNameLength, parametersLengths, namedParametersLengths) = maxLengths[i];
				totalWidth += attrNameLength;

				totalWidth += 2 * (parametersLengths.Length + namedParametersLengths.Length); // "()" + ", "
				totalWidth += 3 * namedParametersLengths.Length; // " = "

				totalWidth += parametersLengths.Sum();
				totalWidth += namedParametersLengths.Sum(r => r.name + r.value);
			}

			for (var i = 0; i < owners.Count; i++)
			{
				var parts   = generatedParts[i];
				var padding = totalWidth;

				if (parts != null)
				{
					var result = new StringBuilder();
					result.Append('[');
					padding -= 3;

					for (var j = 0; j < parts.Count; j++)
					{
						var (attrLen, paramLen, namedParamLen) = maxLengths[j];

						var (attr, parameters, namedParameters) = parts[j];

						result.Append(attr);
						result.Append(' ', attrLen - attr.Length);
						padding -= attrLen;

						if (parameters != null || namedParameters != null)
						{
							result.Append('(');
							padding -= 1;
						}

						var paramsPadding = paramLen.Sum() + 2 * (paramLen.Length == 0 ? 0 : ( namedParamLen.Length == 0 ? paramLen.Length - 1 : paramLen.Length));
						padding -= paramsPadding;

						if (parameters != null)
						{
							for (var k = 0; k < parameters.Count; k++)
							{
								var paramPadding = paramLen[k];
								paramsPadding -= paramPadding;
								result.Append(parameters[k]);
								paramPadding -= parameters[k].Length;
								if (k < parameters.Count - 1 || namedParameters != null)
								{
									result.Append(", ");
									paramsPadding -= 2;
								}

								result.Append(' ', paramPadding);
							}
						}

						result.Append(' ', paramsPadding);

						var namedParamsPadding = namedParamLen.Sum(np => np.name + np.value + 3) + 2 * (namedParamLen.Length > 0 ? namedParamLen.Length - 1 : 0);
						padding -= namedParamsPadding;

						if (namedParameters != null)
						{
							for (var k = 0; k < namedParameters.Count; k++)
							{
								var (namePadding, valuePadding) = namedParamLen[k];
								namedParamsPadding -= namePadding;
								namedParamsPadding -= valuePadding;
								namedParamsPadding -= 3;

								result.Append(namedParameters[k].name);
								namePadding -= namedParameters[k].name.Length;
								result.Append(' ', namePadding);

								result.Append(" = ");

								result.Append(namedParameters[k].value);
								valuePadding -= namedParameters[k].value.Length;

								if (k < namedParameters.Count - 1)
								{
									result.Append(", ");
									namedParamsPadding -= 2;
								}

								result.Append(' ', valuePadding);
							}
						}

						if (parameters != null || namedParameters != null)
						{
							result.Append(')');
							padding -= 1;
						}

						result.Append(' ', namedParamsPadding);
					}

					result.Append(' ', padding);
					result.Append("] ");
					results[i] = result.ToString();

				}
				else
				{
					results[i] = new string(' ', padding);
				}
			}

			return results;
		}

		private void WriteProperties(List<CodeProperty> properties)
		{
			if (properties.Count == 0)
				return;

			// TODO: extract attributes build
			// TODO: implement multiline
			var multiline = new bool[properties.Count];
			var modifier = new string[properties.Count];
			var type = new string[properties.Count];
			var name = new string[properties.Count];
			var getter = new string[properties.Count];
			var setter = new string[properties.Count];
			var maxModifier = 0;
			var maxType = 0;
			var maxName = 0;
			var maxGetter = 0;
			var maxSetter = 0;
			var hasTrailingComment = false;

			var attributes = BuildAttributesAsTable(properties);

			for (var i = 0; i < properties.Count; i++)
			{
				var prop = properties[i];
				hasTrailingComment = hasTrailingComment || prop.TrailingComment != null;

				modifier[i] = BuildFragment<CSharpCodeGenerator>(b => b.WriteAttributes(prop.Attributes));
				type[i] = BuildFragment<CSharpCodeGenerator>(b =>
				{
					b.Visit(prop.Type!);
					Write(' ');
				});
				name[i] = BuildFragment<CSharpCodeGenerator>(b => b.Visit(prop.Name!));
				getter[i] = !prop.HasGetter ? string.Empty : BuildFragment<CSharpCodeGenerator>(b =>
				{
					if (prop.Getter == null)
					{
						Write("get;");
					}
					else
					{
						if (prop.HasSetter)
						{
							Write("get");
							if (prop.Getter.Items.Count == 1)
								Write(" =>");
						}
						else
							Write(" =>");
						b.WriteMethodBody(prop.Getter, true, false, true, false);
					}
				});
				setter[i] = !prop.HasSetter ? string.Empty : BuildFragment<CSharpCodeGenerator>(b =>
				{
					if (prop.Setter == null)
					{
						Write("set;");
					}
					else
					{
						Write("set");
						if (prop.Setter.Items.Count == 1)
							Write(" =>");
						b.WriteMethodBody(prop.Setter, true, true, true, false);
					}
				});

				if (modifier[i].Length > maxModifier) maxModifier = modifier[i].Length;
				if (type[i].Length > maxType) maxType = type[i].Length;
				if (name[i].Length > maxName) maxName = name[i].Length;
				if (getter[i].Length > maxGetter) maxGetter = getter[i].Length;
				if (setter[i].Length > maxSetter) maxSetter = setter[i].Length;
			}

			var useBrackets = maxSetter != 0;
			if (!hasTrailingComment)
			{
				if (!useBrackets)
					maxGetter = 0;
				else
					maxSetter = 0;
			}

			for (var i = 0; i < properties.Count; i++)
			{
				var prop = properties[i];
				if (prop.XmlDoc != null)
					Visit(prop.XmlDoc);

				Write(attributes[i]);
				WriteWithPadding(modifier[i], maxModifier);
				WriteWithPadding(type[i], maxType);
				WriteWithPadding(name[i], maxName);
				if (useBrackets)
					Write(" { ");
				WriteWithPadding(getter[i], maxGetter);
				WriteWithPadding(setter[i], maxSetter);
				if (useBrackets)
					Write(" }");

				// only on auto-properties
				if (_useNRT && prop.Getter == null && prop.Setter == null && !prop.Type.Type.IsNullable && !prop.Type.Type.IsValueType)
					Write(" = null!;");

				if (prop.TrailingComment != null)
				{
					Write(' ');
					Visit(prop.TrailingComment);
				}
				else
					WriteLine();
			}
		}

		private void WriteCustomAttributes(List<CodeAttribute> customAttributes, bool inline)
		{
			if (!inline)
			{
				foreach (var attr in customAttributes)
				{
					Write("[");
					Visit(attr);
					WriteLine("]");
				}
			}
			else if (customAttributes.Count > 0)
			{
				Write("[");
				WriteDelimitedList(customAttributes, ", ", false);
				Write("]");
			}
		}

		protected override void Visit(CodeConstant constant)
		{
			WriteLiteral(constant.Value, constant.Type.Type, constant.TargetTyped);
		}

		private void WriteLiteral(object? value, IType type, bool targetTyped)
		{
			if (value == null)
			{
				Write("null");
			}
			else if (value is bool boolean)
			{
				if (boolean)
					Write("true");
				else
					Write("false");
			}
			else if (value is int intValue)
			{
				Write(intValue.ToString(NumberFormatInfo.InvariantInfo));
			}
			else if (value is long longValue)
			{
				Write(longValue.ToString(NumberFormatInfo.InvariantInfo));
				if (!targetTyped)
					Write('L');
			}
			else if (value is string str)
			{
				Write('"');

				foreach (var chr in str)
				{
					switch (chr)
					{
						case '\t': Write("\\t"); break;
						case '\n': Write("\\n"); break;
						case '\r': Write("\\r"); break;
						case '\\': Write("\\\\"); break;
						case '"': Write("\\\""); break;
						case '\0': Write("\\0"); break;
						case '\u0085':
						case '\u2028':
						case '\u2029':
							Write($"\\u{(ushort)chr:X4}");
							break;
						default: Write(chr); break;
					}
				}

				Write('"');
			}
			else if (value.GetType().IsEnum)
			{
				var enumType = value.GetType();

				if (Enum.IsDefined(enumType, value))
				{
					RenderType(type.WithNullability(false), null, false, false);
					Write('.');
					WriteIdentifier(Enum.GetName(enumType, value));
				}
				else
				{
					// TODO: add [Flags] enums support

					Write('(');
					RenderType(type.WithNullability(false), null, false, false);
					Write(')');
					var underlyingType = Enum.GetUnderlyingType(enumType);
					WriteLiteral(Convert.ChangeType(value, underlyingType), TypeBuilder.FromType(underlyingType, _langServices), true);
				}
			}
			else
				throw new NotImplementedException($"{value.GetType().Name}");
		}

		protected override void Visit(CodeAttribute attribute)
		{
			// remove Attribute suffix
			WriteAttributeType(attribute, Parent?.ElementType == CodeElementType.Class);

			if (attribute.Parameters.Count > 0 || attribute.NamedParameters.Count > 0)
			{
				Write('(');
				if (attribute.Parameters.Count > 0)
				{
					WriteDelimitedList(attribute.Parameters, ", ", false);
				}

				var first = attribute.Parameters.Count == 0;
				foreach (var (prop, value) in attribute.NamedParameters)
				{
					if (first)
						first = false;
					else
						Write(", ");
					Visit(prop);
					Write(" = ");
					Visit(value);
				}
				Write(')');
			}
		}

		private void WriteAttributeType(CodeAttribute attribute, bool onType)
		{
			CodeIdentifier? newTypeName = null;
			if (attribute.Type.Type.Name == null)
				throw new InvalidOperationException();
			if (attribute.Type.Type.Name.Name.EndsWith("Attribute"))
				newTypeName = new CodeIdentifier(attribute.Type.Type.Name.Name.Substring(0, attribute.Type.Type.Name.Name.Length - 9));
			RenderType(attribute.Type.Type, newTypeName, onType, true);
		}

		protected override void Visit(CodeElementComment comment)
		{
			var lines = SplitByNewLine(comment.Text);

			if (comment.Inline)
			{
				throw new NotImplementedException();
			}
			else
			{
				foreach (var line in lines)
				{
					Write("// ");
					WriteLine(line);
				}
			}
		}

		protected override void Visit(CodeElementEmptyLine line)
		{
			WriteLine();
		}

		protected override void Visit(CodeMethod method)
		{
			if (method.XmlDoc != null)
				Visit(method.XmlDoc);

			WriteCustomAttributes(method.CustomAttributes, false);
			WriteAttributes(method.Attributes);
			if (method.ReturnType != null)
				Visit(method.ReturnType);
			else
				Write("void");
			Write(" ");
			Visit(method.Name!);
			if (method.TypeParameters.Count > 0)
			{
				Write('<');
				WriteDelimitedList(method.TypeParameters, ", ", false);
				Write('>');
			}
			Write("(");
			if (method.Attributes.HasFlag(MemberAttributes.Extension))
				Write("this ");
			WriteDelimitedList(method.Parameters, ", ", false);
			if (method.Attributes.HasFlag(MemberAttributes.Partial) && method.Body == null)
				WriteLine(");");
			else
			{
				Write(")");
				WriteMethodBody(method.Body!, false, method.ReturnType == null, false, true);
			}
		}

		protected override void Visit(CodeParameter parameter)
		{
			if (parameter.Direction == Direction.Ref)
				Write("ref ");
			else if (parameter.Direction == Direction.Out)
				Write("out ");

			if (parameter.Type != null)
			{
				Visit(parameter.Type);
				Write(' ');
			}

			Visit(parameter.Name);
		}

		protected override void Visit(CodeXmlComment doc)
		{
			if (doc.Summary != null)
			{
				var lines = SplitByNewLine(doc.Summary);

				if (lines.Length > 0)
				{
					WriteLine("/// <summary>");
					foreach (var line in lines)
					{
						Write("/// ");
						WriteXmlText(line);
						WriteLine();
					}
					WriteLine("/// </summary>");
				}
			}
			foreach (var (p, text) in doc.Parameters)
			{
				var lines = SplitByNewLine(text);

				if (lines.Length > 0)
				{
					Write("/// <param name=\"");
					WriteXmlText(p.Name);
					Write("\">");
					WriteLine();
					foreach (var line in lines)
					{
						Write("/// ");
						WriteXmlText(line);
						WriteLine();
					}
					WriteLine("/// </param>");
				}
			}
		}

		protected override void Visit(CodeConstructor ctor)
		{
			if (ctor.XmlDoc != null)
				Visit(ctor.XmlDoc);
			WriteCustomAttributes(ctor.CustomAttributes, false);
			WriteAttributes(ctor.Attributes);
			Visit(ctor.Type.Name);
			Write("(");
			WriteDelimitedList(ctor.Parameters, ", ", false);
			Write(")");
			if (ctor.BaseArguments.Count > 0 || ctor.ThisCall)
			{
				WriteLine();
				IncreaseIdent();
				Write(": ");
				Write(!ctor.ThisCall ? "base(" : "this(");
				WriteDelimitedList(ctor.BaseArguments, ", ", false);
				Write(")");
				DecreaseIdent();
			}

			WriteMethodBody(ctor.Body!, false, true, false, true);
		}

		protected override void Visit(CodeThisExpression expression)
		{
			Write("this");
		}

		protected override void Visit(CodeCallExpression call)
		{
			// TODO: check we can ommit this
			if (call.Callee != null && call.Callee.ElementType != CodeElementType.This)
			{
				if (call.Extension)
					// TODO: here we could need () around parameter value in future
					Visit(call.Parameters[0]);
				else
					Visit(call.Callee);
				Write('.');
			}

			Visit(call.MethodName);
			if (call.TypeArguments.Length > 0)
			{
				Write('<');
				WriteDelimitedList(call.TypeArguments, ", ", false);
				Write('>');
			}

			Write('(');
			WriteDelimitedList(call.Extension ? call.Parameters.Skip(1) : call.Parameters, ", ", false);
			Write(')');
		}

		protected override void Visit(ReturnStatement statement)
		{
			Write("return");
			if (statement.Expression != null)
			{
				Write(' ');
				Visit(statement.Expression);
			}
		}

		protected override void Visit(CodeProperty property)
		{
			if (property.XmlDoc != null)
				Visit(property.XmlDoc);
			WriteCustomAttributes(property.CustomAttributes, false);
			WriteAttributes(property.Attributes);
			Visit(property.Type!);
			Write(' ');
			Visit(property.Name!);

			var autoProperty = property.Getter == null && property.Setter == null;
			var withBrackets = autoProperty || property.Setter != null;

			var multiline = withBrackets && !autoProperty;

			if (multiline)
				WriteLine();
			else
				Write(' ');

			if (withBrackets)
			{
				OpenBlock(!multiline);
			}

			if (property.HasGetter)
			{
				if (property.Getter == null)
				{
					Write("get;");
				}
				else
				{
					Write("get");
					if (property.Getter.Items.Count == 1)
						Write(" =>");
					WriteMethodBody(property.Getter, true, false, true, true);
				}
			}

			if (property.HasSetter)
			{
				if (!multiline && property.HasGetter)
					Write(' ');

				if (property.Setter == null)
				{

					Write("set;");
				}
				else
				{
					Write("set");
					if (property.Setter.Items.Count == 1)
						Write(" =>");
					WriteMethodBody(property.Setter, true, true, true, true);
				}
			}

			if (multiline)
				DecreaseIdent();

			if (withBrackets)
			{
				if (!multiline)
					Write(' ');
				Write('}');
			}

			if (_useNRT && autoProperty && !property.Type.Type.IsNullable && !property.Type.Type.IsValueType)
				Write(" = null!;");

			if (property.TrailingComment != null)
			{
				Write(' ');
				Visit(property.TrailingComment);
			}

			WriteLine();
		}

		protected override void Visit(CodeElementNamespace @namespace)
		{
			// no stack/aggregation as we don't support nested namespaces for now
			var oldNamespace = _currentNamespace;
			_currentNamespace = @namespace.Name;

			var contextNames = new CollectMemberNamesVisitor(_langServices, @namespace);
			_contextNamesStack.Add(contextNames);
			contextNames.Visit(@namespace);

			Write("namespace ");
			WriteDelimitedList(@namespace.Name, ".", false);
			WriteLine();
			OpenBlock(false);
			WriteMemberGroups(@namespace.Members);
			CloseBlock(false, true);

			_contextNamesStack.RemoveAt(_contextNamesStack.Count - 1);

			_currentNamespace = oldNamespace;
		}

		private void WriteMemberGroups(List<IMemberGroup> groups)
		{
			var first = true;
			foreach (var group in groups)
			{
				if (!group.IsEmpty)
				{
					if (first)
						first = false;
					else
						WriteLine();
					Visit(group);
				}
			}
		}

		protected override void Visit(CodeClass @class)
		{
			var contextNames = new CollectMemberNamesVisitor(_langServices, @class);
			_contextNamesStack.Add(contextNames);
			contextNames.Visit(@class);

			var oldType = _currentType;
			_currentType = @class.Type;

			if (@class.XmlDoc != null)
				Visit(@class.XmlDoc);
			WriteCustomAttributes(@class.CustomAttributes, false);
			WriteAttributes(@class.Attributes);
			Write("class ");
			Visit(@class.Name);
			//TODO:type arguments
			if (@class.Implements.Count > 0 || @class.Inherits != null)
			{
				Write(" : ");
				if (@class.Inherits != null)
				{
					Visit(@class.Inherits);
					if (@class.Implements.Count > 0)
						Write(", ");
				}

				for (var i = 0; i < @class.Implements.Count; i++)
				{ 
					if (i > 0)
						Write(", ");
					Visit(@class.Implements[i]);
				}

			}
			WriteLine();
			OpenBlock(false);

			WriteMemberGroups(@class.Members);

			CloseBlock(false, true);
			_currentType = oldType;

			_contextNamesStack.RemoveAt(_contextNamesStack.Count - 1);
		}

		protected override void Visit(CodeIdentifier identifier)
		{
			WriteIdentifier(identifier.Name);
		}

		private void WriteIdentifier(string identifier)
		{
			if (KeyWords.Contains(identifier))
				Write("@");
			Write(identifier);
		}

		protected override void Visit(TypeReference type)
		{
			RenderType(type.Type, null, false, false);
		}

		protected override void Visit(TypeToken type)
		{
			RenderType(type.Type, null, false, true);
		}

		private void RenderType(IType type, CodeIdentifier? nameOverride, bool ignoreCurrentType, bool typeOnlyContext)
		{
			if (type.Parent != null)
			{
				// skip parent types generation for reference to types, nested into current or parent types
				var t = _currentType;
				var skip = false;
				while (t != null)
				{
					skip = t == type.Parent;
					if (skip)
						break;
					t = t.Parent;
				}

				if (!skip)
				{
					RenderType(type.Parent, null, ignoreCurrentType, typeOnlyContext);
					Write('.');
				}
			}

			if (type.Namespace != null && _contextNamesStack.Count > 0)
			{
				// TODO: replace with proper logic (need to find doc on name resolution logic for C# compiler)
				var withNamespace = false;
				var name = nameOverride?.Name ?? type.Name!.Name;
				if (!typeOnlyContext)
				{
					for (var i = _contextNamesStack.Count - 1; i >= 0; i--)
					{
						var context = _contextNamesStack[i];
						if (((ignoreCurrentType && i == _contextNamesStack.Count - 1) ? context.MemberNames : context.MemberNamesWithOwner).Contains(name))
						{
							withNamespace = true;
							break;
						}

						if (_currentNamespace != null && _currentNamespace.SequenceEqual(type.Namespace, CodeIdentifierComparer.Instance))
							break;
					}
				}

				if (!withNamespace)
				{
					var comparer = _langServices.GetNameComparer();
					foreach (var ns in _knownNamespaces)
					{
						if (comparer.Equals(ns[0].Name, name))
						{
							withNamespace = true;
							break;
						}
					}
				}

				if (withNamespace)
				{
					// TODO: detect how many namespace levels to generate
					Write(type.Namespace[type.Namespace.Length - 1].Name);
					Write('.');
				}
			}

			switch (type.Kind)
			{
				case TypeKind.Regular:
				{
					var regType = (RegularType)type;
					if (regType.IsAlias)
						Write(nameOverride?.Name ?? regType.Name.Name);
					else
						Visit(nameOverride ?? regType.Name);
					break;
				}
				case TypeKind.Generic:
				{
					var regType = (GenericType)type;
					Visit(nameOverride ?? regType.Name);
					Write('<');
					WriteDelimitedList(type.TypeArguments!, t => RenderType(t, null, false, typeOnlyContext), ", ", false);
					Write('>');
					break;
				}
				case TypeKind.Array:
				{
					var arrType = (ArrayType)type;
					RenderType(arrType.ArrayElementType, null, false, typeOnlyContext);
					Write('[');
					var first = true;
					foreach (var size in arrType.ArraySizes)
					{
						if (first)
							first = false;
						else
							Write(',');
						if (size != null)
							WriteLiteral(size.Value, TypeBuilder.FromType(typeof(int), _langServices), false);
					}
					Write(']');
					break;
				}
				case TypeKind.TypeArgument:
				{
					var typeArg = (TypeArgument)type;
					Visit(nameOverride ?? typeArg.Name);
					break;
				}
				case TypeKind.Dynamic:
				case TypeKind.OpenGeneric:
				default:
					throw new NotImplementedException();
			}

			if (type.IsNullable)
			{
				if (type.IsValueType || _useNRT)
				{
					Write('?');
				}
			}
		}

		private void CloseBlock(bool inline, bool newLine)
		{
			if (inline)
				Write(" }");
			else
			{
				DecreaseIdent();
				Write("}");
				if (newLine)
					WriteLine();
			}
		}

		private void OpenBlock(bool inline)
		{
			if (inline)
			{
				Write("{ ");
			}
			else
			{
				WriteLine("{");
				IncreaseIdent();
			}
		}

		// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
		private static readonly HashSet<string> KeyWords = new ()
		{
			"abstract", "as"      , "base"     , "bool"     , "break"    , "byte"    , "case"   , "catch"     , "char"     , "checked",
			"class"   , "const"   , "continue" , "decimal"  , "default"  , "delegate", "do"     , "double"    , "else"     , "enum",
			"event"   , "explicit", "extern"   , "false"    , "finally"  , "fixed"   , "float"  , "for"       , "foreach"  , "goto",
			"if"      , "implicit", "in"       , "int"      , "interface", "internal", "is"     , "lock"      , "long"     , "namespace",
			"new"     , "null"    , "object"   , "operator" , "out"      , "override", "params" , "private"   , "protected", "public",
			"readonly", "ref"     , "return"   , "sbyte"    , "sealed"   , "short"   , "sizeof" , "stackalloc", "static"   , "string",
			"struct"  , "switch"  , "this"     , "throw"    , "true"     , "try"     , "typeof" , "uint"      , "ulong"    , "unchecked",
			"unsafe"  , "ushort"  , "using"    , "virtual"  , "void"     , "volatile", "while",
			// contextual words
			// for now we validate them out of context (globaly)
			"add"     , "and"     , "alias"    , "ascending", "async"    , "await"   , "by"     , "descending", "dynamic"  , "equals",
			"from"    , "get"     , "global"   , "group"    , "init"     , "into"    , "join"   , "let"       , "managed"  , "nameof",
			"nint"    , "not"     , "notnull"  , "nuint"    , "on"       , "or"      , "orderby", "partial"   , "record"   , "remove",
			"select"  , "set"     , "unmanaged", "value"    , "var"      , "when"    , "where"  , "with"      , "yield"
		};

		private void WriteAttributes(MemberAttributes attributes)
		{
			if (attributes.HasFlag(MemberAttributes.Public))
				Write("public ");
			else if (attributes.HasFlag(MemberAttributes.Private))
				Write("private ");

			if (attributes.HasFlag(MemberAttributes.Static))
				Write("static ");

			if (attributes.HasFlag(MemberAttributes.ReadOnly))
				Write("readonly ");

			if (attributes.HasFlag(MemberAttributes.Partial))
				Write("partial ");
		}

		private void WriteMethodBody(
			CodeBlock statements,
			bool preferInline,
			bool allowEmpty,
			bool inlineNeedsSemicolon,
			bool endWithNewLine)
		{
			if (statements.Items.Count == 0)
			{
				if (!allowEmpty)
					throw new InvalidOperationException();

				if (preferInline)
				{
					Write(" { }");
					if (endWithNewLine)
						WriteLine();
				}
				else
				{
					WriteLine();
					OpenBlock(false);
					CloseBlock(false, true);
				}
			}
			else if (statements.Items.Count == 1)
			{
				if (preferInline)
					Write(" ");
				else
				{
					WriteLine();
					OpenBlock(false);
				}

				var stmt = statements.Items[0];
				if (preferInline && stmt is ReturnStatement ret)
				{
					if (ret.Expression != null)
					{
						Visit(ret.Expression);
						if (!preferInline || inlineNeedsSemicolon)
							Write(';');
					}
					else
					{
						if (preferInline)
							Write("{ }");
					}
				}
				else
				{
					Visit(stmt);
					if (!preferInline || inlineNeedsSemicolon)
						Write(';');
				}

				if (!preferInline)
				{
					WriteLine();
					CloseBlock(false, true);
				}
				else if (endWithNewLine)
					WriteLine();
			}
			else
			{
				WriteLine();
				OpenBlock(false);
				foreach (var stmt in statements.Items)
				{
					Visit(stmt);
					WriteLine(';');
				}
				CloseBlock(false, true);
			}
		}

		protected override void Visit(PropertyGroup group)
		{
			if (group.TableLayout)
				WriteProperties(group.Members);
			else
				WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(MethodGroup group)
		{
			if (group.TableLayout)
			{
				// TODO: Table layout support
				VisitList(group.Members);
			}
			else
			{
				WriteNewLineDelimitedList(group.Members);
			}
		}

		protected override void Visit(ConstructorGroup group)
		{
			WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(RegionGroup group)
		{
			WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(ClassGroup group)
		{
			WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(NewExpression expression)
		{
			Write("new ");
			Visit(expression.Type);
			Write('(');
			WriteDelimitedList(expression.Parameters, ", ", false);
			Write(')');

			if (expression.Initializers.Length > 0)
			{
				WriteLine();
				OpenBlock(false);
				WriteDelimitedList(expression.Initializers, ",", true);
				CloseBlock(false, false);
			}
		}

		protected override void Visit(AssignExpression expression)
		{
			Visit(expression.LValue);
			Write(" = ");
			Visit(expression.RValue);
		}

		protected override void Visit(FieldGroup group)
		{
			if (group.TableLayout)
				throw new NotImplementedException();
			else
				WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(CodeField field)
		{
			WriteAttributes(field.Attributes);
			Visit(field.Type);
			Write(' ');
			Visit(field.Name);

			if (field.Setter != null)
			{
				Write(" = ");
				Visit(field.Setter);
			}

			WriteLine(';');
		}

		protected override void Visit(CodeDefault expression)
		{
			var type = expression.Type.Type;
			if (type.IsNullable && !type.IsValueType)
				type = type.WithNullability(false);
			Write("default");
			if (!expression.TargetTyped)
			{
				Write('(');
				RenderType(type, null, false, true);
				Write(')');
			}
			if (_useNRT && !expression.Type.Type.IsNullable && !expression.Type.Type.IsValueType)
				Write('!');
		}

		protected override void Visit(VariableExpression expression)
		{
			if (expression.RValueTyped)
			{
				Write("var ");
			}
			else
			{
				Visit(expression.Type);
				Write(' ');
			}

			Visit(expression.Name);
		}

		protected override void Visit(ArrayExpression expression)
		{
			Write("new ");
			if (!expression.ValueTyped || expression.Values.Length == 0)
				Visit(expression.Type);
			if (expression.Values.Length == 0)
				throw new NotImplementedException();
			{
				Write("[]");
				if (!expression.Inline)
					WriteLine();
				OpenBlock(expression.Inline);
				WriteDelimitedList(expression.Values, ",", !expression.Inline);
				CloseBlock(expression.Inline, false);
			}
		}

		protected override void Visit(IndexExpression expression)
		{
			Visit(expression.Object);
			Write('[');
			Visit(expression.Index);
			Write(']');
		}

		protected override void Visit(CastExpression expression)
		{
			// TODO: add priority resolver by node type to avoid ()-wrapper
			Write('(');
			Visit(expression.Type);
			Write(")(");
			Visit(expression.Value);
			Write(')');
		}

		protected override void Visit(ThrowExpression expression)
		{
			Write("throw ");
			Visit(expression.Exception);
		}

		protected override void Visit(PragmaGroup group)
		{
			WriteNewLineDelimitedList(group.Members);
		}
	}
}
