using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB.Reflection.Extension;

namespace LinqToDB.Sql
{
	public class Join : IQueryElement, ICloneableElement
	{
		public Join()
		{
		}

		public Join(string tableName, params JoinOn[] joinOns)
			: this(tableName, null, joinOns)
		{
		}

		public Join(string tableName, string alias, params JoinOn[] joinOns)
		{
			_tableName = tableName;
			_alias     = alias;
			_joinOns.AddRange(joinOns);
		}

		public Join(string tableName, string alias, IEnumerable<JoinOn> joinOns)
		{
			_tableName = tableName;
			_alias     = alias;
			_joinOns.AddRange(joinOns);
		}

		public Join(AttributeExtension ext)
		{
			_tableName = (string)ext["TableName"];
			_alias     = (string)ext["Alias"];

			var col = ext.Attributes["On"];

			foreach (AttributeExtension ae in col)
				_joinOns.Add(new JoinOn(ae));
		}

		private string _tableName;
		public  string  TableName { get { return _tableName; } set { _tableName = value; } }

		private string _alias;
		public  string  Alias { get { return _alias; } set { _alias = value; } }

		readonly List<JoinOn> _joinOns = new List<JoinOn>();
		public   List<JoinOn>  JoinOns
		{
			get { return _joinOns; }
		}

		public Join Clone()
		{
			var join = new Join(_tableName, _alias);

			foreach (var on in JoinOns)
				join.JoinOns.Add(new JoinOn(on.Field, on.OtherField, on.Expression));

			return join;
		}

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.Join; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return sb;
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
				objectTree.Add(this, clone = new Join(
					_tableName,
					_alias,
					_joinOns.ConvertAll(j => (JoinOn)j.Clone(objectTree, doClone))));

			return clone;
		}

		#endregion
	}
}
