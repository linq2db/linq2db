using System;

namespace LinqToDB.Internal.Metadata
{
	[AttributeUsage(
		AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field |
		AttributeTargets.Class  | AttributeTargets.Interface,
		AllowMultiple = false, Inherited = false)]
	sealed class AiTagsAttribute : Attribute
	{
		// Named-argument presence (not property nullability) signals "explicitly set" - attribute
		// parameter types cannot be Nullable<T> (CS0655), and CustomAttributeData.NamedArguments
		// only contains arguments that were actually supplied at the call site.
		public AiGroup         Groups        { get; init; }
		public AiExecution     Execution     { get; init; }
		public AiComposability Composability { get; init; }
		public AiAffects       Affects       { get; init; }
		public AiPipeline      Pipeline      { get; init; }
		public AiProvider      Provider      { get; init; }
		public AiHintType      HintType      { get; init; }
	}
}
