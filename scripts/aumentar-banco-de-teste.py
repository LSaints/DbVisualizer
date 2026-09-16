#!/usr/bin/env python3
"""Gera SQL para aumentar o banco de testes do Database Diagram.

Cria milhares de tabelas no database informado (ex.: ``erp``):
- ``nucleo_XXXX``: tabelas base com PK simples, índices, unique e uma FK em
  cadeia (aponta para ``nucleo_YYYY`` 10 posições antes) — exercita chaves
  estrangeiras em volume.
- ``detalhe_XXXX``: tabelas com PK composta e FK para uma ``nucleo`` —
  exercita chaves primárias compostas.

Volume padrão (~1300 tabelas, ~1290 FKs) ultrapassa o limite de introspecção
da API (500 tabelas), validando o aviso de truncamento e o comportamento do
diagrama com um banco grande.

Uso:
  python3 scripts/aumentar-banco-de-teste.py [--nucleo 1000] [--detalhe 300]

Pode ser combinado com a pipe para dentro de um MySQL:
  python3 scripts/aumentar-banco-de-teste.py | \
    docker exec -i <container> mysql -uroot -p<senha> erp
"""

import argparse


def gerar_sql(quantidade_nucleo: int, quantidade_detalhe: int) -> str:
    linhas: list[str] = []

    linhas.append("SET FOREIGN_KEY_CHECKS = 0;")

    # Remove apenas as tabelas geradas (idempotente), dependentes primeiro.
    if quantidade_detalhe:
        linhas.append(
            "DROP TABLE IF EXISTS "
            + ", ".join(f"detalhe_{i:04d}" for i in range(quantidade_detalhe))
            + ";"
        )
    if quantidade_nucleo:
        linhas.append(
            "DROP TABLE IF EXISTS "
            + ", ".join(f"nucleo_{i:04d}" for i in range(quantidade_nucleo))
            + ";"
        )

    for i in range(quantidade_nucleo):
        anterior = f"nucleo_{i - 10:04d}" if i >= 10 else None
        fk = (
            f" CONSTRAINT fk_nucleo_{i:04d}_anterior"
            f" FOREIGN KEY (nucleo_anterior_id) REFERENCES {anterior}(id)"
            f" ON DELETE SET NULL ON UPDATE CASCADE,"
            if anterior
            else ""
        )
        linhas.append(
            f"CREATE TABLE nucleo_{i:04d} ("
            f" id INT UNSIGNED NOT NULL AUTO_INCREMENT,"
            f" codigo VARCHAR(32) NOT NULL,"
            f" descricao VARCHAR(200) NULL,"
            f" quantidade INT NOT NULL DEFAULT 0,"
            f" valor DECIMAL(12,2) NULL,"
            f" ativo TINYINT(1) NOT NULL DEFAULT 1,"
            f" criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,"
            f" nucleo_anterior_id INT UNSIGNED NULL,"
            f" PRIMARY KEY (id),"
            f" UNIQUE KEY uk_codigo (codigo),"
            f" KEY idx_descricao (descricao),"
            f"{fk}"
            f" CONSTRAINT chk_valor_{i:04d} CHECK (valor IS NULL OR valor >= 0)"
            f") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci"
            f" COMMENT='Tabela base de teste';"
        )

    for i in range(quantidade_detalhe):
        nucleo = f"nucleo_{i:04d}"
        linhas.append(
            f"CREATE TABLE detalhe_{i:04d} ("
            f" nucleo_id INT UNSIGNED NOT NULL,"
            f" sequencial INT UNSIGNED NOT NULL,"
            f" descricao VARCHAR(200) NULL,"
            f" PRIMARY KEY (nucleo_id, sequencial),"
            f" KEY idx_nucleo (nucleo_id),"
            f" CONSTRAINT fk_detalhe_{i:04d}_nucleo"
            f" FOREIGN KEY (nucleo_id) REFERENCES {nucleo}(id)"
            f" ON DELETE CASCADE ON UPDATE CASCADE"
            f") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci"
            f" COMMENT='Detalhe de teste com PK composta';"
        )

    linhas.append("SET FOREIGN_KEY_CHECKS = 1;")
    return "\n".join(linhas) + "\n"


def principal() -> None:
    analisador = argparse.ArgumentParser(
        description="Gera SQL para aumentar o banco de testes."
    )
    analisador.add_argument(
        "--nucleo", type=int, default=1000, help="Quantidade de tabelas nucleo (padrão: 1000)"
    )
    analisador.add_argument(
        "--detalhe", type=int, default=300, help="Quantidade de tabelas detalhe (padrão: 300)"
    )
    argumentos = analisador.parse_args()

    if argumentos.nucleo < 11:
        raise SystemExit("--nucleo deve ser >= 11 para gerar a cadeia de FKs.")
    if argumentos.nucleo < argumentos.detalhe:
        raise SystemExit("--nucleo deve ser >= --detalhe (detalhe referencia nucleo).")

    print(gerar_sql(argumentos.nucleo, argumentos.detalhe))


if __name__ == "__main__":
    principal()