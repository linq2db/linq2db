using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

namespace LinqToDB.Reflection.Extension
{
	public class TypeExtension
	{
		#region Consts

		public static class NodeName
		{
			public const string Type        = "Type";
			public const string Member      = "Member";
			public const string Association = "Association";
			public const string Relation    = "Relation";
			public const string MasterIndex = "MasterIndex";
			public const string SlaveIndex  = "SlaveIndex";
		}

		public static class AttrName
		{
			public const string Name            = "Name";
			public const string DestinationType = "DestinationType";
		}

		public static class ValueName
		{
			public const char   Delimiter   = '-';
			public const string Value       = "Value";
			public const string Type        = "Type";
			public const string ValueType   = "Value-Type";
			public const string TypePostfix = "-Type";
		}

		#endregion

		#region Public Instance Members

		public TypeExtension()
			: this(new MemberExtensionCollection(), new AttributeNameCollection())
		{
		}

		private TypeExtension(
			MemberExtensionCollection  members,
			AttributeNameCollection    attributes)
		{
			_members    = members;
			_attributes = attributes;
		}

		public string Name { get; set; }

		public MemberExtension this[string memberName]
		{
			get { return _members[memberName]; }
		}

		private readonly MemberExtensionCollection _members;
		public           MemberExtensionCollection  Members
		{
			get { return _members; }
		}

		private readonly AttributeNameCollection _attributes;
		public           AttributeNameCollection  Attributes
		{
			get { return _attributes; }
		}

		private static readonly TypeExtension _null = new TypeExtension(MemberExtensionCollection.Null, AttributeNameCollection.Null);
		public           static TypeExtension  Null
		{
			get { return _null; }
		}

		#endregion

		#region Conversion

		public static bool ToBoolean(object value, bool defaultValue)
		{
			return value == null? defaultValue: ToBoolean(value);
		}

		public static bool ToBoolean(object value)
		{
			if (value != null)
			{
				if (value is bool)
					return (bool)value;

				var s = value as string;

				if (s != null)
				{
					if (s == "1")
						return true;

					s = s.ToLower();

					if (s == "true" || s == "yes" || s == "on")
						return true;
				}

				return Convert.ToBoolean(value);
			}

			return false;
		}

		public static object ChangeType(object value, Type type)
		{
			if (value == null || type == value.GetType())
				return value;

			if (type == typeof(string))
				return value.ToString();

			if (type == typeof(bool))
				return ToBoolean(value);

			if (type.IsEnum)
			{
				if (value is string)
					return Enum.Parse(type, value.ToString(), false);
			}

			return Convert.ChangeType(value, type, Thread.CurrentThread.CurrentCulture);
		}

		#endregion

		#region Public Static Members
		
		public static ExtensionList GetExtensions(string xmlFile)
		{
			return GetExtensions(xmlFile, Assembly.GetCallingAssembly());
		}

		public static ExtensionList GetExtensions(string xmlFile, Assembly assembly)
		{
			StreamReader streamReader = null;

			try
			{
				if (File.Exists(xmlFile))
				{
					streamReader = File.OpenText(xmlFile);
				}
#if !SILVERLIGHT
				else
				{
					var combinePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, xmlFile);

					if (File.Exists(combinePath))
						streamReader = File.OpenText(combinePath);
				}
#endif

				var embedded = streamReader == null;
				var stream   = embedded ? assembly.GetManifestResourceStream(xmlFile) : streamReader.BaseStream;

				if (embedded && stream == null)
				{
					var names = assembly.GetManifestResourceNames();

					// Prepend file anme with a dot to avoid partial name matching.
					//
					xmlFile = "." + xmlFile;

					foreach (var name in names)
					{
						if (name.EndsWith(xmlFile))
						{
							stream = assembly.GetManifestResourceStream(name);
							break;
						}
					}
				}

				if (stream == null)
					throw new TypeExtensionException(
						string.Format("Could not find file '{0}'.", xmlFile));

				using (stream)
					return GetExtensions(stream);
			} 
			finally
			{
				if (streamReader != null)
					streamReader.Close();
			}
		}

		public static ExtensionList GetExtensions(Stream xmlDocStream)
		{
			var doc = XDocument.Load(new StreamReader(xmlDocStream));

			return CreateTypeInfo(doc);
		}

		public static TypeExtension GetTypeExtension(Type type, ExtensionList typeExtensions)
		{
			var attrs = type.GetCustomAttributes(typeof(TypeExtensionAttribute), true);

			if (attrs != null && attrs.Length != 0)
			{
				var attr = (TypeExtensionAttribute)attrs[0];

				if (!string.IsNullOrEmpty(attr.FileName))
					typeExtensions = GetExtensions(attr.FileName, type.Assembly);

				if (typeExtensions != null && !string.IsNullOrEmpty(attr.TypeName))
					return typeExtensions[attr.TypeName];
			}

			return typeExtensions != null? typeExtensions[type]: Null;
		}

		#endregion

		#region Private Static Members

		private static ExtensionList CreateTypeInfo(XDocument doc)
		{
			var list = new ExtensionList();

			foreach (var typeNode in doc.Root.Elements().Where(_ => _.Name.LocalName == NodeName.Type))
				list.Add(ParseType(typeNode));

			return list;
		}

		private static TypeExtension ParseType(XElement typeNode)
		{
			var ext = new TypeExtension();

			foreach (var attr in typeNode.Attributes())
			{
				if (attr.Name.LocalName == AttrName.Name)
					ext.Name = attr.Value;
				else
					ext.Attributes.Add(attr.Name.LocalName, attr.Value);
			}

			foreach (var node in typeNode.Elements())
			{
				if (node.Name.LocalName == NodeName.Member)
					ext.Members.Add(ParseMember(node));
				else
					ext.Attributes.Add(ParseAttribute(node));
			}

			return ext;
		}

		private static MemberExtension ParseMember(XElement memberNode)
		{
			var ext = new MemberExtension();

			foreach (var attr in memberNode.Attributes())
			{
				if (attr.Name.LocalName == AttrName.Name)
					ext.Name = attr.Value;
				else
					ext.Attributes.Add(attr.Name.LocalName, attr.Value);
			}

			foreach (var node in memberNode.Elements())
				ext.Attributes.Add(ParseAttribute(node));

			return ext;
		}

		private static AttributeExtension ParseAttribute(XElement attributeNode)
		{
			var ext = new AttributeExtension
			{
				Name = attributeNode.Name.LocalName
			};

			ext.Values.Add(ValueName.Value, attributeNode.Value);

			foreach (var attr in attributeNode.Attributes())
			{
				if (attr.Name.LocalName == ValueName.Type)
					ext.Values.Add(ValueName.ValueType, attr.Value);
				else
					ext.Values.Add(attr.Name.LocalName, attr.Value);
			}

			foreach (var node in attributeNode.Elements())
				ext.Attributes.Add(ParseAttribute(node));

			return ext;
		}

		#endregion
	}
}
