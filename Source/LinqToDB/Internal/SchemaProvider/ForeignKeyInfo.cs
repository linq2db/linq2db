namespace LinqToDB.Internal.SchemaProvider
{
	public sealed class ForeignKeyInfo
	{
		public string Name         = null!;
		public string ThisTableID  = null!;
		public string ThisColumn   = null!;
		public string OtherTableID = null!;
		public string OtherColumn  = null!;
		public int    Ordinal;
	}
}
