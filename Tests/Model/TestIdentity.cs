using System;

using LinqToDB;

namespace Tests.Model
{
	public class TestIdentity
	{
		[Identity, PrimaryKey]
		//[SequenceName(ProviderName.PostgreSQL, "Seq")]
		//[SequenceName(ProviderName.Firebird,   "PersonID")]
		//[SequenceName("ID")]
		public int ID;
	}
}
