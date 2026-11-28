namespace LinqToDB.Internal.DataProvider.MySql
{
	/// <summary>
	/// MySQL and MariaDB cap a schema object name at 64 characters but an alias far higher - measured at
	/// 256 on MySQL and 255 on MariaDB, so the lower is taken. Declaring the smaller one for everything
	/// truncated aliases the server accepts.
	/// </summary>
	public sealed class MySqlIdentifierService() : IdentifierServiceSimple(64)
	{
		protected override int GetMaxLength(IdentifierKind identifierKind)
			=> identifierKind == IdentifierKind.Alias ? 255 : base.GetMaxLength(identifierKind);
	}
}
