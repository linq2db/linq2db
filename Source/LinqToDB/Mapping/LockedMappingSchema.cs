using System;
using System.Collections.Generic;

using LinqToDB.Common.Internal;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Locked mapping schema.
	/// </summary>
	public abstract class LockedMappingSchema : MappingSchema
	{
		protected LockedMappingSchema(string configuration, params MappingSchema[] schemas)
			: base(configuration, schemas)
		{
		}

		internal LockedMappingSchema(MappingSchemaInfo mappingSchemaInfo) : base(mappingSchemaInfo)
		{
		}

		static Dictionary<Type,int> _configurationIDs = new ();

		protected internal override int GenerateID()
		{
			lock (_configurationIDs)
			{
				var key = GetType();

				if (_configurationIDs.TryGetValue(key, out var id))
					return id;

				id = IdentifierBuilder.CreateNextID();

				_configurationIDs.Add(key, id);

				return id;
			}
		}

		internal override MappingSchemaInfo CreateMappingSchemaInfo(string configuration, MappingSchema mappingSchema)
		{
			return new LockedMappingSchemaInfo(configuration, mappingSchema);
		}

		public override bool IsLockable => true;
		public override bool IsLocked   => Schemas[0].IsLocked;
	}
}
