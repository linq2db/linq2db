using System;

using LinqToDB.Model;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlField : SqlExpressionBase
	{
		internal static SqlField All(ISqlTableSource table)
		{
			return new SqlField(table, "*", "*");
		}

		protected SqlField()
		{

		}

		public SqlField(ISqlTableSource table, string name) : this()
		{
			Table     = table;
			Name      = name;
			CanBeNull = true;
		}

		public SqlField(DbDataType dbDataType, string? name, bool canBeNull) : this()
		{
			Type      = dbDataType;
			Name      = name!;
			CanBeNull = canBeNull;
		}

		SqlField(ISqlTableSource table, string name, string physicalName) : this()
		{
			Table        = table;
			Name         = name;
			PhysicalName = physicalName;
			CanBeNull    = true;
		}

		public SqlField(string name, string physicalName) : this()
		{
			Name         = name;
			PhysicalName = physicalName;
			CanBeNull    = true;
		}

		public SqlField(SqlField field) : this()
		{
			Type             = field.Type;
			Alias            = field.Alias;
			Name             = field.Name;
			PhysicalName     = field.PhysicalName;
			CanBeNull        = field.CanBeNull;
			IsPrimaryKey     = field.IsPrimaryKey;
			PrimaryKeyOrder  = field.PrimaryKeyOrder;
			IsIdentity       = field.IsIdentity;
			IsInsertable     = field.IsInsertable;
			IsUpdatable      = field.IsUpdatable;
			CreateFormat     = field.CreateFormat;
			CreateOrder      = field.CreateOrder;
			ColumnDescriptor = field.ColumnDescriptor;
			IsDynamic        = field.IsDynamic;
		}

		public SqlField(ColumnDescriptor column) : this()
		{
			Type              = column.GetDbDataType(true);
			Name              = column.MemberName;
			PhysicalName      = column.ColumnName;
			CanBeNull         = column.CanBeNull;
			IsPrimaryKey      = column.IsPrimaryKey;
			PrimaryKeyOrder   = column.PrimaryKeyOrder;
			IsIdentity        = column.IsIdentity;
			IsInsertable      = !column.SkipOnInsert;
			IsUpdatable       = !column.SkipOnUpdate;
			SkipOnEntityFetch = column.SkipOnEntityFetch;
			CreateFormat      = column.CreateFormat;
			CreateOrder       = column.Order;
			ColumnDescriptor  = column;
		}

		public DbDataType        Type              { get; set; }
		public string?           Alias             { get; set; }
		public string            Name              { get; set; } = null!; // not always true, see ColumnDescriptor notes
		public bool              IsPrimaryKey      { get; set; }
		public int               PrimaryKeyOrder   { get; set; }
		public bool              IsIdentity        { get; set; }
		public bool              IsInsertable      { get; set; }
		public bool              IsUpdatable       { get; set; }
		public bool              IsDynamic         { get; set; }
		public bool              SkipOnEntityFetch { get; set; }
		public string?           CreateFormat      { get; set; }
		public int?              CreateOrder       { get; set; }

		public SqlField? BasedOn { get; set; }

		public ISqlTableSource?  Table             { get; set; }
		public ColumnDescriptor  ColumnDescriptor  { get; set; } = null!; // TODO: not true, we probably should introduce something else for non-column fields

		public override Type SystemType => Type.SystemType;

		string? _physicalName;
		public  string   PhysicalName
		{
			get => _physicalName ?? Name;
			set => _physicalName = value;
		}

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability) => nullability.CanBeNull(this);

		public bool CanBeNull { get; set; }

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		public override int Precedence => LinqToDB.SqlQuery.Precedence.Primary;

		#endregion

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlField;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			// writer.DebugAppendUniqueId(this);

			if (Table != null)
				writer
					.Append('t')
					.Append(Table.SourceID)
					.Append('.');

			writer.Append(Name);
			if (CanBeNull)
				writer.Append("?");
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(Name);
			hash.Add(PhysicalName);
			hash.Add(Alias);
			hash.Add(CanBeNull);
			hash.Add(IsPrimaryKey);
			hash.Add(PrimaryKeyOrder);
			hash.Add(IsIdentity);
			hash.Add(IsInsertable);
			hash.Add(IsUpdatable);
			hash.Add(CreateFormat);
			hash.Add(CreateOrder);
			return hash.ToHashCode();
		}

		#endregion

		internal static SqlField FakeField(DbDataType dataType, string fieldName, bool canBeNull)
		{
			var field = new SqlField(fieldName, fieldName);
			field.Type = dataType;
			return field;
		}

		public void Assign(SqlField source)
		{
			Type             = source.Type;
			Alias            = source.Alias;
			Name             = source.Name;
			PhysicalName     = source.PhysicalName;
			CanBeNull        = source.CanBeNull;
			IsPrimaryKey     = source.IsPrimaryKey;
			PrimaryKeyOrder  = source.PrimaryKeyOrder;
			IsIdentity       = source.IsIdentity;
			IsInsertable     = source.IsInsertable;
			IsUpdatable      = source.IsUpdatable;
			CreateFormat     = source.CreateFormat;
			CreateOrder      = source.CreateOrder;
			IsDynamic        = source.IsDynamic;
		}
	}
}
