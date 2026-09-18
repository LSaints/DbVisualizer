import { describe, expect, it } from 'vitest';
import { formatarConsultaSqlParaExibicao } from '../src/utilitarios/formatarConsultaSql';

describe('formatarConsultaSqlParaExibicao', () => {
  it('quebra antes das cláusulas de uma consulta em linha única', () => {
    const consulta =
      'SELECT u.id, u.nome FROM usuarios u WHERE u.ativo = 1 AND u.nome IS NOT NULL ORDER BY u.nome LIMIT 10;';

    expect(formatarConsultaSqlParaExibicao(consulta)).toBe(
      'SELECT u.id, u.nome\nFROM usuarios u\nWHERE u.ativo = 1\nAND u.nome IS NOT NULL\nORDER BY u.nome\nLIMIT 10;'
    );
  });

  it('preserva quebras e indentação já existentes na consulta', () => {
    const consulta = 'SELECT a\nFROM tab1\n  INNER JOIN tab2\n    ON tab2.id = tab1.id';

    expect(formatarConsultaSqlParaExibicao(consulta)).toBe(consulta);
  });

  it('converte sequências de escape literais (\\n) em quebras reais', () => {
    const consulta =
      'SELECT a.id, b.nome\\nFROM a\\nINNER JOIN b ON b.id = a.id\\nWHERE a.ativo = 1';

    const formatada = formatarConsultaSqlParaExibicao(consulta);

    expect(formatada).toContain('\nFROM a');
    expect(formatada).not.toContain('\\n');
  });

  it('não quebra linha dentro de um literal de texto', () => {
    const consulta =
      "SELECT nome FROM clientes WHERE observacao = 'João AND Maria' AND ativo = 1";

    const formatada = formatarConsultaSqlParaExibicao(consulta);

    expect(formatada).toContain("observacao = 'João AND Maria'\nAND ativo = 1");
    expect(formatada).not.toContain("'João \nAND Maria");
  });
});