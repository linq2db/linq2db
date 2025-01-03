using System;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public class TableContext<TTable, TProcedure>(ModelGenerator<TTable, TProcedure> transformation, string tableName)
		where TTable     : class, ITable, new()
		where TProcedure : IProcedure<TTable>, new()
	{
		public ModelGenerator<TTable,TProcedure> Transformation = transformation;
		public string                            TableName      = tableName;

		public TableContext<TTable,TProcedure> Column(
			string  columnName,
			string? MemberName  = null,
			string? Type        = null,
			bool?   IsNullable  = null,
			string? Conditional = null)
		{
			var c = Transformation.GetColumn(TableName, columnName);

			if (MemberName  != null) c.MemberName  = MemberName;
			if (Type        != null) c.TypeBuilder = () => Type;
			if (IsNullable  != null) c.IsNullable  = IsNullable.Value;
			if (Conditional != null) c.Conditional = Conditional;

			return this;
		}

		public TableContext<TTable,TProcedure> FK(
			string           fkName,
			string?          MemberName      = null,
			AssociationType? AssociationType = null,
			bool?            CanBeNull       = null)
		{
			var c = Transformation.GetFK(TableName, fkName);

			if (MemberName      != null) c.MemberName      = MemberName;
			if (AssociationType != null) c.AssociationType = AssociationType.Value;
			if (CanBeNull       != null) c.CanBeNull       = CanBeNull.Value;

			return this;
		}
	}
}
