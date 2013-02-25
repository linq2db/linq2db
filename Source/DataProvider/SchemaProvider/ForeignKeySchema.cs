using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SchemaProvider
{
	public class ForeignKeySchema
	{
		public string             KeyName       { get; set; }
		public TableSchema        ThisTable     { get; set; }
		public TableSchema        OtherTable    { get; set; }
		public List<ColumnSchema> ThisColumns   { get; set; }
		public List<ColumnSchema> OtherColumns  { get; set; }
		public bool               CanBeNull     { get; set; }
		public ForeignKeySchema   BackReference { get; set; }
		public string             MemberName    { get; set; }

		private AssociationType _associationType = AssociationType.Auto;
		public  AssociationType  AssociationType
		{
			get { return _associationType; }
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
