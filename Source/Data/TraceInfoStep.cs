using System;

namespace LinqToDB.Data
{
	public enum TraceInfoStep
	{
		BeforeExecute,
		AfterExecute,
		Error,
		MapperCreated,
		Completed
	}
}
