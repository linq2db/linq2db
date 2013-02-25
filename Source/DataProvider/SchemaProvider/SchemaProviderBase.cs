using System;
using System.Linq;

namespace LinqToDB.DataProvider.SchemaProvider
{
	using Data;

	public abstract class SchemaProviderBase : ISchemaProvider
	{
		public abstract DatabaseSchema GetSchema(DataConnection dataConnection);

		protected string ToValidName(string name)
		{
			if (name.Contains(" "))
			{
				var ss = name.Split(' ')
					.Where (s => s.Trim().Length > 0)
					.Select(s => char.ToUpper(s[0]) + s.Substring(1));
				return string.Join("", ss.ToArray());
			}

			return name;
		}

		protected string ToTypeName(Type type, bool isNullable)
		{
			if (type == null)
				type = typeof(object);

			var memberType = type.Name;

			switch (memberType)
			{
				case "Byte"    : memberType = "byte";    break;
				case "Byte[]"  : memberType = "byte[]";  break;
				case "Int16"   : memberType = "short";   break;
				case "Int32"   : memberType = "int";     break;
				case "Int64"   : memberType = "long";    break;
				case "Decimal" : memberType = "decimal"; break;
				case "Single"  : memberType = "float";   break;
				case "Double"  : memberType = "double";  break;
				case "String"  : memberType = "string";  break;
				case "Object"  : memberType = "object";  break;
			}

			if (!type.IsClass && isNullable)
				memberType += "?";

			return memberType;
		}
	}
}
