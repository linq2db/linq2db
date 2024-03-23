using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using Common.Internal;

	sealed class CteTable<T> : ExpressionQuery<T>
	{
		public CteTable(IDataContext dataContext)
		{
			Init(dataContext, null);
		}

		public CteTable(IDataContext dataContext, Expression expression)
		{
			Init(dataContext, expression);
		}

		public string? TableName { get; set; }

		public string GetTableName()
		{
			using var sb = Pools.StringBuilder.Allocate();

			return DataContext.CreateSqlProvider()
				.BuildObjectName(sb.Value, new(TableName!))
				.ToString();
		}

		#region Overrides

		public override string ToString()
		{
			return "CteTable(" + typeof(T).Name + ")";
		}

		#endregion
	}
}
