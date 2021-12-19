using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Contains basic properties of class.
	/// </summary>
	public sealed class ClassModel
	{
		public ClassModel(string name)
		{
			Name = name;
		}

		public ClassModel(string fileName, string name)
		{
			FileName = fileName;
			Name     = name;
		}

		/// <summary>
		/// Gets or sets xml-doc comment summary section text for class.
		/// </summary>
		public string? Summary   { get; set; }

		/// <summary>
		/// Gets or sets class name.
		/// </summary>
		public string  Name      { get; set; }
		/// <summary>
		/// Gets or sets class namespace.
		/// </summary>
		public string? Namespace { get; set; }

		/// <summary>
		/// Gets or sets type of base class to inherit current class from.
		/// </summary>
		public IType?  BaseType  { get; set; }

		/// <summary>
		/// Get or sets class visibility.
		/// </summary>
		public bool    IsPublic  { get; set; }
		/// <summary>
		/// Get or sets class static attribute.
		/// </summary>
		public bool    IsStatic  { get; set; }
		/// <summary>
		/// Get or sets class partial modifier status.
		/// </summary>
		public bool    IsPartial { get; set; }

		/// <summary>
		/// Gets or sets optional file name for class without extension.
		/// </summary>
		public string? FileName  { get; set; }
	}
}
