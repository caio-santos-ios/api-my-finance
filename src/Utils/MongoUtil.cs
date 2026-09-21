using MongoDB.Bson;
using Newtonsoft.Json;

namespace api_finances.src.Shared.Utils
{
    public static class MongoUtil
    {

        public static BsonDocument Lookup(string from, string[] localField, string[] foreignField, string property, dynamic[][]? filters = null, int? limit = null, bool tableShared = false, string _operator = "eq")
        {
            BsonArray match = new();
            BsonDocument letFields = new();

            for (int i = 0; i < localField.Length; i++)
            {
                if (tableShared && localField[i] == "$branch") continue;

                letFields.Add($"field{i}", localField[i]);

                var foreignFieldName = foreignField[i].StartsWith("$") ? foreignField[i] : $"${foreignField[i]}";
                
                match.Add(new BsonDocument($"${_operator}", new BsonArray 
                { 
                    new BsonDocument("$toString", foreignFieldName),
                    new BsonDocument("$toString", $"$$field{i}")    
                }));
            }

            if (filters is not null)
            {
                foreach (dynamic filter in filters)
                {
                    if (tableShared && filter[0] == "branch") continue;

                    string fieldName = filter[0].ToString().StartsWith("$") ? filter[0].ToString() : $"${filter[0]}";
                    
                    if (filter.Length > 2)
                    {
                        match.Add(new BsonDocument($"${filter[2]}", new BsonArray { fieldName, BsonValue.Create(filter[1]) }));
                    }
                    else
                    {
                        match.Add(new BsonDocument("$eq", new BsonArray { fieldName, BsonValue.Create(filter[1]) }));
                    }
                }
            }

            BsonArray pipeline = [
                new BsonDocument("$match", new BsonDocument{
                    {"$expr", new BsonDocument{
                        { "$and", match }
                    }}
                })
            ];

            if (limit is not null) pipeline.Add(new BsonDocument("$limit", limit));

            return new BsonDocument("$lookup", new BsonDocument{
                { "from", from },
                { "let", letFields},
                { "pipeline", pipeline},
                { "as", property }
            });
        }

        public static BsonDocument ToString(string field){
            return new BsonDocument("$toString", field);
        }

        public static BsonDocument First(string field, string? prop = null){
            return prop is null 
                ? new BsonDocument("$first", $"${field}")
                : new BsonDocument($"{prop}", new BsonDocument("$first", $"${field}"));
        }

        public static BsonDocument ValidateNull(string field, dynamic valueIfNull){
            return new BsonDocument("$ifNull", new BsonArray{$"${field}", valueIfNull});
        }

        public static BsonDocument Concat(dynamic[] fields){
            BsonArray bsonFields = [];
            foreach(dynamic field in fields ){
                bsonFields.Add(field);
            }
            return new BsonDocument ("$concat", bsonFields);
        }
    }
}