using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlBuilder
{
	using Mapping;

	public class SqlTable : ISqlTableSource
	{
		#region Init

		public SqlTable()
		{
			_sourceID = Interlocked.Increment(ref SqlQuery.SourceIDCounter);
			_fields   = new Dictionary<string,SqlField>();
		}

		internal SqlTable(
			int id, string name, string alias, string database, string owner, string physicalName, Type objectType,
			SequenceNameAttribute[] sequenceAttributes,
			SqlField[]              fields,
			SqlTableType            sqlTableType,
			ISqlExpression[]        tableArguments)
		{
			_sourceID          = id;
			Name               = name;
			Alias              = alias;
			Database           = database;
			Owner              = owner;
			PhysicalName       = physicalName;
			ObjectType         = objectType;
			SequenceAttributes = sequenceAttributes;

			_fields = new Dictionary<string, SqlField>();

			AddRange(fields);

			foreach (var field in fields)
			{
				if (field.Name == "*")
				{
					_all = field;
					_fields.Remove("*");
					_all.Table = this;
					break;
				}
			}

			SqlTableType   = sqlTableType;
			TableArguments = tableArguments;
		}

		#endregion

		#region Init from type

		public SqlTable([JetBrains.Annotations.NotNull] MappingSchema mappingSchema, Type objectType) : this()
		{
			if (mappingSchema == null) throw new ArgumentNullException("mappingSchema");

			var ed = mappingSchema.GetEntityDescriptor(objectType);

			Database     = ed.DatabaseName;
			Owner        = ed.SchemaName;
			Name         = ed.TableName;
			ObjectType   = objectType;
			PhysicalName = Name;

			foreach (var column in ed.Columns)
			{
				Add(new SqlField
				{
					SystemType       = column.MemberType,
					Name             = column.MemberName,
					PhysicalName     = column.ColumnName,
					Nullable         = column.CanBeNull,
					IsPrimaryKey     = column.IsPrimaryKey,
					PrimaryKeyOrder  = column.PrimaryKeyOrder,
					IsIdentity       = column.IsIdentity,
					IsInsertable     = !column.SkipOnInsert,
					IsUpdatable      = !column.SkipOnUpdate,
					ColumnDescriptor = column,
				});
			}

			var identityField = GetIdentityField();

			if (identityField != null)
			{
				var cd = ed[identityField.Name];

				SequenceAttributes = mappingSchema.GetAttributes<SequenceNameAttribute>(
					cd.MemberAccessor.MemberInfo, a => a.Configuration);
			}
		}

		public SqlTable(Type objectType)
			: this(MappingSchema.Default, objectType)
		{
		}

		#endregion

		#region Init from Table

		public SqlTable(SqlTable table) : this()
		{
			Alias              = table.Alias;
			Database           = table.Database;
			Owner              = table.Owner;
			Name               = table.Name;
			PhysicalName       = table.PhysicalName;
			ObjectType         = table.ObjectType;
			SequenceAttributes = table.SequenceAttributes;

			foreach (var field in table.Fields.Values)
				Add(new SqlField(field));

			SqlTableType   = table.SqlTableType;
			TableArguments = table.TableArguments;
		}

		public SqlTable(SqlTable table, IEnumerable<SqlField> fields, ISqlExpression[] tableArguments) : this()
		{
			Alias              = table.Alias;
			Database           = table.Database;
			Owner              = table.Owner;
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

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region Public Members

		public SqlField this[string fieldName]
		{
			get
			{
				SqlField field;
				Fields.TryGetValue(fieldName, out field);
				return field;
			}
		}

		public string Name         { get; set; }
		public string Alias        { get; set; }
		public string Database     { get; set; }
		public string Owner        { get; set; }
		public Type   ObjectType   { get; set; }
		public string PhysicalName { get; set; }

		private SqlTableType _sqlTableType = SqlTableType.Table;
		public  SqlTableType  SqlTableType { get { return _sqlTableType; } set { _sqlTableType = value; } }

		public ISqlExpression[] TableArguments { get; set; }

		readonly Dictionary<string,SqlField> _fields;
		public   Dictionary<string,SqlField>  Fields { get { return _fields; } }

		public SequenceNameAttribute[] SequenceAttributes { get; private set; }

		private SqlField _all;
		public  SqlField  All
		{
			get { return _all ?? (_all = new SqlField { Name = "*", PhysicalName = "*", Table = this }); }
		}

		public SqlField GetIdentityField()
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

			_fields.Add(field.Name, field);
		}

		public void AddRange(IEnumerable<SqlField> collection)
		{
			foreach (var item in collection)
				Add(item);
		}

		#endregion

		#region ISqlTableSource Members

		readonly int _sourceID;
		public   int  SourceID { get { return _sourceID; } }

		List<ISqlExpression> _keyFields;

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

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
			{
				var table = new SqlTable
				{
					Name               = Name,
					Alias              = Alias,
					Database           = Database,
					Owner              = Owner,
					PhysicalName       = PhysicalName,
					ObjectType         = ObjectType,
					SqlTableType       = SqlTableType,
					SequenceAttributes = SequenceAttributes,
				};

				table._fields.Clear();

				foreach (var field in _fields)
				{
					var fc = new SqlField(field.Value);

					objectTree.Add(field.Value, fc);
					table.     Add(fc);
				}

				if (TableArguments != null)
					TableArguments = TableArguments.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray();

				objectTree.Add(this, table);
				objectTree.Add(All,  table.All);

				clone = table;
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlTable; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return sb.Append(Name);
		}

		#endregion

		#region ISqlExpression Members

		bool ISqlExpression.CanBeNull()
		{
			return true;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		int ISqlExpression.Precedence
		{
			get { return Precedence.Unknown; }
		}

		Type ISqlExpression.SystemType
		{
			get { return ObjectType; }
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return this == other;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (TableArguments != null)
				for (var i = 0; i < TableArguments.Length; i++)
					TableArguments[i] = TableArguments[i].Walk(skipColumns, func);

			return func(this);
		}

		#endregion
	}
}
