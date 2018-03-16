using LinqToDB.Mapping;

namespace Tests.Model
{
	public class Cat
	{
		[Column, Identity, PrimaryKey]
		public int CatID;
		[NotNull] public string Name;
		[NotNull] public int Age;
		[Nullable] public string Color;


		public override bool Equals(object obj)
		{
			return Equals(obj as Cat);
		}

		public bool Equals(Cat other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.CatID == CatID && Equals(other.Name, Name) && Equals(other.Age, Age) && Equals(other.Color, Color);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = CatID;
				result = (result * 397) ^ (Name != null ? Name.GetHashCode() : 0);
				result = (result * 397) ^ Age.GetHashCode();
				result = (result * 397) ^ (Color != null ? Color.GetHashCode() : 0);
				return result;
			}
		}


	}
}
