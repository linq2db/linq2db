using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Data.Sql
{
	public interface IQueryElement //: ICloneableElement
	{
		QueryElementType ElementType { get; }
		StringBuilder    ToString (StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic);
	}
}
