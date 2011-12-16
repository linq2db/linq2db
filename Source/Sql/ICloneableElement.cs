using System;
using System.Collections.Generic;

namespace LinqToDB.Sql
{
	public interface ICloneableElement
	{
		ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone);
	}
}
