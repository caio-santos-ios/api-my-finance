using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace api_finances.src.Repository
{
    public class ImportationRepository(AppDbContext context) : IImportationRepository
    {
        #region CREATE
        public async Task<ResponseApi<Importation?>> CreateAsync(Importation attachment)
        {
            try
            {
                await context.Importations.InsertOneAsync(attachment);
                return new(attachment, 201, "Importação criado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
        #region READ
        public async Task<ResponseApi<List<dynamic>>> GetAllAsync(PaginationUtil<Importation> pagination)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$addFields", new BsonDocument
                    {
                        {"categoryObjId", new BsonDocument("$toObjectId", "$categoryId")}
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"description", 1},
                        {"value", 1},
                        {"type", 1},
                        {"active", 1},
                        {"createdAt", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                    }),
                    new("$sort", pagination.PipelineSort),
                };

                List<BsonDocument> results = await context.Importations.Aggregate<BsonDocument>(pipeline).ToListAsync();
                List<dynamic> list = results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
                return new(list);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(PaginationUtil<Importation> pagination)
        {
            try
            {
                List<BsonDocument> pipeline = new()
                {
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),

                    new("$addFields", new BsonDocument
                    {
                        {"categoryObjId", new BsonDocument("$toObjectId", "$categoryId")}
                    }),

                    MongoUtil.Lookup("categories", ["$categoryObjId"], ["$_id"], "_categories", [["deleted", false]], 1),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"description", 1},
                        {"value", 1},
                        {"type", 1},
                        {"active", 1},
                        {"createdAt", 1},
                        {"categoryName", MongoUtil.First("_categories.name")},
                    }),
                    new("$sort", pagination.PipelineSort),
                };

                List<BsonDocument> results = await context.Importations.Aggregate<BsonDocument>(pipeline).ToListAsync();
                List<dynamic> list = results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).ToList();
                return new(list);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<dynamic?>> GetByIdAggregateAsync(string id)
        {
            try
            {
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),

                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                        {"id", new BsonDocument("$toString", "$_id")},
                        {"name", 1},
                        {"code", 1}
                    }),
                ];

                BsonDocument? response = await context.Importations.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
                dynamic? result = response is null ? null : BsonSerializer.Deserialize<dynamic>(response);
                return result is null ? new(null, 404, "Importação não encontrado") : new(result);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<ResponseApi<Importation?>> GetByIdAsync(string id)
        {
            try
            {
                Importation? attachment = await context.Importations.Find(x => x.Id == id && !x.Deleted).FirstOrDefaultAsync();
                return new(attachment);
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        public async Task<int> GetCountDocumentsAsync(PaginationUtil<Importation> pagination)
        {
            List<BsonDocument> pipeline = new()
            {
                new("$match", pagination.PipelineFilter),
                new("$sort", pagination.PipelineSort),
                new("$addFields", new BsonDocument
                {
                    {"id", new BsonDocument("$toString", "$_id")},
                }),
                new("$project", new BsonDocument
                {
                    {"_id", 0},
                    {"password", 0},
                    {"role", 0},
                    {"blocked", 0},
                    {"codeAccess", 0},
                    {"validatedAccess", 0}
                }),
                new("$sort", pagination.PipelineSort),
            };

            List<BsonDocument> results = await context.Importations.Aggregate<BsonDocument>(pipeline).ToListAsync();
            return results.Select(doc => BsonSerializer.Deserialize<dynamic>(doc)).Count();
        }
        #endregion
        #region UPDATE
        public async Task<ResponseApi<Importation?>> UpdateAsync(Importation attachment)
        {
            try
            {
                await context.Importations.ReplaceOneAsync(x => x.Id == attachment.Id, attachment);
                return new(attachment, 200, "Importação atualizado com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
        #region DELETE
        public async Task<ResponseApi<Importation>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Importation? attachment = await context.Importations.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
                if (attachment is null) return new(null, 404, "Importação não encontrado");

                attachment.Deleted = true;
                attachment.DeletedAt = DateTime.UtcNow;
                attachment.DeletedBy = request.DeletedBy;

                await context.Importations.ReplaceOneAsync(x => x.Id == attachment.Id, attachment);

                return new(attachment, 204, "Importação excluído com sucesso");
            }
            catch
            {
                return new(null, 500, "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
    }
}