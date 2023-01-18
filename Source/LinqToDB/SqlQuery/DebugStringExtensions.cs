namespace LinqToDB.SqlQuery
{
	public static class DebugStringExtensions
	{
		public static QueryElementTextWriter AppendElement<T>(this QueryElementTextWriter writer, T? element)
			where T : IQueryElement
		{
			if (element == null)
				return writer;

			element.ToString(writer);
			return writer;
		}

		internal static string ToDebugString<T>(this T element, SelectQuery? selectQuery = null)
			where T : IQueryElement
		{
			try
			{
				var writer = new QueryElementTextWriter(NullabilityContext.GetContext(selectQuery));
				writer.AppendElement(element);
				return writer.ToString();
			}
			catch
			{
				return $"FAIL ToDebugString('{element.GetType().Name}').";
			}
		}
	}
}
