using System;

namespace LinqToDB.SqlProvider
{
	using SqlBuilder;

	public class SqlProviderFlags
	{
		public bool IsParameterOrderDependent    { get; set; }
		public bool AcceptsTakeAsParameter       { get; set; }
		public bool AcceptsTakeAsParameterIfSkip { get; set; }
		public bool IsTakeSupported              { get; set; }
		public bool IsSkipSupported              { get; set; }
		public bool IsSkipSupportedIfTake        { get; set; }
		public bool IsSubQueryTakeSupported      { get; set; }
		public bool IsSubQueryColumnSupported    { get; set; }
		public bool IsCountSubQuerySupported     { get; set; }
		public bool IsIdentityParameterRequired  { get; set; }
		public bool IsApplyJoinSupported         { get; set; }
		public bool IsInsertOrUpdateSupported    { get; set; }
		public bool CanCombineParameters         { get; set; }

		public bool GetAcceptsTakeAsParameterFlag(SqlQuery sqlQuery)
		{
			return AcceptsTakeAsParameter || AcceptsTakeAsParameterIfSkip && sqlQuery.Select.SkipValue != null;
		}

		public bool GetIsSkipSupportedFlag(SqlQuery sqlQuery)
		{
			return IsSkipSupported || IsSkipSupportedIfTake && sqlQuery.Select.TakeValue != null;
		}
	}
}
