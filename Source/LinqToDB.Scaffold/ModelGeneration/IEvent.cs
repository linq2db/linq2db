namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IEvent : IMemberBase
	{
		bool IsStatic  { get; set; }
		bool IsVirtual { get; set; }
	}
}
