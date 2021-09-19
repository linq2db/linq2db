using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Code generation visitor for C# language.
	/// </summary>
	internal class CSharpCodeGenerator : CodeGenerationVisitor<CSharpCodeGenerator>
	{
		/// <summary>
		/// Code fragments for binary operators.
		/// </summary>
		private static readonly IReadOnlyDictionary<BinaryOperation, string> _operators = new Dictionary<BinaryOperation, string>()
		{
			{ BinaryOperation.Equal, " == " },
			{ BinaryOperation.And  , " && " },
		};

		// newline sequences according to language specs
		// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#line-terminators
		private static readonly string[] _newLines = new []{ "\x000D\x000A", "\x000A", "\x000D", "\x0085", "\x2028", "\x2029" };

		// C# keywords and contextual words
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
			// we don't analyze context for them and tread as keywords to avoid unnecessary complexity in codegeneration
			"add"     , "and"     , "alias"    , "ascending", "async"    , "await"   , "by"     , "descending", "dynamic"  , "equals",
			"from"    , "get"     , "global"   , "group"    , "init"     , "into"    , "join"   , "let"       , "managed"  , "nameof",
			"nint"    , "not"     , "notnull"  , "nuint"    , "on"       , "or"      , "orderby", "partial"   , "record"   , "remove",
			"select"  , "set"     , "unmanaged", "value"    , "var"      , "when"    , "where"  , "with"      , "yield"
		};

		// generate NRT annotations
		private readonly bool                                                                   _useNRT;
		/// <summary>
		/// List of parent identifiers (namespaces and parent/current classes) for current position.
		/// </summary>
		private readonly List<CodeIdentifier>                                                   _currentScope = new();
		// types, defined in current ast as:
		// key: name of type
		// value: list of namespaces with such type
		private readonly IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> _knownTypes;
		private readonly ILanguageProvider                                                      _languageProvider;
		// dictionary with name scopes and names, defined in those scopes:
		// key: name scope (namespace or namespace + type(s) name)
		// value: names in scope: nested namespaces, types and type members
		private readonly IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> _scopedNames;

		// list of imports for current generation context (file)
		private IEnumerable<CodeImport>? _currentImports;
		// current type if current code-generation position is inside of class
		// constains nearest class in case of nested classes
		private IType?                   _currentType;

		public CSharpCodeGenerator(
			ILanguageProvider                                                      languageProvider,
			string                                                                 newLine,
			string                                                                 indent,
			bool                                                                   useNRT,
			IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> sopedNames)
			: base(newLine, indent)
		{
			_languageProvider = languageProvider;
			_useNRT           = useNRT;
			_knownTypes       = knownTypes;
			_scopedNames      = sopedNames;
		}

		protected override string[] NewLineSequences => _newLines;

		#region AST nodes visitors
		protected override void Visit(CodeImport import)
		{
			Write("using ");
			WriteDelimitedList(import.Namespace, ".", false);
			WriteLine(";");
		}

		protected override void Visit(CodePragma pragma)
		{
			switch (pragma.PragmaType)
			{
				case PragmaType.NullableEnable:
					WriteUnindentedLine("#nullable enable");
					break;
				case PragmaType.DisableWarning:
					WriteUnindented("#pragma warning disable");
					var first = true;
					foreach (var warn in pragma.Parameters)
					{

						if (first)
						{
							Write(' ');
							first = false;
						}
						else
							Write(", ");

						// remove any possible newlines to avoid invalid code generation
						Write(string.Join(string.Empty, SplitByNewLine(warn)));
					}
					WriteLine();
					break;
				case PragmaType.Error:
					WriteUnindented("#error ");
					WriteLine(string.Join(string.Empty, SplitByNewLine(pragma.Parameters[0])));
					break;
				default:
					throw new NotImplementedException($"Codegeneration for pragma {pragma.PragmaType} not implemented in C# code generator");
			}
		}

		protected override void Visit(CodeFile file)
		{
			VisitList(file.Header);
			VisitList(file.Imports);
			_currentImports = file.Imports;
			VisitList(file);
		}

		protected override void Visit(CodeBinary expression)
		{
			Visit(expression.Left);

			if (_operators.TryGetValue(expression.Operation, out var operatorCode))
				Write(operatorCode);
			else
				throw new NotImplementedException($"Binary operator {expression.Operation} support missing from C# code generator");

			Visit(expression.Right);
		}

		protected override void Visit(CodeLambda method)
		{
			// XmlComment skipped, as C# doesn't support them on lambda functions

			WriteCustomAttributes(method.CustomAttributes, false);

			WriteModifiers(method.Attributes);

			// parameter list should be enclosed in brackets if there are more/less than 1 parameter or single
			// parameter has type
			var encloseParameters = !method.CanOmmitTypes || method.Parameters.Count != 1;
			if (encloseParameters) Write('(');
			WriteDelimitedList(method.Parameters, p => {
				if (!method.CanOmmitTypes)
				{
					Visit(p.Type);
					Write(' ');
				}

				Visit(p.Name);
			}, ", ", false);
			if (encloseParameters) Write(')');

			Write(" =>");

			WriteMethodBodyBlock(method.Body!, true, true, false, false);
		}

		protected override void Visit(CodeMember expression)
		{
			if (expression.Type != null)
			{
				Visit(expression.Type);
				Write('.');
			}

			// TODO: check if we can ommit "this" or it will result in name conflicts
			// for now ommit it always
			else if (expression.Instance != null && expression.Instance.ElementType != CodeElementType.This)
			{
				Visit(expression.Instance);
				Write('.');
			}

			Visit(expression.Member);
		}

		protected override void Visit(CodeNameOf nameOf)
		{
			Write("nameof(");
			Visit(nameOf.Expression);
			Write(")");
		}

		protected override void Visit(CodeRegion region)
		{
			// skip empty regions
			if (region.IsEmpty())
				return;

			Write("#region ");
			// remove any newline sequences to avoid invalid codegen
			WriteLine(string.Join(string.Empty, SplitByNewLine(region.Name)));

			WriteMemberGroups(region.Members);

			WriteLine("#endregion");
		}

		protected override void Visit(CodeConstant constant)
		{
			WriteLiteral(constant.Value, constant.Type.Type, constant.TargetTyped);
		}

		protected override void Visit(CodeAttribute attribute)
		{
			WriteAttributeType(attribute);

			if (attribute.Parameters.Count > 0 || attribute.NamedParameters.Count > 0)
			{
				Write('(');

				if (attribute.Parameters.Count > 0)
					WriteDelimitedList(attribute.Parameters, ", ", false);

				if (attribute.NamedParameters.Count > 0)
				{
					if (attribute.Parameters.Count > 0)
						Write(", ");

					WriteDelimitedList(
						attribute.NamedParameters,
						namedParam =>
						{
							Visit(namedParam.property);
							Write(" = ");
							Visit(namedParam.value);
						},
						", ",
						false);
				}

				Write(')');
			}
		}

		protected override void Visit(CodeComment comment)
		{
			if (comment.Inline)
			{
				// TODO: implement (not implemented as it is not used right now)
				throw new NotImplementedException($"Inline comment generation missing for C# code generator");
			}
			else
			{
				foreach (var line in SplitByNewLine(comment.Text))
				{
					Write("// ");
					WriteLine(line);
				}
			}
		}

		protected override void Visit(CodeEmptyLine line)
		{
			WriteLine();
		}

		protected override void Visit(CodeReference reference)
		{
			Visit(reference.Referenced.Name);
		}

		protected override void Visit(CodeMethod method)
		{
			if (method.XmlDoc != null)
				Visit(method.XmlDoc);

			WriteCustomAttributes(method.CustomAttributes, false);
			WriteModifiers(method.Attributes);

			if (method.ReturnType != null)
				Visit(method.ReturnType);
			else
				Write("void");

			Write(" ");

			Visit(method.Name);

			if (method.TypeParameters.Count > 0)
			{
				Write('<');
				WriteDelimitedList(method.TypeParameters, ", ", false);
				Write('>');
			}

			Write("(");
			if (method.Attributes.HasFlag(Modifiers.Extension))
				Write("this ");
			WriteDelimitedList(method.Parameters, ", ", false);
			Write(")");

			if (method.Attributes.HasFlag(Modifiers.Partial) && method.Body == null)
				WriteLine(";");
			else
				WriteMethodBodyBlock(method.Body!, false, method.ReturnType == null, false, true);
		}

		protected override void Visit(CodeParameter parameter)
		{
			if (parameter.Direction == ParameterDirection.Ref)
				Write("ref ");
			else if (parameter.Direction == ParameterDirection.Out)
				Write("out ");

			Visit(parameter.Type);
			Write(' ');
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
					WriteXmlAttribute(p.Name);
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

		protected override void Visit(CodeTypeInitializer cctor)
		{
			if (cctor.XmlDoc != null)
				Visit(cctor.XmlDoc);

			WriteCustomAttributes(cctor.CustomAttributes, false);
			Write("static ");

			Visit(cctor.Type.Name);

			Write("()");

			WriteMethodBodyBlock(cctor.Body!, false, true, false, true);
		}

		protected override void Visit(CodeConstructor ctor)
		{
			if (ctor.XmlDoc != null)
				Visit(ctor.XmlDoc);

			WriteCustomAttributes(ctor.CustomAttributes, false);
			WriteModifiers(ctor.Attributes);

			Visit(ctor.Class.Name);

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

			WriteMethodBodyBlock(ctor.Body!, false, true, false, true);
		}

		protected override void Visit(CodeThis expression)
		{
			Write("this");
		}

		protected override void Visit    (CodeCallStatement  call) => WriteCall(call);
		protected override void Visit    (CodeCallExpression call) => WriteCall(call);
		private            void WriteCall(CodeCallBase       call)
		{
			// TODO: check if we can ommit "this" or it will result in name conflicts
			if (call.Callee != null && call.Callee.ElementType != CodeElementType.This)
			{
				if (call.Extension)
					// TODO: here we could need () around parameter value if it is complex expression
					Visit(call.Parameters[0]);
				else
					Visit(call.Callee);
				Write('.');
			}

			Visit(call.MethodName);

			if (call.TypeArguments.Count > 0)
			{
				Write('<');
				WriteDelimitedList(call.TypeArguments, ", ", false);
				Write('>');
			}

			Write('(');
			WriteDelimitedList(call.Extension ? call.Parameters.Skip(1) : call.Parameters, ", ", false);
			Write(')');
		}

		protected override void Visit(CodeReturn statement)
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
			WriteModifiers(property.Attributes);

			Visit(property.Type);
			Write(' ');
			Visit(property.Name);

			var autoProperty = property.Getter == null && property.Setter == null;
			var withBrackets = autoProperty || property.Setter != null;

			var multiline = withBrackets && !autoProperty;

			if (multiline)
				WriteLine();
			else
				Write(' ');

			if (withBrackets)
				OpenBlock(!multiline);

			if (property.HasGetter)
			{
				if (property.Getter == null)
					Write("get;");
				else
				{
					Write("get");
					if (property.Getter.Items.Count == 1)
						Write(" =>");
					WriteMethodBodyBlock(property.Getter, true, false, true, true);
				}
			}

			if (property.HasSetter)
			{
				if (!multiline && property.HasGetter)
					Write(' ');

				if (property.Setter == null)
					Write("set;");
				else
				{
					Write("set");
					if (property.Setter.Items.Count == 1)
						Write(" =>");
					WriteMethodBodyBlock(property.Setter, true, true, true, true);
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

			if (property.Initializer != null)
			{
				Write(" = ");
				Visit(property.Initializer);
				Write(';');
			}
			else if (_useNRT && autoProperty && !property.Type.Type.IsNullable && !property.Type.Type.IsValueType)
				// suppress NRT error for non-nullable reference property
				// (we don't currently support tracking of property initialization by constructor)
				Write(" = null!;");

			if (property.TrailingComment != null)
			{
				Write(' ');
				Visit(property.TrailingComment);
			}

			WriteLine();
		}

		protected override void Visit(CodeNamespace @namespace)
		{
			_currentScope.AddRange(@namespace.Name);

			Write("namespace ");
			WriteDelimitedList(@namespace.Name, ".", false);
			WriteLine();
			OpenBlock(false);
			WriteMemberGroups(@namespace.Members);
			CloseBlock(false, true);

			_currentScope.RemoveRange(_currentScope.Count - @namespace.Name.Count, @namespace.Name.Count);
		}

		protected override void Visit(CodeClass @class)
		{
			if (@class.XmlDoc != null)
				Visit(@class.XmlDoc);

			WriteCustomAttributes(@class.CustomAttributes, false);
			WriteModifiers(@class.Attributes);
			Write("class ");
			Visit(@class.Name);

			// add current type name into parent identifiers scope collection only after custom attributes generated
			// as their naming scope doesn't include current type
			_currentScope.Add(@class.Name);

			var oldType = _currentType;
			_currentType = @class.Type;

			// generic class type arguments currently missing from AST (not used yet)

			if (@class.Implements.Count > 0 || @class.Inherits != null)
			{
				Write(" : ");
				if (@class.Inherits != null)
					Visit(@class.Inherits);

				for (var i = 0; i < @class.Implements.Count; i++)
				{ 
					if (i > 0 || @class.Inherits != null)
						Write(", ");
					Visit(@class.Implements[i]);
				}
			}

			WriteLine();

			OpenBlock(false);
			WriteMemberGroups(@class.Members);
			CloseBlock(false, true);

			_currentType = oldType;

			_currentScope.RemoveAt(_currentScope.Count - 1);
		}

		protected override void Visit(CodeIdentifier identifier)
		{
			WriteIdentifier(identifier.Name);
		}

		protected override void Visit(CodeTypeReference type)
		{
			RenderType(type.Type, null, false);
		}

		protected override void Visit(CodeTypeToken type)
		{
			RenderType(type.Type, null, true);
		}

		protected override void Visit(PropertyGroup group)
		{
			if (group.TableLayout)
				WritePropertiesAsTable(group.Members);
			else
				WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(MethodGroup group)
		{
			if (group.TableLayout)
				// TODO: Table layout support for methods
				VisitList(group.Members);
			else
				WriteNewLineDelimitedList(group.Members);
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

		protected override void Visit(CodeNew expression)
		{
			Write("new ");
			// TODO: we can add target-typed new support in future
			Visit(expression.Type);
			Write('(');
			WriteDelimitedList(expression.Parameters, ", ", false);
			Write(')');

			if (expression.Initializers.Count > 0)
			{
				WriteLine();
				OpenBlock(false);
				WriteDelimitedList(expression.Initializers, ",", true);
				CloseBlock(false, false);
			}
		}

		protected override void Visit          (CodeAssignmentStatement  statement ) => WriteAssignment(statement );
		protected override void Visit          (CodeAssignmentExpression expression) => WriteAssignment(expression);
		private            void WriteAssignment(CodeAssignmentBase       assignment)
		{
			Visit(assignment.LValue);
			Write(" = ");
			Visit(assignment.RValue);
		}

		protected override void Visit(FieldGroup group)
		{
			if (group.TableLayout)
				// TODO: not implemented as not used yet
				throw new NotImplementedException($"Table layout not implemented for fields in C# code generator");
			else
				WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(CodeField field)
		{
			WriteModifiers(field.Attributes);
			Visit(field.Type);
			Write(' ');
			Visit(field.Name);

			if (field.Initializer != null)
			{
				Write(" = ");
				Visit(field.Initializer);
			}

			// TODO: add NRT suppression initilizer support ( == null!)

			WriteLine(';');
		}

		protected override void Visit(CodeDefault expression)
		{
			Write("default");

			if (!expression.TargetTyped)
			{
				// default operator returns null for reference types always, so there is no reason to generate NRT annotation
				// for such types
				var type = expression.Type.Type;
				if (type.IsNullable && !type.IsValueType)
					type = type.WithNullability(false);

				Write('(');
				RenderType(type, null, true);
				Write(')');
			}

			if (_useNRT && !expression.Type.Type.IsNullable && !expression.Type.Type.IsValueType)
				Write('!');
		}

		protected override void Visit(CodeVariable expression)
		{
			if (expression.RValueTyped)
				Write("var ");
			else
			{
				Visit(expression.Type);
				Write(' ');
			}

			Visit(expression.Name);
		}

		protected override void Visit(CodeNewArray expression)
		{
			Write("new ");

			if (!expression.ValueTyped || expression.Values.Count == 0)
				Visit(expression.Type);

			if (expression.Values.Count == 0)
				// TODO: not used right now. Should generate Array.Empty when implemented
				throw new NotImplementedException($"Generation of new array without items not supported by C# code generator");
			{
				Write("[]");

				if (!expression.Inline)
					WriteLine();

				OpenBlock(expression.Inline);
				WriteDelimitedList(expression.Values, ",", !expression.Inline);
				CloseBlock(expression.Inline, false);
			}
		}

		protected override void Visit(CodeIndex expression)
		{
			Visit(expression.Object);
			Write('[');
			Visit(expression.Index);
			Write(']');
		}

		protected override void Visit(CodeTypeCast expression)
		{
			// TODO: add priority resolver to avoid ()-wrapper for value
			Write('(');
			Visit(expression.Type);
			Write(")(");
			Visit(expression.Value);
			Write(')');
		}

		// we don't generate retrhow right now (and it will require try-catch support too by AST)
		protected override void Visit     (CodeThrowStatement  statement ) => WriteThrow(statement );
		protected override void Visit     (CodeThrowExpression expression) => WriteThrow(expression);
		private            void WriteThrow(CodeThrowBase       @throw    )
		{
			Write("throw ");
			Visit(@throw.Exception);
		}

		protected override void Visit(PragmaGroup group)
		{
			WriteNewLineDelimitedList(group.Members);
		}
		#endregion

		#region reusable helpers
		/// <summary>
		/// Generate custom attributes list.
		/// </summary>
		/// <param name="customAttributes">Attributes to generate.</param>
		/// <param name="inline">
		/// <list type="bullet">
		/// <item><c>true</c>: all attributes generates inside same [] brackets in single line</item>
		/// <item><c>false</c>: each attribute generated inside own [] brackets on separate line</item>
		/// </list>
		/// </param>
		private void WriteCustomAttributes(IReadOnlyList<CodeAttribute> customAttributes, bool inline)
		{
			if (customAttributes.Count == 0)
				return;

			if (!inline)
			{
				foreach (var attr in customAttributes)
				{
					Write("[");
					Visit(attr);
					WriteLine("]");
				}
			}
			else
			{
				Write("[");
				WriteDelimitedList(customAttributes, ", ", false);
				Write("]");
			}
		}

		/// <summary>
		/// Generate type/member modifiers/attributes.
		/// </summary>
		/// <param name="attributes">Flags with type/member attributes and modifiers.</param>
		private void WriteModifiers(Modifiers attributes)
		{
			// note that we emit only modifiers we currently support in our AST model
			// also Modifiers.Extension handled not here but in method generator

			// access modifiers
			if (attributes.HasFlag(Modifiers.Public))
				Write("public ");
			else if (attributes.HasFlag(Modifiers.Private))
				Write("private ");

			if (attributes.HasFlag(Modifiers.Static))
				Write("static ");

			if (attributes.HasFlag(Modifiers.ReadOnly))
				Write("readonly ");

			if (attributes.HasFlag(Modifiers.Partial))
				Write("partial ");
		}

		/// <summary>
		/// Render opening curly bracket for block statement.
		/// </summary>
		/// <param name="inline">Indicates wether block is inline or multiline block.</param>
		private void OpenBlock(bool inline)
		{
			if (inline)
				Write("{ ");
			else
			{
				WriteLine("{");
				IncreaseIdent();
			}
		}

		/// <summary>
		/// Render closing curly bracket for block statement.
		/// </summary>
		/// <param name="inline">Indicates wether block is inline or multiline block.</param>
		/// <param name="newLine">Indicates that block should be followed by new line sequence. Used only when <paramref name="inline"/> is <c>false</c>.</param>
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

		/// <summary>
		/// Emits type name for custom attribute. Removes optional "Attribute" suffix.
		/// </summary>
		/// <param name="attribute">Custom attribute.</param>
		private void WriteAttributeType(CodeAttribute attribute)
		{
			CodeIdentifier? newTypeName = null;

			if (attribute.Type.Type.Name == null)
				throw new InvalidOperationException($"Invalid custom attribute type {attribute.Type.Type} ({attribute.Type.Type.Kind})");

			if (attribute.Type.Type.Name.Name.EndsWith("Attribute"))
				newTypeName = new CodeIdentifier(attribute.Type.Type.Name.Name.Substring(0, attribute.Type.Type.Name.Name.Length - 9));

			RenderType(attribute.Type.Type, newTypeName, true);
		}

		/// <summary>
		/// Generate code for groups of elements with new line as separator between groups.
		/// Skips empty groups, so no extra new lines will be generated for empty groups.
		/// </summary>
		/// <param name="groups">Element groups.</param>
		private void WriteMemberGroups(IReadOnlyCollection<IMemberGroup> groups)
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

		/// <summary>
		/// Writes C# identifier and generate verbatim identifier when it is needed.
		/// </summary>
		/// <param name="identifier">Identifier to generate.</param>
		private void WriteIdentifier(string identifier)
		{
			if (KeyWords.Contains(identifier))
				Write("@");

			Write(identifier);
		}

		/// <summary>
		/// Generates value literal including <c>null</c> literals, type hints when it is needed for proper
		/// value typing and enum literals.
		/// Supports only types, currently used by codegeneration.
		/// </summary>
		/// <param name="value">Literal value.</param>
		/// <param name="type">Literal type.</param>
		/// <param name="targetTyped">Indicate that literal is target-typed and type hints could be ommited if they are required for current type in general.</param>
		private void WriteLiteral(object? value, IType type, bool targetTyped)
		{
			if (value == null)
				Write("null");
			else if (value is bool boolean)
			{
				if (boolean)
					Write("true");
				else
					Write("false");
			}
			else if (value is int intValue)
				Write(intValue.ToString(NumberFormatInfo.InvariantInfo));
			else if (value is long longValue)
			{
				Write(longValue.ToString(NumberFormatInfo.InvariantInfo));

				// optional type hint
				if (!targetTyped)
					Write('L');
			}
			else if (value is string str)
			{
				Write('"');

				// we generate non-verbatim string literals only so we need to escape all newline characters
				// (in addition to other escape sequences)
				foreach (var chr in str)
				{
					switch (chr)
					{
						case '\0'    : Write("\\0");                  break;
						case '\t'    : Write("\\t");                  break;
						case '\\'    : Write("\\\\");                 break;
						case '"'     : Write("\\\"");                 break;

						// newlines
						case '\n'    : Write("\\n");                  break;
						case '\r'    : Write("\\r");                  break;
						case '\u0085':
						case '\u2028':
						case '\u2029': Write($"\\u{(ushort)chr:X4}"); break;

						default      : Write(chr);                    break;
					}
				}

				Write('"');
			}
			else if (value.GetType().IsEnum)
			{
				var enumType = value.GetType();

				// if enum value is a known enum field, we generate <ENUM>.<FIELD> literal
				// otherwise we generate underlying numeric value with type cast: (<ENUM>)<numeric_value>
				if (Enum.IsDefined(enumType, value))
				{
					// known enum member
					RenderType(type.WithNullability(false), null, false);
					Write('.');
					WriteIdentifier(Enum.GetName(enumType, value));
				}
				else
				{
					// TODO: generate shortcut for target-typed 0 value
					// TODO: support [Flags] enums
					// flags/arbitrary enum value
					Write('(');
					RenderType(type.WithNullability(false), null, false);
					Write(')');
					var underlyingType = Enum.GetUnderlyingType(enumType);
					WriteLiteral(Convert.ChangeType(value, underlyingType), _languageProvider.TypeParser.Parse(underlyingType), true);
				}
			}
			else
				throw new NotImplementedException($"Literal generation for type {value.GetType().Name} not implemented by C# code generator");
		}

		/// <summary>
		/// Generates method body block.
		/// </summary>
		/// <param name="statements">Method body.</param>
		/// <param name="preferInline">Prefer inline method body generation if possible.</param>
		/// <param name="allowEmpty">Indicate that empty method body is allowed or exception should be thrown.</param>
		/// <param name="inlineNeedsSemicolon">Indicate that when method body generated as inline statement, statement should be terminated by semicolon.</param>
		/// <param name="endWithNewLine">Indicate that method body should be terminated by new line sequence.</param>
		private void WriteMethodBodyBlock(
			CodeBlock statements,
			bool preferInline,
			bool allowEmpty,
			bool inlineNeedsSemicolon,
			bool endWithNewLine)
		{
			// empty body processing
			if (statements.Items.Count == 0)
			{
				if (!allowEmpty)
					throw new InvalidOperationException($"Emty code block encountered in unsuppored context");

				// generate empty block {} according to formatting parameters
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
			// single-statement body processing
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
				if (preferInline && stmt is CodeReturn ret)
				{
					// for inline return statement we ommit curly brackets and "return" keyword
					if (ret.Expression != null)
					{
						Visit(ret.Expression);
						if (!preferInline || inlineNeedsSemicolon)
							Write(';');
					}
					else if(preferInline)
						Write("{ }");
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
			// multi-statement body processing
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

		/// <summary>
		/// Render type token.
		/// </summary>
		/// <param name="type">Type descriptor.</param>
		/// <param name="nameOverride">Optional custom type name to use instead of original name.</param>
		/// <param name="typeOnlyContext">Indicates that type is rendered in type-only context. Otherwise it is rendered in expression context.</param>
		private void RenderType(IType type, CodeIdentifier? nameOverride, bool typeOnlyContext)
		{
			// this method use various visibility/name conflict checks to identify wether we need to generate
			// parent type prefix or namespace
			// those checks are by no means complete and probably will fail in some complex cases
			// but this is something to improve for later

			// identify wether it is necessary to generate parent type(s) for nested type
			// skip generation for aliased types as they cannot be nested
			if (type.Alias == null && type.Parent != null)
			{
				// skip parent types generation for types, directly nested into current class
				// (class we currently generate AKA _currentType, not type we pass into this method)
				// or parent classes of current class
				// otherwise don't generate parent type name for type that present in current visibility scope
				var t = _currentType;
				var skipParentGeneration = false;
				while (t != null)
				{
					// we use by-refrence comparison here as we don't create type duplicates for classes, defined
					// in current AST (if this will happen, this comparison definitely will fails)
					skipParentGeneration = t == type.Parent;
					if (skipParentGeneration)
						break;
					t = t.Parent;
				}

				if (!skipParentGeneration)
				{
					RenderType(type.Parent, null, typeOnlyContext);
					Write('.');
				}
			}

			// identify wether we need to generate type namespace
			// skip generation for aliased types
			if (type.Alias == null && type.Namespace != null)
			{
				var generateNamespace = false;
				var typeName = nameOverride ?? type.Name!;

				// check if type with such name exists in multiple namespaces
				if (_knownTypes.TryGetValue(typeName, out var namespaces))
					// multiple namespaces || type with same name in other namespace
					// second check could look unnecessary as in that case namespaces could have at least two items
					// but actually it is valid case when nameOverride specified
					generateNamespace = namespaces.Count > 1 || !namespaces.Contains(type.Namespace);

				// check that current type name doesn't conflict with names in current context
				// such as namespaces, type members
				if (!generateNamespace)
				{
					// check current and parent scopes for name conflicts
					for (var i = _currentScope.Count - 1; i >= 0; i--)
					{
						// type is from current scope, do nothing
						if (_languageProvider.FullNameEqualityComparer.Equals(type.Namespace, _currentScope.Take(i)) && typeOnlyContext)
							break;

						// type is not from current scope (by check above), but current scope contains such name
						// or current scope have same name as type for type used in expressions
						if (_scopedNames[_currentScope].Contains(typeName)
							|| (!typeOnlyContext && _languageProvider.IdentifierEqualityComparer.Equals(_currentScope[i], typeName)))
						{
							// name conflict detected
							generateNamespace = true;
							break;
						}
					}
				}

				if (!generateNamespace)
				{
					// check namespaced type name conflict with top-level naming scope
					if (_scopedNames.TryGetValue(Array.Empty<CodeIdentifier>(), out var names) && names.Contains(typeName))
						generateNamespace = true;
					else if (_currentImports != null)
					{
						// check that type name doesn't conflict with names in imported namespaces
						// checks only names we aware of, so for other conflicting types user need to provide them
						// in settings so they will be added to _scopedNames
						foreach (var import in _currentImports)
						{
							// ignore self
							if (!_languageProvider.FullNameEqualityComparer.Equals(import.Namespace, type.Namespace))
							{
								if (_scopedNames.TryGetValue(import.Namespace, out names) && names.Contains(typeName))
								{
									generateNamespace = true;
									break;
								}
							}
						}
					}
				}

				if (generateNamespace)
				{
					// TODO: detect how many namespace levels to generate
					// for now we generate full namespace
					foreach (var part in type.Namespace)
					{
						WriteIdentifier(part.Name);
						Write('.');
					}
				}
			}

			// type-kind specific generation
			switch (type.Kind)
			{
				case TypeKind.Regular:
				{
					// for regular type generate name or alias
					var regType = (RegularType)type;
					if (regType.Alias != null)
						// for alias we emit type name as is, without verbatim identifier check
						Write(regType.Alias);
					else
						Visit(nameOverride ?? regType.Name);
					break;
				}
				case TypeKind.Generic:
				{
					// for generic type we generate type name and type arguments
					var regType = (GenericType)type;
					Visit(nameOverride ?? regType.Name);
					Write('<');
					WriteDelimitedList(type.TypeArguments!, t => RenderType(t, null, true), ", ", false);
					Write('>');
					break;
				}
				case TypeKind.Array:
				{
					// for array we render element type name and array sizes
					RenderType(type.ArrayElementType!, null, typeOnlyContext);
					Write('[');
					var first = true;
					foreach (var size in type.ArraySizes!)
					{
						if (first)
							first = false;
						else
							Write(',');
						if (size != null)
							WriteLiteral(size.Value, _languageProvider.TypeParser.Parse<int>(), false);
					}
					Write(']');
					break;
				}
				case TypeKind.TypeArgument:
				{
					// for generic type argument we render type name
					Visit(type.Name!);
					break;
				}
				// not used now
				case TypeKind.Dynamic    :
				case TypeKind.OpenGeneric:
				default:
					throw new NotImplementedException($"Type {type.Kind} support not implemented in C# code generator");
			}

			// generate type nullability annotation
			if (type.IsNullable && (type.IsValueType || _useNRT))
				Write('?');
		}

		// constants for attributes table layout columns
		private const string ATTR_TABLE_ATTRIBUTES_GROUP       = "attributes";
		private const string ATTR_TABLE_TYPE_COLUMN            = "type";
		private const string ATTR_TABLE_PARAMETERS_GROUP       = "parameters";
		private const string ATTR_TABLE_NAMED_PARAMETERS_GROUP = "named_parameters";
		private const string ATTR_TABLE_NAMED_PROPERTY_COLUMN  = "name";
		private const string ATTR_TABLE_PARAMETER_VALUE        = "value";

		/// <summary>
		/// Generates attributes for code group of group owners (e.g. properties) using 'table' layout.
		/// </summary>
		/// <typeparam name="T">Attributes owner type.</typeparam>
		/// <param name="owners">Attributes owners.</param>
		/// <returns>Array of string with each string containing attributes code for corresponding attributes owner.</returns>
		private IReadOnlyList<string> BuildAttributesAsTable<T>(List<T> owners)
			where T : AttributeOwner
		{
			var tableBuilder = new TableLayoutBuilder();

			tableBuilder
				.Layout()
					// group of attributes, enclosed in [] with comma as separator
					.Group(ATTR_TABLE_ATTRIBUTES_GROUP, "[", ", ", "] ")
						// attribute type name column
						.Column(ATTR_TABLE_TYPE_COLUMN)
						// open bracket generated only if there is non-empty column in next 3 columns
						.Fixed("(", 0, 3)
						// group of attribute constructor parameters, separated by comma
						.Group(ATTR_TABLE_PARAMETERS_GROUP, null, ", ", null)
							.Column(ATTR_TABLE_PARAMETER_VALUE)
						.EndGroup()
						// separator between parameters and named parameters, generated if previous and next columns are not empty
						.Fixed(", ", 1, 1)
						// group of attribute named parameters, separated by comma
						.Group(ATTR_TABLE_NAMED_PARAMETERS_GROUP, null, ", ", null)
							.Column(ATTR_TABLE_NAMED_PROPERTY_COLUMN)
							.Fixed(" = ", 0, 0)
							.Column(ATTR_TABLE_PARAMETER_VALUE)
						.EndGroup()
						// open bracket generated only if there is non-empty column in previous 3 columns
						.Fixed(")", 3, 0)
					.EndGroup()
				.End();

			foreach (var owner in owners)
			{
				var row = tableBuilder.DataRow();

				if (owner.CustomAttributes.Count > 0)
				{
					var rowAttributes = row.Group(ATTR_TABLE_ATTRIBUTES_GROUP);

					foreach (var attribute in owner.CustomAttributes)
					{
						var attributeBuilder = rowAttributes.NewGroup();

						attributeBuilder.ColumnValue(ATTR_TABLE_TYPE_COLUMN, BuildFragment(b => WriteAttributeType(attribute)));

						if (attribute.Parameters.Count > 0 || attribute.NamedParameters.Count > 0)
						{
							if (attribute.Parameters.Count > 0)
							{
								var regularParameters = attributeBuilder.Group(ATTR_TABLE_PARAMETERS_GROUP);

								foreach (var parameter in attribute.Parameters)
									regularParameters
										.NewGroup()
										.ColumnValue(ATTR_TABLE_PARAMETER_VALUE, BuildFragment(b => b.Visit(parameter)));
							}
							if (attribute.NamedParameters.Count > 0)
							{
								var namedParameters = attributeBuilder.Group(ATTR_TABLE_NAMED_PARAMETERS_GROUP);

								foreach (var (property, value) in attribute.NamedParameters)
									namedParameters
										.NewGroup()
										.ColumnValue(ATTR_TABLE_NAMED_PROPERTY_COLUMN, BuildFragment(b => b.Visit(property)))
										.ColumnValue(ATTR_TABLE_PARAMETER_VALUE, BuildFragment(b => b.Visit(value)));
							}
						}
					}
				}
			}

			return tableBuilder.GetRows();
		}

		// constants for property group table layout columns
		private const string PROP_TABLE_MODIFIER_COLUMN       = "modifier";
		private const string PROP_TABLE_TYPE_COLUMN           = "type";
		private const string PROP_TABLE_NAME_COLUMN           = "name";
		private const string PROP_TABLE_OPEN_BRACKETS_COLUMN  = "open_brackets";
		private const string PROP_TABLE_CLOSE_BRACKETS_COLUMN = "close_brackets";
		private const string PROP_TABLE_GETTER_COLUMN         = "getter";
		private const string PROP_TABLE_SETTER_COLUMN         = "setter";
		private const string PROP_TABLE_INITIALIZER_COLUMN    = "initializer";

		/// <summary>
		/// Generate code for property group using 'table' layout.
		/// </summary>
		/// <param name="properties">Property group.</param>
		private void WritePropertiesAsTable(List<CodeProperty> properties)
		{
			if (properties.Count == 0)
				return;

			// we generate properties layout without:
			// - attributes as attributes table generated by separate reusable method
			// - trailing comment as we not align it vertically and it simplifies newline generation
			var tableBuilder = new TableLayoutBuilder();

			tableBuilder
				.Layout()
					.Column(PROP_TABLE_MODIFIER_COLUMN      ) // access modifiers
					.Column(PROP_TABLE_TYPE_COLUMN          ) // property type
					.Column(PROP_TABLE_NAME_COLUMN          ) // property name
					.Column(PROP_TABLE_OPEN_BRACKETS_COLUMN ) // { bracket for properties with setter or with complex getter
					.Column(PROP_TABLE_GETTER_COLUMN        ) // getter code (or default "get")
					.Column(PROP_TABLE_SETTER_COLUMN        ) // setter code (or default "set")
					.Column(PROP_TABLE_CLOSE_BRACKETS_COLUMN) // } bracket for properties with setter or with complex getter
					.Column(PROP_TABLE_INITIALIZER_COLUMN   ) // optional property initializer
				.End();

			foreach (var property in properties)
			{
				var row = tableBuilder.DataRow();

				row.ColumnValue(PROP_TABLE_MODIFIER_COLUMN, BuildFragment(b => b.WriteModifiers(property.Attributes)));

				row.ColumnValue(PROP_TABLE_TYPE_COLUMN, BuildFragment(b =>
				{
					b.Visit(property.Type);
					// padding also could be implemented using fixed column
					Write(' ');
				}));

				row.ColumnValue(PROP_TABLE_NAME_COLUMN, BuildFragment(b => b.Visit(property.Name)));

				var needsBrackets = property.HasGetter && property.HasSetter;

				row.ColumnValue(PROP_TABLE_OPEN_BRACKETS_COLUMN, BuildFragment(b =>
				{
					// includes padding after name
					if (needsBrackets)
						b.Write(" { ");
				}));

				// includes padding after name if no brackets used
				row.ColumnValue(PROP_TABLE_GETTER_COLUMN, !property.HasGetter ? string.Empty : BuildFragment(b =>
				{
					if (property.Getter == null)
						Write("get;");
					else
					{
						if (property.HasSetter)
						{
							Write("get");

							if (property.Getter.Items.Count == 1)
								Write(" =>");
						}
						else
							Write(" =>");

						b.WriteMethodBodyBlock(property.Getter, true, false, true, false);
					}
				}));

				row.ColumnValue(PROP_TABLE_CLOSE_BRACKETS_COLUMN, BuildFragment(b =>
				{
					if (needsBrackets)
						b.Write(" }");
				}));

				row.ColumnValue(PROP_TABLE_SETTER_COLUMN, !property.HasSetter ? string.Empty : BuildFragment(b =>
				{
					if (property.HasGetter)
						Write(' ');

					if (property.Setter == null)
						Write("set;");
					else
					{
						Write("set");

						if (property.Setter.Items.Count == 1)
							Write(" =>");

						b.WriteMethodBodyBlock(property.Setter, true, true, true, false);
					}
				}));

				row.ColumnValue(PROP_TABLE_INITIALIZER_COLUMN, BuildFragment(b =>
				{
					if (property.Initializer != null)
					{
						// includes padding after getter/setter block
						Write(" = ");
						Visit(property.Initializer);
						Write(';');
					}
					else if (_useNRT && property.Getter == null && property.Setter == null && !property.Type.Type.IsNullable && !property.Type.Type.IsValueType)
						Write(" = null!;");
				}));
			}

			var propertiesCode = tableBuilder.GetRows();
			var attributes     = BuildAttributesAsTable(properties);

			// write generated code : xml-doc, attributes, property itself and optional trailing comment
			for (var i = 0; i < properties.Count; i++)
			{
				var prop = properties[i];

				if (prop.XmlDoc != null)
					Visit(prop.XmlDoc);

				Write(attributes[i]);
				Write(propertiesCode[i]);

				if (prop.TrailingComment != null)
				{
					Write(' ');
					Visit(prop.TrailingComment);
				}
				else
					WriteLine();
			}
		}
		#endregion
	}
}
