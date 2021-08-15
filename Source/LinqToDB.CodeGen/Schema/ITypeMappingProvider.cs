using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.Schema
{
	public interface ITypeMappingProvider
	{
		(IType clrType, DataType? dataType)? GetTypeMapping(DatabaseType databaseType);
	}
}
