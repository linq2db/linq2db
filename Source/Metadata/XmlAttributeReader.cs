using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using JetBrains.Annotations;

namespace LinqToDB.Metadata
{
	using Common;

	public class XmlAttributeReader : IMetadataReader
	{
		readonly Dictionary<string,MetaTypeInfo> _types;

		public XmlAttributeReader(string xmlFile)
			: this(xmlFile, Assembly.GetCallingAssembly())
		{
		}

		public XmlAttributeReader([NotNull] string xmlFile, [NotNull] Assembly assembly)
		{
			if (xmlFile  == null) throw new ArgumentNullException("xmlFile");
			if (assembly == null) throw new ArgumentNullException("assembly");

			StreamReader streamReader = null;

			try
			{
#if !SILVERLIGHT
				if (File.Exists(xmlFile))
				{
					streamReader = File.OpenText(xmlFile);
				}
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
					var name  = names.FirstOrDefault(n => n.EndsWith("." + xmlFile));

					stream = name != null ? assembly.GetManifestResourceStream(name) : null;
				}

				if (stream == null)
					throw new MetadataException("Could not find file '{0}'.".Args(xmlFile));
				else
					using (stream)
						_types = LoadStream(stream, xmlFile);
			}
			finally
			{
				if (streamReader != null)
					streamReader.Close();
			}
		}

		public XmlAttributeReader([NotNull] Stream xmlDocStream)
		{
			if (xmlDocStream == null) throw new ArgumentNullException("xmlDocStream");

			_types = LoadStream(xmlDocStream, "");
		}

		static AttributeInfo[] GetAttrs(string fileName, XElement el, string exclude, string typeName, string memberName)
		{
			var attrs = el.Elements().Where(e => e.Name.LocalName != exclude).Select(a =>
			{
				var aname  = a.Name.LocalName;
				var values = a.Elements().Select(e =>
				{
					var name  = e.Name.LocalName;
					var value = e.Attribute("Value");
					var type  = e.Attribute("Type");

					if (value == null)
						throw new MetadataException(
							memberName != null ?
								string.Format(
									"'{0}': Element <Type Name='{1}'><Member Name='{2}'><'{3}'><{4} /> has to have 'Value' attribute.",
									fileName, typeName, memberName, aname, name) :
								string.Format(
									"'{0}': Element <Type Name='{1}'><'{2}'><{3} /> has to have 'Value' attribute.",
									fileName, typeName, aname, name));

					var val =
						type != null ?
							Converter.ChangeType(value.Value, Type.GetType(type.Value, true)) :
							value.Value;

					return Tuple.Create(name, val);
				});

				return new AttributeInfo(aname, values.ToDictionary(v => v.Item1, v => v.Item2));
			});

			return attrs.ToArray();
		}

		static Dictionary<string,MetaTypeInfo> LoadStream(Stream xmlDocStream, string fileName)
		{
			var doc = XDocument.Load(new StreamReader(xmlDocStream));

			return doc.Root.Elements().Where(e => e.Name.LocalName == "Type").Select(t =>
			{
				var aname = t.Attribute("Name");

				if (aname == null)
					throw new MetadataException("'{0}': Element 'Type' has to have 'Name' attribute.".Args(fileName));

				var tname = aname.Value;

				var members = t.Elements().Where(e => e.Name.LocalName == "Member").Select(m =>
				{
					var maname = m.Attribute("Name");

					if (maname == null)
						throw new MetadataException(string.Format(
							"'{0}': Element <Type Name='{1}'><Member /> has to have 'Name' attribute.",
							fileName, tname));

					var mname = maname.Value;

					return new MetaMemberInfo(mname, GetAttrs(fileName, m, null, tname, mname));
				});

				return new MetaTypeInfo(tname, members.ToDictionary(m => m.Name), GetAttrs(fileName, t, "Member", tname, null));
			})
			.ToDictionary(t => t.Name);
		}

		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			MetaTypeInfo t;

			if (_types.TryGetValue(type.FullName, out t) || _types.TryGetValue(type.Name, out t))
				return t.GetAttribute(typeof(T)).Select(a => (T) a.MakeAttribute(typeof(T))).ToArray();

			return new T[0];
		}

		public T[] GetAttributes<T>(MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			var type = memberInfo.DeclaringType;

			MetaTypeInfo t;

			if (_types.TryGetValue(type.FullName, out t) || _types.TryGetValue(type.Name, out t))
			{
				MetaMemberInfo m;

				if (t.Members.TryGetValue(memberInfo.Name, out m))
				{
					return m.GetAttribute(typeof(T)).Select(a => (T)a.MakeAttribute(typeof(T))).ToArray();
				}
			}

			return new T[0];
		}
	}
}
