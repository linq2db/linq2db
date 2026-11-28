using System;
using System.Reflection;
using System.Runtime.Serialization;

using LinqToDB.Mapping;

namespace Tests.Linq.Helpers.UnionJoinDtos
{
	public interface IModifiedTimeStamp;

	public class BasicDTO : BasicDTOwithoutID, IMccEntityState, IInterlinqDTO, IDTOWithId
	{
		[PrimaryKey] public Guid Id { get; set; }
	}

	[DefaultMember("Item")]
	public class BasicDTOwithExtensionData : BasicDTO, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData;

	[KnownType("KnownTypes")]
	public class BasicDTOwithoutID : IMccEntityState, IInterlinqDTO;

	public interface ICreatedTimeStamp;

	public interface IDTOWithId;

	public interface IHasArchiveTable;

	public interface IInterlinqDTO;

	public interface IMccEntityState;

	[DefaultMember("Item")]
	public interface IExtensionData;

	[DataContract(Name = "WMS_GlobalTaskDTO")]
	public class UnionJoinGlobalTask : WmsBasicDTO<UnionJoinGlobalTask>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp, IHasArchiveTable
	{
		public UnionJoinGlobalTask()
		{
			throw new NotImplementedException();
		}

		public UnionJoinGlobalTask(UnionJoinGlobalTask dto)
		{
			throw new NotImplementedException();
		}

		[DataMember]
		public Guid ResourceID { get; set; }

		[DataMember]
		public Guid? StorageShelfSourceID { get; set; }

		[DataMember]
		public Guid? RPSourceID { get; set; }

		[DataMember]
		public Guid? StorageShelfDestinationID { get; set; }

		[DataMember]
		public Guid? RPDestinationID { get; set; }

		[DataMember]
		public Guid? RPOrigDestinationID { get; set; }

		[DataMember]
		public Guid? OutfeedTransportOrderID { get; set; }
	}

	[DataContract(Name = "WMS_OutfeedTransportOrderDTO")]
	public class UnionJoinOutfeedOrder : WmsBasicDTO<UnionJoinOutfeedOrder>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp, IHasArchiveTable
	{
		public UnionJoinOutfeedOrder()
		{
			throw new NotImplementedException();
		}

		public UnionJoinOutfeedOrder(UnionJoinOutfeedOrder dto)
		{
			throw new NotImplementedException();
		}
	}

	[DataContract(Name = "WMS_StorageShelfDTO")]
	public class UnionJoinStorageShelf : BasicDTOwithExtensionData, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp;

	[DataContract(Name = "WMS_BasicDTO")]
	public abstract class WmsBasicDTO<T> : WmsBasicWithoutCustomFieldsDTO<T>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp
	{
		protected WmsBasicDTO()
		{
			throw new NotImplementedException();
		}

		protected WmsBasicDTO(T dto)
		{
			throw new NotImplementedException();
		}
	}

	[DataContract(Name = "WMS_BasicDTO")]
	public abstract class WmsBasicWithoutCustomFieldsDTO<T> : BasicDTOwithExtensionData, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp
	{
		protected WmsBasicWithoutCustomFieldsDTO()
		{
			throw new NotImplementedException();
		}

		protected WmsBasicWithoutCustomFieldsDTO(T dto)
		{
			throw new NotImplementedException();
		}
	}

	[DataContract(Name = "WMS_GlobalTaskCombinedDTO")]
	public class UnionJoinCombined : IInterlinqDTO
	{
		[DataMember]
		public UnionJoinGlobalTask? GlobalTask { get; set; }

		[DataMember]
		public UnionJoinLoadCarrier? LoadCarrier { get; set; }

		[DataMember]
		public UnionJoinResourcePoint? Source { get; set; }

		[DataMember]
		public UnionJoinStorageShelf? SourceShelf { get; set; }

		[DataMember]
		public UnionJoinResourcePoint? Destination { get; set; }

		[DataMember]
		public UnionJoinStorageShelf? DestinationShelf { get; set; }

		[DataMember]
		public UnionJoinResourcePoint? OriginDestination { get; set; }

		[DataMember]
		public UnionJoinOutfeedOrder? OutfeedTransportOrder { get; set; }
	}

	[DataContract(Name = "WMS_ResourceDTO")]
	public class UnionJoinLoadCarrier : WmsBasicDTO<UnionJoinLoadCarrier>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp, IHasArchiveTable
	{
		public UnionJoinLoadCarrier()
		{
			throw new NotImplementedException();
		}

		public UnionJoinLoadCarrier(UnionJoinLoadCarrier dto)
		{
			throw new NotImplementedException();
		}
	}

	[DataContract(Name = "WMS_ResourcePointDTO")]
	public class UnionJoinResourcePoint : BasicDTOwithExtensionData, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData;
}
