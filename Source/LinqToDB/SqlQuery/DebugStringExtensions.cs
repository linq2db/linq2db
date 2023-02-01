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

		public static QueryElementTextWriter AppendTag(this QueryElementTextWriter writer, SqlComment? comment)
		{
			if (comment != null)
			{
				writer.Append("/* ");

				for (var i = 0; i < comment.Lines.Count; i++)
				{
					writer.Append(comment.Lines[i].Replace("/*", "").Replace("*/", ""));
					if (i < comment.Lines.Count - 1)
						writer.AppendLine();
				}

				writer.AppendLine(" */");
			}

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
