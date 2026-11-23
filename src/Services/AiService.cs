using System.Globalization;
using System.Text;
using System.Text.Json;
using api_finances.src.Infraestructure;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests.Ai;
using MongoDB.Driver;

namespace api_finances.src.Services
{
    public class AiService(
        AppDbContext context,
        IInvestmentService investmentService,
        IHttpClientFactory httpClientFactory
    ) : IAiService
    {
        public async Task<ResponseApi<string>> ChatAsync(string userId, AiChatRequest request)
        {
            try
            {
                string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
                string model = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-1.5-flash";

                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "SUA_CHAVE_AQUI")
                {
                    return new(
                        "Olá! Sou seu assistente financeiro no **My Finances**. 💡\n\nPara começar a conversar comigo, configure sua chave da API do Google Gemini (`GEMINI_API_KEY`) no arquivo `.env` do backend.",
                        200,
                        "Chave do Gemini não configurada no .env."
                    );
                }

                // 1. Coletar dados financeiros do usuário
                DateTime now = DateTime.UtcNow;
                DateTime startOfMonth = new(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endOfMonth = new(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, 999, DateTimeKind.Utc);

                // Todas as operações do usuário para cálculo de saldos
                List<Operation> allOperations = await context.Operations
                    .Find(x => !x.Deleted && x.CreatedBy == userId)
                    .ToListAsync();

                // Bancos e saldos
                List<Bank> banks = await context.Banks
                    .Find(x => !x.Deleted && x.CreatedBy == userId)
                    .ToListAsync();

                var bankBalances = banks.Select(b =>
                {
                    decimal inc = allOperations.Where(o => o.BankId == b.Id && o.Type == "income").Sum(o => o.Value);
                    decimal exp = allOperations.Where(o => o.BankId == b.Id && (o.Type == "expense" || o.Type == "transfer")).Sum(o => o.Value);
                    decimal inTransfers = allOperations.Where(o => o.DestinationBankId == b.Id && o.Type == "transfer").Sum(o => o.Value);
                    decimal bal = inc - exp + inTransfers;
                    return new { b.Name, Balance = bal };
                }).ToList();

                decimal totalBankBalance = bankBalances.Sum(b => b.Balance);

                // Operações do mês
                var monthOperations = allOperations.Where(o => o.CreatedAt >= startOfMonth && o.CreatedAt <= endOfMonth).ToList();

                // Categorias
                List<Category> categories = await context.Categories
                    .Find(x => !x.Deleted && x.CreatedBy == userId)
                    .ToListAsync();
                var catDict = categories.ToDictionary(c => c.Id, c => c.Name);

                var expenses = monthOperations.Where(o => o.Type == "expense").ToList();
                decimal totalMonthExpenses = expenses.Sum(o => o.Value);

                var incomes = monthOperations.Where(o => o.Type == "income").ToList();
                decimal totalMonthIncomes = incomes.Sum(o => o.Value);

                var expensesByCategory = expenses
                    .GroupBy(o => o.CategoryId)
                    .Select(g => new
                    {
                        Category = catDict.TryGetValue(g.Key, out var name) ? name : "Sem Categoria",
                        Total = g.Sum(x => x.Value),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                // Orçamentos
                List<Budget> budgets = await context.Budgets
                    .Find(x => !x.Deleted && x.CreatedBy == userId && x.Active)
                    .ToListAsync();

                var budgetSummaries = budgets.Select(b =>
                {
                    decimal spent = expenses.Where(o => o.CategoryId == b.CategoryId).Sum(o => o.Value);
                    decimal rem = b.Limit - spent;
                    decimal pct = b.Limit > 0 ? Math.Round((spent / b.Limit) * 100m, 1) : 0;
                    string catName = catDict.TryGetValue(b.CategoryId, out var name) ? name : b.Name;
                    string status = pct > 100 ? "Estourado" : (pct >= b.AlertPercentage ? "Atenção" : "Normal");
                    return new
                    {
                        Name = b.Name,
                        Category = catName,
                        Period = b.Period ?? "monthly",
                        Limit = b.Limit,
                        Spent = spent,
                        Remaining = rem,
                        Percentage = pct,
                        Status = status
                    };
                }).ToList();

                // Metas
                List<Goal> goals = await context.Goals
                    .Find(x => !x.Deleted && x.CreatedBy == userId && x.Active)
                    .ToListAsync();

                var goalSummaries = goals.Select(g => new
                {
                    Name = g.Name,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    Progress = g.TargetAmount > 0 ? Math.Round((g.CurrentAmount / g.TargetAmount) * 100m, 1) : 0,
                    Deadline = g.TargetDate?.ToString("dd/MM/yyyy")
                }).ToList();

                // Investimentos
                decimal totalInvested = 0;
                decimal currentInvestedValue = 0;
                decimal investmentProfit = 0;
                decimal investmentProfitPct = 0;
                List<string> topAssets = [];

                try
                {
                    var portfolioRes = await investmentService.GetPortfolioAsync(userId);
                    if (portfolioRes?.Data != null)
                    {
                        totalInvested = portfolioRes.Data.TotalInvested;
                        currentInvestedValue = portfolioRes.Data.CurrentValue;
                        investmentProfit = portfolioRes.Data.UnrealizedProfit;
                        investmentProfitPct = portfolioRes.Data.UnrealizedProfitPercent;

                        if (portfolioRes.Data.Positions != null)
                        {
                            topAssets = portfolioRes.Data.Positions
                                .Take(5)
                                .Select(p => $"{p.Ticker} ({p.Quantity} cotas a R$ {p.LastPrice:N2}, Total R$ {p.CurrentValue:N2})")
                                .ToList();
                        }
                    }
                }
                catch
                {
                    // Fail-safe em caso de dados parciais
                }

                // 2. Montar Contexto
                CultureInfo ptBr = new("pt-BR");
                string mesAtual = now.ToString("MMMM 'de' yyyy", ptBr);
                mesAtual = char.ToUpper(mesAtual[0]) + mesAtual[1..];

                StringBuilder contextBuilder = new();
                contextBuilder.AppendLine($"Mês de Referência: {mesAtual}");
                contextBuilder.AppendLine();

                contextBuilder.AppendLine("1. CONTAS BANCÁRIAS E SALDO TOTAL:");
                contextBuilder.AppendLine($"- Saldo Total em Contas: R$ {totalBankBalance:N2}");
                if (bankBalances.Count > 0)
                {
                    foreach (var b in bankBalances)
                    {
                        contextBuilder.AppendLine($"  * {b.Name}: R$ {b.Balance:N2}");
                    }
                }
                else
                {
                    contextBuilder.AppendLine("  (Nenhuma conta bancária cadastrada)");
                }
                contextBuilder.AppendLine();

                contextBuilder.AppendLine("2. MOVIMENTAÇÕES DESTE MÊS:");
                contextBuilder.AppendLine($"- Total de Receitas: R$ {totalMonthIncomes:N2}");
                contextBuilder.AppendLine($"- Total de Despesas: R$ {totalMonthExpenses:N2}");
                contextBuilder.AppendLine($"- Saldo Líquido do Mês: R$ {(totalMonthIncomes - totalMonthExpenses):N2}");
                if (expensesByCategory.Count > 0)
                {
                    contextBuilder.AppendLine("- Principais Categorias de Despesa:");
                    foreach (var ec in expensesByCategory)
                    {
                        contextBuilder.AppendLine($"  * {ec.Category}: R$ {ec.Total:N2} ({ec.Count} lançamentos)");
                    }
                }
                contextBuilder.AppendLine();

                contextBuilder.AppendLine("3. ORÇAMENTOS VIGENTES:");
                if (budgetSummaries.Count > 0)
                {
                    foreach (var bs in budgetSummaries)
                    {
                        contextBuilder.AppendLine($"- {bs.Name} (Categoria: {bs.Category}, Período: {bs.Period}): Limite R$ {bs.Limit:N2} | Gasto R$ {bs.Spent:N2} | Saldo R$ {bs.Remaining:N2} ({bs.Percentage}% usado) - Status: {bs.Status}");
                    }
                }
                else
                {
                    contextBuilder.AppendLine("  (Nenhum orçamento ativo)");
                }
                contextBuilder.AppendLine();

                contextBuilder.AppendLine("4. METAS FINANCEIRAS:");
                if (goalSummaries.Count > 0)
                {
                    foreach (var gs in goalSummaries)
                    {
                        string deadlineStr = !string.IsNullOrEmpty(gs.Deadline) ? $" (Prazo: {gs.Deadline})" : "";
                        contextBuilder.AppendLine($"- {gs.Name}: R$ {gs.CurrentAmount:N2} de R$ {gs.TargetAmount:N2} ({gs.Progress}% acumulado){deadlineStr}");
                    }
                }
                else
                {
                    contextBuilder.AppendLine("  (Nenhuma meta ativa)");
                }
                contextBuilder.AppendLine();

                contextBuilder.AppendLine("5. INVESTIMENTOS (CARTEIRA):");
                contextBuilder.AppendLine($"- Total Aplicado: R$ {totalInvested:N2}");
                contextBuilder.AppendLine($"- Valor Atual da Carteira: R$ {currentInvestedValue:N2}");
                contextBuilder.AppendLine($"- Lucro/Prejuízo: R$ {investmentProfit:N2} ({investmentProfitPct:F2}%)");
                if (topAssets.Count > 0)
                {
                    contextBuilder.AppendLine("- Principais Ativos:");
                    foreach (var a in topAssets)
                    {
                        contextBuilder.AppendLine($"  * {a}");
                    }
                }
                contextBuilder.AppendLine();

                // 3. Montar System Prompt
                string systemPrompt = $@"Você é o Assistente Virtual Financeiro do usuário no aplicativo 'My Finances'.
Seu papel é ser um Consultor Financeiro Pessoal inteligente, prestativo, empático e objetivo.

Diretrizes obrigatórias:
1. Responda SEMPRE em Português do Brasil (pt-BR).
2. Baseie suas respostas e análises ESTRITAMENTE nos dados reais do usuário apresentados abaixo no CONTEXTO FINANCEIRO.
3. Formate valores monetários no formato 'R$ 1.250,00'.
4. Utilize formatação Markdown (tópicos com marcadores, negrito) para tornar as respostas fáceis de ler.
5. Seja direto e encorajador. Se houver despesas altas ou orçamento estourado, alerte educadamente e sugira passos práticos.
6. Nunca invente valores que não existam no contexto. Se for perguntado sobre algo que não está cadastrado, diga educadamente que não há registros disso no app.

--- CONTEXTO FINANCEIRO REAL DO USUÁRIO ---
{contextBuilder}
------------------------------------------";

                // 4. Montar Payload para o Gemini
                var contents = new List<object>();

                if (request.History != null)
                {
                    foreach (var msg in request.History)
                    {
                        if (string.IsNullOrWhiteSpace(msg.Content)) continue;
                        string role = msg.Role?.ToLowerInvariant() switch
                        {
                            "assistant" => "model",
                            "model" => "model",
                            _ => "user"
                        };
                        contents.Add(new
                        {
                            role,
                            parts = new[] { new { text = msg.Content } }
                        });
                    }
                }

                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = request.Message } }
                });

                var payload = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents,
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1200
                    }
                };

                // 5. Enviar requisição para o Gemini
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var httpClient = httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(35);

                string jsonPayload = JsonSerializer.Serialize(payload);
                using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var httpResponse = await httpClient.PostAsync(url, httpContent);
                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return new(
                        $"Desculpe, ocorreu um problema ao consultar o serviço de IA ({httpResponse.StatusCode}). Verifique se sua chave da API do Gemini está correta ou tente novamente em instantes.",
                        200,
                        "Erro ao chamar API do Gemini"
                    );
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                string reply = "";

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCand = candidates[0];
                    if (firstCand.TryGetProperty("content", out var candContent) &&
                        candContent.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        reply = parts[0].GetProperty("text").GetString() ?? "";
                    }
                }

                if (string.IsNullOrWhiteSpace(reply))
                {
                    reply = "Não consegui processar uma resposta no momento. Por favor, tente reformular sua pergunta.";
                }

                return new(reply, 200, "Resposta gerada com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado no Assistente IA: {ex.Message}");
            }
        }
    }
}
