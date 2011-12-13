using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.Data.Sql
{
	using DataAccess;
	using Mapping;
	using Reflection.Extension;
	using SqlProvider;

	public class SqlTable : ISqlTableSource
	{
		#region Init

		public SqlTable()
		{
			_sourceID = Interlocked.Increment(ref SqlQuery.SourceIDCounter);
			_fields   = new ChildContainer<ISqlTableSource,SqlField>(this);
		}

		internal SqlTable(
			int id, string name, string alias, string database, string owner, string physicalName, Type objectType,
			SequenceNameAttribute[] sequenceAttributes,
			SqlField[] fields,
			SqlTableType sqlTableType, ISqlExpression[] tableArguments)
		{
			_sourceID           = id;
			Name                = name;
			Alias               = alias;
			Database            = database;
			Owner               = owner;
			PhysicalName        = physicalName;
			ObjectType          = objectType;
			_sequenceAttributes = sequenceAttributes;

			_fields  = new ChildContainer<ISqlTableSource,SqlField>(this);
			_fields.AddRange(fields);

			foreach (var field in fields)
			{
				if (field.Name == "*")
				{
					_all = field;
					_fields.Remove("*");
					((IChild<ISqlTableSource>)_all).Parent = this;
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

			bool isSet;
			Database     = mappingSchema.MetadataProvider.GetDatabaseName(objectType, mappingSchema.Extensions, out isSet);
			Owner        = mappingSchema.MetadataProvider.GetOwnerName   (objectType, mappingSchema.Extensions, out isSet);
			Name         = mappingSchema.MetadataProvider.GetTableName   (objectType, mappingSchema.Extensions, out isSet);
			ObjectType   = objectType;
			PhysicalName = Name;

			var typeExt = TypeExtension.GetTypeExtension(objectType, mappingSchema.Extensions);

			foreach (MemberMapper mm in mappingSchema.GetObjectMapper(objectType))
				if (mm.MapMemberInfo.SqlIgnore == false)
				{
					var ua =
						mappingSchema.MetadataProvider.GetNonUpdatableAttribute(objectType, typeExt, mm.MapMemberInfo.MemberAccessor, out isSet);

					var order = mappingSchema.MetadataProvider.GetPrimaryKeyOrder(objectType, typeExt, mm.MapMemberInfo.MemberAccessor, out isSet);

					Fields.Add(new SqlField(
						mm.Type,
						mm.MemberName,
						mm.Name,
						mm.MapMemberInfo.Nullable,
						isSet ? order : int.MinValue,
						ua,
						mm));
				}

			var identityField = GetIdentityField();

			if (identityField != null)
			{
				var om = mappingSchema.GetObjectMapper(ObjectType);
				var mm = om[identityField.Name, true];

				_sequenceAttributes = mm.MapMemberInfo.MemberAccessor.GetAttributes<SequenceNameAttribute>();
			}
		}

		public SqlTable(Type objectType)
			: this(Map.DefaultSchema, objectType)
		{
		}

		#endregion

		#region Init from Table

		public SqlTable(SqlTable table) : this()
		{
			Alias               = table.Alias;
			Database            = table.Database;
			Owner               = table.Owner;
			Name                = table.Name;
			PhysicalName        = table.PhysicalName;
			ObjectType          = table.ObjectType;
			_sequenceAttributes = table._sequenceAttributes;

			foreach (var field in table.Fields.Values)
				Fields.Add(new SqlField(field));

			foreach (var join in table.Joins)
				Joins.Add(join.Clone());

			SqlTableType   = table.SqlTableType;
			TableArguments = table.TableArguments;
		}

		public SqlTable(SqlTable table, IEnumerable<SqlField> fields, IEnumerable<Join> joins, ISqlExpression[] tableArguments) : this()
		{
			Alias               = table.Alias;
			Database            = table.Database;
			Owner               = table.Owner;
			Name                = table.Name;
			PhysicalName        = table.PhysicalName;
			ObjectType          = table.ObjectType;
			_sequenceAttributes = table._sequenceAttributes;

			Fields.AddRange(fields);
			Joins. AddRange(joins);

			SqlTableType   = table.SqlTableType;
			TableArguments = tableArguments;
		}

		#endregion

		#region Init from XML

		public SqlTable(ExtensionList extensions, string name)
			: this(Map.DefaultSchema, extensions, name)
		{
		}

		public SqlTable([JetBrains.Annotations.NotNull] MappingSchema mappingSchema, ExtensionList extensions, string name) : this()
		{
			if (mappingSchema == null) throw new ArgumentNullException("mappingSchema");
			if (extensions    == null) throw new ArgumentNullException("extensions");
			if (name          == null) throw new ArgumentNullException("name");

			var te = extensions[name];

			if (te == TypeExtension.Null)
				throw new ArgumentException(string.Format("Table '{0}' not found.", name));

			Name         = te.Name;
			Alias        = (string)te.Attributes["Alias"].       Value;
			Database     = (string)te.Attributes["Database"].    Value;
			Owner        = (string)te.Attributes["Owner"].       Value;
			PhysicalName = (string)te.Attributes["PhysicalName"].Value ?? te.Name;

			foreach (var me in te.Members.Values)
				Fields.Add(new SqlField(
					(Type)me["Type"].Value,
					me.Name,
					(string)me["MapField"].Value ?? (string)me["PhysicalName"].Value,
					(bool?)me["Nullable"].Value ?? false,
					-1,
					(bool?)me["Identity"].Value == true ? new IdentityAttribute() : null,
					null));

			foreach (var ae in te.Attributes["Join"])
				Joins.Add(new Join(ae));

			var baseExtension = (string)te.Attributes["BaseExtension"].Value;

			if (!string.IsNullOrEmpty(baseExtension))
				InitFromBase(new SqlTable(mappingSchema, extensions, baseExtension));

			var baseTypeName = (string)te.Attributes["BaseType"].Value;

			if (!string.IsNullOrEmpty(baseTypeName))
				InitFromBase(new SqlTable(mappingSchema, Type.GetType(baseTypeName, true, true)));
		}

		void InitFromBase(SqlTable baseTable)
		{
			if (Alias        == null) Alias        = baseTable.Alias;
			if (Database     == null) Database     = baseTable.Database;
			if (Owner        == null) Owner        = baseTable.Owner;
			if (PhysicalName == null) PhysicalName = baseTable.PhysicalName;

			foreach (var field in baseTable.Fields.Values)
				if (!Fields.ContainsKey(field.Name))
					Fields.Add(new SqlField(field));

			foreach (var join in baseTable.Joins)
				if (Joins.FirstOrDefault(j => j.TableName == join.TableName) == null)
					Joins.Add(join);
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

		readonly ChildContainer<ISqlTableSource,SqlField> _fields;
		public   ChildContainer<ISqlTableSource,SqlField>  Fields { get { return _fields; } }

		readonly List<Join> _joins = new List<Join>();
		public   List<Join>  Joins { get { return _joins; } }

		private SequenceNameAttribute[] _sequenceAttributes;
		public  SequenceNameAttribute[]  SequenceAttributes
		{
			get { return _sequenceAttributes; }
		}

		private SqlField _all;
		public  SqlField  All
		{
			get
			{
				if (_all == null)
				{
					_all = new SqlField(null, "*", "*", true, -1, null, null);
					((IChild<ISqlTableSource>)_all).Parent = this;
				}

				return _all;
			}
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
					Name                = Name,
					Alias               = Alias,
					Database            = Database,
					Owner               = Owner,
					PhysicalName        = PhysicalName,
					ObjectType          = ObjectType,
					SqlTableType        = SqlTableType,
					_sequenceAttributes = _sequenceAttributes,
				};

				table._fields.Clear();

				foreach (var field in _fields)
				{
					var fc = new SqlField(field.Value);

					objectTree.   Add(field.Value, fc);
					table._fields.Add(field.Key,   fc);
				}

				table._joins.AddRange(_joins.ConvertAll(j => j.Clone()));

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
