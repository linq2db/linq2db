using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Code generation visitor for F# language.
	/// </summary>
	/// <remarks>
	/// F# is whitespace-significant and has no braces, no partial types and no C#-style properties, so this
	/// generator diverges from <see cref="CSharpCodeGenerator"/> in several places:
	/// <list type="bullet">
	/// <item>the file body is emitted under a single <c>namespace rec</c> so declaration order and mutually
	/// recursive associations don't matter;</item>
	/// <item>entity classes marked with <see cref="Modifiers.Record"/> are emitted as F# records
	/// (<c>type X = { ... }</c>) with attributes on fields;</item>
	/// <item>nullable columns are rendered as <c>'T option</c>.</item>
	/// </list>
	/// The data-model generator's F# path (see <c>DataModelGenerator</c>) is responsible for producing an AST
	/// shape that only uses the nodes implemented here; genuinely unsupported nodes throw, matching the C#
	/// generator's own handling of unimplemented cases.
	/// </remarks>
	internal sealed class FSharpCodeGenerator : CodeGenerationVisitor<FSharpCodeGenerator>
	{
		/// <summary>
		/// Code fragments for unary operators.
		/// </summary>
		private static readonly IReadOnlyDictionary<UnaryOperation, string> _unaryOperators = new Dictionary<UnaryOperation, string>()
		{
			{ UnaryOperation.Not, "not " },
		};

		/// <summary>
		/// Code fragments for binary operators.
		/// </summary>
		private static readonly IReadOnlyDictionary<BinaryOperation, string> _operators = new Dictionary<BinaryOperation, string>()
		{
			{ BinaryOperation.Equal   , " = "  },
			{ BinaryOperation.NotEqual, " <> " },
			{ BinaryOperation.And     , " && " },
			{ BinaryOperation.Or      , " || " },
			{ BinaryOperation.Add     , " + "  },
		};

		// newline sequences (same set recognized by F# lexer as C#)
		private static readonly string[] _newLines = new []{ "\x000D\x000A", "\x000A", "\x000D", "\x0085", "\x2028", "\x2029" };

		// F# keywords and reserved words.
		// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/keyword-reference
		// Names matching one of these are escaped with double-backticks by WriteIdentifier.
		private static readonly HashSet<string> KeyWords = new (StringComparer.Ordinal)
		{
			"abstract", "and"     , "as"       , "assert"   , "base"    , "begin"   , "class"    , "default"  , "delegate" , "do",
			"done"    , "downcast", "downto"   , "elif"     , "else"    , "end"     , "exception", "extern"   , "false"    , "finally",
			"fixed"   , "for"     , "fun"      , "function" , "global"  , "if"      , "in"       , "inherit"  , "inline"   , "interface",
			"internal", "lazy"    , "let"      , "match"    , "member"  , "module"  , "mutable"  , "namespace", "new"      , "not",
			"null"    , "of"      , "open"     , "or"       , "override", "private" , "public"   , "rec"      , "return"   , "sig",
			"static"  , "struct"  , "then"     , "to"       , "true"    , "try"     , "type"     , "upcast"   , "use"      , "val",
			"void"    , "when"    , "while"    , "with"     , "yield"   ,
			// reserved for future use
			"atomic"  , "break"   , "checked"  , "component", "const"   , "constraint", "continue", "eager"   , "event"    , "external",
			"functor" , "include" , "measure"  , "method"   , "mixin"   , "object"  , "parallel" , "process"  , "protected", "pure",
			"recursive", "sealed" , "tailcall" , "trait"    , "virtual" , "volatile",
		};

		private readonly List<CodeIdentifier>                                                   _currentScope = new();
#pragma warning disable IDE0052 // Remove unread private members
		// F# has no nullable reference types; the NRT flag is accepted for interface parity but not used
		private readonly bool                                                                   _useNRT;
		private readonly IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> _knownTypes;
#pragma warning restore IDE0052 // Remove unread private members
		private readonly ILanguageProvider                                                      _languageProvider;
		private readonly IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> _scopedNames;
		private readonly IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> _scopedTypes;

		// current type if current code-generation position is inside of type
		private IType? _currentType;
		// indicates that the enclosing type is an F# record, so properties are emitted as record fields
		private bool   _currentIsRecord;
		// indicates that the enclosing type is a static class rendered as an [<Extension>] type, so methods
		// are emitted as `[<Extension>] static member` rather than instance members
		private bool   _currentIsExtensionType;
		// file imports, emitted after the `namespace` declaration (F# requires namespace before `open`)
		private IReadOnlyList<CodeImport>? _fileImports;

		public FSharpCodeGenerator(
			ILanguageProvider                                                      languageProvider,
			string                                                                 newLine,
			string                                                                 indent,
			bool                                                                   useNRT,
			IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedNames,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedTypes)
			: base(newLine, indent)
		{
			_languageProvider = languageProvider;
			_useNRT           = useNRT;
			_knownTypes       = knownTypes;
			_scopedNames      = scopedNames;
			_scopedTypes      = scopedTypes;
		}

		protected override string[] NewLineSequences => _newLines;

		#region AST nodes visitors
		protected override void Visit(CodeImport import)
		{
			Write("open ");
			WriteDelimitedList(import.Namespace, ".", false);
			WriteLine();
		}

		protected override void Visit(CodePragma pragma)
		{
			switch (pragma.PragmaType)
			{
				case PragmaType.NullableEnable:
					// F# has no nullable reference types; nothing to emit
					break;
				case PragmaType.DisableWarning:
					WriteUnindented("#nowarn");
					foreach (var warn in pragma.Parameters)
					{
						Write(" \"");
						Write(string.Join(string.Empty, SplitByNewLine(warn)));
						Write('"');
					}

					WriteLine();
					break;
				default:
					throw new NotImplementedException($"Codegeneration for pragma {pragma.PragmaType} not implemented in F# code generator");
			}
		}

		protected override void Visit(CodeFile file)
		{
			VisitList(file.Header);
			WriteLine();

			// F# requires the `namespace` declaration before any `open` statements, so imports are emitted by
			// the namespace visitor rather than here.
			_fileImports = file.Imports;
			VisitList(file);
		}

		protected override void Visit(CodeUnary expression)
		{
			if (_unaryOperators.TryGetValue(expression.Operation, out var operatorCode))
				Write(operatorCode);
			else
				throw new NotImplementedException($"Unary operator {expression.Operation} support missing from F# code generator");

			Visit(expression.Argument);
		}

		protected override void Visit(CodeBinary expression)
		{
			Visit(expression.Left);

			if (_operators.TryGetValue(expression.Operation, out var operatorCode))
				Write(operatorCode);
			else
				throw new NotImplementedException($"Binary operator {expression.Operation} support missing from F# code generator");

			Visit(expression.Right);
		}

		protected override void Visit(CodeTernary expression)
		{
			Write("(if ");
			Visit(expression.Condition);
			Write(" then ");
			Visit(expression.True);
			Write(" else ");
			Visit(expression.False);
			Write(")");
		}

		protected override void Visit(CodeLambda method)
		{
			// F# lambda: fun p1 p2 -> body
			Write("fun ");
			WriteDelimitedList(method.Parameters, p => Visit(p.Name), " ", false);
			Write(" -> ");

			WriteMethodBodyExpression(method.Body);
		}

		protected override void Visit(CodeMember expression)
		{
			if (expression.Type != null)
			{
				if (expression.Type.Type.Name == null || _currentType != expression.Type.Type)
				{
					Visit(expression.Type);
					Write('.');
				}
			}
			else if (expression.Instance != null && expression.Instance.ElementType != CodeElementType.This)
			{
				Visit(expression.Instance);
				Write('.');
			}
			else if (expression.Instance != null && expression.Instance.ElementType == CodeElementType.This)
			{
				Write("this.");
			}

			Visit(expression.Member);
		}

		protected override void Visit(CodeNameOf nameOf)
		{
			// F# nameof() cannot reference an instance member through its declaring type
			// (e.g. nameof(Person.PersonId)) and is not accepted in attribute-argument position, which is where
			// the scaffolder uses it (association ThisKey/OtherKey). Emit the equivalent compile-time string
			// literal instead - the produced value is identical.
			var rendered = BuildFragment(() => Visit(nameOf.Expression));
			var dot      = rendered.LastIndexOf('.');
			var name     = (dot >= 0 ? rendered.Substring(dot + 1) : rendered).Replace("`", "", StringComparison.Ordinal);

			Write('"');
			Write(name);
			Write('"');
		}

		protected override void Visit(CodeRegion region)
		{
			// F# has no #region; just emit contained members
			if (region.IsEmpty())
				return;

			WriteMemberGroups(region.Members);
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
							Visit(namedParam.Property);
							Write(" = ");
							Visit(namedParam.Value);
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
				Write("(* ");
				// string.Join(char, ...) is unavailable on netstandard2.0, use the string-separator overload
#pragma warning disable MA0089
				Write(string.Join(" ", SplitByNewLine(comment.Text)));
#pragma warning restore MA0089
				Write(" *)");
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

			// F# member declaration. Inside an [<Extension>] type, C#-style extension methods are rendered as
			// `[<Extension>] static member` so they remain callable as instance methods (e.g. table.Find(pk))
			// from both F# and C#. Other static methods are plain `static member`; instance methods use
			// `member this.`.
			if (_currentIsExtensionType && method.Attributes.HasFlag(Modifiers.Extension))
			{
				WriteLine("[<System.Runtime.CompilerServices.Extension>]");
				Write("static member ");
				WriteAccessibility(method.Attributes);
				WriteIdentifier(method.Name.Name);
			}
			else if (method.Attributes.HasFlag(Modifiers.Static))
			{
				Write("static member ");
				WriteAccessibility(method.Attributes);
				WriteIdentifier(method.Name.Name);
			}
			else
			{
				Write("member ");
				WriteAccessibility(method.Attributes);
				Write("this.");
				WriteIdentifier(method.Name.Name);
			}

			if (method.TypeParameters.Count > 0)
			{
				Write('<');
				WriteDelimitedList(method.TypeParameters, ", ", false);
				Write('>');
			}

			Write(" (");
			WriteDelimitedList(method.Parameters, ", ", false);
			Write(")");

			if (method.ReturnType != null)
			{
				Write(" : ");
				Visit(method.ReturnType);
			}

			Write(" =");

			WriteMethodBodyBlock(method.Body, method.ReturnType == null);
		}

		protected override void Visit(CodeParameter parameter)
		{
			// F# parameter: name : type
			Visit(parameter.Name);
			Write(" : ");
			Visit(parameter.Type);

			// C# default parameter values (e.g. CancellationToken = default) have no direct F# member-parameter
			// equivalent; the parameter is emitted as required. The produced method is fully usable, callers just
			// pass the argument explicitly.
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
			// F# uses 'static do' / static let for type initialization; not emitted by the F# model path yet
			throw new NotImplementedException("Static type initializers are not yet supported by F# code generator");
		}

		protected override void Visit(CodeConstructor ctor)
		{
			// Additional (non-primary) constructors are emitted by the model path via 'new(...)'; the primary
			// constructor is rendered inline on the type declaration (see Visit(CodeClass)).
			throw new NotImplementedException("Standalone constructor generation is not yet supported by F# code generator");
		}

		protected override void Visit(CodeThis expression)
		{
			Write("this");
		}

		protected override void Visit    (CodeCallStatement  call) => WriteCall(call);
		protected override void Visit    (CodeCallExpression call) => WriteCall(call);
		private            void WriteCall(CodeCallBase       call)
		{
			var hasCallee = call.Callee != null && call.Callee.ElementType != CodeElementType.This;
			if (hasCallee)
			{
				if (call.Extension)
					Visit(call.Parameters[0]);
				else
					Visit(call.Callee!);

				Write('.');
			}
			else if (call.Callee != null && call.Callee.ElementType == CodeElementType.This)
			{
				Write("this.");
			}

			Visit(call.MethodName);

			if (!call.CanSkipTypeArguments && call.TypeArguments.Count > 0)
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
			// F# is expression-based; a return is just the expression value
			if (statement.Expression != null)
				Visit(statement.Expression);
			else
				Write("()");
		}

		protected override void Visit(CodeProperty property)
		{
			if (_currentIsRecord)
			{
				WriteRecordField(property);
				return;
			}

			// class member property
			if (property.XmlDoc != null)
				Visit(property.XmlDoc);

			WriteCustomAttributes(property.CustomAttributes, false);

			Write("member ");
			WriteAccessibility(property.Attributes);
			Write("this.");
			Visit(property.Name);

			if (property.HasSetter)
			{
				// mutable auto-property
				Write(" : ");
				Visit(property.Type);
				Write(" with get, set");
				WriteLine();
			}
			else
			{
				// read-only computed property
				Write(" =");
				WriteMethodBodyBlock(property.Getter, false);
			}
		}

		private void WriteRecordField(CodeProperty property)
		{
			if (property.XmlDoc != null)
				Visit(property.XmlDoc);

			WriteCustomAttributes(property.CustomAttributes, true);
			if (property.CustomAttributes.Count > 0)
				Write(' ');

			// record fields are immutable; linq2db materializes F# records via their primary constructor
			// (FSharpEntityBindingInterceptor). A [<CLIMutable>]/mutable toggle is future work.
			Visit(property.Name);
			Write(" : ");
			Visit(property.Type);

			if (property.TrailingComment != null)
			{
				Write(' ');
				Visit(property.TrailingComment);
			}
			else
				WriteLine();
		}

		protected override void Visit(CodeNamespace @namespace)
		{
			_currentScope.AddRange(@namespace.Name);

			// F# namespaces have no braces and no indentation, and 'rec' allows any declaration order and
			// mutually recursive types (associations) within the namespace.
			Write("namespace rec ");
			WriteDelimitedList(@namespace.Name, ".", false);
			WriteLine();
			WriteLine();

			// emit file-level imports right after the namespace declaration
			if (_fileImports != null)
			{
				VisitList(_fileImports);
				WriteLine();
				_fileImports = null;
			}

			WriteMemberGroups(@namespace.Members);

			_currentScope.RemoveRange(_currentScope.Count - @namespace.Name.Count, @namespace.Name.Count);
		}

		protected override void Visit(CodeClass @class)
		{
			var isRecord        = @class.Attributes.HasFlag(Modifiers.Record);
			// a static (non-record) class is rendered as an F# [<Extension>] type holding static members
			var isExtensionType = !isRecord && @class.Attributes.HasFlag(Modifiers.Static);

			if (@class.XmlDoc != null)
				Visit(@class.XmlDoc);

			WriteCustomAttributes(@class.CustomAttributes, false);

			if (isExtensionType)
				WriteLine("[<System.Runtime.CompilerServices.Extension>]");

			// entity records can contain non-comparable fields (obj, byte[], collection/entity associations);
			// F# would otherwise try to derive structural comparison and fail (FS1178). Structural equality is
			// retained; ordering is not meaningful for entities.
			if (isRecord)
				WriteLine("[<NoComparison>]");

			Write("type ");
			WriteAccessibility(@class.Attributes);
			Visit(@class.Name);

			_currentScope.Add(@class.Name);

			var oldType             = _currentType;
			var oldIsRecord         = _currentIsRecord;
			var oldIsExtensionType  = _currentIsExtensionType;
			_currentType            = @class.Type;
			_currentIsRecord        = isRecord;
			_currentIsExtensionType = isExtensionType;

			if (isRecord)
			{
				WriteLine(" =");
				IncreaseIdent();
				WriteLine("{");
				IncreaseIdent();

				// emit every record field, including association fields nested inside regions, into the record
				// body (F# records require all fields inside the { } block). Each field emits its own newline.
				foreach (var property in @class.Members.EnumerateMembers<PropertyGroup, CodeProperty>())
					Visit(property);

				DecreaseIdent();
				WriteLine("}");

				// method members (e.g. custom equality) are rendered after the field block as record members
				var methodGroups = @class.Members.EnumerateMemberGroups<MethodGroup>().ToList();
				if (methodGroups.Exists(static g => !g.IsEmpty))
				{
					WriteLine();
					foreach (var group in methodGroups)
						if (!group.IsEmpty)
							Visit(group);
				}

				DecreaseIdent();
			}
			else if (isExtensionType)
			{
				// static class -> F# [<Extension>] type holding static (extension) members
				WriteLine(" =");
				IncreaseIdent();
				WriteMemberGroups(@class.Members);
				DecreaseIdent();
			}
			else
			{
				// F# class: pull the (single) constructor into the primary-constructor position and render
				// `inherit Base(args)` from its base-call arguments. Additional constructors are gated off for
				// F# by the model path for now (a single typed-options constructor is generated).
				CodeConstructor? primaryCtor = null;
				foreach (var ctorGroup in @class.Members.OfType<ConstructorGroup>())
				{
					foreach (var ctor in ctorGroup.Members)
					{
						primaryCtor = ctor;
						break;
					}

					if (primaryCtor != null)
						break;
				}

				if (primaryCtor != null)
				{
					Write(" (");
					WriteDelimitedList(primaryCtor.Parameters, ", ", false);
					Write(")");
				}

				WriteLine(" =");
				IncreaseIdent();

				if (@class.Inherits != null)
				{
					Write("inherit ");
					Visit(@class.Inherits);
					if (primaryCtor != null && primaryCtor.BaseArguments.Count > 0)
					{
						Write("(");
						WriteDelimitedList(primaryCtor.BaseArguments, ", ", false);
						Write(")");
					}

					WriteLine();
				}

				// constructor body statements (initializers) rendered as `do` bindings
				if (primaryCtor?.Body != null && primaryCtor.Body.Items.Count > 0)
				{
					foreach (var stmt in primaryCtor.Body.Items)
					{
						Write("do ");
						Visit(stmt);
						WriteLine();
					}
				}

				// render members except constructors (already rendered as the primary constructor)
				WriteMemberGroups(@class.Members.Where(g => g is not ConstructorGroup).ToList());

				DecreaseIdent();
			}

			WriteLine();

			_currentIsExtensionType = oldIsExtensionType;
			_currentIsRecord        = oldIsRecord;
			_currentType            = oldType;

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
			// each property emits its own terminating newline
			VisitList(group.Members);
		}

		protected override void Visit(MethodGroup group)
		{
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
			// F# object construction: TypeName(args)
			Visit(expression.Type);
			Write('(');
			WriteDelimitedList(expression.Parameters, ", ", false);
			Write(')');

			if (expression.Initializers.Count > 0)
				throw new NotImplementedException("Object initializers are not supported by F# code generator");
		}

		protected override void Visit          (CodeAssignmentStatement  statement ) => WriteAssignment(statement );
		protected override void Visit          (CodeAssignmentExpression expression) => WriteAssignment(expression);
		private            void WriteAssignment(CodeAssignmentBase       assignment)
		{
			Visit(assignment.LValue);
			Write(" <- ");
			Visit(assignment.RValue);
		}

		protected override void Visit(FieldGroup group)
		{
			WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(CodeField field)
		{
			// F# type field: 'let mutable name : type = init' inside a class, or a record field elsewhere
			if (_currentIsRecord)
			{
				if (field.Attributes.HasFlag(Modifiers.Static))
					throw new NotImplementedException("Static fields are not supported on F# records");

				Visit(field.Name);
				Write(" : ");
				Visit(field.Type);
				WriteLine();
				return;
			}

			Write(field.Attributes.HasFlag(Modifiers.Static) ? "static val mutable " : "let mutable ");
			Visit(field.Name);
			Write(" : ");
			Visit(field.Type);

			if (field.Initializer != null)
			{
				Write(" = ");
				Visit(field.Initializer);
			}

			WriteLine();
		}

		protected override void Visit(CodeDefault expression)
		{
			Write("Unchecked.defaultof<");
			RenderType(expression.Type.Type, null, true);
			Write('>');
		}

		protected override void Visit(CodeVariable expression)
		{
			Write("let mutable ");
			Visit(expression.Name);
		}

		protected override void Visit(CodeNewArray expression)
		{
			// F# array literal: [| v1; v2 |]
			Write("[| ");
			WriteDelimitedList(expression.Values, "; ", false);
			Write(" |]");
		}

		protected override void Visit(CodeIndex expression)
		{
			Visit(expression.Object);
			Write(".[");
			Visit(expression.Index);
			Write(']');
		}

		protected override void Visit(CodeTypeCast expression)
		{
			Write('(');
			Visit(expression.Value);
			Write(" :> ");
			Visit(expression.Type);
			Write(')');
		}

		protected override void Visit(CodeAsOperator expression)
		{
			// F# safe downcast-ish; not emitted by the F# model path
			throw new NotImplementedException("'as' operator is not supported by F# code generator");
		}

		protected override void Visit(CodeSuppressNull expression)
		{
			// F# has no NRT suppression operator
			Visit(expression.Value);
		}

		protected override void Visit     (CodeThrowStatement  statement ) => WriteThrow(statement );
		protected override void Visit     (CodeThrowExpression expression) => WriteThrow(expression);
		private            void WriteThrow(CodeThrowBase       @throw    )
		{
			Write("raise ");
			Write('(');
			Visit(@throw.Exception);
			Write(')');
		}

		protected override void Visit(PragmaGroup group)
		{
			WriteNewLineDelimitedList(group.Members);
		}

		protected override void Visit(CodeAwaitStatement statement) => WriteAwait(statement.Task);
		protected override void Visit(CodeAwaitExpression expression) => WriteAwait(expression.Task);
		private            void WriteAwait(ICodeExpression task)
		{
			// async is modeled differently in F# (task { }/async { }); not emitted by the F# model path yet
			throw new NotImplementedException("await expressions are not yet supported by F# code generator");
		}
		#endregion

		#region reusable helpers
		/// <summary>
		/// Generate custom attributes list.
		/// </summary>
		/// <param name="customAttributes">Attributes to generate.</param>
		/// <param name="inline">
		/// <see langword="true"/>: all attributes in a single <c>[&lt; &gt;]</c> group separated by <c>;</c>;
		/// <see langword="false"/>: each attribute on its own line.
		/// </param>
		private void WriteCustomAttributes(IReadOnlyList<CodeAttribute> customAttributes, bool inline)
		{
			if (customAttributes.Count == 0)
				return;

			if (!inline)
			{
				foreach (var attr in customAttributes)
				{
					Write("[<");
					Visit(attr);
					WriteLine(">]");
				}
			}
			else
			{
				Write("[<");
				WriteDelimitedList(customAttributes, "; ", false);
				Write(">]");
			}
		}

		/// <summary>
		/// Generate F# accessibility modifier (public is the default and omitted).
		/// </summary>
		private void WriteAccessibility(Modifiers attributes)
		{
			if (attributes.HasFlag(Modifiers.Private))
				Write("private ");
			else if (attributes.HasFlag(Modifiers.Internal))
				Write("internal ");
		}

		/// <summary>
		/// Emits type name for custom attribute. Removes optional "Attribute" suffix.
		/// </summary>
		private void WriteAttributeType(CodeAttribute attribute)
		{
			CodeIdentifier? newTypeName = null;

			if (attribute.Type.Type.Name == null)
				throw new InvalidOperationException($"Invalid custom attribute type {attribute.Type.Type} ({attribute.Type.Type.Kind})");

			if (attribute.Type.Type.Name.Name.EndsWith("Attribute", StringComparison.Ordinal))
				newTypeName = new CodeIdentifier(attribute.Type.Type.Name.Name.Substring(0, attribute.Type.Type.Name.Name.Length - 9), true);

			RenderType(attribute.Type.Type, newTypeName, true);
		}

		/// <summary>
		/// Generate code for groups of elements with new line as separator between groups.
		/// </summary>
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
		/// Writes an F# identifier, escaping keywords with double-backticks.
		/// </summary>
		private void WriteIdentifier(string identifier)
		{
			if (KeyWords.Contains(identifier))
			{
				Write("``");
				Write(identifier);
				Write("``");
			}
			else
				Write(identifier);
		}

		/// <summary>
		/// Generates value literal including <see langword="null"/> literals, numeric type hints and enum literals.
		/// </summary>
		private void WriteLiteral(object? value, IType type, bool targetTyped)
		{
			if (value == null)
				Write("null");
			else if (value is bool boolean)
				Write(boolean ? "true" : "false");
			else if (value is int intValue)
				Write(intValue.ToString(NumberFormatInfo.InvariantInfo));
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
						case '\0'    : Write("\\0");                  break;
						case '\t'    : Write("\\t");                  break;
						case '\\'    : Write("\\\\");                 break;
						case '"'     : Write("\\\"");                 break;

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

				if (Enum.IsDefined(enumType, value))
				{
					RenderType(type.WithNullability(false), null, false);
					Write('.');
					WriteIdentifier(Enum.GetName(enumType, value)!);
				}
				else
				{
					Write("enum<");
					RenderType(type.WithNullability(false), null, false);
					Write(">(");
					var underlyingType = Enum.GetUnderlyingType(enumType);
					WriteLiteral(Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture), _languageProvider.TypeParser.Parse(underlyingType), true);
					Write(')');
				}
			}
			else
				throw new NotImplementedException($"Literal generation for type {value.GetType().Name} not implemented by F# code generator");
		}

		/// <summary>
		/// Generates a member body as an indented block or single expression.
		/// </summary>
		private void WriteMethodBodyBlock(CodeBlock? statements, bool allowEmpty)
		{
			if (statements == null || statements.Items.Count == 0)
			{
				if (!allowEmpty)
					throw new InvalidOperationException("Empty code block encountered in unsupported context");

				Write(" ()");
				WriteLine();
				return;
			}

			if (statements.Items.Count == 1)
			{
				Write(' ');
				WriteMethodBodyExpression(statements);
				WriteLine();
				return;
			}

			WriteLine();
			IncreaseIdent();
			foreach (var stmt in statements.Items)
			{
				WriteTrivia(stmt.Before);
				Visit(stmt);
				WriteLine();
				WriteTrivia(stmt.After);
			}

			DecreaseIdent();
		}

		/// <summary>
		/// Emits a single-statement body inline (used for lambdas and single-expression members).
		/// </summary>
		private void WriteMethodBodyExpression(CodeBlock? statements)
		{
			if (statements == null || statements.Items.Count == 0)
			{
				Write("()");
				return;
			}

			if (statements.Items.Count == 1)
			{
				var stmt = statements.Items[0];
				if (stmt is CodeReturn ret)
				{
					if (ret.Expression != null)
						Visit(ret.Expression);
					else
						Write("()");
				}
				else
					Visit(stmt);

				return;
			}

			throw new NotImplementedException("Multi-statement inline body is not supported by F# code generator");
		}

		/// <summary>
		/// Render type token, rendering nullable types as <c>'T option</c>.
		/// </summary>
		private void RenderType(IType type, CodeIdentifier? nameOverride, bool typeOnlyContext)
		{
			var nullable = type.IsNullable;
			if (nullable)
				type = type.WithNullability(false);

			RenderTypeName(type, nameOverride, typeOnlyContext);

			// Wrap nullable *scalar* types in `option` (idiomatic F#, auto-mapped by linq2db.FSharp's
			// UseFSharp option support). Nullable reference types that are NOT scalar - entity associations,
			// byte[], obj, method return types - are left as plain nullable references, because option over a
			// non-scalar element is not auto-mapped and an entity/array option would not round-trip.
			if (nullable && (type.IsValueType || string.Equals(_languageProvider.GetAlias(type), "string", StringComparison.Ordinal)))
				Write(" option");
		}

		private void RenderTypeName(IType type, CodeIdentifier? nameOverride, bool typeOnlyContext)
		{
			var alias = _languageProvider.GetAlias(type);

			// qualified-name resolution mirrors the C# generator
			if (alias == null && type.Name != null)
			{
				var typeName = nameOverride ?? type.Name;

				if (type.Namespace != null || type.Parent != null)
				{
					var nested = false;
					if (type.Parent != null)
					{
						var t = _currentType;
						while (t != null)
						{
							if (t == type.Parent)
							{
								nested = true;
								break;
							}

							t = t.Parent;
						}
					}

					if (type.Parent != null && !nested)
					{
						RenderTypeName(type.Parent, null, typeOnlyContext);
						Write('.');
					}
					else
					{
						List<CodeIdentifier>? renderedParts = null;

						var scopedNames = typeOnlyContext ? _scopedTypes : _scopedNames;

						var scope         = _currentScope.ToList();
						var remainingName = new List<CodeIdentifier>();
						var currentName   = typeName;

						if (type.Parent != null)
						{
							var walkType = type.Parent;
							while (walkType != null)
							{
								remainingName.Insert(0, walkType.Name!);
								if (walkType.Namespace != null)
									remainingName.InsertRange(0, walkType.Namespace);
								walkType = walkType.Parent;
							}
						}
						else
							remainingName.InsertRange(0, type.Namespace!);

						while (!_languageProvider.FullNameEqualityComparer.Equals(remainingName, scope))
						{
							if (!scopedNames[scope].Contains(currentName))
							{
								if (scope.Count == 0)
									break;

								scope.RemoveAt(scope.Count - 1);
								continue;
							}
							else
							{
								if (remainingName.Count > 0)
								{
									currentName = remainingName[remainingName.Count - 1];
									remainingName.RemoveAt(remainingName.Count - 1);
									(renderedParts ??= new()).Insert(0, currentName);
								}
								else
									break;
							}
						}

						if (renderedParts != null)
						{
							foreach (var part in renderedParts)
							{
								WriteIdentifier(part.Name);
								Write('.');
							}
						}
					}
				}
			}

			switch (type.Kind)
			{
				case TypeKind.Regular:
				{
					var regType = (RegularType)type;
					if (alias != null)
						Write(alias);
					else
						Visit(nameOverride ?? regType.Name);
					break;
				}
				case TypeKind.Generic:
				{
					var regType = (GenericType)type;
					Visit(nameOverride ?? regType.Name);
					Write('<');
					WriteDelimitedList(type.TypeArguments!, t => RenderType(t, null, true), ", ", false);
					Write('>');
					break;
				}
				case TypeKind.Array:
				{
					RenderType(type.ArrayElementType!, null, typeOnlyContext);
					Write('[');
					var first = true;
					foreach (var size in type.ArraySizes!)
					{
						if (first)
							first = false;
						else
							Write(',');
					}

					Write(']');
					break;
				}
				case TypeKind.TypeArgument:
				{
					Write('\'');
					Visit(type.Name!);
					break;
				}
				case TypeKind.Dynamic    :
				case TypeKind.OpenGeneric:
				default:
					throw new NotImplementedException($"Type {type.Kind} support not implemented in F# code generator");
			}
		}
		#endregion
	}
}
