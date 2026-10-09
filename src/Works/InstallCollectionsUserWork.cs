using api_finances.src.Infraestructure;
using api_finances.src.Models;
using MongoDB.Driver;

namespace api_finances.src.Works
{
    public class InstallCollectionsUserWork(IServiceProvider serviceProvider, ILogger<InstallCollectionsUserWork> _logger) : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(120);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueNotifications(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no background worker de instalar collections dos usuários.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ProcessDueNotifications(CancellationToken ct)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await InstallCollectionsAsync(context);
        }

        private async Task InstallCollectionsAsync(AppDbContext context)
        {
            List<User> users = await context.Users
                .Find(x => !x.Deleted && !x.Blocked && !x.InstallCollections)
                .ToListAsync();

            if (users.Count == 0) return;

            foreach (User user in users)
            {
                try
                {
                    List<GenericTable> banks = [
                        new() { Code = "nubank", Name = "Nubank", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "nubank-pj", Name = "Nubank Empresas", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "itau", Name = "Itaú", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "bradesco", Name = "Bradesco", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "banco-do-brasil", Name = "Banco do Brasil", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "caixa", Name = "Caixa Econômica Federal", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "santander", Name = "Santander", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "inter", Name = "Banco Inter", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "c6-bank", Name = "C6 Bank", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "picpay", Name = "PicPay", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "mercado-pago", Name = "Mercado Pago", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "pagbank", Name = "PagBank", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "btg-pactual", Name = "BTG Pactual", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "xp", Name = "XP Investimentos", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "sicredi", Name = "Sicredi", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "sicoob", Name = "Sicoob", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "banrisul", Name = "Banrisul", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "safra", Name = "Banco Safra", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "original", Name = "Banco Original", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "neon", Name = "Neon", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "next", Name = "Next", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "will-bank", Name = "Will Bank", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "agibank", Name = "Agibank", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "bmg", Name = "Banco BMG", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "pan", Name = "Banco Pan", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "daycoval", Name = "Banco Daycoval", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "brb", Name = "BRB", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "carteira", Name = "Carteira (dinheiro)", Table = "types-banks", CreatedBy = user.Id },
                        new() { Code = "outros", Name = "Outros", Table = "types-banks", CreatedBy = user.Id }
                    ];

                    await context.GenericTables.InsertManyAsync(banks);

                    user.InstallCollections = true;
                    await context.Users.ReplaceOneAsync(x => x.Id.Equals(user.Id), user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao atualizar usuário {Id}", user.Id);
                }
            }
        }
    }
}



