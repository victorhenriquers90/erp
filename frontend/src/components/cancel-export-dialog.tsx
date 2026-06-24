import { useRef } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export type CancelExportDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Called only when the user explicitly confirms cancellation. */
  onConfirm: () => void;
  /** True while an export is in flight (controls the Esc hint visibility). */
  busy: boolean;
  /** Current export progress, used to enrich the description. */
  progress?: { step: string; pct: number } | null;
  /**
   * Element that triggered/represents the running export. Focus is restored
   * to it when the dialog closes (so keyboard users land back on the
   * "Cancelar" trigger after dismissing the confirmation).
   */
  triggerRef?: React.RefObject<HTMLElement | null>;
};

/**
 * Confirmation dialog shown before aborting an in-flight export.
 *
 * Behavior contract (covered by tests):
 *  - Clicking outside the content or pressing Esc closes the dialog WITHOUT
 *    calling `onConfirm` — the export must keep running.
 *  - Only the destructive "Cancelar exportação" action invokes `onConfirm`.
 *  - When opened, focus moves automatically to the destructive action.
 *  - When closed, focus is restored to `triggerRef` (the export trigger)
 *    when provided, so keyboard users don't lose their place.
 *
 * Uses the underlying Dialog primitive (not AlertDialog) because AlertDialog
 * hardcodes preventDefault on outside interactions, blocking outside-dismiss.
 */
export function CancelExportDialog({
  open,
  onOpenChange,
  onConfirm,
  busy,
  progress,
  triggerRef,
}: CancelExportDialogProps) {
  const actionRef = useRef<HTMLButtonElement>(null);
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        role="alertdialog"
        onOpenAutoFocus={(e) => {
          e.preventDefault();
          actionRef.current?.focus();
        }}
        onCloseAutoFocus={(e) => {
          const target = triggerRef?.current;
          if (target && typeof target.focus === "function") {
            e.preventDefault();
            target.focus();
          }
        }}
      >

        <DialogHeader>
          <DialogTitle>Cancelar exportação?</DialogTitle>
          <DialogDescription>
            A exportação em andamento será interrompida e o arquivo não será gerado.
            {progress ? ` Progresso atual: ${progress.pct}% (${progress.step}).` : ""}
          </DialogDescription>
          {busy && (
            <div className="mt-2 flex items-start gap-2 rounded-md border border-dashed bg-muted/40 px-2.5 py-1.5 text-[11px] text-muted-foreground">
              <span>Dica:</span>
              <span>
                durante a exportação, pressione{" "}
                <kbd className="rounded border bg-background px-1 py-[1px] font-mono text-[10px]">
                  Esc
                </kbd>{" "}
                para abrir este diálogo de cancelamento rapidamente.
              </span>
            </div>
          )}
        </DialogHeader>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Continuar exportando
          </Button>
          <Button
            ref={actionRef}
            type="button"
            onClick={onConfirm}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            Cancelar exportação
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
