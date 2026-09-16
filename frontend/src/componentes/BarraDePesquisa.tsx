interface Propriedades {
  valor: string;
  aoAlterar: (valor: string) => void;
}

/** Busca por nome de tabela (substring, sem diferenciação de maiúsculas). */
export function BarraDePesquisa({ valor, aoAlterar }: Propriedades) {
  return (
    <div className="barra-de-pesquisa">
      <input
        type="search"
        value={valor}
        placeholder="Pesquisar tabela..."
        aria-label="Pesquisar tabela"
        onChange={(evento) => aoAlterar(evento.target.value)}
      />
    </div>
  );
}