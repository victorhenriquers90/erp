import { describe, it, expect } from "vitest";
import { render, screen, within, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createRouter, createMemoryHistory, RouterProvider, createRootRoute, createRoute, Outlet } from "@tanstack/react-router";
import { StoreProvider } from "@/lib/store";
import { Route as TurmasIndexFileRoute } from "./turmas.index";
import { assertDialogIdWiring } from "./dialogs-a11y.test";


function renderTurmasIndex() {
  const rootRoute = createRootRoute({ component: () => <Outlet /> });
  const indexRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/",
    component: TurmasIndexFileRoute.options.component!,
  });
  // Stub for the typed Link target used inside cards (not reached in empty state).
  const turmaRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/turmas/$turmaId",
    component: () => <div />,
  });
  const router = createRouter({
    routeTree: rootRoute.addChildren([indexRoute, turmaRoute]),
    history: createMemoryHistory({ initialEntries: ["/"] }),
  });
  return render(
    <StoreProvider>
      <RouterProvider router={router as never} />
    </StoreProvider>,
  );
}

describe("turmas.index · acessibilidade do Dialog 'Nova turma'", () => {
  it("não renderiza o dialog antes de abrir", async () => {
    renderTurmasIndex();
    await screen.findByRole("button", { name: /nova turma/i });
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("expõe role=dialog com aria-labelledby/aria-describedby corretos", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);

    const dialog = await screen.findByRole("dialog");
    expect(dialog).toBeInTheDocument();
    expect(dialog).toHaveAttribute("aria-modal", "true");


    // aria-labelledby aponta para o DialogTitle visível
    const labelledBy = dialog.getAttribute("aria-labelledby");
    expect(labelledBy).toBeTruthy();
    const titleEl = document.getElementById(labelledBy!);
    expect(titleEl).not.toBeNull();
    expect(titleEl).toHaveTextContent(/nova turma/i);

    // aria-describedby aponta para a DialogDescription visível
    const describedBy = dialog.getAttribute("aria-describedby");
    expect(describedBy).toBeTruthy();
    const descEl = document.getElementById(describedBy!);
    expect(descEl).not.toBeNull();
    expect(descEl).toHaveTextContent(/dados básicos da turma/i);
  });

  it("garante que os controles dentro do dialog têm nomes acessíveis", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");

    const scoped = within(dialog);
    expect(scoped.getByLabelText(/^nome$/i)).toBeInTheDocument();
    expect(scoped.getByLabelText(/disciplina/i)).toBeInTheDocument();
    expect(scoped.getByLabelText(/ano letivo/i)).toBeInTheDocument();
    expect(scoped.getByRole("button", { name: /criar turma/i })).toBeInTheDocument();
    // Botão de fechar (ícone) deve ter nome acessível
    expect(scoped.getByRole("button", { name: /close/i })).toBeInTheDocument();
  });

  it("devolve o foco ao trigger 'Nova turma' ao fechar via Esc", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    await screen.findByRole("dialog");

    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
    await waitFor(() => {
      expect(document.activeElement).toBe(trigger);
    });
  });

  it("move o foco para dentro do modal ao abrir e prende a navegação por teclado", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");

    // Foco inicial entra no modal (não permanece no trigger nem no body)
    await waitFor(() => {
      expect(document.activeElement).not.toBe(document.body);
      expect(document.activeElement).not.toBe(trigger);
      expect(dialog.contains(document.activeElement)).toBe(true);
    });

    // Teclado funciona imediatamente — foco preso (focus trap) dentro do modal
    for (let i = 0; i < 6; i++) {
      await user.tab();
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
    for (let i = 0; i < 6; i++) {
      await user.tab({ shift: true });
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
  });

  it("fecha ao clicar fora — comportamento consistente com role=dialog (não-alertdialog)", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    await screen.findByRole("dialog");

    // Radix registra o listener pointer-down-outside via setTimeout(0)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });
    await act(async () => {
      trigger.dispatchEvent(new Event("pointerdown", { bubbles: true, cancelable: true }));
    });

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("Esc fecha o Dialog 'Nova turma' (role=dialog)", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");
    expect(dialog).toHaveAttribute("aria-modal", "true");

    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("aria-labelledby/describedby apontam para h2/p únicos dentro do dialog (contrato de IDs)", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");

    assertDialogIdWiring(dialog, {
      titleMatch: /nova turma/i,
      descriptionMatch: /dados básicos da turma/i,
    });
  });

  it("focus trap: sentinelas externas nunca recebem foco enquanto o dialog 'Nova turma' está aberto", async () => {
    const user = userEvent.setup();
    const { container } = renderTurmasIndex();
    const before = document.createElement("button");
    before.type = "button";
    before.dataset.testid = "outside-before";
    before.textContent = "fora-antes";
    container.parentElement!.insertBefore(before, container);
    const after = document.createElement("button");
    after.type = "button";
    after.dataset.testid = "outside-after";
    after.textContent = "fora-depois";
    container.parentElement!.appendChild(after);

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");

    await waitFor(() => {
      expect(dialog.contains(document.activeElement)).toBe(true);
    });

    for (let i = 0; i < 12; i++) {
      await user.tab();
      expect(dialog.contains(document.activeElement)).toBe(true);
      expect(document.activeElement).not.toBe(before);
      expect(document.activeElement).not.toBe(after);
      expect(document.activeElement).not.toBe(document.body);
    }
    for (let i = 0; i < 12; i++) {
      await user.tab({ shift: true });
      expect(dialog.contains(document.activeElement)).toBe(true);
      expect(document.activeElement).not.toBe(before);
      expect(document.activeElement).not.toBe(after);
      expect(document.activeElement).not.toBe(document.body);
    }
  });

  it("clicar no botão X (sr-only 'Close') fecha o dialog 'Nova turma'", async () => {
    const user = userEvent.setup();
    renderTurmasIndex();

    const trigger = await screen.findByRole("button", { name: /nova turma/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");
    const closeBtn = within(dialog).getByRole("button", { name: /close/i });

    await user.click(closeBtn);

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });
});




