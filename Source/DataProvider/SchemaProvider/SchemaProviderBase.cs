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
				case "SByte"   : memberType = "sbyte";   break;
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

		protected virtual DatabaseSchema ProcessSchema(DatabaseSchema databaseSchema)
		{
			foreach (var t in databaseSchema.Tables)
			{
				foreach (var key in t.ForeignKeys.ToList())
				{
					if (!key.KeyName.EndsWith("_BackReference"))
					{
						key.OtherTable.ForeignKeys.Add(
							key.BackReference = new ForeignKeySchema
							{
								KeyName         = key.KeyName    + "_BackReference",
								MemberName      = key.MemberName + "_BackReference",
								AssociationType = AssociationType.Auto,
								OtherTable      = t,
								ThisColumns     = key.OtherColumns,
								OtherColumns    = key.ThisColumns,
							});
					}
				}
			}

			foreach (var t in databaseSchema.Tables)
			{
				foreach (var key in t.ForeignKeys)
				{
					if (key.BackReference != null && key.AssociationType == AssociationType.Auto)
					{
						if (key.ThisColumns.All(_ => _.IsPrimaryKey))
						{
							if (t.Columns.Count(_ => _.IsPrimaryKey) == key.ThisColumns.Count)
								key.AssociationType = AssociationType.OneToOne;
							else
								key.AssociationType = AssociationType.ManyToOne;
						}
						else
							key.AssociationType = AssociationType.ManyToOne;

						key.CanBeNull = key.ThisColumns.All(_ => _.IsNullable);
					}
				}

				foreach (var key in t.ForeignKeys)
				{
					var name = key.MemberName;

					if (key.BackReference != null && key.ThisColumns.Count == 1 && key.ThisColumns[0].MemberName.ToLower().EndsWith("id"))
					{
						name = key.ThisColumns[0].MemberName;
						name = name.Substring(0, name.Length - "id".Length);

						if (t.ForeignKeys.Select(_ => _.MemberName). Concat(
							t.Columns.    Select(_ => _.MemberName)).Concat(
							new[] { t.TypeName }).All(_ => _ != name))
						{
							name = key.MemberName;
						}
					}
			
					if (name == key.MemberName)
					{
						if (name.StartsWith("FK_"))
							name = name.Substring(3);

						if (name.EndsWith("_BackReference"))
							name = name.Substring(0, name.Length - "_BackReference".Length);

						name = string.Join("", name.Split('_').Where(_ => _.Length > 0 && _ != t.TableName).ToArray());
					}

					if (name.Length != 0 &&
						t.ForeignKeys.Select(_ => _.MemberName).Concat(
						t.Columns.    Select(_ => _.MemberName)).Concat(
							new[] { t.TypeName }).All(_ => _ != name))
					{
						key.MemberName = name;
					}
				}
			}

			return databaseSchema;
		}
	}
}
