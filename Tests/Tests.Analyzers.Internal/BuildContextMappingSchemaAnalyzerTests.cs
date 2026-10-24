using System.Threading.Tasks;

using CodeGenerators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

namespace Tests.Analyzers.Internal
{
	[TestFixture]
	public class BuildContextMappingSchemaAnalyzerTests
	{
		// A miniature of the types the rule resolves. Two alias owners (ExpressionBuilder, ParametersContext) are the
		// minimum that tells a derived alias set from a hardcoded one; ExpressionBuilder also carries the
		// CurrentDescriptor shape that must not count as a carrier.
		const string Model = """
			#nullable enable
			namespace LinqToDB
			{
				public interface IDataContext { LinqToDB.Mapping.MappingSchema MappingSchema { get; set; } }

				public static partial class Sql
				{
					public class ExtensionAttribute
					{
						public static object[] GetExtensionAttributes(object expression, LinqToDB.Mapping.MappingSchema mapping) => new object[0];
					}
				}
			}

			namespace LinqToDB.Mapping
			{
				public class MappingSchema         { public bool IsScalarType(System.Type type) => false; }
				public class EntityDescriptor      { public MappingSchema MappingSchema { get; } = null!; }
				public class ColumnDescriptor      { public MappingSchema MappingSchema { get; } = null!; }
				public class AssociationDescriptor { public MappingSchema MappingSchema { get; } = null!; }
			}

			namespace LinqToDB.Internal.Expressions
			{
				sealed class ContextRefExpression { public LinqToDB.Internal.Linq.Builder.IBuildContext BuildContext { get; } = null!; }

				static class InternalExtensions
				{
					public static object SkipMethodChain(this object expression, LinqToDB.Mapping.MappingSchema mappingSchema) => expression;
				}
			}

			namespace LinqToDB.Internal.Linq.Builder
			{
				using LinqToDB.Mapping;

				sealed partial class ExpressionBuilder
				{
					public IDataContext DataContext = null!;

					public MappingSchema     MappingSchema     => DataContext.MappingSchema;
					public ColumnDescriptor? CurrentDescriptor => null;

					public static MappingSchema? TryGetRootMappingSchema(object expression) => null;
				}

				sealed partial class ParametersContext
				{
					public IDataContext DataContext = null!;

					public MappingSchema MappingSchema => DataContext.MappingSchema;
				}

				interface IBuildContext
				{
					ExpressionBuilder Builder       { get; }
					IBuildContext?    Parent        { get; }
					MappingSchema     MappingSchema { get; }
				}

				abstract class BuildContextBase : IBuildContext
				{
					protected BuildContextBase(ExpressionBuilder builder) { Builder = builder; }

					public          ExpressionBuilder Builder       { get; }
					public          IBuildContext?    Parent        { get; set; }
					public abstract MappingSchema     MappingSchema { get; }
				}

				abstract class SchemaContextBase : BuildContextBase
				{
					protected SchemaContextBase(ExpressionBuilder builder, MappingSchema mappingSchema) : base(builder) { }
				}

				abstract class MethodCallBuilder
				{
					protected abstract object BuildMethodCall(ExpressionBuilder builder, object methodCall);
				}
			}

			""";

		static string Code(string body) => Model + $$"""
			namespace LinqToDB.Internal.Linq.Builder
			{
				using LinqToDB.Mapping;
				using LinqToDB.Internal.Expressions;

			{{body}}
			}
			""";

		static Task Verify(string body) => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(Code(body));

		// TO-4: the CounterContext shape - a context holding its source returns the data context's schema.
		[Test]
		public Task GetterOfContextWithSourceReports() => Verify("""
				sealed class CounterContext : BuildContextBase
				{
					readonly IBuildContext _sequence;

					public CounterContext(ExpressionBuilder builder, IBuildContext sequence) : base(builder) { _sequence = sequence; }

					public override MappingSchema MappingSchema => {|LINQ2DB0007:Builder.MappingSchema|};
				}
			""");

		// TO-4: the same getter, statement-bodied, on a true root (only Parent) - silent.
		[Test]
		public Task GetterOfRootContextIsSilent() => Verify("""
				sealed class RootContext : BuildContextBase
				{
					public RootContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema
					{
						get { return Builder.MappingSchema; }
					}
				}
			""");

		// TO-4: statement-bodied getter with a source member reports too.
		[Test]
		public Task StatementBodiedGetterOfContextWithSourceReports() => Verify("""
				sealed class ValuesContext : BuildContextBase
				{
					public ValuesContext(ExpressionBuilder builder) : base(builder) { }

					public IBuildContext? Values { get; set; }

					public override MappingSchema MappingSchema
					{
						get { return {|LINQ2DB0007:Builder.MappingSchema|}; }
					}
				}
			""");

