# Guia Definitivo de Deploy em VPS com Docker, Nginx e CI/CD

Este guia contém o padrão exato de arquivos, configurações e comandos práticos utilizados na VPS para que qualquer novo projeto funcione exatamente igual ao `my-finances`, sem erros de portas, strings de conexão ou Nginx.

---

## 1. Regra Fundamental de Arquitetura

1. **Apenas a API expõe porta no host da VPS** (ex: `5140:5140` para finanças, `5143:5143` para camem).
2. **O MongoDB NUNCA expõe porta no host** (`ports:` NÃO existe no serviço mongodb do docker-compose). A API conversa com o MongoDB internamente pela rede bridge (`camem_network`) na porta padrão `mongodb:27017`.
3. **Se você expuser porta no MongoDB (`27017` ou `27018`)**, haverá conflito (`port is already allocated`) porque múltiplos bancos tentarão disputar a mesma porta na VPS.

---

## 2. Preparação do Servidor (Executar apenas 1 vez como `root`)

```bash
# Conectar como root:
ssh root@191.252.191.227

# 1. Criar o usuário de deploy:
adduser --disabled-password --gecos "" deployer
usermod -aG docker deployer

# 2. Conceder permissão sudo sem senha (para nunca pedir [sudo] password):
echo "deployer ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/deployer
chmod 440 /etc/sudoers.d/deployer

# 3. Criar diretório padrão de projetos:
mkdir -p /home/deployer/www/projects
chown -R deployer:deployer /home/deployer/www
chmod -R 755 /home/deployer/www

# 4. Gerar chave SSH para o GitHub Actions:
su - deployer
mkdir -p ~/.ssh && chmod 700 ~/.ssh
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/id_ed25519 -N ""
cat ~/.ssh/id_ed25519.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
cat ~/.ssh/id_ed25519 # Copie a chave privada para o GitHub Secrets (VPS_SSH_KEY)
exit
```

---

## 3. Passo a Passo para Publicar Qualquer Projeto (Ex: `camem`)

Execute como usuário **`deployer`**:

### Passo 1: Entrar com o Usuário `deployer` e Clonar
```bash
su - deployer
cd /home/deployer/www/projects
git clone https://github.com/seu-usuario/api-camem.git
cd api-camem
```

---

### Passo 2: O Arquivo `docker-compose.yml` Padrão
> **Atenção:** Observe que no bloco `mongodb:` **NÃO há mapeamento de `ports:`**. Isso impede qualquer erro de `port is already allocated`.

Crie ou ajuste o `docker-compose.yml`:
```yaml
services:
  mongodb:
    image: mongo:7.0
    container_name: mongodb-camem
    restart: unless-stopped
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_INITDB_ROOT_USERNAME:-admin}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_INITDB_ROOT_PASSWORD:-admin123}
      MONGO_INITDB_DATABASE: ${DATABASE_NAME:-camem}
    volumes:
      - mongo_camem_data:/data/db
    networks:
      - camem_network

  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: api-camem
    restart: unless-stopped
    depends_on:
      - mongodb
    ports:
      - "5143:5143" # Apenas a API expõe porta externa no host!
    env_file:
      - .env
    volumes:
      - api_camem_uploads:/app/wwwroot/uploads
    networks:
      - camem_network

volumes:
  mongo_camem_data:
  api_camem_uploads:

networks:
  camem_network:
    driver: bridge
```

---

### Passo 3: O Arquivo `.env` de Produção
Crie o arquivo `.env`:
```bash
nano .env
```

Modelo exato para copiar e preencher (sem espaços na `CONNECTION_STRING`):
```env
MONGO_INITDB_ROOT_USERNAME=admin
MONGO_INITDB_ROOT_PASSWORD=suaSenhaForte123
DATABASE_NAME=camem
CONNECTION_STRING=mongodb://admin:suaSenhaForte123@mongodb:27017/camem?authSource=admin
IS_SSL=false
SECRET_KEY=suaChaveSecretaComMaisDe32CaracteresAqui!
ISSUER=CamemIssuer
AUDIENCE=CamemAudience
ALLOWED_ORIGINS=https://camem.seudominio.com,http://localhost:3000
UI_URL=https://camem.seudominio.com
```
*(Salve com `Ctrl + O`, `Enter` e saia com `Ctrl + X`)*.

---

### Passo 4: Subir os Containers
Execute sempre com `-d` (segundo plano):
```bash
docker compose up -d --build
```

Para conferir se estão rodando:
```bash
docker compose ps
```

Para verificar logs da API:
```bash
docker compose logs -f api
```

---

## 4. Configuração do Nginx e Certificado SSL

Execute como `root` ou com `sudo`:

