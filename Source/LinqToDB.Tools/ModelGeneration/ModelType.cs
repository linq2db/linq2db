using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public class ModelType
	{
		public static ModelType Create<TType>(bool referenceNullable)
		{
			return Create(typeof(TType), referenceNullable);
		}

		public static ModelType Create(Type type, bool referenceNullable)
		{
			if (type.IsArray)
				return Array(Create(type.GetElementType()!, false), referenceNullable);

			return new ModelType(type, referenceNullable);
		}

		public static ModelType Array(ModelType elementType, bool referenceNullable)
		{
			return new ModelType(elementType, referenceNullable);
		}

		public ModelType(Type type, bool nullable, params ModelType[] typeArguments)
		{
			if (type.IsConstructedGenericType)
			{
				if (typeArguments is { Length : > 0 })
					throw new ArgumentException($"{type} must be open generic type or {typeArguments} should be empty");

				if (!_aliasedTypes.ContainsKey(type))
					_arguments = [..type.GetGenericArguments().Select(a => new ModelType(a, /* we don't have type info here */false))];
			}

			Type        = type;
			IsReference = !type.IsValueType;
			IsNullable  = nullable || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));

			if (typeArguments is { Length: > 0 })
				_arguments = [..typeArguments];
		}

		public ModelType(string type, bool referenceType, bool isNullable, params ModelType[] typeArguments)
		{
			TypeName    = type;
			IsReference = referenceType;
			IsNullable  = isNullable;

			if (typeArguments is { Length: > 0 })
				_arguments = [..typeArguments];
		}

		// Array constructor.
		//
		public ModelType(ModelType elementType, bool isNullable)
		{
			ElementType = elementType;
			IsReference = true;
			IsNullable  = isNullable;
			IsArray     = true;
		}

		public Type?      Type        { get; }
		public string?    TypeName    { get; }
		public ModelType? ElementType { get; }
		public bool       IsReference { get; }
		public bool       IsNullable  { get; }
		public bool       IsArray     { get; }

		readonly List<ModelType>       _arguments = [];
		public   IEnumerable<ModelType> Arguments => _arguments;

		static readonly IDictionary<Type, string> _aliasedTypes = new Dictionary<Type,string>
		{
			{ typeof(bool),     "bool"     },
			{ typeof(byte),     "byte"     },
			{ typeof(sbyte),    "sbyte"    },
			{ typeof(char),     "char"     },
			{ typeof(decimal),  "decimal"  },
			{ typeof(double),   "double"   },
			{ typeof(float),    "float"    },
			{ typeof(int),      "int"      },
			{ typeof(uint),     "uint"     },
			{ typeof(long),     "long"     },
			{ typeof(ulong),    "ulong"    },
			{ typeof(object),   "object"   },
			{ typeof(short),    "short"    },
			{ typeof(ushort),   "ushort"   },
			{ typeof(string),   "string"   },
			{ typeof(bool?),    "bool?"    },
			{ typeof(byte?),    "byte?"    },
			{ typeof(sbyte?),   "sbyte?"   },
			{ typeof(char?),    "char?"    },
			{ typeof(decimal?), "decimal?" },
			{ typeof(double?),  "double?"  },
			{ typeof(float?),   "float?"   },
			{ typeof(int?),     "int?"     },
			{ typeof(uint?),    "uint?"    },
			{ typeof(long?),    "long?"    },
			{ typeof(ulong?),   "ulong?"   },
			{ typeof(short?),   "short?"   },
			{ typeof(ushort?),  "ushort?"  }
		};

		public string ToTypeName()
		{
			var sb = new StringBuilder();

			if (TypeName != null)
				sb.Append(TypeName);
			else if (Type != null)
				sb.Append(_aliasedTypes.TryGetValue(Type, out var type) ? type : Type.Name[..(Type.Name.IndexOf('`') < 0 ? Type.Name.Length : Type.Name.IndexOf('`'))]);
			else
				sb.Append(ElementType?.ToTypeName());

			if (_arguments.Count > 0)
			{
				sb.Append('<');
				sb.Append(string.Join(", ", _arguments.Select(a => a.ToTypeName())));
				sb.Append('>');
			}

			if (IsArray)
				sb.Append("[]");

			if (ModelGenerator.EnableNullableReferenceTypes && IsReference && IsNullable)
				sb.Append('?');

			return sb.ToString();
		}
	}
}
