using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.Model;
using Tests;

namespace Tests
{
	[AttributeUsage(AttributeTargets.All)]
	public class DataTypeAttribute : Attribute
	{
		public DataTypeAttribute(LinqToDB.DataType dataType)
		{
		}
		public LinqToDB.DataType DataType { get; set; }
	}
}

namespace LinqToDB.Tools
{
	// currently takes care of most duplicate name conflicts
	// most of following known issues should be fixed only if they will become issue
	// TODO: "this." use
	// TODO: type namespace/alias use
	// TODO: add base class/interface conflicts testing
		// TODO: add external conflicts testing (by renaming DUP to e.g. String)
	// TODO: add member access in method(property/ctor) testing
	// TODO: add type access in method(property/ctor) testing
	// TODO: in-method variable scope tracking
	internal static class NameNormalizationTest
	{
		// this name used to name all objects to test:
		// 1. duplicate name conflicts resolve
		// 2. proper conflict resolve with external System.Linq.ParallelMergeOptions enum *not used* in generated code directly
		// 3. test propel namespace prefix generation for multi-part namespace
		const string DUPLICATE_NAME1 = "ParallelMergeOptions";
		// this name used to name all objects to test:
		// 1. duplicate name conflicts resolve (same as DUPLICATE_NAME1)
		// 2. proper conflict resolve with external LinqToDB.DataType enum *used* in generated code directly
		const string DUPLICATE_NAME2 = "DataType";

		// name of methods for overload resolution conflicts
		const string OVERLOAD = "Overload";

		// several special names
		const string VALUE = "value";
		const string THIS = "this";
		const string BASE = "base";

		private static IType _type = null!;
		private static IType _typeNotNull = null!;
		private static IType _filterType = null!;
		private static IType _enumerableType = null!;
		private static IType _attributeType = null!;
		private static ICodeExpression _value = null!;

		public static void NormalizationTest()
		{
			var languageProvider = LanguageProviders.CSharp;

			var builder = new CodeBuilder(languageProvider);
			var modelFile = builder.File("TestNormalization");

			_attributeType = builder.Type(typeof(DataTypeAttribute), false);
			_type = WellKnownTypes.LinqToDB.DataType.WithNullability(true);
			_typeNotNull = WellKnownTypes.LinqToDB.DataType;
			_filterType = WellKnownTypes.System.Func(WellKnownTypes.System.Boolean, _typeNotNull, WellKnownTypes.System.Int32);
			_enumerableType = WellKnownTypes.System.Collections.Generic.IEnumerable(_typeNotNull);
			_value = builder.Constant(DataType.Int32, true);

			// name
			modelFile.Imports.Add(builder.Import(new[] { new CodeIdentifier("System"), new CodeIdentifier("Linq") }));
			modelFile.Imports.Add(builder.Import(new[] { new CodeIdentifier("LinqToDB") }));
			modelFile.Imports.Add(builder.Import(new[] { new CodeIdentifier("Tests") }));
			modelFile.Add(CreateNamespace(builder, false, DUPLICATE_NAME1, DUPLICATE_NAME2, true));
			modelFile.Add(CreateNamespace(builder, false, DUPLICATE_NAME2, DUPLICATE_NAME1, false));

			var classes = new ClassGroup(null);
			modelFile.Add(classes);
			CreateClass(builder, classes.New(new(DUPLICATE_NAME1)), false, false, DUPLICATE_NAME1, DUPLICATE_NAME2, true, true);
			CreateClass(builder, classes.New(new(DUPLICATE_NAME2)), false, false, DUPLICATE_NAME2, DUPLICATE_NAME1, true, false);

			CreateClass(builder, classes.New(new(DUPLICATE_NAME1)), false, false, DUPLICATE_NAME1, DUPLICATE_NAME2, false, true);
			CreateClass(builder, classes.New(new(DUPLICATE_NAME2)), false, false, DUPLICATE_NAME2, DUPLICATE_NAME1, false, false);
			CreateClass(builder, classes.New(new(DUPLICATE_NAME1)), false, true, DUPLICATE_NAME1, DUPLICATE_NAME2, false, true);
			CreateClass(builder, classes.New(new(DUPLICATE_NAME2)), false, true, DUPLICATE_NAME2, DUPLICATE_NAME1, false, false);

			var namesNormalization = languageProvider.GetIdentifiersNormalizer();
			namesNormalization.Visit(modelFile);
			var settings = new CodeGenerationSettings();

			var nameScopes = new NameScopesCollector(languageProvider);
			nameScopes.Visit(modelFile);

			CodeGenerationVisitor codeGenerator = languageProvider.GetCodeGenerator(
				settings.NewLine,
				settings.Indent ?? "\t",
				settings.NullableReferenceTypes,
				nameScopes.TypesNamespaces,
				nameScopes.ScopesWithNames);
			codeGenerator.Visit(modelFile);
			File.WriteAllText(@"..\..\..\Generated\Test.cs", codeGenerator.GetResult());
		}

