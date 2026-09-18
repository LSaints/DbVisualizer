const PALAVRAS_CHAVE_DE_QUEBRA = [
  'SELECT',
  'FROM',
  'WHERE',
  'GROUP BY',
  'ORDER BY',
  'HAVING',
  'LIMIT',
  'OFFSET',
  'UNION ALL',
  'UNION',
  'INSERT INTO',
  'VALUES',
  'UPDATE',
  'SET',
  'DELETE FROM',
  'LEFT JOIN',
  'RIGHT JOIN',
  'INNER JOIN',
  'FULL JOIN',
  'JOIN',
  'ON',
  'AND',
  'OR'
];

const PADRAO_DE_PALAVRAS_CHAVE = new RegExp(
  `\\s+(${PALAVRAS_CHAVE_DE_QUEBRA.join('|')})\\b`,
  'gi'
);

/**
 * Converte sequências de escape literais ("\n", "\t", "\r\n") que alguns
 * provedores emitem como texto dentro do JSON (em vez de quebras reais) para
 * caracteres de controle de verdade, antes de aplicar as quebras de cláusula.
 */
function normalizarEscapesLiterais(consulta: string): string {
  return consulta
    .replace(/\\r\\n|\\n|\\r/g, '\n')
    .replace(/\\t/g, '\t');
}

/**
 * Mapa posicional (true = dentro) das posições que estão dentro de literais
 * de texto ('...'), usado para não quebrar linhas dentro de um string literal.
 * Apostrofes duplicados ('' escapam a aspa no SQL) não alternam o estado.
 */
function posicoesDentroDeLiterais(consulta: string): boolean[] {
  const dentro = new Array<boolean>(consulta.length).fill(false);
  let emLiteral = false;

  for (let indice = 0; indice < consulta.length; indice += 1) {
    if (consulta[indice] !== "'") {
      if (emLiteral) {
        dentro[indice] = true;
      }
      continue;
    }

    dentro[indice] = true;
    if (emLiteral && consulta[indice + 1] === "'") {
      dentro[indice + 1] = true;
      indice += 1;
      continue;
    }
    emLiteral = !emLiteral;
  }

  return dentro;
}

/**
 * Insere quebras de linha antes das principais cláusulas SQL para exibição
 * (a consulta original, sem as quebras, é preservada para "Copiar"). Não é um
 * formatador completo — apenas separa cláusulas para leitura mais elegante do
 * que uma única linha corrida, sem quebrar dentro de literais de texto.
 */
export function formatarConsultaSqlParaExibicao(consulta: string): string {
  const normalizada = normalizarEscapesLiterais(consulta.trim());
  const dentroDeLiteral = posicoesDentroDeLiterais(normalizada);

  return normalizada.replace(
    PADRAO_DE_PALAVRAS_CHAVE,
    (coincidencia, palavra: string, deslocamento: number): string => {
      const brancos = coincidencia.slice(0, coincidencia.length - palavra.length);
      const inicioDaPalavra = deslocamento + brancos.length;

      // Se a palavra estiver dentro de um literal de texto, ou o branco já
      // contiver uma quebra (formatação prévia preservada), mantém o trecho.
      if (dentroDeLiteral[inicioDaPalavra] || /\r|\n/.test(brancos)) {
        return coincidencia;
      }
      return `\n${palavra}`;
    }
  );
}