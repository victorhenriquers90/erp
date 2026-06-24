import { createFileRoute, Link } from "@tanstack/react-router";
import { useState, useMemo } from "react";
import {
  useStore,
  mediaAluno,
  frequenciaAluno,
} from "@/lib/store";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Plus,
  Search,
  Trash2,
  UserRound,
  GraduationCap,
  TrendingUp,
} from "lucide-react";

export const Route = createFileRoute("/alunos")({
  head: () => ({
    meta: [
      { title: "Alunos — Regente" },
      { name: "description", content: "Gerencie todos os alunos por turma." },
    ],
  }),
  component: AlunosPage,
});

function fmtMedia(m: number | null) {
  if (m === null) return "—";
  return m.toFixed(1).replace(".", ",");
}

function fmtFreq(f: number | null) {
  if (f === null) return "—";
  return `${Math.round(f * 100)}%`;
}

function StatusBadge({ media, freq }: { media: number | null; freq: number | null }) {
  if (media === null && freq === null)
    return <span className="text-xs text-muted-foreground/50">sem dados</span>;
  const ok = (media ?? 0) >= 5 && (freq ?? 1) >= 0.75;
  return (
    <Badge
      variant="outline"
      className={
        ok
          ? "border-success/40 bg-success/10 text-success"
          : "border-destructive/40 bg-destructive/10 text-destructive"
      }
    >
      {ok ? "Aprovado" : "Atenção"}
    </Badge>
  );
}

