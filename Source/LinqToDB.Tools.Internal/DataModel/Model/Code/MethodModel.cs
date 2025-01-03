using System.Collections.Generic;
using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Contains basic method (including lambda methods and constructors) attributes.
	/// </summary>
	public sealed class MethodModel
	{
		public MethodModel(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets or sets summary section text for method xml-doc comment.
		/// </summary>
		public string?              Summary          { get; set; }
		/// <summary>
		/// Gets or sets method name.
		/// </summary>
		public string               Name             { get; set; }
		/// <summary>
		/// Gets or sets method modifiers.
		/// </summary>
		public Modifiers            Modifiers        { get; set; }
		/// <summary>
		/// List of additional custom attributes. Doesn't include metadata attributes.
		/// </summary>
		public List<CodeAttribute>? CustomAttributes { get; set; }
	}
}
