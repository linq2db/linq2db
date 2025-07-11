using System;

using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Mapping
{
	sealed class LockedMappingSchemaInfo : MappingSchemaInfo, IEquatable<LockedMappingSchemaInfo>
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
			return IdentifierBuilder.CreateNextID();
		}

		public override void ResetID()
		{
			if (_isLocked)
				throw new LinqToDBException($"MappingSchema '{_mappingSchema.GetType()}' is locked.");
			base.ResetID();
		}

		public bool Equals(LockedMappingSchemaInfo? other)
		{
			if (other is null)                return false;
			if (ReferenceEquals(this, other)) return true;

			return _mappingSchema.GetType() == other._mappingSchema.GetType();
	}

		public override bool Equals(object? obj)
		{
			if (obj is null)                return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((LockedMappingSchemaInfo)obj);
}

		public override int GetHashCode()
		{
			return _mappingSchema.GetType().GetHashCode();
		}
	}
}
