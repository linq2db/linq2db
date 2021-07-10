namespace LinqToDB.CodeGen.CodeModel
{
	public class XmlCommentBuilder
	{
		private readonly CodeXmlComment _comment;
		public XmlCommentBuilder(CodeXmlComment comment)
		{
			_comment = comment;
		}

		public XmlCommentBuilder Summary(string summary)
		{
			_comment.Summary = summary;
			return this;
		}

		public XmlCommentBuilder Parameter(CodeIdentifier parameter, string text)
		{
			_comment.Parameters.Add((parameter, text));
			return this;
		}
	}

}
