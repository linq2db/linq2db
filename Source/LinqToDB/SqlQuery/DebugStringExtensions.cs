using System.Text;

namespace LinqToDB.SqlQuery
{
	public static class DebugStringExtensions
	{
		public static SqlTextWriter Append<T>(this SqlTextWriter writer, T? element, Dictionary<IQueryElement, IQueryElement> dic)
			where T : IQueryElement
		{
			if (element == null)
				return writer;

			var sb = new StringBuilder();
			element.ToString(sb, dic);
			return writer.AppendIdentCheck(sb.ToString());
		}
	}
}
