using System;

namespace LinqToDB
{
	/// <summary>
	/// Associates a class with a column in a database table.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public class ColumnAttribute : Attribute
	{
		public ColumnAttribute()
		{
			IsColumn        = true;
			PrimaryKeyOrder = int.MinValue;
		}

		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets the name of a column.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the type of the database column.
		/// </summary>
		public DataType DataType { get; set; }

		/// <summary>
		/// Gets or sets the name of the database column type.
		/// </summary>
		public string DbType { get; set; }

		/// <summary>
		/// Use NonColumnAttribute instead.
		/// </summary>
		public bool IsColumn { get; set; }

		/// <summary>
		/// Gets or sets a private storage field to hold the value from a column.
		/// </summary>
		public string Storage { get; set; }

		/// <summary>
		/// Gets or sets whether a column contains a discriminator value for a LINQ to DB inheritance hierarchy.
		/// </summary>
		public bool IsDiscriminator { get; set; }

		/// <summary>
		/// Gets or sets whether a column is insertable.
		/// </summary>
		private bool? _skipOnInsert;
		public  bool   SkipOnInsert
		{
			get { return _skipOnInsert ?? false; }
			set { _skipOnInsert = value;         }
		}

		public bool? GetSkipOnInsert()
		{
			return _skipOnInsert;
		}

		/// <summary>
		/// Gets or sets whether a column is updatable.
		/// </summary>
		private bool? _skipOnUpdate;
		public  bool   SkipOnUpdate
		{
			get { return _skipOnUpdate ?? false; }
			set { _skipOnUpdate = value;         }
		}

		public bool? GetSkipOnUpdate()
		{
			return _skipOnUpdate;
		}

		/// <summary>
		/// Gets or sets whether a column contains values that the database auto-generates.
		/// </summary>
		public bool IsIdentity { get; set; }

		/// <summary>
		/// Gets or sets whether this class member represents a column that is part or all of the primary key of the table.
		/// </summary>
		public bool IsPrimaryKey { get; set; }

		/// <summary>
		/// Gets or sets the Primary Key order.
		/// </summary>
		public int PrimaryKeyOrder { get; set; }

		/// <summary>
		/// Gets or sets whether a column can contain null values.
		/// </summary>
		private bool? _canBeNull;
		public  bool   CanBeNull
		{
			get { return _canBeNull ?? true; }
			set { _canBeNull = value;        }
		}

		public bool? GetCanBeNull()
		{
			return _canBeNull;
		}
	}
}
