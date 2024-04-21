using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace LinqToDB.Metadata
{
	using Common;
	using Mapping;

	/*
	 * Side-notes after logic reveng:
	 * 1. Provider works only with attributes with default constructors which we don't take into account when define new mapping attributes
	 * 2. Type of attribute property value in theory could be removed
	 */
	/// <summary>
	/// Metadata atributes provider. Uses XML document to describe attributes and their bindings.
	/// </summary>
	/// <example>
	/// XML format:
	/// <![CDATA[
	/// <[Root]>
	///     <Type Name="<Type.FullName|Type.Name>">
	///         <Member Name="<Name>">
	///             [<Attribute />]*
	///         </Member>
	///         [<Attribute />]*
	///     </Type>
	/// </[Root]>
	/// ]]>
	///
	/// <list type="number">
	/// <item>Root node name could be any</item>
	/// <item>Type nodes define entity classes with entity type name specified in Name attribute (must be <c>typeof(Entity).FullName</c> or <c>typeof(Entity).Name</c> value)</item>
	/// <item>Member nodes define entity class members with mapping attributes with member name specified in Name attribute</item>
	/// <item>Attr nodes define attributes of entity (if nested into Type node) or entity member (when nested into Member node)</item>
	/// </list>
	///
	/// Attribute node format:
	/// <![CDATA[
	/// <[AttributeTypeName]>
	///   <[PropertyName] Value="<serialized_value>" Type="<Type.FullName|Type.Name>" />
	/// </[AttributeTypeName]>
	/// ]]>
	///
	/// <list type="number">
	/// <item>Node name is a mapping attribute type name as <c>Type.FullName</c>, <c>Type.Name</c> or <c>Type.Name</c> without <c>"Attribute"</c> suffix</item>
	/// <item>PropertyName node name is a name of attribute property you want to set</item>
	/// <item>Type attribute specify value type as <c>Type.FullName</c> or <c>Type.Name</c> string and specified only for non-string properties</item>
	/// <item>Value contains attribute property value, serialized as string, understandable by <see cref="Converter.ChangeType(object?, Type, Mapping.MappingSchema?, ConversionType)"/> method</item>
	/// </list>
	/// </example>
	public class XmlAttributeReader : IMetadataReader
	{
		private readonly string _objectId;

		readonly Dictionary<string,MetaTypeInfo> _types;

		static readonly IReadOnlyDictionary<string,Type> _mappingAttributes;

		static XmlAttributeReader()
		{
			var baseType = typeof(MappingAttribute);
			var lookup   = new Dictionary<string,Type>();

			// use of mapping attributes, defined only in linq2db assembly could look like
			// regression if user use XML provider with custom mapping attribute type, derived from one of our mapping attributes
			// but custom attributes/inheritance was never supported for this provider
			foreach (var type in typeof(XmlAttributeReader).Assembly.GetTypes())
			{
				if (baseType.IsAssignableFrom(type))
				{
					lookup[type.FullName!] = type;
					lookup[type.Name]      = type;

					if (type.Name.EndsWith("Attribute"))
						lookup[type.Name.Substring(0, type.Name.Length - 9)] = type;
				}
			}

			_mappingAttributes = lookup;
		}

		/// <summary>
		/// Creates metadata provider instance.
		/// </summary>
		/// <param name="xmlFile">
		/// Parameter could reference to (probed in specified order):
		/// <list type="bullet">
		/// <item>full or relative path to XML file resolved against current directory</item>
		/// <item>full or relative path to XML file resolved against application's app domain base directory (<see cref="AppDomain.BaseDirectory"/>)</item>
		/// <item>name of resource with XML document, resolved using <see cref="Assembly.GetManifestResourceStream(string)" /> method</item>
		/// <item>name of resource suffix (<c>.{<paramref name="xmlFile"/>}</c>) for resource with XML document, resolved using <see cref="Assembly.GetManifestResourceStream(string)" /> method</item>
		/// </list>
		/// Resource search performed in calling assembly.
		/// </param>
		public XmlAttributeReader(string xmlFile)
			: this(xmlFile, Assembly.GetCallingAssembly())
		{
		}

		/// <summary>
		/// Creates metadata provider instance.
		/// </summary>
		/// <param name="xmlFile">
		/// Parameter could reference to (probed in specified order):
		/// <list type="bullet">
		/// <item>full or relative path to XML file resolved against current directory</item>
		/// <item>full or relative path to XML file resolved against application's app domain base directory (<see cref="AppDomain.BaseDirectory"/>)</item>
		/// <item>name of resource with XML document, resolved using <see cref="Assembly.GetManifestResourceStream(string)" /> method</item>
		/// <item>name of resource suffix (<c>.{<paramref name="xmlFile"/>}</c>) for resource with XML document, resolved using <see cref="Assembly.GetManifestResourceStream(string)" /> method</item>
		/// </list>
		/// </param>
		/// <param name="assembly">
		/// Assembly to load resource from for two last options from <paramref name="xmlFile"/> parameter.
		/// </param>
		public XmlAttributeReader(string xmlFile, Assembly assembly)
		{
			if (xmlFile  == null) throw new ArgumentNullException(nameof(xmlFile));
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));

			StreamReader? streamReader = null;
			Stream?       stream       = null;
			var objectID               = xmlFile;
			try
			{
				if (File.Exists(xmlFile))
				{
					streamReader = File.OpenText(xmlFile);
					objectID    += "." + Directory.GetCurrentDirectory();
				}
				else
				{
					var combinePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, xmlFile);

					if (File.Exists(combinePath))
					{
						streamReader = File.OpenText(combinePath);
						objectID    += "." + combinePath;
					}
				}

				var embedded   = streamReader == null;
				if (embedded)
				{
					stream = assembly.GetManifestResourceStream(xmlFile);
				}
				else
				{
					stream = streamReader!.BaseStream;
				}

				if (embedded && stream == null)
				{
					var names = assembly.GetManifestResourceNames();
					var name  = names.FirstOrDefault(n => n.EndsWith("." + xmlFile));

					stream = name != null ? assembly.GetManifestResourceStream(name) : null;
				}

				if (stream == null)
					throw new MetadataException($"Could not find file '{xmlFile}'.");

				_types    = LoadStream(stream, xmlFile);
				objectID += "." + assembly.FullName;
			}
			finally
			{
				stream      ?.Dispose();
				streamReader?.Dispose();
			}

			_objectId = objectID;
		}

		/// <summary>
		/// Creates metadata provider instance.
		/// </summary>
		/// <param name="xmlDocStream">Stream with XML document.</param>
		public XmlAttributeReader(Stream xmlDocStream)
		{
			if (xmlDocStream == null) throw new ArgumentNullException(nameof(xmlDocStream));

			_types    = LoadStream(xmlDocStream, null);
			_objectId = xmlDocStream.GetHashCode().ToString(NumberFormatInfo.InvariantInfo);
		}

		static AttributeInfo[] GetAttrs(string? fileName, XElement el, string? exclude, string typeName, string? memberName)
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
							memberName != null
								? $"'{fileName}': Element <Type Name='{typeName}'><Member Name='{memberName}'><'{aname}'></{name}> has to have 'Value' attribute."
								: $"'{fileName}': Element <Type Name='{typeName}'><'{aname}'></{name}> has to have 'Value' attribute.");

					var val =
						type != null ?
							Converter.ChangeType(value.Value, Type.GetType(type.Value, true)!) :
							value.Value;

					return (name, val);
				});

				if (!_mappingAttributes.TryGetValue(aname, out var atype))
					return null;//throw new MetadataException($"Unknown mapping attribute type name in XML metadata: '{aname}'");

				return new AttributeInfo(atype, values.ToDictionary(v => v.name, v => v.val));
			});

			return attrs.Where(_ => _ != null).ToArray()!;
		}

		static Dictionary<string,MetaTypeInfo> LoadStream(Stream xmlDocStream, string? fileName)
		{
			using var sr = new StreamReader(xmlDocStream);
			var doc      = XDocument.Load(sr);

			if (doc.Root == null)
				throw new MetadataException($"'{fileName}': Root element missing.");

			return doc.Root.Elements().Where(e => e.Name.LocalName == "Type").Select(t =>
			{
				var aname = t.Attribute("Name");

				if (aname == null)
					throw new MetadataException($"'{fileName}': Element 'Type' has to have 'Name' attribute.");

				var tname = aname.Value;

				var members = t.Elements().Where(e => e.Name.LocalName == "Member").Select(m =>
				{
					var maname = m.Attribute("Name");

					if (maname == null)
						throw new MetadataException($"'{fileName}': Element <Type Name='{tname}'><Member /> has to have 'Name' attribute.");

					var mname = maname.Value;

					return new MetaMemberInfo(mname, GetAttrs(fileName, m, null, tname, mname));
				});

				return new MetaTypeInfo(tname, members.ToDictionary(m => m.Name), GetAttrs(fileName, t, "Member", tname, null));
			})
			.ToDictionary(t => t.Name);
		}

		public MappingAttribute[] GetAttributes(Type type)
		{
			if ((_types.TryGetValue(type.FullName!, out var t) || _types.TryGetValue(type.Name, out t)) && t.Attributes.Length > 0)
				return t.Attributes.Select(a => a.MakeAttribute()).ToArray();

			return [];
		}

		public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
		{
			if (_types.TryGetValue(type.FullName!, out var t) || _types.TryGetValue(type.Name, out t))
			{
				if (t.Members.TryGetValue(memberInfo.Name, out var m) && m.Attributes.Length > 0)
					return m.Attributes.Select(a => a.MakeAttribute()).ToArray();
			}

			return [];
		}

		/// <inheritdoc cref="IMetadataReader.GetDynamicColumns"/>
		public MemberInfo[] GetDynamicColumns(Type type) => [];

		public string GetObjectID() => _objectId;
	}
}
