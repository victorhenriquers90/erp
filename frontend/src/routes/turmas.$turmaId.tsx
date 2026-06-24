import { createFileRoute, Link, notFound } from "@tanstack/react-router";
import { useEffect, useMemo, useRef, useState, useSyncExternalStore } from "react";
import {
  useStore,
  useTurma,
  useAlunosByTurma,
  useAvaliacoesByTurma,
  getNota,
  mediaAluno,
  frequenciaAluno,
} from "@/lib/store";
import { PageHeader } from "@/components/page-header";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { CancelExportDialog } from "@/components/cancel-export-dialog";
import { ArrowLeft, Plus, Trash2, CalendarDays, Search, ArrowUp, ArrowDown, ArrowUpDown, RotateCcw, Download, ChevronDown, Loader2, CheckCircle2, XCircle, Ban } from "lucide-react";
import { toast } from "sonner";


function normalize(s: string) {
  return s.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "");
}

type SortDir = "asc" | "desc";

function useSort<K extends string>(
  defaultKey: K,
  defaultDir: SortDir = "asc",
  storageKey?: string,
) {
  const [state, setState] = useState<{ key: K; dir: SortDir }>(() => {
    if (typeof window !== "undefined" && storageKey) {
      try {
        const raw = window.localStorage.getItem(storageKey);
        if (raw) {
          const parsed = JSON.parse(raw) as { key: K; dir: SortDir };
          if (parsed && parsed.key && (parsed.dir === "asc" || parsed.dir === "desc")) {
            return parsed;
          }
        }
      } catch {
        /* ignore */
      }
    }
    return { key: defaultKey, dir: defaultDir };
  });

  useEffect(() => {
    if (typeof window === "undefined" || !storageKey) return;
    try {
      window.localStorage.setItem(storageKey, JSON.stringify(state));
    } catch {
      /* ignore */
    }
  }, [storageKey, state]);

  function toggle(next: K) {
    setState((s) =>
      s.key === next
        ? { key: s.key, dir: s.dir === "asc" ? "desc" : "asc" }
        : { key: next, dir: "asc" },
    );
  }

  function reset() {
    setState({ key: defaultKey, dir: defaultDir });
  }

  return { key: state.key, dir: state.dir, toggle, reset };
}

function usePersistedState<T>(storageKey: string, initial: T) {
  const [value, setValue] = useState<T>(() => {
    if (typeof window === "undefined") return initial;
    try {
      const raw = window.localStorage.getItem(storageKey);
      if (raw !== null) return JSON.parse(raw) as T;
    } catch {
      /* ignore */
    }
    return initial;
  });

  // Re-hydrate when the storage key changes (e.g. switching turma).
  useEffect(() => {
    if (typeof window === "undefined") return;
    try {
      const raw = window.localStorage.getItem(storageKey);
      setValue(raw !== null ? (JSON.parse(raw) as T) : initial);
    } catch {
      setValue(initial);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [storageKey]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    try {
      window.localStorage.setItem(storageKey, JSON.stringify(value));
    } catch {
      /* ignore */
    }
  }, [storageKey, value]);

  return [value, setValue] as const;
}

const PAGE_SIZES = [10, 25, 50, 100, 0] as const; // 0 = todos
type PageSize = (typeof PAGE_SIZES)[number];

function usePagination(storagePrefix: string, totalItems: number) {
  const [pageSize, setPageSize] = usePersistedState<PageSize>(
    `${storagePrefix}.pageSize`,
    10,
  );
  const [page, setPage] = usePersistedState<number>(`${storagePrefix}.page`, 1);

  const effectiveSize = pageSize === 0 ? Math.max(totalItems, 1) : pageSize;
  const totalPages = Math.max(1, Math.ceil(totalItems / effectiveSize));
  const safePage = Math.min(Math.max(1, page), totalPages);

  useEffect(() => {
    if (page !== safePage) setPage(safePage);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [safePage]);

  const start = (safePage - 1) * effectiveSize;
  const end = pageSize === 0 ? totalItems : start + effectiveSize;

  function reset() {
    setPage(1);
    setPageSize(10);
  }

  return {
    page: safePage,
    setPage,
    pageSize,
    setPageSize,
    totalPages,
    start,
    end,
    reset,
  };
}

function Paginator({
  total,
  pag,
}: {
  total: number;
  pag: ReturnType<typeof usePagination>;
}) {
  if (total === 0) return null;
  const from = total === 0 ? 0 : pag.start + 1;
  const to = Math.min(pag.end, total);
  return (
    <div className="flex flex-col gap-2 pt-2 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
      <div className="font-mono">
        {from}–{to} de {total}
      </div>
      <div className="flex items-center gap-2">
        <Select
          value={String(pag.pageSize)}
          onValueChange={(v) => {
            pag.setPageSize(Number(v) as PageSize);
            pag.setPage(1);
          }}
        >
          <SelectTrigger className="h-8 w-[110px] text-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {PAGE_SIZES.map((s) => (
              <SelectItem key={s} value={String(s)}>
                {s === 0 ? "Todos" : `${s} / pág.`}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => pag.setPage(Math.max(1, pag.page - 1))}
          disabled={pag.page <= 1}
        >
          Anterior
        </Button>
        <span className="font-mono">
          {pag.page}/{pag.totalPages}
        </span>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => pag.setPage(Math.min(pag.totalPages, pag.page + 1))}
          disabled={pag.page >= pag.totalPages}
        >
          Próxima
        </Button>
      </div>
    </div>
  );
}

