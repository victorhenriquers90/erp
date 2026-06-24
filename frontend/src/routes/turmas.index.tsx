import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { useStore, mediaTurma } from "@/lib/store";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
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
import { Plus, ArrowRight, Trash2 } from "lucide-react";

export const Route = createFileRoute("/turmas/")({
  head: () => ({
    meta: [
      { title: "Turmas — Regente" },
      { name: "description", content: "Suas turmas, médias e quantidade de alunos." },
    ],
  }),
  component: TurmasPage,
});

function TurmasPage() {
  const { state, addTurma, removeTurma } = useStore();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ nome: "", disciplina: "", ano: String(new Date().getFullYear()) });

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.nome.trim()) return;
    addTurma({
      nome: form.nome.trim(),
      disciplina: form.disciplina.trim(),
      ano: form.ano.trim() || String(new Date().getFullYear()),
    });
    setForm({ nome: "", disciplina: "", ano: String(new Date().getFullYear()) });
    setOpen(false);
  }

  return (
    <div className="mx-auto max-w-6xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
      <PageHeader
        eyebrow="Turmas"
        title="Suas turmas"
        description="Crie turmas, adicione alunos e comece a lançar notas e frequência."
        actions={
          <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
              <Button>
                <Plus className="mr-1.5 h-4 w-4" />
                Nova turma
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle className="font-display">Nova turma</DialogTitle>
                <DialogDescription>Defina os dados básicos da turma.</DialogDescription>
              </DialogHeader>
              <form onSubmit={submit} className="space-y-4">
                <div className="space-y-1.5">
                  <Label htmlFor="nome">Nome</Label>
                  <Input
                    id="nome"
                    placeholder="Ex: 9º Ano A"
                    value={form.nome}
                    onChange={(e) => setForm((f) => ({ ...f, nome: e.target.value }))}
                    required
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <Label htmlFor="disc">Disciplina</Label>
                    <Input
                      id="disc"
                      placeholder="Matemática"
                      value={form.disciplina}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, disciplina: e.target.value }))
                      }
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="ano">Ano letivo</Label>
                    <Input
                      id="ano"
                      value={form.ano}
                      onChange={(e) => setForm((f) => ({ ...f, ano: e.target.value }))}
                    />
                  </div>
                </div>
                <DialogFooter>
                  <Button type="submit">Criar turma</Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        }
      />

      {state.turmas.length === 0 ? (
        <Card>
          <CardContent className="py-16 text-center">
            <p className="text-sm text-muted-foreground">
              Nenhuma turma ainda. Clique em <strong>Nova turma</strong> para começar.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {state.turmas.map((t) => {
            const alunos = state.alunos.filter((a) => a.turmaId === t.id).length;
            const m = mediaTurma(state, t.id);
            return (
              <Card key={t.id} className="group relative overflow-hidden">
                <div className="absolute inset-x-0 top-0 h-1 bg-gold" />
                <CardContent className="space-y-4 pt-6">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <h3 className="font-display text-lg font-semibold text-foreground">
                        {t.nome}
                      </h3>
                      <p className="text-xs text-muted-foreground">
                        {t.disciplina || "—"} · {t.ano}
                      </p>
                    </div>
                    {m !== null ? (
                      <Badge variant="outline" className="border-gold/50 bg-gold/10 font-mono">
                        {m.toFixed(1)}
                      </Badge>
                    ) : (
                      <span className="text-[11px] text-muted-foreground/60">sem notas</span>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {alunos} {alunos === 1 ? "aluno" : "alunos"}
                  </div>
                  <div className="flex items-center justify-between">
                    <Button asChild size="sm" variant="secondary">
                      <Link to="/turmas/$turmaId" params={{ turmaId: t.id }}>
                        Abrir <ArrowRight className="ml-1 h-3.5 w-3.5" />
                      </Link>
                    </Button>
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={() => {
                        if (confirm(`Remover turma "${t.nome}"?`)) removeTurma(t.id);
                      }}
                      aria-label="Remover turma"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
