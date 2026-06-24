import { describe, it, expect, vi } from "vitest";
import { useRef, useState } from "react";
import { render, screen, fireEvent, act, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CancelExportDialog } from "./cancel-export-dialog";

function Harness({
  onConfirm,
  initiallyOpen = true,
}: {
  onConfirm: () => void;
  initiallyOpen?: boolean;
}) {
  const [open, setOpen] = useState(initiallyOpen);
  return (
    <div>
      <button type="button" onClick={() => setOpen(true)}>
        abrir
      </button>
      <span data-testid="open-state">{open ? "open" : "closed"}</span>
      <CancelExportDialog
        open={open}
        onOpenChange={setOpen}
        onConfirm={onConfirm}
        busy
        progress={{ step: "Serializando", pct: 42 }}
      />
    </div>
  );
}

describe("CancelExportDialog", () => {
  it("fecha ao clicar fora sem invocar onConfirm (exportação continua)", async () => {
    const onConfirm = vi.fn();
    render(<Harness onConfirm={onConfirm} />);

    const dialog = await screen.findByRole("alertdialog", {
      name: /cancelar exportação\?/i,
    });
    expect(dialog).toBeInTheDocument();

    // Radix registers the outside-pointer listener via setTimeout(0) — yield first
    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });

    const outside = document.body.querySelector("button");
    expect(outside).not.toBeNull();

    await act(async () => {
      outside!.dispatchEvent(
        new Event("pointerdown", { bubbles: true, cancelable: true }),
      );
    });

    // Modal fechou
    expect(screen.getByTestId("open-state").textContent).toBe("closed");
    expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();

    // E o cancelamento NÃO foi confirmado — exportação segue em andamento
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("Esc fecha o dialog sem confirmar cancelamento", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();
    render(<Harness onConfirm={onConfirm} />);

    await screen.findByRole("alertdialog");
    await user.keyboard("{Escape}");

    expect(screen.getByTestId("open-state").textContent).toBe("closed");
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("apenas o botão 'Cancelar exportação' chama onConfirm", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();
    render(<Harness onConfirm={onConfirm} />);

    await user.click(
      await screen.findByRole("button", { name: /^cancelar exportação$/i }),
    );
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it("clicar em 'Continuar exportando' fecha sem confirmar", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();
    render(<Harness onConfirm={onConfirm} />);

    await user.click(
      await screen.findByRole("button", { name: /continuar exportando/i }),
    );

    expect(screen.getByTestId("open-state").textContent).toBe("closed");
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("foca automaticamente em 'Cancelar exportação' ao abrir (e não em 'Continuar')", async () => {
    const onConfirm = vi.fn();
    render(<Harness onConfirm={onConfirm} />);

    const cancelBtn = await screen.findByRole("button", {
      name: /^cancelar exportação$/i,
    });
    const continueBtn = screen.getByRole("button", {
      name: /continuar exportando/i,
    });

    await waitFor(() => {
      expect(document.activeElement).toBe(cancelBtn);
    });
    expect(document.activeElement).not.toBe(continueBtn);
  });

  it("mantém o foco preso dentro do modal (focus trap) ao tabular", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();
    render(
      <div>
        <button type="button" data-testid="outside-before">
          fora-antes
        </button>
        <Harness onConfirm={onConfirm} />
        <button type="button" data-testid="outside-after">
          fora-depois
        </button>
      </div>,
    );

    const dialog = await screen.findByRole("alertdialog");
    const cancelBtn = screen.getByRole("button", {
      name: /^cancelar exportação$/i,
    });
    const continueBtn = screen.getByRole("button", {
      name: /continuar exportando/i,
    });

    await waitFor(() => {
      expect(document.activeElement).toBe(cancelBtn);
    });

    // Tab → próximo focável dentro do dialog (não escapa para "fora-depois")
    await user.tab();
    expect(dialog.contains(document.activeElement)).toBe(true);
    expect(document.activeElement).not.toBe(
      screen.getByTestId("outside-after"),
    );
    expect(document.activeElement).not.toBe(
      screen.getByTestId("outside-before"),
    );

    // Mais alguns tabs em loop — todos devem permanecer dentro do modal
    for (let i = 0; i < 5; i++) {
      await user.tab();
      expect(dialog.contains(document.activeElement)).toBe(true);
    }

    // Shift+Tab também não escapa
    for (let i = 0; i < 5; i++) {
      await user.tab({ shift: true });
      expect(dialog.contains(document.activeElement)).toBe(true);
    }

    // O foco continua dentro do dialog (já validado acima). Os botões do
    // footer são os alvos primários; o DialogContent também expõe um botão
    // de fechar (X), que faz parte do trap — todos pertencem ao modal.
    expect(dialog.contains(document.activeElement)).toBe(true);
  });

  it("restaura o foco para o triggerRef quando o dialog fecha", async () => {
    function FocusHarness({ onConfirm }: { onConfirm: () => void }) {
      const [open, setOpen] = useState(false);
      const triggerRef = useRef<HTMLButtonElement | null>(null);
      return (
        <div>
          <button
            ref={triggerRef}
            type="button"
            data-testid="export-trigger"
            onClick={() => setOpen(true)}
          >
            Cancelar (trigger da exportação)
          </button>
          <CancelExportDialog
            open={open}
            onOpenChange={setOpen}
            onConfirm={onConfirm}
            busy
            progress={null}
            triggerRef={triggerRef}
          />
        </div>
      );
    }

    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<FocusHarness onConfirm={onConfirm} />);

    const trigger = screen.getByTestId("export-trigger");
    trigger.focus();
    expect(document.activeElement).toBe(trigger);

    await user.click(trigger);
    await screen.findByRole("alertdialog");

    // Foco move para o botão destrutivo dentro do modal
    await waitFor(() => {
      expect(document.activeElement).toBe(
        screen.getByRole("button", { name: /^cancelar exportação$/i }),
      );
    });

    // Fecha via Esc — foco deve retornar para o trigger da exportação
    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(document.activeElement).toBe(trigger);
    });
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("restaura o foco ao trigger quando fecha por clique fora (sem confirmar)", async () => {
    function FocusHarness({ onConfirm }: { onConfirm: () => void }) {
      const [open, setOpen] = useState(false);
      const triggerRef = useRef<HTMLButtonElement | null>(null);
      return (
        <div>
          <button
            ref={triggerRef}
            type="button"
            data-testid="export-trigger"
            onClick={() => setOpen(true)}
          >
            Cancelar (trigger da exportação)
          </button>
          <CancelExportDialog
            open={open}
            onOpenChange={setOpen}
            onConfirm={onConfirm}
            busy
            progress={null}
            triggerRef={triggerRef}
          />
        </div>
      );
    }

    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<FocusHarness onConfirm={onConfirm} />);

    const trigger = screen.getByTestId("export-trigger") as HTMLButtonElement;
    await user.click(trigger);

    await screen.findByRole("alertdialog");

    // Radix registra o listener pointer-down-outside via setTimeout(0)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });

    // Dispara pointerdown FORA do conteúdo do modal — no próprio trigger
    await act(async () => {
      trigger.dispatchEvent(
        new Event("pointerdown", { bubbles: true, cancelable: true }),
      );
    });

    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(document.activeElement).toBe(trigger);
    });
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("Esc durante exportação fecha o modal e devolve o foco ao trigger", async () => {
    function FocusHarness({ onConfirm }: { onConfirm: () => void }) {
      const [open, setOpen] = useState(false);
      const triggerRef = useRef<HTMLButtonElement | null>(null);
      return (
        <div>
          <button
            ref={triggerRef}
            type="button"
            data-testid="export-trigger"
            onClick={() => setOpen(true)}
          >
            Cancelar exportação (trigger)
          </button>
          <CancelExportDialog
            open={open}
            onOpenChange={setOpen}
            onConfirm={onConfirm}
            busy
            progress={{ step: "Serializando", pct: 80 }}
            triggerRef={triggerRef}
          />
        </div>
      );
    }

    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<FocusHarness onConfirm={onConfirm} />);

    const trigger = screen.getByTestId("export-trigger") as HTMLButtonElement;
    await user.click(trigger);

    await screen.findByRole("alertdialog");

    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(document.activeElement).toBe(trigger);
    });
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("ao abrir o modal durante a exportação, o foco inicial vai para 'Cancelar exportação'", async () => {
    function OpenDuringExportHarness({ onConfirm }: { onConfirm: () => void }) {
      const [open, setOpen] = useState(false);
      const triggerRef = useRef<HTMLButtonElement | null>(null);
      return (
        <div>
          <button
            ref={triggerRef}
            type="button"
            data-testid="export-trigger"
            onClick={() => setOpen(true)}
          >
            Cancelar exportação (trigger)
          </button>
          <CancelExportDialog
            open={open}
            onOpenChange={setOpen}
            onConfirm={onConfirm}
            busy
            progress={{ step: "Serializando", pct: 33 }}
            triggerRef={triggerRef}
          />
        </div>
      );
    }

    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<OpenDuringExportHarness onConfirm={onConfirm} />);

    // Antes de abrir, o modal não existe
    expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();

    // Abre o modal durante a exportação em andamento
    await user.click(screen.getByTestId("export-trigger"));

    const dialog = await screen.findByRole("alertdialog", {
      name: /cancelar exportação\?/i,
    });
    const cancelBtn = screen.getByRole("button", {
      name: /^cancelar exportação$/i,
    });
    const continueBtn = screen.getByRole("button", {
      name: /continuar exportando/i,
    });

    // Foco inicial vai para o botão destrutivo dentro do modal
    await waitFor(() => {
      expect(document.activeElement).toBe(cancelBtn);
    });
    expect(dialog.contains(document.activeElement)).toBe(true);
    expect(document.activeElement).not.toBe(continueBtn);
    expect(document.activeElement).not.toBe(
      screen.getByTestId("export-trigger"),
    );
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("expõe propriedades de acessibilidade corretas (role/aria-labelledby/aria-describedby)", async () => {
    render(<Harness onConfirm={vi.fn()} />);

    const dialog = await screen.findByRole("alertdialog", {
      name: /cancelar exportação\?/i,
    });

    // role explícito
    expect(dialog).toHaveAttribute("role", "alertdialog");

    // aria-labelledby aponta para o título visível

    const labelledBy = dialog.getAttribute("aria-labelledby");
    expect(labelledBy).toBeTruthy();
    const titleEl = document.getElementById(labelledBy!);
    expect(titleEl).not.toBeNull();
    expect(titleEl!.textContent).toMatch(/cancelar exportação\?/i);

    // aria-describedby aponta para a descrição explicativa
    const describedBy = dialog.getAttribute("aria-describedby");
    expect(describedBy).toBeTruthy();
    const descEl = document.getElementById(describedBy!);
    expect(descEl).not.toBeNull();
    expect(descEl!.textContent).toMatch(
      /exportação em andamento será interrompida/i,
    );

    // Os botões de ação têm nomes acessíveis (não são icon-only sem label)
    expect(
      screen.getByRole("button", { name: /^cancelar exportação$/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /continuar exportando/i }),
    ).toBeInTheDocument();
  });

  it("expõe aria-modal=\"true\" e mantém role=alertdialog (comportamento de overlay coerente)", async () => {
    render(<Harness onConfirm={vi.fn()} />);

    const dialog = await screen.findByRole("alertdialog");
    // Modal real → aria-modal="true"
    expect(dialog).toHaveAttribute("aria-modal", "true");
    // Role explícito mantido (alertdialog) mesmo construído sobre Dialog primitive
    expect(dialog).toHaveAttribute("role", "alertdialog");
  });

  it("contrato role=alertdialog: Esc fecha o modal, NÃO confirma cancelamento e mantém role=alertdialog até desmontar", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();
    render(<Harness onConfirm={onConfirm} />);

    const dialog = await screen.findByRole("alertdialog");
    // Pré-condição: role permanece alertdialog enquanto aberto
    expect(dialog).toHaveAttribute("role", "alertdialog");
    expect(dialog).toHaveAttribute("aria-modal", "true");

    await user.keyboard("{Escape}");

    // Contrato: Esc fecha o alertdialog (consistente com Dialog primitive subjacente)…
    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
    // …mas NUNCA confirma o cancelamento da exportação por Esc.
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("Esc no alertdialog devolve o foco ao trigger que o abriu (contrato role=alertdialog)", async () => {
    function EscRestoreHarness({ onConfirm }: { onConfirm: () => void }) {
      const [open, setOpen] = useState(false);
      const triggerRef = useRef<HTMLButtonElement | null>(null);
      return (
        <div>
          <button
            ref={triggerRef}
            type="button"
            data-testid="esc-trigger"
            onClick={() => setOpen(true)}
          >
            abrir cancelamento
          </button>
          <CancelExportDialog
            open={open}
            onOpenChange={setOpen}
            onConfirm={onConfirm}
            busy
            progress={{ step: "Serializando", pct: 10 }}
            triggerRef={triggerRef}
          />
        </div>
      );
    }

    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(<EscRestoreHarness onConfirm={onConfirm} />);

    const trigger = screen.getByTestId("esc-trigger") as HTMLButtonElement;
    await user.click(trigger);

    const dialog = await screen.findByRole("alertdialog");
    expect(dialog).toHaveAttribute("role", "alertdialog");
    expect(dialog).toHaveAttribute("aria-modal", "true");
    // Pré-condição: o foco entrou no modal (saiu do trigger)
    await waitFor(() => {
      expect(dialog.contains(document.activeElement)).toBe(true);
      expect(document.activeElement).not.toBe(trigger);
    });

    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
    // Contrato: Esc devolve o foco ao trigger que abriu o alertdialog
    await waitFor(() => {
      expect(document.activeElement).toBe(trigger);
    });
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("contrato de IDs ARIA: aria-labelledby/describedby apontam para h2/p únicos e dentro do alertdialog", async () => {
    render(<Harness onConfirm={vi.fn()} />);

    const dialog = await screen.findByRole("alertdialog");

    const labelledBy = dialog.getAttribute("aria-labelledby");
    const describedBy = dialog.getAttribute("aria-describedby");
    expect(labelledBy).toBeTruthy();
    expect(describedBy).toBeTruthy();
    expect(labelledBy).not.toBe(describedBy);

    // IDs únicos no documento
    expect(document.querySelectorAll(`#${CSS.escape(labelledBy!)}`).length).toBe(1);
    expect(document.querySelectorAll(`#${CSS.escape(describedBy!)}`).length).toBe(1);

    const titleEl = document.getElementById(labelledBy!);
    const descEl = document.getElementById(describedBy!);
    expect(titleEl).not.toBeNull();
    expect(descEl).not.toBeNull();

    // Elementos referenciados pertencem ao próprio alertdialog
    expect(dialog.contains(titleEl!)).toBe(true);
    expect(dialog.contains(descEl!)).toBe(true);

    // Tags semânticas do shadcn DialogTitle/DialogDescription
    expect(titleEl!.tagName).toBe("H2");
    expect(descEl!.tagName).toBe("P");

    expect(titleEl!.textContent).toMatch(/cancelar exportação\?/i);
    expect(descEl!.textContent).toMatch(/exportação em andamento será interrompida/i);
  });

  it("contrato de overlay: pointerdown no OVERLAY do Radix fecha o alertdialog SEM disparar onConfirm", async () => {
    const onConfirm = vi.fn();
    render(<Harness onConfirm={onConfirm} />);

    await screen.findByRole("alertdialog");

    // Radix registra o listener pointer-down-outside via setTimeout(0)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });

    // Localiza o overlay portalado (fixed inset-0 com data-state="open")
    const candidates = Array.from(
      document.querySelectorAll<HTMLElement>('[data-state="open"]'),
    );
    const overlay = candidates.find((el) => el.className.includes("fixed inset-0"));
    expect(overlay).toBeTruthy();

    await act(async () => {
      overlay!.dispatchEvent(
        new Event("pointerdown", { bubbles: true, cancelable: true }),
      );
    });

    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
    expect(screen.getByTestId("open-state").textContent).toBe("closed");
    // Contrato crítico: clicar fora NÃO confirma o cancelamento da exportação
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("contrato negativo: pointerdown DENTRO do alertdialog NÃO fecha nem dispara onConfirm", async () => {
    const onConfirm = vi.fn();
    render(<Harness onConfirm={onConfirm} />);

    const dialog = await screen.findByRole("alertdialog");

    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });

    // pointerdown no título (dentro do modal) — não deve fechar nem confirmar
    const title = within(dialog).getByText(/cancelar exportação\?/i);
    await act(async () => {
      title.dispatchEvent(
        new Event("pointerdown", { bubbles: true, cancelable: true }),
      );
    });

    await act(async () => {
      await new Promise((r) => setTimeout(r, 20));
    });
    expect(screen.queryByRole("alertdialog")).toBeInTheDocument();
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("clicar no botão X (sr-only 'Close') fecha o alertdialog e NUNCA chama onConfirm", async () => {
    const onConfirm = vi.fn();
    const user = userEvent.setup();
    render(<Harness onConfirm={onConfirm} />);

    const dialog = await screen.findByRole("alertdialog");
    const closeBtn = within(dialog).getByRole("button", { name: /close/i });

    await user.click(closeBtn);

    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
    expect(screen.getByTestId("open-state").textContent).toBe("closed");
    // Contrato crítico: o X é dismiss, não confirma o cancelamento da exportação
    expect(onConfirm).not.toHaveBeenCalled();
  });

  describe("contrato negativo do X · NUNCA dispara onConfirm em nenhum cenário", () => {
    /**
     * Harness com inputs/estado externos ao redor do dialog para simular um
     * formulário real "preenchido" enquanto a exportação roda. O contrato:
     * mudanças em qualquer estado/inputs do app NÃO devem fazer o X virar
     * confirm — X é sempre dismiss puro.
     */
    function StatefulHarness({
      onConfirm,
      initiallyOpen = true,
    }: {
      onConfirm: () => void;
      initiallyOpen?: boolean;
    }) {
      const [open, setOpen] = useState(initiallyOpen);
      const [nome, setNome] = useState("");
      const [obs, setObs] = useState("");
      const [pct, setPct] = useState(10);
      return (
        <div>
          <label>
            Nome do arquivo
            <input
              data-testid="filename"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
            />
          </label>
          <label>
            Observações
            <textarea
              data-testid="notes"
              value={obs}
              onChange={(e) => setObs(e.target.value)}
            />
          </label>
          <button
            type="button"
            data-testid="bump-progress"
            onClick={() => setPct((p) => Math.min(100, p + 25))}
          >
            avançar progresso
          </button>
          <button type="button" onClick={() => setOpen(true)}>
            reabrir
          </button>
          <span data-testid="open-state">{open ? "open" : "closed"}</span>
          <CancelExportDialog
            open={open}
            onOpenChange={setOpen}
            onConfirm={onConfirm}
            busy
            progress={{ step: "Serializando", pct }}
          />
        </div>
      );
    }

    it("clique no X com inputs preenchidos e progresso atualizado: fecha, NÃO chama onConfirm", async () => {
      const onConfirm = vi.fn();
      const user = userEvent.setup();
      render(<StatefulHarness onConfirm={onConfirm} initiallyOpen={false} />);

      // Preenche estado externo ANTES de abrir (Radix bloqueia pointer-events fora)
      await user.type(screen.getByTestId("filename"), "relatorio-anual.csv");
      await user.type(screen.getByTestId("notes"), "exportar com cabeçalhos");
      expect((screen.getByTestId("filename") as HTMLInputElement).value).toBe(
        "relatorio-anual.csv",
      );

      // Abre o alertdialog
      await user.click(screen.getByRole("button", { name: /reabrir/i }));
      const dialog = await screen.findByRole("alertdialog");
      const closeBtn = within(dialog).getByRole("button", { name: /close/i });

      await user.click(closeBtn);

      await waitFor(() => {
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
      });
      expect(screen.getByTestId("open-state").textContent).toBe("closed");
      expect(onConfirm).not.toHaveBeenCalled();
    });

    it("ativação do X via teclado (Enter) NÃO dispara onConfirm", async () => {
      const onConfirm = vi.fn();
      const user = userEvent.setup();
      render(<StatefulHarness onConfirm={onConfirm} />);

      const dialog = await screen.findByRole("alertdialog");
      const closeBtn = within(dialog).getByRole("button", { name: /close/i });
      closeBtn.focus();
      expect(document.activeElement).toBe(closeBtn);

      await user.keyboard("{Enter}");

      await waitFor(() => {
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
      });
      expect(onConfirm).not.toHaveBeenCalled();
    });

    it("ativação do X via teclado (Space) NÃO dispara onConfirm", async () => {
      const onConfirm = vi.fn();
      const user = userEvent.setup();
      render(<StatefulHarness onConfirm={onConfirm} />);

      const dialog = await screen.findByRole("alertdialog");
      const closeBtn = within(dialog).getByRole("button", { name: /close/i });
      closeBtn.focus();

      await user.keyboard(" ");

      await waitFor(() => {
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
      });
      expect(onConfirm).not.toHaveBeenCalled();
    });

    it("progresso mudando antes do clique no X: fecha sem chamar onConfirm", async () => {
      const onConfirm = vi.fn();
      const user = userEvent.setup();
      render(<StatefulHarness onConfirm={onConfirm} />);

      // O botão "avançar progresso" está FORA do modal — em jsdom o
      // pointer-down-outside do Radix fecharia o modal antes do clique X.
      // Disparamos a atualização de estado por API React (não simula clique fora).
      const bump = screen.getByTestId("bump-progress");
      await act(async () => {
        bump.click();
      });
      // Se o pointer-outside fechou o modal, o teste de X não se aplica:
      // reabrimos explicitamente para garantir a pré-condição.
      if (!screen.queryByRole("alertdialog")) {
        await user.click(screen.getByRole("button", { name: /reabrir/i }));
      }

      const dialog = await screen.findByRole("alertdialog");
      // A descrição reflete o novo progresso
      expect(dialog.textContent).toMatch(/progresso atual: 35%/i);

      const closeBtn = within(dialog).getByRole("button", { name: /close/i });
      await user.click(closeBtn);

      await waitFor(() => {
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
      });
      expect(onConfirm).not.toHaveBeenCalled();
    });

    it("reabrir após fechar pelo X e fechar de novo pelo X: onConfirm NUNCA é chamado", async () => {
      const onConfirm = vi.fn();
      const user = userEvent.setup();
      render(<StatefulHarness onConfirm={onConfirm} />);

      // 1ª rodada: fecha pelo X
      let dialog = await screen.findByRole("alertdialog");
      await user.click(within(dialog).getByRole("button", { name: /close/i }));
      await waitFor(() => {
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
      });

      // Reabre
      await user.click(screen.getByRole("button", { name: /reabrir/i }));
      dialog = await screen.findByRole("alertdialog");

      // 2ª rodada: fecha pelo X novamente
      await user.click(within(dialog).getByRole("button", { name: /close/i }));
      await waitFor(() => {
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
      });

      // Contrato: nenhuma das fechadas via X confirmou
      expect(onConfirm).not.toHaveBeenCalled();
    });
  });
});




