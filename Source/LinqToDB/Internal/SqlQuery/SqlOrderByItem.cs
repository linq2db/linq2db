namespace LinqToDB.Internal.SqlQuery
{
	public class SqlOrderByItem : QueryElement
	{
		public SqlOrderByItem(ISqlExpression expression, bool isDescending, bool isPositioned)
		{
			Expression   = expression;
			IsDescending = isDescending;
			IsPositioned = isPositioned;
		}

		public ISqlExpression Expression   { get; internal set; }
		public bool           IsDescending { get; }
		public bool           IsPositioned { get; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.OrderByItem;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendElement(Expression);

			if (IsPositioned)
				writer.Append(":by_index");

			if (IsDescending)
				writer.Append(" DESC");

			return writer;
		}

		public override string ToString()
		{
			return this.ToDebugString();
		}

		#endregion
	}
}
