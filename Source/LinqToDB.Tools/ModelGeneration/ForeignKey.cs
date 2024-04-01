using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	public interface IForeignKey : IProperty
	{
		public string?         KeyName         { get; set; }
		public ITable?         OtherTable      { get; set; }
		public List<IColumn>   ThisColumns     { get; set; }
		public List<IColumn>   OtherColumns    { get; set; }
		public bool            CanBeNull       { get; set; }
		public IForeignKey?    BackReference   { get; set; }
		public string?         MemberName      { get; set; }
		public AssociationType AssociationType { get; set; }
	}

	public class ForeignKey<T> : Property<T>, IForeignKey
		where T : ForeignKey<T>, new()
	{
		public string?       KeyName       { get; set; }
		public ITable?       OtherTable    { get; set; }
		public List<IColumn> ThisColumns   { get; set; } = new();
		public List<IColumn> OtherColumns  { get; set; } = new();
		public bool          CanBeNull     { get; set; }
		public IForeignKey?  BackReference { get; set; }

		public string? MemberName
		{
			get => Name;
			set => Name = value;
		}

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

		public override bool EnforceNotNullable => ModelGenerator.EnableNullableReferenceTypes && ModelGenerator.EnforceModelNullability && (!CanBeNull || AssociationType == AssociationType.OneToMany);
	}
}
