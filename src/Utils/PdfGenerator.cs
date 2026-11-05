using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using api_finances.src.Shared.DTOs;

namespace api_finances.src.Utils
{
    public static class PdfGenerator
    {
        public static (byte[] pdfBytes, string sha256Hex) GenerateIrpfReport(int baseYear, TaxPreviewDto preview)
        {
            var pages = new List<List<string>>();
            var currentPageLines = new List<string>();

            void PushLine(string text, string font = "/F1", int size = 9, int leading = 13)
            {
                if (currentPageLines.Count >= 46)
                {
                    pages.Add(currentPageLines);
                    currentPageLines = [];
                }
                currentPageLines.Add($"{font} {size} Tf {leading} TL ({EscapePdfText(text)}) '");
            }

            void PushHeader(string title)
            {
                if (currentPageLines.Count >= 42)
                {
                    pages.Add(currentPageLines);
                    currentPageLines = [];
                }
                PushLine("", "/F1", 6, 8);
                PushLine($"--- {title.ToUpperInvariant()} ---", "/F2", 11, 16);
            }

            var ptBr = new CultureInfo("pt-BR");

            PushLine($"MINHAS FINANCAS - INFORME DE APOIO IRPF (ANO-BASE {baseYear})", "/F2", 14, 18);
            PushLine("Material de apoio ao preenchimento da Declaracao de Ajuste Anual. Nao substitui orientacao contabil.", "/F1", 8, 12);
            PushLine($"Gerado em: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC", "/F1", 8, 14);

            PushHeader("1. RESUMO DOS TOTAIS FISCAIS");
            PushLine($"Rendimentos Tributaveis: {preview.TotalTaxableIncome.ToString("C", ptBr)}", "/F1", 9, 13);
            PushLine($"Rendimentos Isentos e Nao Tributaveis: {preview.TotalExemptIncome.ToString("C", ptBr)}", "/F1", 9, 13);
            PushLine($"Rendimentos Sujeitos a Tributacao Exclusiva: {preview.TotalExclusiveIncome.ToString("C", ptBr)}", "/F1", 9, 13);
            PushLine($"Total de Pagamentos Efetuados (Deducoes): {preview.TotalDeductions.ToString("C", ptBr)}", "/F1", 9, 13);
            PushLine($"Total em Bens e Direitos (Custo de Aquisicao em 31/12): {preview.TotalAssetsCost.ToString("C", ptBr)}", "/F1", 9, 13);
            PushLine($"Total em Dividas e Onus Reais (Saldo em 31/12): {preview.TotalDebtsBalance.ToString("C", ptBr)}", "/F1", 9, 13);
            PushLine($"Patrimonio Liquido Fiscal (Bens - Dividas): {(preview.TotalAssetsCost - preview.TotalDebtsBalance).ToString("C", ptBr)}", "/F2", 9, 13);
            PushLine($"Pendencias / Alertas de Inconsistencia: {preview.PendingCount}", "/F1", 9, 14);

            PushHeader($"2. RENDIMENTOS TRIBUTAVEIS ({preview.TaxableIncome.Count})");
            if (preview.TaxableIncome.Count == 0)
            {
                PushLine("Nenhum rendimento tributavel identificado.");
            }
            else
            {
                foreach (var inc in preview.TaxableIncome)
                {
                    string doc = !string.IsNullOrEmpty(inc.PayerDocument) ? $" | Doc: {inc.PayerDocument}" : "";
                    PushLine($"* {inc.CategoryName}: {inc.TotalValue.ToString("C", ptBr)} - {inc.Description}{doc}");
                }
            }

            PushHeader($"3. RENDIMENTOS ISENTOS E NAO TRIBUTAVEIS ({preview.ExemptIncome.Count})");
            if (preview.ExemptIncome.Count == 0)
            {
                PushLine("Nenhum rendimento isento identificado.");
            }
            else
            {
                foreach (var inc in preview.ExemptIncome)
                {
                    PushLine($"* {inc.CategoryName}: {inc.TotalValue.ToString("C", ptBr)} - {inc.Description}");
                }
            }

            PushHeader($"4. TRIBUTACAO EXCLUSIVA / DEFINITIVA ({preview.ExclusiveIncome.Count})");
            if (preview.ExclusiveIncome.Count == 0)
            {
                PushLine("Nenhum rendimento de tributacao exclusiva identificado.");
            }
            else
            {
                foreach (var inc in preview.ExclusiveIncome)
                {
                    PushLine($"* {inc.CategoryName}: {inc.TotalValue.ToString("C", ptBr)} - {inc.Description}");
                }
            }

            PushHeader($"5. PAGAMENTOS EFETUADOS / DEDUCOES ({preview.Deductions.Count})");
            if (preview.Deductions.Count == 0)
            {
                PushLine("Nenhuma deducao identificada.");
            }
            else
            {
                foreach (var d in preview.Deductions)
                {
                    string rec = d.HasReceipt ? "[Comprovante Anexado]" : "[Sem Comprovante]";
                    string doc = !string.IsNullOrEmpty(d.PayeeDocument) ? $"CPF/CNPJ: {d.PayeeDocument}" : "Sem documento";
                    PushLine($"* [{d.Subtype.ToUpperInvariant()}] {d.Amount.ToString("C", ptBr)} | {d.PayeeName ?? d.Description} | {doc} {rec}");
                }
            }

            PushHeader($"6. BENS E DIREITOS EM 31/12 ({preview.Assets.Count})");
            if (preview.Assets.Count == 0)
            {
                PushLine("Nenhum bem ou direito cadastrado.");
            }
            else
            {
                foreach (var a in preview.Assets)
                {
                    PushLine($"* Cod {a.Code}: {a.Name} | 31/12/{baseYear - 1}: {a.SituationPreviousYear.ToString("C", ptBr)} | 31/12/{baseYear}: {a.SituationBaseYear.ToString("C", ptBr)}");
                    if (!string.IsNullOrWhiteSpace(a.Discrimination))
                    {
                        string desc = a.Discrimination.Length > 85 ? a.Discrimination[..82] + "..." : a.Discrimination;
                        PushLine($"    Discriminacao: {desc}", "/F1", 8, 11);
                    }
                }
            }

            PushHeader($"7. DIVIDAS E ONUS REAIS EM 31/12 ({preview.Debts.Count})");
            if (preview.Debts.Count == 0)
            {
                PushLine("Nenhuma divida cadastrada.");
            }
            else
            {
                foreach (var debt in preview.Debts)
                {
                    string cred = !string.IsNullOrEmpty(debt.CreditorName) ? $" | Credor: {debt.CreditorName}" : "";
                    PushLine($"* Cod {debt.Code}: {debt.Name}{cred} | 31/12/{baseYear - 1}: {debt.SituationPreviousYear.ToString("C", ptBr)} | 31/12/{baseYear}: {debt.SituationBaseYear.ToString("C", ptBr)}");
                }
            }

            if (preview.Pendings.Count > 0)
            {
                PushHeader($"8. PENDENCIAS E INCONSISTENCIAS ({preview.Pendings.Count})");
                foreach (var p in preview.Pendings)
                {
                    PushLine($"[!] {p.Title}: {p.Description}");
                }
            }

            if (currentPageLines.Count > 0)
            {
                pages.Add(currentPageLines);
            }
            if (pages.Count == 0)
            {
                pages.Add(["/F1 10 Tf 14 TL (Nenhum dado encontrado para o periodo) '"]);
            }

            byte[] pdfBytes = AssemblePdf(pages);
            using var sha = SHA256.Create();
            string sha256Hex = Convert.ToHexString(sha.ComputeHash(pdfBytes)).ToLowerInvariant();

            return (pdfBytes, sha256Hex);
        }