		// TO-5: the SelectContext shape - the ctor fills the getter's backing field from the live schema. The
		// context member is also a carrier in scope, so only the message tells the definition-site branch apart.
		[Test]
		public Task CtorAssigningBackingFieldWithSourceMemberReports() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				sealed class SelectContext : BuildContextBase
				{
					readonly MappingSchema _mappingSchema;

					public SelectContext(ExpressionBuilder builder) : base(builder)
					{
						_mappingSchema = {|#0:builder.MappingSchema|};
					}

					public IBuildContext? InnerContext { get; set; }

					public override MappingSchema MappingSchema => _mappingSchema;
				}
			"""),
			new DiagnosticResult("LINQ2DB0007", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'SelectContext' holds the source context 'InnerContext': return its MappingSchema instead of the data context's"));

		// TO-5 control: the same ctor on a root context is a capture point.
		[Test]
		public Task CtorAssigningBackingFieldOnRootIsSilent() => Verify("""
				sealed class RootSelectContext : BuildContextBase
				{
					readonly MappingSchema _mappingSchema;

					public RootSelectContext(ExpressionBuilder builder) : base(builder)
					{
						_mappingSchema = builder.MappingSchema;
					}

					public override MappingSchema MappingSchema => _mappingSchema;
				}
			""");

		// TO-6: the AssociationHelper shape - a schema parameter ignored, including from a lambda body.
		[Test]
		public Task SchemaParameterInScopeReports() => Verify("""
				static class Helper
				{
					public static bool M(ExpressionBuilder builder, MappingSchema mappingSchema)
					{
						System.Func<bool> f = () => {|LINQ2DB0007:builder.MappingSchema|}.IsScalarType(typeof(int));

						return {|LINQ2DB0007:builder.MappingSchema|}.IsScalarType(typeof(int)) && f();
					}
				}
			""");

		// TO-7: every descriptor carrier, as a parameter and as an earlier local.
		[Test]
		public Task DescriptorCarriersReport() => Verify("""
				static class Helper
				{
					public static object A(ExpressionBuilder builder, EntityDescriptor entityDescriptor)       => {|LINQ2DB0007:builder.MappingSchema|};
					public static object B(ExpressionBuilder builder, ColumnDescriptor? columnDescriptor)      => {|LINQ2DB0007:builder.MappingSchema|};
					public static object C(ExpressionBuilder builder, AssociationDescriptor associationDescriptor) => {|LINQ2DB0007:builder.MappingSchema|};

					public static object D(ExpressionBuilder builder, object o)
					{
						var descriptor = (ColumnDescriptor)o;

						return {|LINQ2DB0007:builder.MappingSchema|};
					}
				}
			""");

		// TO-8: reads through a context's Builder - FinalizeExpressionVisitor's field shape, the implicit form in a
		// context, and the DataContext spelling.
		[Test]
		public Task ReadsThroughContextBuilderReport() => Verify("""
				sealed class Visitor
				{
					IBuildContext _context = null!;

					public object M() => {|LINQ2DB0007:_context.Builder.MappingSchema|};
				}

				sealed class KeyContext : BuildContextBase
				{
					public KeyContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema => throw new System.NotSupportedException();

					public bool IsScalar(System.Type type) => {|LINQ2DB0007:Builder.MappingSchema|}.IsScalarType(type);

					public object Alternative() => {|LINQ2DB0007:Builder.DataContext.MappingSchema|};
				}
			""");

		// TO-8 / TO-17: the DataContext spelling on an ExpressionBuilder parameter, inside BuildMethodCall.
		[Test]
		public Task DataContextSpellingInBuildMethodCallReports() => Verify("""
				sealed class SomeBuilder : MethodCallBuilder
				{
					protected override object BuildMethodCall(ExpressionBuilder builder, object methodCall)
						=> {|LINQ2DB0007:builder.DataContext.MappingSchema|};
				}
			""");

		// TO-9: the second alias owner is recognised by its getter shape.
		[Test]
		public Task DerivedAliasReports() => Verify("""
				sealed partial class ParametersContext
				{
					public bool Prepare(MappingSchema mappingSchema, System.Type type) => {|LINQ2DB0007:MappingSchema|}.IsScalarType(type);
				}
			""");

		// TO-10: a context parameter and a ContextRefExpression pattern variable.
		[Test]
		public Task ContextCarriersReport() => Verify("""
				static class Helper
				{
					public static object A(ExpressionBuilder builder, IBuildContext context) => {|LINQ2DB0007:builder.MappingSchema|};

					public static object? B(ExpressionBuilder builder, object expression)
					{
						if (expression is ContextRefExpression contextRef)
							return {|LINQ2DB0007:builder.MappingSchema|};

						return null;
					}
				}
			""");

		// TO-11: an instance context field counts in a non-static member of a context type, not in a static one,
		// and the alias owner's own CurrentDescriptor never counts.
		[Test]
		public Task InstanceCarriersOnlyOnContextTypes() => Verify("""
				sealed class OwnerContext : BuildContextBase
				{
					readonly IBuildContext _inner;

					public OwnerContext(ExpressionBuilder builder, IBuildContext inner) : base(builder) { _inner = inner; }

					public override MappingSchema MappingSchema => _inner.MappingSchema;

					public object Instance(ExpressionBuilder builder) => {|LINQ2DB0007:builder.MappingSchema|};

					public static object Static(ExpressionBuilder builder) => builder.MappingSchema;
				}

				sealed partial class ExpressionBuilder
				{
					public bool IsScalar(System.Type type) => MappingSchema.IsScalarType(type);
				}
			""");

		// TO-12: a local declared before reports, one declared after does not; a self-initialiser is not a carrier.
		[Test]
		public Task LocalDeclarationPosition() => Verify("""
				static class Helper
				{
					public static object Before(ExpressionBuilder builder, object o)
					{
						var context = (IBuildContext)o;

						return {|LINQ2DB0007:builder.MappingSchema|};
					}

					public static object After(ExpressionBuilder builder, object o)
					{
						var schema  = builder.MappingSchema;
						var context = (IBuildContext)o;

						return schema;
					}

					public static object SelfInitialiser(ExpressionBuilder builder)
					{
						var mappingSchema = builder.MappingSchema;

						return mappingSchema;
					}
				}
			""");

		// TO-13: a context local in a sibling block that closed before the read is out of scope.
		[Test]
		public Task ClosedSiblingBlockIsSilent() => Verify("""
				static class Helper
				{
					public static object M(ExpressionBuilder builder, object o, bool flag)
					{
						if (flag)
						{
							var context = (IBuildContext)o;
							_ = context;
						}

						return builder.MappingSchema;
					}
				}
			""");

		// TO-14: a local function and a lambda capturing an outer context.
		[Test]
		public Task CapturedContextReports() => Verify("""
				static class Helper
				{
					public static object M(ExpressionBuilder builder, IBuildContext context)
					{
						object Local() => {|LINQ2DB0007:builder.MappingSchema|};

						System.Func<object> lambda = () => {|LINQ2DB0007:builder.MappingSchema|};

						return Local() ?? lambda();
					}
				}
			""");

		// TO-15: Parent and a parameter named parent are the outer context, not a source; nor is a root's own
		// MappingSchema, which is the live schema.
		[Test]
		public Task ParentIsNotASource() => Verify("""
				static class Helper
				{
					public static object M(ExpressionBuilder builder, IBuildContext? parent) => builder.MappingSchema;
				}

				sealed class ChildContext : BuildContextBase
				{
					public ChildContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema => Builder.MappingSchema;

					public object Read(ExpressionBuilder builder) => Parent != null ? builder.MappingSchema : builder.MappingSchema;
				}
			""");

		// A-1: a root's own MappingSchema, expression- or statement-bodied, is no source; any other context's is.
		[Test]
		public Task OwnSchemaIsASourceUnlessRoot() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				sealed class RootContext : BuildContextBase
				{
					public RootContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema
					{
						get { return Builder.MappingSchema; }
					}

					public object Read(ExpressionBuilder builder) => builder.MappingSchema;
				}

				sealed class SnapshotContext : BuildContextBase
				{
					public SnapshotContext(ExpressionBuilder builder, MappingSchema mappingSchema) : base(builder) { MappingSchema = mappingSchema; }

					public override MappingSchema MappingSchema { get; }

					public object Read(ExpressionBuilder builder) => {|#0:builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0007", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'MappingSchema' is in scope: read 'MappingSchema' instead of the data context's MappingSchema"));

		// A foreach variable is a carrier in its body, not in its own collection expression.
		[Test]
		public Task ForEachVariablePosition() => Verify("""
				static class Helper
				{
					public static object? Body(ExpressionBuilder builder, object o)
					{
						foreach (var column in (ColumnDescriptor[])o)
							return {|LINQ2DB0007:builder.MappingSchema|};

						return null;
					}

					public static object? Collection(ExpressionBuilder builder)
					{
						foreach (var column in Columns(builder.MappingSchema))
							return column;

						return null;
					}

					static ColumnDescriptor[] Columns(MappingSchema mappingSchema) => new ColumnDescriptor[0];
				}
			""");

		// TO-16: instance members are not usable in a constructor initializer.
		[Test]
		public Task ConstructorInitializerIsSilent() => Verify("""
				sealed class RawContext : SchemaContextBase
				{
					readonly IBuildContext? _inner;

					public RawContext(ExpressionBuilder builder) : base(builder, builder.MappingSchema) { }

					public override MappingSchema MappingSchema => throw new System.NotSupportedException();
				}
			""");

		// TO-17: BuildMethodCall reports without a carrier, except as an argument to a live lookup.
		[Test]
		public Task BuildMethodCallReadsReportExceptLiveLookups() => Verify("""
				sealed class SomeBuilder : MethodCallBuilder
				{
					protected override object BuildMethodCall(ExpressionBuilder builder, object methodCall)
					{
						var attributes = LinqToDB.Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);
						var root       = methodCall.SkipMethodChain(builder.MappingSchema);

						return {|LINQ2DB0007:builder.MappingSchema|};
					}
				}
			""");

		// TO-18: the fallback is exempt only when its left operand is a source's schema, and only as the whole
		// right operand.
		[Test]
		public Task DocumentedFallbackShapes() => Verify("""
				static class Helper
				{
					public static MappingSchema A(ExpressionBuilder builder, IBuildContext? context) => context?.MappingSchema ?? builder.MappingSchema;

