using System.Globalization;
using System.Text.RegularExpressions;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Dashboard;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public record DashboardDataResponse(
    string Id, string Name, string Type, string? CardClass,
    Dictionary<string, string>? Formats, List<object?> Data, string? Error);

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

        #region READ
        public async Task<ResponseApi<dynamic>> GetDashAsync(GetAllRequest request)
        {
            try
            {
                string createdBy = request.QueryParams["createdBy"];

                List<Dashboard> dashboards = await repository.GetDashAsync(createdBy);

                Dictionary<string, BsonValue> parameters = request.QueryParams
                .ToDictionary(f => $"@{f.Key}", f => ParseParameter(f.Value));
                parameters["@userId"] = createdBy;

                var tasks = dashboards.Select(async d =>
                {
                    try
                    {
                        var rows = await ExecuteAsync(d, parameters);
                        return new DashboardDataResponse(
                            d.Id!, d.Name, d.Type, d.CardClass, d.Formats,
                            rows.Select(ToPlain).ToList(), null);
                    }
                    catch (Exception ex)
                    {
                        return new DashboardDataResponse(
                            d.Id!, d.Name, d.Type, d.CardClass, d.Formats,
                            [], ex.Message);
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
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Dashboard> pagination = new(request.QueryParams);

                List<BsonDocument> pipeline = new()
                {
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"type", 1},
                        {"collection", 1},
                        {"cardClass", 1},
                        {"sequence", 1},
                        {"createdAt", 1},
                    }),
                    new("$sort", pagination.PipelineSort),
                };

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);

                return new(data, 200, "Dashboard listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
                    new("$match", new BsonDocument
                    {
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),

                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")},
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0}
                    }),
                };

                dynamic? entity = await repository.GetByIdAggregateAsync(pipeline);
                if (entity is null) return new(null, 404, "Operação não encontrado");
                return new(entity, 200, "Operação encontrado");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region CREATE
        public async Task<ResponseApi<Dashboard?>> CreateAsync(CreateDashboardRequest request)
        {
            try
            {
                Dashboard dashboard = ObjectMapper.Map<CreateDashboardRequest, Dashboard>(request);

                Dashboard? response = await repository.CreateAsync(dashboard);
                if (response is null) return new(null, 400, "Falha ao criar dashboard.");

                return new(response, 201, "Dashboard criado com sucesso.");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<Dashboard?>> UpdateAsync(UpdateDashboardRequest request)
        {
            try
            {
                Dashboard? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Dashboard não encontrado.");

                existed.Name = request.Name;
                existed.Type = request.Type;
                existed.Collection = request.Collection;
                existed.CardClass = request.CardClass;
                existed.Sequence = request.Sequence;

                Dashboard? response = await repository.UpdateAsync(existed);
                if (response is null) return new(null, 400, "Falha ao atualizar dashboard.");

                return new(response, 200, "Dashboard atualizado com sucesso.");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }
        #endregion
        #region DELETE
        public async Task<ResponseApi<Dashboard?>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Dashboard? dashboard = await repository.GetByIdAsync(request.Id);
                if (dashboard is null) return new(null, 404, "Dashboard não encontrado.");

                dashboard.Deleted = true;
                dashboard.DeletedBy = request.DeletedBy;
                dashboard.DeletedAt = DateTime.Now;

                Dashboard? response = await repository.UpdateAsync(dashboard);
                if (response is null) return new(null, 400, "Falha ao excluir dashboard.");

                return new(response, 204, "Dashboard excluido com sucesso.");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }
        #endregion

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

        private static BsonValue ParseParameter(string value)
        {
            if (bool.TryParse(value, out var b)) return b;
            if (Regex.IsMatch(value, @"^-?\d{1,18}$")) return long.Parse(value);
            if (Regex.IsMatch(value, @"^-?\d+\.\d+$")) return double.Parse(value, CultureInfo.InvariantCulture);
            if (Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}") &&
                DateTime.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d))
                return new BsonDateTime(d);

            return value;
        }
    }
}