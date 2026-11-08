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

				class SqlQueryRootExpression { public LinqToDB.Mapping.MappingSchema MappingSchema { get; } = null!; }

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

		// The CounterContext shape - a context holding its source returns the data context's schema.
		[Test]
		public Task GetterOfContextWithSourceReports() => Verify("""
				sealed class CounterContext : BuildContextBase
				{
					readonly IBuildContext _sequence;

					public CounterContext(ExpressionBuilder builder, IBuildContext sequence) : base(builder) { _sequence = sequence; }

					public override MappingSchema MappingSchema => {|LINQ2DB0010:Builder.MappingSchema|};
				}
			""");

		// The same getter, statement-bodied, on a true root (only Parent) - silent.
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

		// Statement-bodied getter with a source member reports too.
		[Test]
		public Task StatementBodiedGetterOfContextWithSourceReports() => Verify("""
				sealed class ValuesContext : BuildContextBase
				{
					public ValuesContext(ExpressionBuilder builder) : base(builder) { }

					public IBuildContext? Values { get; set; }

					public override MappingSchema MappingSchema
					{
						get { return {|LINQ2DB0010:Builder.MappingSchema|}; }
					}
				}
			""");

		// The SelectContext shape - the ctor fills the getter's backing field from the live schema. The
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
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'SelectContext' holds the source 'InnerContext': return its MappingSchema instead of the data context's"));

		// The same ctor on a root context is a capture point.
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

		// A root that captures the live schema into its own auto-property is a capture point too.
		[Test]
		public Task CtorAssigningOwnAutoPropertyOnRootIsSilent() => Verify("""
				sealed class RootSelectContext : BuildContextBase
				{
					public RootSelectContext(ExpressionBuilder builder) : base(builder)
					{
						MappingSchema = builder.MappingSchema;
					}

					public override MappingSchema MappingSchema { get; }
				}
			""");

		// The same auto-property capture while the context holds a source is the definition-site report.
		[Test]
		public Task CtorAssigningOwnAutoPropertyWithSourceMemberReports() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				sealed class SelectContext : BuildContextBase
				{
					public SelectContext(ExpressionBuilder builder) : base(builder)
					{
						MappingSchema = {|#0:builder.MappingSchema|};
					}

					public IBuildContext? InnerContext { get; set; }

					public override MappingSchema MappingSchema { get; }
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'SelectContext' holds the source 'InnerContext': return its MappingSchema instead of the data context's"));

		// The GroupJoinInnerContext shape - a ctor handed the source's schema that captures the live one instead.
		[Test]
		public Task CtorCapturingLiveSchemaWithSourceParameterReports() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				sealed class InnerContext : BuildContextBase
				{
					readonly MappingSchema _mappingSchema;

					public InnerContext(ExpressionBuilder builder, MappingSchema mappingSchema) : base(builder)
					{
						_mappingSchema = {|#0:builder.MappingSchema|};
					}

					public override MappingSchema MappingSchema => _mappingSchema;
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'mappingSchema' is in scope: read 'mappingSchema' instead of the data context's MappingSchema"));

		// The variable a live read is assigned to is not a source for that read.
		[Test]
		public Task AssignmentTargetIsNoCarrier() => Verify("""
				static class Helper
				{
					public static void Out(ExpressionBuilder builder, out MappingSchema mappingSchema)
					{
						mappingSchema = builder.MappingSchema;
					}

					public static object Split(ExpressionBuilder builder)
					{
						MappingSchema mappingSchema;

						mappingSchema = builder.MappingSchema;

						return mappingSchema;
					}
				}

				sealed class FieldCachingContext : BuildContextBase
				{
					MappingSchema? _schema;

					public FieldCachingContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema => Builder.MappingSchema;

					public void Capture(ExpressionBuilder builder) { _schema = builder.MappingSchema; }
				}

				sealed class PropertyCachingContext : BuildContextBase
				{
					public PropertyCachingContext(ExpressionBuilder builder) : base(builder) { }

					MappingSchema? Cached { get; set; }

					public override MappingSchema MappingSchema => Builder.MappingSchema;

					public void Capture(ExpressionBuilder builder) { Cached = builder.MappingSchema; }
				}
			""");

		// A variable not yet assigned where the read sits is no source, however deep the read is in its assignment.
		[Test]
		public Task UnassignedVariableIsNoCarrier() => Verify("""
				static class Helper
				{
					public static bool Out(ExpressionBuilder builder, System.Type type, out EntityDescriptor? descriptor)
					{
						descriptor = Describe(builder.MappingSchema, type);
						return descriptor != null;
					}

					public static object Split(ExpressionBuilder builder)
					{
						IBuildContext context;

						context = Make(builder.MappingSchema);

						return context;
					}

					static EntityDescriptor? Describe(MappingSchema mappingSchema, System.Type type) => null;

					static IBuildContext Make(MappingSchema mappingSchema) => null!;
				}
			""");

		// The AssociationHelper shape - a schema parameter ignored, including from a lambda body.
		[Test]
		public Task SchemaParameterInScopeReports() => Verify("""
				static class Helper
				{
					public static bool M(ExpressionBuilder builder, MappingSchema mappingSchema)
					{
						System.Func<bool> f = () => {|LINQ2DB0010:builder.MappingSchema|}.IsScalarType(typeof(int));

						return {|LINQ2DB0010:builder.MappingSchema|}.IsScalarType(typeof(int)) && f();
					}
				}
			""");

		// Every descriptor carrier, as a parameter and as an earlier local.
		[Test]
		public Task DescriptorCarriersReport() => Verify("""
				static class Helper
				{
					public static object A(ExpressionBuilder builder, EntityDescriptor entityDescriptor)       => {|LINQ2DB0010:builder.MappingSchema|};
					public static object B(ExpressionBuilder builder, ColumnDescriptor? columnDescriptor)      => {|LINQ2DB0010:builder.MappingSchema|};
					public static object C(ExpressionBuilder builder, AssociationDescriptor associationDescriptor) => {|LINQ2DB0010:builder.MappingSchema|};

					public static object D(ExpressionBuilder builder, object o)
					{
						var descriptor = (ColumnDescriptor)o;

						return {|LINQ2DB0010:builder.MappingSchema|};
					}
				}
			""");

		// Reads through a context's Builder - FinalizeExpressionVisitor's field shape, the implicit form in a
		// context, and the DataContext spelling.
		[Test]
		public Task ReadsThroughContextBuilderReport() => Verify("""
				sealed class Visitor
				{
					IBuildContext _context = null!;

					public object M() => {|LINQ2DB0010:_context.Builder.MappingSchema|};
				}

				sealed class KeyContext : BuildContextBase
				{
					public KeyContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema => throw new System.NotSupportedException();

					public bool IsScalar(System.Type type) => {|LINQ2DB0010:Builder.MappingSchema|}.IsScalarType(type);

					public object Alternative() => {|LINQ2DB0010:Builder.DataContext.MappingSchema|};
				}
			""");

		// Parent's Builder is the outer context's, not a source to read instead.
		[Test]
		public Task ReadThroughParentBuilderIsSilent() => Verify("""
				sealed class LeafContext : BuildContextBase
				{
					public LeafContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema => Builder.MappingSchema;

					public object Read() => Parent!.Builder.MappingSchema;
				}
			""");

		// A context holding its source as a ContextRefExpression holds a source too.
		[Test]
		public Task GetterOfContextWithSourceRefReports() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				sealed class RefContext : BuildContextBase
				{
					readonly ContextRefExpression _sourceRef;

					public RefContext(ExpressionBuilder builder, ContextRefExpression sourceRef) : base(builder) { _sourceRef = sourceRef; }

					public override MappingSchema MappingSchema => {|#0:Builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'RefContext' holds the source '_sourceRef': return its MappingSchema instead of the data context's"));

		// The DataContext spelling on an ExpressionBuilder parameter, inside BuildMethodCall.
		[Test]
		public Task DataContextSpellingInBuildMethodCallReports() => Verify("""
				sealed class SomeBuilder : MethodCallBuilder
				{
					protected override object BuildMethodCall(ExpressionBuilder builder, object methodCall)
						=> {|LINQ2DB0010:builder.DataContext.MappingSchema|};
				}
			""");

		// The second alias owner is recognised by its getter shape.
		[Test]
		public Task DerivedAliasReports() => Verify("""
				sealed partial class ParametersContext
				{
					public bool Prepare(MappingSchema mappingSchema, System.Type type) => {|LINQ2DB0010:MappingSchema|}.IsScalarType(type);
				}
			""");

		// An alias getter is recognised in its accessor forms as well.
		[Test]
		public Task AccessorFormAliasReports() => Verify("""
				sealed class AccessorAlias
				{
					public IDataContext DataContext = null!;

					public MappingSchema MappingSchema { get => DataContext.MappingSchema; }

					public bool Prepare(MappingSchema mappingSchema, System.Type type) => {|LINQ2DB0010:MappingSchema|}.IsScalarType(type);
				}

				sealed class BodyAlias
				{
					public IDataContext DataContext = null!;

					public MappingSchema MappingSchema { get { return DataContext.MappingSchema; } }

					public bool Prepare(MappingSchema mappingSchema, System.Type type) => {|LINQ2DB0010:MappingSchema|}.IsScalarType(type);
				}
			""");

		// IDataContext.MappingSchema is live only when reached through an ExpressionBuilder.
		[Test]
		public Task DataContextReadWithoutBuilderIsSilent() => Verify("""
				static class Helper
				{
					public static object M(IDataContext dataContext, IBuildContext context) => dataContext.MappingSchema;
				}
			""");

		// A context parameter and a ContextRefExpression pattern variable.
		[Test]
		public Task ContextCarriersReport() => Verify("""
				static class Helper
				{
					public static object A(ExpressionBuilder builder, IBuildContext context) => {|LINQ2DB0010:builder.MappingSchema|};

					public static object? B(ExpressionBuilder builder, object expression)
					{
						if (expression is ContextRefExpression contextRef)
							return {|LINQ2DB0010:builder.MappingSchema|};

						return null;
					}
				}
			""");

		// A pattern variable is a source only where it is definitely assigned.
		[Test]
		public Task UnassignedPatternVariableIsNoCarrier() => Verify("""
				static class Helper
				{
					public static MappingSchema ElseBranch(ExpressionBuilder builder, object expression)
					{
						if (expression is ContextRefExpression contextRef)
							return contextRef.BuildContext.MappingSchema;
						else
							return builder.MappingSchema;
					}

					public static MappingSchema Guard(ExpressionBuilder builder, object expression)
					{
						if (expression is not ContextRefExpression contextRef)
							return builder.MappingSchema;

						return contextRef.BuildContext.MappingSchema;
					}
				}
			""");

		// A nullable carrier is suggested in the fallback form, which compiles where the carrier may be null.
		[Test]
		public Task NullableCarrierSuggestsFallback() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static MappingSchema M(ExpressionBuilder builder, IBuildContext? context)
						=> context != null ? context.MappingSchema : {|#0:builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'context' is in scope: read 'context?.MappingSchema ?? builder.MappingSchema' instead of the data context's MappingSchema"));

		// A var local is nullable by declaration; its initializer decides which form is suggested.
		[Test]
		public Task VarCarrierSuggestionFollowsInitializer() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static object Cast(ExpressionBuilder builder, object o)
					{
						var descriptor = (EntityDescriptor)o;
						return {|#0:builder.MappingSchema|};
					}

					public static object? Loop(ExpressionBuilder builder, EntityDescriptor[] descriptors)
					{
						foreach (var descriptor in descriptors)
							return {|#1:builder.MappingSchema|};

						return null;
					}

					public static object As(ExpressionBuilder builder, object o)
					{
						var descriptor = o as EntityDescriptor;
						return {|#2:builder.MappingSchema|};
					}
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'descriptor' is in scope: read 'descriptor.MappingSchema' instead of the data context's MappingSchema"),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(1)
				.WithArguments("'descriptor' is in scope: read 'descriptor.MappingSchema' instead of the data context's MappingSchema"),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(2)
				.WithArguments("'descriptor' is in scope: read 'descriptor?.MappingSchema ?? builder.MappingSchema' instead of the data context's MappingSchema"));

		// The fallback form over a nullable schema property, which the carrier search suggests for one, is silent.
		[Test]
		public Task SchemaPropertyFallbackIsSilent() => Verify("""
				sealed class SchemaPropertyContext : BuildContextBase
				{
					public SchemaPropertyContext(ExpressionBuilder builder) : base(builder) { }

					public MappingSchema? Schema { get; set; }

					public override MappingSchema MappingSchema => Builder.MappingSchema;

					public MappingSchema M(ExpressionBuilder builder) => Schema ?? builder.MappingSchema;
				}
			""");

		// An instance context field counts in a non-static member of a context type, not in a static one,
		// and the alias owner's own CurrentDescriptor never counts.
		[Test]
		public Task InstanceCarriersOnlyOnContextTypes() => Verify("""
				sealed class OwnerContext : BuildContextBase
				{
					readonly IBuildContext _inner;

					public OwnerContext(ExpressionBuilder builder, IBuildContext inner) : base(builder) { _inner = inner; }

					public override MappingSchema MappingSchema => _inner.MappingSchema;

					public object Instance(ExpressionBuilder builder) => {|LINQ2DB0010:builder.MappingSchema|};

					public static object Static(ExpressionBuilder builder) => builder.MappingSchema;
				}

				sealed partial class ExpressionBuilder
				{
					public bool IsScalar(System.Type type) => MappingSchema.IsScalarType(type);
				}
			""");

		// Instance members are not usable in a static lambda or local function inside a context's instance member.
		[Test]
		public Task StaticFunctionInContextIsSilent() => Verify("""
				sealed class LambdaContext : BuildContextBase
				{
					readonly IBuildContext _inner;

					public LambdaContext(ExpressionBuilder builder, IBuildContext inner) : base(builder) { _inner = inner; }

					public override MappingSchema MappingSchema => _inner.MappingSchema;

					public object M()
					{
						System.Func<ExpressionBuilder, object> lambda = static builder => builder.MappingSchema;

						static object Local(ExpressionBuilder builder) => builder.MappingSchema;

						return lambda(Builder) ?? Local(Builder);
					}
				}
			""");

		// Nor are the outer member's parameters and locals: a static function cannot capture them.
		[Test]
		public Task StaticFunctionIgnoresOuterVariables() => Verify("""
				static class Helper
				{
					public static object M(ExpressionBuilder builder, IBuildContext context)
					{
						var descriptor = (EntityDescriptor)(object)context;

						System.Func<ExpressionBuilder, object> lambda = static b => b.MappingSchema;

						static object Local(ExpressionBuilder b) => b.MappingSchema;

						return lambda(builder) ?? Local(builder);
					}
				}
			""");

		// A local declared before reports, one declared after does not; a self-initialiser is not a carrier.
		[Test]
		public Task LocalDeclarationPosition() => Verify("""
				static class Helper
				{
					public static object Before(ExpressionBuilder builder, object o)
					{
						var context = (IBuildContext)o;

						return {|LINQ2DB0010:builder.MappingSchema|};
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

		// A context local in a sibling block that closed before the read is out of scope.
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

		// A local function and a lambda capturing an outer context.
		[Test]
		public Task CapturedContextReports() => Verify("""
				static class Helper
				{
					public static object M(ExpressionBuilder builder, IBuildContext context)
					{
						object Local() => {|LINQ2DB0010:builder.MappingSchema|};

						System.Func<object> lambda = () => {|LINQ2DB0010:builder.MappingSchema|};

						return Local() ?? lambda();
					}
				}
			""");

		// Parent and a parameter named parent are the outer context, not a source; nor is a root's own
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

		// A root's own MappingSchema, expression- or statement-bodied, is no source; any other context's is.
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
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
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
							return {|LINQ2DB0010:builder.MappingSchema|};

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

		// Instance members are not usable in a constructor initializer.
		[Test]
		public Task ConstructorInitializerIsSilent() => Verify("""
				sealed class RawContext : SchemaContextBase
				{
					readonly IBuildContext? _inner;

					public RawContext(ExpressionBuilder builder) : base(builder, builder.MappingSchema) { }

					public override MappingSchema MappingSchema => throw new System.NotSupportedException();
				}
			""");

		// BuildMethodCall reports without a carrier, except as an argument to a live lookup.
		[Test]
		public Task BuildMethodCallReadsReportExceptLiveLookups() => Verify("""
				sealed class SomeBuilder : MethodCallBuilder
				{
					protected override object BuildMethodCall(ExpressionBuilder builder, object methodCall)
					{
						var attributes = LinqToDB.Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);
						var root       = methodCall.SkipMethodChain(builder.MappingSchema);

						return {|LINQ2DB0010:builder.MappingSchema|};
					}
				}
			""");

		// The fallback is exempt only when its left operand is a source's schema, and only as the whole
		// right operand.
		[Test]
		public Task DocumentedFallbackShapes() => Verify("""
				static class Helper
				{
					public static MappingSchema A(ExpressionBuilder builder, IBuildContext? context) => context?.MappingSchema ?? builder.MappingSchema;

					public static MappingSchema B(ExpressionBuilder builder, IBuildContext? context) => context?.MappingSchema ?? Wrap({|LINQ2DB0010:builder.MappingSchema|});

					public static MappingSchema C(ExpressionBuilder builder, IBuildContext context) => context.MappingSchema ?? builder.MappingSchema;

					public static MappingSchema D(ExpressionBuilder builder, MappingSchema? mappingSchema) => mappingSchema ?? builder.MappingSchema;

					public static MappingSchema E(ExpressionBuilder builder, IBuildContext? parent, IBuildContext context) => parent?.MappingSchema ?? {|LINQ2DB0010:builder.MappingSchema|};

					public static MappingSchema F(ExpressionBuilder builder, IBuildContext? context) => context?.Parent!.MappingSchema ?? {|LINQ2DB0010:builder.MappingSchema|};

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

					public MappingSchema FromParent(ExpressionBuilder builder) => Parent?.MappingSchema ?? {|LINQ2DB0010:builder.MappingSchema|};
				}
			""");

		// The rule is scoped to the builder namespace.
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

		// Without ExpressionBuilder in the compilation assembly the rule does nothing.
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

		// A write to the live schema is not a read.
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

		// A query root carries the snapshot a handle was created under.
		[Test]
		public Task QueryRootIsACarrier() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static MappingSchema M(ExpressionBuilder builder, SqlQueryRootExpression root) => {|#0:builder.MappingSchema|};
				}

				sealed class RootHoldingContext : BuildContextBase
				{
					readonly SqlQueryRootExpression _root;

					public RootHoldingContext(ExpressionBuilder builder, SqlQueryRootExpression root) : base(builder) { _root = root; }

					public override MappingSchema MappingSchema => {|#1:Builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'root' is in scope: read 'root.MappingSchema' instead of the data context's MappingSchema"),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(1)
				.WithArguments("'RootHoldingContext' holds the source '_root': return its MappingSchema instead of the data context's"));

		// A type parameter constrained to a context is a context - as a parameter and as a context's source member.
		[Test]
		public Task TypeParameterConstrainedToContextIsACarrier() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static MappingSchema M<T>(ExpressionBuilder builder, T context) where T : IBuildContext => {|#0:builder.MappingSchema|};
				}

				sealed class ProxyContext<TOwner> : BuildContextBase
					where TOwner : IBuildContext
				{
					readonly TOwner _owner;

					public ProxyContext(ExpressionBuilder builder, TOwner owner) : base(builder) { _owner = owner; }

					public override MappingSchema MappingSchema => {|#1:Builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'context' is in scope: read 'context.MappingSchema' instead of the data context's MappingSchema"),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(1)
				.WithArguments("'ProxyContext' holds the source '_owner': return its MappingSchema instead of the data context's"));

		// In a root context Builder.MappingSchema is its own MappingSchema, so reading it there changes nothing - in any
		// getter spelling the root is recognised by.
		[Test]
		public Task ReadThroughRootBuilderIsSilent() => Verify("""
				sealed class RootContext : BuildContextBase
				{
					public RootContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema => Builder.MappingSchema;

					public bool IsScalar(System.Type type) => Builder.MappingSchema.IsScalarType(type);
				}

				sealed class ThisRootContext : BuildContextBase
				{
					public ThisRootContext(ExpressionBuilder builder) : base(builder) { }

					public override MappingSchema MappingSchema => this.Builder.MappingSchema;

					public bool IsScalar(ExpressionBuilder builder, System.Type type) => builder.MappingSchema.IsScalarType(type);
				}
			""");

		// An alias is recognised by the type its getter reads through, however the receiver is spelled.
		[Test]
		public Task AliasSpellingsAreLiveReads() => Verify("""
				sealed class ThisAlias
				{
					public IDataContext DataContext = null!;

					public MappingSchema MappingSchema => this.DataContext.MappingSchema;
				}

				sealed class BangAlias
				{
					IDataContext? _dataContext;

					public MappingSchema MappingSchema => (_dataContext!).MappingSchema;
				}

				sealed class TwoHopAlias
				{
					public ExpressionBuilder Builder = null!;

					public MappingSchema MappingSchema => Builder.DataContext.MappingSchema;
				}

				static class Helper
				{
					public static object A(ThisAlias   owner, IBuildContext context) => {|LINQ2DB0010:owner.MappingSchema|};
					public static object B(BangAlias   owner, IBuildContext context) => {|LINQ2DB0010:owner.MappingSchema|};
					public static object C(TwoHopAlias owner, IBuildContext context) => {|LINQ2DB0010:owner.MappingSchema|};
				}
			""");

		// A local copy of the builder's data context reads the live schema too, unless it is reassigned.
		[Test]
		public Task LocalCopyOfDataContextIsALiveRead() => Verify("""
				static class Helper
				{
					public static object Copy(ExpressionBuilder builder, IBuildContext sequence)
					{
						var dc = builder.DataContext;
						return {|LINQ2DB0010:dc.MappingSchema|};
					}

					public static object Reassigned(ExpressionBuilder builder, IBuildContext sequence, IDataContext other)
					{
						var dc = builder.DataContext;
						dc = other;
						return dc.MappingSchema;
					}
				}
			""");

		// A constructor fallback still defines the context's schema, through any number of links: its own schema members
		// are no carriers there.
		[Test]
		public Task ConstructorFallbackDefinesTheSchema() => Verify("""
				sealed class FieldFallbackContext : BuildContextBase
				{
					readonly MappingSchema _mappingSchema;

					public FieldFallbackContext(ExpressionBuilder builder, MappingSchema? hint) : base(builder) { _mappingSchema = hint ?? builder.MappingSchema; }

					public override MappingSchema MappingSchema => _mappingSchema;
				}

				sealed class TwoLinkFieldFallbackContext : BuildContextBase
				{
					readonly MappingSchema _mappingSchema;

					public TwoLinkFieldFallbackContext(ExpressionBuilder builder, MappingSchema? hint, MappingSchema? other) : base(builder) { _mappingSchema = hint ?? other ?? builder.MappingSchema; }

					public override MappingSchema MappingSchema => _mappingSchema;
				}

				sealed class PropertyFallbackContext : BuildContextBase
				{
					public PropertyFallbackContext(ExpressionBuilder builder, MappingSchema? hint) : base(builder) { MappingSchema = hint ?? builder.MappingSchema; }

					public override MappingSchema MappingSchema { get; }
				}

				sealed class TwoLinkPropertyFallbackContext : BuildContextBase
				{
					public TwoLinkPropertyFallbackContext(ExpressionBuilder builder, MappingSchema? hint, MappingSchema? other) : base(builder) { MappingSchema = hint ?? other ?? builder.MappingSchema; }

					public override MappingSchema MappingSchema { get; }
				}
			""");

		// In a definition site's fallback the source the context holds is still the one to read; its own schema members are not.
		[Test]
		public Task DefinitionSiteFallbackStillSeesSourceMembers() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				sealed class HintWithSourceContext : BuildContextBase
				{
					readonly IBuildContext  _sequence;
					readonly MappingSchema? _hint;

					public HintWithSourceContext(ExpressionBuilder builder, IBuildContext sequence, MappingSchema? hint) : base(builder) { _sequence = sequence; _hint = hint; }

					public override MappingSchema MappingSchema => _hint ?? {|#0:Builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'_sequence' is in scope: read '_sequence.MappingSchema' instead of the data context's MappingSchema"));

		// The suggestion must not be reported in its turn: a definite source wins over a nullable schema.
		[Test]
		public Task DefiniteCarrierOutranksNullableSchema() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static MappingSchema M(ExpressionBuilder builder, MappingSchema? hint, IBuildContext context) => {|#0:builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'context' is in scope: read 'context.MappingSchema' instead of the data context's MappingSchema"));

		// Every way of writing to the local ends the copy: a tuple target, a ref argument and a ref alias.
		[Test]
		public Task WrittenLocalCopyIsNoLiveRead() => Verify("""
				static class Helper
				{
					static void Touch(ref IDataContext dc) { }

					public static object Tuple(ExpressionBuilder builder, IBuildContext sequence, IDataContext other)
					{
						var dc = builder.DataContext;
						(dc, _) = (other, 1);
						return dc.MappingSchema;
					}

					public static object RefArgument(ExpressionBuilder builder, IBuildContext sequence)
					{
						var dc = builder.DataContext;
						Touch(ref dc);
						return dc.MappingSchema;
					}

					public static object RefAlias(ExpressionBuilder builder, IBuildContext sequence, IDataContext other)
					{
						var dc = builder.DataContext;
						ref var alias = ref dc;
						alias = other;
						return dc.MappingSchema;
					}
				}
			""");

		// An alias over a primary-constructor parameter reads the live schema like one over a field.
		[Test]
		public Task PrimaryConstructorAliasIsALiveRead() => Verify("""
				sealed class ParameterAlias(IDataContext dataContext)
				{
					public MappingSchema MappingSchema => dataContext.MappingSchema;
				}

				static class Helper
				{
					public static object M(ParameterAlias owner, IBuildContext context) => {|LINQ2DB0010:owner.MappingSchema|};
				}
			""");

		// A type parameter constrained to a query root counts by its constraint too: that goes through base types, not interfaces.
		[Test]
		public Task TypeParameterConstrainedToQueryRootIsACarrier() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static MappingSchema M<T>(ExpressionBuilder builder, T root) where T : SqlQueryRootExpression => {|#0:builder.MappingSchema|};
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'root' is in scope: read 'root.MappingSchema' instead of the data context's MappingSchema"));
		// A tuple is a write only as a deconstruction target; as a value it leaves the copy alone.
		[Test]
		public Task TupleValueIsNoWrite() => Verify("""
				static class Helper
				{
					public static object Value(ExpressionBuilder builder, IBuildContext sequence)
					{
						var dc   = builder.DataContext;
						var pair = (dc, 1);
						return {|LINQ2DB0010:dc.MappingSchema|};
					}

					public static object NestedTarget(ExpressionBuilder builder, IBuildContext sequence, IDataContext other)
					{
						var dc = builder.DataContext;
						((dc, _), _) = ((other, 1), 1);
						return dc.MappingSchema;
					}
				}
			""");

		// An explicitly typed nullable local follows its null check, like a parameter.
		[Test]
		public Task ExplicitNullableLocalFollowsNullChecks() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					static MappingSchema? Provide() => null;

					public static MappingSchema M(ExpressionBuilder builder)
					{
						MappingSchema? local = Provide();

						if (local != null)
							return {|#0:builder.MappingSchema|};

						return null!;
					}
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'local' is in scope: read 'local' instead of the data context's MappingSchema"));

		// At a definition site a schema-typed member is no carrier, field or property alike.
		[Test]
		public Task DefinitionSiteFallbackIgnoresSchemaProperties() => Verify("""
				sealed class SchemaPropertyContext : BuildContextBase
				{
					readonly MappingSchema? _hint;

					public SchemaPropertyContext(ExpressionBuilder builder, MappingSchema? hint) : base(builder) { _hint = hint; }

					public MappingSchema Other { get; } = null!;

					public override MappingSchema MappingSchema => _hint ?? Builder.MappingSchema;
				}
			""");

		// A schema-typed left operand says nothing about the other sources in scope: one that cannot be null is still
		// the source to read, and TryGetRootMappingSchema is ExpressionBuilder's, not any method of that name.
		[Test]
		public Task SchemaTypedFallbackStillSeesCarriers() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static MappingSchema Hint(ExpressionBuilder builder, MappingSchema? hint, IBuildContext context) => hint ?? {|#0:builder.MappingSchema|};

					public static MappingSchema Root(ExpressionBuilder builder, object expression, IBuildContext context)
						=> ExpressionBuilder.TryGetRootMappingSchema(expression) ?? {|#1:builder.MappingSchema|};

					public static MappingSchema Local(ExpressionBuilder builder, IBuildContext? context)
					{
						static MappingSchema? TryGetRootMappingSchema(int value) => null;

						return TryGetRootMappingSchema(1) ?? {|#2:builder.MappingSchema|};
					}

					public static MappingSchema NullableOnly(ExpressionBuilder builder, MappingSchema? hint, IBuildContext? context) => hint ?? builder.MappingSchema;

					public static MappingSchema OwnOperand(ExpressionBuilder builder, MappingSchema hint) => hint ?? builder.MappingSchema;
				}

				// The CteContext shape: in its own getter the context's members are no carriers.
				sealed class CteLikeContext : BuildContextBase
				{
					public CteLikeContext(ExpressionBuilder builder) : base(builder) { }

					public IBuildContext? Inner      { get; set; }
					public object         Expression { get; set; } = null!;

					public override MappingSchema MappingSchema => Inner?.MappingSchema ?? ExpressionBuilder.TryGetRootMappingSchema(Expression) ?? Builder.MappingSchema;
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'context' is in scope: read 'context.MappingSchema' instead of the data context's MappingSchema"),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(1)
				.WithArguments("'context' is in scope: read 'context.MappingSchema' instead of the data context's MappingSchema"),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(2)
				.WithArguments("'context' is in scope: read 'context?.MappingSchema ?? builder.MappingSchema' instead of the data context's MappingSchema"));

		// The suggestion prefers a parameter over a local taken from Parent, and a null-checked parameter is not nullable.
		[Test]
		public Task SuggestionPrefersParametersAndFollowsNullChecks() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Code("""
				static class Helper
				{
					public static object ParentLocal(ExpressionBuilder builder, IBuildContext sequence)
					{
						var outer = sequence.Parent;
						return {|#0:builder.MappingSchema|};
					}

					public static object Checked(ExpressionBuilder builder, IBuildContext? context)
					{
						if (context == null)
							throw new System.ArgumentNullException(nameof(context));

						return {|#1:builder.MappingSchema|};
					}
				}
			"""),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("'sequence' is in scope: read 'sequence.MappingSchema' instead of the data context's MappingSchema"),
			new DiagnosticResult("LINQ2DB0010", DiagnosticSeverity.Warning)
				.WithLocation(1)
				.WithArguments("'context' is in scope: read 'context.MappingSchema' instead of the data context's MappingSchema"));

		[Test]
		public Task NameOfIsNoRead() => Verify("""
				static class Helper
				{
					public static string N(ExpressionBuilder builder, IBuildContext context) => nameof(builder.MappingSchema);
				}
			""");

		// A companion type that has gone missing is reported once, on ExpressionBuilder.
		[Test]
		public Task DriftIsReported() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync("""
			namespace LinqToDB.Internal.Linq.Builder
			{
				sealed class {|LINQ2DB0011:ExpressionBuilder|}
				{
				}
			}
			""");

		// ExpressionBuilder.MappingSchema no longer alias-shaped disables the rule, reported on ExpressionBuilder.
		[Test]
		public Task AliasDriftIsReported() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Model
				.Replace("sealed partial class ExpressionBuilder", "sealed partial class {|#0:ExpressionBuilder|}")
				.Replace("MappingSchema     => DataContext.MappingSchema;", "MappingSchema     { get; } = null!;"),
			new DiagnosticResult("LINQ2DB0011", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithMessage("The LINQ2DB0010 model could not be read: 'LinqToDB.Internal.Linq.Builder.ExpressionBuilder.MappingSchema' is no longer a '<IDataContext member>.MappingSchema' getter. The rule is disabled for the whole compilation until the analyzer is updated."));

		// A renamed member a trigger matches by name disables the rule visibly, not silently.
		[Test]
		public Task BuilderMemberDriftIsReported() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Model
				.Replace("sealed partial class ExpressionBuilder", "sealed partial class {|#0:ExpressionBuilder|}")
				.Replace("ExpressionBuilder Builder       { get; }", "ExpressionBuilder Owner         { get; }")
				.Replace("Builder = builder;", "Owner = builder;"),
			new DiagnosticResult("LINQ2DB0011", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithMessage("The LINQ2DB0010 model could not be read: 'LinqToDB.Internal.Linq.Builder.IBuildContext.Builder' was not found in the assembly that declares 'LinqToDB.Internal.Linq.Builder.ExpressionBuilder'. The rule is disabled for the whole compilation until the analyzer is updated."));

		[Test]
		public Task BuildMethodCallDriftIsReported() => AnalyzerVerifier<BuildContextMappingSchemaAnalyzer>.VerifyAsync(
			Model
				.Replace("sealed partial class ExpressionBuilder", "sealed partial class {|#0:ExpressionBuilder|}")
				.Replace("protected abstract object BuildMethodCall(", "protected abstract object BuildCall("),
			new DiagnosticResult("LINQ2DB0011", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithMessage("The LINQ2DB0010 model could not be read: 'LinqToDB.Internal.Linq.Builder.MethodCallBuilder.BuildMethodCall' was not found in the assembly that declares 'LinqToDB.Internal.Linq.Builder.ExpressionBuilder'. The rule is disabled for the whole compilation until the analyzer is updated."));
	}
}
