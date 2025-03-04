using System;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public class Event<T> : MemberBase, IEvent
		where T : Event<T>
	{
		public bool IsStatic  { get; set; }
		public bool IsVirtual { get; set; }

		public Event()
		{
		}

		public Event(Type eventType, string name, bool nullable)
		{
			TypeBuilder = () => new ModelType(eventType, nullable).ToTypeName();
			Name        = name;
		}

		public Event(string eventType, string name, bool nullable)
		{
			TypeBuilder = () => new ModelType(eventType, true, nullable).ToTypeName();
			Name        = name;
		}

		public Event(Func<string> typeBuilder, string name)
		{
			TypeBuilder = typeBuilder;
			Name        = name;
		}

		public override int CalcModifierLen()
		{
			return
				(IsStatic  ? " static". Length : 0) +
				(IsVirtual ? " virtual".Length : 0) +
				" event".Length;
		}

		public override int CalcBodyLen() { return 1; }

		public override void Render(ModelGenerator tt, bool isCompact)
		{
			tt.WriteEvent(this);
		}
	}
}