		private static CodeNamespace CreateNamespace(CodeBuilder codeModel, bool final, string name1, string name2, bool withEmptyClasses)
		{
			var ns = codeModel.Namespace($"{name1}.{name1}.{name1}");

			if (withEmptyClasses)
			{
				CreateClass(codeModel, ns.Classes().New(codeModel.Name(name1)), false, false, name1, name2, true, true);
				CreateClass(codeModel, ns.Classes().New(codeModel.Name(name2)), false, false, name2, name1, true, false);
			}

			CreateClass(codeModel, ns.Classes().New(codeModel.Name(name1)), false, false, name1, name2, false, true);
			CreateClass(codeModel, ns.Classes().New(codeModel.Name(name1)), false, true, name1, name2, false, false);
			CreateClass(codeModel, ns.Classes().New(codeModel.Name(name2)), false, false, name1, name2, false, true);
			CreateClass(codeModel, ns.Classes().New(codeModel.Name(name2)), false, true, name1, name2, false, false);

			return ns.Namespace;
		}

		private static CodeClass CreateClass(CodeBuilder codeModel, ClassBuilder builder, bool withBase, bool methodsOnly, string name1, string name2, bool empty, bool withEmptyClasses)
		{
			builder.Public();
			AddAttribute(codeModel, builder.AddAttribute(_attributeType));

			if (withBase)
				throw new NotImplementedException();

			if (!empty)
			{
				if (!methodsOnly)
				{
					// field
					CreateField(codeModel, builder, name1);
					CreateField(codeModel, builder, name1);
					CreateField(codeModel, builder, name2);
					CreateField(codeModel, builder, name2);

					// property
					CreateProperty(codeModel, builder, name1, name2);
					CreateProperty(codeModel, builder, name1, name2);
					CreateProperty(codeModel, builder, name2, name1);
					CreateProperty(codeModel, builder, name2, name1);
				}

				// method
				CreateMethod(codeModel, builder, false, name1, name2);
				CreateMethod(codeModel, builder, true, name1, name2);
				CreateMethod(codeModel, builder, false, name2, name1);
				CreateMethod(codeModel, builder, true, name2, name1);

				// test overload
				CreateOverloadMethod(codeModel, builder, false, ParameterDirection.In, name1);
				CreateOverloadMethod(codeModel, builder, true, ParameterDirection.Ref, name1);
				CreateOverloadMethod(codeModel, builder, false, ParameterDirection.Out, name1);
				CreateOverloadMethod(codeModel, builder, false, ParameterDirection.In, name2);
				CreateOverloadMethod(codeModel, builder, true, ParameterDirection.Ref, name2);
				CreateOverloadMethod(codeModel, builder, false, ParameterDirection.Out, name2);

				// ctors
				CreateCtor(codeModel, builder, name1, name2);

				// nested types
				// test 2 levels of nesting
				if (builder.Type.Type.Parent == null || builder.Type.Type.Parent.Parent == null)
				{
					if (withEmptyClasses)
					{
						CreateClass(codeModel, builder.Classes().New(codeModel.Name(name1)), false, false, name1, name2, true, true);
						CreateClass(codeModel, builder.Classes().New(codeModel.Name(name2)), false, false, name2, name1, true, false);
					}

					CreateClass(codeModel, builder.Classes().New(codeModel.Name(name1)), false, false, name1, name2, false, true);
					CreateClass(codeModel, builder.Classes().New(codeModel.Name(name1)), false, true, name1, name2, false, false);
					CreateClass(codeModel, builder.Classes().New(codeModel.Name(name2)), false, false, name2, name1, false, true);
					CreateClass(codeModel, builder.Classes().New(codeModel.Name(name2)), false, true, name2, name1, false, false);
				}
			}

			return builder.Type;
		}

