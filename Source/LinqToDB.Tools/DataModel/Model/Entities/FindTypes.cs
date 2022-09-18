using System;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Defines which Find method signatures should be generated.
	/// </summary>
	[Flags]
	public enum FindTypes
	{
		/// <summary>
		/// Generate no Find methods.
		/// </summary>
		None,
		/// <summary>
		/// Method version: sync Find().
		/// </summary>
		Sync                       = 0x0001,
		/// <summary>
		/// Method version: async FindAsync().
		/// </summary>
		Async                      = 0x0002,
		/// <summary>
		/// Method version: FindQuery().
		/// </summary>
		Query                      = 0x0004,
		/// <summary>
		/// Method primary key: from parameters.
		/// </summary>
		ByPrimaryKey               = 0x0010,
		/// <summary>
		/// Method primary key: from entity object.
		/// </summary>
		ByEntity                   = 0x0020,
		/// <summary>
		/// Method extends: entity table.
		/// </summary>
		OnTable                    = 0x0100,
		/// <summary>
		/// Method extends: generated context.
		/// </summary>
		OnContext                  = 0x0200,

		/// <summary>
		/// Generate Find extension method on <see cref="ITable{T}"/> object with primary key fields as parameters.
		/// </summary>
		FindByPkOnTable            = Sync | ByPrimaryKey | OnTable,
		/// <summary>
		/// Generate FindAsync extension method on <see cref="ITable{T}"/> object with primary key fields as parameters.
		/// </summary>
		FindAsyncByPkOnTable       = Async | ByPrimaryKey | OnTable,
		/// <summary>
		/// Generate FindQuery extension method on <see cref="ITable{T}"/> object with primary key fields as parameters.
		/// </summary>
		FindQueryByPkOnTable       = Query | ByPrimaryKey | OnTable,
		/// <summary>
		/// Generate Find extension method on <see cref="ITable{T}"/> object with entity object as parameter.
		/// </summary>
		FindByRecordOnTable        = Sync | ByEntity | OnTable,
		/// <summary>
		/// Generate FindAsync extension method on <see cref="ITable{T}"/> object with entity object as parameter.
		/// </summary>
		FindAsyncByRecordOnTable   = Async | ByEntity | OnTable,
		/// <summary>
		/// Generate FindQuery extension method on <see cref="ITable{T}"/> object with entity object as parameter.
		/// </summary>
		FindQueryByRecordOnTable   = Query | ByEntity | OnTable,
		/// <summary>
		/// Generate extension method on generated context object with primary key fields as parameters.
		/// </summary>
		FindByPkOnContext          = Sync | ByPrimaryKey | OnContext,
		/// <summary>
		/// Generate extension method on generated context object with primary key fields as parameters.
		/// </summary>
		FindAsyncByPkOnContext     = Async | ByPrimaryKey | OnContext,
		/// <summary>
		/// Generate extension method on generated context object with primary key fields as parameters.
		/// </summary>
		FindQueryByPkOnContext     = Query | ByPrimaryKey | OnContext,
		/// <summary>
		/// Generate extension method on generated context object with entity object as parameter.
		/// </summary>
		FindByRecordOnContext      = Sync | ByEntity | OnContext,
		/// <summary>
		/// Generate extension method on generated context object with entity object as parameter.
		/// </summary>
		FindAsyncByRecordOnContext = Async | ByEntity | OnContext,
		/// <summary>
		/// Generate extension method on generated context object with entity object as parameter.
		/// </summary>
		FindQueryByRecordOnContext = Query | ByEntity | OnContext,
	}
}
