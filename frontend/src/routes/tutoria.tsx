import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useStore } from "@/lib/store";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
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
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Plus, Trash2, HeartHandshake } from "lucide-react";

export const Route = createFileRoute("/tutoria")({
  head: () => ({
    meta: [
      { title: "Tutoria — Regente" },
      {
        name: "description",
        content: "Acompanhamento individual de alunos: anotações, planos de ação e status.",
      },
    ],
  }),
  component: TutoriaPage,
});

const statusLabel: Record<string, string> = {
  aberto: "Aberto",
  em_andamento: "Em andamento",
  concluido: "Concluído",
};

function TutoriaPage() {
  const { state, addTutoria, updateTutoria, removeTutoria } = useStore();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({
    alunoId: "",
    titulo: "",
    notas: "",
    acao: "",
    data: new Date().toISOString().slice(0, 10),
  });

  function alunoNome(id: string) {
    return state.alunos.find((a) => a.id === id)?.nome ?? "Aluno removido";
  }
  function turmaDoAluno(id: string) {
    const a = state.alunos.find((x) => x.id === id);
    if (!a) return "";
    return state.turmas.find((t) => t.id === a.turmaId)?.nome ?? "";
  }

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.alunoId || !form.titulo.trim()) return;
    addTutoria({
      alunoId: form.alunoId,
      titulo: form.titulo.trim(),
      notas: form.notas.trim(),
      acao: form.acao.trim(),
      data: form.data,
      status: "aberto",
    });
    setForm({
      alunoId: "",
      titulo: "",
      notas: "",
      acao: "",
      data: new Date().toISOString().slice(0, 10),
    });
    setOpen(false);
  }

  const ordenadas = [...state.tutorias].sort((a, b) => (a.data < b.data ? 1 : -1));

  return (
    <div className="mx-auto max-w-5xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
      <PageHeader
        eyebrow="Tutoria"
        title="Acompanhamento individual"
        description="Registre conversas, combinados e planos de ação com cada aluno."
        actions={
          <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
              <Button disabled={state.alunos.length === 0}>
                <Plus className="mr-1 h-4 w-4" /> Nova tutoria
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle className="font-display">Nova tutoria</DialogTitle>
                <DialogDescription>
                  Registre uma sessão de tutoria com aluno, observações e plano de ação.
                </DialogDescription>
              </DialogHeader>
              <form onSubmit={submit} className="space-y-4">
                <div className="space-y-1.5">
                  <Label>Aluno</Label>
                  <Select
                    value={form.alunoId}
                    onValueChange={(v) => setForm((f) => ({ ...f, alunoId: v }))}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione o aluno" />
                    </SelectTrigger>
                    <SelectContent>
                      {state.alunos.map((a) => (
                        <SelectItem key={a.id} value={a.id}>
                          {a.nome} — {turmaDoAluno(a.id)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="t-titulo">Título</Label>
                  <Input
                    id="t-titulo"
                    placeholder="Ex: Dificuldade em frações"
                    value={form.titulo}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, titulo: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="t-notas">Observações</Label>
                  <Textarea
                    id="t-notas"
                    rows={3}
                    placeholder="O que foi conversado, contexto, percepções..."
                    value={form.notas}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, notas: e.target.value }))
                    }
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="t-acao">Plano de ação</Label>
                  <Textarea
                    id="t-acao"
                    rows={2}
                    placeholder="Combinados, exercícios, reforço..."
                    value={form.acao}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, acao: e.target.value }))
                    }
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="t-data">Data</Label>
                  <Input
                    id="t-data"
                    type="date"
                    value={form.data}
                    onChange={(e) => setForm((f) => ({ ...f, data: e.target.value }))}
                  />
                </div>
                <DialogFooter>
                  <Button type="submit">Registrar</Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        }
      />

      {ordenadas.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
            <HeartHandshake className="h-10 w-10 text-muted-foreground/25" />
            <div className="space-y-1">
              <p className="text-sm font-medium text-foreground">
                {state.alunos.length === 0
                  ? "Nenhum aluno cadastrado ainda"
                  : "Nenhuma tutoria registrada"}
              </p>
              <p className="text-xs text-muted-foreground">
                {state.alunos.length === 0
                  ? "Crie uma turma e adicione alunos antes de iniciar acompanhamentos."
                  : "Registre conversas, planos de ação e o progresso individual dos alunos."}
              </p>
            </div>
            {state.alunos.length > 0 && (
              <Button size="sm" onClick={() => setOpen(true)}>
                <Plus className="mr-1.5 h-3.5 w-3.5" /> Nova tutoria
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {ordenadas.map((t) => (
            <Card key={t.id} className="overflow-hidden">
              <CardContent className="space-y-3 pt-6">
                <div className="grid grid-cols-[minmax(0,1fr)_auto] items-start gap-3">
                  <div className="min-w-0">
                    <div className="truncate text-xs uppercase tracking-wider text-gold">
                      {alunoNome(t.alunoId)} · {turmaDoAluno(t.alunoId)}
                    </div>
                    <h3 className="mt-1 font-display text-lg font-semibold text-foreground">
                      {t.titulo}
                    </h3>
                    <div className="text-xs text-muted-foreground">
                      {new Date(t.data).toLocaleDateString("pt-BR")}
                    </div>
                  </div>
                  <div className="flex shrink-0 items-center gap-2">
                    <Select
                      value={t.status}
                      onValueChange={(v) =>
                        updateTutoria(t.id, { status: v as typeof t.status })
                      }
                    >
                      <SelectTrigger className="h-8 w-[120px] sm:w-[160px]">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="aberto">Aberto</SelectItem>
                        <SelectItem value="em_andamento">Em andamento</SelectItem>
                        <SelectItem value="concluido">Concluído</SelectItem>
                      </SelectContent>
                    </Select>
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={() => {
                        if (confirm("Excluir registro?")) removeTutoria(t.id);
                      }}
                      aria-label="Excluir"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>

                {t.notas && (
                  <div>
                    <div className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                      Observações
                    </div>
                    <p className="mt-1 whitespace-pre-wrap text-sm text-foreground/90">
                      {t.notas}
                    </p>
                  </div>
                )}
                {t.acao && (
                  <div>
                    <div className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                      Plano de ação
                    </div>
                    <p className="mt-1 whitespace-pre-wrap text-sm text-foreground/90">
                      {t.acao}
                    </p>
                  </div>
                )}

                <Badge
                  variant="outline"
                  className={
                    t.status === "concluido"
                      ? "border-success/40 bg-success/10 text-success"
                      : t.status === "em_andamento"
                        ? "border-gold/50 bg-gold/10 text-foreground"
                        : "border-border bg-muted text-muted-foreground"
                  }
                >
                  {statusLabel[t.status]}
                </Badge>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
