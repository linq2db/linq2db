using System;

namespace LinqToDB.Data.Sql.SqlProvider
{
	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
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

		private string _providerName; public string ProviderName { get { return _providerName; } set { _providerName = value; } }
		private string _sequenceName; public string SequenceName { get { return _sequenceName; } set { _sequenceName = value; } }
	}
}
