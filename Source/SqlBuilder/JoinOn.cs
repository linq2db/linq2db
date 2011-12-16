using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB.Reflection.Extension;

namespace LinqToDB.SqlBuilder
{
	public class JoinOn : IQueryElement, ICloneableElement
	{
		public JoinOn()
		{
		}

		public JoinOn(string field, string otherField)
		{
			_field      = field;
			_otherField = otherField;
		}

		public JoinOn(string field, string otherField, string expression)
		{
			_field      = field;
			_otherField = otherField;
			_expression = expression;
		}

		public JoinOn(AttributeExtension ext)
		{
			_field      = (string)ext["Field"];
			_otherField = (string)ext["OtherField"];
			_expression = (string)ext["Expression"];
		}

		private string _field;
		public  string  Field { get { return _field; } set { _field = value; } }

		private string _otherField;
		public  string  OtherField { get { return _otherField; } set { _otherField = value; } }

		private string _expression;
		public  string  Expression { get { return _expression; } set { _expression = value; } }

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.JoinOn; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return sb;
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
				objectTree.Add(this, clone = new JoinOn(_field, _otherField, _expression));

			return clone;
		}

		#endregion
	}
}