		private static void AddAttribute(CodeBuilder codeModel, AttributeBuilder builder)
		{
			builder
				.Parameter(_value)
				.Parameter(codeModel.Name(nameof(DataTypeAttribute.DataType)), _value);
		}

		private static void CreateField(CodeBuilder codeModel, ClassBuilder builder, string name1)
		{
			builder.Fields(false).New(codeModel.Name(name1), _type).Public()
				.AddInitializer(_value);

			// TODO: not supported in code model now
			//AddAttribute(codeModel, field.AddAttribute(_attributeType));
		}

		private static void CreateProperty(CodeBuilder codeModel, ClassBuilder builder, string name1, string name2)
		{
			var prop = builder.Properties(false).New(codeModel.Name(name1), _type);

			AddAttribute(codeModel, prop.AddAttribute(_attributeType));

			CreateBody(codeModel, prop.AddGetter(), 0, name1, name2)
				.Append(codeModel.Return(_value));

			CreateBody(codeModel, prop.AddSetter(), 0, name1, name2);
		}

		private static BlockBuilder CreateBody(CodeBuilder codeModel, BlockBuilder block, int nestingLevel, string name1, string name2)
		{
			block.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(name1), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(name1), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(name2), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(name2), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(VALUE), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(VALUE), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(THIS), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(THIS), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(BASE), _type, false), _value))
				.Append(codeModel.Assign(codeModel.Variable(codeModel.Name(BASE), _type, false), _value));

			if (nestingLevel < 2)
			{
				var lambda = codeModel.Lambda(_filterType, true);
				lambda.Parameter(codeModel.LambdaParameter(codeModel.Name(name1), _typeNotNull));
				lambda.Parameter(codeModel.LambdaParameter(codeModel.Name(name1), WellKnownTypes.System.Int32));
				CreateBody(codeModel, lambda.Body(), nestingLevel + 1, name1, name2)
					.Append(codeModel.Return(codeModel.Equal(_value, _value)));
				block.Append(
					codeModel.Assign(
						codeModel.Variable(codeModel.Name(BASE), codeModel.Type(typeof(IEnumerable<DataType>), false), true),
						codeModel.ExtCall(
							WellKnownTypes.System.Linq.Enumerable,
							codeModel.Name(nameof(Enumerable.Where)),
							Array.Empty<IType>(),
							new ICodeExpression[]
							{
								codeModel.Array(_typeNotNull, true, new ICodeExpression[] { _value, _value }, false),
								lambda.Method
							},
							_enumerableType)));
			}

			return block;
		}

		private static void CreateOverloadMethod(CodeBuilder codeModel, ClassBuilder builder, bool isStatic, ParameterDirection direction, string name)
		{
			var method = builder.Methods(false).New(codeModel.Name(OVERLOAD));

			// TODO: not supported in code model now
			//AddAttribute(codeModel, method.AddAttribute(_attributeType));

			if (isStatic)
			{
				method.Static();
			}

			var p = codeModel.Parameter(_type, codeModel.Name(name), direction);
			method.Parameter(p);

			if (direction == ParameterDirection.Out)
			{
				method.Body()
					.Append(codeModel.Assign(p.Reference, _value));
			}
			else
				method.Body();
		}

		private static void CreateMethod(CodeBuilder codeModel, ClassBuilder builder, bool isStatic, string name1, string name2)
		{
			var method = builder.Methods(false).New(codeModel.Name(name1));

			// TODO: not supported in code model now
			//AddAttribute(codeModel, method.AddAttribute(_attributeType));

			if (isStatic)
			{
				method.Static();
			}

			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name1), ParameterDirection.In));
			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name1), ParameterDirection.In));
			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name2), ParameterDirection.In));
			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name2), ParameterDirection.In));

			CreateBody(codeModel, method.Body(), 0, name1, name2);
		}

		private static void CreateCtor(CodeBuilder codeModel, ClassBuilder builder, string name1, string name2)
		{
			var method = builder.Constructors().New();
			// TODO: not supported in code model now
			//AddAttribute(codeModel, method.AddAttribute(_attributeType));

			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name1), ParameterDirection.In));
			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name1), ParameterDirection.In));
			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name2), ParameterDirection.In));
			method.Parameter(codeModel.Parameter(_type, codeModel.Name(name2), ParameterDirection.In));

			CreateBody(codeModel, method.Body(), 0, name1, name2);
		}
	}
}
