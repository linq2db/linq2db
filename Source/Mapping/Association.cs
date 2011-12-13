using System;

using JNotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB.Mapping
{
	using Common;
	using Reflection;

	public class Association
	{
		public Association(
			[JNotNull] MemberAccessor memberAccessor,
			[JNotNull] string[]       thisKey,
			[JNotNull] string[]       otherKey,
			           string         storage,
			           bool           canBeNull)
		{
			if (memberAccessor == null) throw new ArgumentNullException("memberAccessor");
			if (thisKey        == null) throw new ArgumentNullException("thisKey");
			if (otherKey       == null) throw new ArgumentNullException("otherKey");

			if (thisKey.Length == 0)
				throw new ArgumentOutOfRangeException(
					"thisKey",
					string.Format("Association '{0}.{1}' does not define keys.", memberAccessor.TypeAccessor.Type.Name, memberAccessor.Name));

			if (thisKey.Length != otherKey.Length)
				throw new ArgumentException(
					string.Format(
						"Association '{0}.{1}' has different number of keys for parent and child objects.",
						memberAccessor.TypeAccessor.Type.Name, memberAccessor.Name));

			MemberAccessor = memberAccessor;
			ThisKey        = thisKey;
			OtherKey       = otherKey;
			Storage        = storage;
			CanBeNull      = canBeNull;
		}

		public MemberAccessor MemberAccessor { get; set; }
		public string[]       ThisKey        { get; set; }
		public string[]       OtherKey       { get; set; }
		public string         Storage        { get; set; }
		public bool           CanBeNull      { get; set; }

		public static string[] ParseKeys(string keys)
		{
			return keys == null ? Array<string>.Empty : keys.Replace(" ", "").Split(',');
		}
	}
}
