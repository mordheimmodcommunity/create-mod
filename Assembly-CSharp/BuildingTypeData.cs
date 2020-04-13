using Mono.Data.Sqlite;

public class BuildingTypeData : DataCore
{
    public BuildingTypeId Id
    {
        get;
        private set;
    }

    public string Name
    {
        get;
        private set;
    }

    public override void Populate(SqliteDataReader reader)
    {
        Id = (BuildingTypeId)reader.GetInt32(0);
        Name = reader.GetString(1);
    }
}