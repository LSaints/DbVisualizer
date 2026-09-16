import { useState, type FormEvent } from 'react';
import type { ConexaoDeBanco } from '../modelos/tipos';

interface Propriedades {
  aoSubmeter: (conexao: ConexaoDeBanco) => void;
  carregando?: boolean;
}

/**
 * Tela de conexão: provedor, host, porta, banco de dados, usuário e senha.
 * Mensagens e rótulos em pt-BR (constituição II).
 */
export function FormularioDeConexao({
  aoSubmeter,
  carregando = false
}: Propriedades) {
  const [provedor, setProvedor] = useState('mysql');
  const [host, setHost] = useState('localhost');
  const [porta, setPorta] = useState('3306');
  const [bancoDeDados, setBancoDeDados] = useState('');
  const [usuario, setUsuario] = useState('readonly');
  const [senha, setSenha] = useState('');

  function aoEnviar(evento: FormEvent): void {
    evento.preventDefault();
    aoSubmeter({
      provedor,
      host,
      porta: porta === '' ? undefined : Number(porta),
      bancoDeDados,
      usuario,
      senha
    });
  }

  return (
    <form className="formulario-de-conexao" onSubmit={aoEnviar}>
      <h2 className="formulario-titulo">Conectar ao banco de dados</h2>

      <label className="campo">
        <span className="campo-rotulo">Provedor</span>
        <select value={provedor} onChange={(evento) => setProvedor(evento.target.value)}>
          <option value="mysql">MySQL</option>
        </select>
      </label>

      <label className="campo">
        <span className="campo-rotulo">Host</span>
        <input
          type="text"
          value={host}
          onChange={(evento) => setHost(evento.target.value)}
          placeholder="localhost"
          required
        />
      </label>

      <label className="campo">
        <span className="campo-rotulo">Porta</span>
        <input
          type="number"
          min={1}
          max={65535}
          value={porta}
          onChange={(evento) => setPorta(evento.target.value)}
          placeholder="3306"
        />
      </label>

      <label className="campo">
        <span className="campo-rotulo">Banco de dados</span>
        <input
          type="text"
          value={bancoDeDados}
          onChange={(evento) => setBancoDeDados(evento.target.value)}
          placeholder="erp"
          required
        />
      </label>

      <label className="campo">
        <span className="campo-rotulo">Usuário</span>
        <input
          type="text"
          value={usuario}
          onChange={(evento) => setUsuario(evento.target.value)}
          placeholder="readonly"
          required
          autoComplete="username"
        />
      </label>

      <label className="campo">
        <span className="campo-rotulo">Senha</span>
        <input
          type="password"
          value={senha}
          onChange={(evento) => setSenha(evento.target.value)}
          placeholder="••••••••"
          required
          autoComplete="current-password"
        />
      </label>

      <button type="submit" className="botao-primario" disabled={carregando}>
        {carregando ? 'Testando...' : 'Testar conexão'}
      </button>
    </form>
  );
}