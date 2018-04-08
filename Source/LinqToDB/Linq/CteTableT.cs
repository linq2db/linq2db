using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Linq
{
	class CteTable<T> : ExpressionQuery<T>
	{
		public CteTable(IDataContext dataContext)
		{
			Init(dataContext, null);
		}

		public CteTable(IDataContext dataContext, Expression expression)
		{
			Init(dataContext, expression);
		}

		public string TableName    { get; set; }

		public string GetTableName() =>
			DataContext.CreateSqlProvider()
				.ConvertTableName(new StringBuilder(), "", "", TableName)
				.ToString();

		#region Overrides

		public override string ToString()
		{
			return "CteTable(" + typeof(T).Name + ")";
		}

		#endregion
	}
}
