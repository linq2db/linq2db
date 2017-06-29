using System;
using System.Reflection;

using JNotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB.Mapping
{
	using Common;

	public class AssociationDescriptor
	{
		public AssociationDescriptor(
			[JNotNull] Type       type,
			[JNotNull] MemberInfo memberInfo,
			[JNotNull] string[]   thisKey,
			[JNotNull] string[]   otherKey,
			           string     storage,
			           bool       canBeNull)
		{
			if (memberInfo == null) throw new ArgumentNullException("memberInfo");
			if (thisKey    == null) throw new ArgumentNullException("thisKey");
			if (otherKey   == null) throw new ArgumentNullException("otherKey");

			if (thisKey.Length == 0)
				throw new ArgumentOutOfRangeException(
					"thisKey",
					string.Format("Association '{0}.{1}' does not define keys.", type.Name, memberInfo.Name));

			if (thisKey.Length != otherKey.Length)
				throw new ArgumentException(
					string.Format(
						"Association '{0}.{1}' has different number of keys for parent and child objects.",
						type.Name, memberInfo.Name));

			MemberInfo = memberInfo;
			ThisKey    = thisKey;
			OtherKey   = otherKey;
			Storage    = storage;
			CanBeNull  = canBeNull;
		}

		public MemberInfo MemberInfo { get; set; }
		public string[]   ThisKey    { get; set; }
		public string[]   OtherKey   { get; set; }
		public string     Storage    { get; set; }
		public bool       CanBeNull  { get; set; }

		public static string[] ParseKeys(string keys)
		{
			return keys == null ? Array<string>.Empty : keys.Replace(" ", "").Split(',');
		}

		const string constantKeyPrefix = "const:";
		internal static bool TryParseConstantKey(string key, out string value)
		{
			if (key.StartsWith(constantKeyPrefix))
			{
				value = key.Substring(constantKeyPrefix.Length);
				return true;
			}
			else
			{
				value = null;
				return false;
			}
		}
	}
}
