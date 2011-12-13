using System;

namespace LinqToDB.Reflection.Extension
{
	public class MemberExtension
	{
		public MemberExtension()
		{
			_attributes = new AttributeNameCollection();
		}

		private MemberExtension(AttributeNameCollection attributes)
		{
			_attributes = attributes;
		}

		public string Name { get; set; }

		public AttributeExtensionCollection this[string attributeName]
		{
			get { return _attributes[attributeName]; }
		}

		private readonly AttributeNameCollection _attributes;
		public           AttributeNameCollection  Attributes
		{
			get { return _attributes; }
		}

		private static readonly MemberExtension _null = new MemberExtension(AttributeNameCollection.Null);
		public  static          MemberExtension  Null
		{
			get { return _null; }
		}
	}
}
