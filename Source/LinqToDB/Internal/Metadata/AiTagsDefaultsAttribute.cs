using System;

namespace LinqToDB.Internal.Metadata
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
	sealed class AiTagsDefaultsAttribute : Attribute
	{
		public AiGroup         Groups        { get; init; }
		public AiExecution     Execution     { get; init; }
		public AiComposability Composability { get; init; }
		public AiAffects       Affects       { get; init; }
		public AiPipeline      Pipeline      { get; init; }
		public AiProvider      Provider      { get; init; }
		public AiHintType      HintType      { get; init; }
	}
}
