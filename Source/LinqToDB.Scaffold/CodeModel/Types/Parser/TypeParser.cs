using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LinqToDB.CodeModel
{
	internal sealed class TypeParser : ITypeParser
	{
		private static Dictionary<Type, IType> _typeCache { get; } = new ();

		private readonly ILanguageProvider _languageProvider;

		public TypeParser(ILanguageProvider languageProvider)
		{
			_languageProvider = languageProvider;
		}

		#region ITypeParser
		IType ITypeParser.Parse   (Type type                      ) => ParseCLRType(type);
		IType ITypeParser.Parse<T>(                               ) => ParseCLRType(typeof(T));
		IType ITypeParser.Parse   (string typeName, bool valueType)
		{
			var (type, consumedCharacters) = ParseTypeInternal(null, typeName, valueType, false);

			if (type == null || consumedCharacters != typeName.Length)
				throw new InvalidOperationException($"Cannot parse type: {typeName}");

			return type;
		}

		CodeIdentifier[] ITypeParser.ParseNamespaceOrRegularTypeName(string name, bool generated)
		{
			// simplified version of type parser
			// parsed name
			var ns = new List<CodeIdentifier>();

			// parser position in parsed string
			var position          = 0;
			var currentIdentifier = new StringBuilder();
			var parserState       = ParserState.SearchForIdentifierStart;

			// use state machine for type parsing
			foreach (var (chr, cat) in name.EnumerateCharacters())
			{
				position += chr.Length;

				// there are several characters that could trigger parser state change:
				// " " (as filler before/after name)
				// "." (as name components separator)
				// other characters expected to be valid namespace/type name characters with regards to their position in name
				// (like first character or rest character)
				if (string.Equals(chr, " ", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseTrailingSpaces                   : // consume trailing spaces
						case ParserState.SearchForIdentifierStart              : // consume leading spaces
							continue;
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // end of name
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse name \"{name}\": space found at unexpected position");

							// consume trailing spaces
							parserState = ParserState.ParseTrailingSpaces;
							continue;
						default:
							throw new InvalidOperationException($"Invalid name parser state: {parserState}");
					}
				}
				else if (string.Equals(chr, ".", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // dot after name identifer
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse name \"{name}\": dot found but name expected");

							// save name component
							ns.Add(new CodeIdentifier(currentIdentifier.ToString(), !generated));

							// reset current identifier and parse for next name component or type name (no state change)
							currentIdentifier.Clear();
							continue;
						case ParserState.SearchForIdentifierStart              : // identifier cannot start with .
						case ParserState.ParseTrailingSpaces                   : // only spaces allowed after type
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse name \"{name}\": unexpected dot found at {position}"));
						default:
							throw new InvalidOperationException($"Invalid name parser state: {parserState}");
					}
				}
				else // any other character (it should be identifier character only)
				{
					switch (parserState)
					{
						case ParserState.SearchForIdentifierStart              : // identifier start character expected
							if (!_languageProvider.IsValidIdentifierFirstChar(chr, cat))
								throw new InvalidOperationException($"Cannot parse name \"{name}\": identifier cannot start from \"{chr}\"");

							currentIdentifier.Append(chr);
							parserState = ParserState.ParseNamespaceTypeNameOrParentTypeName;
							continue;
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // identifier non-start character expected
							if (!_languageProvider.IsValidIdentifierNonFirstChar(chr, cat))
								throw new InvalidOperationException($"Cannot parse name \"{name}\": identifier cannot contain \"{chr}\"");

							currentIdentifier.Append(chr);
							continue;
						case ParserState.ParseTrailingSpaces                   : // spaces expected
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse name \"{name}\": unexpected character at {position}"));
						default:
							throw new InvalidOperationException($"Invalid name parser state: {parserState}");
					}
				}
			}

			// TODO: should it be possible or we save name in parser always so we should throw assert exception here?
			if (currentIdentifier.Length != 0)
				ns.Add(new CodeIdentifier(currentIdentifier.ToString(), !generated));

			if (ns.Count == 0 || position != name.Length)
				throw new InvalidOperationException($"Cannot parse name: {name}");

			return ns.ToArray();
		}
		#endregion

		#region type parser
		/// <summary>
		/// Type parser state indicators.
		/// </summary>
		private enum ParserState
		{
			/// <summary>
			/// Parser is looking for type or sub-type starting character.
			/// </summary>
			SearchForIdentifierStart,
			/// <summary>
			/// Parser is parsing identifier that could be namespace sub-component, type name or (parent) type name.
			/// </summary>
			ParseNamespaceTypeNameOrParentTypeName,
			/// <summary>
			/// Parser is parsing type name for current type or parent type.
			/// </summary>
			ParseTypeNameOrParentTypeName,
			/// <summary>
			/// Parses is looking for optional nullability marker after type name.
			/// </summary>
			ParseNullablity,
			/// <summary>
			/// Parser consume all space characters to the end of string.
			/// </summary>
			ParseTrailingSpaces,
			/// <summary>
			/// Parser is looking for next type argument or end of type argument list.
			/// </summary>
			SearchNextTypeArgument,
		}

		/// <summary>
		/// Parse type from string.
		/// </summary>
		/// <param name="parentType">Optional parent type for currently parsed type (such type cannot have namespace).</param>
		/// <param name="typeName">Type name string to parse.</param>
		/// <param name="valueType">Indicate wether parsed type should be treated as value type or as reference type.</param>
		/// <param name="typeArgument">Parsed type located at generic type argument position.</param>
		/// <returns>Parsed type or null for empty open generic argument and type name length in parsed string.</returns>
		private (IType? type, int consumedCharacters) ParseTypeInternal(
			IType? parentType,
			string typeName,
			bool   valueType,
			bool   typeArgument)
		{
			// parsed type properties
			var ns               = new List<CodeIdentifier>();
			var nullable         = false;
			var openGeneric      = false;
			var openGenericCount = 0;
			var typeArgs         = new List<IType>();
			CodeIdentifier? name = null;

			// parser position in type string
			var position          = 0;
			var isParent          = false;
			var currentIdentifier = new StringBuilder();
			var parserState       = parentType != null ? ParserState.ParseTypeNameOrParentTypeName : ParserState.SearchForIdentifierStart;
			var skip              = 0;
			var isTypeArgument    = false;

			// use state machine for type parsing
			foreach (var (chr, cat) in typeName.EnumerateCharacters())
			{
				position += chr.Length;

				if (skip > 0)
				{
					skip -= chr.Length;
					continue;
				}

				// there are several characters that could trigger parser state change:
				// " " (as filler before/after type or type arguments)
				// "." (as namespaces and namespace/type separator)
				// "+" (as parent and nested type separator)
				// "<" (as generic arguments list open bracket)
				// ">" (as generic arguments list close bracket)
				// "," (as generic arguments list separator)
				// "?" (as nullable type marker)
				// other characters expected to be valid namespace/type name characters with regrards to their position in name
				// (like first character or rest character)
				
				if (string.Equals(chr, " ", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseTrailingSpaces                   : // consume trailing spaces
						case ParserState.SearchNextTypeArgument                : // consume padding spaces
						case ParserState.SearchForIdentifierStart              : // consume leading spaces
							continue;
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // end of type
						case ParserState.ParseTypeNameOrParentTypeName         : // end of type
							// validate we have identifier
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": whitespace found but name expected");

							// consume trailing spaces
							parserState = ParserState.ParseTrailingSpaces;
							continue;
						case ParserState.ParseNullablity                       :  // nullability marker absent for type
							// consume any trailing whitespaces
							parserState = ParserState.ParseTrailingSpaces;
							continue;
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}
				else if (string.Equals(chr, ".", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // dot after namespace identifer
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": dot found but name expected");

							// save namespace component
							ns.Add(new CodeIdentifier(currentIdentifier.ToString(), true));

							// reset current identifier and parse for next namespace component or type name (no state change)
							currentIdentifier.Clear();
							continue;
						case ParserState.SearchForIdentifierStart              : // identifier cannot start with .
						case ParserState.ParseTypeNameOrParentTypeName         : // type name cannot have . separator (only +)
						case ParserState.ParseNullablity                       : // type cannot end with .
						case ParserState.ParseTrailingSpaces                   : // only spaces allowed after type
						case ParserState.SearchNextTypeArgument                : // type arguments list cannot have .
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected dot found at {position}"));
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}
				else if (string.Equals(chr, "+", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // current identifier is parent type name
						case ParserState.ParseTypeNameOrParentTypeName         : // current identifier is parent type name
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": + found but name expected");

							// save identifier as type name
							name = new CodeIdentifier(currentIdentifier.ToString(), true);

							// stop parsing to create parent type token and continue nested type parsing
							currentIdentifier.Clear();
							isParent = true;
							break;
						case ParserState.ParseNullablity                       : // ? or space expected
						{
							isParent = true;
							break;
						}
						case ParserState.ParseTrailingSpaces                   : // space expected
						case ParserState.SearchForIdentifierStart              : // identifier start character expected or space
						case ParserState.SearchNextTypeArgument                : // ,> or identifier start character expected
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected + found at {position}"));
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}
				else if (string.Equals(chr, "<", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // current identifier is a name of generic type
						case ParserState.ParseTypeNameOrParentTypeName         : // current identifier is a name of generic type
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": < found but name expected");

							// save identifier as type name
							name = new CodeIdentifier(currentIdentifier.ToString(), true);

							currentIdentifier.Clear();

							// parse first type argument
							(var typeArg, skip) = ParseTypeInternal(null, typeName.Substring(position), false, true);
							if (typeArg == null)
							{
								// no type arguments - open generic type with 1 argument
								openGeneric      = true;
								openGenericCount = 1;
							}
							else // add first type argument to list of type arguments and look for more type arguments
								typeArgs.Add(typeArg);
							parserState = ParserState.SearchNextTypeArgument;
							continue;

						case ParserState.ParseNullablity                       :
						case ParserState.ParseTrailingSpaces                   :
						case ParserState.SearchForIdentifierStart              :
						case ParserState.SearchNextTypeArgument                :
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected < found at {position}"));
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}
				else if (string.Equals(chr, ",", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // current identifier is a type name for generic type argument
						case ParserState.ParseTypeNameOrParentTypeName         : // current identifier is a type name for generic type argument
							if (!typeArgument)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": \",\" found outside of generic type arguments list");

							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": , found but name expected");

							// save type name
							name = new CodeIdentifier(currentIdentifier.ToString(), true);
							currentIdentifier.Clear();

							// reset position to include only current type name (so parent parser will resume parsing from ,)
							position -= chr.Length;
							isTypeArgument = true;
							break;
						case ParserState.ParseNullablity                       : // type argument has no nullability marker
						case ParserState.ParseTrailingSpaces                   : // skipped trailing spaces for previous type argument
							if (!typeArgument)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": \",\" found outside of generic type arguments list");

							// reset position to include only current type name (so parent parser will resume parsing from ,)
							position -= chr.Length;
							isTypeArgument = true;
							break;
						case ParserState.SearchNextTypeArgument                : // found next type argument start
							// parse next type argument
							(var typeArg, skip) = ParseTypeInternal(null, typeName.Substring(position), false, true);
							// empty position found
							if (typeArg == null)
							{
								// error: we are not in open generic type
								if (openGenericCount == 0)
									throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": empty generic type argument found for generic type");

								openGenericCount++;
							}
							else
							{
								// error: open generic type with type specified
								if (typeArgs.Count == 0)
									throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": generic type argument found for open generic type");
								typeArgs.Add(typeArg);
							}

							continue;
						case ParserState.SearchForIdentifierStart              : // unexpected comma outside of generic type arguments list
							if (!typeArgument)
								throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected identifier at {position}"));

							// emtpy type argument position (open generic type argument)
							return (null, position - chr.Length);
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}
				else if (string.Equals(chr, ">", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.SearchForIdentifierStart              : // was looking for type argument identifier
							if (!typeArgument)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": \">\" found outside of generic type arguments list");

							// emtpy type argument position (open generic type argument)
							return (null, position - chr.Length);
						case ParserState.SearchNextTypeArgument                : // type arguments list ended
							// check for generic type nullability marker
							parserState = ParserState.ParseNullablity;
							continue;

						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // current identifier is a type name for generic type argument
						case ParserState.ParseTypeNameOrParentTypeName         : // current identifier is a type name for generic type argument
							if (!typeArgument)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": \",\" found outside of generic type arguments list");

							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": , found but name expected");

							// save type name
							name = new CodeIdentifier(currentIdentifier.ToString(), true);
							currentIdentifier.Clear();

							// reset position to include only current type name (so parent parser will resume parsing from ,)
							position -= chr.Length;
							isTypeArgument = true;
							break;
						case ParserState.ParseNullablity                       : // only spaces or ? expected after type
							if (typeArgument)
							{
								isTypeArgument = true;
								break;
							}

							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected > at {position}"));
						case ParserState.ParseTrailingSpaces                   : // only spaces expected after type
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected > at {position}"));
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}
				else if (string.Equals(chr, "?", StringComparison.Ordinal))
				{
					switch (parserState)
					{
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // identifier is type name
						case ParserState.ParseTypeNameOrParentTypeName         : // identifier is type name
							if (currentIdentifier.Length == 0)
								throw new InvalidOperationException($"Cannot parse type name \"{typeName}\": , found but name expected");

							name = new CodeIdentifier(currentIdentifier.ToString(), true);
							currentIdentifier.Clear();
							goto case ParserState.ParseNullablity;
						case ParserState.ParseNullablity                       : // looking for nullability marker
							// mark type nullable
							nullable    = true;
							if (typeArgument)
							{
								isTypeArgument = true;
								break;
							}
							// remove any trailing spaces
							parserState = ParserState.ParseTrailingSpaces;
							continue;
						case ParserState.SearchNextTypeArgument                : // type argument or , or > expected
						case ParserState.ParseTrailingSpaces                   : // only spaces expected
						case ParserState.SearchForIdentifierStart              : // identifier expected
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected ? at {position}"));
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}
				else // any other character (it should be identifier character only)
				{
					switch (parserState)
					{
						case ParserState.SearchForIdentifierStart              : // identifier start character expected
							if (!_languageProvider.IsValidIdentifierFirstChar(chr, cat))
								throw new InvalidOperationException($"Cannot parse type \"{typeName}\": identifier cannot start from \"{chr}\"");

							currentIdentifier.Append(chr);
							parserState = ParserState.ParseNamespaceTypeNameOrParentTypeName;
							continue;
						case ParserState.ParseNamespaceTypeNameOrParentTypeName: // identifier non-start character expected
						case ParserState.ParseTypeNameOrParentTypeName         : // identifier non-start character expected
							if (!_languageProvider.IsValidIdentifierNonFirstChar(chr, cat))
								throw new InvalidOperationException($"Cannot parse type \"{typeName}\": identifier cannot contain \"{chr}\"");

							currentIdentifier.Append(chr);
							continue;
						case ParserState.SearchNextTypeArgument                : // < or , expected
						case ParserState.ParseNullablity                       : // ? or space expected
						case ParserState.ParseTrailingSpaces                   : // spaces expected
							throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Cannot parse type \"{typeName}\": unexpected character at {position}"));
						default                                                :
							throw new InvalidOperationException($"Invalid type parser state: {parserState}");
					}
				}

				if (isParent || isTypeArgument)
				{
					break;
				}
			}

			// assert parser state once more
			if ((currentIdentifier.Length != 0 && name != null)
				|| (currentIdentifier.Length == 0 && name == null))
				throw new InvalidOperationException("Invalid type parser state");

			// TODO: should it be possible or we save name in parser always so we should throw assert exception here?
			if (currentIdentifier.Length != 0)
				name = new CodeIdentifier(currentIdentifier.ToString(), true);

			// create type descriptor
			IType parsedType;
			if (parentType != null)
			{
				// create parent type descriptor
				if (openGeneric)
					parsedType = new OpenGenericType(
						parentType,
						name!,
						false,
						nullable,
						openGenericCount,
						true);
				else if (typeArgs.Count > 0)
					parsedType = new GenericType(
						parentType,
						name!,
						false,
						nullable,
						typeArgs.ToArray(),
						true);
				else
					parsedType = new RegularType(
							parentType,
							name!,
							false,
							nullable,
							true);
			}
			else
			{
				// create top-level or generic argument type descriptor
				var @namespace =  ns.Count > 0 ? ns.ToArray() : null;

				if (openGeneric)
					parsedType = new OpenGenericType(
						@namespace,
						name!,
						valueType,
						nullable,
						openGenericCount,
						true);
				else if (typeArgs.Count > 0)
					parsedType = new GenericType(
						@namespace,
						name!,
						valueType,
						nullable,
						typeArgs.ToArray(),
						true);
				else
				{
					// currently supported languages doesn't have built-in aliases for generic types
					// and they cannot have child types, so we check for alias only in this branch
					parsedType = new RegularType(
							@namespace,
							name!,
							valueType,
							nullable,
							true);
				}
			}

			if (isParent)
			{
				var (t, l) = ParseTypeInternal(parsedType, typeName.Substring(position), valueType, typeArgument);
				return (t, l + position);
			}

			return (parsedType, position);
		}
		#endregion

		/// <summary>
		/// Parse .NET type (<see cref="Type"/>) without NRT information.
		/// NRT information could be added by caller to produced type descriptor.
		/// </summary>
		/// <param name="type">Type to parse.</param>
		/// <returns>Parsed type token.</returns>
		internal IType ParseCLRType(Type type)
		{
			if (_typeCache.TryGetValue(type, out var cachedType))
				return cachedType;

			// parse type wrapped in Nullable<T> wrapper
			var isNullable = type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(Nullable<>);
			if (isNullable)
				return _typeCache[type] = ParseCLRType(type.GenericTypeArguments[0]).WithNullability(true);

			// parse array type
			if (type.IsArray)
				// we don't have sizes information in Type
				return _typeCache[type] = new ArrayType(ParseCLRType(type.GetElementType()!), new int?[type.GetArrayRank()], false);

			// current type is nested type, parse parent type first
			IType? parent = null;
			if (type.DeclaringType != null)
				parent = ParseCLRType(type.DeclaringType);

			// type is generic type argument token
			if (type.IsGenericParameter)
				// TODO: not needed currently
				throw new InvalidOperationException("Generic type arguments not supported by type parser");

			// regular or generic type
			CodeIdentifier[]? ns = null;
			if (type.Namespace != null)
				ns = ((ITypeParser)this).ParseNamespaceOrRegularTypeName(type.Namespace, false);

			var name  = new CodeIdentifier(type.IsGenericType ? type.Name.Substring(0, type.Name.IndexOf('`', StringComparison.Ordinal)) : type.Name, true);

			// generic/open generic type
			if (type.IsGenericType)
			{
				var typeArgs = type.GetGenericArguments();

				// open generic type
				if (type.IsGenericTypeDefinition)
				{
					if (parent != null)
						return _typeCache[type] = new OpenGenericType(parent, name, type.IsValueType, isNullable, typeArgs.Length, true);
					else
						return _typeCache[type] = new OpenGenericType(ns, name, type.IsValueType, isNullable, typeArgs.Length, true);
				}

				// generic type
				var typeArguments = new IType[typeArgs.Length];
				for (var i = 0; i < typeArgs.Length; i++)
					// TODO: extranct missing NRT nullability here from attributes?
					typeArguments[i] = ParseCLRType(typeArgs[i]);

				if (parent != null)
					return _typeCache[type] = new GenericType(parent, name, type.IsValueType, isNullable, typeArguments, true);
				else
					return _typeCache[type] = new GenericType(ns, name, type.IsValueType, isNullable, typeArguments, true);
			}

			// regular type
			if (parent != null)
				return _typeCache[type] = new RegularType(parent, name, type.IsValueType, isNullable, true);
			else
				return _typeCache[type] = new RegularType(ns, name, type.IsValueType, isNullable, true);
		}
	}
}
