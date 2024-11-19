using System.Collections.Generic;

using LinqToDB.CodeModel;

using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public class TypeParserTests : TestBase
	{
		private sealed class TestType : IType
		{
			public TypeKind Kind { get; set; }

			public bool IsNullable { get; set; }

			public bool IsValueType { get; set; }

			public IReadOnlyList<CodeIdentifier>? Namespace { get; set; }

			public IType? Parent { get; set; }

			public CodeIdentifier? Name { get; set; }

			public IType? ArrayElementType { get; set; }

			public IReadOnlyList<int?>? ArraySizes { get; set; }

			public int? OpenGenericArgCount { get; set; }

			public IReadOnlyList<IType>? TypeArguments { get; set; }

			public bool External { get; set; }

			public IType WithNullability(bool nullable)
			{
				return nullable == IsNullable ? this : new TestType()
				{
					Kind = Kind,
					IsNullable = nullable,
					IsValueType = IsValueType,
					Namespace = Namespace,
					Parent = Parent,
					Name = Name,
					ArrayElementType = ArrayElementType,
					ArraySizes = ArraySizes,
					OpenGenericArgCount = OpenGenericArgCount,
					TypeArguments = TypeArguments,
					External = External,
				};
			}

			public IType WithTypeArguments(params IType[] typeArguments)
			{
				return typeArguments == TypeArguments ? this : new TestType()
				{
					Kind = Kind,
					IsNullable = IsNullable,
					IsValueType = IsValueType,
					Namespace = Namespace,
					Parent = Parent,
					Name = Name,
					ArrayElementType = ArrayElementType,
					ArraySizes = ArraySizes,
					OpenGenericArgCount = OpenGenericArgCount,
					TypeArguments = typeArguments,
					External = External,
				};
			}
		}

		private static TestCaseData[] TypeParseCases
		{
			get
			{
				var typeName = new CodeIdentifier("type", true);
				var nsName = new CodeIdentifier("ns", true);

				var type = new TestType() { Kind = TypeKind.Regular, Name = typeName };
				var typeN = type.WithNullability(true);

				var typeType = new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = type };
				var typeTypeN = typeType.WithNullability(true);

				var type1Type = new TestType() { Kind = TypeKind.Generic, Name = typeName, TypeArguments = new[]{ type } };
				var type1TypeN = new TestType() { Kind = TypeKind.Generic, Name = typeName, TypeArguments = new[]{ typeN }, IsNullable = true };

				var nsType = new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName } };
				var nsTypeN = nsType.WithNullability(true);

				var nsNsTypeTypeType = new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName, nsName } } } };
				var nsNsTypeNTypeNTypeN = new TestType() { Kind = TypeKind.Regular, Name = typeName, IsNullable = true, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName, nsName } } } };

				var type1 = new TestType() { Kind = TypeKind.OpenGeneric, Name = typeName, OpenGenericArgCount = 1 };
				var type1N = type1.WithNullability(true);

				var type3 = new TestType() { Kind = TypeKind.OpenGeneric, Name = typeName, OpenGenericArgCount = 3 };
				var type3N = type3.WithNullability(true);

				return new[]
				{
					// plain
					new TestCaseData("type", type),
					new TestCaseData("type?", typeN),
					new TestCaseData("ns.type", nsType),
					new TestCaseData("ns.type?", nsTypeN),

					// nested
					new TestCaseData("type+type", typeType),
					new TestCaseData("type+type?", typeTypeN),
					new TestCaseData("ns.ns.type+type+type", nsNsTypeTypeType),
					new TestCaseData("ns.ns.type+type+type?", nsNsTypeNTypeNTypeN),

					// open generic
					new TestCaseData("type<>", type1),
					new TestCaseData("type<>?", type1N),
					new TestCaseData("type<,,>", type3),
					new TestCaseData("type<,,>?", type3N),

					// generic
					new TestCaseData("ns.type<ns.type>", new TestType() { Kind = TypeKind.Generic, Name = typeName, Namespace = new[] { nsName }, TypeArguments = new[]{ new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName } } } }),
					new TestCaseData("ns.type<ns.type?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, Namespace = new[] { nsName }, TypeArguments = new[]{ new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName }, IsNullable = true } }, IsNullable = true }),
					new TestCaseData("type+type<type>", new TestType() { Kind = TypeKind.Generic, Name = typeName, Parent = type, TypeArguments = new[] { type } }),
					new TestCaseData("type+type<type?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, Parent = type, TypeArguments = new[] {typeN }, IsNullable = true }),
					new TestCaseData("type+type<type,type>", new TestType() { Kind = TypeKind.Generic, Name = typeName, Parent = type, TypeArguments = new[] { type, type } }),
					new TestCaseData("type+type<type?,type?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, Parent = type, TypeArguments = new[] { typeN, typeN }, IsNullable = true }),
					new TestCaseData("type+type<ns.type,type+type>", new TestType() { Kind = TypeKind.Generic, Name = typeName, Parent = type, TypeArguments = new[] { nsType, typeType } }),
					new TestCaseData("type+type<ns.type?,type+type?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, Parent = type, TypeArguments = new[] { nsTypeN, typeTypeN }, IsNullable = true }),
					new TestCaseData("ns.type<ns.ns.type+type,ns.type+type>", new TestType() { Kind = TypeKind.Generic, Name = typeName, Namespace = new[]{ nsName }, TypeArguments = new[] { new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName, nsName } } }, new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName } } } } }),
					new TestCaseData("ns.type<ns.ns.type+type?,ns.type+type?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, Namespace = new[]{ nsName }, TypeArguments = new[] { new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName, nsName } }, IsNullable = true }, new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Regular, Name = typeName, Namespace = new[] { nsName } }, IsNullable = true } }, IsNullable = true }),
					new TestCaseData("ns.type<type>+type", new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Generic, Name = typeName, Namespace = new[]{ nsName }, TypeArguments = new[]{ type } } }),
					new TestCaseData("ns.type<type?>+type?", new TestType() { Kind = TypeKind.Regular, Name = typeName, Parent = new TestType() { Kind = TypeKind.Generic, Name = typeName, Namespace = new[]{ nsName }, TypeArguments = new[]{ typeN } }, IsNullable = true }),
					new TestCaseData("type<type<type>,type<type>>", new TestType() { Kind = TypeKind.Generic, Name = typeName, TypeArguments = new[] { type1Type, type1Type } }),
					new TestCaseData("type<type<type?>?,type<type?>?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, TypeArguments = new[] { type1TypeN, type1TypeN }, IsNullable = true }),

					// TODO: for now we don't support arrays by parser (including multidim and sized arrays)
					// array
					//new TestCaseData("type[][,][]", new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], ArrayElementType = new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[2], ArrayElementType = new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], ArrayElementType = type } } }),
					//new TestCaseData("type?[]?[,]?[]?", new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], IsNullable = true, ArrayElementType = new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[2], IsNullable = true, ArrayElementType = new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], ArrayElementType = typeN, IsNullable = true } } }),
					//new TestCaseData("type<type[]>", new TestType() { Kind = TypeKind.Generic, Name = typeName, TypeArguments = new[] { new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], ArrayElementType = type } } }),
					//new TestCaseData("type<type?[]?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, IsNullable = true, TypeArguments = new[] { new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], IsNullable = true, ArrayElementType = typeN } } }),
					//new TestCaseData("type<type[],ns.type[]>", new TestType() { Kind = TypeKind.Generic, Name = typeName, TypeArguments = new[] { new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], ArrayElementType = type }, new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], ArrayElementType = nsType } } }),
					//new TestCaseData("type<type?[]?,ns.type?[]?>?", new TestType() { Kind = TypeKind.Generic, Name = typeName, IsNullable = true, TypeArguments = new[] { new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], IsNullable = true, ArrayElementType = typeN }, new TestType() { Kind = TypeKind.Array, ArraySizes = new int?[1], IsNullable = true, ArrayElementType = nsTypeN } } }),
				};
			}
		}

		[TestCaseSource(nameof(TypeParseCases))]
		public void TestTypeParser(string typeName, IType expectedType)
		{
			var parsedType = LanguageProviders.CSharp.TypeParser.Parse(typeName, false);

			// nunit API just doesn't work
			//Assert.That(parsedType, Is.EqualTo(expectedType).Using(LanguageProviders.CSharp.TypeEqualityComparerWithNRT));
			Assert.That(LanguageProviders.CSharp.TypeEqualityComparerWithNRT.Equals(parsedType, expectedType), Is.True);
		}
	}
}
