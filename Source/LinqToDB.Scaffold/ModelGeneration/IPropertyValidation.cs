namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IPropertyValidation : IProperty
	{
		bool CustomValidation { get; set; }
		bool ValidateProperty { get; set; }
	}
}
