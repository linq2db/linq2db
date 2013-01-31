using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class SequenceNameAttribute : Attribute
	{
		public SequenceNameAttribute(string providerName, string sequenceName)
		{
			ProviderName = providerName;
			SequenceName = sequenceName;
		}

		public SequenceNameAttribute(string sequenceName)
		{
			SequenceName = sequenceName;
		}

		public string ProviderName { get; set; }
		public string SequenceName { get; set; }
	}
}
