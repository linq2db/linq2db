namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IField : IMemberBase
	{
		bool    IsStatic   { get; set; }
		bool    IsReadonly { get; set; }
		string? InitValue  { get; set; }
	}
}
