using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using LinqToDB.Naming;
using LinqToDB.SqlQuery;

namespace LinqToDB.Scaffold.Internal
{
	/// <summary>
	/// Internal API.
	/// </summary>
	public static class NameGenerationServices
	{
		public static string GenerateAssociationName(
			Func<SqlObjectName, string, bool> isPrimaryKeyColumn,
			SqlObjectName                     thisTable,
			SqlObjectName                     otherTable,
			bool                              isBackReference,
			string[]                          thisColumns,
			string                            fkName,
			NameTransformation                transformationSettings,
			ISet<string>                      defaultSchemas)
		{
			var name = otherTable.Name;

			// T4 compatibility mode use logic, similar to one, used by old T4 templates
			if (!isBackReference && transformationSettings == NameTransformation.Association)
			{
				var newName = fkName;

				// FK side of one-to-one relation by primary keys
				var isOneToOne = !isBackReference && thisColumns.All(c => isPrimaryKeyColumn(thisTable, c));

				// if column name provided - generate association name based on column name
				if (!isOneToOne && thisColumns.Length == 1 && thisColumns[0].ToLowerInvariant().EndsWith("id"))
				{
					// if column name provided and ends with ID suffix
					// we trim ID part and possible _ connectors before it
					newName = thisColumns[0];
					newName = newName.Substring(0, newName.Length - "id".Length).TrimEnd('_');
					// here name could become empty if column name was ID
				}
				else
				{
					// if column name not provided - use FK name for association name

					// remove FK_ prefix
					if (newName.StartsWith("FK_"))
						newName = newName.Substring(3);

					// - split name into words using _ as separator
					// - remove words that match target table name, schema or any of default schema
					// - concat remaining words back into single name
					newName = string.Concat(newName
						.Split('_')
						.Where(_ =>
							_.Length > 0 && _ != thisTable.Name &&
							(thisTable.Schema == null || defaultSchemas.Contains(thisTable.Schema) || _ != thisTable.Schema)));

					// remove trailing digits
					// note that new implementation match all digits, not just 0-9 as it was in T4
					var skip = true;
					newName = string.Concat(newName.EnumerateCharacters().Reverse().Select(_ =>
					{
						if (skip)
						{
							if (_.category == UnicodeCategory.DecimalDigitNumber)
								return string.Empty;
							else
								skip = false;
						}

						return _.codePoint;
					}).Reverse());
				}

				if (string.IsNullOrEmpty(newName))
					newName = thisTable == otherTable ? thisTable.Name : fkName;

				name = newName;
			}

			return name;
		}
	}
}
