namespace Smart.Data.Accessor.Builders.SqlServer
{
    public class MergeAttribute
    {
        /*
                "MERGE INTO Setting T " +
                "USING (SELECT @Id AS Id) AS T0 ON (T.Id = T0.Id) " +
                "WHEN MATCHED THEN UPDATE SET Value = @Value " +
                "WHEN NOT MATCHED THEN INSERT (Id, Value) VALUES (@Id, @Value);",
                new { Id = key, Value = value.ToString() }));

                "MERGE INTO Progress T " +
                "USING (SELECT @Key1 AS Key1, @Key2 AS Key2) AS T0 " +
                "ON (T.Key1 = T0.Key1 AND T.Key2 = T0.Key2) " +
                "WHEN NOT MATCHED THEN INSERT (" +
                "Key1, " +
                "Key2, " +
                "Value1, " +
...
                ") VALUES (" +
                "@Key1, " +
                "@Key2, " +
                "@Value1, " +
...
                ") " +
                "WHEN MATCHED THEN UPDATE SET " +
                "Value1 = @Value1, " + " +
...
                "Value9 = @Value9");
         */
    }
}
