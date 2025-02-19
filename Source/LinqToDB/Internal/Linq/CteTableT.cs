namespace LinqToDB.Internal.Linq
{
	sealed class CteTable<T> : ExpressionQuery<T>
	{
		public CteTable(IDataContext dataContext)
		{
			Init(dataContext, null);
		}

		#region Overrides

		public override string ToString()
		{
			return "CteTable(" + typeof(T).Name + ")";
		}

		#endregion
	}
}