        private static string EscapePdfText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    if (c == '(' || c == ')' || c == '\\') sb.Append('\\').Append(c);
                    else if (c >= 32 && c <= 126) sb.Append(c);
                    else if (c == '§') sb.Append("Par.");
                    else sb.Append(' ');
                }
            }
            return sb.ToString();
        }

        private static byte[] AssemblePdf(List<List<string>> pages)
        {
            using var ms = new MemoryStream();
            var offsets = new List<long>();

            void Write(string s)
            {
                byte[] b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            Write("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

            offsets.Add(ms.Position);
            Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            int pageCount = pages.Count;
            var kids = new StringBuilder();
            for (int i = 0; i < pageCount; i++)
            {
                kids.Append($"{5 + i * 2} 0 R ");
            }
            offsets.Add(ms.Position);
            Write($"2 0 obj\n<< /Type /Pages /Kids [{kids.ToString().Trim()}] /Count {pageCount} >>\nendobj\n");

            offsets.Add(ms.Position);
            Write("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");

            offsets.Add(ms.Position);
            Write("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

            for (int i = 0; i < pageCount; i++)
            {
                int pageObjId = 5 + i * 2;
                int contentObjId = pageObjId + 1;

                var sbStream = new StringBuilder();
                sbStream.AppendLine("BT");
                sbStream.AppendLine("40 790 Td");
                foreach (var line in pages[i])
                {
                    sbStream.AppendLine(line);
                }
                sbStream.AppendLine("ET");

                byte[] streamBytes = Encoding.ASCII.GetBytes(sbStream.ToString());

                offsets.Add(ms.Position);
                Write($"{pageObjId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents {contentObjId} 0 R /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >>\nendobj\n");

                offsets.Add(ms.Position);
                Write($"{contentObjId} 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
                ms.Write(streamBytes, 0, streamBytes.Length);
                Write("\nendstream\nendobj\n");
            }

            long xrefOffset = ms.Position;
            int totalObjs = offsets.Count + 1;
            Write($"xref\n0 {totalObjs}\n0000000000 65535 f \n");
            foreach (var off in offsets)
            {
                Write($"{off:D10} 00000 n \n");
            }

            Write($"trailer\n<< /Size {totalObjs} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
