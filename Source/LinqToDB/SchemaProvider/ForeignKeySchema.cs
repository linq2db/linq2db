using System.Collections.Generic;

namespace LinqToDB.SchemaProvider
{
	public class ForeignKeySchema
	{
		public string             KeyName       { get; set; } = null!;
		public TableSchema?       ThisTable     { get; set; }
		public TableSchema        OtherTable    { get; set; } = null!;
		public List<ColumnSchema> ThisColumns   { get; set; } = null!;
		public List<ColumnSchema> OtherColumns  { get; set; } = null!;
		public bool               CanBeNull     { get; set; }
		public ForeignKeySchema?  BackReference { get; set; }
		public string             MemberName    { get; set; } = null!;

		public AssociationType AssociationType
		{
			get;
			set
			{
				field = value;

				BackReference?.AssociationType = value switch
				{
					AssociationType.Auto      => AssociationType.Auto,
					AssociationType.OneToOne  => AssociationType.OneToOne,
					AssociationType.OneToMany => AssociationType.ManyToOne,
					AssociationType.ManyToOne => AssociationType.OneToMany,
					_ => (AssociationType)-1,
				};
			}
		} = AssociationType.Auto;
	}
}
