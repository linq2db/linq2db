using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LinqToDB.CodeGen.ContextModel;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CSharpNameNormalizationVisitor : NoopCodeModelVisitor
	{
		private string? _namespace;
		private string? _typeName;
		private readonly ISet<string> _globalNames;
		private readonly ISet<string> _globalTypeNames;
		private ISet<string>? _memberNames;
		private Dictionary<string, List<CodeMethod>>? _overloads;
		private ISet<string>? _parametersAndVariables;

		private readonly ILanguageServices _langServices;

		private readonly ISet<IType> _localTypes;
		private readonly ISet<IType> _externalTypes;

		public CSharpNameNormalizationVisitor(ILanguageServices langServices, ISet<IType> localTypes, ISet<IType> externalTypes)
		{
			_langServices = langServices;

			_globalNames = langServices.GetUniqueNameCollection();
			_globalTypeNames = langServices.GetUniqueNameCollection();

			_localTypes = localTypes;
			_externalTypes = externalTypes;
		}

		protected override void Visit(LambdaMethod method)
		{
			var ownContext = _parametersAndVariables == null;
			if (ownContext)
				_parametersAndVariables = _langServices.GetUniqueNameCollection();

			base.Visit(method);

			if (ownContext)
				_parametersAndVariables = null;
			
		}

		protected override void Visit(CodeMethod method)
		{
			var isOverload = false;
			if (_overloads!.TryGetValue(method.Name.Name, out var overloads))
			{
				// overload logic
				// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/basic-concepts#signatures-and-overloading
				// overload key contains:
				// 1. name (used for _overloads dictionary)
				// 2. number of type parameters (currently missing from out code model)
				// 3. for each parameter (positional): parameter type (without NRT annotation) + direction (kind)
				// NOTE: ref direction equal to out direction
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
								&& (overload.Parameters[i].Direction == Direction.In
									|| method.Parameters[i].Direction == Direction.In))
							{
								sameParameters = false;
								break;
							}

							if (!AreTypesEqualForOverload(overload.Parameters[i].Type!.Type, method.Parameters[i].Type!.Type))
							{
								sameParameters = false;
								break;
							}
						}

						if (sameParameters)
						{
							isOverload = false;
						}
					}
				}

				if (isOverload)
				{
					overloads.Add(method);
				}
			}

			if (!isOverload)
			{
				var name = FixName(_memberNames!, method.Name, n => n, false);
				_memberNames!.Add(name);

				_overloads.Add(name, new List<CodeMethod>() { method });
			}

			// as we don't have local methods yet, we don't need to save/reuse name sets
			_parametersAndVariables = _langServices.GetUniqueNameCollection();
			base.Visit(method);
			_parametersAndVariables = null;
		}

		private bool AreTypesEqualForOverload(IType type1, IType type2)
		{
			if (type1.Kind != type2.Kind)
				return false;

			if (type1.IsValueType != type2.IsValueType)
				return false;

			if (type1.External != type2.External)
				return false;

			if (type1.IsValueType && type1.IsNullable != type2.IsNullable) // ignore NRT
				return false;

			if (type1.Name?.Name != type2.Name?.Name)
				return false;

			if ((type1.Namespace == null && type2.Namespace != null)
				|| (type1.Namespace != null && type2.Namespace == null))
				return false;
			if (type1.Namespace != null
				&& (type1.Namespace.Length != type2.Namespace!.Length
				|| !type1.Namespace.Select(_ => _.Name).SequenceEqual(type2.Namespace.Select(_ => _.Name))))
				return false;

			if ((type1.Parent == null && type2.Parent != null)
				|| (type1.Parent != null && type2.Parent == null))
				return false;
			if (type1.Parent != null
				&& !AreTypesEqualForOverload(type1.Parent, type2.Parent!))
				return false;

			if ((type1.ArrayElementType == null && type2.ArrayElementType != null)
				|| (type1.ArrayElementType != null && type2.ArrayElementType == null))
				return false;
			if (type1.ArrayElementType != null
				&& !AreTypesEqualForOverload(type1.ArrayElementType, type2.ArrayElementType!))
				return false;

			if ((type1.TypeArguments == null && type2.TypeArguments != null)
				|| (type1.TypeArguments != null && type2.TypeArguments == null))
				return false;
			if (type1.TypeArguments != null)
			{
				if (type1.TypeArguments.Length != type2.TypeArguments!.Length)
					return false;

				for (var i = 0; i < type1.TypeArguments.Length; i++)
					if (!AreTypesEqualForOverload(type1.TypeArguments[i], type2.TypeArguments[i]))
						return false;
			}

			//OpenGenericArgCount: not applicable
			//ArraySizes: not applicable

			return true;
		}

		protected override void Visit(CodeParameter parameter)
		{
			FixName(_parametersAndVariables!, parameter.Name, n => n, true);
			base.Visit(parameter);
		}

		protected override void Visit(CodeConstructor ctor)
		{
			// don't check for overloads as we cannot do anything with them

			_parametersAndVariables = _langServices.GetUniqueNameCollection();
			base.Visit(ctor);
			_parametersAndVariables = null;
		}

		protected override void Visit(CodeProperty property)
		{
			FixName(_memberNames!, property.Name, n => n, true);

			if (property.XmlDoc != null)
				Visit(property.XmlDoc);

			VisitList(property.CustomAttributes);

			Visit(property.Type);
			Visit(property.Name);


			if (property.Getter != null)
			{
				_parametersAndVariables = _langServices.GetUniqueNameCollection();
				VisitList(property.Getter);
				_parametersAndVariables = null;
			}

			if (property.Setter != null)
			{
				_parametersAndVariables = _langServices.GetUniqueNameCollection();
				_parametersAndVariables.Add("value");
				VisitList(property.Setter);
				_parametersAndVariables = null;
			}

			if (property.TrailingComment != null)
				Visit(property.TrailingComment);
		}

		protected override void Visit(CodeElementNamespace @namespace)
		{
			var oldNs = _namespace;

			for (var i = 0; i < @namespace.Name.Length; i++)
			{
				var name = @namespace.Name[i];
				var fullName = FixName(_globalTypeNames, name, n => string.Join(".", @namespace.Name.Take(i)) + (i > 0 ? "." : null) + n, false);
				_globalNames.Add(fullName);
			}

			_namespace = string.Join(".", @namespace.Name.Select(_ => _.Name));

			base.Visit(@namespace);

			_namespace = oldNs;
		}

		protected override void Visit(CodeClass @class)
		{
			if (@class.Type.Parent == null)
			{
				var fullName = FixName(_globalNames, @class.Name, n => _namespace + (_namespace != null ? "." : null) + _typeName + (_typeName != null ? "." : null) + n, true);
				_globalTypeNames.Add(fullName);

			}
			else
			{
				FixName(_memberNames!, @class.Name, n => n, true);
			}


			var oldOverloads = _overloads;
			_overloads = new Dictionary<string, List<CodeMethod>>();

			var oldMembers = _memberNames;
			_memberNames = _langServices.GetUniqueNameCollection();
			_memberNames.Add(@class.Name.Name);

			var oldTypeName = _typeName;
			_typeName = _typeName + (_typeName != null ? "." : null) + @class.Name.Name;
			base.Visit(@class);
			_typeName = oldTypeName;
			_memberNames = oldMembers;
			_overloads = oldOverloads;
		}

		private string FixName(ISet<string> scopeNames, CodeIdentifier name, Func<string, string> getFullName, bool add)
		{
			var rawName = name.Name;
			var baseName = name.Name;

			var fullName = getFullName(rawName);

			var cnt = 0;
			while (fullName == "" || (add ? !scopeNames.Add(fullName) : scopeNames.Contains(fullName)))
			{
				//if (name.Protected)
				//	throw new InvalidOperationException();

				if (name.FixOptions != null)
				{
					switch (name.FixOptions.FixType)
					{
						case FixType.Replace:
							rawName = name.FixOptions.Fixer;
							break;
						case FixType.ReplaceWithPosition:
							rawName = name.FixOptions.Fixer + name.Position?.ToString(NumberFormatInfo.InvariantInfo);
							break;
						case FixType.Suffix:
							rawName = name.Name + name.FixOptions.Fixer;
							break;
						case FixType.SuffixWithPosition:
							rawName = name.Name + name.FixOptions.Fixer + name.Position?.ToString(NumberFormatInfo.InvariantInfo);
							break;
						default:
							throw new InvalidOperationException();
					}
				}

				baseName = string.IsNullOrEmpty(rawName) ? "_" : rawName;

				if (cnt > 0)
					baseName += cnt.ToString(NumberFormatInfo.InvariantInfo);

				fullName = getFullName(baseName!);
				cnt++;
			}

			name.Name = baseName!;

			return fullName;
		}

		protected override void Visit(CodeField field)
		{
			FixName(_memberNames!, field.Name, n => n, true);

			base.Visit(field);
		}

		protected override void Visit(VariableExpression expression)
		{
			FixName(_parametersAndVariables!, expression.Name, n => n, true);

			base.Visit(expression);
		}
	}
}
