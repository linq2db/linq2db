using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.CodeGen.CodeModel
{
	public static class TypeBuilder
	{
		public static IType FromType(Type type, ILanguageServices langServices)
		{
			// Nullable<T>
			var isNullable = type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(Nullable<>);
			if (isNullable)
				return FromType(type.GenericTypeArguments[0], langServices).WithNullability(true);

			// array
			if (type.IsArray)
			{
				var rank = type.GetArrayRank();
				return new ArrayType(
					FromType(type.GetElementType(), langServices),
					new int?[rank],
					false);
			}

			// nested type
			IType? parent = null;
			if (type.DeclaringType != null)
			{
				parent = FromType(type.DeclaringType, langServices);
			}

			// type argument
			if (type.IsGenericParameter)
			{
				throw new NotImplementedException();
			}

			// regular or generic type
			CodeIdentifier[]? ns = null;
			if (type.Namespace != null)
			{
				var parts = type.Namespace.Split('.');
				ns = new CodeIdentifier[parts.Length];
				for (var i = 0; i < parts.Length; i++)
					ns[i] = new CodeIdentifier(parts[i]);
			}
			
			var alias = langServices.GetAlias(ns, type.Name);

			var name = alias
				?? (type.IsGenericType
						? type.Name.Substring(0, type.Name.IndexOf('`'))
						: type.Name);

			if (type.IsGenericType)
			{
				var typeArgs = type.GetGenericArguments();
				// generic/open generic
				if (type.IsGenericTypeDefinition)
				{
					if (parent != null)
						return new OpenGenericType(parent, new CodeIdentifier(name), alias != null, type.IsValueType, isNullable, typeArgs.Length, true);
					else
						return new OpenGenericType(ns, new CodeIdentifier(name), alias != null, type.IsValueType, isNullable, typeArgs.Length, true);
				}
				var typeArguments = new IType[typeArgs.Length];
				for (var i = 0; i < typeArgs.Length; i++)
					// TODO: missing NRT nullability here
					typeArguments[i] = FromType(typeArgs[i], langServices);

				if (parent != null)
					return new GenericType(parent, new CodeIdentifier(name), alias != null, type.IsValueType, isNullable, typeArguments, true);
				else
					return new GenericType(ns, new CodeIdentifier(name), alias != null, type.IsValueType, isNullable, typeArguments, true);
			}

			if (parent != null)
				return new RegularType(parent, new CodeIdentifier(name), alias != null, type.IsValueType, isNullable, true);
			else
				return new RegularType(ns, new CodeIdentifier(name), alias != null, type.IsValueType, isNullable, true);
		}

		// we accept only C# like type names with following distinctions:
		// - nested type separator is '+' (as it generated type metadata), not '.'
		// - type aliases not supported
		// - arrays not supported
		// - type arguments not recognized as such
		// - all types treated as reference types
		// if any of those above will become an issue, we will introduce extra hinting syntax to be able to parse it properly
		internal static IType ParseType(ILanguageServices langServices, string typeName, bool valueType)
		{
			var (type, length) = ParseTypeInternal(langServices, null, typeName, valueType, false, false);
			if (type == null || length != typeName.Length)
				throw new InvalidOperationException();
			return type;
		}

		private enum ParserState
		{
			SearchForIdentifierStart,
			ParseNamespaceTypeNameOrParentTypeName,
			ParseTypeNameOrParentTypeName,
			ParseNullablity,
			ParseTrailingSpaces,
			SearchNextTypeArgument,
		}

		private static (IType? type, int length) ParseTypeInternal(ILanguageServices langServices, IType? parentType, string typeName, bool valueType, bool nested, bool genericList)
		{
			var ns = new List<CodeIdentifier>();
			var nullable = false;
			var openGeneric = false;
			var openGenericCount = 0;
			var typeArgs = new List<IType>();

			var length = 0;
			var isParent = false;

			CodeIdentifier? name = null;
			var currentIdentifier = new StringBuilder();
			var state = parentType != null ? ParserState.ParseTypeNameOrParentTypeName : ParserState.SearchForIdentifierStart;

			// use state machine for type parsing
			foreach (var (chr, cat) in typeName.EnumerateCharacters())
			{
				length += chr.Length;
				if (chr == " ")
				{
					switch (state)
					{
						case ParserState.SearchForIdentifierStart:
							// skip leading spaces
							continue;
						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
						case ParserState.ParseTypeNameOrParentTypeName:
							// trailing space
							state = ParserState.ParseTrailingSpaces;
							continue;
						case ParserState.ParseNullablity:
							if (nested)
								break;
							state = ParserState.ParseTrailingSpaces;
							continue;
						case ParserState.ParseTrailingSpaces:
							// skip trailing spaces
							continue;
						case ParserState.SearchNextTypeArgument:
							continue;
						default:
							throw new InvalidOperationException();
					}
				}
				if (chr == ".")
				{
					switch (state)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException();
							ns.Add(new CodeIdentifier(currentIdentifier.ToString()));
							currentIdentifier.Clear();
							state = ParserState.ParseNamespaceTypeNameOrParentTypeName;
							continue;
						
						case ParserState.SearchForIdentifierStart:
						case ParserState.ParseTypeNameOrParentTypeName:
						case ParserState.ParseNullablity:
						case ParserState.ParseTrailingSpaces:
						case ParserState.SearchNextTypeArgument:
						default:
							throw new InvalidOperationException();
					}
				}
				else if (chr == "+")
				{
					switch (state)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
						case ParserState.ParseTypeNameOrParentTypeName:
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException();
							name = new CodeIdentifier(currentIdentifier.ToString());
							currentIdentifier.Clear();
							isParent = true;
							break;

						case ParserState.ParseTrailingSpaces:
						case ParserState.ParseNullablity:
						case ParserState.SearchForIdentifierStart:
						case ParserState.SearchNextTypeArgument:
						default:
							throw new InvalidOperationException();
					}
				}
				else if (chr == "<")
				{
					switch (state)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
						case ParserState.ParseTypeNameOrParentTypeName:
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException();
							name = new CodeIdentifier(currentIdentifier.ToString());
							currentIdentifier.Clear();

							var (typeArg, len) = ParseTypeInternal(langServices, null, typeName.Substring(length), valueType, false, true);
							if (typeArg == null)
							{
								openGeneric = true;
								openGenericCount = 1;
							}
							else
							{
								typeArgs.Add(typeArg);
							}

							state = ParserState.SearchNextTypeArgument;

							continue;

						case ParserState.ParseNullablity:
						case ParserState.ParseTrailingSpaces:
						case ParserState.SearchForIdentifierStart:
						case ParserState.SearchNextTypeArgument:
						default:
							throw new InvalidOperationException();
					}
				}
				else if (chr == ",")
				{
					switch (state)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
						case ParserState.ParseTypeNameOrParentTypeName:
							if (!genericList || currentIdentifier.Length == 0)
								throw new InvalidOperationException();
							name = new CodeIdentifier(currentIdentifier.ToString());
							currentIdentifier.Clear();

							length -= chr.Length;
							break;

						case ParserState.ParseNullablity:
						case ParserState.ParseTrailingSpaces:
							if (!genericList)
								throw new InvalidOperationException();
							length -= chr.Length;
							break;

						case ParserState.SearchNextTypeArgument:
							var (typeArg, len) = ParseTypeInternal(langServices, null, typeName.Substring(length), valueType, false, true);
							if (typeArg == null)
							{
								if (openGenericCount == 0)
									throw new InvalidOperationException();
								openGenericCount++;
							}
							else
							{
								if (typeArgs.Count == 0)
									throw new InvalidOperationException();
								typeArgs.Add(typeArg);
							}
							continue;

						case ParserState.SearchForIdentifierStart:
						default:
							throw new InvalidOperationException();
					}
				}
				else if (chr == ">")
				{
					switch (state)
					{
						case ParserState.SearchForIdentifierStart:
							if (typeArgs.Count == 0 && openGenericCount == 0)
							{
								// T<>
								if (genericList)
									return (null, length - chr.Length);

								throw new InvalidOperationException();
							}
							state = ParserState.ParseNullablity;
							continue;

						case ParserState.SearchNextTypeArgument:
							state = ParserState.ParseNullablity;
							continue;

						case ParserState.ParseNullablity:
						case ParserState.ParseTrailingSpaces:
						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
						case ParserState.ParseTypeNameOrParentTypeName:
						default:
							throw new InvalidOperationException();
					}
				}
				else if (chr == "?")
				{
					//nullable = true;

					switch (state)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
						case ParserState.ParseTypeNameOrParentTypeName:
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException();
							name = new CodeIdentifier(currentIdentifier.ToString());
							currentIdentifier.Clear();
							nullable = true;
							state = ParserState.ParseTrailingSpaces;
							continue;

						case ParserState.ParseNullablity:
							nullable = true;
							state = ParserState.ParseTrailingSpaces;
							continue;

						case ParserState.SearchNextTypeArgument:
						case ParserState.ParseTrailingSpaces:
						case ParserState.SearchForIdentifierStart:
						default:
							throw new InvalidOperationException();
					}
				}
				else
				{
					switch (state)
					{
						case ParserState.SearchForIdentifierStart:
							if (!langServices.IsValidIdentifierFirstChar(chr, cat))
								throw new InvalidOperationException();
							currentIdentifier.Append(chr);
							state = ParserState.ParseNamespaceTypeNameOrParentTypeName;
							continue;

						case ParserState.ParseNamespaceTypeNameOrParentTypeName:
						case ParserState.ParseTypeNameOrParentTypeName:
							if (!langServices.IsValidIdentifierNonFirstChar(chr, cat))
								throw new InvalidOperationException();
							currentIdentifier.Append(chr);
							continue;

						case ParserState.SearchNextTypeArgument:
						case ParserState.ParseNullablity:
						case ParserState.ParseTrailingSpaces:
						default:
							throw new InvalidOperationException();
					}
				}
			}

			if ((currentIdentifier.Length != 0 && name != null) || (currentIdentifier.Length == 0 && name == null))
				throw new InvalidOperationException();

			if (currentIdentifier.Length != 0)
				name = new CodeIdentifier(currentIdentifier.ToString());

			IType parsedType;
			if (parentType != null)
			{
				if (openGeneric)
				{
					parsedType = new OpenGenericType(
						parentType,
						name!,
						false,
						valueType,
						nullable,
						openGenericCount,
						true);
				}
				else if (typeArgs.Count > 0)
				{
					parsedType = new GenericType(
						parentType,
						name!,
						false,
						valueType,
						nullable,
						typeArgs.ToArray(),
						true);
				}
				else
				{
					parsedType = new RegularType(
							parentType,
							name!,
							false,
							valueType,
							nullable,
							true);
				}
			}
			else
			{
				var nsArr =  ns.Count > 0 ? ns.ToArray() : null;

				if (openGeneric)
				{
					parsedType = new OpenGenericType(
						nsArr,
						name!,
						false,
						valueType,
						nullable,
						openGenericCount,
						true);
				}
				else if (typeArgs.Count > 0)
				{
					parsedType = new GenericType(
						nsArr,
						name!,
						false,
						valueType,
						nullable,
						typeArgs.ToArray(),
						true);
				}
				else
				{
					var alias = langServices.GetAlias(nsArr, name!.Name);
					parsedType = new RegularType(
							nsArr,
							name,
							false,
							valueType,
							nullable,
							true);
				}
			}

			if (isParent)
			{
				return ParseTypeInternal(langServices, parsedType, typeName.Substring(length), valueType, false, genericList);
			}

			return (parsedType, length);
		}
	}
}
