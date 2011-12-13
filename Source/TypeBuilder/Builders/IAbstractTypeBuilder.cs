using System;

namespace LinqToDB.TypeBuilder.Builders
{
	public interface IAbstractTypeBuilder
	{
		int    ID            { get; set; }
		object TargetElement { get; set; }

		Type[] GetInterfaces();
		bool   IsCompatible (BuildContext context, IAbstractTypeBuilder typeBuilder);

		bool   IsApplied    (BuildContext context, AbstractTypeBuilderList builders);
		int    GetPriority  (BuildContext context);
		void   Build        (BuildContext context);
	}
}
