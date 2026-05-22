using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2033Tests : TestBase
	{
		[Table(Name = "NC_CODE")]
		public partial class NcCode
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("NC_CODE"), Nullable] public string? NcCodeColumn { get; set; } // NVARCHAR2(48)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("NC_CATEGORY"), Nullable] public string? NcCategory { get; set; } // NVARCHAR2(60)
			[Column("DPMO_CATEGORY_BO"), Nullable] public string? DpmoCategoryBo { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "NC_GROUP_MEMBER")]
		public partial class NcGroupMember
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("NC_GROUP_BO"), Nullable] public string? NcGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("NC_CODE_OR_GROUP_GBO"), Nullable] public string? NcCodeOrGroupGbo { get; set; } // NVARCHAR2(1236)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
		}

		[Table(Name = "SFC_ROUTING")]
		public partial class SfcRouting
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SFC_BO"), Nullable] public string? SfcBo { get; set; } // NVARCHAR2(1236)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "SFC_ROUTER")]
		public partial class SfcRouter
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("SFC_ROUTING_BO"), Nullable] public string? SfcRoutingBo { get; set; } // NVARCHAR2(1236)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
			[Column("IN_USE"), Nullable] public string? InUse { get; set; } // NVARCHAR2(15)
			[Column("COMPLETED"), Nullable] public string? Completed { get; set; } // NVARCHAR2(15)
			[Column("SUB_ROUTER"), Nullable] public string? SubRouter { get; set; } // NVARCHAR2(15)
			[Column("QTY"), Nullable] public decimal? Qty { get; set; } // NUMBER (38,6)
			[Column("REWORKED_FROM_SFC_STEP_BO"), Nullable] public string? ReworkedFromSfcStepBo { get; set; } // NVARCHAR2(1236)
			[Column("RETURN_TYPE"), Nullable] public string? ReturnType { get; set; } // NVARCHAR2(3)
			[Column("SUB_TYPE"), Nullable] public string? SubType { get; set; } // NVARCHAR2(3)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
		}
		[Table(Name = "SFC_STEP")]
		public partial class SfcStep
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("SFC_ROUTER_BO"), Nullable] public string? SfcRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STEP_ID"), Nullable] public string? StepId { get; set; } // NVARCHAR2(18)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("DONE"), Nullable] public string? Done { get; set; } // NVARCHAR2(15)
			[Column("BYPASSED"), Nullable] public string? Bypassed { get; set; } // NVARCHAR2(15)
			[Column("QTY_IN_QUEUE"), Nullable] public decimal? QtyInQueue { get; set; } // NUMBER (38,6)
			[Column("QTY_IN_WORK"), Nullable] public decimal? QtyInWork { get; set; } // NUMBER (38,6)
			[Column("QTY_COMPLETED"), Nullable] public decimal? QtyCompleted { get; set; } // NUMBER (38,6)
			[Column("QTY_COMPLETE_PENDING"), Nullable] public decimal? QtyCompletePending { get; set; } // NUMBER (38,6)
			[Column("QTY_REJECTED"), Nullable] public decimal? QtyRejected { get; set; } // NUMBER (38,6)
			[Column("TIMES_PROCESSED"), Nullable] public decimal? TimesProcessed { get; set; } // NUMBER (38,0)
			[Column("MAX_LOOP"), Nullable] public decimal? MaxLoop { get; set; } // NUMBER (38,0)
			[Column("USE_AS_REWORK"), Nullable] public string? UseAsRework { get; set; } // NVARCHAR2(15)
			[Column("PREVIOUSLY_STARTED"), Nullable] public string? PreviouslyStarted { get; set; } // NVARCHAR2(15)
			[Column("LAST_WORK_CENTER_BO"), Nullable] public string? LastWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("PREVIOUS_STEP_ID"), Nullable] public string? PreviousStepId { get; set; } // NVARCHAR2(18)
			[Column("DATE_QUEUED"), Nullable] public DateTime? DateQueued { get; set; } // DATE
			[Column("REPORTING_STEP"), Nullable] public string? ReportingStep { get; set; } // NVARCHAR2(108)
			[Column("STEP_SEQUENCE"), Nullable] public decimal? StepSequence { get; set; } // NUMBER (38,0)
			[Column("RESOURCE_OR_CENTER_GBO"), Nullable] public string? ResourceOrCenterGbo { get; set; } // NVARCHAR2(1236)
			[Column("STATE"), Nullable] public string? State { get; set; } // NVARCHAR2(3)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("PREVIOUS_RESOURCE_BO"), Nullable] public string? PreviousResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("MULTI_Q_SIGNOFF_NEEDED"), Nullable] public string? MultiQSignoffNeeded { get; set; } // NVARCHAR2(15)
			[Column("STEP_PULLED_INTO"), Nullable] public string? StepPulledInto { get; set; } // NVARCHAR2(18)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("LOCAL_REWORK"), Nullable] public string? LocalRework { get; set; } // NVARCHAR2(3)
			[Column("SPECIAL_INSTRUCTION"), Nullable] public string? SpecialInstruction { get; set; } // NVARCHAR2(384)
			[Column("FUTURE_HOLD_COUNT"), Nullable] public decimal? FutureHoldCount { get; set; } // NUMBER (38,0)
			[Column("ERP_SENT"), Nullable] public string? ErpSent { get; set; } // NVARCHAR2(15)
			[Column("SCRAP_REPORTING_STEP"), Nullable] public string? ScrapReportingStep { get; set; } // NVARCHAR2(108)
			[Column("ERP_TB_SENT"), Nullable] public string? ErpTbSent { get; set; } // NVARCHAR2(15)
		}
		[Table(Name = "ROUTER_STEP")]
		public partial class RouterStep
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STEP_ID"), Nullable] public string? StepId { get; set; } // NVARCHAR2(18)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("REWORK"), Nullable] public string? Rework { get; set; } // NVARCHAR2(15)
			[Column("QUEUE_DECISION_TYPE"), Nullable] public string? QueueDecisionType { get; set; } // NVARCHAR2(3)
			[Column("ROUTER_COMP_GBO"), Nullable] public string? RouterCompGbo { get; set; } // NVARCHAR2(1236)
			[Column("REPORTING_STEP"), Nullable] public string? ReportingStep { get; set; } // NVARCHAR2(108)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("TABULAR_STEP_TYPE"), Nullable] public string? TabularStepType { get; set; } // NVARCHAR2(3)
			[Column("SCRAP_REPORTING_STEP"), Nullable] public string? ScrapReportingStep { get; set; } // NVARCHAR2(108)
			[Column("IS_LAST_REPORTING_STEP"), Nullable] public string? IsLastReportingStep { get; set; } // NVARCHAR2(15)
			[Column("ERP_SEQUENCE"), Nullable] public string? ErpSequence { get; set; } // NVARCHAR2(18)
			[Column("ERP_CONTROL_KEY_BO"), Nullable] public string? ErpControlKeyBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_WORK_CENTER_BO"), Nullable] public string? ErpWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_INSPECTION_COMPLETE"), Nullable] public string? ErpInspectionComplete { get; set; } // NVARCHAR2(15)
		}
		[Table(Name = "ROUTER_OPERATION")]
		public partial class RouterOperation
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("ROUTER_STEP_BO"), Nullable] public string? RouterStepBo { get; set; } // NVARCHAR2(1236)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("MAX_LOOP"), Nullable] public decimal? MaxLoop { get; set; } // NUMBER (38,0)
			[Column("REQUIRED_TIME_IN_PROCESS"), Nullable] public decimal? RequiredTimeInProcess { get; set; } // NUMBER (38,0)
			[Column("STEP_TYPE"), Nullable] public string? StepType { get; set; } // NVARCHAR2(3)
			[Column("SPECIAL_INSTRUCTION"), Nullable] public string? SpecialInstruction { get; set; } // NVARCHAR2(384)
		}
		[Table(Name = "ROUTER")]
		public partial class Router
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("ROUTER"), Nullable] public string? RouterColumn { get; set; } // NVARCHAR2(384)
			[Column("ROUTER_TYPE"), Nullable] public string? RouterType { get; set; } // NVARCHAR2(3)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("TEMPORARY_ROUTER"), Nullable] public string? TemporaryRouter { get; set; } // NVARCHAR2(15)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("ENTRY_ROUTER_STEP_BO"), Nullable] public string? EntryRouterStepBo { get; set; } // NVARCHAR2(1236)
			[Column("COPIED_FROM_ROUTER_BO"), Nullable] public string? CopiedFromRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("REVISION"), Nullable] public string? Revision { get; set; } // NVARCHAR2(60)
			[Column("CURRENT_REVISION"), Nullable] public string? CurrentRevision { get; set; } // NVARCHAR2(15)
			[Column("HAS_BEEN_RELEASED"), Nullable] public string? HasBeenReleased { get; set; } // NVARCHAR2(15)
			[Column("EFF_START_DATE"), Nullable] public DateTime? EffStartDate { get; set; } // DATE
			[Column("EFF_END_DATE"), Nullable] public DateTime? EffEndDate { get; set; } // DATE
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("GUI_REPRESENTATION"), Nullable] public string? GuiRepresentation { get; set; } // NCLOB
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_GROUP_BO"), Nullable] public string? DispositionGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("DISPLAY_TYPE"), Nullable] public string? DisplayType { get; set; } // NVARCHAR2(3)
			[Column("HOLD_ID"), Nullable] public decimal? HoldId { get; set; } // NUMBER (38,0)
			[Column("SEND_AS_SHARED"), Nullable] public string? SendAsShared { get; set; } // NVARCHAR2(15)
			[Column("SENT_TO_ERP"), Nullable] public string? SentToErp { get; set; } // NVARCHAR2(15)
			[Column("ERP_CHANGE_NUMBER"), Nullable] public string? ErpChangeNumber { get; set; } // NVARCHAR2(36)
			[Column("RELAXED_FLOW"), Nullable] public string? RelaxedFlow { get; set; } // NVARCHAR2(15)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
		}

		[Table(Name = "CUSTOM_FIELDS")]
		public partial class CustomFields
		{
			[Column("HANDLE"), Nullable] public string? Handle { get; set; } // NVARCHAR2(1236)
			[Column("ATTRIBUTE"), Nullable] public string? Attribute { get; set; } // NVARCHAR2(180)
			[Column("VALUE"), Nullable] public string? Value { get; set; } // NVARCHAR2(3072)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "NC_DATA")]
		public partial class NcData
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("NC_CONTEXT_GBO"), Nullable] public string? NcContextGbo { get; set; } // NVARCHAR2(1236)
			[Column("USER_BO"), Nullable] public string? UserBo { get; set; } // NVARCHAR2(1236)
			[Column("DATE_TIME"), Nullable] public DateTime? DateTime { get; set; } // DATE
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("PARENT_NC_DATA_BO"), Nullable] public string? ParentNcDataBo { get; set; } // NVARCHAR2(1236)
			[Column("NC_STATE"), Nullable] public string? NcState { get; set; } // NVARCHAR2(3)
			[Column("NC_CODE_BO"), Nullable] public string? NcCodeBo { get; set; } // NVARCHAR2(1236)
			[Column("NC_DATA_TYPE_BO"), Nullable] public string? NcDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY"), Nullable] public decimal? Qty { get; set; } // NUMBER (38,6)
			[Column("DEFECT_COUNT"), Nullable] public decimal? DefectCount { get; set; } // NUMBER (38,6)
			[Column("COMPONENT_BO"), Nullable] public string? ComponentBo { get; set; } // NVARCHAR2(1236)
			[Column("COMP_CONTEXT_GBO"), Nullable] public string? CompContextGbo { get; set; } // NVARCHAR2(1236)
			[Column("REF_DES"), Nullable] public string? RefDes { get; set; } // NVARCHAR2(108)
			[Column("COMMENTS"), Nullable] public string? Comments { get; set; } // NCLOB
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_ROUTER_BO"), Nullable] public string? DispositionRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STEP_ID"), Nullable] public string? StepId { get; set; } // NVARCHAR2(18)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("TIMES_PROCESSED"), Nullable] public decimal? TimesProcessed { get; set; } // NUMBER (38,0)
			[Column("RESOURCE_BO"), Nullable] public string? ResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("WORK_CENTER_BO"), Nullable] public string? WorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
			[Column("CLOSURE_REQUIRED"), Nullable] public string? ClosureRequired { get; set; } // NVARCHAR2(15)
			[Column("CLOSED_USER_BO"), Nullable] public string? ClosedUserBo { get; set; } // NVARCHAR2(1236)
			[Column("CLOSED_DATE_TIME"), Nullable] public DateTime? ClosedDateTime { get; set; } // DATE
			[Column("CANCELLED_USER_BO"), Nullable] public string? CancelledUserBo { get; set; } // NVARCHAR2(1236)
			[Column("CANCELLED_DATE_TIME"), Nullable] public DateTime? CancelledDateTime { get; set; } // DATE
			[Column("INCIDENT_DATE_TIME"), Nullable] public DateTime? IncidentDateTime { get; set; } // DATE
			[Column("IDENTIFIER"), Nullable] public string? Identifier { get; set; } // NVARCHAR2(120)
			[Column("FAILURE_ID"), Nullable] public string? FailureId { get; set; } // NVARCHAR2(120)
			[Column("VERIFIED_STATE"), Nullable] public string? VerifiedState { get; set; } // NVARCHAR2(3)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("NC_CATEGORY"), Nullable] public string? NcCategory { get; set; } // NVARCHAR2(60)
			[Column("VERIFIED_DATE_TIME"), Nullable] public DateTime? VerifiedDateTime { get; set; } // DATE
			[Column("LOCATION"), Nullable] public string? Location { get; set; } // NVARCHAR2(60)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("INCIDENT_NUMBER_BO"), Nullable] public string? IncidentNumberBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_DONE"), Nullable] public string? DispositionDone { get; set; } // NVARCHAR2(15)
			[Column("ROOT_CAUSE_OPER_BO"), Nullable] public string? RootCauseOperBo { get; set; } // NVARCHAR2(1236)
			[Column("TRANSFERRED_TO_DPMO"), Nullable] public string? TransferredToDpmo { get; set; } // NVARCHAR2(3)
			[Column("COMPONENT_SFC_BO"), Nullable] public string? ComponentSfcBo { get; set; } // NVARCHAR2(1236)
			[Column("COMPONENT_SFC_ITEM_BO"), Nullable] public string? ComponentSfcItemBo { get; set; } // NVARCHAR2(1236)
			[Column("DISPOSITION_FUNCTION_BO"), Nullable] public string? DispositionFunctionBo { get; set; } // NVARCHAR2(1236)
			[Column("ASSEMBLY_INCIDENT_NUM"), Nullable] public string? AssemblyIncidentNum { get; set; } // NVARCHAR2(108)
			[Column("BATCH_INCIDENT_NUM"), Nullable] public string? BatchIncidentNum { get; set; } // NVARCHAR2(108)
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("ACTION_CODE"), Nullable] public string? ActionCode { get; set; } // NVARCHAR2(60)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("COPIED_FROM_NC_DATA_BO"), Nullable] public string? CopiedFromNcDataBo { get; set; } // NVARCHAR2(1236)
			[Column("TXN_ID"), Nullable] public string? TxnId { get; set; } // NVARCHAR2(150)
		}
		[Table(Name = "SFC")]
		public partial class Sfc
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("SFC"), Nullable] public string? SfcColumn { get; set; } // NVARCHAR2(384)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("SHOP_ORDER_BO"), Nullable] public string? ShopOrderBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY"), Nullable] public decimal? Qty { get; set; } // NUMBER (38,6)
			[Column("QTY_DONE"), Nullable] public decimal? QtyDone { get; set; } // NUMBER (38,6)
			[Column("QTY_SCRAPPED"), Nullable] public decimal? QtyScrapped { get; set; } // NUMBER (38,6)
			[Column("QTY_HISTORICAL_MIN"), Nullable] public decimal? QtyHistoricalMin { get; set; } // NUMBER (38,6)
			[Column("QTY_HISTORICAL_MAX"), Nullable] public decimal? QtyHistoricalMax { get; set; } // NUMBER (38,6)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
			[Column("PRIORITY"), Nullable] public decimal? Priority { get; set; } // NUMBER (38,0)
			[Column("LOCATION"), Nullable] public string? Location { get; set; } // NVARCHAR2(60)
			[Column("RMA_MAX_TIMES_PROCESSED"), Nullable] public decimal? RmaMaxTimesProcessed { get; set; } // NUMBER (38,0)
			[Column("LCC_BO"), Nullable] public string? LccBo { get; set; } // NVARCHAR2(1236)
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY_MULT_PERFORMED"), Nullable] public string? QtyMultPerformed { get; set; } // NVARCHAR2(15)
			[Column("ACTUAL_COMP_DATE"), Nullable] public DateTime? ActualCompDate { get; set; } // DATE
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("IMMEDIATE_ARCHIVE"), Nullable] public string? ImmediateArchive { get; set; } // NVARCHAR2(15)
			[Column("TRANSFER_DATETIME"), Nullable] public DateTime? TransferDatetime { get; set; } // DATE
			[Column("TRANSFER_USER"), Nullable] public string? TransferUser { get; set; } // NVARCHAR2(90)
			[Column("SN_DONE"), Nullable] public string? SnDone { get; set; } // NVARCHAR2(15)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
		}
		[Table(Name = "OPERATION")]
		public partial class Operation
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("OPERATION"), Nullable] public string? OperationColumn { get; set; } // NVARCHAR2(108)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("TYPE"), Nullable] public string? Type { get; set; } // NVARCHAR2(3)
			[Column("SPECIAL_ROUTER_BO"), Nullable] public string? SpecialRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("RESOURCE_TYPE_BO"), Nullable] public string? ResourceTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("REVISION"), Nullable] public string? Revision { get; set; } // NVARCHAR2(60)
			[Column("CURRENT_REVISION"), Nullable] public string? CurrentRevision { get; set; } // NVARCHAR2(15)
			[Column("EFF_START_DATE"), Nullable] public DateTime? EffStartDate { get; set; } // DATE
			[Column("EFF_END_DATE"), Nullable] public DateTime? EffEndDate { get; set; } // DATE
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("PCA_DASHBOARD_MODE"), Nullable] public string? PcaDashboardMode { get; set; } // NVARCHAR2(3)
			[Column("DEFAULT_NC_CODE_BO"), Nullable] public string? DefaultNcCodeBo { get; set; } // NVARCHAR2(1236)
			[Column("FAILURE_TRACKING_CONFIG_BO"), Nullable] public string? FailureTrackingConfigBo { get; set; } // NVARCHAR2(1236)
			[Column("RESOURCE_BO"), Nullable] public string? ResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("MAX_LOOP"), Nullable] public decimal? MaxLoop { get; set; } // NUMBER (38,0)
			[Column("REQUIRED_TIME_IN_PROCESS"), Nullable] public decimal? RequiredTimeInProcess { get; set; } // NUMBER (38,0)
			[Column("REPORTING_STEP"), Nullable] public string? ReportingStep { get; set; } // NVARCHAR2(108)
			[Column("PREV_SITE"), Nullable] public string? PrevSite { get; set; } // NVARCHAR2(18)
			[Column("ORIGINAL_TRANSFER_KEY"), Nullable] public string? OriginalTransferKey { get; set; } // NVARCHAR2(1236)
			[Column("SPECIAL_INSTRUCTION"), Nullable] public string? SpecialInstruction { get; set; } // NVARCHAR2(384)
			[Column("REPORTING_CENTER_BO"), Nullable] public string? ReportingCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_CONTROL_KEY_BO"), Nullable] public string? ErpControlKeyBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_WORK_CENTER_BO"), Nullable] public string? ErpWorkCenterBo { get; set; } // NVARCHAR2(1236)
		}

		[Table(Name = "SITE")]
		public partial class Site
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? SiteColumn { get; set; } // NVARCHAR2(18)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("TYPE"), Nullable] public string? Type { get; set; } // NVARCHAR2(3)
			[Column("IS_LOCAL"), Nullable] public string? IsLocal { get; set; } // NVARCHAR2(15)
			[Column("URL"), Nullable] public string? Url { get; set; } // NVARCHAR2(3072)
			[Column("SERVER"), Nullable] public string? Server { get; set; } // NVARCHAR2(96)
			[Column("PORT"), Nullable] public decimal? Port { get; set; } // NUMBER (38,0)
			[Column("LOGON_ID"), Nullable] public string? LogonId { get; set; } // NVARCHAR2(90)
			[Column("PASSWORD"), Nullable] public string? Password { get; set; } // NVARCHAR2(90)
			[Column("TIME_ZONE"), Nullable] public string? TimeZone { get; set; } // NVARCHAR2(108)
			[Column("SITE_LOCALE"), Nullable] public string? SiteLocale { get; set; } // NVARCHAR2(120)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "USR")]
		public partial class Usr
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("USER_ID"), Nullable] public string? UserId { get; set; } // NVARCHAR2(90)
			[Column("CURRENT_OPERATION_BO"), Nullable] public string? CurrentOperationBo { get; set; } // NVARCHAR2(1236)
			[Column("CURRENT_RESOURCE_BO"), Nullable] public string? CurrentResourceBo { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("BADGE_NUMBER"), Nullable] public string? BadgeNumber { get; set; } // NVARCHAR2(120)
			[Column("EMPLOYEE_NUMBER"), Nullable] public string? EmployeeNumber { get; set; } // NVARCHAR2(120)
			[Column("HIRE_DATE"), Nullable] public string? HireDate { get; set; } // NVARCHAR2(24)
			[Column("TERMINATION_DATE"), Nullable] public string? TerminationDate { get; set; } // NVARCHAR2(24)
			[Column("ALLOW_CLOCK_IN_NON_PROD"), Nullable] public string? AllowClockInNonProd { get; set; } // NVARCHAR2(15)
			[Column("ACTION_CLOCK_OUT_SFC"), Nullable] public string? ActionClockOutSfc { get; set; } // NVARCHAR2(3)
			[Column("CLOCK_IN_OUT_RANGE"), Nullable] public string? ClockInOutRange { get; set; } // NVARCHAR2(3)
			[Column("ALLOW_SUP_TIME_EDIT_APPR"), Nullable] public string? AllowSupTimeEditAppr { get; set; } // NVARCHAR2(15)
			[Column("APPR_REQ_FOR_EXPORT"), Nullable] public string? ApprReqForExport { get; set; } // NVARCHAR2(15)
			[Column("AUTO_CLOCK_OUT"), Nullable] public string? AutoClockOut { get; set; } // NVARCHAR2(15)
			[Column("CLOCK_IN_CONTROL"), Nullable] public string? ClockInControl { get; set; } // NVARCHAR2(3)
			[Column("DEFAULT_WORK_CENTER_BO"), Nullable] public string? DefaultWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_PERSONNEL_NUMBER"), Nullable] public string? ErpPersonnelNumber { get; set; } // NVARCHAR2(24)
			[Column("ERP_USER"), Nullable] public string? ErpUser { get; set; } // NVARCHAR2(36)
		}
		[Table(Name = "SHOP_ORDER")]
		public partial class ShopOrder
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("SHOP_ORDER"), Nullable] public string? ShopOrderColumn { get; set; } // NVARCHAR2(108)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("PRIORITY"), Nullable] public decimal? Priority { get; set; } // NUMBER (38,0)
			[Column("PLANNED_WORK_CENTER_BO"), Nullable] public string? PlannedWorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("PLANNED_ITEM_BO"), Nullable] public string? PlannedItemBo { get; set; } // NVARCHAR2(1236)
			[Column("PLANNED_BOM_BO"), Nullable] public string? PlannedBomBo { get; set; } // NVARCHAR2(1236)
			[Column("PLANNED_ROUTER_BO"), Nullable] public string? PlannedRouterBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY_TO_BUILD"), Nullable] public decimal? QtyToBuild { get; set; } // NUMBER (38,6)
			[Column("QTY_ORDERED"), Nullable] public decimal? QtyOrdered { get; set; } // NUMBER (38,6)
			[Column("QTY_RELEASED"), Nullable] public decimal? QtyReleased { get; set; } // NUMBER (38,6)
			[Column("RELEASED_DATE"), Nullable] public DateTime? ReleasedDate { get; set; } // DATE
			[Column("PLANNED_START_DATE"), Nullable] public DateTime? PlannedStartDate { get; set; } // DATE
			[Column("PLANNED_COMP_DATE"), Nullable] public DateTime? PlannedCompDate { get; set; } // DATE
			[Column("SCHEDULED_START_DATE"), Nullable] public DateTime? ScheduledStartDate { get; set; } // DATE
			[Column("SCHEDULED_COMP_DATE"), Nullable] public DateTime? ScheduledCompDate { get; set; } // DATE
			[Column("ACTUAL_START_DATE"), Nullable] public DateTime? ActualStartDate { get; set; } // DATE
			[Column("ACTUAL_COMP_DATE"), Nullable] public DateTime? ActualCompDate { get; set; } // DATE
			[Column("QTY_DONE"), Nullable] public decimal? QtyDone { get; set; } // NUMBER (38,6)
			[Column("QTY_SCRAPPED"), Nullable] public decimal? QtyScrapped { get; set; } // NUMBER (38,6)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("CUSTOMER"), Nullable] public string? Customer { get; set; } // NVARCHAR2(120)
			[Column("CUSTOMER_ORDER"), Nullable] public string? CustomerOrder { get; set; } // NVARCHAR2(120)
			[Column("RMA_SFC_DATA_TYPE_BO"), Nullable] public string? RmaSfcDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("RMA_SHOP_ORDER_DATA_TYPE_BO"), Nullable] public string? RmaShopOrderDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("TRANSFER_SITE"), Nullable] public string? TransferSite { get; set; } // NVARCHAR2(18)
			[Column("TRANSFER_TYPE"), Nullable] public string? TransferType { get; set; } // NVARCHAR2(3)
			[Column("LCC_BO"), Nullable] public string? LccBo { get; set; } // NVARCHAR2(1236)
			[Column("SHOP_ORDER_TYPE_BO"), Nullable] public string? ShopOrderTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("HOLD_ID"), Nullable] public decimal? HoldId { get; set; } // NUMBER (38,0)
			[Column("END_UNIT_NUMBER"), Nullable] public string? EndUnitNumber { get; set; } // NVARCHAR2(108)
			[Column("REQ_SERIAL_CHANGE"), Nullable] public string? ReqSerialChange { get; set; } // NVARCHAR2(15)
			[Column("COLLECT_PARENT_SERIAL"), Nullable] public string? CollectParentSerial { get; set; } // NVARCHAR2(15)
			[Column("BATCH_NUMBER"), Nullable] public string? BatchNumber { get; set; } // NVARCHAR2(60)
			[Column("ERP_ORDER"), Nullable] public string? ErpOrder { get; set; } // NVARCHAR2(15)
			[Column("ERP_PRODUCTION_VERSION"), Nullable] public string? ErpProductionVersion { get; set; } // NVARCHAR2(12)
			[Column("ERP_UNIT_OF_MEASURE"), Nullable] public string? ErpUnitOfMeasure { get; set; } // NVARCHAR2(9)
			[Column("PARTITION_DATE"), NotNull] public DateTime PartitionDate { get; set; } // TIMESTAMP(6)
			[Column("INSPECTION_LOT"), Nullable] public string? InspectionLot { get; set; } // NVARCHAR2(60)
			[Column("INSPECTION_GROUP_SIZE"), Nullable] public decimal? InspectionGroupSize { get; set; } // NUMBER (38,0)
			[Column("ERP_PUTAWAY_STORLOC"), Nullable] public string? ErpPutawayStorloc { get; set; } // NVARCHAR2(12)
			[Column("WAREHOUSE_NUMBER"), Nullable] public string? WarehouseNumber { get; set; } // NVARCHAR2(9)
		}
		[Table(Name = "RESRCE")]
		public partial class Resrce
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("RESRCE"), Nullable] public string? ResrceColumn { get; set; } // NVARCHAR2(108)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("PROCESS_RESOURCE"), Nullable] public string? ProcessResource { get; set; } // NVARCHAR2(15)
			[Column("OPERATION_BO"), Nullable] public string? OperationBo { get; set; } // NVARCHAR2(1236)
			[Column("VALID_FROM"), Nullable] public DateTime? ValidFrom { get; set; } // DATE
			[Column("VALID_TO"), Nullable] public DateTime? ValidTo { get; set; } // DATE
			[Column("SETUP_STATE"), Nullable] public string? SetupState { get; set; } // NVARCHAR2(3)
			[Column("SETUP_DESCRIPTION"), Nullable] public string? SetupDescription { get; set; } // NVARCHAR2(120)
			[Column("CNC_MACHINE"), Nullable] public string? CncMachine { get; set; } // NVARCHAR2(1236)
			[Column("PENDING_STATUS_BO"), Nullable] public string? PendingStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("PENDING_REASON_CODE_BO"), Nullable] public string? PendingReasonCodeBo { get; set; } // NVARCHAR2(1236)
			[Column("PENDING_COMMENTS"), Nullable] public string? PendingComments { get; set; } // NCLOB
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("ERP_PLANT_MAINT_ORDER"), Nullable] public string? ErpPlantMaintOrder { get; set; } // NVARCHAR2(36)
			[Column("ERP_EQUIPMENT_NUMBER"), Nullable] public string? ErpEquipmentNumber { get; set; } // NVARCHAR2(54)
			[Column("ERP_INTERNAL_ID"), Nullable] public decimal? ErpInternalId { get; set; } // NUMBER (38,0)
			[Column("ERP_CAPACITY_CATEGORY"), Nullable] public string? ErpCapacityCategory { get; set; } // NVARCHAR2(9)
		}
		[Table(Name = "WORK_CENTER")]
		public partial class WorkCenter
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("WORK_CENTER"), Nullable] public string? WorkCenterColumn { get; set; } // NVARCHAR2(108)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("CAN_BE_RELEASED_TO"), Nullable] public string? CanBeReleasedTo { get; set; } // NVARCHAR2(15)
			[Column("WC_CATEGORY"), Nullable] public string? WcCategory { get; set; } // NVARCHAR2(60)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("WC_TYPE"), Nullable] public string? WcType { get; set; } // NVARCHAR2(60)
			[Column("ASSIGNMENT_ENFORCEMENT"), Nullable] public string? AssignmentEnforcement { get; set; } // NVARCHAR2(60)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("ERP_INTERNAL_ID"), Nullable] public decimal? ErpInternalId { get; set; } // NUMBER (38,0)
			[Column("IS_ERP_WORK_CENTER"), Nullable] public string? IsErpWorkCenter { get; set; } // NVARCHAR2(15)
			[Column("ERP_WORK_CENTER"), Nullable] public string? ErpWorkCenter { get; set; } // NVARCHAR2(24)
			[Column("ERP_CAPACITY_CATEGORY"), Nullable] public string? ErpCapacityCategory { get; set; } // NVARCHAR2(9)
			[Column("STANDARD_VALUE_KEY_BO"), Nullable] public string? StandardValueKeyBo { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "WORK_CENTER_MEMBER")]
		public partial class WorkCenterMember
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("WORK_CENTER_BO"), Nullable] public string? WorkCenterBo { get; set; } // NVARCHAR2(1236)
			[Column("WORK_CENTER_OR_RESOURCE_GBO"), Nullable] public string? WorkCenterOrResourceGbo { get; set; } // NVARCHAR2(1236)
			[Column("PRIMARY_WORK_CENTER"), Nullable] public string? PrimaryWorkCenter { get; set; } // NVARCHAR2(15)
			[Column("SEQUENCE"), Nullable] public decimal? Sequence { get; set; } // NUMBER (38,0)
		}

		[Table(Name = "ITEM_GROUP")]
		public partial class ItemGroup
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("ITEM_GROUP"), Nullable] public string? ItemGroupColumn { get; set; } // NVARCHAR2(96)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
			[Column("MASK_GROUP_BO"), Nullable] public string? MaskGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
		}
		[Table(Name = "ITEM_GROUP_MEMBER")]
		public partial class ItemGroupMember
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("ITEM_GROUP_BO"), Nullable] public string? ItemGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_BO"), Nullable] public string? ItemBo { get; set; } // NVARCHAR2(1236)
		}
		[Table(Name = "ITEM")]
		public partial class Item
		{
			[Column("HANDLE"), NotNull] public string Handle { get; set; } = null!; // NVARCHAR2(1236)
			[Column("CHANGE_STAMP"), Nullable] public decimal? ChangeStamp { get; set; } // NUMBER (38,0)
			[Column("SITE"), Nullable] public string? Site { get; set; } // NVARCHAR2(18)
			[Column("ITEM"), Nullable] public string? ItemColumn { get; set; } // NVARCHAR2(384)
			[Column("DESCRIPTION"), Nullable] public string? Description { get; set; } // NVARCHAR2(120)
			[Column("STATUS_BO"), Nullable] public string? StatusBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_TYPE"), Nullable] public string? ItemType { get; set; } // NVARCHAR2(3)
			[Column("EFF_START_SEQ"), Nullable] public decimal? EffStartSeq { get; set; } // NUMBER (38,0)
			[Column("EFF_END_SEQ"), Nullable] public decimal? EffEndSeq { get; set; } // NUMBER (38,0)
			[Column("LOT_SIZE"), Nullable] public decimal? LotSize { get; set; } // NUMBER (38,6)
			[Column("QUANTITY_RESTRICTION"), Nullable] public string? QuantityRestriction { get; set; } // NVARCHAR2(3)
			[Column("ROUTER_BO"), Nullable] public string? RouterBo { get; set; } // NVARCHAR2(1236)
			[Column("BOM_BO"), Nullable] public string? BomBo { get; set; } // NVARCHAR2(1236)
			[Column("COMPONENT_GROUP_BO"), Nullable] public string? ComponentGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("ITEM_GROUP_BO"), Nullable] public string? ItemGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("LAST_RELEASED_DATE"), Nullable] public DateTime? LastReleasedDate { get; set; } // DATE
			[Column("ASSY_DATA_TYPE_BO"), Nullable] public string? AssyDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("PRE_ASSEMBLED"), Nullable] public string? PreAssembled { get; set; } // NVARCHAR2(15)
			[Column("REVISION"), Nullable] public string? Revision { get; set; } // NVARCHAR2(60)
			[Column("CURRENT_REVISION"), Nullable] public string? CurrentRevision { get; set; } // NVARCHAR2(15)
			[Column("EFF_START_DATE"), Nullable] public DateTime? EffStartDate { get; set; } // DATE
			[Column("EFF_END_DATE"), Nullable] public DateTime? EffEndDate { get; set; } // DATE
			[Column("SELECTOR_ACTIVITY_BO"), Nullable] public string? SelectorActivityBo { get; set; } // NVARCHAR2(1236)
			[Column("SELECTOR_NOTE"), Nullable] public string? SelectorNote { get; set; } // NVARCHAR2(120)
			[Column("ASSIGN_SERIAL_AT_RELEASE"), Nullable] public string? AssignSerialAtRelease { get; set; } // NVARCHAR2(15)
			[Column("CREATED_DATE_TIME"), Nullable] public DateTime? CreatedDateTime { get; set; } // DATE
			[Column("MODIFIED_DATE_TIME"), Nullable] public DateTime? ModifiedDateTime { get; set; } // DATE
			[Column("DRAWING_NAME"), Nullable] public string? DrawingName { get; set; } // NVARCHAR2(768)
			[Column("MAXIMUM_USAGE"), Nullable] public decimal? MaximumUsage { get; set; } // NUMBER (38,0)
			[Column("USE_COMP_FROM_DRAWING"), Nullable] public string? UseCompFromDrawing { get; set; } // NVARCHAR2(15)
			[Column("PANEL"), Nullable] public string? Panel { get; set; } // NVARCHAR2(15)
			[Column("REMOVAL_ASSY_DATA_TYPE_BO"), Nullable] public string? RemovalAssyDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("INV_ASSY_DATA_TYPE_BO"), Nullable] public string? InvAssyDataTypeBo { get; set; } // NVARCHAR2(1236)
			[Column("ORIGINAL_STATUS_BO"), Nullable] public string? OriginalStatusBo { get; set; } // NVARCHAR2(1236)
			[Column("QTY_MULTIPLIER"), Nullable] public decimal? QtyMultiplier { get; set; } // NUMBER (38,6)
			[Column("CREATE_TRACKABLE_SFC"), Nullable] public string? CreateTrackableSfc { get; set; } // NVARCHAR2(3)
			[Column("MASK_GROUP_BO"), Nullable] public string? MaskGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("TRANSFER_ITEM_GROUP_BO"), Nullable] public string? TransferItemGroupBo { get; set; } // NVARCHAR2(1236)
			[Column("UNIT_OF_MEASURE"), Nullable] public string? UnitOfMeasure { get; set; } // NVARCHAR2(120)
			[Column("HOLD_ID"), Nullable] public decimal? HoldId { get; set; } // NUMBER (38,0)
			[Column("COLLECT_PARENT_SERIAL"), Nullable] public string? CollectParentSerial { get; set; } // NVARCHAR2(15)
			[Column("REQ_SERIAL_CHANGE"), Nullable] public string? ReqSerialChange { get; set; } // NVARCHAR2(15)
			[Column("IS_COLLECTOR"), Nullable] public string? IsCollector { get; set; } // NVARCHAR2(15)
			[Column("INC_BATCH_NUMBER"), Nullable] public string? IncBatchNumber { get; set; } // NVARCHAR2(3)
			[Column("TIME_SENSITIVE"), Nullable] public string? TimeSensitive { get; set; } // NVARCHAR2(15)
			[Column("MAX_SHELF_LIFE"), Nullable] public decimal? MaxShelfLife { get; set; } // NUMBER (38,6)
			[Column("MAX_SHELF_LIFE_UNITS"), Nullable] public string? MaxShelfLifeUnits { get; set; } // NVARCHAR2(6)
			[Column("MAX_FLOOR_LIFE"), Nullable] public decimal? MaxFloorLife { get; set; } // NUMBER (38,6)
			[Column("MAX_FLOOR_LIFE_UNITS"), Nullable] public string? MaxFloorLifeUnits { get; set; } // NVARCHAR2(6)
			[Column("NOTES"), Nullable] public string? Notes { get; set; } // NVARCHAR2(4000)
			[Column("TB_COMP_TYPE"), Nullable] public string? TbCompType { get; set; } // NVARCHAR2(3)
			[Column("CONSUMPTION_TOL"), Nullable] public decimal? ConsumptionTol { get; set; } // NUMBER (38,6)
			[Column("ERP_BACKFLUSHING"), Nullable] public string? ErpBackflushing { get; set; } // NVARCHAR2(15)
			[Column("STORAGE_LOCATION_BO"), Nullable] public string? StorageLocationBo { get; set; } // NVARCHAR2(1236)
			[Column("ERP_PUTAWAY_STORLOC"), Nullable] public string? ErpPutawayStorloc { get; set; } // NVARCHAR2(12)
			[Column("USE_ORDER_ID_REL1"), Nullable] public string? UseOrderIdREL1 { get; set; } // NVARCHAR2(15)
			[Column("ERP_GTIN"), Nullable] public string? ErpGtin { get; set; } // NVARCHAR2(54)
			[Column("AIN_MODEL_EXTERNAL_ID"), Nullable] public string? AinModelExternalId { get; set; } // NVARCHAR2(120)
		}

		internal sealed class WipCte
		{
			private readonly WipDB db;

			/// <summary>
			///     Costruttore a cui viene inviato il db su cui eseguire le CTE
			/// </summary>
			/// <param name="db"></param>
			internal WipCte(WipDB db)
			{
				this.db = db;
			}

			/// <summary>
			///     Restituisce l'elenco dei codici delle non conformità ammessi.
			///     Sono quelli per i quali i gruppi delle non conformità sono CATAN_AUTO, CATAN_MAN e CATAN_ALL
			/// </summary>
			/// <returns></returns>
			internal IQueryable<AllowedNcCodeOutput> AllowedNcCode()
			{
				return (from ncCode in db.NcCode
						join ncGroupMember in db.NcGroupMember
							on ncCode.Handle equals ncGroupMember.NcCodeOrGroupGbo
						where
							ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_AUTO" ||
							ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_MAN" ||
							ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_ALL"
						select new AllowedNcCodeOutput
						{
							NcCodeBo = ncCode.Handle,
							NcCode = ncCode.NcCodeColumn,
							NcCodeDescription = ncCode.Description
						}).Distinct().AsCte(nameof(AllowedNcCode));
			}
		}

		public class AllowedNcCodeOutput
		{
			internal string? NcCodeBo { get; set; }
			internal string? NcCode { get; set; }
			internal string? NcCodeDescription { get; set; }
		}

		public class ProductionFailedNcDataOutput
		{
			public AllowedNcCodeOutput? NcCode { get; set; }
			public string? UserBo { get; set; }
			public string? Operation { get; set; }
			public string? OperationBo { get; set; }
			public string? OperationDescription { get; set; }
			public string? CurrentOperationBo { get; set; }
			public string? SfcBo { get; set; }
			public string? Sfc { get; set; }
			public string? ShopOrderBo { get; set; }
			public string? ItemBo { get; set; }
			public string? NcDataBo { get; set; }
			public string? ResourceBo { get; set; }
			public string? WorkCenterBo { get; set; }
			public DateTime PartitionDate { get; set; }
			public string? Site { get; set; }
		}

		public class FilterByTestOperationOutput
		{
			public ProductionFailedNcDataOutput? Parent { get; set; }
			public string? SfcStepBo { get; set; }
			public string? RouterOperationBo { get; set; }
		}

		public class GetAdditionalDataOutput
		{
			public string? SiteDescription { get; set; }
			public FilterByTestOperationOutput? Parent { get; set; }
			public string? ShopOrder { get; set; }
			public string? Resrce { get; set; }
			public string? ResrceDescription { get; set; }
			public string? Workcenter { get; set; }
			public string? WorkcenterDescription { get; set; }
			public string? Line { get; set; }
			public string? LineDescription { get; set; }
			public string? Item { get; set; }
			public string? ItemDescription { get; set; }
			public string? ProductLine { get; set; }
			public string? ProductGroup { get; set; }
			public string? ItemGroup { get; set; }
			public string? ItemGroupDescription { get; set; }
			public string? TestCategory { get; set; }
			public string? UserId { get; set; }
			public string? BadgeNumber { get; set; }
		}

		private static IQueryable<AllowedNcCodeOutput> GetAllowedNcCode(WipDB wipDB)
		{
			return (from ncCode in wipDB.NcCode
					join ncGroupMember in wipDB.NcGroupMember
						on ncCode.Handle equals ncGroupMember.NcCodeOrGroupGbo
					where
						ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_AUTO" ||
						ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_MAN" ||
						ncGroupMember.NcGroupBo == "NCGroupBO:" + ncCode.Site + ",CATAN_ALL"
					select new AllowedNcCodeOutput
					{
						NcCodeBo = ncCode.Handle,
						NcCode = ncCode.NcCodeColumn,
						NcCodeDescription = ncCode.Description
					}).Distinct().AsCte(nameof(GetAllowedNcCode));
		}

		private static IQueryable<ProductionFailedNcDataOutput> FindProductionFailedNcData(WipDB wipDB,
			IQueryable<AllowedNcCodeOutput> ncCode)
		{
			return (from ncData in wipDB.NcData
					from ncCodeItem in ncCode.InnerJoin(ncCodeItem => ncCodeItem.NcCodeBo == ncData.NcCodeBo)
					join sfc in wipDB.Sfc
						on ncData.NcContextGbo equals sfc.Handle
					join operationItem in wipDB.Operation
						on ncData.OperationBo equals operationItem.Handle
					join customFields in wipDB.CustomFields
						on new { Handle = sfc.ShopOrderBo, Attribute = "ORDER_TYPE", Value = "ZPRN" } equals new
						{ customFields.Handle, customFields.Attribute, customFields.Value }
					select new ProductionFailedNcDataOutput
					{
						NcCode = ncCodeItem,
						Operation = operationItem.OperationColumn,
						OperationBo = ncData.OperationBo,
						UserBo = ncData.UserBo,
						OperationDescription = operationItem.Description,
						CurrentOperationBo = "OperationBO:" + ncData.Site + "," + operationItem.OperationColumn + ",#",
						SfcBo = sfc.Handle,
						Sfc = sfc.SfcColumn,
						ShopOrderBo = sfc.ShopOrderBo,
						ItemBo = sfc.ItemBo,
						NcDataBo = ncData.Handle,
						ResourceBo = ncData.ResourceBo,
						WorkCenterBo = ncData.WorkCenterBo,
						PartitionDate = ncData.PartitionDate,
						Site = ncData.Site
					}).AsCte(nameof(FindProductionFailedNcData));
		}

		private static IQueryable<FilterByTestOperationOutput> FilterByTestOperation(WipDB wipDB,
			IQueryable<ProductionFailedNcDataOutput> input)
		{
			return (from inputItem in input
					join sfcRouting in wipDB.SfcRouting
						on inputItem.SfcBo equals sfcRouting.SfcBo
					join sfcRouter in wipDB.SfcRouter
						on sfcRouting.Handle equals sfcRouter.SfcRoutingBo
					join sfcStep in wipDB.SfcStep
						on sfcRouter.Handle equals sfcStep.SfcRouterBo
					join routerStep in wipDB.RouterStep
						on new { sfcRouter.RouterBo, sfcStep.StepId } equals new { routerStep.RouterBo, routerStep.StepId }
					join routerOperation in wipDB.RouterOperation
						on new { RouterStepBo = routerStep.Handle, OperationBo = inputItem.CurrentOperationBo } equals new
						{ routerOperation.RouterStepBo, routerOperation.OperationBo }
					join customFields in wipDB.CustomFields
						on new { routerOperation.Handle, Attribute = "OPERATION_TYPE", Value = "T" } equals new
						{ customFields.Handle, customFields.Attribute, customFields.Value }
					join router in wipDB.Router
						on sfcRouter.RouterBo equals router.Handle
					where (sfcRouter.Completed == "false" && sfcRouter.InUse == "true") ||
						  (sfcRouter.Completed == "true" && router.RouterType == "U")
					select new FilterByTestOperationOutput
					{
						Parent = inputItem,
						SfcStepBo = sfcStep.Handle,
						RouterOperationBo = routerOperation.Handle
					}).AsCte(nameof(FilterByTestOperation));
		}

		private static IQueryable<GetAdditionalDataOutput> GetAdditionalData(WipDB wipDB,
			IQueryable<FilterByTestOperationOutput> input)
		{
			return (from inputItem in input
					from site in wipDB.Site.LeftJoin(site => site.SiteColumn == inputItem.Parent!.Site)
					from usr in wipDB.Usr.LeftJoin(usr => usr.Handle == inputItem.Parent!.UserBo)
					from shopOrder in wipDB.ShopOrder.LeftJoin(
						shopOrder => shopOrder.Handle == inputItem.Parent!.ShopOrderBo)
					from resrce in wipDB.Resrce.LeftJoin(resrce => resrce.Handle == inputItem.Parent!.ResourceBo)
					from workCenter in wipDB.WorkCenter.LeftJoin(workCenter =>
						workCenter.Handle == inputItem.Parent!.WorkCenterBo)
					from workCenterMember in wipDB.WorkCenterMember.LeftJoin(workCenterMember =>
						workCenterMember.WorkCenterOrResourceGbo == inputItem.Parent!.WorkCenterBo)
					from line in wipDB.WorkCenter.LeftJoin(line => line.Handle == workCenterMember.WorkCenterBo)
					from item in wipDB.Item.LeftJoin(item => item.Handle == inputItem.Parent!.ItemBo)
					from itemGroupMember in wipDB.ItemGroupMember.LeftJoin(itemGroupMember =>
						itemGroupMember.ItemBo == inputItem.Parent!.ItemBo)
					from itemGroup in wipDB.ItemGroup.LeftJoin(itemGroup => itemGroup.Handle == itemGroupMember.ItemGroupBo)
					from customField in wipDB.CustomFields.LeftJoin(customField =>
						customField.Attribute == "PRODUCT_LINE" && customField.Handle == inputItem.Parent!.ItemBo)
					from customField2 in wipDB.CustomFields.LeftJoin(customField =>
						customField.Attribute == "SPART" && customField.Handle == inputItem.Parent!.ItemBo)
					from customField3 in wipDB.CustomFields.LeftJoin(customField =>
						customField.Attribute == "TEST_CATEGORY" && customField.Handle == inputItem.RouterOperationBo)
					select new GetAdditionalDataOutput
					{
						SiteDescription = site.Description,
						Parent = inputItem,
						ShopOrder = shopOrder.ShopOrderColumn,
						Resrce = resrce.ResrceColumn,
						ResrceDescription = resrce.Description,
						Workcenter = workCenter.WorkCenterColumn,
						WorkcenterDescription = workCenter.Description,
						Line = line.WorkCenterColumn,
						LineDescription = line.Description,
						Item = item.ItemColumn,
						ItemDescription = item.Description,
						ProductLine = customField.Value,
						ProductGroup = customField2.Value,
						ItemGroup = itemGroup.ItemGroupColumn,
						ItemGroupDescription = itemGroup.Description,
						TestCategory = customField3.Value,
						UserId = usr.UserId,
						BadgeNumber = usr.BadgeNumber
					}).AsCte(nameof(GetAdditionalData));
		}

		public class WipDB : DataConnection
		{
			public WipDB(string configuration)
				: base(configuration)
			{
			}

			public ITable<NcCode> NcCode => this.GetTable<NcCode>();
			public ITable<NcGroupMember> NcGroupMember => this.GetTable<NcGroupMember>();
			public ITable<SfcRouting> SfcRouting => this.GetTable<SfcRouting>();
			public ITable<SfcRouter> SfcRouter => this.GetTable<SfcRouter>();
			public ITable<RouterStep> RouterStep => this.GetTable<RouterStep>();
			public ITable<RouterOperation> RouterOperation => this.GetTable<RouterOperation>();
			public ITable<CustomFields> CustomFields => this.GetTable<CustomFields>();
			public ITable<Router> Router => this.GetTable<Router>();
			public ITable<SfcStep> SfcStep => this.GetTable<SfcStep>();
			public ITable<Operation> Operation => this.GetTable<Operation>();
			public ITable<Sfc> Sfc => this.GetTable<Sfc>();
			public ITable<NcData> NcData => this.GetTable<NcData>();
			public ITable<Site> Site => this.GetTable<Site>();
			public ITable<Usr> Usr => this.GetTable<Usr>();
			public ITable<ShopOrder> ShopOrder => this.GetTable<ShopOrder>();
			public ITable<Resrce> Resrce => this.GetTable<Resrce>();
			public ITable<WorkCenter> WorkCenter => this.GetTable<WorkCenter>();
			public ITable<WorkCenterMember> WorkCenterMember => this.GetTable<WorkCenterMember>();
			public ITable<Item> Item => this.GetTable<Item>();
			public ITable<ItemGroupMember> ItemGroupMember => this.GetTable<ItemGroupMember>();
			public ITable<ItemGroup> ItemGroup => this.GetTable<ItemGroup>();
		}

		[Test]
		public void Issue2033([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (var db = new WipDB(context))
			using (db.CreateLocalTable<NcCode>())
			using (db.CreateLocalTable<NcGroupMember>())
			using (db.CreateLocalTable<SfcRouting>())
			using (db.CreateLocalTable<SfcRouter>())
			using (db.CreateLocalTable<RouterStep>())
			using (db.CreateLocalTable<RouterOperation>())
			using (db.CreateLocalTable<CustomFields>())
			using (db.CreateLocalTable<Router>())
			using (db.CreateLocalTable<SfcStep>())
			using (db.CreateLocalTable<Operation>())
			using (db.CreateLocalTable<Sfc>())
			using (db.CreateLocalTable<NcData>())
			using (db.CreateLocalTable<Site>())
			using (db.CreateLocalTable<Usr>())
			using (db.CreateLocalTable<ShopOrder>())
			using (db.CreateLocalTable<Resrce>())
			using (db.CreateLocalTable<WorkCenter>())
			using (db.CreateLocalTable<WorkCenterMember>())
			using (db.CreateLocalTable<Item>())
			using (db.CreateLocalTable<ItemGroupMember>())
			using (db.CreateLocalTable<ItemGroup>())
			{
				var sfcBo = "SFCBO:8110,C17C05016";

				var query =
					from item in GetAdditionalData(db,
						FilterByTestOperation(db, FindProductionFailedNcData(db, GetAllowedNcCode(db))))
					where item.Parent!.Parent!.SfcBo == sfcBo
					select item;

				Assert.DoesNotThrow(() => query.ToArray());
			}
		}

	}
}