					public static MappingSchema B(ExpressionBuilder builder, IBuildContext? context) => context?.MappingSchema ?? Wrap({|LINQ2DB0007:builder.MappingSchema|});

					static MappingSchema Wrap(MappingSchema mappingSchema) => mappingSchema;
				}

				sealed class RootBuilder : MethodCallBuilder
				{
					protected override object BuildMethodCall(ExpressionBuilder builder, object methodCall)
						=> ExpressionBuilder.TryGetRootMappingSchema(methodCall) ?? builder.MappingSchema;
				}

				sealed class InnerContext : BuildContextBase
				{
					readonly IBuildContext _source;

					public InnerContext(ExpressionBuilder builder, IBuildContext source) : base(builder) { _source = source; }

					public override MappingSchema MappingSchema => _source.MappingSchema;

					public MappingSchema FromParent(ExpressionBuilder builder) => Parent?.MappingSchema ?? {|LINQ2DB0007:builder.MappingSchema|};
				}
			""");

		// TO-19: the rule is scoped to the builder namespace.
		[Test]
		public Task OutsideBuilderNamespaceIsSilent() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(Model + """
			namespace Elsewhere
			{
				using LinqToDB.Internal.Linq.Builder;

				static class Helper
				{
					public static object M(ExpressionBuilder builder, IBuildContext context) => builder.MappingSchema;
				}
			}
			""");

		// TO-19: without ExpressionBuilder in the compilation assembly the rule does nothing.
		[Test]
		public Task NoExpressionBuilderIsSilent() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync("""
			namespace LinqToDB.Internal.Linq.Builder
			{
				interface IBuildContext { }

