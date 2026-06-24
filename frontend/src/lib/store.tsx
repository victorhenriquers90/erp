import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type { User } from "@supabase/supabase-js";
import { supabase } from "./supabase";
import type {
  AppState,
  Aluno,
  Avaliacao,
  Frequencia,
  Nota,
  Tutoria,
  Turma,
} from "./types";

const emptyState: AppState = {
  turmas: [],
  alunos: [],
  avaliacoes: [],
  notas: [],
  frequencias: [],
  tutorias: [],
};

// ─── DB row → app type ────────────────────────────────────────────────────────
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const mapTurma = (r: any): Turma => ({
  id: r.id, nome: r.nome, disciplina: r.disciplina ?? "", ano: r.ano ?? "",
  createdAt: new Date(r.created_at).getTime(),
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const mapAluno = (r: any): Aluno => ({
  id: r.id, turmaId: r.turma_id, nome: r.nome, matricula: r.matricula ?? "",
  createdAt: new Date(r.created_at).getTime(),
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const mapAvaliacao = (r: any): Avaliacao => ({
  id: r.id, turmaId: r.turma_id, titulo: r.titulo, peso: Number(r.peso), data: r.data,
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const mapNota = (r: any): Nota => ({
  id: r.id, avaliacaoId: r.avaliacao_id, alunoId: r.aluno_id, valor: Number(r.valor),
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const mapFrequencia = (r: any): Frequencia => ({
  id: r.id, turmaId: r.turma_id, alunoId: r.aluno_id, data: r.data, presente: r.presente,
});
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const mapTutoria = (r: any): Tutoria => ({
  id: r.id, alunoId: r.aluno_id, data: r.data,
  titulo: r.titulo ?? "", notas: r.notas ?? "", acao: r.acao ?? "",
  status: r.status as Tutoria["status"],
});

async function fetchAll(userId: string): Promise<AppState> {
  const [t, a, av, n, f, tu] = await Promise.all([
    supabase.from("turmas").select("*").eq("user_id", userId).order("created_at"),
    supabase.from("alunos").select("*").eq("user_id", userId).order("created_at"),
    supabase.from("avaliacoes").select("*").eq("user_id", userId),
    supabase.from("notas").select("*").eq("user_id", userId),
    supabase.from("frequencias").select("*").eq("user_id", userId),
    supabase.from("tutorias").select("*").eq("user_id", userId),
  ]);
  return {
    turmas: (t.data ?? []).map(mapTurma),
    alunos: (a.data ?? []).map(mapAluno),
    avaliacoes: (av.data ?? []).map(mapAvaliacao),
    notas: (n.data ?? []).map(mapNota),
    frequencias: (f.data ?? []).map(mapFrequencia),
    tutorias: (tu.data ?? []).map(mapTutoria),
  };
}

// ─── Context type ─────────────────────────────────────────────────────────────
type Ctx = {
  user: User | null;
  loading: boolean;
  state: AppState;
  setState: (updater: (s: AppState) => AppState) => void;
  addTurma: (data: Omit<Turma, "id" | "createdAt">) => Turma;
  removeTurma: (id: string) => void;
  addAluno: (data: Omit<Aluno, "id" | "createdAt">) => Aluno;
  removeAluno: (id: string) => void;
  addAvaliacao: (data: Omit<Avaliacao, "id">) => Avaliacao;
  removeAvaliacao: (id: string) => void;
  setNota: (avaliacaoId: string, alunoId: string, valor: number) => void;
  setFrequencia: (turmaId: string, alunoId: string, data: string, presente: boolean) => void;
  addTutoria: (data: Omit<Tutoria, "id">) => Tutoria;
  updateTutoria: (id: string, patch: Partial<Tutoria>) => void;
  removeTutoria: (id: string) => void;
  clear: () => void;
  signOut: () => Promise<void>;
};

const StoreContext = createContext<Ctx | null>(null);

export function StoreProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [state, setStateRaw] = useState<AppState>(emptyState);

  useEffect(() => {
    supabase.auth.getSession().then(({ data: { session } }) => {
      const u = session?.user ?? null;
      setUser(u);
      if (u) {
        fetchAll(u.id).then((s) => { setStateRaw(s); setLoading(false); });
      } else {
        setLoading(false);
      }
    });

    const { data: { subscription } } = supabase.auth.onAuthStateChange(
      async (_event, session) => {
        const u = session?.user ?? null;
        setUser(u);
        if (u) {
          setLoading(true);
          const s = await fetchAll(u.id);
          setStateRaw(s);
          setLoading(false);
        } else {
          setStateRaw(emptyState);
        }
      },
    );
    return () => subscription.unsubscribe();
  }, []);

  const setState = useCallback((updater: (s: AppState) => AppState) => {
    setStateRaw((prev) => updater(prev));
  }, []);

  const api = useMemo<Ctx>(() => {
    const uid = () => crypto.randomUUID();
    const userId = user?.id ?? "";

    function sb(promise: Promise<{ error: unknown }>, label: string) {
      promise.then(({ error }) => { if (error) console.error(`[supabase] ${label}:`, error); });
    }

    return {
      user,
      loading,
      state,
      setState,

      addTurma: (data) => {
        const t: Turma = { ...data, id: uid(), createdAt: Date.now() };
        setState((s) => ({ ...s, turmas: [...s.turmas, t] }));
        sb(supabase.from("turmas").insert({
          id: t.id, user_id: userId, nome: t.nome, disciplina: t.disciplina, ano: t.ano,
        }), "addTurma");
        return t;
      },

      removeTurma: (id) => {
        setState((s) => {
          const alunoIds = s.alunos.filter((a) => a.turmaId === id).map((a) => a.id);
          const avIds = s.avaliacoes.filter((a) => a.turmaId === id).map((a) => a.id);
          return {
            ...s,
            turmas: s.turmas.filter((t) => t.id !== id),
            alunos: s.alunos.filter((a) => a.turmaId !== id),
            avaliacoes: s.avaliacoes.filter((a) => a.turmaId !== id),
            notas: s.notas.filter((n) => !avIds.includes(n.avaliacaoId)),
            frequencias: s.frequencias.filter((f) => f.turmaId !== id),
            tutorias: s.tutorias.filter((t) => !alunoIds.includes(t.alunoId)),
          };
        });
        sb(supabase.from("turmas").delete().eq("id", id).eq("user_id", userId), "removeTurma");
      },

      addAluno: (data) => {
        const a: Aluno = { ...data, id: uid(), createdAt: Date.now() };
        setState((s) => ({ ...s, alunos: [...s.alunos, a] }));
        sb(supabase.from("alunos").insert({
          id: a.id, user_id: userId, turma_id: a.turmaId, nome: a.nome, matricula: a.matricula,
        }), "addAluno");
        return a;
      },

      removeAluno: (id) => {
        setState((s) => ({
          ...s,
          alunos: s.alunos.filter((a) => a.id !== id),
          notas: s.notas.filter((n) => n.alunoId !== id),
          frequencias: s.frequencias.filter((f) => f.alunoId !== id),
          tutorias: s.tutorias.filter((t) => t.alunoId !== id),
        }));
        sb(supabase.from("alunos").delete().eq("id", id).eq("user_id", userId), "removeAluno");
      },

      addAvaliacao: (data) => {
        const av: Avaliacao = { ...data, id: uid() };
        setState((s) => ({ ...s, avaliacoes: [...s.avaliacoes, av] }));
        sb(supabase.from("avaliacoes").insert({
          id: av.id, user_id: userId, turma_id: av.turmaId,
          titulo: av.titulo, peso: av.peso, data: av.data,
        }), "addAvaliacao");
        return av;
      },

      removeAvaliacao: (id) => {
        setState((s) => ({
          ...s,
          avaliacoes: s.avaliacoes.filter((a) => a.id !== id),
          notas: s.notas.filter((n) => n.avaliacaoId !== id),
        }));
        sb(supabase.from("avaliacoes").delete().eq("id", id).eq("user_id", userId), "removeAvaliacao");
      },

      setNota: (avaliacaoId, alunoId, valor) => {
        setState((s) => {
          const existing = s.notas.find(
            (n) => n.avaliacaoId === avaliacaoId && n.alunoId === alunoId,
          );
          if (existing) {
            return { ...s, notas: s.notas.map((n) => n.id === existing.id ? { ...n, valor } : n) };
          }
          return { ...s, notas: [...s.notas, { id: uid(), avaliacaoId, alunoId, valor }] };
        });
        sb(supabase.from("notas").upsert(
          { user_id: userId, avaliacao_id: avaliacaoId, aluno_id: alunoId, valor },
          { onConflict: "avaliacao_id,aluno_id" },
        ), "setNota");
      },

      setFrequencia: (turmaId, alunoId, data, presente) => {
        setState((s) => {
          const existing = s.frequencias.find(
            (f) => f.alunoId === alunoId && f.data === data,
          );
          if (existing) {
            return {
              ...s,
              frequencias: s.frequencias.map((f) =>
                f.id === existing.id ? { ...f, presente } : f,
              ),
            };
          }
          return {
            ...s,
            frequencias: [...s.frequencias, { id: uid(), turmaId, alunoId, data, presente }],
          };
        });
        sb(supabase.from("frequencias").upsert(
          { user_id: userId, turma_id: turmaId, aluno_id: alunoId, data, presente },
          { onConflict: "aluno_id,data" },
        ), "setFrequencia");
      },

      addTutoria: (data) => {
        const t: Tutoria = { ...data, id: uid() };
        setState((s) => ({ ...s, tutorias: [...s.tutorias, t] }));
        sb(supabase.from("tutorias").insert({
          id: t.id, user_id: userId, aluno_id: t.alunoId,
          data: t.data, titulo: t.titulo, notas: t.notas, acao: t.acao, status: t.status,
        }), "addTutoria");
        return t;
      },

      updateTutoria: (id, patch) => {
        setState((s) => ({
          ...s,
          tutorias: s.tutorias.map((t) => t.id === id ? { ...t, ...patch } : t),
        }));
        const dbPatch: Record<string, unknown> = {};
        if (patch.alunoId !== undefined) dbPatch.aluno_id = patch.alunoId;
        if (patch.data !== undefined) dbPatch.data = patch.data;
        if (patch.titulo !== undefined) dbPatch.titulo = patch.titulo;
        if (patch.notas !== undefined) dbPatch.notas = patch.notas;
        if (patch.acao !== undefined) dbPatch.acao = patch.acao;
        if (patch.status !== undefined) dbPatch.status = patch.status;
        sb(supabase.from("tutorias").update(dbPatch).eq("id", id).eq("user_id", userId), "updateTutoria");
      },

      removeTutoria: (id) => {
        setState((s) => ({
          ...s,
          tutorias: s.tutorias.filter((t) => t.id !== id),
        }));
        sb(supabase.from("tutorias").delete().eq("id", id).eq("user_id", userId), "removeTutoria");
      },

      clear: () => {
        setState(() => emptyState);
        sb(supabase.from("turmas").delete().eq("user_id", userId), "clear");
      },

      signOut: async () => {
        await supabase.auth.signOut();
        setState(() => emptyState);
      },
    };
  }, [user, loading, state, setState]);

  return <StoreContext.Provider value={api}>{children}</StoreContext.Provider>;
}

export function useStore() {
  const ctx = useContext(StoreContext);
  if (!ctx) throw new Error("useStore must be used inside StoreProvider");
  return ctx;
}

// ─── Selectors ────────────────────────────────────────────────────────────────
export function useTurma(id: string) {
  const { state } = useStore();
  return state.turmas.find((t) => t.id === id);
}

export function useAlunosByTurma(turmaId: string) {
  const { state } = useStore();
  return state.alunos.filter((a) => a.turmaId === turmaId);
}

export function useAvaliacoesByTurma(turmaId: string) {
  const { state } = useStore();
  return state.avaliacoes.filter((a) => a.turmaId === turmaId);
}

export function getNota(state: AppState, avaliacaoId: string, alunoId: string) {
  return state.notas.find(
    (n) => n.avaliacaoId === avaliacaoId && n.alunoId === alunoId,
  );
}

export function mediaAluno(state: AppState, alunoId: string, turmaId: string) {
  const avs = state.avaliacoes.filter((a) => a.turmaId === turmaId);
  if (avs.length === 0) return null;
  let somaPesos = 0;
  let soma = 0;
  for (const av of avs) {
    const n = getNota(state, av.id, alunoId);
    if (n) {
      soma += n.valor * av.peso;
      somaPesos += av.peso;
    }
  }
  if (somaPesos === 0) return null;
  return soma / somaPesos;
}

export function mediaTurma(state: AppState, turmaId: string) {
  const alunos = state.alunos.filter((a) => a.turmaId === turmaId);
  const medias = alunos
    .map((a) => mediaAluno(state, a.id, turmaId))
    .filter((m): m is number => m !== null);
  if (medias.length === 0) return null;
  return medias.reduce((a, b) => a + b, 0) / medias.length;
}

export function frequenciaAluno(state: AppState, alunoId: string) {
  const fs = state.frequencias.filter((f) => f.alunoId === alunoId);
  if (fs.length === 0) return null;
  return fs.filter((f) => f.presente).length / fs.length;
}
