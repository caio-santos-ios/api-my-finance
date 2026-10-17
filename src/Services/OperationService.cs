using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.DTOs;
using api_finances.src.Shared.Utils;
using api_finances.src.Utils;
using MongoDB.Bson;

namespace api_finances.src.Services
{
    public class OperationService(
        IOperationRepository repository,
        IAttachmentRepository attachmentRepository
    ) : IOperationService
    {
        #region READ
        public async Task<ResponseApi<PaginationApi<List<dynamic>>>> GetAllAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Operation> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),
                    new("$skip", pagination.Skip),
                    new("$limit", pagination.Limit),

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        }
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
                        {"categoryId", 1},
                        {"bankId", 1},
                        {"destinationBankId", 1},
                        {"repeat", 1},
                        { "categoryName", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", "Transferência" },
                                { "else", MongoUtil.First("_categories.name") }
                            })
                        }
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetAllAsync(pipeline);
                int count = await repository.GetCountDocumentsAsync(pipeline);
                PaginationApi<List<dynamic>> data = new(entities, count, pagination.PageNumber, pagination.PageSize);
                return new(data, 200, "Operações listados com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        public async Task<ResponseApi<List<dynamic>>> GetSelectAsync(GetAllRequest request)
        {
            try
            {
                PaginationUtil<Operation> pagination = new(request.QueryParams);
                BsonDocument[] pipeline = [
                    new("$match", pagination.PipelineFilter),
                    new("$sort", pagination.PipelineSort),

                    new("$addFields", new BsonDocument
                    {
                        { "categoryObjId", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", BsonNull.Value },
                                { "else", new BsonDocument("$toObjectId", "$categoryId") }
                            })
                        }
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
                        {"categoryId", 1},
                        {"bankId", 1},
                        { "categoryName", new BsonDocument("$cond", new BsonDocument
                            {
                                { "if", new BsonDocument("$in", new BsonArray { "$categoryId", new BsonArray { BsonNull.Value, "" } }) },
                                { "then", "Transferência" },
                                { "else", MongoUtil.First("_categories.name") }
                            })
                        }
                    }),
                    new("$sort", pagination.PipelineSort),
                ];

                List<dynamic> entities = await repository.GetSelectAsync(pipeline);
                return new(entities, 200, "Operações listados com sucesso");
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
                BsonDocument[] pipeline = [
                    new("$match", new BsonDocument{
                        {"_id", new ObjectId(id)},
                        {"deleted", false}
                    }),
                    new("$addFields", new BsonDocument
                    {
                        {"id", new BsonDocument("$toString", "$_id")}
                    }),
                    new("$project", new BsonDocument
                    {
                        {"_id", 0},
                    }),
                ];

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
        public async Task<ResponseApi<Operation?>> CreateAsync(CreateOperationRequest request)
        {
            try
            {
                if (request.Type != "transfer" && string.IsNullOrWhiteSpace(request.CategoryId))
                    return new(null, 400, "A Categoria é obrigatória.");

                if (request.Type == "transfer" && string.IsNullOrWhiteSpace(request.DestinationBankId))
                    return new(null, 400, "O Banco de destino é obrigatório.");

                Operation operation = ObjectMapper.Map<CreateOperationRequest, Operation>(request);

                Operation? response = await repository.CreateAsync(operation);
                if (response is null) return new(null, 400, "Falha ao criar conta.");

                if (!string.IsNullOrEmpty(request.ParentId))
                {
                    List<Attachment> attachments = await attachmentRepository.GetByParentIdAsync(request.ParentId, "operations");
                    foreach (Attachment attachment in attachments)
                    {
                        attachment.ParentId = response.Id;
                        await attachmentRepository.UpdateAsync(attachment);
                    }
                }

                return new(response, 201, "Operação criado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion

        #region UPDATE
        public async Task<ResponseApi<Operation?>> UpdateAsync(UpdateOperationRequest request)
        {
            try
            {
                if (request.Type != "transfer" && string.IsNullOrWhiteSpace(request.CategoryId))
                    return new(null, 400, "A Categoria é obrigatória.");

                if (request.Type == "transfer" && string.IsNullOrWhiteSpace(request.DestinationBankId))
                    return new(null, 400, "O Banco de destino é obrigatório.");

                Operation? existed = await repository.GetByIdAsync(request.Id);
                if (existed is null) return new(null, 404, "Falha ao atualizar");

                existed.UpdatedAt = DateTime.UtcNow;
                existed.UpdatedBy = request.UpdatedBy;
                existed.CreatedAt = request.CreatedAt;
                existed.Value = request.Value;
                existed.CategoryId = request.CategoryId;
                existed.BankId = request.BankId;
                existed.DestinationBankId = request.DestinationBankId;
                existed.Description = request.Description;
                existed.Type = request.Type;
                existed.Repeat = request.Repeat;

                Operation? response = await repository.UpdateAsync(existed);
                if (response is null) return new(null, 400, "Falha ao atualizar");

                return new(response, 200, "Atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion

        #region DELETE
        public async Task<ResponseApi<Operation>> DeleteAsync(DeleteRequest request)
        {
            try
            {
                Operation? entity = await repository.GetByIdAsync(request.Id);
                if (entity is null) return new(null, 404, "Operação não encontrado");

                entity.Deleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = request.DeletedBy;

                await repository.DeleteAsync(entity);

                return new(entity, 204, "Operação excluído com sucesso");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. {ex.Message}");
            }
        }
        #endregion
    }
}