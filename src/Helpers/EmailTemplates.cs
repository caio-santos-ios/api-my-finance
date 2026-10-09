namespace api_finances.src.Helpers
{
  public static class EmailTemplates
  {
    private static string GetUiUrl()
    {
      string url = Environment.GetEnvironmentVariable("UI_URL") ?? "";
      if (string.IsNullOrWhiteSpace(url))
      {
        url = "http://localhost:4300";
      }
      return url.TrimEnd('/');
    }

    private static bool IsLightColor(string hex)
    {
      if (string.IsNullOrWhiteSpace(hex)) return true;
      hex = hex.Trim().TrimStart('#');
      if (hex.Length != 6) return true;
      if (int.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out int r) &&
          int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out int g) &&
          int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out int b))
      {
        double luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.6;
      }
      return true;
    }

    public static string GetAccountConfirmationTemplate(string? name, string code)
    {
      string greeting = !string.IsNullOrWhiteSpace(name) ? $"Ol&#225;, <strong>{System.Net.WebUtility.HtmlEncode(name)}</strong>!" : "Ol&#225;!";
      string link = $"{GetUiUrl()}/confirmation/{code}";
      string clinicName = "Minhas Finanças";

      return BuildTemplate(
          title: "Confirma&#231;&#227;o de Cadastro",
          preheader: $"Confirme sua conta na {clinicName}: {link}",
          greeting: greeting,
          intro: "Seja muito bem-vindo(a)! Para concluir a cria&#231;&#227;o da sua conta e ativar seu acesso &#224; plataforma, clique no bot&#227;o abaixo ou utilize o c&#243;digo de verifica&#231;&#227;o:",
          code: code,
          link: link,
          buttonText: "Confirmar Minha Conta",
          notice: "Se voc&#234; n&#227;o realizou esta solicita&#231;&#227;o de cadastro, nenhuma a&#231;&#227;o adicional &#233; necess&#225;ria. Este e-mail pode ser desconsiderado com seguran&#231;a.",
          badgeText: "CONFIRMA&#199;&#195;O DE CONTA"

      );
    }

    public static string GetPasswordResetTemplate(string? name, string code)
    {
      string greeting = !string.IsNullOrWhiteSpace(name) ? $"Ol&#225;, <strong>{System.Net.WebUtility.HtmlEncode(name)}</strong>!" : "Ol&#225;!";
      string link = $"{GetUiUrl()}/confirmation/{code}";
      string clinicName = "Minhas Finanças";

      return BuildTemplate(
          title: "Redefini&#231;&#227;o de Senha",
          preheader: $"Redefina sua senha na {clinicName}: {link}",
          greeting: greeting,
          intro: "Recebemos uma solicita&#231;&#227;o para redefinir a senha de acesso da sua conta. Clique no bot&#227;o abaixo para redefinir ou utilize o c&#243;digo de verifica&#231;&#227;o:",
          code: code,
          link: link,
          buttonText: "Redefinir Minha Senha",
          notice: "Se voc&#234; n&#227;o solicitou a redefini&#231;&#227;o de senha, desconsidere esta mensagem. Sua conta e senha atuais continuam totalmente seguras.",
          badgeText: "RECUPERA&#199;&#195;O DE SENHA"
      );
    }

    private static string BuildTemplate(string title, string preheader, string greeting, string intro, string code, string link, string buttonText, string notice, string badgeText)
    {
      int year = DateTime.UtcNow.Year;
      string clinicName = "Minhas Finanças";
      string primaryColor = "#dca311";
      string secondaryColor = "#0b1120";
      string logo = "";
      string btnTextColor = IsLightColor(primaryColor) ? "#0b1120" : "#ffffff";

      string headerBrand = !string.IsNullOrWhiteSpace(logo)
          ? $@"<img src=""{logo}"" alt=""{clinicName}"" style=""max-height: 48px; max-width: 220px; display: block; margin: 0 auto; object-fit: contain;"" />"
          : $@"<table cellpadding=""0"" cellspacing=""0"" border=""0"" align=""center"" style=""margin: 0 auto;"">
                    <tr>
                      <td style=""background-color: {primaryColor}; width: 36px; height: 36px; border-radius: 8px; text-align: center; vertical-align: middle; font-size: 20px; font-weight: bold; color: {secondaryColor}; line-height: 36px;"">
                        &#10010;
                      </td>
                      <td style=""padding-left: 12px; font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; font-size: 22px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px;"">
                        {clinicName}
                      </td>
                    </tr>
                  </table>";

      return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f1f5f9; font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
  <div style=""display: none; max-height: 0px; overflow: hidden; mso-hide: all; font-size: 1px; line-height: 1px; color: #ffffff; opacity: 0;"">
    {preheader}
  </div>

  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #f1f5f9; padding: 40px 16px;"">
    <tr>
      <td align=""center"">
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 580px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05), 0 8px 10px -6px rgba(0, 0, 0, 0.04); border: 1px solid #e2e8f0;"">
          <tr>
            <td style=""background-color: {secondaryColor}; padding: 28px 24px; text-align: center; border-bottom: 3px solid {primaryColor};"">
              {headerBrand}
            </td>
          </tr>

          <tr>
            <td style=""padding: 36px 32px 28px 32px;"">
              <div style=""display: inline-block; padding: 5px 14px; background-color: #f8fafc; border: 1px solid #e2e8f0; color: #334155; border-radius: 9999px; font-size: 11px; font-weight: 700; letter-spacing: 0.5px; margin-bottom: 16px;"">
                <span style=""display: inline-block; width: 8px; height: 8px; border-radius: 50%; background-color: {primaryColor}; margin-right: 6px; vertical-align: middle;""></span>
                {badgeText}
              </div>

              <h1 style=""margin: 0 0 16px 0; font-size: 24px; font-weight: 800; color: #0f172a; line-height: 1.25;"">
                {title}
              </h1>

              <p style=""margin: 0 0 12px 0; font-size: 15px; color: #334155; line-height: 1.6;"">
                {greeting}
              </p>

              <p style=""margin: 0 0 24px 0; font-size: 15px; color: #475569; line-height: 1.6;"">
                {intro}
              </p>

              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin: 28px 0 24px 0;"">
                <tr>
                  <td align=""center"">
                    <a href=""{link}"" target=""_blank"" style=""display: inline-block; background-color: {primaryColor}; color: {btnTextColor}; font-size: 15px; font-weight: 700; text-decoration: none; padding: 14px 32px; border-radius: 10px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12); text-align: center;"">
                      {buttonText}
                    </a>
                  </td>
                </tr>
              </table>

              <div style=""text-align: center; margin: 20px 0 8px 0; font-size: 13px; color: #64748b;"">
                Ou se preferir, utilize o c&#243;digo de verifica&#231;&#227;o:
              </div>

              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin: 10px 0 20px 0; background-color: #fafafa; border: 2px dashed {primaryColor}; border-radius: 12px; text-align: center;"">
                <tr>
                  <td style=""padding: 20px 16px;"">
                    <div style=""font-size: 11px; font-weight: 700; color: {primaryColor}; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 8px;"">
                      C&#211;DIGO DE VERIFICA&#199;&#195;O
                    </div>
                    <div style=""font-family: 'Consolas', 'Courier New', Courier, monospace; font-size: 36px; font-weight: 800; letter-spacing: 12px; color: #0f172a; padding-left: 12px;"">
                      {code}
                    </div>
                    <div style=""font-size: 12px; color: #64748b; margin-top: 8px; font-weight: 500;"">
                      &#9201; Este c&#243;digo expira em <strong>5 minutos</strong>
                    </div>
                  </td>
                </tr>
              </table>

              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #f8fafc; border-left: 4px solid {primaryColor}; border-radius: 6px; margin: 20px 0;"">
                <tr>
                  <td style=""padding: 14px 16px; font-size: 13px; line-height: 1.5; color: #64748b;"">
                    {notice}
                  </td>
                </tr>
              </table>

              <p style=""margin: 20px 0 0 0; font-size: 12px; color: #94a3b8; line-height: 1.5; text-align: center; word-break: break-all;"">
                Se o bot&#227;o n&#227;o abrir, copie e cole o link abaixo em seu navegador:<br>
                <a href=""{link}"" target=""_blank"" style=""color: {primaryColor}; text-decoration: underline;"">{link}</a>
              </p>

              <p style=""margin: 20px 0 0 0; font-size: 12px; color: #94a3b8; line-height: 1.5; text-align: center;"">
                &#128274; Por seguran&#231;a, nunca compartilhe este link ou c&#243;digo com terceiros.
              </p>
            </td>
          </tr>

          <tr>
            <td style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 24px 20px; text-align: center; font-size: 12px; color: #94a3b8; line-height: 1.6;"">
              <strong style=""color: #64748b;"">{clinicName}</strong> &bull; Sistema de Gest&#227;o para Cl&#237;nicas e Consult&#243;rios<br>
              Este &#233; um e-mail autom&#225;tico do sistema, por favor n&#227;o responda.<br>
              &copy; {year} {clinicName}. Todos os direitos reservados.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
  }
}
