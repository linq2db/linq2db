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

		private AssociationType _associationType = AssociationType.Auto;
		public  AssociationType  AssociationType
		{
			get => _associationType;
			set
			{
				_associationType = value;

				if (BackReference != null)
				{
					switch (value)
					{
						case AssociationType.Auto      : BackReference.AssociationType = AssociationType.Auto;      break;
						case AssociationType.OneToOne  : BackReference.AssociationType = AssociationType.OneToOne;  break;
						case AssociationType.OneToMany : BackReference.AssociationType = AssociationType.ManyToOne; break;
						case AssociationType.ManyToOne : BackReference.AssociationType = AssociationType.OneToMany; break;
					}
				}
			}
		}
	}
}
