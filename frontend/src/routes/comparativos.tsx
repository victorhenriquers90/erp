import { createFileRoute } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  useStore,
  mediaAluno,
  mediaTurma,
  getNota,
} from "@/lib/store";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { BarChart2 } from "lucide-react";

const CHART_COLORS = [
  "var(--chart-1)",
  "var(--chart-2)",
  "var(--chart-3)",
  "var(--chart-4)",
  "var(--chart-5)",
];
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/comparativos")({
  head: () => ({
    meta: [
      { title: "Comparativos — Regente" },
      {
        name: "description",
        content: "Gráficos comparativos de desempenho entre turmas, alunos e avaliações.",
      },
    ],
  }),
  component: ComparativosPage,
});

function ComparativosPage() {
  const { state } = useStore();
  const [turmaId, setTurmaId] = useState<string>(state.turmas[0]?.id ?? "");

  const turmasData = useMemo(
    () =>
      state.turmas
        .filter((t) => mediaTurma(state, t.id) !== null)
        .map((t) => ({
          nome: t.nome,
          media: mediaTurma(state, t.id) ?? 0,
        })),
    [state],
  );

  const turma = state.turmas.find((t) => t.id === turmaId);
  const alunosTurma = state.alunos.filter((a) => a.turmaId === turmaId);
  const avsTurma = state.avaliacoes
    .filter((a) => a.turmaId === turmaId)
    .sort((a, b) => (a.data < b.data ? -1 : 1));

  const alunosData = useMemo(
    () =>
      alunosTurma
        .map((a) => ({
          nome: a.nome.split(" ")[0],
          media: mediaAluno(state, a.id, turmaId) ?? 0,
        }))
        .sort((a, b) => b.media - a.media),
    [alunosTurma, state, turmaId],
  );

  const evolucao = useMemo(() => {
    return avsTurma.map((av) => {
      const valores = alunosTurma
        .map((a) => getNota(state, av.id, a.id)?.valor)
        .filter((v): v is number => typeof v === "number");
      const media =
        valores.length > 0
          ? valores.reduce((acc, v) => acc + v, 0) / valores.length
          : 0;
      return { avaliacao: av.titulo, media: Number(media.toFixed(2)) };
    });
  }, [avsTurma, alunosTurma, state]);

  return (
    <div className="mx-auto max-w-6xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
      <PageHeader
        eyebrow="Comparativos"
        title="Análise visual"
        description="Visualize tendências por turma, ranking de alunos e evolução nas avaliações."
      />

      <Card>
        <CardHeader>
          <CardTitle className="font-display">Média por turma</CardTitle>
        </CardHeader>
        <CardContent className="h-64 sm:h-72">
          {turmasData.length === 0 ? (
            <Empty />
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={turmasData}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                <XAxis dataKey="nome" stroke="var(--muted-foreground)" fontSize={12} />
                <YAxis domain={[0, 10]} stroke="var(--muted-foreground)" fontSize={12} />
                <Tooltip
                  contentStyle={{
                    background: "var(--card)",
                    border: "1px solid var(--border)",
                    borderRadius: 8,
                  }}
                />
                <ReferenceLine y={6} stroke="var(--gold)" strokeDasharray="4 3" strokeWidth={1.5} label={{ value: "6,0", position: "right", fontSize: 11, fill: "var(--gold)" }} />
                <Bar dataKey="media" radius={[6, 6, 0, 0]}>
                  {turmasData.map((_d, i) => (
                    <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      <div className="grid items-end gap-3 sm:grid-cols-2">
        <div className="space-y-1.5">
          <Label>Selecione uma turma</Label>
          <Select value={turmaId} onValueChange={setTurmaId}>
            <SelectTrigger>
              <SelectValue placeholder="Escolha" />
            </SelectTrigger>
            <SelectContent>
              {state.turmas.map((t) => (
                <SelectItem key={t.id} value={t.id}>
                  {t.nome}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="font-display">
              Ranking de alunos {turma ? `· ${turma.nome}` : ""}
            </CardTitle>
          </CardHeader>
          <CardContent className="h-72 sm:h-80">
            {alunosData.length === 0 ? (
              <Empty />
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={alunosData} layout="vertical" margin={{ left: 20 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                  <XAxis
                    type="number"
                    domain={[0, 10]}
                    stroke="var(--muted-foreground)"
                    fontSize={12}
                  />
                  <YAxis
                    type="category"
                    dataKey="nome"
                    width={80}
                    stroke="var(--muted-foreground)"
                    fontSize={12}
                  />
                  <Tooltip
                    contentStyle={{
                      background: "var(--card)",
                      border: "1px solid var(--border)",
                      borderRadius: 8,
                    }}
                  />
                  <ReferenceLine x={6} stroke="var(--gold)" strokeDasharray="4 3" strokeWidth={1.5} label={{ value: "6,0", position: "top", fontSize: 11, fill: "var(--gold)" }} />
                  <Bar dataKey="media" radius={[0, 6, 6, 0]} fill="var(--gold)" />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="font-display">Evolução por avaliação</CardTitle>
          </CardHeader>
          <CardContent className="h-72 sm:h-80">
            {evolucao.length === 0 ? (
              <Empty />
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={evolucao}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                  <XAxis
                    dataKey="avaliacao"
                    stroke="var(--muted-foreground)"
                    fontSize={11}
                  />
                  <YAxis domain={[0, 10]} stroke="var(--muted-foreground)" fontSize={12} />
                  <Tooltip
                    contentStyle={{
                      background: "var(--card)",
                      border: "1px solid var(--border)",
                      borderRadius: 8,
                    }}
                  />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="media"
                    name="Média da turma"
                    stroke="var(--primary)"
                    strokeWidth={2.5}
                    dot={{ r: 4, fill: "var(--gold)" }}
                  />
                </LineChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function Empty() {
  return (
    <div className="flex h-full flex-col items-center justify-center gap-3 text-center">
      <BarChart2 className="h-8 w-8 text-muted-foreground/25" />
      <p className="text-sm text-muted-foreground">Sem dados suficientes ainda.</p>
    </div>
  );
}
