using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class FirebirdSchemaProvider : SchemaProviderBase
	{
		private readonly FirebirdDataProvider _provider;

		public FirebirdSchemaProvider(FirebirdDataProvider provider)
		{
			_provider = provider;
		}

		#region rdb$ system-table mappings

		[Table("RDB$RELATIONS")]
		sealed class RdbRelation
		{
			[Column("RDB$RELATION_NAME")] public string  RelationName { get; set; } = null!;
			[Column("RDB$SYSTEM_FLAG")  ] public short?  SystemFlag   { get; set; }
			[Column("RDB$RELATION_TYPE")] public short?  RelationType { get; set; }
			[Column("RDB$OWNER_NAME")   ] public string? OwnerName    { get; set; }
			[Column("RDB$DESCRIPTION")  ] public string? Description  { get; set; }
			[Column("RDB$SCHEMA_NAME")  ] public string? SchemaName   { get; set; } // Firebird 6+
		}

		[Table("RDB$RELATION_FIELDS")]
		sealed class RdbRelationField
		{
			[Column("RDB$RELATION_NAME")] public string  RelationName { get; set; } = null!;
			[Column("RDB$FIELD_NAME")   ] public string  FieldName    { get; set; } = null!;
			[Column("RDB$FIELD_SOURCE") ] public string  FieldSource  { get; set; } = null!;
			[Column("RDB$FIELD_POSITION")] public short? FieldPosition { get; set; }
			[Column("RDB$NULL_FLAG")    ] public short?  NullFlag     { get; set; }
			[Column("RDB$DESCRIPTION")  ] public string? Description  { get; set; }
			[Column("RDB$SCHEMA_NAME")  ] public string? SchemaName   { get; set; } // Firebird 6+
		}

		[Table("RDB$FIELDS")]
		sealed class RdbField
		{
			[Column("RDB$FIELD_NAME")     ] public string FieldName       { get; set; } = null!;
			[Column("RDB$FIELD_TYPE")     ] public short? FieldType       { get; set; }
			[Column("RDB$FIELD_SUB_TYPE") ] public short? FieldSubType    { get; set; }
			[Column("RDB$CHARACTER_LENGTH")] public short? CharacterLength { get; set; }
			[Column("RDB$FIELD_PRECISION")] public short? FieldPrecision  { get; set; }
			[Column("RDB$FIELD_SCALE")    ] public short? FieldScale      { get; set; }
			[Column("RDB$COMPUTED_SOURCE")] public string? ComputedSource { get; set; }
		}

		[Table("RDB$RELATION_CONSTRAINTS")]
		sealed class RdbRelationConstraint
		{
			[Column("RDB$CONSTRAINT_NAME")] public string  ConstraintName { get; set; } = null!;
			[Column("RDB$CONSTRAINT_TYPE")] public string? ConstraintType { get; set; }
			[Column("RDB$RELATION_NAME")  ] public string  RelationName   { get; set; } = null!;
			[Column("RDB$INDEX_NAME")     ] public string? IndexName      { get; set; }
			[Column("RDB$SCHEMA_NAME")    ] public string? SchemaName     { get; set; } // Firebird 6+
		}

		[Table("RDB$INDEX_SEGMENTS")]
		sealed class RdbIndexSegment
		{
			[Column("RDB$INDEX_NAME")    ] public string IndexName     { get; set; } = null!;
			[Column("RDB$FIELD_NAME")    ] public string FieldName     { get; set; } = null!;
			[Column("RDB$FIELD_POSITION")] public short? FieldPosition { get; set; }
		}

		[Table("RDB$REF_CONSTRAINTS")]
		sealed class RdbRefConstraint
		{
			[Column("RDB$CONSTRAINT_NAME")] public string  ConstraintName { get; set; } = null!;
			[Column("RDB$CONST_NAME_UQ")  ] public string? ConstNameUq    { get; set; }
		}

		#endregion

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			return Path.GetFileNameWithoutExtension(base.GetDatabaseName(dbConnection));
		}

		// Firebird 6 introduces SQL-standard schemas (RDB$SCHEMA_NAME); pre-FB6 has a flat namespace and
		// reports the object owner as the schema (preserving the prior GetSchema-based behaviour).
		bool SupportsSchemas => _provider.Version >= FirebirdVersion.v6;

		// Correlation key shared by GetTables/GetColumns/GetPrimaryKeys/GetForeignKeys; the schema part is
		// only meaningful on FB6 (null otherwise), matching the flat-namespace TableID used previously.
		static string MakeTableID(string? schema, string name) => (schema ?? string.Empty) + ".." + name;

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var hasSchemas = SupportsSchemas;

			var rows = dataConnection.GetTable<RdbRelation>()
				.Where(r => r.SystemFlag == null || r.SystemFlag == 0)
				.Select(r => new
				{
					r.RelationName,
					r.RelationType,
					r.OwnerName,
					r.Description,
					SchemaName = hasSchemas ? r.SchemaName : null,
				})
				.ToList();

			return rows.Select(r =>
			{
				var name   = r.RelationName.TrimEnd();
				var schema = (hasSchemas ? r.SchemaName : r.OwnerName)?.TrimEnd();

				return new TableInfo()
				{
					TableID         = MakeTableID(hasSchemas ? schema : null, name),
					CatalogName     = null,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = string.Equals(schema, hasSchemas ? "PUBLIC" : "SYSDBA", StringComparison.Ordinal),
					IsView          = r.RelationType == 1,
					Description     = r.Description?.TrimEnd(),
				};
			}).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var hasSchemas = SupportsSchemas;

			var rows =
				(
					from rc in dataConnection.GetTable<RdbRelationConstraint>()
					where rc.ConstraintType == "PRIMARY KEY"
					join seg in dataConnection.GetTable<RdbIndexSegment>() on rc.IndexName equals seg.IndexName
					select new
					{
						rc.RelationName,
						rc.ConstraintName,
						SchemaName = hasSchemas ? rc.SchemaName : null,
						seg.FieldName,
						seg.FieldPosition,
					}
				).ToList();

			return rows.Select(r => new PrimaryKeyInfo()
			{
				TableID        = MakeTableID(r.SchemaName?.TrimEnd(), r.RelationName.TrimEnd()),
				PrimaryKeyName = r.ConstraintName.TrimEnd(),
				ColumnName     = r.FieldName.TrimEnd(),
				Ordinal        = r.FieldPosition ?? 0,
			}).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var hasSchemas = SupportsSchemas;

			var rows =
				(
					from rf in dataConnection.GetTable<RdbRelationField>()
					join f in dataConnection.GetTable<RdbField>() on rf.FieldSource equals f.FieldName
					select new
					{
						rf.RelationName,
						rf.FieldName,
						rf.FieldPosition,
						rf.NullFlag,
						rf.Description,
						SchemaName = hasSchemas ? rf.SchemaName : null,
						f.FieldType,
						f.FieldSubType,
						f.CharacterLength,
						f.FieldPrecision,
						f.FieldScale,
						f.ComputedSource,
					}
				).ToList();

			return rows.Select(r =>
			{
				var typeName  = CreateTypeName(r.FieldType ?? 0, r.FieldSubType ?? 0, r.FieldScale ?? 0).ToLowerInvariant();
				var dt        = GetDataType(typeName, null, options);
				var precision = (int?)r.FieldPrecision;
				var computed  = r.ComputedSource != null;

				return new ColumnInfo()
				{
					TableID      = MakeTableID(r.SchemaName?.TrimEnd(), r.RelationName.TrimEnd()),
					Name         = r.FieldName.TrimEnd(),
					DataType     = dt?.TypeName,
					IsNullable   = r.NullFlag != 1,
					Ordinal      = r.FieldPosition ?? 0,
					Length       = (typeName is "char" or "varchar") ? (int?)r.CharacterLength : null,
					Precision    = precision is null or 0 ? null : precision,
					Scale        = (typeName is "decimal" or "numeric") && r.FieldScale.HasValue ? Math.Abs((int)r.FieldScale.Value) : null,
					IsIdentity   = false,
					Description  = r.Description?.TrimEnd(),
					SkipOnInsert = computed,
					SkipOnUpdate = computed,
				};
			}).ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var hasSchemas = SupportsSchemas;

			var rows =
				(
					from rc in dataConnection.GetTable<RdbRelationConstraint>()
					where rc.ConstraintType == "FOREIGN KEY"
					join refc    in dataConnection.GetTable<RdbRefConstraint>()      on rc.ConstraintName equals refc.ConstraintName
					join rcUq    in dataConnection.GetTable<RdbRelationConstraint>() on refc.ConstNameUq  equals rcUq.ConstraintName
					join thisSeg in dataConnection.GetTable<RdbIndexSegment>()       on rc.IndexName       equals thisSeg.IndexName
					join otherSeg in dataConnection.GetTable<RdbIndexSegment>()
						on new { Index = rcUq.IndexName, thisSeg.FieldPosition } equals new { Index = otherSeg.IndexName, otherSeg.FieldPosition }
					select new
					{
						rc.ConstraintName,
						ThisRelation  = rc.RelationName,
						ThisSchema    = hasSchemas ? rc.SchemaName   : null,
						OtherRelation = rcUq.RelationName,
						OtherSchema   = hasSchemas ? rcUq.SchemaName : null,
						ThisColumn    = thisSeg.FieldName,
						OtherColumn   = otherSeg.FieldName,
						thisSeg.FieldPosition,
					}
				).ToList();

			return rows.Select(r => new ForeignKeyInfo()
			{
				Name         = r.ConstraintName.TrimEnd(),
				ThisTableID  = MakeTableID(r.ThisSchema?.TrimEnd(),  r.ThisRelation.TrimEnd()),
				ThisColumn   = r.ThisColumn.TrimEnd(),
				OtherTableID = MakeTableID(r.OtherSchema?.TrimEnd(), r.OtherRelation.TrimEnd()),
				OtherColumn  = r.OtherColumn.TrimEnd(),
				Ordinal      = r.FieldPosition ?? 0,
			}).ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			var sql = _provider.Version switch
			{
				> FirebirdVersion.v25 =>
					"""
					SELECT * FROM (
						SELECT
							RDB$PACKAGE_NAME                                        AS PackageName,
							RDB$PROCEDURE_NAME                                      AS ProcedureName,
							RDB$DESCRIPTION                                         AS Description,
							RDB$PROCEDURE_SOURCE                                    AS Source,
							CASE WHEN RDB$PROCEDURE_TYPE = 1 THEN 'TF' ELSE 'P' END AS Type
						FROM RDB$PROCEDURES
						WHERE RDB$SYSTEM_FLAG = 0 AND (RDB$PRIVATE_FLAG IS NULL OR RDB$PRIVATE_FLAG = 0) AND RDB$PROCEDURE_TYPE IS NOT NULL
						UNION ALL
						SELECT
							RDB$PACKAGE_NAME,
							RDB$FUNCTION_NAME,
							RDB$DESCRIPTION,
							RDB$FUNCTION_SOURCE,
							'F'
						FROM RDB$FUNCTIONS
						WHERE RDB$SYSTEM_FLAG = 0  AND (RDB$PRIVATE_FLAG IS NULL OR RDB$PRIVATE_FLAG = 0)
					) ORDER BY PackageName, ProcedureName
					""",

				_ =>
					"""
					SELECT * FROM (
						SELECT
							NULL                                                    AS PackageName,
							RDB$PROCEDURE_NAME                                      AS ProcedureName,
							RDB$DESCRIPTION                                         AS Description,
							RDB$PROCEDURE_SOURCE                                    AS Source,
							CASE WHEN RDB$PROCEDURE_TYPE = 1 THEN 'TF' ELSE 'P' END AS Type
						FROM RDB$PROCEDURES
						WHERE RDB$SYSTEM_FLAG = 0 AND RDB$PROCEDURE_TYPE IS NOT NULL
						UNION ALL
						SELECT
							NULL,
							RDB$FUNCTION_NAME,
							RDB$DESCRIPTION,
							NULL,
							'F'
						FROM RDB$FUNCTIONS
						WHERE RDB$SYSTEM_FLAG = 0
					) ORDER BY ProcedureName
					""",
			};

			return dataConnection
				.Query(
					rd =>
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						// TrimEnd added to remove padding from union text columns
						var packageName   = rd.IsDBNull(0) ? null : rd.GetString(0).TrimEnd();
						var procedureName = rd.GetString(1).TrimEnd();
						var description   = rd.IsDBNull(2) ? null : rd.GetString(2).TrimEnd();
						var source        = rd.IsDBNull(3) ? null : rd.GetString(3).TrimEnd();
						var procedureType = rd.GetString(4).TrimEnd();

						return new ProcedureInfo()
						{
							ProcedureID         = $"{packageName}.{procedureName}",
							PackageName         = packageName,
							ProcedureName       = procedureName,
							IsFunction          = !string.Equals(procedureType, "P", StringComparison.Ordinal),
							IsTableFunction     = string.Equals(procedureType, "TF", StringComparison.Ordinal),
							IsDefaultSchema     = true,
							ProcedureDefinition = source,
							Description         = description,
						};
					},
					sql
				)
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			var sql = _provider.Version switch
			{
				> FirebirdVersion.v25 =>
					"""
					SELECT
						p.RDB$PACKAGE_NAME                                   AS PackageName,
						p.RDB$PROCEDURE_NAME                                 AS ProcedureName,
						p.RDB$PARAMETER_NAME                                 AS ParameterName,
						p.RDB$PARAMETER_NUMBER                               AS Ordinal,
						p.RDB$PARAMETER_TYPE                                 AS Direction,
						p.RDB$DESCRIPTION                                    AS Decsription,
						f.RDB$FIELD_TYPE                                     AS Type,
						f.RDB$FIELD_SUB_TYPE                                 AS SubType,
						COALESCE(f.RDB$CHARACTER_LENGTH, f.RDB$FIELD_LENGTH) AS Length,
						f.RDB$FIELD_PRECISION                                AS "precision",
						f.RDB$FIELD_SCALE                                    AS Scale,
						COALESCE(f.RDB$NULL_FLAG, p.RDB$NULL_FLAG)           AS IsNullable
					FROM RDB$PROCEDURE_PARAMETERS p
						INNER JOIN RDB$PROCEDURES pr ON p.RDB$PROCEDURE_NAME = pr.RDB$PROCEDURE_NAME
							AND (p.RDB$PACKAGE_NAME = pr.RDB$PACKAGE_NAME OR (p.RDB$PACKAGE_NAME IS NULL AND pr.RDB$PACKAGE_NAME IS NULL))
						LEFT JOIN RDB$FIELDS f ON p.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
					WHERE p.RDB$SYSTEM_FLAG = 0 AND (pr.RDB$PROCEDURE_TYPE <> 1 OR p.RDB$PARAMETER_TYPE <> 1)
					UNION ALL
					SELECT
						p.RDB$PACKAGE_NAME,
						p.RDB$FUNCTION_NAME,
						p.RDB$ARGUMENT_NAME,
						p.RDB$ARGUMENT_POSITION,
						CASE WHEN fn.RDB$RETURN_ARGUMENT = p.RDB$ARGUMENT_POSITION THEN 2 ELSE 0 END,
						p.RDB$DESCRIPTION,
						COALESCE(f.RDB$FIELD_TYPE, p.RDB$FIELD_TYPE),
						COALESCE(f.RDB$FIELD_SUB_TYPE, p.RDB$FIELD_TYPE),
						COALESCE(f.RDB$CHARACTER_LENGTH, f.RDB$FIELD_LENGTH, p.RDB$CHARACTER_LENGTH, p.RDB$FIELD_LENGTH),
						COALESCE(f.RDB$FIELD_PRECISION, p.RDB$FIELD_PRECISION),
						COALESCE(f.RDB$FIELD_SCALE, p.RDB$FIELD_SCALE),
						COALESCE(f.RDB$NULL_FLAG, p.RDB$NULL_FLAG)
						FROM RDB$FUNCTION_ARGUMENTS p
							INNER JOIN RDB$FUNCTIONS fn ON p.RDB$FUNCTION_NAME = fn.RDB$FUNCTION_NAME
								AND (p.RDB$PACKAGE_NAME = fn.RDB$PACKAGE_NAME OR (p.RDB$PACKAGE_NAME IS NULL AND fn.RDB$PACKAGE_NAME IS NULL))
							LEFT JOIN RDB$FIELDS f ON p.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
					WHERE p.RDB$SYSTEM_FLAG = 0
					""",

				_ =>
					"""
					SELECT
						NULL                                                 AS PackageName,
						p.RDB$PROCEDURE_NAME                                 AS ProcedureName,
						p.RDB$PARAMETER_NAME                                 AS ParameterName,
						p.RDB$PARAMETER_NUMBER                               AS Ordinal,
						p.RDB$PARAMETER_TYPE                                 AS Direction,
						p.RDB$DESCRIPTION                                    AS Decsription,
						f.RDB$FIELD_TYPE                                     AS Type,
						f.RDB$FIELD_SUB_TYPE                                 AS SubType,
						COALESCE(f.RDB$CHARACTER_LENGTH, f.RDB$FIELD_LENGTH) AS Length,
						f.RDB$FIELD_PRECISION                                AS "precision",
						f.RDB$FIELD_SCALE                                    AS Scale,
						COALESCE(f.RDB$NULL_FLAG, p.RDB$NULL_FLAG)           AS IsNullable
					FROM RDB$PROCEDURE_PARAMETERS p
						INNER JOIN RDB$PROCEDURES pr ON p.RDB$PROCEDURE_NAME = pr.RDB$PROCEDURE_NAME
						LEFT JOIN RDB$FIELDS f ON p.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
					WHERE p.RDB$SYSTEM_FLAG = 0 AND (pr.RDB$PROCEDURE_TYPE <> 1 OR p.RDB$PARAMETER_TYPE <> 1)
					UNION ALL
					SELECT
						NULL,
						p.RDB$FUNCTION_NAME,
						NULL,
						p.RDB$ARGUMENT_POSITION,
						CASE WHEN fn.RDB$RETURN_ARGUMENT = p.RDB$ARGUMENT_POSITION THEN 2 ELSE 0 END,
						NULL,
						p.RDB$FIELD_TYPE,
						p.RDB$FIELD_TYPE,
						COALESCE(p.RDB$CHARACTER_LENGTH, p.RDB$FIELD_LENGTH),
						p.RDB$FIELD_PRECISION,
						p.RDB$FIELD_SCALE,
						NULL
					FROM RDB$FUNCTION_ARGUMENTS p
							INNER JOIN RDB$FUNCTIONS fn ON p.RDB$FUNCTION_NAME = fn.RDB$FUNCTION_NAME
					""",
			};

			return dataConnection
				.Query(
					rd =>
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						// TrimEnd added to remove padding from union text columns
						var packageName   = rd.IsDBNull(0) ? null : rd.GetString(0).TrimEnd();
						var procedureName = rd.GetString(1).TrimEnd();
						var parameterName = rd.IsDBNull(2) ? null : rd.GetString(2).TrimEnd();
						var ordinal       = rd.GetInt32(3);
						// 2: return, 0: in, 1: out
						var direction     = rd.GetInt32(4);
						var description   = rd.IsDBNull(5) ? null : rd.GetString(5).TrimEnd();
						var type          = rd.GetInt32(6);
						var subType       = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7);
						var length        = rd.GetInt32(8);
						var precision     = rd.IsDBNull(9) ? (int?)null : rd.GetInt32(9);
						var scale         = rd.GetInt32(10);
						var isNullable    = rd.IsDBNull(11) ? true : rd.GetInt32(11) != 1;

						return new ProcedureParameterInfo()
						{
							ProcedureID   = $"{packageName}.{procedureName}",
							// input/output parameters in procedure have non-shared ordinals
							Ordinal       = direction == 1 ? ordinal + 1000 : ordinal,
							ParameterName = parameterName,
							Length        = length,
							Precision     = precision,
							Scale         = scale,
							IsIn          = direction == 0,
							IsOut         = direction == 1,
							IsResult      = direction == 2,
							IsNullable    = isNullable,
							Description   = description,
							DataType      = CreateTypeName(type, subType ?? 0, scale),
						};
					},
					sql
				)
				.ToList();
		}

		private static string CreateTypeName(int type, int subType, int scale)
		{
			// translation table based on logic from
			// https://github.com/FirebirdSQL/NETProvider/blob/master/src/FirebirdSql.Data.FirebirdClient/Common/TypeHelper.cs
			return (type, subType, scale) switch
			{
				(37, _, _) or (38, _, _)                                              => "VARCHAR",
				(14, _, _) or (15, _, _) or (40, _, _) or (41, _, _)                  => "CHAR",
				(7 or 8 or 9 or 16 or 45 or 11 or 27 or 26, 2, _)
					or (7 or 8 or 9 or 16 or 45 or 11 or 27 or 26, not (1 or 2), < 0) => "DECIMAL",
				(7 or 8 or 9 or 16 or 45 or 11 or 27 or 26, 1, _)                     => "NUMERIC",
				(7, not (1 or 2), >= 0)                                               => "SMALLINT",
				(8, not (1 or 2), >= 0)                                               => "INTEGER",
				(9 or 16 or 45, not (1 or 2), >= 0)                                   => "BIGINT",
				(10, _, _)                                                            => "FLOAT",
				(11 or 27, not (1 or 2), >= 0)                                        => "DOUBLE PRECISION",
				(261, 1, _)                                                           => "BLOB SUB_TYPE 1", // Text
				(261, not 1, _)                                                       => "BLOB",
				(35, _, _)                                                            => "TIMESTAMP",
				(13, _, _)                                                            => "TIME",
				(12, _, _)                                                            => "DATE",
				(23, _, _)                                                            => "BOOLEAN",
				(29 or 31, _, _)                                                      => "TIMESTAMP WITH TIME ZONE",
				(28 or 39, _, _)                                                      => "TIME WITH TIME ZONE",
				(24 or 25, _, _)                                                      => "DECFLOAT",
				(26, not (1 or 2), >= 0)                                              => "INT128",
				_                                                                     => "unknown",
			};
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let systemType   = r.Field<Type>("DataType")
				let columnName   = r.Field<string>("ColumnName")
				let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
				let dataType     = GetDataTypeByProviderDbType(providerType, options)
				let columnType   = dataType == null ? null : dataType.TypeName
				let length       = r.Field<int> ("ColumnSize")
				let precision    = Converter.ChangeTypeTo<int> (r["NumericPrecision"])
				let scale        = Converter.ChangeTypeTo<int> (r["NumericScale"])
				let isNullable   = Converter.ChangeTypeTo<bool>(r["AllowDBNull"])

				select new ColumnSchema
				{
					ColumnType           = GetDbType(options, columnType, dataType, length, precision, scale, null, null, null),
					ColumnName           = columnName,
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType,
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					Precision            = providerType == 21 ? 16 : null,
				}
			).ToList();
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			try
			{
				return base.GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);
			}
			catch (Exception ex)
			{
				// procedure XXX does not return any values
				if (ex.Message.Contains("SQL error code = -84", StringComparison.Ordinal)
					// SchemaOnly doesn't work for non-selectable procedures in FB
					|| ex.Message.Contains("is not selectable", StringComparison.Ordinal))
					return null;
				throw;
			}
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dataTypes = base.GetDataTypes(dataConnection);

			var knownTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var dataType in dataTypes)
			{
				knownTypes.Add(dataType.TypeName);
				if (string.IsNullOrEmpty(dataType.CreateFormat) && !string.IsNullOrEmpty(dataType.CreateParameters))
				{
					dataType.CreateFormat =
						dataType.TypeName + "(" +
						string.JoinStrings(',', dataType.CreateParameters!.Split(',').Select((_,i) => string.Create(CultureInfo.InvariantCulture, $"{{{i}}}"))) +
						")";
				}
			}

			// provider doesn't add new FB4 types to DATATYPES schema API and older boolean type
			// https://github.com/FirebirdSQL/NETProvider/blob/master/Provider/src/FirebirdSql.Data.FirebirdClient/Schema/FbMetaData.xml
			if (!knownTypes.Contains("boolean"))
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = false, TypeName = "boolean", DataType = "System.Boolean", ProviderDbType = 3 });
			if (!knownTypes.Contains("int128"))
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = false, TypeName = "int128", DataType = "System.Numerics.BigInteger", ProviderDbType = 23 });
			if (!knownTypes.Contains("decfloat"))
			{
				// decfloat(16)
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "decfloat", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbDecFloat", CreateFormat = "DECFLOAT({0})", ProviderDbType = 21 });
				// decfloat(34)
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "decfloat", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbDecFloat", CreateFormat = null, ProviderDbType = 22 });
			}

			if (!knownTypes.Contains("timestamp with time zone"))
			{
				// tstz
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "timestamp with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedDateTime", ProviderDbType = 17 });
				// tstzEx
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "timestamp with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedDateTime", ProviderDbType = 18 });
			}

			if (!knownTypes.Contains("time with time zone"))
			{
				//ttz
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "time with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedTime", ProviderDbType = 19 });
				//ttzEx
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "time with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedTime", ProviderDbType = 20 });
			}

			return dataTypes;
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType?.ToLowerInvariant() switch
			{
				"array"                    => DataType.VarBinary,
				"bigint"                   => DataType.Int64,
				"blob"                     => DataType.Blob,
				"boolean"                  => DataType.Boolean,
				"char"                     => DataType.NChar,
				"date"                     => DataType.Date,
				"decimal"                  => DataType.Decimal,
				"double precision"         => DataType.Double,
				"float"                    => DataType.Single,
				"integer"                  => DataType.Int32,
				"numeric"                  => DataType.Decimal,
				"smallint"                 => DataType.Int16,
				"blob sub_type 1"          => DataType.Text,
				"time"                     => DataType.Time,
				"timestamp"                => DataType.DateTime,
				"varchar"                  => DataType.NVarChar,
				"int128"                   => DataType.Int128,
				"decfloat"                 => DataType.DecFloat,
				"timestamp with time zone" => DataType.DateTimeOffset,
				"time with time zone"      => DataType.TimeTZ,
				_                          => DataType.Undefined,
			};
		}

		protected override string? GetProviderSpecificTypeNamespace()
		{
			return _provider.Adapter.ProviderTypesNamespace;
		}

		protected override string? GetProviderSpecificType(string? dataType)
		{
			return (dataType?.ToLowerInvariant()) switch
			{
				"decfloat"                 => _provider.Adapter.FbDecFloatType?.Name,
				"timestamp with time zone" => _provider.Adapter.FbZonedDateTimeType?.Name,
				"time with time zone"      => _provider.Adapter.FbZonedTimeType?.Name,
				_                          => base.GetProviderSpecificType(dataType),
			};
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return (dataType?.ToLowerInvariant()) switch
			{
				"int128"                   => typeof(BigInteger),
				"decfloat"                 => _provider.Adapter.FbDecFloatType,
				"timestamp with time zone" => _provider.Adapter.FbZonedDateTimeType,
				"time with time zone"      => _provider.Adapter.FbZonedTimeType,
				_                          => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		protected override void LoadProcedureTableSchema(DataConnection dataConnection, GetSchemaOptions options, ProcedureSchema procedure, string commandText, List<TableSchema> tables)
		{
			base.LoadProcedureTableSchema(dataConnection, options, procedure, commandText, tables);

			// remove output parameters, defined for return columns if `FOR SELECT` procedures
			if (procedure.ResultTable != null)
				foreach (var col in procedure.ResultTable.Columns)
					for (var i = 0; i < procedure.Parameters.Count; i++)
						if (procedure.Parameters[i].IsOut && string.Equals(col.ColumnName, procedure.Parameters[i].ParameterName, StringComparison.Ordinal))
						{
							procedure.Parameters.RemoveAt(i);
							break;
						}
		}

		/// <summary>
		/// Builds table function call command.
		/// </summary>
		protected override string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			commandText = "SELECT * FROM " + commandText;

			if (procedure.Parameters.Count > 0)
			{
				commandText += "(";

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					if (i != 0)
						commandText += ",";
					commandText += "NULL";
				}

				commandText += ")";
			}

			return commandText;
		}
	}
}
