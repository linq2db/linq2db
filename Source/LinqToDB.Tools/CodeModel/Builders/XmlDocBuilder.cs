namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeXmlComment"/> object builder.
	/// </summary>
	public sealed class XmlDocBuilder
	{
		private readonly CodeXmlComment _comment;

		internal XmlDocBuilder(CodeXmlComment comment)
		{
			_comment = comment;
		}

		/// <summary>
		/// Add summary section to xml-doc.
		/// </summary>
		/// <param name="summary">Summary text.</param>
		/// <returns>Builder instance.</returns>
		public XmlDocBuilder Summary(string summary)
		{
			_comment.Summary = summary;
			return this;
		}

		/// <summary>
		/// Add parameter section to xml-doc.
		/// </summary>
		/// <param name="parameter">Parameter name.</param>
		/// <param name="text">Help text.</param>
		/// <returns>Builder instance.</returns>
		public XmlDocBuilder Parameter(CodeIdentifier parameter, string text)
		{
			_comment.AddParameter(parameter, text);
			return this;
		}
	}
}
