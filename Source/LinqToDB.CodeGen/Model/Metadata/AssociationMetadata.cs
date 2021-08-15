using LinqToDB.CodeGen.Model;
using LinqToDB.Mapping;

namespace LinqToDB.CodeGen.Metadata
{
	public class AssociationMetadata
	{
		public AssociationMetadata()
		{
		}

		public AssociationMetadata(bool isBackReference, bool canBeNull)
		{
			IsBackReference = isBackReference;
			CanBeNull = canBeNull;
		}

		// set by framework
		public bool IsBackReference { get; set; }
		public bool CanBeNull { get; set; }
		public ICodeExpression? ThisKeyExpression { get; set; }
		public ICodeExpression? OtherKeyExpression { get; set; }

		// additional metadata, that could be set by user
		public string? Configuration { get; set; }
		public string? Alias { get; set; }
		public string? Storage { get; set; }
		public string? ThisKey { get; set; }
		public string? OtherKey { get; set; }
		public string? ExpressionPredicate { get; set; }
		public string? QueryExpressionMethod { get; set; }

		// not used by linq2db
		public string? KeyName { get; set; }
		public string? BackReferenceName { get; set; }
		public bool? HasIsBackReference { get; set; }
		public Relationship? Relationship { get; set; }
	}
}
