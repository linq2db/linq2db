using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using LinqToDB.Extensions;
using LinqToDB.Tools;

namespace LinqToDB.Common.Internal
{
	public static class TypeExtensions
	{
		private static readonly Dictionary<Type, string> _builtInTypeNames = new Dictionary<Type, string>
		{
			{ typeof(bool),    "bool"    },
			{ typeof(byte),    "byte"    },
			{ typeof(char),    "char"    },
			{ typeof(decimal), "decimal" },
			{ typeof(double),  "double"  },
			{ typeof(float),   "float"   },
			{ typeof(int),     "int"     },
			{ typeof(long),    "long"    },
			{ typeof(object),  "object"  },
			{ typeof(sbyte),   "sbyte"   },
			{ typeof(short),   "short"   },
			{ typeof(string),  "string"  },
			{ typeof(uint),    "uint"    },
			{ typeof(ulong),   "ulong"   },
			{ typeof(ushort),  "ushort"  },
			{ typeof(void),    "void"    }
		};

		public static bool IsDefaultValue(this Type type, object? value)
			=> (value == null) || value.Equals(type.GetDefaultValue());

		public static string ShortDisplayName(this Type type)
			=> type.DisplayName(fullName: false);

		public static string DisplayName(this Type type, bool fullName = true)
		{
			var stringBuilder = new StringBuilder();
			ProcessType(stringBuilder, type, fullName);
			return stringBuilder.ToString();
		}

		private static void ProcessType(StringBuilder builder, Type type, bool fullName)
		{
			if (type.IsGenericType)
			{
				var genericArguments = type.GetGenericArguments();
				ProcessGenericType(builder, type, genericArguments, genericArguments.Length, fullName);
			}
			else if (type.IsArray)
			{
				ProcessArrayType(builder, type, fullName);
			}
			else if (_builtInTypeNames.TryGetValue(type, out var builtInName))
			{
				builder.Append(builtInName);
			}
			else if (!type.IsGenericParameter)
			{
				builder.Append(fullName ? type.FullName : type.Name);
			}
		}

		private static void ProcessArrayType(StringBuilder builder, Type type, bool fullName)
		{
			var innerType = type;
			while (innerType.IsArray)
			{
				innerType = innerType.GetElementType()!;
			}

			ProcessType(builder, innerType, fullName);

			while (type.IsArray)
			{
				builder.Append('[');
				builder.Append(',', type.GetArrayRank() - 1);
				builder.Append(']');
				type = type.GetElementType()!;
			}
		}

		private static void ProcessGenericType(StringBuilder builder, Type type, Type[] genericArguments, int length, bool fullName)
		{
			var offset = type.IsNested ? type.DeclaringType!.GetGenericArguments().Length : 0;

			if (fullName)
			{
				if (type.IsNested)
				{
					ProcessGenericType(builder, type.DeclaringType!, genericArguments, offset, fullName);
					builder.Append('+');
				}
				else
				{
					builder.Append(type.Namespace);
					builder.Append('.');
				}
			}

			var genericPartIndex = type.Name.IndexOf('`');
			if (genericPartIndex <= 0)
			{
				builder.Append(type.Name);
				return;
			}

			builder.Append(type.Name, 0, genericPartIndex);
			builder.Append('<');

			for (var i = offset; i < length; i++)
			{
				ProcessType(builder, genericArguments[i], fullName);
				if (i + 1 == length)
				{
					continue;
				}

				builder.Append(',');
				if (!genericArguments[i + 1].IsGenericParameter)
				{
					builder.Append(' ');
				}
			}

			builder.Append('>');
		}

		public static FieldInfo GetFieldInfo(this Type type, string fieldName)
			=> type.GetRuntimeFields().FirstOrDefault(f => f.Name == fieldName && !f.IsStatic);

		public static IEnumerable<string> GetNamespaces(this Type type)
		{
			if (_builtInTypeNames.ContainsKey(type))
			{
				yield break;
			}

			if (type.Namespace != null)
				yield return type.Namespace;

			if (type.GetTypeInfo().IsGenericType)
			{
				foreach (var typeArgument in type.GetTypeInfo().GenericTypeArguments)
				{
					foreach (var ns in typeArgument.GetNamespaces())
					{
						yield return ns;
					}
				}
			}
		}

		public static bool IsNumericType(this Type? type)
		{
			return type != null && type.In(
				typeof(byte),
				typeof(decimal),
				typeof(double),
				typeof(float),
				typeof(int),
				typeof(long),
				typeof(sbyte),
				typeof(short),
				typeof(uint),
				typeof(ulong),
				typeof(ushort)
			);
		}

		public static bool IsSignedType(this Type? type)
		{
			return type != null && type.In(
				typeof(decimal),
				typeof(double),
				typeof(float),
				typeof(int),
				typeof(long),
				typeof(short)
			);
		}

	}
}