function AlunosPage() {
  const { state, addAluno, removeAluno } = useStore();
  const [turmaFiltro, setTurmaFiltro] = useState("todas");
  const [busca, setBusca] = useState("");
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ turmaId: "", nome: "", matricula: "" });

  const alunosFiltrados = useMemo(() => {
    return state.alunos.filter((a) => {
      const matchTurma = turmaFiltro === "todas" || a.turmaId === turmaFiltro;
      const matchBusca =
        busca === "" ||
        a.nome.toLowerCase().includes(busca.toLowerCase()) ||
        a.matricula.toLowerCase().includes(busca.toLowerCase());
      return matchTurma && matchBusca;
    });
  }, [state.alunos, turmaFiltro, busca]);

  const totalPorTurma = useMemo(
    () =>
      state.turmas.map((t) => ({
        ...t,
        count: state.alunos.filter((a) => a.turmaId === t.id).length,
      })),
    [state.turmas, state.alunos],
  );

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.turmaId || !form.nome.trim()) return;
    addAluno({ turmaId: form.turmaId, nome: form.nome.trim(), matricula: form.matricula.trim() });
    setForm({ turmaId: form.turmaId, nome: "", matricula: "" });
    setOpen(false);
  }

  function nomeTurma(turmaId: string) {
    return state.turmas.find((t) => t.id === turmaId)?.nome ?? "—";
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
      <PageHeader
        eyebrow="Alunos"
        title="Gestão de alunos"
        description="Visualize e gerencie todos os alunos cadastrados."
        actions={
          <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
              <Button disabled={state.turmas.length === 0}>
                <Plus className="mr-1.5 h-4 w-4" /> Novo aluno
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle className="font-display">Novo aluno</DialogTitle>
              </DialogHeader>
              <form onSubmit={submit} className="space-y-4">
                <div className="space-y-1.5">
                  <Label>Turma</Label>
                  <Select
                    value={form.turmaId}
                    onValueChange={(v) => setForm((f) => ({ ...f, turmaId: v }))}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione a turma" />
                    </SelectTrigger>
                    <SelectContent>
                      {state.turmas.map((t) => (
                        <SelectItem key={t.id} value={t.id}>
                          {t.nome}{t.disciplina ? ` — ${t.disciplina}` : ""}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="a-nome">Nome</Label>
                  <Input
                    id="a-nome"
                    placeholder="Nome completo"
                    value={form.nome}
                    onChange={(e) => setForm((f) => ({ ...f, nome: e.target.value }))}
                    required
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="a-mat">Matrícula <span className="text-muted-foreground">(opcional)</span></Label>
                  <Input
                    id="a-mat"
                    placeholder="Ex: 20260001"
                    value={form.matricula}
                    onChange={(e) => setForm((f) => ({ ...f, matricula: e.target.value }))}
                  />
                </div>
                <DialogFooter>
                  <Button type="submit" disabled={!form.turmaId || !form.nome.trim()}>
                    Adicionar
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        }
      />

      {/* Stat cards */}
      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardContent className="flex items-center gap-4 pt-6">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
              <UserRound className="h-5 w-5 text-primary" />
            </div>
            <div>
              <p className="text-2xl font-display font-semibold">{state.alunos.length}</p>
              <p className="text-xs text-muted-foreground">alunos cadastrados</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 pt-6">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-gold/15">
              <GraduationCap className="h-5 w-5 text-gold-foreground" />
            </div>
            <div>
              <p className="text-2xl font-display font-semibold">{state.turmas.length}</p>
              <p className="text-xs text-muted-foreground">turmas ativas</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 pt-6">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-success/10">
              <TrendingUp className="h-5 w-5 text-success" />
            </div>
            <div>
              <p className="text-2xl font-display font-semibold">
                {state.alunos.length > 0
                  ? state.alunos.filter((a) => {
                      const m = mediaAluno(state, a.id, a.turmaId);
                      const f = frequenciaAluno(state, a.id);
                      return m !== null && m >= 5 && (f === null || f >= 0.75);
                    }).length
                  : "—"}
              </p>
              <p className="text-xs text-muted-foreground">aprovados (média ≥ 5)</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {state.turmas.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
            <UserRound className="h-10 w-10 text-muted-foreground/25" />
            <div className="space-y-1">
              <p className="text-sm font-medium">Nenhuma turma cadastrada</p>
              <p className="text-xs text-muted-foreground">
                Crie uma turma antes de adicionar alunos.
              </p>
            </div>
            <Button size="sm" asChild>
              <Link to="/turmas">Criar turma</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {/* Filters */}
          <div className="flex flex-col gap-3 sm:flex-row">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                className="pl-9"
                placeholder="Buscar por nome ou matrícula…"
                value={busca}
                onChange={(e) => setBusca(e.target.value)}
              />
            </div>
            <Select value={turmaFiltro} onValueChange={setTurmaFiltro}>
              <SelectTrigger className="w-full sm:w-56">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="todas">Todas as turmas</SelectItem>
                {totalPorTurma.map((t) => (
                  <SelectItem key={t.id} value={t.id}>
                    {t.nome} ({t.count})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Table */}
          {alunosFiltrados.length === 0 ? (
            <Card>
              <CardContent className="flex flex-col items-center gap-3 py-12 text-center">
                <Search className="h-8 w-8 text-muted-foreground/25" />
                <p className="text-sm text-muted-foreground">
                  {state.alunos.length === 0
                    ? "Nenhum aluno cadastrado ainda."
                    : "Nenhum aluno encontrado com esses filtros."}
                </p>
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardContent className="p-0">
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/30 text-xs uppercase tracking-wider text-muted-foreground">
                        <th className="px-5 py-3 text-left">Aluno</th>
                        <th className="px-5 py-3 text-left">Turma</th>
                        <th className="px-5 py-3 text-left">Matrícula</th>
                        <th className="px-5 py-3 text-left">Média</th>
                        <th className="px-5 py-3 text-left">Freq.</th>
                        <th className="px-5 py-3 text-left">Status</th>
                        <th className="px-5 py-3"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {alunosFiltrados.map((aluno) => {
                        const media = mediaAluno(state, aluno.id, aluno.turmaId);
                        const freq = frequenciaAluno(state, aluno.id);
                        return (
                          <tr
                            key={aluno.id}
                            className="group transition-colors hover:bg-muted/20"
                          >
                            <td className="px-5 py-3">
                              <div className="flex items-center gap-2.5">
                                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                                  {aluno.nome
                                    .split(" ")
                                    .slice(0, 2)
                                    .map((n) => n[0])
                                    .join("")
                                    .toUpperCase()}
                                </div>
                                <span className="font-medium">{aluno.nome}</span>
                              </div>
                            </td>
                            <td className="px-5 py-3 text-muted-foreground">
                              <Link
                                to={`/turmas/${aluno.turmaId}`}
                                className="hover:text-foreground hover:underline"
                              >
                                {nomeTurma(aluno.turmaId)}
                              </Link>
                            </td>
                            <td className="px-5 py-3 font-mono text-xs text-muted-foreground">
                              {aluno.matricula || "—"}
                            </td>
                            <td className="px-5 py-3">
                              <span
                                className={
                                  media === null
                                    ? "text-muted-foreground"
                                    : media >= 5
                                      ? "font-mono font-medium text-success"
                                      : "font-mono font-medium text-destructive"
                                }
                              >
                                {fmtMedia(media)}
                              </span>
                            </td>
                            <td className="px-5 py-3">
                              <span
                                className={
                                  freq === null
                                    ? "text-muted-foreground"
                                    : freq >= 0.75
                                      ? "text-success"
                                      : "text-warning"
                                }
                              >
                                {fmtFreq(freq)}
                              </span>
                            </td>
                            <td className="px-5 py-3">
                              <StatusBadge media={media} freq={freq} />
                            </td>
                            <td className="px-5 py-3 text-right">
                              <Button
                                size="icon"
                                variant="ghost"
                                className="h-8 w-8 opacity-0 group-hover:opacity-100"
                                onClick={() => {
                                  if (confirm(`Remover ${aluno.nome}?`)) removeAluno(aluno.id);
                                }}
                                aria-label="Remover aluno"
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
                <div className="border-t px-5 py-2.5 text-xs text-muted-foreground">
                  {alunosFiltrados.length} de {state.alunos.length} aluno(s)
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      )}
    </div>
  );
}
