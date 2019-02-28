using System;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Parser
{
	public class ParseBuildInfo
	{
		public ParseBuildInfo()
		{
			Sequence = new Sequence();
		}

		public ParseBuildInfo([NotNull] Sequence sequence)
		{
			Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
		}

		public Sequence Sequence { get; }
	}
}
