using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using LinqToDB.Extensions;

namespace LinqToDB.Common.Internal
{
	public static class TypeExtensions
	{
		private static readonly Dictionary<Type, string> _builtInTypeNames = new ()
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

		public static FieldInfo? GetFieldInfo(this Type type, string fieldName)
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

		public static Type UnwrapNullableType(this Type type)
			=> Nullable.GetUnderlyingType(type) ?? type;

		/// <summary>
		/// Returns <c>true</c> if type is reference type or <see cref="Nullable{T}"/>.
		/// </summary>
		/// <param name="type">Type to test.</param>
		/// <returns><c>true</c> if type is reference type or <see cref="Nullable{T}"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullableType(this Type type)
			=> !type.IsValueType || type.IsNullable();


		public static bool IsInteger(this Type type)
		{
			type = type.UnwrapNullableType();

			return type    == typeof(int)
			       || type == typeof(long)
			       || type == typeof(short)
			       || type == typeof(byte)
			       || type == typeof(uint)
			       || type == typeof(ulong)
			       || type == typeof(ushort)
			       || type == typeof(sbyte)
			       || type == typeof(char);
		}

		public static bool IsNumericType(this Type? type)
		{
			if (type == null)
				return false;

			type = type.UnwrapNullableType();

			return type.IsInteger()
			       || type == typeof(decimal)
			       || type == typeof(float)
			       || type == typeof(double);
		}

		public static bool IsSignedInteger(this Type type)
		{
			return type    == typeof(int)
			       || type == typeof(long)
			       || type == typeof(short)
			       || type == typeof(sbyte);
		}

		public static bool IsSignedType(this Type? type)
		{
			return type != null &&
			       (IsSignedInteger(type)
			        || type == typeof(decimal)
			        || type == typeof(double)
			        || type == typeof(float)
			       );
		}

		public static bool IsTupleType(this Type type)
		{
			if (type == typeof(Tuple))
			{
				return true;
			}

			if (type.IsGenericType)
			{
				var genericDefinition = type.GetGenericTypeDefinition();
				if (genericDefinition    == typeof(Tuple<>)
				    || genericDefinition == typeof(Tuple<,>)
				    || genericDefinition == typeof(Tuple<,,>)
				    || genericDefinition == typeof(Tuple<,,,>)
				    || genericDefinition == typeof(Tuple<,,,,>)
				    || genericDefinition == typeof(Tuple<,,,,,>)
				    || genericDefinition == typeof(Tuple<,,,,,,>)
				    || genericDefinition == typeof(Tuple<,,,,,,,>)
				    || genericDefinition == typeof(Tuple<,,,,,,,>))
				{
					return true;
				}
			}

			return false;
		}
	}
}
