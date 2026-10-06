using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Dashboard;
using api_finances.src.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public record DashboardDataResponse(
        string Id, string Name, string Type, List<object?> Data, string? Error);

    public class DashboardService(
        IDashboardRepository repository,
        IMongoDatabase db
    ) : IDashboardService
    {
        private static readonly HashSet<string> ForbiddenOperators = new()
        {
            "$out", "$merge", "$where", "$function", "$accumulator"
        };

        private static readonly HashSet<string> _allowedCollections = ["categories", "operations"];

        public async Task<ResponseApi<dynamic>> GetAllAsync(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var dashboards = await repository.GetAllAsync(userId);

                var parameters = new Dictionary<string, BsonValue>
                {
                    ["@userId"] = userId
                };

                var tasks = dashboards.Select(async d =>
                {
                    try
                    {
                        var rows = await ExecuteAsync(d, parameters);
                        return new DashboardDataResponse(
                            d.Id!, d.Name, d.Type, rows.Select(ToPlain).ToList(), null);
                    }
                    catch (Exception ex)
                    {
                        return new DashboardDataResponse(d.Id!, d.Name, d.Type, [], ex.Message);
                    }
                });

                var result = (await Task.WhenAll(tasks)).ToList();

                return new(result, 200, "Dashboard listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }

        public async Task<ResponseApi<Dashboard?>> CreateAsync(CreateDashboardRequest request)
        {
            try
            {
                Dashboard operation = ObjectMapper.Map<CreateDashboardRequest, Dashboard>(request);

                Dashboard? response = await repository.CreateAsync(operation);
                if (response is null) return new(null, 400, "Falha ao criar dashboard.");

                return new(response, 201, "Dashboard criado com sucesso.");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }
        public async Task<ResponseApi<Dashboard?>> UpdateAsync(UpdateDashboardRequest request)
        {
            try
            {
                Dashboard? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Dashboard não encontrado.");

                Dashboard operation = ObjectMapper.Map<UpdateDashboardRequest, Dashboard>(request);

                Dashboard? response = await repository.UpdateAsync(operation);
                if (response is null) return new(null, 400, "Falha ao atualizar dashboard.");

                return new(response, 200, "Dashboard atualizado com sucesso.");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }

        private async Task<List<BsonDocument>> ExecuteAsync(Dashboard dashboard, Dictionary<string, BsonValue> parameters, CancellationToken ct = default)
        {
            if (!_allowedCollections.Contains(dashboard.Collection))
                throw new InvalidOperationException("Collection não permitida.");

            var stages = ((BsonArray)ApplyParameters(ParsePipeline(dashboard.Query), parameters))
            .Select(s => s.AsBsonDocument)
            .ToList();

            foreach (var stage in stages) Validate(stage);
            stages.Add(new BsonDocument("$limit", 5000));

            var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
            var options = new AggregateOptions { MaxTime = TimeSpan.FromSeconds(10) };

            return await db.GetCollection<BsonDocument>(dashboard.Collection)
                .Aggregate(pipeline, options)
                .ToListAsync(ct);
        }

        private static BsonValue ApplyParameters(BsonValue value, Dictionary<string, BsonValue> parameters)
        {
            switch (value)
            {
                case BsonString s when s.Value.StartsWith('@') && parameters.TryGetValue(s.Value, out var replacement):
                    return replacement;

                case BsonDocument doc:
                    var newDoc = new BsonDocument();
                    foreach (var el in doc)
                        newDoc[el.Name] = ApplyParameters(el.Value, parameters);
                    return newDoc;

                case BsonArray arr:
                    return new BsonArray(arr.Select(v => ApplyParameters(v, parameters)));

                default:
                    return value;
            }
        }

        private static void Validate(BsonValue value)
        {
            switch (value)
            {
                case BsonDocument doc:
                    foreach (var el in doc)
                    {
                        if (ForbiddenOperators.Contains(el.Name))
                            throw new InvalidOperationException($"Operador {el.Name} não é permitido.");
                        Validate(el.Value);
                    }
                    break;

                case BsonArray arr:
                    foreach (var item in arr) Validate(item);
                    break;
            }
        }

        private static object? ToPlain(BsonValue v) => v.BsonType switch
        {
            BsonType.Document => v.AsBsonDocument.ToDictionary(e => e.Name, e => ToPlain(e.Value)),
            BsonType.Array => v.AsBsonArray.Select(ToPlain).ToList(),
            BsonType.ObjectId => v.AsObjectId.ToString(),
            BsonType.DateTime => v.ToUniversalTime(),
            BsonType.Int32 => v.AsInt32,
            BsonType.Int64 => v.AsInt64,
            BsonType.Double => v.AsDouble,
            BsonType.Decimal128 => (decimal)v.AsDecimal128,
            BsonType.Boolean => v.AsBoolean,
            BsonType.String => v.AsString,
            BsonType.Null => null,
            _ => v.ToString()
        };

        private static BsonArray ParsePipeline(string json)
        {
            var doc = BsonDocument.Parse($"{{ \"stages\": {json} }}");
            return doc["stages"].AsBsonArray;
        }
    }
}