### Passo 1: Criar o Arquivo no Nginx
```bash
sudo nano /etc/nginx/sites-available/camem.conf
```

Cole a configuração exata (ajustando o domínio e a porta `5143`):
```nginx
server {
    server_name camem-caio-santos-ios.duckdns.org;

    location / {
        proxy_pass http://127.0.0.1:5143;
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
*(Salve com `Ctrl + O`, `Enter` e saia com `Ctrl + X`)*.

### Passo 2: Ativar o Site e Testar a Sintaxe
```bash
sudo ln -s /etc/nginx/sites-available/camem.conf /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Passo 3: Gerar Certificado SSL (HTTPS)
> **Obrigatório para evitar o erro `net::ERR_CERT_COMMON_NAME_INVALID`:**
```bash
sudo certbot --nginx -d camem-caio-santos-ios.duckdns.org
```
*(Se perguntar se deseja redirecionar o tráfego HTTP para HTTPS, confirme. Depois execute `sudo systemctl reload nginx`)*.

---

## 5. Automação CI/CD (GitHub Actions)

### 5.1 Cadastrar Secrets no Repositório GitHub
Em `Settings > Secrets and variables > Actions > New repository secret`:
- `VPS_HOST`: `191.252.191.227`
- `VPS_USER`: `deployer`
- `VPS_SSH_KEY`: Conteúdo da chave privada `id_ed25519`

### 5.2 Arquivo `.github/workflows/deploy.yml` no Projeto
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
            cd /home/deployer/www/projects/camem/api-camem
            git pull origin main
            docker compose up -d --build
```

---

## 6. Tabela Completa de Erros Práticos e Soluções

| Erro Real | Causa | Solução Imediata |
| :--- | :--- | :--- |
| **`Bind for 127.0.0.1:27018 failed: port is already allocated`** | Bloco `ports:` colocado no serviço `mongodb:` do `docker-compose.yml`. | **Remova o `ports:` do mongodb**. A API conecta internamente pelo nome do serviço: `@mongodb:27017`. |
| **`System.Exception: The connection string 'mongodb: admin:...' is not valid`** | Espaço em branco ou ausência das duas barras `//` no `.env`. | Deixe exatamente: `CONNECTION_STRING=mongodb://user:senha@mongodb:27017/banco?authSource=admin`. |
| **`(failed) net::ERR_CERT_COMMON_NAME_INVALID`** | Domínio acessado via HTTPS antes de gerar o certificado do novo subdomínio. | Execute: `sudo certbot --nginx -d seu-subdominio.duckdns.org` e recarregue o Nginx. |
| **`invalid number of arguments in "proxy_pass"`** | Espaço entre `http:` e o IP (ex: `http: 127.0.0.1`) ou falta de `;` no fim. | Corrija no arquivo do Nginx para: `proxy_pass http://127.0.0.1:PORTA;`. |
| **`invalid URL prefix in /etc/nginx/...`** | Faltaram as duas barras no `http://`. | Deixe `http://127.0.0.1:PORTA;` e teste com `sudo nginx -t`. |
| **`[sudo] password for deployer: Sorry, try again.`** | Usuário `deployer` sem regra no `/etc/sudoers.d/`. | Como `root`, execute: `echo "deployer ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/deployer`. |
| **`-bash: /etc/nginx/...: No such file or directory`** | Caminho do arquivo colado direto no terminal sem chamar um editor. | Chame o editor: `sudo nano /etc/nginx/sites-available/nome.conf`. |
| **Terminal preso nos logs após subir containers** | Comando rodado sem a flag `-d`. | Pressione `Ctrl + C` e rode: `docker compose up -d`. |
| **`mv: cannot stat '/root/www/...': Permission denied`** | Arquivos clonados ou manipulados em `/root`. | Trabalhe sempre exclusivamente em `/home/deployer/www/projects/`. |

---

## 7. Como Acessar o MongoDB pelo Studio 3T (Sem Expor Portas)

Como o MongoDB não expõe portas públicas na VPS por segurança, conecte via **SSH Tunnel**:
1. No Studio 3T, crie uma nova conexão.
2. Na aba **SSH Tunnel**:
   - Marque **Use SSH Tunnel**.
   - **SSH Host:** `191.252.191.227` | **Port:** `22`.
   - **User:** `deployer` (ou `root`) | **Authentication:** sua Chave Privada SSH ou senha.
3. Na aba **Server**:
   - Se você conectar no container diretamente pela rede Docker ou mapeamento localhost:
   - Defina Server: `127.0.0.1` e porta correspondente (caso tenha mapeado internamente) ou use o IP interno do container.
4. Na aba **Authentication**:
   - **Mode:** `SCRAM-SHA-256`
   - **User:** `admin` | **Database:** `admin`
