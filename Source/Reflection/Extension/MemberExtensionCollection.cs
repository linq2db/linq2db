using System;
using System.Collections.Generic;

namespace LinqToDB.Reflection.Extension
{
	public class MemberExtensionCollection : Dictionary<string,MemberExtension>
	{
		public new MemberExtension this[string memberName]
		{
			get
			{
				if (this == _null)
					return MemberExtension.Null;

				MemberExtension value;

				return TryGetValue(memberName, out value) ? value : MemberExtension.Null;
			}
		}

		public void Add(MemberExtension memberInfo)
		{
			if (this != _null)
				Add(memberInfo.Name, memberInfo);
		}

		private static readonly MemberExtensionCollection _null = new MemberExtensionCollection();
		public  static          MemberExtensionCollection  Null
		{
			get { return _null; }
		}
	}
}
