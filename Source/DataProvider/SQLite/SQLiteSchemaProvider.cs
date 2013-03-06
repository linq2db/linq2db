using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Data;
	using SchemaProvider;

	class SQLiteSchemaProvider : SchemaProviderBase
	{
		public override DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (options == null)
				options = new GetSchemaOptions();

			var dbConnection = (DbConnection)dataConnection.Connection;
			var dataTypes    = dbConnection.GetSchema("DataTypes").AsEnumerable().ToList();

			#region Tables

			var tables =
			(
				from t in dbConnection.GetSchema("Tables").AsEnumerable()
				where t.Field<string>("TABLE_TYPE") != "SYSTEM_TABLE"
				let schema = t.Field<string>("TABLE_SCHEMA")
				let name   = t.Field<string>("TABLE_NAME")
				select new TableSchema
				{
					CatalogName     = t.Field<string>("TABLE_CATALOG"),
					SchemaName      = schema,
					TableName       = name,
					TypeName        = ToValidName(name),
					IsDefaultSchema = schema.IsNullOrEmpty(),
					Columns         = new List<ColumnSchema>(),
					ForeignKeys     = new List<ForeignKeySchema>(),
				}
			).ToList();
			
			#endregion

			#region PKs

			var pks =
			(
				from pk in dbConnection.GetSchema("IndexColumns").AsEnumerable()
				join idx in dbConnection.GetSchema("Indexes").AsEnumerable()
					on pk.Field<string>("CONSTRAINT_NAME") equals idx.Field<string>("INDEX_NAME")
				where idx.Field<bool>("PRIMARY_KEY")
				select new
				{
					id         = pk.Field<string>("TABLE_CATALOG") + "." + pk.Field<string>("TABLE_SCHEMA") + "." + pk.Field<string>("TABLE_NAME"),
					pkName     = pk.Field<string>("CONSTRAINT_NAME"),
					columnName = pk.Field<string>("COLUMN_NAME"),
					ordinal    = pk.Field<int>   ("ORDINAL_POSITION"),
				}
			).ToList();

			#endregion

			#region Columns

			var cols =
			(
				from c in dbConnection.GetSchema("Columns").AsEnumerable()
				let tschema = c.Field<string>("TABLE_SCHEMA")
				let schema  = tschema == "sqlite_default_schema" ? "" : tschema
				select new
				{
					id         = c.Field<string>("TABLE_CATALOG") + "." + schema + "." + c.Field<string>("TABLE_NAME"),
					name       = c.Field<string>("COLUMN_NAME"),
					isNullable = c.Field<bool>  ("IS_NULLABLE"),
					ordinal    = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					dataType   = c.Field<string>("DATA_TYPE"),
					length     = Converter.ChangeTypeTo<int>(c["CHARACTER_MAXIMUM_LENGTH"]),
					prec       = Converter.ChangeTypeTo<int>(c["NUMERIC_PRECISION"]),
					scale      = Converter.ChangeTypeTo<int>(c["NUMERIC_SCALE"]),
					isIdentity = c.Field<bool>  ("AUTOINCREMENT"),
				}
			).ToList();

			var columns =
				from c  in cols

				join dt in dataTypes
					on c.dataType equals dt.Field<string>("TypeName") into g1
				from dt in g1.DefaultIfEmpty()

				join pk in pks
					on c.id + "." + c.name equals pk.id + "." + pk.columnName into g2
				from pk in g2.DefaultIfEmpty()

				join t  in tables on c.id equals t.CatalogName + "." + t.SchemaName + "." + t.TableName

				orderby c.ordinal
				select new { t, c, dt, pk };

			foreach (var column in columns)
			{
				var columnType = column.c.dataType;
				var systemType = GetSystemType(columnType, column.dt);
				var isNullable = column.c.isNullable;

				var skipOnInsert = false;
				var skipOnUpdate = false;

				switch (columnType)
				{
					case "timestamp" : skipOnInsert = skipOnUpdate = true; break;
				}

				column.t.Columns.Add(new ColumnSchema
				{
					Table           = column.t,
					ColumnName      = column.c.name,
					ColumnType      = GetDbType(columnType, column.dt, column.c.length, column.c.prec, column.c.scale),
					IsNullable      = isNullable,
					MemberName      = ToValidName(column.c.name),
					MemberType      = ToTypeName(systemType, isNullable),
					SystemType      = systemType ?? typeof(object),
					DataType        = GetDataType(columnType),
					SkipOnInsert    = skipOnInsert || column.c.isIdentity,
					SkipOnUpdate    = skipOnUpdate || column.c.isIdentity,
					IsPrimaryKey    = column.pk != null,
					PrimaryKeyOrder = column.pk != null ? column.pk.ordinal : -1,
					IsIdentity      = column.c.isIdentity,
				});
			}

			#endregion

			#region FK

			var fks =
			(
				from fk in dbConnection.GetSchema("ForeignKeys").AsEnumerable()
				where fk.Field<string>("CONSTRAINT_TYPE") == "FOREIGN_KEY"
				select new
				{
					name        = fk.Field<string>("CONSTRAINT_NAME"),
					thisTable   = fk.Field<string>("TABLE_CATALOG")   + "." + fk.Field<string>("TABLE_SCHEMA")   + "." + fk.Field<string>("TABLE_NAME"),
					thisColumn  = fk.Field<string>("FKEY_FROM_COLUMN"),
					otherTable  = fk.Field<string>("FKEY_TO_CATALOG") + "." + fk.Field<string>("FKEY_TO_SCHEMA") + "." + fk.Field<string>("FKEY_TO_NAME"),
					otherColumn = fk.Field<string>("FKEY_TO_COLUMN"),
					ordinal     = fk.Field<string>("FKEY_FROM_ORDINAL_POSITION"),
				}
			).ToList();

			foreach (var fk in fks)
			{
				var thisTable   = (from t in tables             where t.ID         == fk.thisTable   select t).Single();
				var otherTable  = (from t in tables             where t.ID         == fk.otherTable  select t).Single();
				var thisColumn  = (from c in thisTable. Columns where c.ColumnName == fk.thisColumn  select c).Single();
				var otherColumn = (from c in otherTable.Columns where c.ColumnName == fk.otherColumn select c).Single();

				var key = thisTable.ForeignKeys.FirstOrDefault(f => f.KeyName == fk.name);

				if (key == null)
				{
					key = new ForeignKeySchema
					{
						KeyName      = fk.name,
						MemberName   = fk.name,
						ThisTable    = thisTable,
						OtherTable   = otherTable,
						ThisColumns  = new List<ColumnSchema>(),
						OtherColumns = new List<ColumnSchema>(),
						CanBeNull    = true,
					};
					thisTable.ForeignKeys.Add(key);
				}

				key.ThisColumns. Add(thisColumn);
				key.OtherColumns.Add(otherColumn);
			}

			#endregion

			return ProcessSchema(new DatabaseSchema
			{
				DataSource    = dbConnection.DataSource,
				Database      = dbConnection.Database,
				ServerVersion = dbConnection.ServerVersion,
				Tables        = tables,
				Procedures    = new List<ProcedureSchema>(),
			});
		}

		static string GetDbType(string columnType, DataRow dataType, int length, int prec, int scale)
		{
			var dbType = columnType;

			if (dataType != null)
			{
				var format = dataType.Field<string>("CreateFormat");
				var parms  = dataType.Field<string>("CreateParameters");

				if (!string.IsNullOrWhiteSpace(format) && !string.IsNullOrWhiteSpace(parms))
				{
					var paramNames  = parms.Split(',');
					var paramValues = new object[paramNames.Length];

					for (var i = 0; i < paramNames.Length; i++)
					{
						switch (paramNames[i].Trim())
						{
							case "length"     :
							case "max length" : paramValues[i] = length; break;
							case "precision"  : paramValues[i] = prec;   break;
							case "scale"      : paramValues[i] = scale;  break;
						}
					}

					if (paramValues.All(v => v != null))
						dbType = format.Args(paramValues);
				}
			}

			return dbType;
		}

		static DataType GetDataType(string columnType)
		{
			switch (columnType)
			{
				case "smallint"         : return DataType.Int16;
				case "int"              : return DataType.Int32;
				case "real"             : return DataType.Single;
				case "float"            : return DataType.Double;
				case "double"           : return DataType.Double;
				case "money"            : return DataType.Money;
				case "currency"         : return DataType.Money;
				case "decimal"          : return DataType.Decimal;
				case "numeric"          : return DataType.Decimal;
				case "bit"              : return DataType.Boolean;
				case "yesno"            : return DataType.Boolean;
				case "logical"          : return DataType.Boolean;
				case "bool"             : return DataType.Boolean;
				case "boolean"          : return DataType.Boolean;
				case "tinyint"          : return DataType.Byte;
				case "integer"          : return DataType.Int64;
				case "counter"          : return DataType.Int64;
				case "autoincrement"    : return DataType.Int64;
				case "identity"         : return DataType.Int64;
				case "long"             : return DataType.Int64;
				case "bigint"           : return DataType.Int64;
				case "binary"           : return DataType.Binary;
				case "varbinary"        : return DataType.VarBinary;
				case "blob"             : return DataType.VarBinary;
				case "image"            : return DataType.Image;
				case "general"          : return DataType.VarBinary;
				case "oleobject"        : return DataType.VarBinary;
				case "varchar"          : return DataType.VarChar;
				case "nvarchar"         : return DataType.NVarChar;
				case "memo"             : return DataType.Text;
				case "longtext"         : return DataType.Text;
				case "note"             : return DataType.Text;
				case "text"             : return DataType.Text;
				case "ntext"            : return DataType.NText;
				case "string"           : return DataType.Char;
				case "char"             : return DataType.Char;
				case "nchar"            : return DataType.NChar;
				case "datetime"         : return DataType.DateTime;
				case "datetime2"        : return DataType.DateTime2;
				case "smalldate"        : return DataType.SmallDateTime;
				case "timestamp"        : return DataType.Timestamp;
				case "date"             : return DataType.Date;
				case "time"             : return DataType.Time;
				case "uniqueidentifier" : return DataType.Guid;
				case "guid"             : return DataType.Guid;
			}

			return DataType.Undefined;
		}

		static Type GetSystemType(string columnType, DataRow dataType)
		{
			Type systemType = null;

			if (dataType != null)
				systemType = Type.GetType(dataType.Field<string>("DataType"));

			if (systemType != null)
				return systemType;

			switch (columnType)
			{
				case "datetime2" : return typeof(DateTime);
			}

			return null;
		}
	}
}
