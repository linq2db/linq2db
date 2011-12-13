using System;
using System.Collections.Generic;

namespace LinqToDB.Reflection.Extension
{
	public class AttributeNameCollection : Dictionary<string,AttributeExtensionCollection>
	{
		public new AttributeExtensionCollection this[string attributeName]
		{
			get
			{
				if (this == _null)
					return AttributeExtensionCollection.Null;

				AttributeExtensionCollection ext;

				return TryGetValue(attributeName, out ext) ? ext : AttributeExtensionCollection.Null;
			}
		}

		public void Add(AttributeExtension attributeExtension)
		{
			if (this != _null)
			{
				// Add attribute.
				//
				AttributeExtensionCollection attr;

				if (!TryGetValue(attributeExtension.Name, out attr))
					Add(attributeExtension.Name, attr = new AttributeExtensionCollection());

				attr.Add(attributeExtension);

				/*
				// Convert value type.
				//
				bool isType = attributeExtension.Name.EndsWith(TypeExtension.AttrName.TypePostfix);

				if (isType)
				{
					string attrName = attributeExtension.Name.Substring(
						0, attributeExtension.Name.Length - 5);

					AttributeExtensionCollection ext =
						(AttributeExtensionCollection)_attributes[attrName];

					if (ext != null && ext.Count == 1)
						ext[0].Values.ChangeValueType(attributeExtension.Value.ToString());
				}
				else
				{
					string attrName = attributeExtension.Name + TypeExtension.AttrName.TypePostfix;

					AttributeExtensionCollection ext =
						(AttributeExtensionCollection)_attributes[attrName];

					if (ext != null && ext.Count == 1)
						attributeExtension.Values.ChangeValueType(ext.Value.ToString());
				}
				*/
			}
		}

		public void Add(string name, string value)
		{
			if (this != _null)
			{
				var attrName  = name;
				var valueName = string.Empty;
				var idx       = name.IndexOf(TypeExtension.ValueName.Delimiter);

				if (idx > 0)
				{
					valueName = name.Substring(idx + 1).TrimStart(TypeExtension.ValueName.Delimiter);
					attrName  = name.Substring(0, idx);
				}

				if (valueName.Length == 0)
					valueName = TypeExtension.ValueName.Value;
				else if (valueName == TypeExtension.ValueName.Type)
					valueName = TypeExtension.ValueName.ValueType;

				AttributeExtensionCollection ext;

				if (TryGetValue(attrName, out ext))
					ext[0].Values.Add(valueName, value);
				else
				{
					var attributeExtension = new AttributeExtension { Name = name };

					attributeExtension.Values.Add(valueName, value);

					Add(attributeExtension);
				}
			}
		}

		private static readonly AttributeNameCollection _null = new AttributeNameCollection();
		public  static          AttributeNameCollection  Null
		{
			get { return _null; }
		}
	}
}
