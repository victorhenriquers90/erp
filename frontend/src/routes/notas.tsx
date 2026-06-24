import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useStore } from "@/lib/store";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { NotasTab } from "./turmas.$turmaId";
import { ClipboardList } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Link } from "@tanstack/react-router";

export const Route = createFileRoute("/notas")({
  head: () => ({
    meta: [
      { title: "Notas — Regente" },
      {
        name: "description",
        content: "Lance e gerencie as avaliações e notas de todas as turmas.",
      },
    ],
  }),
  component: NotasPage,
});

function NotasPage() {
  const { state } = useStore();
  const [turmaId, setTurmaId] = useState<string>(state.turmas[0]?.id ?? "");

  const turma = state.turmas.find((t) => t.id === turmaId);

  return (
    <div className="mx-auto max-w-6xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
      <PageHeader
        eyebrow="Notas"
        title="Lançamento de notas"
        description="Crie avaliações e lance as notas de cada aluno por turma."
      />

      {state.turmas.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
            <ClipboardList className="h-10 w-10 text-muted-foreground/25" />
            <div className="space-y-1">
              <p className="text-sm font-medium text-foreground">
                Nenhuma turma cadastrada
              </p>
              <p className="text-xs text-muted-foreground">
                Crie uma turma e adicione alunos antes de lançar notas.
              </p>
            </div>
            <Button size="sm" asChild>
              <Link to="/turmas">Criar turma</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-6">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
            <div className="space-y-1.5">
              <Label>Turma</Label>
              <Select value={turmaId} onValueChange={setTurmaId}>
                <SelectTrigger className="w-64">
                  <SelectValue placeholder="Selecione a turma" />
                </SelectTrigger>
                <SelectContent>
                  {state.turmas.map((t) => (
                    <SelectItem key={t.id} value={t.id}>
                      {t.nome}
                      {t.disciplina ? ` — ${t.disciplina}` : ""}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            {turma && (
              <p className="text-xs text-muted-foreground pb-2">
                {turma.disciplina || "—"} · {turma.ano} ·{" "}
                {state.alunos.filter((a) => a.turmaId === turmaId).length} alunos
              </p>
            )}
          </div>

          {turmaId && <NotasTab turmaId={turmaId} />}
        </div>
      )}
    </div>
  );
}
