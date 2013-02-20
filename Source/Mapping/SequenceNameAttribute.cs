using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class SequenceNameAttribute : Attribute
	{
		public SequenceNameAttribute(string configuration, string sequenceName)
		{
			Configuration = configuration;
			SequenceName  = sequenceName;
		}

		public SequenceNameAttribute(string sequenceName)
		{
			SequenceName = sequenceName;
		}

		public string Configuration { get; set; }
		public string SequenceName  { get; set; }
	}
}
