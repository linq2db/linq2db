using System;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace Tests.Model
{
	#region Parent/Child/GrandChild

	public interface IParent
	{
		int  ParentID { get; }
		int? Value1   { get; }
	}

	public class Parent : IEquatable<Parent>, IComparable
	{
		public int  ParentID;
		public int? Value1;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public List<Child> Children;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public List<GrandChild> GrandChildren;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public IEnumerable<Child> Children2
		{
			get { return Children; }
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(Parent)) return false;
			return Equals((Parent)obj);
		}

		public bool Equals(Parent other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.ParentID == ParentID && other.Value1.Equals(Value1);
		}

		public override int GetHashCode()
		{
			unchecked { return (ParentID * 397) ^ (Value1 ?? 0); }
		}

		public int CompareTo(object obj)
		{
			return ParentID - ((Parent)obj).ParentID;
		}

		[Association(ThisKey = "ParentID", OtherKey = "ID")]
		public LinqDataTypes Types;
	}

	public class Child
	{
		[PrimaryKey] public int ParentID;
		[PrimaryKey] public int ChildID;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public Parent  Parent;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = false)]
		public Parent1 Parent1;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID2", CanBeNull = false)]
		public Parent3 ParentID2;

		[Association(ThisKey = "ParentID, ChildID", OtherKey = "ParentID, ChildID")]
		public List<GrandChild> GrandChildren;

		public override bool Equals(object obj)
		{
			return Equals(obj as Child);
		}

		public bool Equals(Child other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return other.ParentID == ParentID && other.ChildID == ChildID;
		}

		public override int GetHashCode()
		{
			unchecked { return (ParentID * 397) ^ ChildID; }
		}
	}

	public class GrandChild : IEquatable<GrandChild>
	{
		public GrandChild()
		{
			
		}

		public int? ParentID;
		public int? ChildID;
		public int? GrandChildID;

		[Association(ThisKey = "ParentID, ChildID", OtherKey = "ParentID, ChildID")]
		public Child Child;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (GrandChild)) return false;

			return Equals((GrandChild)obj);
		}

		public bool Equals(GrandChild other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return other.ParentID.Equals(ParentID) && other.ChildID.Equals(ChildID) && other.GrandChildID.Equals(GrandChildID);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = ParentID.HasValue ? ParentID.Value : 0;

				result = (result * 397) ^ (ChildID.     HasValue ? ChildID.     Value : 0);
				result = (result * 397) ^ (GrandChildID.HasValue ? GrandChildID.Value : 0);

				return result;
			}
		}
	}

	[Table(Name="Parent")]
	public class Parent3 : IEquatable<Parent3>, IComparable
	{
		[MapField("ParentID")]
		public int  ParentID2 { get; set; }
		public int? Value1    { get; set; }

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof (Parent3)) return false;
			return Equals((Parent3)obj);
		}

		public bool Equals(Parent3 other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.ParentID2 == ParentID2 && other.Value1.Equals(Value1);
		}

		public override int GetHashCode()
		{
			unchecked { return (ParentID2 * 397) ^ (Value1 ?? 0); }
		}

		public int CompareTo(object obj)
		{
			return ParentID2 - ((Parent3)obj).ParentID2;
		}
	}

	[Table(Name="Parent")]
	public class Parent4 : IEquatable<Parent4>, IComparable
	{
		public int       ParentID;
		public TypeValue Value1;

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof (Parent4)) return false;
			return Equals((Parent4)obj);
		}

		public bool Equals(Parent4 other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.ParentID == ParentID && other.Value1.Equals(Value1);
		}

		public override int GetHashCode()
		{
			unchecked { return (ParentID * 397) ^ (int)Value1; }
		}

		public int CompareTo(object obj)
		{
			return ParentID - ((Parent4)obj).ParentID;
		}
	}

	[Table(Name="Parent")]
	public class Parent5 : IEquatable<Parent5>, IComparable
	{
		public int  ParentID;
		public int? Value1;

		[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
		public List<Parent5> Children;

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(Parent5)) return false;
			return Equals((Parent5)obj);
		}

		public bool Equals(Parent5 other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.ParentID == ParentID && other.Value1.Equals(Value1);
		}

		public override int GetHashCode()
		{
			unchecked { return (ParentID * 397) ^ (int)Value1; }
		}

		public int CompareTo(object obj)
		{
			return ParentID - ((Parent5)obj).ParentID;
		}
	}

	#endregion

	#region Parent1/GrandChild1

	[Table(Name="Parent")]
	public class Parent1 : IEquatable<Parent1>, IComparable
	{
		[PrimaryKey]
		public int  ParentID;
		public int? Value1;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public List<Child> Children;

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof (Parent1)) return false;
			return Equals((Parent1)obj);
		}

		public bool Equals(Parent1 other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.ParentID == ParentID;
		}

		public override int GetHashCode()
		{
			unchecked { return ParentID * 397; }
		}

		public int CompareTo(object obj)
		{
			return ParentID - ((Parent1)obj).ParentID;
		}
	}

	[Table(Name="GrandChild")]
	public class GrandChild1 : IEquatable<GrandChild1>
	{
		public int  ParentID;
		public int? ChildID;
		public int? GrandChildID;

		[Association(ThisKey = "ParentID, ChildID", OtherKey = "ParentID, ChildID")]
		public Child Child;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = false)]
		public Parent1 Parent;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (GrandChild1)) return false;

			return Equals((GrandChild1)obj);
		}

		public bool Equals(GrandChild1 other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return other.ParentID.Equals(ParentID) && other.ChildID.Equals(ChildID) && other.GrandChildID.Equals(GrandChildID);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = ParentID;

				result = (result * 397) ^ (ChildID.     HasValue ? ChildID.     Value : 0);
				result = (result * 397) ^ (GrandChildID.HasValue ? GrandChildID.Value : 0);

				return result;
			}
		}
	}

	#endregion

	#region Inheritance

	[Table(Name="Parent")]
	[InheritanceMapping(Code = null, Type = typeof(ParentInheritanceNull))]
	[InheritanceMapping(Code = 1,    Type = typeof(ParentInheritance1))]
	[InheritanceMapping(             Type = typeof(ParentInheritanceValue), IsDefault = true)]
	public abstract class ParentInheritanceBase : IEquatable<ParentInheritanceBase>, IComparable
	{
		[PrimaryKey]
		public int ParentID;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public List<Child> Children;

		public override bool Equals(object obj)
		{
			if (obj.GetType() != GetType()) return false;
			return Equals((ParentInheritanceBase)obj);
		}

		public bool Equals(ParentInheritanceBase other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.ParentID == ParentID;
		}

		public override int GetHashCode()
		{
			return ParentID;
		}

		public int CompareTo(object obj)
		{
			return ParentID - ((Parent)obj).ParentID;
		}
	}

	public class ParentInheritanceNull : ParentInheritanceBase
	{
	}

	public class ParentInheritance1 : ParentInheritanceBase, IEquatable<ParentInheritance1>
	{
		[Column(IsDiscriminator = true)]
		[MapField(IsInheritanceDiscriminator = true)]
		public int Value1;

		public override bool Equals(object obj)
		{
			var ret = base.Equals(obj) && Equals((ParentInheritance1)obj);
			return ret;
		}

		public bool Equals(ParentInheritance1 other)
		{
			return base.Equals(other) && other.Value1.Equals(Value1);
		}

		public override int GetHashCode()
		{
			unchecked { return (ParentID * 397) ^ Value1; }
		}
	}

	public class ParentInheritanceValue : ParentInheritanceBase
	{
		[Column(IsDiscriminator = true)]
		[MapField(IsInheritanceDiscriminator = true)]
		public int Value1;

		public override bool Equals(object obj)
		{
			return base.Equals(obj) && Equals((ParentInheritanceValue)obj);
		}

		public bool Equals(ParentInheritanceValue other)
		{
			return base.Equals(other) && other.Value1.Equals(Value1);
		}

		public override int GetHashCode()
		{
			unchecked { return (ParentID * 397) ^ Value1; }
		}
	}

	#endregion

	#region Inheritance2

	[Table(Name="Parent")]
	[InheritanceMapping(Code = null, Type = typeof(ParentInheritanceBase2))]
	[InheritanceMapping(Code = 1,    Type = typeof(ParentInheritance12))]
	[InheritanceMapping(Code = 2,    Type = typeof(ParentInheritance12))]
	public class ParentInheritanceBase2
	{
		[PrimaryKey]
		public int ParentID;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public List<Child> Children;
	}

	public class ParentInheritance12 : ParentInheritanceBase2
	{
		[MapField(IsInheritanceDiscriminator = true)]
		public int Value1;
	}

	#endregion

	#region Inheritance3

	[Table(Name="Parent")]
	[InheritanceMapping(Code = null, Type = typeof(ParentInheritanceBase3))]
	[InheritanceMapping(Code = 1,    Type = typeof(ParentInheritance13))]
	[InheritanceMapping(Code = 2,    Type = typeof(ParentInheritance13))]
	public abstract class ParentInheritanceBase3
	{
		[PrimaryKey]
		public int ParentID;

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public List<Child> Children;
	}

	public class ParentInheritance13 : ParentInheritanceBase3
	{
		[MapField("Value1", IsInheritanceDiscriminator = true)]
		public int Value;
	}

	#endregion

	#region Inheritance4

	public enum Parent4Type
	{
		Value1 = 1,
		Value2 = 2
	}

	[Table(Name="Parent")]
	[InheritanceMapping(Code = (int)Parent4Type.Value1, Type = typeof(ParentInheritance14))]
	[InheritanceMapping(Code = (int)Parent4Type.Value2, Type = typeof(ParentInheritance24))]
	public abstract class ParentInheritanceBase4
	{
		[PrimaryKey]
		public int ParentID;

		public abstract Parent4Type Value1 { get; }
	}

	public class ParentInheritance14 : ParentInheritanceBase4
	{
		[MapField(IsInheritanceDiscriminator = true)]
		public override Parent4Type Value1 { get { return Parent4Type.Value1; } }
	}

	public class ParentInheritance24 : ParentInheritanceBase4
	{
		[MapField(IsInheritanceDiscriminator = true)]
		public override Parent4Type Value1 { get { return Parent4Type.Value2; } }
	}

	#endregion

	public class Functions
	{
		private readonly IDataContext _ctx;

		public Functions(IDataContext ctx)
		{
			_ctx = ctx;
		}

		[TableFunction(Name="GetParentByID")]
		public Table<Parent> GetParentByID(int? id)
		{
			return _ctx.GetTable<Parent>(this, (MethodInfo)(MethodBase.GetCurrentMethod()), id);
		}

		[TableExpression("{0} {1} WITH (TABLOCK)")]
		public Table<T> WithTabLock<T>()
			where T : class 
		{
			return _ctx.GetTable<T>(this, ((MethodInfo)(MethodBase.GetCurrentMethod())).MakeGenericMethod(typeof(T)));
		}
	}
}
