namespace LinqToDB.DataProvider.Firebird
{
	using SqlProvider;

	public partial class FirebirdSqlBuilder
	{
		// source subquery select list shouldn't contain parameters otherwise following error will be
		// generated:
		//
		// FirebirdSql.Data.Common.IscException : Dynamic SQL Error
		// SQL error code = -804
		//Data type unknown
		protected override bool MergeSupportsParametersInSource => false;

		// VALUES(...) syntax not supported in MERGE source
		protected override bool MergeSupportsSourceDirectValues => false;

		protected override string FakeTable => "rdb$database";
	}
}
