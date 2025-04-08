using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IForeignKey : IProperty
	{
		string          KeyName         { get; set; }
		ITable          ThisTable       { get; set; }
		ITable          OtherTable      { get; set; }
		List<IColumn>   ThisColumns     { get; set; }
		List<IColumn>   OtherColumns    { get; set; }
		bool            CanBeNull       { get; set; }
		IForeignKey?    BackReference   { get; set; }
		string          MemberName      { get; set; }
		AssociationType AssociationType { get; set; }
	}
}