				static class Helper
				{
					public static object M(IBuildContext context) => context;
				}
			}
			""");

		// TO-20: a write to the live schema is not a read.
		[Test]
		public Task WriteIsSilent() => Verify("""
				static class Helper
				{
					public static void M(ExpressionBuilder builder, IBuildContext context, MappingSchema mappingSchema)
					{
						builder.DataContext.MappingSchema = mappingSchema;
					}
				}
			""");

		// TO-21: a companion type that has gone missing is reported once, on ExpressionBuilder.
		[Test]
		public Task DriftIsReported() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync("""
			namespace LinqToDB.Internal.Linq.Builder
			{
				sealed class {|LINQ2DB0007:ExpressionBuilder|}
				{
				}
			}
			""");

		// TO-21: ExpressionBuilder.MappingSchema no longer alias-shaped disables the rule, reported on ExpressionBuilder.
		[Test]
		public Task AliasDriftIsReported() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Model
				.Replace("sealed partial class ExpressionBuilder", "sealed partial class {|#0:ExpressionBuilder|}")
				.Replace("MappingSchema     => DataContext.MappingSchema;", "MappingSchema     { get; } = null!;"),
			new DiagnosticResult("LINQ2DB0007", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("The LINQ2DB0007 model could not be read: 'LinqToDB.Internal.Linq.Builder.ExpressionBuilder.MappingSchema' is no longer an expression-bodied '=> <IDataContext member>.MappingSchema' getter. The rule is disabled for the whole compilation until the analyzer is updated."));
	}
}
