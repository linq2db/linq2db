using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
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

		public static QueryElementTextWriter AppendExtensions(this QueryElementTextWriter writer, ICollection<SqlQueryExtension>? extensions)
		{
			if (extensions?.Count > 0)
			{
				writer.AppendLine();

				foreach (var extension in extensions)
				{
					if (extension.Arguments.TryGetValue("hint", out var hintValue))
					{
						writer
							.Append("/* ")
							.Append(hintValue)
							.AppendLine(" */");
					}
					else
					{
						writer.AppendLine("/*extension*/");
					}
				}
			}

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

		/// <summary>
		/// Appends UniqId to writer only for Debug configuration.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="writer"></param>
		/// <param name="element"></param>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		internal static QueryElementTextWriter DebugAppendUniqueId<T>(this QueryElementTextWriter writer, T element, SelectQuery? selectQuery = null)
			where T : QueryElement
		{
#if DEBUG
			writer.Append("[UID:")
				.Append(element.UniqueId)
				.Append(']');
#endif
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
