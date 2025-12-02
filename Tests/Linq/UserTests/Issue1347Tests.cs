using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.UserTests.MCC.WMS.ServiceInterfaces.DTO;

namespace Tests.UserTests
{
	namespace MCC.Common.ServiceInterfaces.DTO
	{
		public interface IModifiedTimeStamp
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		public class BasicDTO : BasicDTOwithoutID, IMccEntityState, IInterlinqDTO, IDTOWithId
		{
			[PrimaryKey] public Guid Id { get; set; }
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		[DefaultMember("Item")]
		public class BasicDTOwithExtensionData : BasicDTO, IMccEntityState, IInterlinqDTO, IDTOWithId, Interfaces.IExtensionData
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		[KnownType("KnownTypes")]
		public class BasicDTOwithoutID : IMccEntityState, IInterlinqDTO
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		public interface ICreatedTimeStamp
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		public interface IDTOWithId
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		public interface IHasArchiveTable
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		public interface IInterlinqDTO
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.DTO.Base
	{
		public interface IMccEntityState
		{
		}
	}

	namespace MCC.Common.ServiceInterfaces.Interfaces
	{
		[DefaultMember("Item")]
		public interface IExtensionData
		{
		}
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_GlobalTaskDTO")]
		public class GlobalTaskDTO : WmsBasicDTO<GlobalTaskDTO>, Common.ServiceInterfaces.DTO.Base.IMccEntityState, Common.ServiceInterfaces.DTO.Base.IInterlinqDTO, Common.ServiceInterfaces.DTO.Base.IDTOWithId, Common.ServiceInterfaces.Interfaces.IExtensionData, Common.ServiceInterfaces.DTO.IModifiedTimeStamp, Common.ServiceInterfaces.DTO.Base.ICreatedTimeStamp, Common.ServiceInterfaces.DTO.Base.IHasArchiveTable
		{
			public GlobalTaskDTO()
			{
				throw new NotImplementedException();
			}

			public GlobalTaskDTO(GlobalTaskDTO dto)
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
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_OutfeedTransportOrderDTO")]
		public class OutfeedTransportOrderDTO : WmsBasicDTO<OutfeedTransportOrderDTO>, Common.ServiceInterfaces.DTO.Base.IMccEntityState, Common.ServiceInterfaces.DTO.Base.IInterlinqDTO, Common.ServiceInterfaces.DTO.Base.IDTOWithId, Common.ServiceInterfaces.Interfaces.IExtensionData, Common.ServiceInterfaces.DTO.IModifiedTimeStamp, Common.ServiceInterfaces.DTO.Base.ICreatedTimeStamp, Common.ServiceInterfaces.DTO.Base.IHasArchiveTable
		{
			public OutfeedTransportOrderDTO()
			{
				throw new NotImplementedException();
			}

			public OutfeedTransportOrderDTO(OutfeedTransportOrderDTO dto)
			{
				throw new NotImplementedException();
			}
		}
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_StorageShelfDTO")]
		public class StorageShelfDTO : Common.ServiceInterfaces.DTO.Base.BasicDTOwithExtensionData, Common.ServiceInterfaces.DTO.Base.IMccEntityState, Common.ServiceInterfaces.DTO.Base.IInterlinqDTO, Common.ServiceInterfaces.DTO.Base.IDTOWithId, Common.ServiceInterfaces.Interfaces.IExtensionData, Common.ServiceInterfaces.DTO.IModifiedTimeStamp
		{
		}
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_BasicDTO")]
		public abstract class WmsBasicDTO<T> : WmsBasicWithoutCustomFieldsDTO<T>, Common.ServiceInterfaces.DTO.Base.IMccEntityState, Common.ServiceInterfaces.DTO.Base.IInterlinqDTO, Common.ServiceInterfaces.DTO.Base.IDTOWithId, Common.ServiceInterfaces.Interfaces.IExtensionData, Common.ServiceInterfaces.DTO.IModifiedTimeStamp, Common.ServiceInterfaces.DTO.Base.ICreatedTimeStamp
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
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_BasicDTO")]
		public abstract class WmsBasicWithoutCustomFieldsDTO<T> : Common.ServiceInterfaces.DTO.Base.BasicDTOwithExtensionData, Common.ServiceInterfaces.DTO.Base.IMccEntityState, Common.ServiceInterfaces.DTO.Base.IInterlinqDTO, Common.ServiceInterfaces.DTO.Base.IDTOWithId, Common.ServiceInterfaces.Interfaces.IExtensionData, Common.ServiceInterfaces.DTO.IModifiedTimeStamp, Common.ServiceInterfaces.DTO.Base.ICreatedTimeStamp
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
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_GlobalTaskCombinedDTO")]
		public class WmsGlobalTaskCombinedDTO : Common.ServiceInterfaces.DTO.Base.IInterlinqDTO
		{
			[DataMember]
			public GlobalTaskDTO? GlobalTask { get; set; }

			[DataMember]
			public WmsLoadCarrierDTO? LoadCarrier { get; set; }

			[DataMember]
			public WmsResourcePointDTO? Source { get; set; }

			[DataMember]
			public StorageShelfDTO? SourceShelf { get; set; }

			[DataMember]
			public WmsResourcePointDTO? Destination { get; set; }

			[DataMember]
			public StorageShelfDTO? DestinationShelf { get; set; }

			[DataMember]
			public WmsResourcePointDTO? OriginDestination { get; set; }

			[DataMember]
			public OutfeedTransportOrderDTO? OutfeedTransportOrder { get; set; }
		}
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_ResourceDTO")]
		public class WmsLoadCarrierDTO : WmsBasicDTO<MCC.WMS.ServiceInterfaces.DTO.WmsLoadCarrierDTO>, Common.ServiceInterfaces.DTO.Base.IMccEntityState, Common.ServiceInterfaces.DTO.Base.IInterlinqDTO, Common.ServiceInterfaces.DTO.Base.IDTOWithId, Common.ServiceInterfaces.Interfaces.IExtensionData, Common.ServiceInterfaces.DTO.IModifiedTimeStamp, Common.ServiceInterfaces.DTO.Base.ICreatedTimeStamp, Common.ServiceInterfaces.DTO.Base.IHasArchiveTable
		{
			public WmsLoadCarrierDTO()
			{
				throw new NotImplementedException();
			}

			public WmsLoadCarrierDTO(WmsLoadCarrierDTO dto)
			{
				throw new NotImplementedException();
			}
		}
	}

	namespace MCC.WMS.ServiceInterfaces.DTO
	{
		[DataContract(Name = "WMS_ResourcePointDTO")]
		public class WmsResourcePointDTO : Common.ServiceInterfaces.DTO.Base.BasicDTOwithExtensionData, Common.ServiceInterfaces.DTO.Base.IMccEntityState, Common.ServiceInterfaces.DTO.Base.IInterlinqDTO, Common.ServiceInterfaces.DTO.Base.IDTOWithId, Common.ServiceInterfaces.Interfaces.IExtensionData
		{
		}
	}

	namespace Tests.UserTests
	{
		[TestFixture]
		public class Issue1347Tests : TestBase
		{
			[YdbTableNotFound]
			[Test]
			public void Test5([DataSources] string context)
			{
				using var db = GetDataContext(context);
				using var t1 = db.CreateLocalTable<GlobalTaskDTO>();
				using var t11 = db.CreateLocalTable<GlobalTaskDTO>("WMS_GlobalTaskA");
				using var t5 = db.CreateLocalTable<WmsLoadCarrierDTO>();
				using var t51 = db.CreateLocalTable<WmsLoadCarrierDTO>("WMS_LoadCarrierA");

				var gts = db.GetTable<GlobalTaskDTO>().Union(
						db.GetTable<GlobalTaskDTO>()
							.TableName(
								"WMS_GlobalTaskA"));

				var lcs = db.GetTable<WmsLoadCarrierDTO>().Union(
						db.GetTable<WmsLoadCarrierDTO>()
							.TableName(
								"WMS_LoadCarrierA"));

				var qry = from g in gts
						  join res in lcs on g.ResourceID equals res.Id into reslist
						  from res in reslist.DefaultIfEmpty()
						  select new WmsGlobalTaskCombinedDTO()
						  { GlobalTask = g, LoadCarrier = res };

				qry.ToArray();
			}

			[Test]
			public void Test4([DataSources] string context)
			{
				using var db = GetDataContext(context);
				using var t1 = db.CreateLocalTable<GlobalTaskDTO>();
				using var t11 = db.CreateLocalTable<GlobalTaskDTO>("WMS_GlobalTaskA");

				var gts = db.GetTable<GlobalTaskDTO>().Union(
						db.GetTable<GlobalTaskDTO>()
							.TableName(
								"WMS_GlobalTaskA"));

				var qry = from g in gts
						  join source1 in db.GetTable<WmsResourcePointDTO>() on g.RPSourceID equals source1.Id into
							source1List
						  select new WmsGlobalTaskCombinedDTO()
						  {
							  GlobalTask = g,
						  };

				qry.ToArray();
			}

			[Test]
			public void Test3([DataSources] string context)
			{
				using var db = GetDataContext(context);
				using var t1 = db.CreateLocalTable<GlobalTaskDTO>();
				using var t11 = db.CreateLocalTable<GlobalTaskDTO>("WMS_GlobalTaskA");
				using var t2 = db.CreateLocalTable<WmsResourcePointDTO>();
				using var t3 = db.CreateLocalTable<StorageShelfDTO>();

				var gts = db.GetTable<GlobalTaskDTO>().Union(
						db.GetTable<GlobalTaskDTO>()
							.TableName(
								"WMS_GlobalTaskA"));

				var qry = from g in gts
						  join source1 in db.GetTable<WmsResourcePointDTO>() on g.RPSourceID equals source1.Id into
							source1List
						  from source in source1List.DefaultIfEmpty()
						  join sourceShelf1 in db.GetTable<StorageShelfDTO>() on g.StorageShelfSourceID equals sourceShelf1
							.Id into sourceShelf1List
						  from sourceShelf in sourceShelf1List.DefaultIfEmpty()
						  join dest1 in db.GetTable<WmsResourcePointDTO>() on g.RPDestinationID equals dest1.Id into
							destList
						  from dest in destList.DefaultIfEmpty()
						  join destShelf1 in db.GetTable<StorageShelfDTO>() on g.StorageShelfDestinationID equals
							destShelf1.Id into destShelf1List
						  from destShelf in destShelf1List.DefaultIfEmpty()
						  join origdest1 in db.GetTable<WmsResourcePointDTO>() on g.RPOrigDestinationID equals origdest1.Id
							into origdestList
						  from origdest in origdestList.DefaultIfEmpty()
						  select new WmsGlobalTaskCombinedDTO()
						  {
							  GlobalTask = g,
							  Source = source,
							  SourceShelf = sourceShelf,
							  Destination = dest,
							  DestinationShelf = destShelf,
							  OriginDestination = origdest
						  };

				qry.ToArray();
			}

			[YdbTableNotFound]
			[Test]
			public void Test2([DataSources] string context)
			{
				using var db = GetDataContext(context);
				using var t1 = db.CreateLocalTable<GlobalTaskDTO>();
				using var t11 = db.CreateLocalTable<GlobalTaskDTO>("WMS_GlobalTaskA");
				using var t2 = db.CreateLocalTable<WmsResourcePointDTO>();
				using var t3 = db.CreateLocalTable<StorageShelfDTO>();
				using var t4 = db.CreateLocalTable<OutfeedTransportOrderDTO>();
				using var t41 = db.CreateLocalTable<OutfeedTransportOrderDTO>("WMS_OutfeedTransportOrderA");
				using var t5 = db.CreateLocalTable<WmsLoadCarrierDTO>();
				using var t51 = db.CreateLocalTable<WmsLoadCarrierDTO>("WMS_LoadCarrierA");

				var gts = db.GetTable<GlobalTaskDTO>().Union(
						db.GetTable<GlobalTaskDTO>()
							.TableName(
								"WMS_GlobalTaskA"));

				var lcs = db.GetTable<WmsLoadCarrierDTO>().Union(
						db.GetTable<WmsLoadCarrierDTO>()
							.TableName(
								"WMS_LoadCarrierA"));

				var ots = db.GetTable<OutfeedTransportOrderDTO>().Union(
						db.GetTable<OutfeedTransportOrderDTO>()
							.TableName(
								"WMS_OutfeedTransportOrderA"));

				var qry = from g in gts
						  join source1 in db.GetTable<WmsResourcePointDTO>() on g.RPSourceID equals source1.Id into source1List
						  from source in source1List.DefaultIfEmpty()
						  join sourceShelf1 in db.GetTable<StorageShelfDTO>() on g.StorageShelfSourceID equals sourceShelf1.Id into sourceShelf1List
						  from sourceShelf in sourceShelf1List.DefaultIfEmpty()
						  join dest1 in db.GetTable<WmsResourcePointDTO>() on g.RPDestinationID equals dest1.Id into destList
						  from dest in destList.DefaultIfEmpty()
						  join destShelf1 in db.GetTable<StorageShelfDTO>() on g.StorageShelfDestinationID equals destShelf1.Id into destShelf1List
						  from destShelf in destShelf1List.DefaultIfEmpty()
						  join origdest1 in db.GetTable<WmsResourcePointDTO>() on g.RPOrigDestinationID equals origdest1.Id into origdestList
						  from origdest in origdestList.DefaultIfEmpty()
						  join res in lcs on g.ResourceID equals res.Id into reslist
						  from res in reslist.DefaultIfEmpty()
						  join outfeed1 in ots on g.OutfeedTransportOrderID equals outfeed1.Id into outfeed1List
						  from outfeed in outfeed1List.DefaultIfEmpty()
						  select new WmsGlobalTaskCombinedDTO() { GlobalTask = g, LoadCarrier = res, Source = source, SourceShelf = sourceShelf, Destination = dest, DestinationShelf = destShelf, OriginDestination = origdest, OutfeedTransportOrder = outfeed };

				_ = qry.ToArray();
			}

			[YdbTableNotFound]
			[Test]
			public void Test([DataSources] string context)
			{
				using var db = GetDataContext(context);
				using var t1 = db.CreateLocalTable<GlobalTaskDTO>();
				using var t11 = db.CreateLocalTable<GlobalTaskDTO>("WMS_GlobalTaskA");
				using var t2 = db.CreateLocalTable<WmsResourcePointDTO>();
				using var t3 = db.CreateLocalTable<StorageShelfDTO>();
				using var t4 = db.CreateLocalTable<OutfeedTransportOrderDTO>();
				using var t41 = db.CreateLocalTable<OutfeedTransportOrderDTO>("WMS_OutfeedTransportOrderA");
				using var t5 = db.CreateLocalTable<WmsLoadCarrierDTO>();
				using var t51 = db.CreateLocalTable<WmsLoadCarrierDTO>("WMS_ResourceA");

				var query = db.GetTable<GlobalTaskDTO>()
						.Union(
							db.GetTable<GlobalTaskDTO>()
								.TableName(
									"WMS_GlobalTaskA"))
						.GroupJoin(
							db.GetTable<WmsResourcePointDTO>(),
							g => g.RPSourceID,
							source1 => (Guid?)source1.Id,
							(g, source1List) => new
							{
								g = g,
								source1List = source1List
							})
						.SelectMany(
							tp0 => tp0.source1List
								.DefaultIfEmpty(),
							(tp0, source) => new
							{
								tp0 = tp0,
								source = source
							})
						.GroupJoin(
							db.GetTable<StorageShelfDTO>(),
							tp1 => tp1.tp0.g.StorageShelfSourceID,
							sourceShelf1 => (Guid?)sourceShelf1.Id,
							(tp1, sourceShelf1List) => new
							{
								tp1 = tp1,
								sourceShelf1List = sourceShelf1List
							})
						.SelectMany(
							tp2 => tp2.sourceShelf1List
								.DefaultIfEmpty(),
							(tp2, sourceShelf) => new
							{
								tp2 = tp2,
								sourceShelf = sourceShelf
							})
						.GroupJoin(
							db.GetTable<WmsResourcePointDTO>(),
							tp3 => tp3.tp2.tp1.tp0.g.RPDestinationID,
							dest1 => (Guid?)dest1.Id,
							(tp3, destList) => new
							{
								tp3 = tp3,
								destList = destList
							})
						.SelectMany(
							tp4 => tp4.destList
								.DefaultIfEmpty(),
							(tp4, dest) => new
							{
								tp4 = tp4,
								dest = dest
							})
						.GroupJoin(
							db.GetTable<StorageShelfDTO>(),
							tp5 => tp5.tp4.tp3.tp2.tp1.tp0.g.StorageShelfDestinationID,
							destShelf1 => (Guid?)destShelf1.Id,
							(tp5, destShelf1List) => new
							{
								tp5 = tp5,
								destShelf1List = destShelf1List
							})
						.SelectMany(
							tp6 => tp6.destShelf1List
								.DefaultIfEmpty(),
							(tp6, destShelf) => new
							{
								tp6 = tp6,
								destShelf = destShelf
							})
						.GroupJoin(
							db.GetTable<WmsResourcePointDTO>(),
							tp7 => tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g.RPOrigDestinationID,
							origdest1 => (Guid?)origdest1.Id,
							(tp7, origdestList) => new
							{
								tp7 = tp7,
								origdestList = origdestList
							})
						.SelectMany(
							tp8 => tp8.origdestList
								.DefaultIfEmpty(),
							(tp8, origdest) => new
							{
								tp8 = tp8,
								origdest = origdest
							})
						.GroupJoin(
							db.GetTable<WmsLoadCarrierDTO>()
								.Union(
									db.GetTable<WmsLoadCarrierDTO>()
										.TableName(
											"WMS_ResourceA")),
							tp9 => tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g.ResourceID,
							res => res.Id,
							(tp9, reslist) => new
							{
								tp9 = tp9,
								reslist = reslist
							})
						.SelectMany(
							tp10 => tp10.reslist
								.DefaultIfEmpty(),
							(tp10, res) => new
							{
								tp10 = tp10,
								res = res
							})
						.GroupJoin(
							db.GetTable<OutfeedTransportOrderDTO>()
								.Union(
									db.GetTable<OutfeedTransportOrderDTO>()
										.TableName(
											"WMS_OutfeedTransportOrderA")),
							tp11 => tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g.OutfeedTransportOrderID,
							outfeed1 => (Guid?)outfeed1.Id,
							(tp11, outfeed1List) => new
							{
								tp11 = tp11,
								outfeed1List = outfeed1List
							})
						.SelectMany(
							tp12 => tp12.outfeed1List
								.DefaultIfEmpty(),
							(tp12, outfeed) => new WmsGlobalTaskCombinedDTO()
							{
								GlobalTask = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.tp0.g,
								LoadCarrier = tp12.tp11.res,
								Source = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.tp2.tp1.source,
								SourceShelf = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.tp4.tp3.sourceShelf,
								Destination = tp12.tp11.tp10.tp9.tp8.tp7.tp6.tp5.dest,
								DestinationShelf = tp12.tp11.tp10.tp9.tp8.tp7.destShelf,
								OriginDestination = tp12.tp11.tp10.tp9.origdest,
								OutfeedTransportOrder = outfeed
							});

				_ = query.ToArray();
			}
		}
	}

}
