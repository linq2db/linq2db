using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// This visitor used to walk all generated code and fix identifiers to be valid according to C# naming
	/// and conflict resolution rules.
	/// </summary>
	internal sealed class CSharpNameNormalizationVisitor : NoopCodeModelVisitor
	{
		// Note that we use strings for names tracking as this visitor modifies CodeIdentifier instances

		private readonly ILanguageProvider    _languageProvider;
		/// <summary>
		/// Tracks names with global visibility: namespaces, types (with namespace/parent type prefixes).
		/// </summary>
		private readonly ISet<string>         _globalNames;
		/// <summary>
		/// Tracks type names with global visibility (with namespace/parent type prefixes).
		/// </summary>
		private readonly ISet<string>         _globalTypeNames;
		/// <summary>
		/// Tracks already validated identifier instances, so they are not validated/fixed multiple times.
		/// No custom comparer used as we use reference comparison here.
		/// </summary>
		private readonly ISet<CodeIdentifier> _visitedNames = new HashSet<CodeIdentifier>();

		/// <summary>
		/// Tracks known names of parameters and local variables for current method.
		/// </summary>
		private ISet<string>?                                  _parametersAndVariables;
		/// <summary>
		/// Tracks method overloads in current type.
		/// </summary>
		private IDictionary<string, List<CodeMethod>>?         _overloads;
		/// <summary>
		/// Tracks names of members for current type(class).
		/// </summary>
		private ISet<string>?                                  _memberNames;
		/// <summary>
		/// Tracks current namespace if any (as full indentifier).
		/// </summary>
		private string?                                        _namespace;
		/// <summary>
		/// Tracks current type name including parent type(s) name but without namespace.
		/// </summary>
		private string?                                        _typeName;

		public CSharpNameNormalizationVisitor(ILanguageProvider languageProvider)
		{
			_languageProvider = languageProvider;
			_globalNames      = CreateNewNamesSet();
			_globalTypeNames  = CreateNewNamesSet();
		}

		#region visitors
		protected override void Visit(CodeLambda method)
		{
			// when lambda-method used inside of other method - it inherits owner method context
			// otherwise it create own naming context
			// when we inherit context - we create context copy to avoid parent context pollution
			var parentContext = _parametersAndVariables;

			_parametersAndVariables = CreateNewNamesSet(parentContext);

			base.Visit(method);

			// restore context
			_parametersAndVariables = parentContext;
		}

		protected override void Visit(CodeMethod method)
		{
			var isOverload = false;
			if (_overloads!.TryGetValue(method.Name.Name, out var overloads))
			{
				// here we test if new method could be used as overload alongside with other methods with such name
				// and if it cannot be used as overload we should change it's name to remove naming conflict
				//
				// overload logic
				// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/basic-concepts#signatures-and-overloading
				// overload key contains:
				// 1. name (used for _overloads dictionary key)
				// 2. number of type parameters (skipped, as currently missing from our code model)
				// 3. for each parameter (positional): parameter type (without NRT annotation) + direction (kind)
				// NOTE: ref direction is same as out direction for overload resolution logic
				isOverload = true;
				foreach (var overload in overloads)
				{
					// TODO: add type parameters count check when added to code model
					if (overload.Parameters.Count == method.Parameters.Count)
					{
						var sameParameters = true;
						for (var i = 0; i < overload.Parameters.Count; i++)
						{
							if (overload.Parameters[i].Direction != method.Parameters[i].Direction
								&& (overload.Parameters[i].Direction == CodeParameterDirection.In
									|| method.Parameters[i].Direction == CodeParameterDirection.In))
							{
								sameParameters = false;
								break;
							}

							if (!_languageProvider.TypeEqualityComparerWithoutNRT.Equals(overload.Parameters[i].Type!.Type, method.Parameters[i].Type!.Type))
							{
								sameParameters = false;
								break;
							}
						}

						// found overload with same set of parameters (according to overload resolution procedure)
						// so we cannot use this method as overload for current method group
						if (sameParameters)
						{
							isOverload = false;
							break;
						}
					}
				}

				if (isOverload)
					overloads.Add(method);
			}

			if (!isOverload)
			{
				FixName(_memberNames!, method.Name, null, true);

				// create new method group for current method
				// note that we do not try to find another existing overloads group with name derived from
				// initial method name as it doesn't make any sense to create new artifical group of overloads
				_overloads.Add(method.Name.Name, new List<CodeMethod>() { method });
			}

			// as we don't have local methods yet, we don't need to save/reuse method contexts
			_parametersAndVariables = CreateNewNamesSet();
			base.Visit(method);
			_parametersAndVariables = null;
		}

		protected override void Visit(CodeParameter parameter)
		{
			FixName(_parametersAndVariables!, parameter.Name, null, true);
			base.Visit(parameter);
		}

		protected override void Visit(CodeTypeInitializer cctor)
		{
			// actually variables only in this case, as static constructor has no parameters
			_parametersAndVariables = CreateNewNamesSet();
			base.Visit(cctor);
			_parametersAndVariables = null;
		}

		protected override void Visit(CodeConstructor ctor)
		{
			// don't check for overloads as we cannot do anything to fix them
			_parametersAndVariables = CreateNewNamesSet();
			base.Visit(ctor);
			_parametersAndVariables = null;
		}

		protected override void Visit(CodeProperty property)
		{
			FixName(_memberNames!, property.Name, null, true);

			// as we need to inspect getters/setters, we cannot just call base.Visit and need to re-implement property
			// visitor here
			if (property.XmlDoc != null)
				Visit(property.XmlDoc);

			VisitList(property.CustomAttributes);

			Visit(property.Type);
			Visit(property.Name);

			if (property.Getter != null)
			{
				_parametersAndVariables = CreateNewNamesSet();
				VisitList(property.Getter);
				_parametersAndVariables = null;
			}

			if (property.Setter != null)
			{
				_parametersAndVariables = CreateNewNamesSet();
				// register pre-defined "value" parameter for setter
				_parametersAndVariables.Add("value");
				VisitList(property.Setter);
				_parametersAndVariables = null;
			}

			if (property.Initializer != null)
				Visit(property.Initializer);

			if (property.TrailingComment != null)
				Visit(property.TrailingComment);
		}

		protected override void Visit(CodeNamespace @namespace)
		{
			var oldNamespace = _namespace;

			// validate all namespaces, defined by current namespace block.
			// E.g. for namespace A.B.C we should validate following names for conflicts:
			// A
			// A.B
			// A.B.C
			for (var i = 0; i < @namespace.Name.Count; i++)
			{
				var name = @namespace.Name[i];
				var fullName = FixName(
					_globalTypeNames,
					name,
					n => string.JoinStrings('.', @namespace.Name.Take(i).Select(n => string.Format(CultureInfo.InvariantCulture, "{0}", n.Name))) + (i > 0 ? "." : null) + n,
					false);
				_globalNames.Add(fullName);
			}

			// save namespace name to context only after it was fixed
			_namespace = string.JoinStrings('.', @namespace.Name.Select(_ => _.Name));

			base.Visit(@namespace);

			_namespace = oldNamespace;
		}

		protected override void Visit(CodeClass @class)
		{
			if (@class.Type.Parent == null)
			{
				var fullName = FixName(_globalNames, @class.Name, n => _namespace + (_namespace != null ? "." : null) + _typeName + (_typeName != null ? "." : null) + n, true);
				_globalTypeNames.Add(fullName);
			}
			else
				FixName(_memberNames!, @class.Name, null, true);

			var oldOverloads = _overloads;
			_overloads       = new Dictionary<string, List<CodeMethod>>(_languageProvider.RawIdentifierEqualityComparer);

			var oldMembers = _memberNames;
			_memberNames   = CreateNewNamesSet();
			// track current type name in type members and C# doesn't allow members with same name as type name
			_memberNames.Add(@class.Name.Name);

			var oldTypeName = _typeName;
			_typeName       = _typeName + (_typeName != null ? "." : null) + @class.Name.Name;

			base.Visit(@class);

			_typeName    = oldTypeName;
			_memberNames = oldMembers;
			_overloads   = oldOverloads;
		}

		protected override void Visit(CodeField field)
		{
			FixName(_memberNames!, field.Name, null, true);

			base.Visit(field);
		}

		protected override void Visit(CodeVariable expression)
		{
			FixName(_parametersAndVariables!, expression.Name, null, true);

			base.Visit(expression);
		}
		#endregion

		/// <summary>
		/// Validate and fix provided identifier value.
		/// </summary>
		/// <param name="scopeNames">Collection of names with same scope as validated name to check for name being unique.</param>
		/// <param name="name">Identifier to validate and fix.</param>
		/// <param name="getFullName">Optional action to generate full identifier name. Useful for composite identifers that include namespace and/or and parent type name.</param>
		/// <param name="add">Indicate that after validation name should be added to <paramref name="scopeNames"/> collection.</param>
		/// <returns>Full identifier name, returned by <paramref name="getFullName"/> action.</returns>
		private string FixName(ISet<string> scopeNames, CodeIdentifier name, Func<string, string>? getFullName, bool add)
		{
			// identifier already fixed
			// technically we should trigger this branch as we shouldn't call FixName for same identifier twice
			// (probably we should throw exception if multiple calls detected)
			if (!_visitedNames.Add(name))
				return getFullName?.Invoke(name.Name) ?? name.Name;

			var identifierName = NormalizeIdentifier(name.Name);
			var fullName       = getFullName?.Invoke(identifierName) ?? identifierName;

			string? baseName = null;

			// counter to use in name for conflict resolution
			var cnt = 0;
			// repeat until non-empty unique name generated
			while (fullName.Length == 0 || scopeNames.Contains(fullName))
			{
				// generate base name only if we need to generate new name
				identifierName = baseName ??= GetBaseIdentifierName(name.Name, name.FixOptions, name.Position);

				// apply counter only if first iteration produced unacceptable name
				if (cnt > 0)
					identifierName += cnt.ToString(NumberFormatInfo.InvariantInfo);

				identifierName = NormalizeIdentifier(identifierName);
				fullName       = getFullName?.Invoke(identifierName) ?? identifierName;

				cnt++;
			}

			// save new name back to identifier instance
			if (!string.Equals(name.Name, identifierName, StringComparison.Ordinal))
				name.Name = identifierName;

			// register new name in scope
			if (add)
				scopeNames.Add(fullName);

			return fullName;
		}

		/// <summary>
		/// Normalize identifier name according to C# naming rules by removing unsupported characters in unsupported positions.
		/// Could return empty string as result if all characters were removed.
		/// </summary>
		/// <param name="name">Identifier name to normalize.</param>
		/// <returns>Normalized identifer name or empty string.</returns>
		private string NormalizeIdentifier(string name)
		{
			// character filtration based on spec
			// with one ommission - we don't allow leading @ and add it later based on keyword lookup
			// (https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers)
			var newName = new StringBuilder();

			foreach (var (chr, cat) in name.EnumerateCharacters())
			{
				switch (cat)
				{
					// valid as first/non-first character
					case UnicodeCategory.UppercaseLetter     :
					case UnicodeCategory.LowercaseLetter     :
					case UnicodeCategory.TitlecaseLetter     :
					case UnicodeCategory.ModifierLetter      :
					case UnicodeCategory.OtherLetter         :
					case UnicodeCategory.LetterNumber        :
						newName.Append(chr);
						break;
					// valid as non-first identifier character
					case UnicodeCategory.DecimalDigitNumber  :
					case UnicodeCategory.ConnectorPunctuation:
					case UnicodeCategory.NonSpacingMark      :
					case UnicodeCategory.SpacingCombiningMark:
					case UnicodeCategory.Format              :
						// if first character is not valid on first position, prefix it with "_"
						if (newName.Length == 0)
							newName.Append('_');
						newName.Append(chr);
						break;
					default                                  :
						// if character invalid - remove it
						break;
				}
			}

			// if identifier starts with two (or more) underscores: remove all but one (__ is MS implementation-specific reserved prefix)
			while (newName.Length > 1 && newName[0] == '_' && newName[1] == '_')
				newName.Remove(0, 1);

			return newName.ToString();
		}

		/// <summary>
		/// Generate base identifier name using normalized name and optional name fix settings.
		/// </summary>
		/// <param name="identifierName">Normalized original identifier name.</param>
		/// <param name="fixOptions">Optional identifier fix options.</param>
		/// <param name="position">Optional identifier position value (e.g. position of parameter that use identifier for name).</param>
		/// <returns>Base name to use for unique identifier generation.</returns>
		private static string GetBaseIdentifierName(string identifierName, NameFixOptions? fixOptions, int? position)
		{
			var baseName = fixOptions switch
			{
				null => identifierName,

				// if fix options specified for identifier - use them to generate base name to use for new identifier generation
				{ FixType: NameFixType.Replace } =>
					fixOptions.DefaultValue,
				{ FixType: NameFixType.ReplaceWithPosition } =>
					fixOptions.DefaultValue + position?.ToString(NumberFormatInfo.InvariantInfo),
				{ FixType: NameFixType.Suffix } =>
					identifierName + fixOptions.DefaultValue,
				{ FixType: NameFixType.SuffixWithPosition } =>
					identifierName + fixOptions.DefaultValue + position?.ToString(NumberFormatInfo.InvariantInfo),

				_ => 
					throw new NotImplementedException($"C# name validator doesn't implement {fixOptions.FixType} name fix strategy"),
			};

			if (string.IsNullOrEmpty(baseName))
				baseName = "_";

			return baseName;
		}

		/// <summary>
		/// Create hash set for identifiers using language-specific comparison logic.
		/// </summary>
		/// <param name="initialData">Optional initial collection data.</param>
		/// <returns>New hash set collection.</returns>
		private ISet<string> CreateNewNamesSet(ISet<string>? initialData = null)
		{
			return initialData == null
				? new HashSet<string>(_languageProvider.RawIdentifierEqualityComparer)
				: new HashSet<string>(initialData, _languageProvider.RawIdentifierEqualityComparer);
		}
	}
}
