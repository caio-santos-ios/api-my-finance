using MongoDB.Bson;
using MongoDB.Driver;

namespace api_finances.src.Shared.Utils
{
    public class PaginationUtil<T>
    {
        public FilterDefinition<T>? Filter {get;set;}
        public SortDefinition<T>? Sort {get;set;}
        public BsonDocument PipelineSort {get;set;}
        public BsonDocument PipelineFilter {get;set;}
        public int Skip {get;set;}
        public int Limit {get;set;}
        public int PageSize {get;set;}
        public int PageNumber {get;set;} 
        public long Count {get;set;} 
        readonly BsonDocument pipelineFilter = [];
        public PaginationUtil(Dictionary<string,string> queries)
        {
            int pageSize = queries.TryGetValue("pageSize", out string? valuePageSize) ? int.Parse(valuePageSize) : 10;
            int pageNumber = queries.TryGetValue("pageNumber", out string? valuePageNumber) ? int.Parse(valuePageNumber) : 1;
            int sort = queries.TryGetValue("sort", out string? valueSort) ? valueSort.Equals("desc") ? -1 : 1 : -1;
            string orderBy = queries.TryGetValue("orderBy", out string? valueOrderBy) ? valueOrderBy : "createdAt";
            BsonDocument pipelineSort = new (){{ orderBy, sort}};

            int skip = pageSize * (pageNumber - 1);
            int limit = pageSize;

            SortDefinition<T>? sortDefinition = sort.Equals("desc") ? Builders<T>.Sort.Descending(orderBy) : Builders<T>.Sort.Ascending(orderBy);

            foreach (var query in queries)
            {
                string key = query.Key;
                if (!new List<string> { "pageSize", "pageNumber", "page", "sort", "orderBy" }.Contains(key))
                {
                    string value = query.Value;
                    string type = "";
                    type = bool.TryParse(value, out _) ? "bool" : type;
                    type = int.TryParse(value, out int _) ? "int" : type;
                    type = DateTime.TryParse(value, out DateTime _) ? "date" : type;


                    string[] expressions = key.Split("$");
                    string comparison = "$eq";
                    string logic = "and";
                    string field = "";

                    if (expressions.Length == 3)
                    {
                        comparison = $"${expressions[0]}";
                        logic = expressions[1];
                        field = expressions[2];
                    };

                    if (expressions.Length == 2)
                    {
                        comparison = $"${expressions[0]}";
                        field = expressions[1];
                    };

                    if (expressions.Length == 1)
                    {
                        field = expressions[0];
                    };

                    if(comparison.Equals("$regex")) 
                    {
                        type = "string";
                    }

                    switch (type)
                    {
                        case "bool":
                            BsonDocument valueBool = new(comparison, Convert.ToBoolean(value));

                            if (logic == "and")
                            {
                                pipelineFilter.Add(field, valueBool);
                            };

                            if (logic == "or")
                            {
                                bool isOrExisted = pipelineFilter.Contains("$or");

                                if (isOrExisted)
                                {
                                    pipelineFilter["$or"].AsBsonArray.Add(new BsonDocument(field, valueBool));
                                }
                                else
                                {
                                    pipelineFilter.Add("$or", new BsonArray { new BsonDocument(field, valueBool) });
                                };
                            };

                            break;

                        case "int":
                            BsonDocument valueNum = new(comparison, int.Parse(value));

                            if (logic == "and")
                            {
                                pipelineFilter.Add(field, valueNum);
                            };

                            if (logic == "or")
                            {
                                bool isOrExisted = pipelineFilter.Contains("$or");

                                if (isOrExisted)
                                {
                                    pipelineFilter["$or"].AsBsonArray.Add(new BsonDocument(field, valueNum));
                                }
                                else
                                {
                                    pipelineFilter.Add("$or", new BsonArray { new BsonDocument(field, valueNum) });
                                };
                            };
                            break;

                        case "date":
                            DateTime date = comparison == "$lte" ? Convert.ToDateTime(value).Date.AddDays(1) : Convert.ToDateTime(value).Date;
                            BsonDocument valueDt = new(comparison, date);

                            if (logic == "and")
                            {
                                if (pipelineFilter.Contains(field))
                                {
                                    pipelineFilter[field].AsBsonDocument.Add(comparison, date);
                                }
                                else
                                {
                                    pipelineFilter.Add(field, valueDt);
                                }
                            }
                            else if (logic == "or")
                            {
                                if (pipelineFilter.Contains("$or"))
                                {
                                    pipelineFilter["$or"].AsBsonArray.Add(new BsonDocument(field, valueDt));
                                }
                                else
                                {
                                    pipelineFilter.Add("$or", new BsonArray { new BsonDocument(field, valueDt) });
                                }
                            }
                            break;

                        default:
                            BsonDocument valueString = comparison.Equals("$regex") ? new BsonDocument { { "$regex", value }, { "$options", "i" } } : new BsonDocument(comparison, value);
                            
                            if(comparison == "$in")
                            {
                                BsonArray list = new(
                                    value
                                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(v => (BsonValue)v.Trim())
                                );
                                valueString = new BsonDocument("$in", list);
                            }

                            if (logic == "and")
                            {
                                pipelineFilter.Add(field, valueString);
                            };

                            if (logic == "or")
                            {
                                bool isOrExisted = pipelineFilter.Contains("$or");

                                if (isOrExisted)
                                {
                                    pipelineFilter["$or"].AsBsonArray.Add(new BsonDocument(field, valueString));
                                }
                                else
                                {
                                    pipelineFilter.Add("$or", new BsonArray { new BsonDocument(field, valueString) });
                                };
                            };

                            break;
                    };
                };
            };

            PipelineSort = pipelineSort;
            PipelineFilter = pipelineFilter;
            Sort = sortDefinition;
            Skip = skip;
            Limit = limit;
            PageSize = pageSize;
            PageNumber = pageNumber;
        }
    }
}