using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.CodeGeneration
{
	public interface IMetadataBuilder
	{
		void BuildEntityMetadata(EntityMetadata metadata, ClassBuilder entityBuilder);
		void BuildColumnMetadata(ColumnMetadata metadata, PropertyBuilder propertyBuilder);
		void BuildAssociationMetadata(AssociationMetadata metadata, PropertyBuilder propertyBuilder);
		void BuildAssociationMetadata(AssociationMetadata metadata, MethodBuilder methodBuilder);
		void BuildFunctionMetadata(FunctionMetadata metadata, MethodBuilder methodBuilder);
		void BuildTableFunctionMetadata(TableFunctionMetadata metadata, MethodBuilder methodBuilder);
	}
}
