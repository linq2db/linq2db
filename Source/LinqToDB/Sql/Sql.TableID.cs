using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public struct SqlID
		{
			public string ID { get; }

			public SqlID(string id)
			{
				ID = id;
			}

			public override string ToString()
			{
				return ID;
			}

			public override bool Equals(object? obj)
			{
				return ID == (obj is SqlID id ? id.ID : null);
			}

			public override int GetHashCode()
			{
				return ID.GetHashCode();
			}
		}

		public static SqlID TableID(string id)
		{
			return new(id);
		}
	}
}
