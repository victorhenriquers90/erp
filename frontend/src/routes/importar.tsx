import { createFileRoute, Link } from "@tanstack/react-router";
import { useState, useRef, useCallback } from "react";
import * as XLSX from "xlsx";
import { useStore } from "@/lib/store";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Upload,
  FileText,
  CheckCircle2,
  XCircle,
  Download,
  Users,
  ClipboardList,
  AlertCircle,
} from "lucide-react";

export const Route = createFileRoute("/importar")({
  head: () => ({
    meta: [
      { title: "Importar — Regente" },
      { name: "description", content: "Importe alunos via CSV ou notas via XLSX." },
    ],
  }),
  component: ImportarPage,
});

/* ─── CSV Alunos ─── */

type AlunoRow = {
  nome: string;
  matricula: string;
  erro?: string;
  selected: boolean;
};

function parseCSV(text: string): AlunoRow[] {
  const lines = text
    .split(/\r?\n/)
    .map((l) => l.trim())
    .filter(Boolean);
  if (lines.length < 2) return [];
  const headerLine = lines[0].toLowerCase();
  const delim = headerLine.includes(";") ? ";" : ",";
  const headers = headerLine.split(delim).map((h) => h.replace(/["'\s]/g, ""));
  const nomeIdx = headers.findIndex((h) =>
    ["nome", "aluno", "name", "student"].includes(h),
  );
  const matIdx = headers.findIndex((h) =>
    ["matricula", "matrícula", "rm", "ra", "codigo", "código", "id"].includes(h),
  );
  const dataLines = nomeIdx === -1 ? lines : lines.slice(1);
  const nCol = nomeIdx === -1 ? 0 : nomeIdx;
  const mCol = matIdx === -1 ? (nomeIdx === -1 ? 1 : -1) : matIdx;
  return dataLines.map((line) => {
    const cols = line.split(delim).map((c) => c.replace(/^"|"$/g, "").trim());
    const nome = cols[nCol]?.trim() ?? "";
    const matricula = mCol >= 0 ? (cols[mCol]?.trim() ?? "") : "";
    return { nome, matricula, erro: !nome ? "Nome vazio" : undefined, selected: !!nome };
  });
}

function downloadAlunosTemplate() {
  const csv =
    "Nome,Matrícula\n" +
    "Ana Beatriz Costa,20250101\n" +
    "Bruno Ferreira,20250102\n" +
    "Carla Nascimento,20250103\n" +
    "Diego Martins,20250104\n" +
    "Eduarda Rocha,20250105";
  const blob = new Blob(["﻿" + csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "template_alunos.csv";
  a.click();
  URL.revokeObjectURL(url);
}

/* ─── XLSX Notas ─── */

type NotaRow = {
  nomeOriginal: string;
  nota: number | null;
  alunoId?: string;
  alunoNome?: string;
  erroMatch?: string;
  erroValor?: string;
  selected: boolean;
};

function normalize(s: string) {
  return s
    .toLowerCase()
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .trim();
}

type RawXLSX = {
  data: Record<string, unknown>[];
  cols: string[];
  colPreviews: Record<string, string>;
  sugNome: string;
  sugNota: string;
};

const NOME_KW = ["nome", "aluno", "name", "student", "estudante", "discente"];
// Termos específicos para VALORES de nota — evitar palavras que aparecem em títulos de relatório
const NOTA_KW = [
  "nota", "valor", "grade", "score", "pontuacao", "resultado", "media",
  "conceito", "mencao", "prova", "trabalho",
  "nf", "vf", "n1", "n2", "n3", "n4", "p1", "p2", "p3",
];

function colPreview(data: Record<string, unknown>[], col: string): string {
  const vals = data
    .map((r) => String(r[col] ?? "").trim())
    .filter(Boolean)
    .slice(0, 2);
  return vals.join(", ");
}

// Letras de coluna Excel (A, B, C...) para exibir __EMPTY com rótulo legível
function colLetter(i: number): string {
  let s = "";
  let n = i;
  do { s = String.fromCharCode(65 + (n % 26)) + s; n = Math.floor(n / 26) - 1; } while (n >= 0);
  return s;
}

async function readXLSX(file: File): Promise<RawXLSX> {
  const buf = await file.arrayBuffer();
  const wb = XLSX.read(buf, { type: "array" });
  const ws = wb.Sheets[wb.SheetNames[0]];

  // Lê como array de arrays para encontrar a linha de cabeçalho real
  const raw2d = XLSX.utils.sheet_to_json<unknown[]>(ws, { header: 1, defval: "" });

  // Procura nas primeiras 15 linhas por células curtas (≤35 chars) com keywords de nome ou nota
  // Limite de comprimento evita falso-positivo em títulos longos tipo "Relatório de Consulta de Avaliação"
  let headerRow = 0;
  for (let i = 0; i < Math.min(15, raw2d.length); i++) {
    const cells = (raw2d[i] as unknown[]).map((c) => {
      const s = normalize(String(c ?? ""));
      return s.length <= 35 ? s : "";
    });
    const hasNome = cells.some((c) => c && NOME_KW.some((kw) => c.includes(kw)));
    const hasNota = cells.some((c) => c && NOTA_KW.some((kw) => c === kw || c.includes(kw)));
    if (hasNome || hasNota) { headerRow = i; break; }
  }

  // Relê a partir da linha de cabeçalho encontrada
  const data = XLSX.utils.sheet_to_json<Record<string, unknown>>(ws, {
    defval: "",
    raw: true,
    range: headerRow,
  });

  if (data.length === 0) return { data: [], cols: [], colPreviews: {}, sugNome: "", sugNota: "" };

  // Todas as colunas — renomeia __EMPTY para "Coluna X" (letra) para mostrar no mapping UI
  const allKeys = Object.keys(data[0]);
  const keyMap: Record<string, string> = {}; // chave original → rótulo exibido
  let colIdx = 0;
  for (const k of allKeys) {
    if (!k || k.startsWith("__EMPTY") || k.trim() === "") {
      keyMap[k] = `Coluna ${colLetter(colIdx)}`;
    } else {
      keyMap[k] = k;
    }
    colIdx++;
  }

  // Renomeia as chaves nos dados para os rótulos legíveis
  const dataRenamed = data.map((row) => {
    const r: Record<string, unknown> = {};
    for (const k of allKeys) r[keyMap[k]] = row[k];
    return r;
  });

  const cols = Object.values(keyMap);
  if (cols.length === 0) return { data: [], cols: [], colPreviews: {}, sugNome: "", sugNota: "" };

  const colPreviews: Record<string, string> = {};
  for (const c of cols) colPreviews[c] = colPreview(dataRenamed, c);

  // Auto-detecção usa apenas colunas com nome real (não "Coluna X")
  const namedCols = cols.filter((c) => !c.startsWith("Coluna "));

  const sugNome =
    namedCols.find((k) => NOME_KW.some((kw) => normalize(k).includes(kw))) ??
    namedCols[0] ??
    cols[0];

  const notaCandidates = cols.filter((k) => k !== sugNome);
  const sugNota =
    notaCandidates.find((k) => NOTA_KW.some((kw) => normalize(k) === kw || normalize(k).includes(kw))) ??
    // fallback: coluna com maioria de valores numéricos nos primeiros 10 registros
    notaCandidates.find((k) => {
      const vals = dataRenamed.slice(0, 10).map((r) => r[k]);
      const numCount = vals.filter(
        (v) => typeof v === "number" || (String(v).trim() !== "" && !isNaN(parseFloat(String(v).replace(",", ".")))),
      ).length;
      return numCount >= Math.min(3, vals.length);
    }) ??
    notaCandidates[0] ??
    cols[1] ??
    cols[0];

  return { data: dataRenamed, cols, colPreviews, sugNome, sugNota };
}

function buildRows(
  raw: Record<string, unknown>[],
  nomeKey: string,
  notaKey: string,
  alunos: { id: string; nome: string }[],
  criarNovos = false,
): NotaRow[] {
  return raw.map((row) => {
    const nomeOriginal = String(row[nomeKey] ?? "").trim();
    const notaRaw = row[notaKey];
    const notaStr = String(notaRaw ?? "").replace(",", ".");
    const nota =
      typeof notaRaw === "number"
        ? notaRaw
        : notaStr !== ""
          ? parseFloat(notaStr)
          : null;

    const erroValor =
      nota !== null && !isNaN(nota) && (nota < 0 || nota > 10)
        ? "Nota fora do intervalo 0–10"
        : nota !== null && isNaN(nota)
          ? "Valor inválido"
          : undefined;

    const normNome = normalize(nomeOriginal);
    const match =
      alunos.find((a) => normalize(a.nome) === normNome) ??
      alunos.find((a) => {
        const aN = normalize(a.nome);
        return aN.includes(normNome) || normNome.includes(aN);
      });

    return {
      nomeOriginal,
      nota: nota !== null && !isNaN(nota) ? nota : null,
      alunoId: match?.id,
      alunoNome: match?.nome,
      erroMatch: !nomeOriginal
        ? "Nome vazio"
        : !match
          ? "Aluno não encontrado na turma"
          : undefined,
      erroValor,
      selected: (!!match || (criarNovos && !!nomeOriginal)) && nota !== null && !isNaN(nota) && !erroValor,
    };
  });
}

function downloadNotasTemplate() {
  const data = [
    ["Nome", "Nota"],
    ["Ana Beatriz Silva", 7.5],
    ["Bruno Carvalho", 6.0],
    ["Carla Mendes", 8.5],
    ["Diego Ribeiro", 5.5],
    ["Eduarda Lima", 9.0],
  ];
  const ws = XLSX.utils.aoa_to_sheet(data);
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, "Notas");
  XLSX.writeFile(wb, "template_notas.xlsx");
}

/* ─── Page ─── */

function ImportarPage() {
  const { state, addAluno, addAvaliacao, setNota, clear } = useStore();


  return (
    <div className="mx-auto max-w-4xl space-y-8 px-4 py-6 sm:px-6 sm:py-8">
      <PageHeader
        eyebrow="Importar"
        title="Importação de dados"
        description="Importe alunos via CSV ou lançe notas em massa via planilha Excel."
      />

      {state.turmas.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
            <Users className="h-10 w-10 text-muted-foreground/25" />
            <div className="space-y-1">
              <p className="text-sm font-medium">Nenhuma turma cadastrada</p>
              <p className="text-xs text-muted-foreground">
                Crie uma turma antes de importar dados.
              </p>
            </div>
            <Button size="sm" asChild>
              <Link to="/turmas">Criar turma</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <Tabs defaultValue="notas">
          <TabsList className="mb-6">
            <TabsTrigger value="alunos">
              <Users className="mr-1.5 h-4 w-4" /> Alunos (CSV)
            </TabsTrigger>
            <TabsTrigger value="notas">
              <ClipboardList className="mr-1.5 h-4 w-4" /> Notas (XLSX)
            </TabsTrigger>
          </TabsList>

          <TabsContent value="alunos">
            <AlunosImport turmas={state.turmas} addAluno={addAluno} />
          </TabsContent>

          <TabsContent value="notas">
            <NotasImport
              turmas={state.turmas}
              state={state}
              addAluno={addAluno}
              addAvaliacao={addAvaliacao}
              setNota={setNota}
            />
          </TabsContent>
        </Tabs>
      )}

      <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4">
        <p className="mb-3 text-sm font-medium text-destructive">Zona de perigo</p>
        <div className="flex items-center justify-between gap-4">
          <p className="text-sm text-muted-foreground">
            Remove todas as turmas, alunos, avaliações, notas e frequências. Irreversível.
          </p>
          <Button
            variant="destructive"
            size="sm"
            className="shrink-0"
            onClick={() => {
              if (window.confirm("Zerar o sistema? Todos os dados serão apagados permanentemente.")) {
                clear();
              }
            }}
          >
            Zerar sistema
          </Button>
        </div>
      </div>
    </div>
  );
}

/* ─── Alunos Tab ─── */

function AlunosImport({
  turmas,
  addAluno,
}: {
  turmas: ReturnType<typeof useStore>["state"]["turmas"];
  addAluno: ReturnType<typeof useStore>["addAluno"];
}) {
  const [turmaId, setTurmaId] = useState(turmas[0]?.id ?? "");
  const [rows, setRows] = useState<AlunoRow[]>([]);
  const [fileName, setFileName] = useState("");
  const [imported, setImported] = useState<{ count: number; turmaId: string } | null>(null);
  const [dragging, setDragging] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFile = useCallback((file: File) => {
    setImported(null);
    setFileName(file.name);
    const reader = new FileReader();
    reader.onload = (e) => setRows(parseCSV(e.target?.result as string));
    reader.readAsText(file, "UTF-8");
  }, []);

  const onDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setDragging(false);
      const file = e.dataTransfer.files[0];
      if (file) handleFile(file);
    },
    [handleFile],
  );

  function toggleRow(i: number) {
    setRows((rs) => rs.map((r, j) => (j === i && !r.erro ? { ...r, selected: !r.selected } : r)));
  }
  function toggleAll() {
    const valid = rows.filter((r) => !r.erro);
    const all = valid.every((r) => r.selected);
    setRows((rs) => rs.map((r) => (r.erro ? r : { ...r, selected: !all })));
  }
  function confirmImport() {
    const valid = rows.filter((r) => r.selected && !r.erro && r.nome);
    const dest = turmaId;
    valid.forEach((r) => addAluno({ turmaId: dest, nome: r.nome, matricula: r.matricula }));
    setImported({ count: valid.length, turmaId: dest });
    setRows([]);
    setFileName("");
  }

  const selectedCount = rows.filter((r) => r.selected && !r.erro).length;
  const allSelected = rows.filter((r) => !r.erro).every((r) => r.selected) && rows.length > 0;

  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="flex flex-col gap-4 pt-6 sm:flex-row sm:items-end sm:justify-between">
          <div className="space-y-1.5">
            <Label>Turma de destino</Label>
            <Select value={turmaId} onValueChange={(v) => { setTurmaId(v); setRows([]); setImported(null); }}>
              <SelectTrigger className="w-64">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {turmas.map((t) => (
                  <SelectItem key={t.id} value={t.id}>
                    {t.nome}{t.disciplina ? ` — ${t.disciplina}` : ""}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <Button variant="outline" size="sm" onClick={downloadAlunosTemplate}>
            <Download className="mr-1.5 h-4 w-4" /> Baixar modelo CSV
          </Button>
        </CardContent>
      </Card>

      <DropZone
        accept=".csv,.txt"
        fileName={fileName}
        dragging={dragging}
        hint="Colunas: Nome (obrigatório) · Matrícula (opcional) · separador , ou ;"
        onDrop={onDrop}
        onDragOver={() => setDragging(true)}
        onDragLeave={() => setDragging(false)}
        onClick={() => inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".csv,.txt"
          className="hidden"
          onChange={(e) => { const f = e.target.files?.[0]; if (f) handleFile(f); e.target.value = ""; }}
        />
      </DropZone>

      {rows.length > 0 && (
        <Card>
          <CardContent className="pt-6">
            <div className="mb-4 flex items-center justify-between gap-3">
              <span className="text-sm font-medium">{rows.length} linha(s) · {selectedCount} selecionada(s)</span>
              <Button size="sm" disabled={selectedCount === 0} onClick={confirmImport}>
                <CheckCircle2 className="mr-1.5 h-4 w-4" />
                Importar {selectedCount > 0 ? `${selectedCount} aluno(s)` : ""}
              </Button>
            </div>
            <AlunosTable rows={rows} allSelected={allSelected} onToggleAll={toggleAll} onToggle={toggleRow} />
          </CardContent>
        </Card>
      )}

      {imported && (
        <SuccessCard
          message={`${imported.count} aluno(s) importado(s) com sucesso`}
          sub={`Adicionados à turma ${turmas.find((t) => t.id === imported.turmaId)?.nome}`}
          link={`/turmas/${imported.turmaId}`}
          linkLabel="Ver turma"
          onDismiss={() => setImported(null)}
        />
      )}
    </div>
  );
}

/* ─── Notas Tab ─── */

function NotasImport({
  turmas,
  state,
  addAluno,
  addAvaliacao,
  setNota,
}: {
  turmas: ReturnType<typeof useStore>["state"]["turmas"];
  state: ReturnType<typeof useStore>["state"];
  addAluno: ReturnType<typeof useStore>["addAluno"];
  addAvaliacao: ReturnType<typeof useStore>["addAvaliacao"];
  setNota: ReturnType<typeof useStore>["setNota"];
}) {
  const [turmaId, setTurmaId] = useState(turmas[0]?.id ?? "");
  const [avaliacaoId, setAvaliacaoId] = useState("nova");
  const [novaAvTitulo, setNovaAvTitulo] = useState("");
  const [novaAvPeso, setNovaAvPeso] = useState("100");
  const [rawXLSX, setRawXLSX] = useState<RawXLSX | null>(null);
  const [nomeCol, setNomeCol] = useState("");
  const [notaCol, setNotaCol] = useState("");
  const [rows, setRows] = useState<NotaRow[]>([]);
  const [fileName, setFileName] = useState("");
  const [imported, setImported] = useState<number | null>(null);
  const [dragging, setDragging] = useState(false);
  const [criarNovos, setCriarNovos] = useState(true);
  const inputRef = useRef<HTMLInputElement>(null);

  const avaliacoesDaTurma = state.avaliacoes.filter((a) => a.turmaId === turmaId);
  const alunosDaTurma = state.alunos.filter((a) => a.turmaId === turmaId);

  const handleFile = useCallback(async (file: File, currentTurmaId: string, novos: boolean) => {
    setImported(null);
    setFileName(file.name);
    // Auto-preenche título da avaliação com o nome do arquivo (sem extensão)
    setNovaAvTitulo((prev) => prev || file.name.replace(/\.xlsx?$/i, ""));
    const result = await readXLSX(file);
    setRawXLSX(result);
    setNomeCol(result.sugNome);
    setNotaCol(result.sugNota);
    if (result.sugNome && result.sugNota && result.data.length > 0) {
      const alunos = state.alunos.filter((a) => a.turmaId === currentTurmaId);
      setRows(buildRows(result.data, result.sugNome, result.sugNota, alunos, novos));
    } else {
      setRows([]);
    }
  }, [state.alunos]);

  const onDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setDragging(false);
      const file = e.dataTransfer.files[0];
      if (file) handleFile(file, turmaId, criarNovos);
    },
    [handleFile, turmaId, criarNovos],
  );

  function handleNomeColChange(val: string) {
    setNomeCol(val);
    if (rawXLSX && val && notaCol) {
      const alunos = state.alunos.filter((a) => a.turmaId === turmaId);
      setRows(buildRows(rawXLSX.data, val, notaCol, alunos, criarNovos));
    }
  }

  function handleNotaColChange(val: string) {
    setNotaCol(val);
    if (rawXLSX && nomeCol && val) {
      const alunos = state.alunos.filter((a) => a.turmaId === turmaId);
      setRows(buildRows(rawXLSX.data, nomeCol, val, alunos, criarNovos));
    }
  }

  function handleCriarNovos(checked: boolean) {
    setCriarNovos(checked);
    if (rawXLSX && nomeCol && notaCol) {
      const alunos = state.alunos.filter((a) => a.turmaId === turmaId);
      setRows(buildRows(rawXLSX.data, nomeCol, notaCol, alunos, checked));
    }
  }

  function toggleRow(i: number) {
    setRows((rs) =>
      rs.map((r, j) => {
        if (j !== i || r.erroValor) return r;
        // Pode selecionar: encontrado OU (criarNovos + nome válido)
        const canToggle = r.alunoId || (criarNovos && !!r.nomeOriginal);
        return canToggle ? { ...r, selected: !r.selected } : r;
      }),
    );
  }

  async function confirmImport() {
    let avId = avaliacaoId;
    if (avaliacaoId === "nova") {
      if (!novaAvTitulo.trim()) return;
      const av = addAvaliacao({
        turmaId,
        titulo: novaAvTitulo.trim(),
        peso: parseFloat(novaAvPeso) || 100,
        data: new Date().toISOString().slice(0, 10),
      });
      avId = av.id;
    }
    let count = 0;
    for (const r of rows) {
      if (!r.selected || r.erroValor || r.nota === null) continue;
      let alunoId = r.alunoId;
      if (!alunoId && criarNovos && r.nomeOriginal && !r.erroValor) {
        const novo = addAluno({ turmaId, nome: r.nomeOriginal, matricula: "" });
        alunoId = novo.id;
      }
      if (alunoId) { setNota(avId, alunoId, r.nota); count++; }
    }
    setImported(count);
    setRawXLSX(null);
    setFileName("");
  }

  // Com criarNovos ativo, linhas sem match mas com nome e nota válidos também contam
  const selectedCount = rows.filter((r) => {
    if (!r.selected || r.erroValor || r.nota === null) return false;
    return r.alunoId || (criarNovos && !!r.nomeOriginal);
  }).length;
  const matchedCount = rows.filter((r) => !!r.alunoId).length;
  const unmatchedCount = rows.filter((r) => !!r.erroMatch && !!r.nomeOriginal).length;

  const canImport =
    selectedCount > 0 &&
    (avaliacaoId !== "nova" || novaAvTitulo.trim().length > 0);

  return (
    <div className="space-y-6">
      {/* Config */}
      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Turma</Label>
              <Select
                value={turmaId}
                onValueChange={(v) => {
                  setTurmaId(v);
                  setAvaliacaoId("nova");
                  setRawXLSX(null);
                  setNomeCol("");
                  setNotaCol("");
                  setImported(null);
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {turmas.map((t) => (
                    <SelectItem key={t.id} value={t.id}>
                      {t.nome}{t.disciplina ? ` — ${t.disciplina}` : ""}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label>Avaliação</Label>
              <Select value={avaliacaoId} onValueChange={setAvaliacaoId}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="nova">+ Nova avaliação</SelectItem>
                  {avaliacoesDaTurma.map((av) => (
                    <SelectItem key={av.id} value={av.id}>
                      {av.titulo}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {avaliacaoId === "nova" && (
            <div className="grid gap-4 rounded-lg border bg-muted/20 p-4 sm:grid-cols-[1fr_120px]">
              <div className="space-y-1.5">
                <Label htmlFor="av-titulo">Título da avaliação</Label>
                <Input
                  id="av-titulo"
                  placeholder="Ex: Prova 1 — Bimestre 1"
                  value={novaAvTitulo}
                  onChange={(e) => setNovaAvTitulo(e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="av-peso">Peso (%)</Label>
                <Input
                  id="av-peso"
                  type="number"
                  min="1"
                  max="100"
                  step="5"
                  value={novaAvPeso}
                  onChange={(e) => setNovaAvPeso(e.target.value)}
                />
              </div>
            </div>
          )}

          <div className="flex justify-end">
            <Button variant="outline" size="sm" onClick={downloadNotasTemplate}>
              <Download className="mr-1.5 h-4 w-4" /> Baixar modelo XLSX
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Aviso turma vazia */}
      {alunosDaTurma.length === 0 && (
        <div className="flex items-start gap-2 rounded-lg border border-warning/40 bg-warning/10 px-4 py-3 text-sm">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
          <span>
            A turma selecionada não possui alunos cadastrados. Nenhum nome do arquivo será encontrado.{" "}
            <Link to="/turmas" className="underline">Adicionar alunos</Link>
          </span>
        </div>
      )}

      {/* Drop zone */}
      <DropZone
        accept=".xlsx,.xls"
        fileName={fileName}
        dragging={dragging}
        hint={`Colunas: Nome · Nota — ${alunosDaTurma.length} aluno(s) na turma selecionada`}
        onDrop={onDrop}
        onDragOver={() => setDragging(true)}
        onDragLeave={() => setDragging(false)}
        onClick={() => inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx,.xls"
          className="hidden"
          onChange={(e) => { const f = e.target.files?.[0]; if (f) handleFile(f, turmaId, criarNovos); e.target.value = ""; }}
        />
      </DropZone>

      {/* Preview */}
      {rows.length > 0 && (
        <Card>
          <CardContent className="pt-6">
            {/* Mapeamento de colunas */}
            {rawXLSX && rawXLSX.cols.length > 0 && (
              <div className="mb-5 rounded-lg border bg-muted/30 p-4">
                <p className="mb-3 text-xs font-medium text-foreground">
                  Mapeamento de colunas
                  <span className="ml-1.5 font-normal text-muted-foreground">
                    — ajuste se a detecção automática estiver errada
                  </span>
                </p>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label className="text-xs">Coluna com o nome do aluno</Label>
                    <Select value={nomeCol} onValueChange={handleNomeColChange}>
                      <SelectTrigger className="h-8 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {rawXLSX.cols.map((c) => (
                          <SelectItem key={c} value={c} className="text-xs">
                            <span className="font-mono">{c}</span>
                            {rawXLSX.colPreviews?.[c] && (
                              <span className="ml-1.5 text-muted-foreground">
                                — {rawXLSX.colPreviews[c]}
                              </span>
                            )}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-xs">Coluna com a nota</Label>
                    <Select value={notaCol} onValueChange={handleNotaColChange}>
                      <SelectTrigger className="h-8 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {rawXLSX.cols.map((c) => (
                          <SelectItem key={c} value={c} className="text-xs">
                            <span className="font-mono">{c}</span>
                            {rawXLSX.colPreviews?.[c] && (
                              <span className="ml-1.5 text-muted-foreground">
                                — {rawXLSX.colPreviews[c]}
                              </span>
                            )}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </div>
            )}

            {unmatchedCount > 0 && (
              <label className="mb-4 flex cursor-pointer items-center gap-2.5 rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-sm">
                <input
                  type="checkbox"
                  checked={criarNovos}
                  onChange={(e) => handleCriarNovos(e.target.checked)}
                  className="h-4 w-4 accent-primary"
                />
                <span>
                  <span className="font-medium">Criar alunos não cadastrados</span>
                  <span className="ml-1.5 text-muted-foreground">
                    — {unmatchedCount} aluno(s) serão adicionados à turma automaticamente
                  </span>
                </span>
              </label>
            )}

            <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <span className="font-medium">{rows.length} linha(s)</span>
                {matchedCount > 0 && (
                  <Badge variant="outline" className="border-success/40 bg-success/10 text-success">
                    {matchedCount} encontrado(s)
                  </Badge>
                )}
                {unmatchedCount > 0 && (
                  <Badge variant="outline" className={criarNovos ? "border-primary/40 bg-primary/10 text-primary" : "border-warning/40 bg-warning/10 text-warning-foreground"}>
                    {unmatchedCount} {criarNovos ? "a criar" : "não encontrado(s)"}
                  </Badge>
                )}
              </div>
              <Button size="sm" disabled={!canImport} onClick={confirmImport}>
                <CheckCircle2 className="mr-1.5 h-4 w-4" />
                Lançar {selectedCount > 0 ? `${selectedCount} nota(s)` : ""}
              </Button>
            </div>

            <div className="overflow-x-auto rounded-lg border">
              <table className="w-full text-sm">
                <thead className="bg-muted/40">
                  <tr className="text-xs uppercase tracking-wider text-muted-foreground">
                    <th className="px-4 py-2.5 text-left">Nome no arquivo</th>
                    <th className="px-4 py-2.5 text-left">Aluno encontrado</th>
                    <th className="px-4 py-2.5 text-left">Nota</th>
                    <th className="px-4 py-2.5 text-left">Status</th>
                    <th className="px-4 py-2.5 w-8"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {rows.map((row, i) => {
                    const hasError = !!row.erroMatch || !!row.erroValor;
                    return (
                      <tr
                        key={i}
                        className={`cursor-pointer transition-colors hover:bg-muted/20 ${!row.selected || hasError ? "opacity-50" : ""}`}
                        onClick={() => toggleRow(i)}
                      >
                        <td className="px-4 py-2.5 text-muted-foreground">
                          {row.nomeOriginal || <span className="italic">vazio</span>}
                        </td>
                        <td className="px-4 py-2.5 font-medium">
                          {row.alunoNome ?? <span className="italic text-muted-foreground">—</span>}
                        </td>
                        <td className="px-4 py-2.5">
                          {row.nota !== null ? (
                            <span
                              className={`font-mono font-medium ${
                                row.erroValor
                                  ? "text-destructive"
                                  : row.nota >= 5
                                    ? "text-success"
                                    : "text-destructive"
                              }`}
                            >
                              {row.nota.toFixed(1).replace(".", ",")}
                            </span>
                          ) : (
                            <span className="text-muted-foreground">—</span>
                          )}
                        </td>
                        <td className="px-4 py-2.5">
                          {row.erroMatch ? (
                            <span className="flex items-center gap-1 text-xs text-muted-foreground">
                              <AlertCircle className="h-3.5 w-3.5 text-warning" /> {row.erroMatch}
                            </span>
                          ) : row.erroValor ? (
                            <span className="flex items-center gap-1 text-xs text-destructive">
                              <XCircle className="h-3.5 w-3.5" /> {row.erroValor}
                            </span>
                          ) : (
                            <span className="flex items-center gap-1 text-xs text-success">
                              <CheckCircle2 className="h-3.5 w-3.5" /> OK
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-2.5 text-center">
                          {!hasError && (
                            <input
                              type="checkbox"
                              checked={row.selected}
                              readOnly
                              className="h-4 w-4 accent-primary"
                            />
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {unmatchedCount > 0 && (
              <p className="mt-3 flex items-start gap-1.5 text-xs text-muted-foreground">
                <AlertCircle className="mt-0.5 h-3.5 w-3.5 shrink-0 text-warning" />
                {criarNovos
                  ? "Alunos não encontrados serão criados automaticamente na turma ao lançar."
                  : "Alunos não encontrados são ignorados. Ative \"Criar alunos não cadastrados\" para criá-los automaticamente."}
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {imported !== null && (
        <SuccessCard
          message={`${imported} nota(s) lançada(s) com sucesso`}
          sub={`Avaliação salva na turma ${turmas.find((t) => t.id === turmaId)?.nome}`}
          link={`/notas`}
          linkLabel="Ver notas"
          onDismiss={() => setImported(null)}
        />
      )}
    </div>
  );
}

/* ─── Shared components ─── */

function DropZone({
  accept,
  fileName,
  dragging,
  hint,
  onDrop,
  onDragOver,
  onDragLeave,
  onClick,
  children,
}: {
  accept: string;
  fileName: string;
  dragging: boolean;
  hint: string;
  onDrop: (e: React.DragEvent) => void;
  onDragOver: () => void;
  onDragLeave: () => void;
  onClick: () => void;
  children?: React.ReactNode;
}) {
  return (
    <div
      role="button"
      tabIndex={0}
      aria-label="Área de upload"
      className={`relative flex cursor-pointer flex-col items-center justify-center gap-3 rounded-xl border-2 border-dashed p-12 text-center transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
        dragging
          ? "border-primary bg-primary/5"
          : "border-border hover:border-primary/40 hover:bg-muted/30"
      }`}
      onDragOver={(e) => { e.preventDefault(); onDragOver(); }}
      onDragLeave={onDragLeave}
      onDrop={onDrop}
      onClick={onClick}
      onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") onClick(); }}
    >
      {children}
      <Upload className="h-8 w-8 text-muted-foreground/40" />
      <div>
        <p className="text-sm font-medium">
          Arraste o arquivo aqui, ou clique para escolher
        </p>
        <p className="mt-1 text-xs text-muted-foreground">{hint}</p>
      </div>
      {fileName && (
        <Badge variant="outline">
          <FileText className="mr-1 h-3 w-3" /> {fileName}
        </Badge>
      )}
    </div>
  );
}

function SuccessCard({
  message,
  sub,
  link,
  linkLabel,
  onDismiss,
}: {
  message: string;
  sub?: string;
  link: string;
  linkLabel: string;
  onDismiss: () => void;
}) {
  return (
    <Card className="border-success/30 bg-success/5">
      <CardContent className="flex items-center gap-3 py-4">
        <CheckCircle2 className="h-5 w-5 shrink-0 text-success" />
        <div className="min-w-0">
          <p className="text-sm font-medium">{message}</p>
          {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
        </div>
        <div className="ml-auto flex shrink-0 gap-2">
          <Button variant="outline" size="sm" asChild>
            <Link to={link}>{linkLabel}</Link>
          </Button>
          <Button variant="ghost" size="sm" onClick={onDismiss}>
            Nova importação
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

function AlunosTable({
  rows,
  allSelected,
  onToggleAll,
  onToggle,
}: {
  rows: AlunoRow[];
  allSelected: boolean;
  onToggleAll: () => void;
  onToggle: (i: number) => void;
}) {
  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-muted/40">
          <tr className="text-xs uppercase tracking-wider text-muted-foreground">
            <th className="px-4 py-2.5 text-left">
              <input
                type="checkbox"
                checked={allSelected}
                onChange={onToggleAll}
                className="h-4 w-4 accent-primary"
              />
            </th>
            <th className="px-4 py-2.5 text-left">Nome</th>
            <th className="px-4 py-2.5 text-left">Matrícula</th>
            <th className="px-4 py-2.5 text-left">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.map((row, i) => (
            <tr
              key={i}
              className={`cursor-pointer transition-colors hover:bg-muted/20 ${!row.selected || row.erro ? "opacity-40" : ""}`}
              onClick={() => onToggle(i)}
            >
              <td className="px-4 py-2.5">
                <input
                  type="checkbox"
                  checked={row.selected && !row.erro}
                  disabled={!!row.erro}
                  readOnly
                  className="h-4 w-4 accent-primary"
                />
              </td>
              <td className="px-4 py-2.5 font-medium">
                {row.nome || <span className="italic text-muted-foreground">vazio</span>}
              </td>
              <td className="px-4 py-2.5 font-mono text-xs text-muted-foreground">
                {row.matricula || "—"}
              </td>
              <td className="px-4 py-2.5">
                {row.erro ? (
                  <span className="flex items-center gap-1 text-xs text-destructive">
                    <XCircle className="h-3.5 w-3.5" /> {row.erro}
                  </span>
                ) : (
                  <span className="flex items-center gap-1 text-xs text-success">
                    <CheckCircle2 className="h-3.5 w-3.5" /> OK
                  </span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
