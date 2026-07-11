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
			// When the lambda targets a Func/Action delegate, wrap it in an explicit delegate construction
			// (e.g. System.Func<_, _>(fun x -> ...)). F# does not implicitly convert a lambda to a delegate
			// during overload resolution, so an unwrapped lambda leaves calls with delegate-vs-template
			// overloads (IDataContext.QueryProc/QueryProcAsync) ambiguous (FS0041). Expression<>-targeted
			// lambdas are left bare - F# auto-quotes those into LINQ expression trees.
			var wrapInDelegate = IsDelegateType(method.TargetType);

			if (wrapInDelegate)
			{
				RenderType(method.TargetType, null, true);
			}

			// F# lambda: fun p1 p2 -> body
			// Parenthesize the lambda: as a call argument, an unparenthesized `fun x -> a, b` would be parsed
			// by F# as a lambda returning the tuple (a, b) rather than two arguments.
			Write("(fun ");
			// a parameterless F# lambda is `fun () -> ...`, not `fun -> ...`
			if (method.Parameters.Count == 0)
				Write("()");
			else
				WriteDelimitedList(method.Parameters, p => Visit(p.Name), " ", false);
			Write(" -> ");

			WriteMethodBodyExpression(method.Body);
			Write(")");
		}

		// Func<...>/Action<...> delegate types (System namespace). Used to decide when a lambda must be
		// wrapped in an explicit delegate construction for F# overload resolution (see Visit(CodeLambda)).
		private static bool IsDelegateType(IType type)
		{
			var name = type.Name?.Name;
			return name is "Func" or "Action";
		}

		protected override void Visit(CodeMember expression)
		{
			// instance fields are rendered as `let`-bound values in F# and are accessed unqualified (no `this.`)
			var isInstanceField = expression.Instance is CodeThis
				&& expression.Member.Referenced is CodeField field
				&& !field.Attributes.HasFlag(Modifiers.Static);

			if (!isInstanceField)
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
			// static field/property references must be qualified with the declaring type in F# (C# allows
			// unqualified access within the class). Example: the context mapping schema referenced from the
			// primary constructor's base call.
			var isStatic = reference.Referenced switch
			{
				CodeProperty p => p.Attributes.HasFlag(Modifiers.Static),
				CodeField    f => f.Attributes.HasFlag(Modifiers.Static),
				_              => false,
			};

			if (isStatic && _currentType?.Name != null)
			{
				WriteIdentifier(_currentType.Name.Name);
				Write('.');
			}

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

			// async methods (Task-returning) are rendered with an F# `task { }` computation expression;
			// `await`ed calls become `let!`/`return!` bindings
			if (method.Attributes.HasFlag(Modifiers.Async))
				WriteTaskBody(method.Body);
			else
				WriteMethodBodyBlock(method.Body, method.ReturnType == null);
		}

		protected override void Visit(CodeParameter parameter)
		{
			// F# parameter: name : type. out/ref parameters (e.g. sync stored procedures with output
			// parameters) are rendered as byref<T>, which supports the `param <- value` mutation the body uses.
			var byRef = parameter.Direction is CodeParameterDirection.Out or CodeParameterDirection.Ref;

			Visit(parameter.Name);
			Write(" : ");
			if (byRef)
				Write("byref<");
			Visit(parameter.Type);
			if (byRef)
				Write(">");

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

			// auto-property: no getter/setter bodies. F# expresses these with `member val` and requires an
			// initializer, so emit the property initializer if present, otherwise Unchecked.defaultof<_>.
			var isAuto = property.Getter == null && property.Setter == null;

			if (isAuto)
			{
				Write(property.Attributes.HasFlag(Modifiers.Static) ? "static member val " : "member val ");
				WriteAccessibility(property.Attributes);
				Visit(property.Name);
				Write(" : ");
				Visit(property.Type);
				Write(" = ");
				if (property.Initializer != null)
					Visit(property.Initializer);
				else
					Write("Unchecked.defaultof<_>");
				Write(property.HasSetter ? " with get, set" : " with get");
				WriteLine();
			}
			else
			{
				// computed property with a getter body
				Write("member ");
				WriteAccessibility(property.Attributes);
				Write("this.");
				Visit(property.Name);
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

			// F# forbids nested type definitions, so every type - including result-set records that the
			// scaffolder nests inside the extensions class - is emitted flat at namespace level. `namespace rec`
			// makes declaration order irrelevant, and intra-class references stay unqualified so they resolve to
			// the lifted top-level type.
			var first = true;
			foreach (var type in EnumerateAllClasses(@namespace.Members))
			{
				if (!first)
					WriteLine();
				first = false;
				Visit(type);
			}

			_currentScope.RemoveRange(_currentScope.Count - @namespace.Name.Count, @namespace.Name.Count);
		}

		protected override void Visit(CodeClass @class)
		{
			// per-schema wrapper -> F# module (keeps its types isolated instead of lifted to namespace level)
			if (@class.Attributes.HasFlag(Modifiers.Module))
			{
				WriteModule(@class);
				return;
			}

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

				var hasCtorBody = primaryCtor?.Body != null && primaryCtor.Body.Items.Count > 0;
				// a self identifier is only needed when a `do` binding actually references the instance; an
				// instance-field mutation renders as `field <- v` (no `this`), so it doesn't require one, and an
				// unused `as this` is a warning (error under warnings-as-errors)
				var ctorUsesThis = hasCtorBody && primaryCtor!.Body!.Items.Any(CtorStatementUsesThis);

				if (primaryCtor != null)
				{
					Write(" (");
					WriteDelimitedList(primaryCtor.Parameters, ", ", false);
					Write(")");
					if (ctorUsesThis)
						Write(" as this");
				}
				else
				{
					// F# requires a primary constructor for `member val` auto-properties (e.g. proc/function
					// result classes, which are generated as classes rather than records)
					Write(" ()");
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

				// instance fields become `let`-bound values and must precede the constructor `do` bindings
				foreach (var fieldGroup in @class.Members.OfType<FieldGroup>())
					VisitList(fieldGroup.Members);

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

				// render remaining members (constructors are the primary constructor above; field groups are the
				// `let` bindings above)
				WriteMemberGroups(@class.Members.Where(g => g is not ConstructorGroup and not FieldGroup).ToList());

				DecreaseIdent();
			}

			WriteLine();

			_currentIsExtensionType = oldIsExtensionType;
			_currentIsRecord        = oldIsRecord;
			_currentType            = oldType;

			_currentScope.RemoveAt(_currentScope.Count - 1);
		}

		/// <summary>
		/// Renders a per-schema wrapper as an F# <c>module rec</c>. F# allows type definitions inside a module
		/// (unlike inside a class), so the schema's entity/result/context types stay isolated under the module -
		/// avoiding name collisions between same-named tables in different schemas - while the schema's
		/// extension methods (Find, functions) are collected into an <c>[&lt;Extension&gt;]</c> type inside it.
		/// </summary>
		private void WriteModule(CodeClass @class)
		{
			if (@class.XmlDoc != null)
				Visit(@class.XmlDoc);

			// no `rec` here: the enclosing `namespace rec` already provides recursion, and repeating it warns
			// (FS3199)
			Write("module ");
			Visit(@class.Name);
			WriteLine(" =");
			IncreaseIdent();

			_currentScope.Add(@class.Name);
			var oldType  = _currentType;
			_currentType = @class.Type;

			// nested type definitions (schema-context class, entity records, result classes)
			var first = true;
			foreach (var type in EnumerateAllClasses(@class.Members))
			{
				if (!first)
					WriteLine();
				first = false;
				Visit(type);
			}

			// schema extension methods (Find, functions) collected into one [<Extension>] type
			var methods = @class.Members.EnumerateMemberGroups<MethodGroup>()
				.SelectMany(static g => g.Members)
				.ToList();

			if (methods.Count > 0)
			{
				if (!first)
					WriteLine();

				WriteLine("[<System.Runtime.CompilerServices.Extension>]");
				Write("type ");
				WriteIdentifier(DataModelConstants_FSharpSchemaExtensionsType);
				WriteLine(" =");
				IncreaseIdent();

				var oldIsExtensionType  = _currentIsExtensionType;
				_currentIsExtensionType = true;

				var firstMethod = true;
				foreach (var method in methods)
				{
					if (!firstMethod)
						WriteLine();
					firstMethod = false;
					Visit(method);
				}

				_currentIsExtensionType = oldIsExtensionType;
				DecreaseIdent();
			}

			_currentType = oldType;
			_currentScope.RemoveAt(_currentScope.Count - 1);

			DecreaseIdent();
			WriteLine();
		}

		// name of the [<Extension>] type synthesized inside a schema module to hold its extension methods
		private const string DataModelConstants_FSharpSchemaExtensionsType = "Extensions";

		/// <summary>
		/// Determines whether a primary-constructor body statement references the instance. An instance-field
		/// mutation (an assignment to a field of the current instance) is rendered as an unqualified
		/// <c>field &lt;- v</c> (F# let-bound field), so it does not require an "as this" self identifier;
		/// anything else does.
		/// </summary>
		private static bool CtorStatementUsesThis(ICodeStatement statement)
		{
			return statement is not CodeAssignmentStatement
			{
				LValue: CodeMember { Instance: CodeThis, Member.Referenced: CodeField field },
			}
			|| field.Attributes.HasFlag(Modifiers.Static);
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
			// F# object construction: TypeName(args). C# object initializers (`new T { Prop = v }`) are rendered
			// as named-argument / property-setter syntax inside the constructor call: TypeName(Prop = v, ...).
			Visit(expression.Type);
			Write('(');

			var first = true;
			foreach (var parameter in expression.Parameters)
			{
				if (!first)
					Write(", ");
				first = false;
				Visit(parameter);
			}

			foreach (var initializer in expression.Initializers)
			{
				if (!first)
					Write(", ");
				first = false;
				Visit(initializer.LValue);
				Write(" = ");
				Visit(initializer.RValue);
			}

			Write(')');
		}

		protected override void Visit          (CodeAssignmentStatement  statement ) => WriteAssignment(statement );
		protected override void Visit          (CodeAssignmentExpression expression) => WriteAssignment(expression);
		private            void WriteAssignment(CodeAssignmentBase       assignment)
		{
			Visit(assignment.LValue);
			// a variable declaration (`let mutable x`) is initialized with `=`; assignment to an existing
			// target is mutation with `<-`
			Write(assignment.LValue is CodeVariable ? " = " : " <- ");
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

			// instance field -> `let mutable name : type = value` (F# `let` bindings require an initializer);
			// the constructor body assigns the real value afterwards
			Write(field.Attributes.HasFlag(Modifiers.Static) ? "static val mutable " : "let mutable ");
			Visit(field.Name);
			Write(" : ");
			Visit(field.Type);
			Write(" = ");
			if (field.Initializer != null)
				Visit(field.Initializer);
			else
				Write("Unchecked.defaultof<_>");

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
				// nested type definitions are lifted to namespace level (see Visit(CodeNamespace)); skip them here
				if (group is ClassGroup)
					continue;

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
		/// Recursively enumerates every type defined in the given member groups, including types nested inside
		/// other types or regions. F# does not allow nested type definitions, so all of them are emitted flat at
		/// namespace level.
		/// </summary>
		private static IEnumerable<CodeClass> EnumerateAllClasses(IEnumerable<IMemberGroup> groups)
		{
			foreach (var group in groups)
			{
				if (group is ClassGroup classGroup)
				{
					foreach (var @class in classGroup.Members)
					{
						yield return @class;

						// module wrappers render their own nested types (see WriteModule); don't lift those out
						if (@class.Attributes.HasFlag(Modifiers.Module))
							continue;

						foreach (var nested in EnumerateAllClasses(@class.Members))
							yield return nested;
					}
				}
				else if (group is RegionGroup regionGroup)
				{
					foreach (var region in regionGroup.Members)
						foreach (var nested in EnumerateAllClasses(region.Members))
							yield return nested;
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
		/// Renders an async method body as an F# <c>task { }</c> computation expression, translating
		/// <c>await</c>ed calls to <c>let!</c> / <c>return!</c> bindings.
		/// </summary>
		private void WriteTaskBody(CodeBlock? statements)
		{
			WriteLine();
			IncreaseIdent();
			WriteLine("task {");
			IncreaseIdent();

			if (statements != null)
			{
				foreach (var statement in statements.Items)
				{
					WriteTrivia(statement.Before);
					WriteTaskStatement(statement);
					WriteLine();
					WriteTrivia(statement.After);
				}
			}

			DecreaseIdent();
			WriteLine("}");
			DecreaseIdent();
		}

		/// <summary>
		/// Renders a single statement inside a <c>task { }</c> computation expression.
		/// </summary>
		private void WriteTaskStatement(ICodeStatement statement)
		{
			switch (statement)
			{
				// `var x = await task;` -> `let! x = task`
				case CodeAssignmentStatement { RValue: CodeAwaitExpression await } assign:
					Write("let! ");
					WriteBindingTarget(assign.LValue);
					Write(" = ");
					Visit(await.Task);
					break;

				// `var x = value;` -> `let mutable x = value` (variable declaration)
				case CodeAssignmentStatement { LValue: CodeVariable variable } varAssign:
					Write("let mutable ");
					Visit(variable.Name);
					Write(" = ");
					Visit(varAssign.RValue);
					break;

				// assignment to an existing target -> mutation
				case CodeAssignmentStatement assign:
					Visit(assign.LValue);
					Write(" <- ");
					Visit(assign.RValue);
					break;

				// `await task;` -> `do! task`
				case CodeAwaitStatement awaitStatement:
					Write("do! ");
					Visit(awaitStatement.Task);
					break;

				case CodeReturn { Expression: CodeAwaitExpression returnAwait }:
					Write("return! ");
					Visit(returnAwait.Task);
					break;

				case CodeReturn { Expression: { } returnExpression }:
					Write("return ");
					Visit(returnExpression);
					break;

				case CodeReturn:
					Write("return ()");
					break;

				default:
					Visit(statement);
					break;
			}
		}

		/// <summary>
		/// Renders the target of a <c>let!</c> binding: a declared variable emits just its name.
		/// </summary>
		private void WriteBindingTarget(ILValue target)
		{
			if (target is CodeVariable variable)
				Visit(variable.Name);
			else
				Visit(target);
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
							// A scope may be absent from the map when a type was lifted out of its original
							// (nested) position - e.g. proc result records moved to namespace level for F#. Treat a
							// missing scope as "no conflict": the lifted type is top-level and needs no qualifier.
							if (!scopedNames.TryGetValue(scope, out var scopeNames) || !scopeNames.Contains(currentName))
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
