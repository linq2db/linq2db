namespace Tests.Model
{
    using LinqToDB;
    using LinqToDB.Mapping;

    public class TestEntity1
    {
        [PrimaryKey, Identity]
        public long Id { get; set; }

        [Column(DataType = DataType.Int64, Name = "TestEntity2_id", Transparent = true)]
        [Association(ThisKey = "TestEntity2", OtherKey = "Id")]
        public TestEntity2 TestEntity2 { get; set; }
    }
}