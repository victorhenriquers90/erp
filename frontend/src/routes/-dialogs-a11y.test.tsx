import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, within, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import {
  createRouter,
  createMemoryHistory,
  RouterProvider,
  createRootRoute,
  createRoute,
  Outlet,
} from "@tanstack/react-router";
import { StoreProvider } from "@/lib/store";
import { NotasTab } from "./turmas.$turmaId";
import { Route as TutoriaFileRoute } from "./tutoria";

const STORAGE_KEY = "regente_state_v1";

function seedStateForTutoria() {
  const turmaId = crypto.randomUUID();
  const alunoId = crypto.randomUUID();
  window.localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify({
      turmas: [
        { id: turmaId, nome: "1º Ano X", disciplina: "Mat", ano: "2025", createdAt: 0 },
      ],
      alunos: [{ id: alunoId, turmaId, nome: "Aluno Teste", matricula: "1", createdAt: 0 }],
      avaliacoes: [],
      notas: [],
      frequencias: [],
      tutorias: [],
    }),
  );
}

function renderTutoriaRoute() {
  const rootRoute = createRootRoute({ component: () => <Outlet /> });
  const tutoriaRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/tutoria",
    component: TutoriaFileRoute.options.component!,
  });
  const router = createRouter({
    routeTree: rootRoute.addChildren([tutoriaRoute]),
    history: createMemoryHistory({ initialEntries: ["/tutoria"] }),
  });
  return render(
    <StoreProvider>
      <RouterProvider router={router as never} />
    </StoreProvider>,
  );
}

async function assertDialogA11y(opts: {
  titleMatch: RegExp;
  descriptionMatch: RegExp;
}) {
  const dialog = await screen.findByRole("dialog");
  expect(dialog).toBeInTheDocument();
  // Modal real → expõe aria-modal="true" (Radix Dialog define isso)
  expect(dialog).toHaveAttribute("aria-modal", "true");

  assertDialogIdWiring(dialog, opts);

  // Botão de fechar (icon-only) deve ter nome acessível
  expect(within(dialog).getByRole("button", { name: /close/i })).toBeInTheDocument();
  return dialog;
}

/**
 * Valida o contrato de IDs ARIA do dialog:
 *  - aria-labelledby e aria-describedby existem e são distintos
 *  - apontam para IDs únicos no documento (sem duplicação)
 *  - os elementos referenciados são descendentes do próprio dialog
 *  - têm a tag semântica correta (h2 para o título, p para a descrição) — o
 *    contrato padrão de DialogTitle/DialogDescription do shadcn/Radix
 *  - o texto bate com o esperado
 */
function assertDialogIdWiring(
  dialog: HTMLElement,
  opts: { titleMatch: RegExp; descriptionMatch: RegExp },
) {
  const labelledBy = dialog.getAttribute("aria-labelledby");
  const describedBy = dialog.getAttribute("aria-describedby");
  expect(labelledBy, "aria-labelledby ausente").toBeTruthy();
  expect(describedBy, "aria-describedby ausente").toBeTruthy();
  expect(labelledBy).not.toBe(describedBy);

  // IDs únicos no documento (não duplicados)
  expect(document.querySelectorAll(`#${CSS.escape(labelledBy!)}`).length).toBe(1);
  expect(document.querySelectorAll(`#${CSS.escape(describedBy!)}`).length).toBe(1);

  const titleEl = document.getElementById(labelledBy!);
  const descEl = document.getElementById(describedBy!);
  expect(titleEl).not.toBeNull();
  expect(descEl).not.toBeNull();

  // Referenciados pertencem ao próprio dialog (escopo correto)
  expect(dialog.contains(titleEl!)).toBe(true);
  expect(dialog.contains(descEl!)).toBe(true);

  // Tags semânticas do shadcn DialogTitle/DialogDescription
  expect(titleEl!.tagName).toBe("H2");
  expect(descEl!.tagName).toBe("P");

  expect(titleEl).toHaveTextContent(opts.titleMatch);
  expect(descEl).toHaveTextContent(opts.descriptionMatch);
}

