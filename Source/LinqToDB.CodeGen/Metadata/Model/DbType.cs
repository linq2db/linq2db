namespace LinqToDB.CodeGen.Metadata
{
	public record DbType(bool IsNullable, string? Type, long? Length, int? Precision, int? Scale, DataType? DataType);
}
