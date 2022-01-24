using LinqToDB.CodeModel;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Type mapping information.
	/// </summary>
	/// <param name="CLRType">.net type.</param>
	/// <param name="DataType">Optional <see cref="DataType"/> hint.</param>
	public sealed record TypeMapping(IType CLRType, DataType? DataType);
}
