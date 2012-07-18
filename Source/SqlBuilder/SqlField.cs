using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlBuilder
{
	using Mapping;

	public class SqlField : IChild<ISqlTableSource>, ISqlExpression
	{
		public SqlField()
		{
		}

		public SqlField(SqlField field)
			: this(field.SystemType, field.Name, field.PhysicalName, field.Nullable, field.PrimaryKeyOrder, field._nonUpdatableAttribute, field.MemberMapper)
		{
		}

		public SqlField(
			Type                  systemType,
			string                name,
			string                physicalName,
			bool                  nullable,
			int                   pkOrder,
			NonUpdatableAttribute nonUpdatableAttribute,
			MemberMapper          memberMapper)
		{
			SystemType             = systemType;
			Alias                  = name.Replace('.', '_');
			Name                   = name;
			Nullable               = nullable;
			PrimaryKeyOrder        = pkOrder;
			_memberMapper          = memberMapper;
			_physicalName          = physicalName;
			_nonUpdatableAttribute = nonUpdatableAttribute;
		}

		public Type            SystemType      { get; set; }
		public string          Alias           { get; set; }
		public string          Name            { get; set; }
		public bool            Nullable        { get; set; }
		public int             PrimaryKeyOrder { get; set; }
		public ISqlTableSource Table           { get; private set; }

		readonly MemberMapper _memberMapper;
		public   MemberMapper  MemberMapper
		{
			get { return _memberMapper; }
		}

		private string _physicalName;
		public  string  PhysicalName
		{
			get { return _physicalName ?? Name; }
			set { _physicalName = value; }
		}

		public bool IsIdentity   { get { return _nonUpdatableAttribute != null && _nonUpdatableAttribute.IsIdentity; } }
		public bool IsInsertable { get { return _nonUpdatableAttribute == null || !_nonUpdatableAttribute.OnInsert;  } }
		public bool IsUpdatable  { get { return _nonUpdatableAttribute == null || !_nonUpdatableAttribute.OnUpdate;  } }

		public bool IsPrimaryKey { get { return PrimaryKeyOrder != int.MinValue; } }

		readonly NonUpdatableAttribute  _nonUpdatableAttribute;

		ISqlTableSource IChild<ISqlTableSource>.Parent { get { return Table; } set { Table = value; } }

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull()
		{
			return Nullable;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		public int Precedence
		{
			get { return SqlBuilder.Precedence.Primary; }
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
