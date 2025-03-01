using LinqToDB.Mapping;

namespace LinqToDB.Internal.Mapping
{
	sealed class LockedMappingSchemaInfo : MappingSchemaInfo
	{
		public LockedMappingSchemaInfo(string configuration, MappingSchema mappingSchema) : base(configuration)
		{
			_mappingSchema = mappingSchema;
		}

		readonly MappingSchema _mappingSchema;
		bool                   _isLocked;

		public override bool IsLocked => _isLocked;

		protected override int GenerateID()
		{
			_isLocked = true;
			return _mappingSchema.GenerateID();
		}

		public override void ResetID()
		{
			if (_isLocked)
				throw new LinqToDBException($"MappingSchema '{_mappingSchema.GetType()}' is locked.");
			base.ResetID();
		}
	}
}
