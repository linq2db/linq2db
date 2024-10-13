namespace LinqToDB.DataProvider.Oracle
{
	using LinqToDB.SqlQuery;

	public class Oracle23SqlExpressionConvertVisitor : Oracle12SqlExpressionConvertVisitor
	{
		public Oracle23SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}
	}
}
