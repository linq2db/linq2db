using System.Collections.Generic;
using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Contains basic class property attributes.
	/// </summary>
	public sealed class PropertyModel
	{
		public PropertyModel(string name)
		{
			Name = name;
		}

		public PropertyModel(string name, IType type)
		{
			Name = name;
			Type = type;
		}

		/// <summary>
		/// Gets or sets property name.
		/// </summary>
		public string               Name             { get; set; }
		/// <summary>
		/// Gets or sets property type.
		/// </summary>
		public IType?               Type             { get; set; }
		/// <summary>
		/// Gets or sets summary section text for property xml-doc comment.
		/// </summary>
		public string?              Summary          { get; set; }
		/// <summary>
		/// Gets or sets property modifiers.
		/// </summary>
		public Modifiers            Modifiers        { get; set; }
		/// <summary>
		/// Gets or sets property default implementation attribute.
		/// </summary>
		public bool                 IsDefault        { get; set; }
		/// <summary>
		/// Gets or sets property setter status.
		/// </summary>
		public bool                 HasSetter        { get; set; }
		/// <summary>
		/// Gets or sets trailing code comment after property definition.
		/// Example:
		/// <code>
		/// public string MyProperty { get; set; } // this is property comment
		/// </code>
		/// </summary>
		public string?              TrailingComment  { get; set; }
		/// <summary>
		/// List of additional custom attributes. Doesn't include metadata attributes.
		/// </summary>
		public List<CodeAttribute>? CustomAttributes { get; set; }
	}
}
