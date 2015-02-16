namespace Tests.Model
{
    using LinqToDB.Mapping;

    public class TestEntity2
    {
        [PrimaryKey, Identity]
        public long Id { get; set; }

        [Column]
        public string Name { get; set; }
    }
}