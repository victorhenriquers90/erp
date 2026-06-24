export type Turma = {
  id: string;
  nome: string;
  disciplina: string;
  ano: string;
  createdAt: number;
};

export type Aluno = {
  id: string;
  turmaId: string;
  nome: string;
  matricula: string;
  createdAt: number;
};

export type Avaliacao = {
  id: string;
  turmaId: string;
  titulo: string;
  peso: number;
  data: string; // ISO date
};

export type Nota = {
  id: string;
  avaliacaoId: string;
  alunoId: string;
  valor: number; // 0..10
};

export type Frequencia = {
  id: string;
  turmaId: string;
  alunoId: string;
  data: string; // ISO date (aula)
  presente: boolean;
};

export type Tutoria = {
  id: string;
  alunoId: string;
  data: string;
  titulo: string;
  notas: string;
  acao: string;
  status: "aberto" | "em_andamento" | "concluido";
};

export type AppState = {
  turmas: Turma[];
  alunos: Aluno[];
  avaliacoes: Avaliacao[];
  notas: Nota[];
  frequencias: Frequencia[];
  tutorias: Tutoria[];
};
