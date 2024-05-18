using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IForeignKey : IProperty
	{
		string          KeyName         { get; set; }
		ITable          ThisTable       { get; set; }
		ITable          OtherTable      { get; set; }
		List<IColumn>   ThisColumns     { get; set; }
		List<IColumn>   OtherColumns    { get; set; }
		bool            CanBeNull       { get; set; }
		IForeignKey?    BackReference   { get; set; }
		string          MemberName      { get; set; }
		AssociationType AssociationType { get; set; }
	}

	/// <summary>
	/// For internal use.
	/// </summary>
	public class ForeignKey<T> : Property<T>, IForeignKey
		where T : ForeignKey<T>, new()
	{
		public string        KeyName       { get; set; } = null!;
		public ITable        ThisTable     { get; set; } = null!;
		public ITable        OtherTable    { get; set; } = null!;
		public List<IColumn> ThisColumns   { get; set; } = new();
		public List<IColumn> OtherColumns  { get; set; } = new();
		public bool          CanBeNull     { get; set; }
		public IForeignKey?  BackReference { get; set; }

		public string MemberName
		{
			get => Name!;
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