beforeEach(() => {
  window.localStorage.clear();
});

describe("dialogs do projeto · acessibilidade (role e atributos aria)", () => {
  it("Dialog 'Nova avaliação' (turmas.$turmaId) tem role=dialog + aria-labelledby/describedby", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    await assertDialogA11y({
      titleMatch: /nova avaliação/i,
      descriptionMatch: /título, peso e data/i,
    });
  });

  it("Dialog 'Nova tutoria' (tutoria) tem role=dialog + aria-labelledby/describedby", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    await user.click(trigger);
    await assertDialogA11y({
      titleMatch: /nova tutoria/i,
      descriptionMatch: /sessão de tutoria/i,
    });
  });

  it("Dialog 'Nova avaliação' devolve o foco ao trigger ao fechar via Esc", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    const trigger = screen.getByRole("button", { name: /nova avaliação/i });
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

  it("Dialog 'Nova tutoria' devolve o foco ao trigger ao fechar via Esc", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
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

  it("Dialog 'Nova avaliação' move o foco para dentro do modal ao abrir e prende a navegação por teclado", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    const dialog = await screen.findByRole("dialog");

    // Foco inicial vai para dentro do modal (Radix autofoca primeiro focável)
    await waitFor(() => {
      expect(document.activeElement).not.toBe(document.body);
      expect(dialog.contains(document.activeElement)).toBe(true);
    });

    // Teclado funciona imediatamente: Tab / Shift+Tab permanecem presos no modal
    for (let i = 0; i < 6; i++) {
      await user.tab();
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
    for (let i = 0; i < 6; i++) {
      await user.tab({ shift: true });
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
  });

  it("Dialog 'Nova tutoria' move o foco para dentro do modal ao abrir e prende a navegação por teclado", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");

    await waitFor(() => {
      expect(document.activeElement).not.toBe(document.body);
      expect(dialog.contains(document.activeElement)).toBe(true);
    });

    for (let i = 0; i < 6; i++) {
      await user.tab();
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
    for (let i = 0; i < 6; i++) {
      await user.tab({ shift: true });
      expect(dialog.contains(document.activeElement)).toBe(true);
    }
  });

  it("Dialog 'Nova avaliação' (role=dialog) fecha ao clicar fora — consistente com não-alertdialog", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    const trigger = screen.getByRole("button", { name: /nova avaliação/i });
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

  it("Dialog 'Nova tutoria' (role=dialog) fecha ao clicar fora — consistente com não-alertdialog", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
    await user.click(trigger);
    await screen.findByRole("dialog");

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
});

describe("Esc por role · dialogs com role=dialog fecham via Esc", () => {
  it("'Nova avaliação' (role=dialog) fecha ao pressionar Esc", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    const dialog = await screen.findByRole("dialog");
    expect(dialog).toHaveAttribute("aria-modal", "true");

    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("'Nova tutoria' (role=dialog) fecha ao pressionar Esc", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");
    expect(dialog).toHaveAttribute("aria-modal", "true");

    await user.keyboard("{Escape}");

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });
});



describe("dialogs · contrato de IDs ARIA (aria-labelledby/aria-describedby)", () => {
  it("'Nova avaliação' aponta aria-labelledby/describedby para o h2/p corretos, únicos e dentro do dialog", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    const dialog = await screen.findByRole("dialog");
    assertDialogIdWiring(dialog, {
      titleMatch: /nova avaliação/i,
      descriptionMatch: /título, peso e data/i,
    });
  });

  it("'Nova tutoria' aponta aria-labelledby/describedby para o h2/p corretos, únicos e dentro do dialog", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");
    assertDialogIdWiring(dialog, {
      titleMatch: /nova tutoria/i,
      descriptionMatch: /sessão de tutoria/i,
    });
  });
});

describe("dialogs · focus trap (Tab/Shift+Tab não escapam para fora do modal)", () => {
  it("'Nova avaliação': sentinelas externas nunca recebem foco enquanto o dialog está aberto", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <button type="button" data-testid="outside-before">fora-antes</button>
        <NotasTab turmaId="qualquer" />
        <button type="button" data-testid="outside-after">fora-depois</button>
      </StoreProvider>,
    );

    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    const dialog = await screen.findByRole("dialog");
    const before = screen.getByTestId("outside-before");
    const after = screen.getByTestId("outside-after");

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

  it("'Nova tutoria': sentinelas externas nunca recebem foco enquanto o dialog está aberto", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    const { container } = renderTutoriaRoute();
    // Insere sentinelas como irmãs da árvore renderizada
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

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
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
});

describe("dialogs role=dialog · clique fora via OVERLAY do Radix fecha o modal", () => {
  async function dispatchPointerDownOutside(target: Element) {
    // Radix registra o listener pointer-down-outside via setTimeout(0)
    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });
    await act(async () => {
      target.dispatchEvent(new Event("pointerdown", { bubbles: true, cancelable: true }));
    });
  }

  function getOverlay(): HTMLElement {
    // Radix renderiza <DialogPrimitive.Overlay> com data-state="open" e fixed inset-0.
    // Não tem role/aria nominal — selecionamos pelo data-state="open" da árvore portalada.
    const candidates = Array.from(
      document.querySelectorAll<HTMLElement>('[data-state="open"]'),
    );
    const overlay = candidates.find((el) => el.className.includes("fixed inset-0"));
    if (!overlay) throw new Error("Overlay do Radix Dialog não encontrado");
    return overlay;
  }

  it("'Nova avaliação' fecha ao pointerdown no OVERLAY (não no trigger)", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    await screen.findByRole("dialog");
    const overlay = getOverlay();

    await dispatchPointerDownOutside(overlay);

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("'Nova tutoria' fecha ao pointerdown no OVERLAY (não no trigger)", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
    await user.click(trigger);
    await screen.findByRole("dialog");
    const overlay = getOverlay();

    await dispatchPointerDownOutside(overlay);

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("clique DENTRO do dialog NÃO fecha o modal (contrato negativo)", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    const dialog = await screen.findByRole("dialog");

    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });
    // pointerdown DENTRO do dialog (no próprio título) — não deve fechar
    const title = within(dialog).getByRole("heading", { name: /nova avaliação/i });
    await act(async () => {
      title.dispatchEvent(new Event("pointerdown", { bubbles: true, cancelable: true }));
    });

    // Mantém-se aberto após o tick
    await act(async () => {
      await new Promise((r) => setTimeout(r, 20));
    });
    expect(screen.queryByRole("dialog")).toBeInTheDocument();
  });
});

describe("dialogs · botão de fechar (X) fecha o modal", () => {
  it("'Nova avaliação': clicar no X (sr-only 'Close') fecha o dialog", async () => {
    const user = userEvent.setup();
    render(
      <StoreProvider>
        <NotasTab turmaId="qualquer" />
      </StoreProvider>,
    );

    await user.click(screen.getByRole("button", { name: /nova avaliação/i }));
    const dialog = await screen.findByRole("dialog");
    const closeBtn = within(dialog).getByRole("button", { name: /close/i });

    await user.click(closeBtn);

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });

  it("'Nova tutoria': clicar no X (sr-only 'Close') fecha o dialog", async () => {
    seedStateForTutoria();
    const user = userEvent.setup();
    renderTutoriaRoute();

    const trigger = await screen.findByRole("button", { name: /nova tutoria/i });
    await user.click(trigger);
    const dialog = await screen.findByRole("dialog");
    const closeBtn = within(dialog).getByRole("button", { name: /close/i });

    await user.click(closeBtn);

    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });
});

export { assertDialogIdWiring };








