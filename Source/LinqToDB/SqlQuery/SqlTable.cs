using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	public class SqlTable : SqlExpressionBase, ISqlTableSource
	{
		#region Init

		protected internal SqlTable(Type objectType, int? sourceId, SqlObjectName tableName)
		{
			SourceID   = sourceId ?? Interlocked.Increment(ref SelectQuery.SourceIDCounter);
			ObjectType = objectType;
			TableName  = tableName;
			_all       = SqlField.All(this);
		}

		internal SqlTable(
			int                      id,
			string?                  expression,
			string                   alias,
			SqlObjectName            tableName,
			Type                     objectType,
			SequenceNameAttribute[]? sequenceAttributes,
			IEnumerable<SqlField>    fields,
			SqlTableType             sqlTableType,
			ISqlExpression[]?        tableArguments,
			TableOptions             tableOptions,
			string?                  tableID)
			: this(objectType, id, tableName)
		{
			Expression         = expression;
			Alias              = alias;
			SequenceAttributes = sequenceAttributes;
			ID                 = tableID;

			AddRange(fields);

			SqlTableType   = sqlTableType;
			TableArguments = tableArguments;
			TableOptions   = tableOptions;

			_all ??= SqlField.All(this);
		}

		#endregion

		#region Init from type

		public SqlTable(EntityDescriptor entityDescriptor, string? physicalName = null)
			: this(entityDescriptor.ObjectType, (int?)null, new(string.Empty))
		{
			TableName    = physicalName != null && entityDescriptor.Name.Name != physicalName ? entityDescriptor.Name with { Name = physicalName } : entityDescriptor.Name;
			TableOptions = entityDescriptor.TableOptions;

			if (!entityDescriptor.MappingSchema.IsScalarType(ObjectType))
			{
				foreach (var column in entityDescriptor.Columns)
				{
					var field = new SqlField(column);

					Add(field);

					if (field.Type.DataType == DataType.Undefined)
					{
						field.Type = SuggestType(field.Type, entityDescriptor.MappingSchema, out var canBeNull);
						if (canBeNull is not null)
							field.CanBeNull = canBeNull.Value;
					}
				}

				var identityField = GetIdentityField();

				if (identityField != null)
				{
					var cd = entityDescriptor[identityField.Name]!;
					SequenceAttributes = cd.SequenceName == null ? null : new[] { cd.SequenceName };
				}
			}

			_all ??= SqlField.All(this);
		}

		#endregion

		#region Init from Table

		public SqlTable(SqlTable table)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			SequenceAttributes = table.SequenceAttributes;

			foreach (var field in table.Fields)
				Add(new SqlField(field));

			SqlTableType       = table.SqlTableType;
			SqlQueryExtensions = table.SqlQueryExtensions;

			Expression         = table.Expression;
			TableArguments     = table.TableArguments;

			_all ??= SqlField.All(this);
		}

		public SqlTable(SqlTable table, IEnumerable<SqlField> fields, ISqlExpression[] tableArguments)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			Expression         = table.Expression;
			SequenceAttributes = table.SequenceAttributes;
			TableOptions       = table.TableOptions;

			AddRange(fields);

			SqlTableType       = table.SqlTableType;
			TableArguments     = tableArguments;
			SqlQueryExtensions = table.SqlQueryExtensions;

			_all ??= SqlField.All(this);
		}

		#endregion

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SqlTable;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (TableName.Server   != null) writer.Append('[').Append(TableName.Server).Append("].");
			if (TableName.Database != null) writer.Append('[').Append(TableName.Database).Append("].");
			if (TableName.Schema   != null) writer.Append('[').Append(TableName.Schema).Append("].");

			writer.Append('[');
			if (Expression != null)
			{
				writer.Append(Expression);

				var len = writer.Length;
				var arguments  = (TableArguments ?? Enumerable.Empty<ISqlExpression>()).Select(p =>
				{
					p.ToString(writer);
					var s = writer.ToString(len, writer.Length - len);
					writer.Length = len;
					return s;
				}).ToList();

				if (arguments.Count > 0)
				{
					writer
						.Append('(')
						.Append(string.Join(", ", arguments))
						.Append(')');
				}
			}
			else
			{
				writer.Append(TableName.Name);
			}
			
			writer.Append('(').Append(SourceID).Append(")]");

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(SourceID);
			hash.Add(TableName);
			hash.Add(Alias);
			hash.Add(ObjectType);
			hash.Add(SqlTableType);
			hash.Add(TableOptions);
			hash.Add(Expression);
			if (TableArguments != null)
			{
				foreach (var arg in TableArguments)
					hash.Add(arg.GetElementHashCode());
			}
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (other is not SqlTable otherTable)
				return false;

			return ObjectType == otherTable.ObjectType &&
			       TableName  == otherTable.TableName  &&
			       Alias      == otherTable.Alias;
		}

		public override bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public override int Precedence => SqlQuery.Precedence.Primary;
		public override Type SystemType => ObjectType;
		
		#endregion

		#region Public Members

		/// <summary>
		/// Search for table field by mapping class member name.
		/// </summary>
		/// <param name="memberName">Mapping class member name.</param>
		public SqlField? FindFieldByMemberName(string memberName)
		{
			_fieldsLookup.TryGetValue(memberName, out var field);
			return field;
		}

		public         string?           Alias          { get; set; }
		public virtual SqlObjectName     TableName      { get; set; }
		public         Type              ObjectType     { get; protected internal set; }
		public virtual SqlTableType      SqlTableType   { get; set; }
		public         TableOptions      TableOptions   { get; set; }
		public virtual string?           ID             { get; set; }

		public bool CanBeNull { get; set; } = true;

		/// <summary>
		/// Custom SQL expression format string (used together with <see cref="TableArguments"/>) to
		/// transform <see cref="SqlTable"/> to custom table expression.
		/// Arguments:
		/// <list type="bullet">
		/// <item>{0}: <see cref="TableName"/></item>
		/// <item>{1}: <see cref="Alias"/></item>
		/// <item>{2+}: arguments from <see cref="TableArguments"/> (with index adjusted by 2)</item>
		/// </list>
		/// </summary>
		public string?           Expression     { get; set; }
		public ISqlExpression[]? TableArguments { get; set; }

		internal string NameForLogging => Expression ?? TableName.Name;

		// list user to preserve order of fields in queries
		internal readonly List<SqlField>              _orderedFields = new();
		readonly          Dictionary<string,SqlField> _fieldsLookup  = new();

		public           List<SqlField> Fields => _orderedFields;
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }

		// identity fields cached, as it is most used fields filter
		private readonly List<SqlField>                  _identityFields = new ();
		public IReadOnlyList<SqlField> IdentityFields => _identityFields;

		internal void ClearFields()
		{
			_fieldsLookup  .Clear();
			_orderedFields .Clear();
			_identityFields.Clear();
		}

		public SequenceNameAttribute[]? SequenceAttributes { get; protected internal set; }

		SqlField?       _all;
		public SqlField All => _all!;

		public SqlField? GetIdentityField()
		{
			foreach (var field in Fields)
				if (field.IsIdentity)
					return field;

			var keys = GetKeys(true);

			if (keys?.Count == 1)
				return (SqlField)keys[0];

			return null;
		}

		public void Add(SqlField field)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;

			ResetKeys();

			if (field.Name == "*")
				_all = field;
			else
			{
				_fieldsLookup.Add(field.Name, field);
				_orderedFields.Add(field);

				if (field.IsIdentity)
					_identityFields.Add(field);
			}
		}

		public void AddRange(IEnumerable<SqlField> collection)
		{
			foreach (var item in collection)
				Add(item);
		}

		#endregion

		#region ISqlTableSource Members

		public int SourceID { get; }

		List<ISqlExpression>? _keyFields;

		public virtual IList<ISqlExpression>? GetKeys(bool allIfEmpty)
		{
			_keyFields ??=
			(
				from f in Fields
				where f.IsPrimaryKey
				orderby f.PrimaryKeyOrder
				select f as ISqlExpression
			)
			.ToList();

			if (_keyFields.Count == 0 && allIfEmpty)
				return Fields.Select(f => f as ISqlExpression).ToList();

			return _keyFields;
		}

		public void ResetKeys()
		{
			_keyFields = null;
		}

		#endregion

		internal static SqlTable Create<T>(IDataContext dataContext)
		{
			return new SqlTable(dataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated));
		}

		internal static DbDataType SuggestType(DbDataType fieldType, MappingSchema mappingSchema, out bool? canBeNull)
		{
			var dataType = mappingSchema.GetDataType(fieldType.SystemType);

			canBeNull = null;

			if (dataType.Type.DataType == DataType.Undefined)
			{
				dataType = mappingSchema.GetUnderlyingDataType(fieldType.SystemType, out var underlyingCanBeNull);

				if (underlyingCanBeNull)
					canBeNull = true;
			}

			fieldType = fieldType.WithDataType(dataType.Type.DataType);

			// try to get type from converter
			if (fieldType.DataType == DataType.Undefined)
			{
				try
				{
					var converter = mappingSchema.GetConverter(
						fieldType,
						new DbDataType(typeof(DataParameter)),
						true,
						ConversionType.ToDatabase);

					var parameter = converter?.ConvertValueToParameter(DefaultValue.GetValue(fieldType.SystemType, mappingSchema));
					if (parameter != null)
						fieldType = fieldType.WithDataType(parameter.DataType);
				}
				catch
				{
					// converter cannot handle default value?
				}
			}

			if (fieldType.Length    == null) fieldType = fieldType.WithLength(dataType.Type.Length);
			if (fieldType.Precision == null) fieldType = fieldType.WithPrecision(dataType.Type.Precision);
			if (fieldType.Scale     == null) fieldType = fieldType.WithScale(dataType.Type.Scale);

			return fieldType;
		}

	}
}
