using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Data;
	using Mapping;

	public class SqlTable : ISqlTableSource
	{
		#region Init

		public SqlTable()
		{
			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
			Fields   = new Dictionary<string,SqlField>();
		}

		internal SqlTable(
			int id, string? name, string alias,
			string? server, string? database, string? schema, string? physicalName,
			Type?                    objectType,
			SequenceNameAttribute[]? sequenceAttributes,
			SqlField[]               fields,
			SqlTableType             sqlTableType,
			ISqlExpression[]?        tableArguments)
		{
			SourceID           = id;
			Name               = name;
			Alias              = alias;
			Server             = server;
			Database           = database;
			Schema             = schema;
			PhysicalName       = physicalName;
			ObjectType         = objectType;
			SequenceAttributes = sequenceAttributes;

			Fields = new Dictionary<string, SqlField>();

			AddRange(fields);

			foreach (var field in fields)
			{
				if (field.Name == "*")
				{
					_all = field;
					Fields.Remove("*");
					_all.Table = this;
					break;
				}
			}

			SqlTableType   = sqlTableType;
			TableArguments = tableArguments;
		}

		#endregion

		#region Init from type

		public SqlTable(MappingSchema mappingSchema, Type objectType, string? physicalName = null) : this()
		{
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

			var ed = mappingSchema.GetEntityDescriptor(objectType);

			Server       = ed.ServerName;
			Database     = ed.DatabaseName;
			Schema       = ed.SchemaName;
			Name         = ed.TableName;
			ObjectType   = objectType;
			PhysicalName = physicalName ?? Name;

			foreach (var column in ed.Columns)
			{
				var field = new SqlField(column);

				Add(field);

				if (field.Type!.Value.DataType == DataType.Undefined)
				{
					var dataType = mappingSchema.GetDataType(field.Type!.Value.SystemType);

					if (dataType.Type.DataType == DataType.Undefined)
					{
						dataType = mappingSchema.GetUnderlyingDataType(field.Type!.Value.SystemType, out var canBeNull);

						if (canBeNull)
							field.CanBeNull = true;
					}

					field.Type = field.Type!.Value.WithDataType(dataType.Type.DataType);

					// try to get type from converter
					if (field.Type!.Value.DataType == DataType.Undefined)
					{
						try
						{
							var converter = mappingSchema.GetConverter(
								field.Type!.Value,
								new DbDataType(typeof(DataParameter)), true);

							var parameter = converter?.ConvertValueToParameter?.Invoke(DefaultValue.GetValue(field.Type!.Value.SystemType, mappingSchema));
							if (parameter != null)
								field.Type = field.Type!.Value.WithDataType(parameter.DataType);
						}
						catch
						{
							// converter cannot handle default value?
						}
					}

					if (field.Type!.Value.Length    == null) field.Type = field.Type!.Value.WithLength   (dataType.Type.Length);
					if (field.Type!.Value.Precision == null) field.Type = field.Type!.Value.WithPrecision(dataType.Type.Precision);
					if (field.Type!.Value.Scale     == null) field.Type = field.Type!.Value.WithScale    (dataType.Type.Scale);
				}
			}

			var identityField = GetIdentityField();

			if (identityField != null)
			{
				var cd = ed[identityField.Name];
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
			: this()
		{
			Alias              = table.Alias;
			Server             = table.Server;
			Database           = table.Database;
			Schema             = table.Schema;
			Name               = table.Name;
			PhysicalName       = table.PhysicalName;
			ObjectType         = table.ObjectType;
			SequenceAttributes = table.SequenceAttributes;

			foreach (var field in table.Fields.Values)
				Add(new SqlField(field));

			SqlTableType   = table.SqlTableType;
			TableArguments = table.TableArguments;
		}

		public SqlTable(SqlTable table, IEnumerable<SqlField> fields, ISqlExpression[] tableArguments)
			: this()
		{
			Alias              = table.Alias;
			Server             = table.Server;
			Database           = table.Database;
			Schema             = table.Schema;
			Name               = table.Name;
			PhysicalName       = table.PhysicalName;
			ObjectType         = table.ObjectType;
			SequenceAttributes = table.SequenceAttributes;

			AddRange(fields);

			SqlTableType   = table.SqlTableType;
			TableArguments = tableArguments;
		}

		#endregion

		#region Overrides

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

		#endregion

		#region Public Members

		public SqlField this[string fieldName]
		{
			get
			{
				Fields.TryGetValue(fieldName, out var field);
				return field;
			}
		}

		public virtual string?           Name           { get; set; }
		public         string?           Alias          { get; set; }
		public         string?           Server         { get; set; }
		public         string?           Database       { get; set; }
		public         string?           Schema         { get; set; }
		public         Type?             ObjectType     { get; set; }
		public virtual string?           PhysicalName   { get; set; }
		public virtual SqlTableType      SqlTableType   { get; set; }
		public         ISqlExpression[]? TableArguments { get; set; }

		public Dictionary<string,SqlField> Fields { get; }

		public SequenceNameAttribute[]? SequenceAttributes { get; protected set; }

		private SqlField? _all;
		public  SqlField  All => _all ?? (_all = SqlField.All(this));

		public SqlField? GetIdentityField()
		{
			foreach (var field in Fields)
				if (field.Value.IsIdentity)
					return field.Value;

			var keys = GetKeys(true);

			if (keys != null && keys.Count == 1)
				return (SqlField)keys[0];

			return null;
		}

		public void Add(SqlField field)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;

			Fields.Add(field.Name, field);
		}

		public void AddRange(IEnumerable<SqlField> collection)
		{
			foreach (var item in collection)
				Add(item);
		}

		#endregion

		#region ISqlTableSource Members

		public   int  SourceID { get; protected set; }

		List<ISqlExpression>? _keyFields;

		public IList<ISqlExpression> GetKeys(bool allIfEmpty)
		{
			if (_keyFields == null)
			{
				_keyFields = (
					from f in Fields.Values
					where   f.IsPrimaryKey
					orderby f.PrimaryKeyOrder
					select f as ISqlExpression
				).ToList();
			}

			if (_keyFields.Count == 0 && allIfEmpty)
				return Fields.Values.Select(f => f as ISqlExpression).ToList();

			return _keyFields;
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
			{
				var table = new SqlTable
				{
					Name               = Name,
					Alias              = Alias,
					Server             = Server,
					Database           = Database,
					Schema             = Schema,
					PhysicalName       = PhysicalName,
					ObjectType         = ObjectType,
					SqlTableType       = SqlTableType,
					SequenceAttributes = SequenceAttributes,
				};

				table.Fields.Clear();

				foreach (var field in Fields)
				{
					var fc = new SqlField(field.Value);

					objectTree.Add(field.Value, fc);
					table.     Add(fc);
				}

				TableArguments = TableArguments?.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray();

				objectTree.Add(this, table);
				objectTree.Add(All,  table.All);

				clone = table;
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public virtual QueryElementType ElementType { [DebuggerStepThrough] get; } = QueryElementType.SqlTable;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (Server   != null) sb.Append($"[{Server}].");
			if (Database != null) sb.Append($"[{Database}].");
			if (Schema   != null) sb.Append($"[{Schema}].");
			return sb.Append($"[{Name}({SourceID})]");
		}

		#endregion

		#region ISqlExpression Members

		bool  ISqlExpression.CanBeNull  => true;
		int   ISqlExpression.Precedence => Precedence.Primary;
		Type? ISqlExpression.SystemType => ObjectType;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return this == other;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public virtual ISqlExpression Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			if (TableArguments != null)
				for (var i = 0; i < TableArguments.Length; i++)
					TableArguments[i] = TableArguments[i].Walk(options, func)!;

			return func(this);
		}

		#endregion
	}
}
