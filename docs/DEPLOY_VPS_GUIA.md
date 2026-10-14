# Guia Definitivo de Deploy em VPS com Docker, Nginx e CI/CD

Este guia contém o passo a passo exato, validado na prática, para configuração da VPS, publicação de novas aplicações (.NET/Docker), múltiplos projetos independentes, banco de dados isolado, proxy reverso Nginx com SSL e automação via GitHub Actions.

---

## 1. Visão Geral da Arquitetura

- **Containers Docker**: Cada projeto roda de forma isolada na sua própria rede interna e com uma porta externa exclusiva no host da VPS.
  - Exemplo 1: `api-finances` escutando na porta `5140`.
  - Exemplo 2: `api-camem` escutando na porta `5143`.
- **Bancos de Dados**: MongoDB em container Docker com rede bridge interna, sem expor portas públicas para a internet (acesso externo seguro via SSH Tunnel).
- **Nginx (Reverse Proxy)**: Recebe o tráfego HTTP/HTTPS das portas 80 e 443 e faz o encaminhamento (`proxy_pass`) para a porta interna de cada aplicação de acordo com o subdomínio/domínio.
- **Certbot (Let's Encrypt)**: Certificados SSL individuais emitidos para cada domínio/subdomínio.
- **GitHub Actions**: Pipeline contínua via SSH com o usuário `deployer`.

---

## 2. Preparação Única do Servidor (Executar como `root`)

Ao receber a VPS ou preparar o ambiente, execute os comandos abaixo diretamente como **`root`**:

### 2.1 Acessar a VPS como root
```bash
ssh root@191.252.191.227
```

### 2.2 Criar o Usuário `deployer` com Permissões de Docker e Sudo sem Senha
> **Por que configurar sudo sem senha?** Para que o usuário `deployer` consiga gerenciar arquivos de configuração e comandos administrativos sem travar pedindo `[sudo] password for deployer`.

```bash
# 1. Criar o usuário:
adduser --disabled-password --gecos "" deployer

# 2. Adicionar ao grupo docker:
usermod -aG docker deployer

# 3. Conceder permissão sudo NOPASSWD:
echo "deployer ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/deployer
chmod 440 /etc/sudoers.d/deployer
```

### 2.3 Criar a Estrutura de Pastas de Projetos
> **Regra Obrigatória:** Nunca clone repositórios dentro de `/root`. Todos os projetos devem ficar em `/home/deployer/www/projects`.

```bash
mkdir -p /home/deployer/www/projects
chown -R deployer:deployer /home/deployer/www
chmod -R 755 /home/deployer/www
```

### 2.4 Gerar Chave SSH para GitHub Actions
```bash
su - deployer
mkdir -p ~/.ssh && chmod 700 ~/.ssh
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/id_ed25519 -N ""
cat ~/.ssh/id_ed25519.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

# Exibir a chave privada para copiar e colar no GitHub Secrets:
cat ~/.ssh/id_ed25519
```
> Copie todo o conteúdo gerado (de `-----BEGIN OPENSSH PRIVATE KEY-----` até `-----END OPENSSH PRIVATE KEY-----`).

---

## 3. Publicando um Novo Projeto (Exemplo Completo: `api-camem`)

Para cada novo projeto a ser publicado, siga rigorosamente a sequência de passos abaixo:

### Passo 1: Clonar o Projeto com o Usuário `deployer`
```bash
su - deployer
cd ~/www/projects
git clone https://github.com/seu-usuario/api-camem.git
cd api-camem
```

### Passo 2: Configurar o `docker-compose.yml` (Porta Exclusiva)
Cada aplicação deve possuir uma porta no host que **não esteja em uso por outro container**.
Se a `api-finances` usa a `5140:5140`, defina uma nova porta no host para o novo projeto (ex: `5143:8080` ou `5143:5143`):

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
      - "5143:5143"
    env_file:
      - .env
    networks:
      - camem_network

volumes:
  mongo_camem_data:

networks:
  camem_network:
    driver: bridge
```

### Passo 3: Criar o Arquivo `.env` de Produção
```bash
nano .env
```
Preencha as variáveis de ambiente necessárias e salve com `Ctrl + O`, `Enter` e `Ctrl + X`.

### Passo 4: Subir os Containers em Segundo Plano (`-d`)
> **Atenção:** Sempre use a flag `-d` para rodar em modo detached (segundo plano) e não prender o terminal nos logs.

```bash
docker compose up -d --build
```

Para verificar se os containers subiram:
```bash
docker compose ps
```

---

## 4. Configuração do Nginx e Certificado SSL HTTPS

### Passo 1: Criar o Arquivo de Configuração do Virtual Host
> **Atenção à sintaxe:** Abra o arquivo sempre utilizando um editor de texto (`sudo nano`). O `proxy_pass` deve conter obrigatoriamente `http://` completo sem espaços e terminar com `;`.

```bash
sudo nano /etc/nginx/sites-available/camem.conf
```

Cole o conteúdo ajustando o `server_name` e a porta configurada no docker-compose:
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

### Passo 2: Ativar o Site e Testar o Nginx
```bash
# 1. Criar o link simbólico em sites-enabled:
sudo ln -s /etc/nginx/sites-available/camem.conf /etc/nginx/sites-enabled/

# 2. Testar a sintaxe do Nginx (OBRIGATÓRIO antes de recarregar):
sudo nginx -t
```
> O retorno **deve** ser:
> `nginx: the configuration file /etc/nginx/nginx.conf syntax is ok`
> `nginx: configuration file /etc/nginx/nginx.conf test is successful`

```bash
# 3. Recarregar o Nginx:
sudo systemctl reload nginx
```

### Passo 3: Emitir o Certificado SSL com o Certbot
> **Crítico:** Se tentar acessar o domínio por HTTPS sem emitir o certificado para o novo subdomínio, ocorrerá o erro `net::ERR_CERT_COMMON_NAME_INVALID` no navegador.

Execute:
```bash
sudo certbot --nginx -d camem-caio-santos-ios.duckdns.org
```
- Se solicitar redirecionamento HTTP -> HTTPS, selecione a opção de redirecionar automaticamente.
- O Certbot atualizará o arquivo do Nginx com as chaves SSL.
- Recarregue o Nginx para aplicar:
```bash
sudo systemctl reload nginx
```

---

## 5. Configuração do CI/CD Contínuo (GitHub Actions)

### 5.1 Cadastrar Secrets no Repositório do GitHub
No repositório da API no GitHub (`Settings > Secrets and variables > Actions > New repository secret`):
- `VPS_HOST`: `191.252.191.227`
- `VPS_USER`: `deployer`
- `VPS_SSH_KEY`: Chave privada OpenSSH gerada no Passo 2.4

### 5.2 Arquivo `.github/workflows/deploy.yml`
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

## 6. Guia Prático de Troubleshooting (Erros e Soluções Reais)

| Erro Observado | Causa Raiz | Como Resolver na Prática |
| :--- | :--- | :--- |
| **`-bash: /etc/nginx/...: No such file or directory`** | O caminho do arquivo foi colado direto no terminal como se fosse um executável. | Use um editor de texto: `sudo nano /etc/nginx/sites-available/nome.conf`. |
| **`[sudo] password for deployer: Sorry, try again.`** | O usuário `deployer` não possui permissão sudo sem senha configurada. | Como `root`, execute: `echo "deployer ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/deployer` com permissão `chmod 440`. |
| **`invalid number of arguments in "proxy_pass"`** | Espaço em branco indevido na URL (ex: `http: 127.0.0.1`) ou falta de ponto e vírgula no fim. | Deixe exatamente: `proxy_pass http://127.0.0.1:PORTA;`. |
| **`invalid URL prefix in /etc/nginx/...`** | Faltaram as duas barras no protocolo (`http:/` ou `http:` em vez de `http://`). | Corrija para `http://127.0.0.1:PORTA;` e teste com `nginx -t`. |
| **`(failed) net::ERR_CERT_COMMON_NAME_INVALID`** | O subdomínio foi acessado via HTTPS mas o Certbot ainda não emitiu o certificado para ele. | Execute `sudo certbot --nginx -d seu-subdominio.duckdns.org` e depois `sudo systemctl reload nginx`. |
| **Terminal preso exibindo logs após `docker compose`** | O comando foi executado sem o argumento `-d`. | Pressione `Ctrl + C` e suba com `docker compose up -d`. |
| **`mv: cannot stat '/root/www/...': Permission denied`** | O usuário `deployer` tentou acessar arquivos dentro de `/root`. | Não clone em `/root`. Trabalhe sempre em `/home/deployer/www/projects/`. |
| **`bind: address already in use`** | Dois containers tentando usar a mesma porta externa no host. | No `docker-compose.yml`, altere a porta mapeada à esquerda (ex: `"5143:PORTA_INTERNA"`). |
| **`AuthenticationFailed (code 18)` no MongoDB** | Senha do `.env` alterada após o volume do MongoDB já ter sido inicializado. | Em ambiente de dev/staging, resete o volume com `docker compose down -v` e suba com `docker compose up -d`. |

---

## 7. Comandos Essenciais de Referência

```bash
# Alternar usuários
su - deployer     # Muda para deployer
exit              # Volta para root

# Gerenciamento de containers
docker compose ps             # Lista containers do projeto atual
docker compose logs -f api    # Acompanha logs da API em tempo real
docker compose up -d --build  # Recompila e sobe em segundo plano
docker compose down           # Para os containers do projeto

# Gerenciamento do Nginx
sudo nginx -t                 # Testa sintaxe (sempre execute antes de recarregar)
sudo systemctl reload nginx   # Aplica alterações sem derrubar conexões
sudo systemctl status nginx   # Exibe status e logs de erro do Nginx

# Recursos do servidor
docker stats                  # Exibe uso de CPU e memória em tempo real
df -h                         # Exibe espaço em disco disponível
```
