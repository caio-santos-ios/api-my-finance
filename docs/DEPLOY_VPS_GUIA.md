# Documentação de Deploy em VPS com Docker e CI/CD

Guia prático e definitivo para configuração de infraestrutura, publicação de APIs, banco de dados isolado e automação contínua via GitHub Actions.

---

## 1. Visão Geral da Arquitetura

- **API (.NET)**: Container Docker executando na porta interna `5140`.
- **MongoDB**: Container na mesma rede bridge (`finances_network`), sem expor portas públicas para a internet.
- **Nginx (Reverse Proxy)**: Recebe requisições públicas HTTPS (porta 443) e encaminha para `http://127.0.0.1:5140`.
- **Certbot (Let's Encrypt)**: Certificados SSL gratuitos com renovação automática.
- **GitHub Actions**: Pipeline SSH que atualiza o código e reinicia os containers a cada `git push origin main`.

---

## 2. Preparação do Servidor (VPS)

### 2.1 Criar o Usuário do Deploy
```bash
adduser --disabled-password --gecos "" deployer
usermod -aG docker deployer
```

### 2.2 Estrutura de Pastas e Permissões
```bash
mkdir -p /home/deployer/www/projects
chown -R deployer:deployer /home/deployer/www
```

### 2.3 Gerar Chave SSH para o GitHub Actions
```bash
su - deployer
mkdir -p ~/.ssh && chmod 700 ~/.ssh
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/id_ed25519 -N ""
cat ~/.ssh/id_ed25519.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
cat ~/.ssh/id_ed25519
```
> Copie o conteúdo completo da chave privada (desde `-----BEGIN OPENSSH PRIVATE KEY-----` até `-----END OPENSSH PRIVATE KEY-----`).

---

## 3. Configuração do Docker

### 3.1 Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5140
EXPOSE 5140
ENTRYPOINT ["dotnet", "api-finances.dll"]
```

### 3.2 docker-compose.yml
```yaml
services:
  mongodb:
    image: mongo:7.0
    container_name: mongodb-finances
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME:-admin}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD:-admin123}
      MONGO_INITDB_DATABASE: ${DATABASE_NAME:-finances}
    volumes:
      - mongo_data:/data/db
    networks:
      - finances_network

  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: api-finances
    restart: unless-stopped
    depends_on:
      - mongodb
    ports:
      - "5140:5140"
    env_file:
      - .env
    volumes:
      - api_uploads:/app/wwwroot/uploads
    networks:
      - finances_network

volumes:
  mongo_data:
  api_uploads:

networks:
  finances_network:
    driver: bridge
```

### 3.3 Arquivo .env de Produção
> **Atenção:** Caracteres como `@` na senha do MongoDB devem ser codificados como `%40` na `CONNECTION_STRING` ou use senhas sem `@`.
```env
MONGO_INITDB_ROOT_USERNAME=admin
MONGO_INITDB_ROOT_PASSWORD=suaSenhaSegura123
DATABASE_NAME=finances
CONNECTION_STRING=mongodb://admin:suaSenhaSegura123@mongodb:27017/finances?authSource=admin
IS_SSL=false
SECRET_KEY=suaChaveJwtComMaisDe32Caracteres
ISSUER=FinanceIssuer
AUDIENCE=FinanceAudience
ALLOWED_ORIGINS=https://seu-front.vercel.app
UI_URL=https://seu-front.vercel.app
```

---

## 4. Domínio, Nginx e Certificado SSL

### 4.1 Configuração do Nginx (`/etc/nginx/sites-available/my-finances.conf`)
```nginx
server {
    server_name seu-dominio.duckdns.org;

    location / {
        proxy_pass http://127.0.0.1:5140;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 4.2 Ativação e Certificado HTTPS
```bash
ln -s /etc/nginx/sites-available/my-finances.conf /etc/nginx/sites-enabled/
nginx -t
systemctl reload nginx
certbot --nginx -d seu-dominio.duckdns.org
```

---

## 5. Automação de Deploy Contínuo (GitHub Actions)

### 5.1 Secrets no Repositório GitHub
No repositório da API (`Settings > Secrets and variables > Actions`):
- `VPS_HOST`: IP da VPS (ex: `191.252.191.227`)
- `VPS_USER`: `deployer`
- `VPS_SSH_KEY`: Chave privada gerada no Passo 2.3

### 5.2 Workflow `.github/workflows/deploy.yml`
```yaml
name: Deploy API to VPS

on:
  push:
    branches:
      - main

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
      - name: Deploy via SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          port: 22
          script: |
            cd /home/deployer/www/projects/my-finances/api-my-finance
            git pull origin main
            docker compose up -d --build
```

---

## 6. Comandos Úteis e Manutenção

```bash
# Ver logs da API em tempo real
docker compose logs -f api

# Ver logs do MongoDB em tempo real
docker compose logs -f mongodb

# Reiniciar serviços após alterar o .env
docker compose up -d

# Resetar o banco e recriar o volume com nova senha
docker compose down -v
docker compose up -d

# Monitorar uso de CPU e memória
docker stats
```
