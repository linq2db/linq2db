#pragma warning disable CA1305
#pragma warning disable RS0030

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IEditableObjectProperty : IProperty
	{
		public bool   IsEditable  { get; set; }
		public string IsDirtyText { get; set; }
	}
}
