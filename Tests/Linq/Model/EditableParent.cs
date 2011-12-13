using System;

using LinqToDB.DataAccess;
using LinqToDB.EditableObjects;

namespace Data.Linq.Model
{
	[TableName("Parent")]
	public abstract class EditableParent : EditableObject<EditableParent>
	{
		public abstract int  ParentID { get; set; }
		public abstract int? Value1   { get; set; }
	}
}
