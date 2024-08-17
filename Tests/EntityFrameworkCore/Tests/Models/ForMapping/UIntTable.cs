using System.ComponentModel.DataAnnotations;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public class UIntTable
	{
		[Key]
		public int ID { get; set; }

		public ushort  Field16  { get; set; }
		public uint    Field32  { get; set; }
		public ulong   Field64  { get; set; }
		public ushort? Field16N { get; set; }
		public uint?   Field32N { get; set; }
		public ulong?  Field64N { get; set; }
	}
}
