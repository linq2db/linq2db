using System;

namespace LinqToDB
{
	public interface INotifyEntityCreated
	{
		object EntityCreated(object entity);
	}
}
