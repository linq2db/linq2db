using System;
using System.Collections.Generic;

namespace LinqToDB.LINQPad
{
	// TODO: move to linq2db.Tools (see CSharpCodeGenerator.KeyWords)
	internal static class CSharpUtils
	{
		// C# keywords and contextual words
		// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
		private static readonly HashSet<string> KeyWords = new (StringComparer.Ordinal)
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
			"select"  , "set"     , "unmanaged", "value"    , "var"      , "when"    , "where"  , "with"      , "yield",
		};

		public static string EscapeIdentifier(string identifier)
		{
			if (KeyWords.Contains(identifier))
			{
				return $"@{identifier}";
			}

			return identifier;
		}
	}
}
