import { createFileRoute, Link } from "@tanstack/react-router";
import { useStore, mediaTurma, frequenciaAluno } from "@/lib/store";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  ArrowRight,
  BookOpen,
  ClipboardCheck,
  GraduationCap,
  TrendingUp,
} from "lucide-react";

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "Painel — Regente" },
      {
        name: "description",
        content: "Visão geral das suas turmas, médias e tutorias em aberto.",
      },
    ],
  }),
  component: Dashboard,
});

function Dashboard() {
  const { state } = useStore();
  const totalAlunos = state.alunos.length;
  const totalTurmas = state.turmas.length;
  const tutoriasAbertas = state.tutorias.filter((t) => t.status !== "concluido").length;

  const medias = state.turmas
    .map((t) => ({ t, m: mediaTurma(state, t.id) }))
    .filter((x): x is { t: typeof x.t; m: number } => x.m !== null);
  const mediaGeral =
    medias.length > 0
      ? medias.reduce((acc, x) => acc + x.m, 0) / medias.length
      : null;

  const freqMedia = (() => {
    const fs = state.alunos
      .map((a) => frequenciaAluno(state, a.id))
      .filter((f): f is number => f !== null);
    if (fs.length === 0) return null;
    return fs.reduce((a, b) => a + b, 0) / fs.length;
  })();

  return (
    <div className="mx-auto max-w-6xl space-y-8 px-4 py-6 sm:space-y-10 sm:px-6 sm:py-10">
      {/* Magazine hero */}
      <section className="grid gap-6 lg:grid-cols-5">
        <div className="min-w-0 lg:col-span-3">
          <div className="flex items-center gap-3">
            <span className="gold-rule" />
            <span className="eyebrow">
              {new Date().toLocaleDateString("pt-BR", { weekday: "long", day: "numeric", month: "long" })} · Painel
            </span>
          </div>
          <h1 className="headline mt-4 text-[40px] text-foreground sm:text-5xl lg:text-6xl">
            Olá,
            <br />
            <span className="text-secondary">professor(a).</span>
          </h1>
          <p className="mt-5 max-w-xl text-[15px] leading-relaxed text-muted-foreground">
            Acompanhe suas turmas, médias e os alunos que pedem mais atenção —
            tudo organizado como um caderno editorial.
          </p>
          <div className="mt-6 flex flex-wrap gap-2">
            <Button asChild>
              <Link to="/turmas">
                Minhas turmas <ArrowRight className="ml-1.5 h-4 w-4" />
              </Link>
            </Button>
            <Button asChild variant="outline">
              <Link to="/tutoria">Abrir tutorias</Link>
            </Button>
          </div>
        </div>

        <div className="navy-card min-w-0 rounded-2xl p-6 sm:p-7 lg:col-span-2">
          <div className="eyebrow text-gold">Painel</div>
          <div className="mt-4 font-display text-[56px] font-semibold leading-none text-gold sm:text-[64px]">
            {mediaGeral !== null ? mediaGeral.toFixed(1) : "—"}
          </div>
          <div className="mt-2 text-sm text-white/70">média geral ponderada</div>
          <div className="mt-6 h-px w-full bg-white/10" />
          <div className="mt-5 grid grid-cols-2 gap-4 text-white/90">
            <div>
              <div className="font-mono text-2xl">{totalTurmas}</div>
              <div className="text-[11px] uppercase tracking-wider text-white/50">turmas</div>
            </div>
            <div>
              <div className="font-mono text-2xl">{totalAlunos}</div>
              <div className="text-[11px] uppercase tracking-wider text-white/50">alunos</div>
            </div>
            <div>
              <div className="font-mono text-2xl">
                {freqMedia !== null ? `${Math.round(freqMedia * 100)}%` : "—"}
              </div>
              <div className="text-[11px] uppercase tracking-wider text-white/50">frequência</div>
            </div>
            <div>
              <div className="font-mono text-2xl">{tutoriasAbertas}</div>
              <div className="text-[11px] uppercase tracking-wider text-white/50">tutorias</div>
            </div>
          </div>
        </div>
      </section>

      {/* Indicadores rápidos */}
      <div className="grid gap-3 grid-cols-2 lg:grid-cols-4">
        <StatCard icon={<BookOpen className="h-4 w-4" />} label="Turmas" value={String(totalTurmas)} />
        <StatCard icon={<GraduationCap className="h-4 w-4" />} label="Alunos" value={String(totalAlunos)} />
        <StatCard
          icon={<TrendingUp className="h-4 w-4" />}
          label="Média geral"
          value={mediaGeral !== null ? mediaGeral.toFixed(1) : "—"}
          hint="ponderada por avaliação"
        />
        <StatCard
          icon={<ClipboardCheck className="h-4 w-4" />}
          label="Frequência"
          value={freqMedia !== null ? `${Math.round(freqMedia * 100)}%` : "—"}
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="ink-card lg:col-span-2">
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <div className="eyebrow">Seção</div>
              <CardTitle className="headline mt-1 text-2xl">Turmas</CardTitle>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/turmas">Ver todas</Link>
            </Button>
          </CardHeader>
          <CardContent>
            {state.turmas.length === 0 ? (
              <EmptyHint
                title="Você ainda não cadastrou turmas"
                cta={
                  <Button asChild size="sm">
                    <Link to="/turmas">Criar primeira turma</Link>
                  </Button>
                }
              />
            ) : (
              <ul className="divide-y divide-border/70">
                {state.turmas.map((t) => {
                  const m = mediaTurma(state, t.id);
                  const alunosCount = state.alunos.filter(
                    (a) => a.turmaId === t.id,
                  ).length;
                  return (
                    <li key={t.id}>
                      <Link
                        to="/turmas/$turmaId"
                        params={{ turmaId: t.id }}
                        className="group -mx-2 flex items-center justify-between rounded-md px-2 py-3.5 transition-colors hover:bg-accent/60"
                      >
                        <div className="flex min-w-0 items-center gap-3 sm:gap-4">
                          <span className="shrink-0 font-mono text-xs text-muted-foreground">
                            {String(state.turmas.indexOf(t) + 1).padStart(2, "0")}
                          </span>
                          <div className="min-w-0">
                            <div className="truncate font-display text-base font-medium text-foreground">
                              {t.nome}
                            </div>
                            <div className="truncate text-xs text-muted-foreground">
                              {t.disciplina} · {alunosCount} alunos · {t.ano}
                            </div>
                          </div>
                        </div>
                        <div className="flex items-center gap-3">
                          <Badge
                            variant="outline"
                            className="border-gold/40 bg-gold/10 font-mono text-foreground"
                          >
                            {m !== null ? m.toFixed(1) : "—"}
                          </Badge>
                          <ArrowRight className="h-4 w-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
                        </div>
                      </Link>
                    </li>
                  );
                })}
              </ul>
            )}
          </CardContent>
        </Card>

        <Card className="ink-card">
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <div className="eyebrow">Seção</div>
              <CardTitle className="headline mt-1 text-2xl">Tutoria</CardTitle>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/tutoria">Abrir</Link>
            </Button>
          </CardHeader>
          <CardContent>
            <div className="flex items-baseline gap-2">
              <span className="font-display text-5xl font-semibold text-foreground">
                {tutoriasAbertas}
              </span>
              <span className="text-sm text-muted-foreground">em aberto</span>
            </div>
            <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
              Registre conversas, planos de ação e o progresso de cada aluno em
              acompanhamento individual.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function StatCard({
  icon,
  label,
  value,
  hint,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <Card className="ink-card">
      <CardContent className="pt-6">
        <div className="flex items-center gap-2 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
          <span className="text-gold">{icon}</span>
          {label}
        </div>
        <div className="mt-3 font-display text-3xl font-semibold text-foreground">
          {value}
        </div>
        {hint && <div className="mt-1 text-xs text-muted-foreground">{hint}</div>}
      </CardContent>
    </Card>
  );
}


function EmptyHint({ title, cta }: { title: string; cta?: React.ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 rounded-md border border-dashed border-border py-10 text-center">
      <p className="text-sm text-muted-foreground">{title}</p>
      {cta}
    </div>
  );
}
