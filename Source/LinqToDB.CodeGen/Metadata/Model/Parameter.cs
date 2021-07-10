using LinqToDB.CodeGen.ContextModel;

namespace LinqToDB.CodeGen.Metadata
{
	public record Parameter(string Name, string? Description, DbType Type, ParameterDirection Direction);
}
