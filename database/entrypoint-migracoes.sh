#!/usr/bin/env bash
set -euo pipefail

# Entrypoint do banco MySQL do Database Diagram (docker compose).
#
# Substitui o entrypoint padrão da imagem mysql para garantir que, a cada
# subida do servidor:
#   1. O data dir é inicializado (primeira execução), como na imagem oficial.
#   2. O database e o usuário de somente leitura existem (idempotente).
#   3. As migrations pendentes de database/migrations/*.sql são aplicadas
#      automaticamente (rastreadas em `_schema_migrations`).
#   4. O mysqld definitivo sobe em foreground.

MYSQL_DATABASE="${MYSQL_DATABASE:-erp}"
MYSQL_USER="${MYSQL_USER:-readonly}"
MYSQL_PASSWORD="${MYSQL_PASSWORD:-segredo}"
MYSQL_ROOT_PASSWORD="${MYSQL_ROOT_PASSWORD:-rootsegredo}"

DATADIR="/var/lib/mysql"
# Socket próprio do servidor temporário (isola daquele usado pelo mysqld
# definitivo, evitando colisão a cada subida do container).
SOQUETE="/run/mysqld/migracao.sock"
MIGRACOES="/migrations"

log() { echo ">> $*"; }

limpar() {
  [ -n "${PID_TEMP:-}" ] && kill "$PID_TEMP" 2>/dev/null || true
}
trap limpar EXIT

# 1) Inicialização do data dir (somente primeira execução). Root inicia sem
# senha; MYSQL_ROOT_PASSWORD é aplicado ao final pelos mesmos passos da imagem.
FRESCO=0
if [ ! -d "$DATADIR/mysql" ]; then
  log "Inicializando data dir (primeira execução)..."
  mysqld --user=mysql --datadir="$DATADIR" --initialize-insecure
  FRESCO=1
fi

mkdir -p "$(dirname "$SOQUETE")"
chown mysql:mysql "$(dirname "$SOQUETE")" 2>/dev/null || true

# Remove eventuais arquivos residuais do servidor temporário (restart/crash).
rm -f "$SOQUETE" "$SOQUETE.lock" /run/mysqld/servidor-temporario.pid

# 2) Servidor temporário local (sem rede) para bootstrap e migrations.
log "Iniciando servidor temporário..."
mysqld \
  --user=mysql \
  --datadir="$DATADIR" \
  --socket="$SOQUETE" \
  --skip-networking \
  --pid-file=/run/mysqld/servidor-temporario.pid &
PID_TEMP=$!

# Aguarda ficar pronto detectando a autenticação root vigente: data dir novo
# (root sem senha) ou já inicializado (senha de MYSQL_ROOT_PASSWORD).
ROOT_AUTH=()

aguardar_pronto() {
  for _ in $(seq 1 60); do
    if mysql --socket="$SOQUETE" -uroot -e "SELECT 1" >/dev/null 2>&1; then
      ROOT_AUTH=()
      return 0
    fi
    if [ -n "$MYSQL_ROOT_PASSWORD" ] && \
       mysql --socket="$SOQUETE" -uroot -p"$MYSQL_ROOT_PASSWORD" -e "SELECT 1" >/dev/null 2>&1; then
      ROOT_AUTH=(-p"$MYSQL_ROOT_PASSWORD")
      return 0
    fi
    sleep 1
  done
  return 1
}

if ! aguardar_pronto; then
  echo "Não foi possível iniciar o servidor temporário." >&2
  exit 1
fi

mysql_raiz() { mysql --socket="$SOQUETE" -uroot "${ROOT_AUTH[@]}" "$@"; }

# 3) Database + usuário de somente leitura (constituição III).
log "Configurando database '$MYSQL_DATABASE' e usuário '$MYSQL_USER'..."
mysql_raiz <<SQL
CREATE DATABASE IF NOT EXISTS \`$MYSQL_DATABASE\`
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '$MYSQL_USER'@'%' IDENTIFIED BY '$MYSQL_PASSWORD';
ALTER USER '$MYSQL_USER'@'%' IDENTIFIED BY '$MYSQL_PASSWORD';
GRANT SELECT, SHOW VIEW ON \`$MYSQL_DATABASE\`.* TO '$MYSQL_USER'@'%';
FLUSH PRIVILEGES;
SQL

# 4) Migrations pendentes, em ordem alfabética, registradas em
#    `_schema_migrations` (idempotente a cada subida).
mysql_raiz <<SQL
CREATE TABLE IF NOT EXISTS \`$MYSQL_DATABASE\`.\`_schema_migrations\` (
  nome VARCHAR(150) NOT NULL PRIMARY KEY,
  aplicado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
SQL

for migracao in "$MIGRACOES"/*.sql; do
  [ -e "$migracao" ] || continue
  nome="$(basename "$migracao")"
  aplicada="$(mysql_raiz -N -e \
    "SELECT 1 FROM \`$MYSQL_DATABASE\`.\`_schema_migrations\` WHERE nome = '$nome'")"

  if [ "$aplicada" = "1" ]; then
    log "Migração $nome já aplicada; pulando."
    continue
  fi

  log "Aplicando migração $nome..."
  if ! mysql_raiz "$MYSQL_DATABASE" < "$migracao"; then
    echo "Falha ao aplicar a migração $nome." >&2
    kill -TERM "$PID_TEMP" 2>/dev/null || true
    exit 1
  fi
  mysql_raiz -e \
    "INSERT INTO \`$MYSQL_DATABASE\`.\`_schema_migrations\` (nome) VALUES ('$nome')"
done

# 5) Senha do root na primeira execução (mesmo comportamento da imagem).
if [ "$FRESCO" = "1" ] && [ -n "$MYSQL_ROOT_PASSWORD" ]; then
  log "Definindo senha do usuário root..."
  mysql_raiz -e \
    "ALTER USER 'root'@'localhost' IDENTIFIED BY '$MYSQL_ROOT_PASSWORD'; FLUSH PRIVILEGES;"
  ROOT_AUTH=(-p"$MYSQL_ROOT_PASSWORD")
fi

# 6) Encerra o servidor temporário (SIGTERM, sem depender de auth) e sobe o
#    definitivo em foreground.
log "Desligando servidor temporário..."
kill -TERM "$PID_TEMP" 2>/dev/null || true
wait "$PID_TEMP" 2>/dev/null || true
PID_TEMP=""
rm -f "$SOQUETE"

exec mysqld --user=mysql --datadir="$DATADIR" "$@"