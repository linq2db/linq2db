namespace LinqToDB.CodeModel
{
	internal static class TriviaExtensions
	{
		public static T NewLine<T>(this T node, bool after = true)
			where T : ICodeStatement
		{
			node.AddSimpleTrivia(SimpleTrivia.NewLine, after);

			return node;
		}

		public static T Wrap<T>(this T node, int paddingSize)
			where T: CodeCallBase
		{
			node.AddWrapSimpleTrivia(SimpleTrivia.NewLine);

			while (paddingSize > 0)
			{
				node.AddWrapSimpleTrivia(SimpleTrivia.Padding);
				paddingSize--;
			}

			return node;
		}
	}
}
