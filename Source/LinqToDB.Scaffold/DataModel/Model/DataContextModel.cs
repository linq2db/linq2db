using System.Collections.Generic;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Main database context descriptor. Also contains data model for current/default database schema.
	/// </summary>
	public sealed class DataContextModel : SchemaModelBase
	{
		public DataContextModel(ClassModel classModel)
		{
			Class = classModel;
		}

		/// <summary>
		/// Context class descriptor.
		/// </summary>
		public ClassModel             Class                                    { get; set; }
		/// <summary>
		/// Enables generation of default constructor.
		/// </summary>
		public bool                   HasDefaultConstructor                    { get; set; }
		/// <summary>
		/// Enables generation of constructor with configuration name <see cref="string"/> parameter.
		/// </summary>
		public bool                   HasConfigurationConstructor              { get; set; }
		/// <summary>
		/// Enables generation of constructor with non-generic configuration options <see cref="DataOptions"/> parameter.
		/// </summary>
		public bool                   HasUntypedOptionsConstructor             { get; set; }
		/// <summary>
		/// Enables generation of constructor with generic configuration options <see cref="DataOptions{T}"/> parameter.
		/// </summary>
		public bool                   HasTypedOptionsConstructor               { get; set; }
		/// <summary>
		/// Contains all associations (relations) for data model.
		/// </summary>
		public List<AssociationModel> Associations                             { get;      } = new();
		/// <summary>
		/// Contains descriptors of addtional database schemas.
		/// </summary>
		public Dictionary<string,     AdditionalSchemaModel> AdditionalSchemas { get;      } = new(System.StringComparer.Ordinal);
	}
}
