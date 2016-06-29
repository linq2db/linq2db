using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Mapping;

	public class SqlField : ISqlExpression
	{
		public SqlField()
		{
			CanBeNull = true;
		}

		public SqlField(SqlField field)
		{
			SystemType       = field.SystemType;
			Alias            = field.Alias;
			Name             = field.Name;
			PhysicalName     = field.PhysicalName;
			CanBeNull        = field.CanBeNull;
			IsPrimaryKey     = field.IsPrimaryKey;
			PrimaryKeyOrder  = field.PrimaryKeyOrder;
			IsIdentity       = field.IsIdentity;
			IsInsertable     = field.IsInsertable;
			IsUpdatable      = field.IsUpdatable;
			DataType         = field.DataType;
			DbType           = field.DbType;
			Length           = field.Length;
			Precision        = field.Precision;
			Scale            = field.Scale;
			CreateFormat     = field.CreateFormat;
			ColumnDescriptor = field.ColumnDescriptor;
		}

		public Type             SystemType       { get; set; }
		public string           Alias            { get; set; }
		public string           Name             { get; set; }
		public bool             IsPrimaryKey     { get; set; }
		public int              PrimaryKeyOrder  { get; set; }
		public bool             IsIdentity       { get; set; }
		public bool             IsInsertable     { get; set; }
		public bool             IsUpdatable      { get; set; }
		public DataType         DataType         { get; set; }
		public string           DbType           { get; set; }
		public int?             Length           { get; set; }
		public int?             Precision        { get; set; }
		public int?             Scale            { get; set; }
		public string           CreateFormat     { get; set; }

		public ISqlTableSource  Table            { get; set; }
		public ColumnDescriptor ColumnDescriptor { get; set; }

		private string _physicalName;
		public  string  PhysicalName
		{
			get { return _physicalName ?? Name; }
			set { _physicalName = value; }
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull { get; set; }

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		public int Precedence
		{
			get { return PrecedenceLevel.Primary; }
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return this == other;
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			Table.Clone(objectTree, doClone);
			return objectTree[this];
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlField; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return sb
				.Append('t')
				.Append(Table.SourceID)
				.Append('.')
				.Append(Name);
		}

		#endregion
	}
}
