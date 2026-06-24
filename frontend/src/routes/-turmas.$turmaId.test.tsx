import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, act, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ExportMenu } from "./turmas.$turmaId";

// jsdom doesn't implement these — the CSV path uses them.
beforeEach(() => {
  URL.createObjectURL = vi.fn(() => "blob:mock");
  URL.revokeObjectURL = vi.fn();
  HTMLAnchorElement.prototype.click = vi.fn();
});

function renderMenu() {
  return render(
    <ExportMenu
      baseName="turma-test"
      sheetName="Turma"
      headers={["col"]}
      pageRows={[["x"]]}
      // big enough to keep the CSV serializer busy for a few ticks
      allRows={Array.from({ length: 2000 }, (_, i) => [i])}
    />,
  );
}

describe("turmas.$turmaId · ExportMenu (integração)", () => {
  it("não exibe o AlertDialog enquanto não há exportação em andamento", async () => {
    const user = userEvent.setup();
    renderMenu();

    // Sem exportação: nenhum dialog presente
    expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

    // Esc sem exportação ativa NÃO deve abrir o dialog
    await user.keyboard("{Escape}");
    expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();

    // O botão visível é "Exportar", não "Exportando…"
    expect(screen.getByRole("button", { name: /^exportar$/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /exportando/i })).not.toBeInTheDocument();
  });

  it("Esc abre o AlertDialog apenas durante a exportação; clicar fora fecha sem cancelar", async () => {
    const user = userEvent.setup();
    renderMenu();

    // Inicia exportação CSV (formato mais leve, sem dynamic import de xlsx)
    await user.click(screen.getByRole("button", { name: /^exportar$/i }));
    const items = await screen.findAllByRole("menuitem", {
      name: /todos os filtrados/i,
    });
    // First match is the CSV variant (lighter than the xlsx dynamic import)
    await user.click(items[0]);

    // Estado "exportando" deve aparecer
    await screen.findByRole("button", { name: /exportando/i });

    // Pressionar Esc agora ABRE o dialog de confirmação
    fireEvent.keyDown(window, { key: "Escape" });
    const dialog = await screen.findByRole("alertdialog", {
      name: /cancelar exportação\?/i,
    });
    expect(dialog).toBeInTheDocument();

    // Aguardar Radix registrar o listener de pointerdown-outside (setTimeout(0))
    await act(async () => {
      await new Promise((r) => setTimeout(r, 10));
    });

    // Clicar fora do conteúdo do dialog
    await act(async () => {
      document.body.dispatchEvent(
        new Event("pointerdown", { bubbles: true, cancelable: true }),
      );
    });

    // Dialog fechou
    await waitFor(() => {
      expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });

    // …e a exportação NÃO foi cancelada: deve concluir gerando o blob CSV.
    await waitFor(
      () => {
        expect(URL.createObjectURL).toHaveBeenCalled();
      },
      { timeout: 5000 },
    );

    // Após concluir, o botão "Exportar" volta a aparecer (estado ocioso)
    await screen.findByRole("button", { name: /^exportar$/i });
  });
});
