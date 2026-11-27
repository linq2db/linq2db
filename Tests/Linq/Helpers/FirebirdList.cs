using LinqToDB;

namespace Tests.Linq.Helpers.FirebirdList
{
	public static class FBExtensions
	{
		[Sql.Extension("LIST({expr}, {splitter})", TokenName = "function", PreferServerSide = true)]
		public static string FbList<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] string splitter)
			=> throw new ServerSideOnlyException(nameof(FbList));

		[Sql.Extension("LIST({expr})", TokenName = "function",  PreferServerSide = true)]
		public static string FbList<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
			=> throw new ServerSideOnlyException(nameof(FbList));
	}
}
