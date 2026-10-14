# Documentação de Deploy em VPS com Docker e CI/CD

Guia prático e definitivo para configuração de infraestrutura, publicação de múltiplas APIs, banco de dados isolado e automação contínua via GitHub Actions.

---

## 1. Visão Geral da Arquitetura

- **APIs (.NET / Node / Python)**: Containers Docker executando em portas internas exclusivas (ex: `5140` para `my-finances`, `5141` para `camem`).
- **Bancos de Dados (MongoDB / Postgres / MySQL)**: Containers em rede bridge isolada, sem expor portas públicas desnecessárias para a internet.
- **Nginx (Reverse Proxy)**: Recebe requisições públicas HTTP/HTTPS (portas 80 e 443) e encaminha via `proxy_pass` para as portas internas de cada container.
- **Certbot (Let's Encrypt)**: Emissão e renovação automática de certificados SSL gratuitos por domínio.
- **GitHub Actions**: Pipeline SSH que atualiza o código e reinicia os containers a cada `git push origin main`.

---

## 2. Preparação do Servidor (VPS)

### 2.1 Conexão Inicial e Usuários (`root` vs `deployer`)
Para administrar o servidor (instalar pacotes, configurar Nginx, certificados), utilize o usuário `root`.
Para executar deploys de aplicações, repositórios Git e containers Docker, utilize o usuário `deployer`.

```bash
# Conectar como root a partir da sua máquina local:
ssh root@191.252.191.227

# Alternar do root para o deployer:
su - deployer

# Sair do deployer e voltar para o root:
exit
```

---

### 2.2 Criar o Usuário `deployer` e Conceder Permissões
Execute como **`root`**:
```bash
# 1. Criar o usuário caso não exista:
adduser --disabled-password --gecos "" deployer

# 2. Adicionar ao grupo do Docker:
usermod -aG docker deployer

# 3. Configurar sudo sem senha (IMPORTANTE para evitar erros de senha ao editar configurações do sistema):
echo "deployer ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/deployer
chmod 440 /etc/sudoers.d/deployer
```

---

### 2.3 Estrutura de Pastas e Permissões
Execute como **`root`**:
```bash
mkdir -p /home/deployer/www/projects
chown -R deployer:deployer /home/deployer/www
chmod -R 755 /home/deployer/www
```

> **Atenção:** Nunca clone projetos dentro de `/root/www` ou tente mover arquivos de `/root` com o usuário `deployer`, pois ocorrerá erro de `Permission denied`. Sempre clone diretamente em `/home/deployer/www/projects/<nome-do-projeto>`.

---

### 2.4 Gerar Chave SSH para o GitHub Actions
Execute como **`deployer`** (`su - deployer`):
```bash
mkdir -p ~/.ssh && chmod 700 ~/.ssh
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/id_ed25519 -N ""
cat ~/.ssh/id_ed25519.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

# Exibir a chave privada para copiar:
cat ~/.ssh/id_ed25519
```
> Copie o conteúdo completo da chave privada (desde `-----BEGIN OPENSSH PRIVATE KEY-----` até `-----END OPENSSH PRIVATE KEY-----`) para cadastrar no GitHub Secrets (`VPS_SSH_KEY`).

---

## 3. Publicando Múltiplos Projetos (Ex: `api-finances` e `api-camem`)

Cada novo projeto deve ter:
1. Sua própria pasta em `/home/deployer/www/projects/<projeto>`.
2. Sua própria porta externa mapeada no `docker-compose.yml` (sem conflitar com outras).
3. Seu próprio arquivo de configuração no Nginx em `/etc/nginx/sites-available/`.

### 3.1 Exemplo: `docker-compose.yml` da API `camem`
```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: api-camem
    restart: unless-stopped
    ports:
      - "5141:8080" # Porta 5141 no host (5140 já está em uso pelo my-finances)
    env_file:
      - .env
    networks:
      - camem_network

networks:
  camem_network:
    driver: bridge
```

---

## 4. Configuração do Nginx e Domínios

### 4.1 Criar Configuração de Virtual Host no Nginx
> **Erro comum:** Digitar apenas `/etc/nginx/sites-available/camem.conf` no terminal tenta executar o arquivo como um script e retorna `No such file or directory`. É obrigatório usar um editor (ex: `nano`) com privilégios de administrador (`sudo`).

Como `deployer` (com sudo) ou como `root`:
```bash
sudo nano /etc/nginx/sites-available/camem.conf
```

Cole o conteúdo ajustando `server_name` e a porta de redirecionamento (`proxy_pass`):
```nginx
server {
    server_name camem.seu-dominio.com;

    location / {
        proxy_pass http://127.0.0.1:5141;
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

---

### 4.2 Ativar o Site e Recarregar o Nginx
```bash
# 1. Criar link simbólico para habilitar o site:
sudo ln -s /etc/nginx/sites-available/camem.conf /etc/nginx/sites-enabled/

# 2. Testar sintaxe dos arquivos do Nginx:
sudo nginx -t

# 3. Recarregar o Nginx:
sudo systemctl reload nginx
```

---

### 4.3 Gerar Certificado SSL (HTTPS)
```bash
sudo certbot --nginx -d camem.seu-dominio.com
```

---

## 5. Automação de Deploy Contínuo (GitHub Actions)

### 5.1 Secrets no Repositório GitHub
Em `Settings > Secrets and variables > Actions > New repository secret`:
- `VPS_HOST`: IP da VPS (ex: `191.252.191.227`)
- `VPS_USER`: `deployer`
- `VPS_SSH_KEY`: Chave privada OpenSSH gerada no Passo 2.4

---

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
            cd /home/deployer/www/projects/camem/api-camem
            git pull origin main
            docker compose up -d --build
```

---

## 6. Guia de Solução de Erros Frequentes (Troubleshooting)

### Erro: `mv: cannot stat '/root/www/projects': Permission denied`
- **Causa:** O usuário `deployer` não tem permissão para acessar a pasta `/root`.
- **Solução:** Nunca faça clones ou operações dentro de `/root`. Crie e use exclusivamente o caminho `/home/deployer/www/projects/`.

---

### Erro: `-bash: /etc/nginx/sites-available/camem.conf: No such file or directory`
- **Causa:** O caminho do arquivo foi digitado diretamente no Bash sem o comando de um editor de texto.
- **Solução:** Utilize `sudo nano /etc/nginx/sites-available/camem.conf`.

---

### Erro: `[sudo] password for deployer: Sorry, try again.`
- **Causa:** O usuário `deployer` não tem permissão de `sudo` sem senha ou a senha não foi informada.
- **Solução:** No usuário `root`, execute:
  ```bash
  echo "deployer ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/deployer
  chmod 440 /etc/sudoers.d/deployer
  ```
  Ou edite as configurações do Nginx diretamente logado como `root`.

---

### Erro: `bind: address already in use` no Docker
- **Causa:** Duas aplicações tentando expor a mesma porta no host da VPS (ex: dois containers usando a porta `5140`).
- **Solução:** Altere a porta mapeada à esquerda no `docker-compose.yml` do segundo projeto:
  ```yaml
  ports:
    - "5141:8080" # 5141 é a porta externa no host, 8080 é a interna do container
  ```
  E aponte o `proxy_pass` no arquivo do Nginx para `http://127.0.0.1:5141;`.

---

### Erro: `AuthenticationFailed (code 18)` no MongoDB (Studio 3T ou API)
- **Causa:** A senha do banco foi alterada no `.env` depois que o volume do MongoDB já havia sido criado pela primeira vez.
- **Solução:**
  1. A senha gravada no volume persistente não muda apenas alterando o `.env`.
  2. Para resetar as credenciais (em ambiente de desenvolvimento/staging):
     ```bash
     docker compose down -v
     docker compose up -d
     ```
  3. No Studio 3T, conecte via **SSH Tunnel**:
     - **SSH Host:** IP da VPS | **User:** `deployer` ou `root` | **Auth:** Chave SSH ou Senha.
     - **Server:** `localhost:27017` (ou `mongodb:27017` se o túnel apontar para o container).
     - **Authentication:** `SCRAM-SHA-256`, User: `admin`, Database: `admin`.

---

## 7. Comandos Úteis do Dia a Dia

```bash
# Ver status dos containers em execução
docker compose ps

# Ver logs da API em tempo real
docker compose logs -f api

# Reiniciar serviços sem perda de dados
docker compose restart

# Rebuildar imagens após alterar Dockerfile ou código
docker compose up -d --build

# Testar sintaxe do Nginx
sudo nginx -t

# Recarregar Nginx sem derrubar conexões
sudo systemctl reload nginx

# Monitorar uso de CPU e Memória da VPS
docker stats
```
