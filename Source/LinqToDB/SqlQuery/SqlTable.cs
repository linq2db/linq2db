using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Data;
	using Mapping;
	using Remote;

	public class SqlTable : ISqlTableSource, IQueryExtendible
	{
		#region Init

		protected internal SqlTable(Type objectType, int? sourceId, SqlObjectName tableName)
		{
			SourceID   = sourceId ?? Interlocked.Increment(ref SelectQuery.SourceIDCounter);
			ObjectType = objectType;
			TableName  = tableName;
		}

		internal SqlTable(
			int                      id,
			string?                  name,
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
			Name               = name;
			Alias              = alias;
			SequenceAttributes = sequenceAttributes;
			ID                 = tableID;

			AddRange(fields);

			SqlTableType   = sqlTableType;
			TableArguments = tableArguments;
			TableOptions   = tableOptions;
		}

		#endregion

		#region Init from type

		public SqlTable(MappingSchema mappingSchema, Type objectType, string? physicalName = null)
			: this(objectType, null, new(String.Empty))
		{
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			var ed = mappingSchema.GetEntityDescriptor(objectType);

			Name         = ed.Name.Name;
			TableName    = physicalName != null && ed.Name.Name != physicalName ? ed.Name with { Name = physicalName } : ed.Name;
			TableOptions = ed.TableOptions;

			foreach (var column in ed.Columns)
			{
				var field = new SqlField(column);

				Add(field);

				if (field.Type.DataType == DataType.Undefined)
				{
					var dataType = mappingSchema.GetDataType(field.Type.SystemType);

					if (dataType.Type.DataType == DataType.Undefined)
					{
						dataType = mappingSchema.GetUnderlyingDataType(field.Type.SystemType, out var canBeNull);

						if (canBeNull)
							field.CanBeNull = true;
					}

					field.Type = field.Type.WithDataType(dataType.Type.DataType);

					// try to get type from converter
					if (field.Type.DataType == DataType.Undefined)
					{
						try
						{
							var converter = mappingSchema.GetConverter(
								field.Type,
								new DbDataType(typeof(DataParameter)), true);

							var parameter = converter?.ConvertValueToParameter?.Invoke(DefaultValue.GetValue(field.Type.SystemType, mappingSchema));
							if (parameter != null)
								field.Type = field.Type.WithDataType(parameter.DataType);
						}
						catch
						{
							// converter cannot handle default value?
						}
					}

					if (field.Type.Length    == null) field.Type = field.Type.WithLength   (dataType.Type.Length);
					if (field.Type.Precision == null) field.Type = field.Type.WithPrecision(dataType.Type.Precision);
					if (field.Type.Scale     == null) field.Type = field.Type.WithScale    (dataType.Type.Scale);
				}
			}

			var identityField = GetIdentityField();

			if (identityField != null)
			{
				var cd = ed[identityField.Name]!;
				SequenceAttributes = cd.SequenceName == null ? null : new[] { cd.SequenceName };
			}
		}

		public SqlTable(Type objectType)
			: this(MappingSchema.Default, objectType)
		{
		}

		#endregion

		#region Init from Table

		public SqlTable(SqlTable table)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			Name               = table.Name;
			SequenceAttributes = table.SequenceAttributes;

			foreach (var field in table.Fields)
				Add(new SqlField(field));

			SqlTableType       = table.SqlTableType;
			TableArguments     = table.TableArguments;
			SqlQueryExtensions = table.SqlQueryExtensions;
		}

		public SqlTable(SqlTable table, IEnumerable<SqlField> fields, ISqlExpression[] tableArguments)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			Name               = table.Name;
			SequenceAttributes = table.SequenceAttributes;
			TableOptions       = table.TableOptions;

			AddRange(fields);

			SqlTableType       = table.SqlTableType;
			TableArguments     = tableArguments;
			SqlQueryExtensions = table.SqlQueryExtensions;
		}

		#endregion

		#region Overrides

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

		#endregion

		#region Public Members

		public SqlField? this[string fieldName]
		{
			get
			{
				_fieldsLookup.TryGetValue(fieldName, out var field);
				return field;
			}
		}

		public virtual string?           Name           { get; set; }
		public         string?           Alias          { get; set; }
		public virtual SqlObjectName     TableName      { get; set; }
		public         Type              ObjectType     { get; protected internal set; }
		public virtual SqlTableType      SqlTableType   { get; set; }
		public         ISqlExpression[]? TableArguments { get; set; }
		public         TableOptions      TableOptions   { get; set; }
		public virtual string?           ID             { get; set; }

		// list user to preserve order of fields in queries
		readonly List<SqlField>              _orderedFields = new();
		readonly Dictionary<string,SqlField> _fieldsLookup  = new();

		public           IReadOnlyList<SqlField>         Fields => _orderedFields;
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

		private SqlField? _all;
		public  SqlField   All => _all ??= SqlField.All(this);

		public SqlField? GetIdentityField()
		{
			foreach (var field in Fields)
				if (field.IsIdentity)
					return field;

			var keys = GetKeys(true);

			if (keys.Count == 1)
				return (SqlField)keys[0];

			return null;
		}

		public void Add(SqlField field)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;

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

		public IList<ISqlExpression> GetKeys(bool allIfEmpty)
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

		#endregion

		#region IQueryElement Members

		public virtual QueryElementType ElementType => QueryElementType.SqlTable;

		public virtual StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (TableName.Server   != null) sb.Append($"[{TableName.Server}].");
			if (TableName.Database != null) sb.Append($"[{TableName.Database}].");
			if (TableName.Schema   != null) sb.Append($"[{TableName.Schema}].");
			return sb.Append($"[{Name}({SourceID})]");
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull { get; set; } = true;

		int  ISqlExpression.Precedence => Precedence.Primary;
		Type ISqlExpression.SystemType => ObjectType;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return this == other;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public virtual ISqlExpression Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			if (TableArguments != null)
				for (var i = 0; i < TableArguments.Length; i++)
					TableArguments[i] = TableArguments[i].Walk(options, context, func)!;

			if (SqlQueryExtensions != null)
				foreach (var e in SqlQueryExtensions)
					e.Walk(options, context, func);

			return func(context, this);
		}

		#endregion

		#region System tables

		internal static SqlTable Inserted(Type objectType)
			=> new (objectType)
			{
				Name         = "INSERTED",
				TableName    = new ("INSERTED"),
				SqlTableType = SqlTableType.SystemTable,
			};

		internal static SqlTable Deleted(Type objectType)
			=> new (objectType)
			{
				Name         = "DELETED",
				TableName    = new ("DELETED"),
				SqlTableType = SqlTableType.SystemTable,
			};

		#endregion
	}
}