function csvEscape(v: unknown): string {
  if (v === null || v === undefined) return "";
  const s = String(v);
  if (/[",;\n\r]/.test(s)) return `"${s.replace(/"/g, '""')}"`;
  return s;
}

type ProgressFn = (step: string, pct: number) => void;

class CancelledError extends Error {
  constructor() {
    super("Exportação cancelada");
    this.name = "CancelledError";
  }
}

const yieldToUI = () => new Promise<void>((r) => setTimeout(r, 0));

function checkAbort(signal?: AbortSignal) {
  if (signal?.aborted) throw new CancelledError();
}

async function downloadCSV(
  filename: string,
  headers: string[],
  rows: unknown[][],
  onProgress?: ProgressFn,
  signal?: AbortSignal,
) {
  checkAbort(signal);
  onProgress?.("Preparando cabeçalho", 2);
  await yieldToUI();
  checkAbort(signal);
  const lines: string[] = [headers.map(csvEscape).join(";")];

  const total = rows.length;
  const chunk = Math.max(200, Math.ceil(total / 50));
  for (let i = 0; i < total; i += chunk) {
    checkAbort(signal);
    const end = Math.min(total, i + chunk);
    for (let j = i; j < end; j++) lines.push(rows[j].map(csvEscape).join(";"));
    const pct = 5 + Math.round((end / Math.max(1, total)) * 80);
    onProgress?.(`Serializando linhas (${end}/${total})`, pct);
    await yieldToUI();
  }

  checkAbort(signal);
  onProgress?.("Gerando arquivo", 90);
  await yieldToUI();
  checkAbort(signal);
  const blob = new Blob(["\ufeff" + lines.join("\r\n")], {
    type: "text/csv;charset=utf-8;",
  });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  onProgress?.("Iniciando download", 98);
  await yieldToUI();
  if (signal?.aborted) {
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    throw new CancelledError();
  }
  a.click();
  document.body.removeChild(a);
  setTimeout(() => URL.revokeObjectURL(url), 0);
  onProgress?.("Concluído", 100);
}

async function downloadXLSX(
  filename: string,
  sheetName: string,
  headers: string[],
  rows: unknown[][],
  onProgress?: ProgressFn,
  signal?: AbortSignal,
) {
  checkAbort(signal);
  onProgress?.("Carregando biblioteca", 5);
  await yieldToUI();
  checkAbort(signal);
  const XLSX = await import("xlsx");

  checkAbort(signal);
  onProgress?.("Montando planilha", 15);
  await yieldToUI();
  const aoa: unknown[][] = [headers];
  const total = rows.length;
  const chunk = Math.max(200, Math.ceil(total / 40));
  for (let i = 0; i < total; i += chunk) {
    checkAbort(signal);
    const end = Math.min(total, i + chunk);
    for (let j = i; j < end; j++) aoa.push(rows[j]);
    const pct = 15 + Math.round((end / Math.max(1, total)) * 55);
    onProgress?.(`Preparando linhas (${end}/${total})`, pct);
    await yieldToUI();
  }

  checkAbort(signal);
  onProgress?.("Convertendo para planilha", 75);
  await yieldToUI();
  checkAbort(signal);
  const ws = XLSX.utils.aoa_to_sheet(aoa);
  ws["!cols"] = headers.map((h) => ({ wch: Math.max(12, h.length + 2) }));

  checkAbort(signal);
  onProgress?.("Criando workbook", 85);
  await yieldToUI();
  checkAbort(signal);
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, sheetName.slice(0, 31));

  checkAbort(signal);
  onProgress?.("Gerando arquivo", 95);
  await yieldToUI();
  checkAbort(signal);
  XLSX.writeFile(wb, filename);
  onProgress?.("Concluído", 100);
}

// Global subscribable counter of in-flight exports, so RetryButtons and any
// other lingering UI in toast-land can react to exports started elsewhere.
const exportBusy = (() => {
  let count = 0;
  const listeners = new Set<() => void>();
  return {
    begin() {
      count += 1;
      listeners.forEach((l) => l());
    },
    end() {
      count = Math.max(0, count - 1);
      listeners.forEach((l) => l());
    },
    subscribe(cb: () => void) {
      listeners.add(cb);
      return () => listeners.delete(cb);
    },
    getSnapshot() {
      return count;
    },
  };
})();

function useExportBusy() {
  return useSyncExternalStore(
    exportBusy.subscribe,
    exportBusy.getSnapshot,
    exportBusy.getSnapshot,
  );
}

function RetryButton({ onRetry }: { onRetry: () => Promise<void> }) {
  const [retrying, setRetrying] = useState(false);
  const busyCount = useExportBusy();
  const disabled = retrying || busyCount > 0;
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={async () => {
        if (disabled) return;
        setRetrying(true);
        try {
          await onRetry();
        } finally {
          // component will usually unmount when the toast is dismissed/replaced
          setRetrying(false);
        }
      }}
      className="inline-flex items-center gap-1 text-[11px] font-medium underline-offset-2 hover:underline text-destructive disabled:opacity-60 disabled:no-underline disabled:cursor-not-allowed"
    >
      {retrying || busyCount > 0 ? (
        <>
          <Loader2 className="h-3 w-3 animate-spin" />
          {retrying ? "Reenviando…" : "Aguardando…"}
        </>
      ) : (
        "Tentar novamente"
      )}
    </button>
  );
}




export function ExportMenu({
  baseName,
  sheetName,
  headers,
  pageRows,
  allRows,
}: {
  baseName: string;
  sheetName: string;
  headers: string[];
  pageRows: unknown[][];
  allRows: unknown[][];
}) {
  const totalCount = allRows.length;
  const pageCount = pageRows.length;
  const [busy, setBusy] = useState<null | string>(null);
  const [progress, setProgress] = useState<{ step: string; pct: number } | null>(null);
  const [open, setOpen] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const busyRef = useRef(false);
  const exportTriggerRef = useRef<HTMLButtonElement | null>(null);

  const [confirmCancel, setConfirmCancel] = useState(false);

  // auto-close the cancel dialog when the export ends (success, error, or cancelled)
  useEffect(() => {
    if (!busy && confirmCancel) setConfirmCancel(false);
  }, [busy, confirmCancel]);

  // Esc opens the cancel confirmation while an export is running.
  // (When the AlertDialog is already open, Radix handles Esc to close it.)
  useEffect(() => {
    if (!busy || confirmCancel) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== "Escape") return;
      e.preventDefault();
      setConfirmCancel(true);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [busy, confirmCancel]);

  function requestCancel() {
    if (!abortRef.current) return;
    setConfirmCancel(true);
  }

  function confirmCancelNow() {
    abortRef.current?.abort();
    setConfirmCancel(false);
  }

  async function run(
    label: string,
    format: "CSV" | "Excel",
    fn: (onProgress: ProgressFn, signal: AbortSignal) => Promise<void>,
  ) {
    if (busyRef.current) return;
    busyRef.current = true;
    exportBusy.begin();
    const controller = new AbortController();
    abortRef.current = controller;
    setBusy(label);
    setOpen(false);
    setProgress({ step: "Iniciando", pct: 0 });
    let lastStep = "Iniciando";
    let lastPct = 0;

    const tid = toast.loading("…");

    const renderToast = (
      kind: "loading" | "success" | "error" | "cancelled",
      step: string,
      pct: number,
      errMsg?: string,
    ) => {
      const titleClass =
        kind === "success"
          ? "text-emerald-600 dark:text-emerald-400"
          : kind === "error"
            ? "text-destructive"
            : kind === "cancelled"
              ? "text-amber-600 dark:text-amber-400"
              : "";
      const Icon =
        kind === "success"
          ? CheckCircle2
          : kind === "error"
            ? XCircle
            : kind === "cancelled"
              ? Ban
              : kind === "loading"
                ? Loader2
                : null;
      const title =
        kind === "success"
          ? `${format} exportado com sucesso`
          : kind === "error"
            ? `Erro ao exportar ${format}`
            : kind === "cancelled"
              ? `Exportação cancelada pelo usuário`
              : `Gerando ${format}…`;
      const node = (
        <div className="flex flex-col gap-1.5 w-full">
          <div className="flex items-center justify-between gap-2 text-sm">
            <span className={`font-medium inline-flex items-center gap-1.5 ${titleClass}`}>
              {Icon && (
                <Icon
                  className={`h-4 w-4 ${kind === "loading" ? "animate-spin" : ""}`}
                />
              )}
              {title}
            </span>
            {kind === "loading" && (
              <span className="tabular-nums text-xs opacity-70">{pct}%</span>
            )}
            {kind === "cancelled" && (
              <span className="tabular-nums text-xs opacity-70">{pct}%</span>
            )}
          </div>
          <div className="text-xs opacity-80 truncate">
            {kind === "error"
              ? (errMsg ?? "Ocorreu um erro inesperado durante a geração.")
              : kind === "cancelled"
                ? `Interrompido em "${step}" — nenhum arquivo foi salvo.`
                : step}
          </div>
          {kind === "loading" && (
            <>
              <div className="h-1.5 w-full rounded-full bg-muted overflow-hidden">
                <div
                  className="h-full bg-primary transition-all duration-150"
                  style={{ width: `${Math.max(2, pct)}%` }}
                />
              </div>
              <div className="flex items-center justify-between gap-2 pt-0.5">
                <span className="text-[10px] opacity-60 truncate">{label}</span>
                <button
                  type="button"
                  onClick={() => {
                    if (controller.signal.aborted) return;
                    setConfirmCancel(true);
                  }}
                  className="text-[11px] font-medium underline-offset-2 hover:underline opacity-80 hover:opacity-100"
                >
                  Cancelar
                </button>
              </div>
            </>
          )}
          {kind !== "loading" && (
            <div className="flex items-center justify-between gap-2 pt-0.5">
              <span className="text-[10px] opacity-60 truncate">{label}</span>
              {kind === "error" && (
                <RetryButton
                  onRetry={async () => {
                    toast.dismiss(tid);
                    await run(label, format, fn);
                  }}
                />
              )}
            </div>
          )}
        </div>
      );
      const opts = {
        id: tid,
        duration:
          kind === "loading" ? Infinity : kind === "error" ? 6000 : 4000,
      };
      if (kind === "loading") toast.loading(node, opts);
      else if (kind === "success") toast.success(node, opts);
      else if (kind === "cancelled") toast.warning(node, opts);
      else toast.error(node, opts);
    };

    renderToast("loading", "Iniciando", 0);

    const onProgress: ProgressFn = (step, pct) => {
      if (controller.signal.aborted) return;
      lastStep = step;
      lastPct = pct;
      setProgress({ step, pct });
      renderToast("loading", step, pct);
    };

    try {
      await yieldToUI();
      await fn(onProgress, controller.signal);
      renderToast("success", "Arquivo gerado e download iniciado.", 100);
    } catch (e) {
      if (e instanceof CancelledError || controller.signal.aborted) {
        renderToast("cancelled", lastStep, lastPct);
      } else {
        renderToast(
          "error",
          lastStep,
          lastPct,
          e instanceof Error ? e.message : String(e),
        );
      }
    } finally {
      abortRef.current = null;
      busyRef.current = false;
      exportBusy.end();
      setBusy(null);
      setProgress(null);
    }
  }

  const cancelDialog = (
    <CancelExportDialog
      open={confirmCancel}
      onOpenChange={setConfirmCancel}
      onConfirm={confirmCancelNow}
      busy={!!busy}
      progress={progress}
      triggerRef={exportTriggerRef}
    />
  );

  if (busy) {
    return (
      <>
        <div className="flex items-center gap-1.5">
          <Button type="button" variant="outline" size="sm" disabled>
            <Loader2 className="mr-1 h-4 w-4 animate-spin" />
            {progress ? `Exportando… ${progress.pct}%` : "Exportando…"}
          </Button>
          <Button
            ref={exportTriggerRef}
            type="button"
            variant="ghost"
            size="sm"
            onClick={requestCancel}
            className="text-destructive hover:text-destructive"
          >
            Cancelar
          </Button>
        </div>
        {cancelDialog}
      </>
    );
  }

  return (
    <>
      <DropdownMenu open={open} onOpenChange={setOpen}>
        <DropdownMenuTrigger asChild>
          <Button type="button" variant="outline" size="sm" disabled={totalCount === 0}>
            <Download className="mr-1 h-4 w-4" />
            Exportar
            <ChevronDown className="ml-1 h-3 w-3 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuLabel className="text-xs">CSV</DropdownMenuLabel>
          <DropdownMenuItem
            onSelect={(e) => {
              e.preventDefault();
              void run(`Página atual (${pageCount})`, "CSV", (op, sig) =>
                downloadCSV(`${baseName}-pagina.csv`, headers, pageRows, op, sig),
              );
            }}
            disabled={pageCount === 0}
          >
            Página atual ({pageCount})
          </DropdownMenuItem>
          <DropdownMenuItem
            onSelect={(e) => {
              e.preventDefault();
              void run(`Todos os filtrados (${totalCount})`, "CSV", (op, sig) =>
                downloadCSV(`${baseName}.csv`, headers, allRows, op, sig),
              );
            }}
            disabled={totalCount === 0}
          >
            Todos os filtrados ({totalCount})
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuLabel className="text-xs">Excel (.xlsx)</DropdownMenuLabel>
          <DropdownMenuItem
            onSelect={(e) => {
              e.preventDefault();
              void run(`Página atual (${pageCount})`, "Excel", (op, sig) =>
                downloadXLSX(`${baseName}-pagina.xlsx`, sheetName, headers, pageRows, op, sig),
              );
            }}
            disabled={pageCount === 0}
          >
            Página atual ({pageCount})
          </DropdownMenuItem>
          <DropdownMenuItem
            onSelect={(e) => {
              e.preventDefault();
              void run(`Todos os filtrados (${totalCount})`, "Excel", (op, sig) =>
                downloadXLSX(`${baseName}.xlsx`, sheetName, headers, allRows, op, sig),
              );
            }}
            disabled={totalCount === 0}
          >
            Todos os filtrados ({totalCount})
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      {cancelDialog}
    </>
  );
}





function cmp(a: unknown, b: unknown, dir: SortDir) {
  const nullA = a === null || a === undefined;
  const nullB = b === null || b === undefined;
  if (nullA && nullB) return 0;
  if (nullA) return 1; // nulls last
  if (nullB) return -1;
  let r: number;
  if (typeof a === "number" && typeof b === "number") r = a - b;
  else r = String(a).localeCompare(String(b), "pt-BR", { sensitivity: "base" });
  return dir === "asc" ? r : -r;
}

function SortHead<K extends string>({
  label,
  sortKey,
  state,
  className,
}: {
  label: string;
  sortKey: K;
  state: { key: K; dir: SortDir; toggle: (k: K) => void };
  className?: string;
}) {
  const active = state.key === sortKey;
  const Icon = !active ? ArrowUpDown : state.dir === "asc" ? ArrowUp : ArrowDown;
  return (
    <TableHead className={className}>
      <button
        type="button"
        onClick={() => state.toggle(sortKey)}
        className={`inline-flex items-center gap-1 hover:text-foreground ${active ? "text-foreground" : ""}`}
      >
        {label}
        <Icon className="h-3 w-3 opacity-60" />
      </button>
    </TableHead>
  );
}



export const Route = createFileRoute("/turmas/$turmaId")({
  head: ({ params }) => ({
    meta: [{ title: `Turma — Regente` }, { name: "robots", content: "noindex" }],
  }),
  component: TurmaDetail,
  notFoundComponent: () => (
    <div className="p-8 text-sm text-muted-foreground">Turma não encontrada.</div>
  ),
});

function TurmaDetail() {
  const { turmaId } = Route.useParams();
  const turma = useTurma(turmaId);
  if (!turma) throw notFound();

  return (
    <div className="mx-auto max-w-6xl space-y-6 px-4 py-6 sm:px-6 sm:py-8">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/turmas">
          <ArrowLeft className="mr-1 h-4 w-4" /> Turmas
        </Link>
      </Button>

      <PageHeader
        eyebrow={`${turma.disciplina || "Disciplina"} · ${turma.ano}`}
        title={turma.nome}
      />

      <Tabs defaultValue="alunos" className="w-full">
        <TabsList className="grid w-full max-w-xl grid-cols-4 text-xs sm:text-sm">
          <TabsTrigger value="alunos">Alunos</TabsTrigger>
          <TabsTrigger value="notas">Notas</TabsTrigger>
          <TabsTrigger value="frequencia">Freq.</TabsTrigger>
          <TabsTrigger value="boletim">Boletim</TabsTrigger>
        </TabsList>

        <TabsContent value="alunos" className="mt-6">
          <AlunosTab turmaId={turmaId} />
        </TabsContent>
        <TabsContent value="notas" className="mt-6">
          <NotasTab turmaId={turmaId} />
        </TabsContent>
        <TabsContent value="frequencia" className="mt-6">
          <FrequenciaTab turmaId={turmaId} />
        </TabsContent>
        <TabsContent value="boletim" className="mt-6">
          <BoletimTab turmaId={turmaId} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function AlunosTab({ turmaId }: { turmaId: string }) {
  const turma = useTurma(turmaId);
  const alunos = useAlunosByTurma(turmaId);
  const { addAluno, removeAluno } = useStore();

  const [nome, setNome] = useState("");
  const [matricula, setMatricula] = useState("");
  const [query, setQuery] = usePersistedState(`regente.filters.alunos.${turmaId}.query`, "");


  const sort = useSort<"nome" | "matricula">("nome", "asc", `regente.sort.alunos.${turmaId}`);
  const filtered = useMemo(() => {
    const q = normalize(query.trim());
    const base = q
      ? alunos.filter(
          (a) => normalize(a.nome).includes(q) || normalize(a.matricula || "").includes(q),
        )
      : alunos;
    return [...base].sort((a, b) => cmp(a[sort.key] || "", b[sort.key] || "", sort.dir));
  }, [alunos, query, sort.key, sort.dir]);

  const pag = usePagination(`regente.pagination.alunos.${turmaId}`, filtered.length);
  const pageItems = useMemo(
    () => filtered.slice(pag.start, pag.end),
    [filtered, pag.start, pag.end],
  );



  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!nome.trim()) return;
    addAluno({ turmaId, nome: nome.trim(), matricula: matricula.trim() });
    setNome("");
    setMatricula("");
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="font-display">Alunos ({filtered.length}/{alunos.length})</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <form onSubmit={submit} className="flex flex-col gap-2 sm:flex-row">
          <Input
            placeholder="Nome do aluno"
            value={nome}
            onChange={(e) => setNome(e.target.value)}

            required
            className="sm:flex-1"
          />
          <Input
            placeholder="Matrícula (opcional)"
            value={matricula}
            onChange={(e) => setMatricula(e.target.value)}
            className="sm:w-48"
          />
          <Button type="submit">
            <Plus className="mr-1 h-4 w-4" /> Adicionar
          </Button>
        </form>

        {alunos.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">
            Nenhum aluno cadastrado nessa turma ainda.
          </p>
        ) : (
          <>
            <div className="flex flex-col gap-2 sm:flex-row">
              <SearchBox
                value={query}
                onChange={setQuery}
                placeholder="Buscar por nome ou matrícula…"
                className="sm:flex-1"
              />
              <ExportMenu
                baseName={`alunos-${turma?.nome ?? turmaId}`}
                sheetName="Alunos"
                headers={["Nome", "Matrícula"]}
                pageRows={pageItems.map((a) => [a.nome, a.matricula || ""])}
                allRows={filtered.map((a) => [a.nome, a.matricula || ""])}
              />


              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  setQuery("");
                  sort.reset();
                  pag.reset();
                }}
                disabled={
                  !query &&
                  sort.key === "nome" &&
                  sort.dir === "asc" &&
                  pag.page === 1 &&
                  pag.pageSize === 10
                }
                className="sm:w-auto"
              >
                <RotateCcw className="mr-1 h-4 w-4" /> Resetar
              </Button>

            </div>


            {filtered.length === 0 ? (
              <p className="py-8 text-center text-sm text-muted-foreground">
                Nenhum aluno corresponde a "{query}".
              </p>
            ) : (
              <>
                <div className="-mx-6 overflow-x-auto sm:mx-0">
                  <Table className="min-w-[480px]">
                    <TableHeader>
                      <TableRow>
                        <SortHead label="Nome" sortKey="nome" state={sort} />
                        <SortHead label="Matrícula" sortKey="matricula" state={sort} />
                        <TableHead className="w-12"></TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {pageItems.map((a) => (
                        <TableRow key={a.id}>
                          <TableCell className="font-medium">{a.nome}</TableCell>
                          <TableCell className="text-muted-foreground">{a.matricula || "—"}</TableCell>
                          <TableCell>
                            <Button
                              size="icon"
                              variant="ghost"
                              onClick={() => {
                                if (confirm(`Remover ${a.nome}?`)) removeAluno(a.id);
                              }}
                              aria-label="Remover aluno"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
                <Paginator total={filtered.length} pag={pag} />
              </>
            )}

          </>
        )}

      </CardContent>
    </Card>
  );
}

export function NotasTab({ turmaId }: { turmaId: string }) {
  const { state, addAvaliacao, removeAvaliacao, setNota } = useStore();
  const alunos = useAlunosByTurma(turmaId);
  const avaliacoes = useAvaliacoesByTurma(turmaId);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({
    titulo: "",
    peso: "100",
    data: new Date().toISOString().slice(0, 10),
  });

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.titulo.trim()) return;
    addAvaliacao({
      turmaId,
      titulo: form.titulo.trim(),
      peso: Number(form.peso) || 100,
      data: form.data,
    });
    setForm({ titulo: "", peso: "100", data: new Date().toISOString().slice(0, 10) });
    setOpen(false);
  }

  return (
    <Card>
      <CardHeader className="grid grid-cols-[minmax(0,1fr)_auto] items-center gap-3">
        <CardTitle className="font-display truncate">
          Avaliações ({avaliacoes.length})
        </CardTitle>
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger asChild>
            <Button size="sm" className="shrink-0">
              <Plus className="mr-1 h-4 w-4" /> <span className="hidden sm:inline">Nova avaliação</span><span className="sm:hidden">Nova</span>
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle className="font-display">Nova avaliação</DialogTitle>
              <DialogDescription>
                Defina título, peso e data da nova avaliação desta turma.
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={submit} className="space-y-4">
              <div className="space-y-1.5">
                <Label htmlFor="titulo">Título</Label>
                <Input
                  id="titulo"
                  placeholder="Prova 1 — Bimestre 2"
                  value={form.titulo}
                  onChange={(e) => setForm((f) => ({ ...f, titulo: e.target.value }))}
                  required
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label htmlFor="peso">Peso (%)</Label>
                  <Input
                    id="peso"
                    type="number"
                    min={1}
                    max={100}
                    step={5}
                    value={form.peso}
                    onChange={(e) => setForm((f) => ({ ...f, peso: e.target.value }))}
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="data">Data</Label>
                  <Input
                    id="data"
                    type="date"
                    value={form.data}
                    onChange={(e) => setForm((f) => ({ ...f, data: e.target.value }))}
                  />
                </div>
              </div>
              <DialogFooter>
                <Button type="submit">Criar</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </CardHeader>
      <CardContent className="space-y-4">
        {alunos.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">
            Adicione alunos antes de lançar notas.
          </p>
        ) : avaliacoes.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">
            Crie uma avaliação para começar a lançar notas.
          </p>
        ) : (
          <div className="-mx-6 overflow-x-auto sm:mx-0">
            <Table className="min-w-[640px]">
              <TableHeader>
                <TableRow>
                  <TableHead className="min-w-[200px]">Aluno</TableHead>
                  {avaliacoes.map((av) => (
                    <TableHead key={av.id} className="text-center">
                      <div className="font-display font-medium text-foreground">
                        {av.titulo}
                      </div>
                      <div className="text-[10px] font-normal text-muted-foreground">
                        {av.peso}%
                        <button
                          onClick={() => {
                            if (confirm(`Excluir avaliação "${av.titulo}"?`))
                              removeAvaliacao(av.id);
                          }}
                          className="ml-2 text-destructive hover:underline"
                        >
                          remover
                        </button>
                      </div>
                    </TableHead>
                  ))}
                  <TableHead className="text-center">Média</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {alunos.map((a) => {
                  const media = mediaAluno(state, a.id, turmaId);
                  return (
                    <TableRow key={a.id}>
                      <TableCell className="font-medium">{a.nome}</TableCell>
                      {avaliacoes.map((av) => {
                        const n = getNota(state, av.id, a.id);
                        return (
                          <TableCell key={av.id} className="text-center">
                            <Input
                              type="number"
                              min={0}
                              max={10}
                              step={0.1}
                              value={n?.valor ?? ""}
                              onChange={(e) => {
                                const v = e.target.value;
                                if (v === "") return;
                                const num = Math.max(0, Math.min(10, Number(v)));
                                setNota(av.id, a.id, num);
                              }}
                              className={`mx-auto h-9 w-20 text-center font-mono ${
                                n?.valor === undefined ? "" :
                                n.valor >= 5
                                  ? "border-success/50 bg-success/5 text-success"
                                  : "border-destructive/50 bg-destructive/5 text-destructive"
                              }`}
                            />
                          </TableCell>
                        );
                      })}
                      <TableCell className="text-center">
                        <Badge
                          variant="outline"
                          className={
                            media === null
                              ? "font-mono"
                              : media >= 5
                                ? "border-success/40 bg-success/10 font-mono text-success"
                                : "border-destructive/40 bg-destructive/10 font-mono text-destructive"
                          }
                        >
                          {media !== null ? media.toFixed(1) : "—"}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function FrequenciaTab({ turmaId }: { turmaId: string }) {
  const turma = useTurma(turmaId);
  const { state, setFrequencia } = useStore();
  const alunos = useAlunosByTurma(turmaId);

  const [data, setData] = usePersistedState(
    `regente.filters.frequencia.${turmaId}.data`,
    new Date().toISOString().slice(0, 10),
  );
  const [query, setQuery] = usePersistedState(`regente.filters.frequencia.${turmaId}.query`, "");
  const [statusFilter, setStatusFilter] = usePersistedState<
    "todos" | "presentes" | "ausentes" | "sem"
  >(`regente.filters.frequencia.${turmaId}.status`, "todos");


  const datasRegistradas = useMemo(() => {
    const set = new Set(
      state.frequencias.filter((f) => f.turmaId === turmaId).map((f) => f.data),
    );
    return Array.from(set).sort().reverse();
  }, [state.frequencias, turmaId]);

  function get(alunoId: string) {
    return state.frequencias.find(
      (f) => f.alunoId === alunoId && f.data === data,
    );
  }

  function marcarTodos(presente: boolean) {
    alunos.forEach((a) => setFrequencia(turmaId, a.id, data, presente));
  }

  const sort = useSort<"nome" | "presente" | "total">("nome", "asc", `regente.sort.frequencia.${turmaId}`);
  const filtered = useMemo(() => {
    const q = normalize(query.trim());
    const base = alunos.filter((a) => {
      if (q && !normalize(a.nome).includes(q) && !normalize(a.matricula || "").includes(q)) {
        return false;
      }
      const f = get(a.id);
      if (statusFilter === "presentes") return f?.presente === true;
      if (statusFilter === "ausentes") return f?.presente === false;
      if (statusFilter === "sem") return !f;
      return true;
    });
    return [...base].sort((a, b) => {
      if (sort.key === "nome") return cmp(a.nome, b.nome, sort.dir);
      if (sort.key === "presente") {
        const va = get(a.id)?.presente;
        const vb = get(b.id)?.presente;
        return cmp(va === undefined ? null : va ? 1 : 0, vb === undefined ? null : vb ? 1 : 0, sort.dir);
      }
      return cmp(frequenciaAluno(state, a.id), frequenciaAluno(state, b.id), sort.dir);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [alunos, query, statusFilter, data, state.frequencias, sort.key, sort.dir]);

  const pag = usePagination(`regente.pagination.frequencia.${turmaId}`, filtered.length);
  const pageItems = useMemo(
    () => filtered.slice(pag.start, pag.end),
    [filtered, pag.start, pag.end],
  );




  return (
    <Card>
      <CardHeader>
        <CardTitle className="font-display flex items-center gap-2">
          <CalendarDays className="h-4 w-4 text-gold" /> Frequência
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
          <div className="space-y-1.5">
            <Label htmlFor="aula">Data da aula</Label>
            <Input
              id="aula"
              type="date"
              value={data}
              onChange={(e) => setData(e.target.value)}
              className="w-full sm:w-48"
            />
          </div>
          <div className="flex flex-wrap gap-2 sm:ml-auto">
            <Button size="sm" variant="secondary" onClick={() => marcarTodos(true)} className="flex-1 sm:flex-none">
              <span className="hidden sm:inline">Marcar todos </span>presentes
            </Button>
            <Button size="sm" variant="ghost" onClick={() => marcarTodos(false)} className="flex-1 sm:flex-none">
              <span className="hidden sm:inline">Marcar todos </span>ausentes
            </Button>
          </div>
        </div>

        {alunos.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">
            Adicione alunos antes de registrar frequência.
          </p>
        ) : (
          <>
            <div className="flex flex-col gap-2 sm:flex-row">
              <SearchBox
                value={query}
                onChange={setQuery}
                placeholder="Buscar aluno…"
                className="sm:flex-1"
              />
              <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as typeof statusFilter)}>
                <SelectTrigger className="sm:w-48">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos</SelectItem>
                  <SelectItem value="presentes">Presentes no dia</SelectItem>
                  <SelectItem value="ausentes">Ausentes no dia</SelectItem>
                  <SelectItem value="sem">Sem registro</SelectItem>
                </SelectContent>
              </Select>
              {(() => {
                const headers = ["Aluno", "Matrícula", "Data", "Presença", "Frequência total"];
                const toRow = (a: (typeof filtered)[number]) => {
                  const f = get(a.id);
                  const total = frequenciaAluno(state, a.id);
                  return [
                    a.nome,
                    a.matricula || "",
                    data,
                    f === undefined ? "Sem registro" : f.presente ? "Presente" : "Ausente",
                    total !== null ? `${Math.round(total * 100)}%` : "",
                  ];
                };
                return (
                  <ExportMenu
                    baseName={`frequencia-${turma?.nome ?? turmaId}-${data}`}
                    sheetName="Frequência"
                    headers={headers}
                    pageRows={pageItems.map(toRow)}
                    allRows={filtered.map(toRow)}
                  />
                );

              })()}

              <Button
                type="button"

                variant="ghost"
                size="sm"
                onClick={() => {
                  setQuery("");
                  setStatusFilter("todos");
                  sort.reset();
                  pag.reset();
                }}
                disabled={
                  !query &&
                  statusFilter === "todos" &&
                  sort.key === "nome" &&
                  sort.dir === "asc" &&
                  pag.page === 1 &&
                  pag.pageSize === 10
                }
              >
                <RotateCcw className="mr-1 h-4 w-4" /> Resetar
              </Button>
            </div>


            {filtered.length === 0 ? (
              <p className="py-8 text-center text-sm text-muted-foreground">
                Nenhum aluno corresponde ao filtro.
              </p>
            ) : (
              <>
                <div className="-mx-6 overflow-x-auto sm:mx-0">
                  <Table className="min-w-[480px]">
                    <TableHeader>
                      <TableRow>
                        <SortHead label="Aluno" sortKey="nome" state={sort} />
                        <SortHead label="Presente" sortKey="presente" state={sort} className="w-32 text-center" />
                        <SortHead label="Frequência total" sortKey="total" state={sort} className="w-32 text-right" />
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {pageItems.map((a) => {
                        const f = get(a.id);
                        const total = frequenciaAluno(state, a.id);
                        return (
                          <TableRow key={a.id}>
                            <TableCell className="font-medium">{a.nome}</TableCell>
                            <TableCell className="text-center">
                              <Checkbox
                                checked={f?.presente ?? false}
                                onCheckedChange={(c) =>
                                  setFrequencia(turmaId, a.id, data, !!c)
                                }
                              />
                            </TableCell>
                            <TableCell className="text-right font-mono text-sm">
                              {total !== null ? `${Math.round(total * 100)}%` : "—"}
                            </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </div>
                <Paginator total={filtered.length} pag={pag} />
              </>
            )}

          </>
        )}

        {datasRegistradas.length > 0 && (
          <div className="text-xs text-muted-foreground">
            Aulas registradas: {datasRegistradas.length}
          </div>
        )}
      </CardContent>
    </Card>
  );
}


function BoletimTab({ turmaId }: { turmaId: string }) {
  const turma = useTurma(turmaId);
  const { state } = useStore();
  const alunos = useAlunosByTurma(turmaId);

  const [query, setQuery] = usePersistedState(`regente.filters.boletim.${turmaId}.query`, "");
  const [situacao, setSituacao] = usePersistedState<"todos" | "aprovado" | "atencao" | "sem">(
    `regente.filters.boletim.${turmaId}.situacao`,
    "todos",
  );


  const sort = useSort<"nome" | "media" | "freq" | "situacao">("nome", "asc", `regente.sort.boletim.${turmaId}`);
  const rows = useMemo(() => {
    const q = normalize(query.trim());
    const base = alunos
      .map((a) => {
        const m = mediaAluno(state, a.id, turmaId);
        const f = frequenciaAluno(state, a.id);
        const ok = (m ?? 0) >= 5 && (f ?? 1) >= 0.75;
        const status: "aprovado" | "atencao" | "sem" =
          m === null ? "sem" : ok ? "aprovado" : "atencao";
        return { a, m, f, ok, status };
      })
      .filter((r) => {
        if (q && !normalize(r.a.nome).includes(q) && !normalize(r.a.matricula || "").includes(q)) {
          return false;
        }
        if (situacao !== "todos" && r.status !== situacao) return false;
        return true;
      });
    return [...base].sort((x, y) => {
      if (sort.key === "nome") return cmp(x.a.nome, y.a.nome, sort.dir);
      if (sort.key === "media") return cmp(x.m, y.m, sort.dir);
      if (sort.key === "freq") return cmp(x.f, y.f, sort.dir);
      return cmp(x.status, y.status, sort.dir);
    });
  }, [alunos, state, turmaId, query, situacao, sort.key, sort.dir]);

  const pag = usePagination(`regente.pagination.boletim.${turmaId}`, rows.length);
  const pageItems = useMemo(
    () => rows.slice(pag.start, pag.end),
    [rows, pag.start, pag.end],
  );




  return (
    <Card>
      <CardHeader>
        <CardTitle className="font-display">Boletim resumido</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {alunos.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">
            Sem alunos para exibir.
          </p>
        ) : (
          <>
            <div className="flex flex-col gap-2 sm:flex-row">
              <SearchBox
                value={query}
                onChange={setQuery}
                placeholder="Buscar aluno…"
                className="sm:flex-1"
              />
              <Select value={situacao} onValueChange={(v) => setSituacao(v as typeof situacao)}>
                <SelectTrigger className="sm:w-48">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todas situações</SelectItem>
                  <SelectItem value="aprovado">Aprovados</SelectItem>
                  <SelectItem value="atencao">Atenção</SelectItem>
                  <SelectItem value="sem">Sem notas</SelectItem>
                </SelectContent>
              </Select>
              {(() => {
                const headers = ["Aluno", "Matrícula", "Média", "Frequência", "Situação"];
                const toRow = ({ a, m, f, ok, status }: (typeof rows)[number]) => [
                  a.nome,
                  a.matricula || "",
                  m !== null ? m.toFixed(1) : "",
                  f !== null ? `${Math.round(f * 100)}%` : "",
                  status === "sem" ? "Sem notas" : ok ? "Aprovado" : "Atenção",
                ];
                return (
                  <ExportMenu
                    baseName={`boletim-${turma?.nome ?? turmaId}`}
                    sheetName="Boletim"
                    headers={headers}
                    pageRows={pageItems.map(toRow)}
                    allRows={rows.map(toRow)}
                  />
                );

              })()}

              <Button
                type="button"

                variant="ghost"
                size="sm"
                onClick={() => {
                  setQuery("");
                  setSituacao("todos");
                  sort.reset();
                  pag.reset();
                }}
                disabled={
                  !query &&
                  situacao === "todos" &&
                  sort.key === "nome" &&
                  sort.dir === "asc" &&
                  pag.page === 1 &&
                  pag.pageSize === 10
                }
              >
                <RotateCcw className="mr-1 h-4 w-4" /> Resetar
              </Button>
            </div>


            {rows.length === 0 ? (
              <p className="py-8 text-center text-sm text-muted-foreground">
                Nenhum aluno corresponde ao filtro.
              </p>
            ) : (
              <>
                <div className="-mx-6 overflow-x-auto sm:mx-0">
                  <Table className="min-w-[520px]">
                    <TableHeader>
                      <TableRow>
                        <SortHead label="Aluno" sortKey="nome" state={sort} />
                        <SortHead label="Média" sortKey="media" state={sort} className="text-center" />
                        <SortHead label="Frequência" sortKey="freq" state={sort} className="text-center" />
                        <SortHead label="Situação" sortKey="situacao" state={sort} className="text-center" />
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {pageItems.map(({ a, m, f, ok }) => (
                        <TableRow key={a.id}>
                          <TableCell className="font-medium">{a.nome}</TableCell>
                          <TableCell className={`text-center font-mono font-semibold ${
                            m === null ? "text-muted-foreground" :
                            m >= 5 ? "text-success" : "text-destructive"
                          }`}>
                            {m !== null ? m.toFixed(1) : "—"}
                          </TableCell>
                          <TableCell className={`text-center font-mono ${
                            f === null ? "text-muted-foreground" :
                            f >= 0.75 ? "text-success" : "text-warning"
                          }`}>
                            {f !== null ? `${Math.round(f * 100)}%` : "—"}
                          </TableCell>
                          <TableCell className="text-center">
                            <Badge
                              variant="outline"
                              className={
                                m === null
                                  ? ""
                                  : ok
                                    ? "border-success/40 bg-success/10 text-success"
                                    : "border-warning/40 bg-warning/10 text-foreground"
                              }
                            >
                              {m === null ? "Sem notas" : ok ? "Aprovado" : "Atenção"}
                            </Badge>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
                <Paginator total={rows.length} pag={pag} />
              </>
            )}

          </>
        )}
      </CardContent>
    </Card>
  );
}

function SearchBox({
  value,
  onChange,
  placeholder,
  className,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  className?: string;
}) {
  return (
    <div className={`relative ${className ?? ""}`}>
      <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="pl-9"
      />
    </div>
  );
}

