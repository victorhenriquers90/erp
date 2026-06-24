import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  Outlet,
  Link,
  createRootRouteWithContext,
  useRouter,
  useNavigate,
  useLocation,
  HeadContent,
  Scripts,
} from "@tanstack/react-router";
import { useEffect, type ReactNode } from "react";

import appCss from "../styles.css?url";
import { reportLovableError } from "../lib/lovable-error-reporting";
import { StoreProvider, useStore } from "@/lib/store";
import {
  SidebarProvider,
  SidebarInset,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/app-sidebar";
import { Button } from "@/components/ui/button";
import { LogOut } from "lucide-react";

function NotFoundComponent() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="max-w-md text-center">
        <h1 className="text-7xl font-bold text-foreground">404</h1>
        <h2 className="mt-4 text-xl font-semibold text-foreground">Página não encontrada</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Esta página não existe ou foi movida.
        </p>
        <div className="mt-6">
          <Link
            to="/"
            className="inline-flex items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          >
            Voltar ao início
          </Link>
        </div>
      </div>
    </div>
  );
}

function ErrorComponent({ error, reset }: { error: Error; reset: () => void }) {
  console.error(error);
  const router = useRouter();
  useEffect(() => {
    reportLovableError(error, { boundary: "tanstack_root_error_component" });
  }, [error]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="max-w-md text-center">
        <h1 className="text-xl font-semibold tracking-tight text-foreground">
          Algo deu errado
        </h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Ocorreu um erro inesperado. Tente recarregar a página.
        </p>
        <div className="mt-6 flex flex-wrap justify-center gap-2">
          <button
            onClick={() => { router.invalidate(); reset(); }}
            className="inline-flex items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          >
            Tentar novamente
          </button>
          <a
            href="/"
            className="inline-flex items-center justify-center rounded-md border border-input bg-background px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-accent"
          >
            Voltar ao início
          </a>
        </div>
      </div>
    </div>
  );
}

export const Route = createRootRouteWithContext<{ queryClient: QueryClient }>()({
  head: () => ({
    meta: [
      { charSet: "utf-8" },
      { name: "viewport", content: "width=device-width, initial-scale=1" },
      { title: "Regente — Caderno do professor" },
      { name: "description", content: "Sistema para professores: lançamento de notas, frequência, comparativos e tutoria de alunos." },
      { property: "og:title", content: "Regente — Caderno do professor" },
      { property: "og:description", content: "Lançamento de notas, frequência, comparativos e tutoria — tudo em um só caderno digital." },
      { property: "og:type", content: "website" },
      { name: "twitter:title", content: "Regente — Caderno do professor" },
      { name: "twitter:card", content: "summary_large_image" },
    ],
    links: [
      { rel: "preconnect", href: "https://fonts.googleapis.com" },
      { rel: "preconnect", href: "https://fonts.gstatic.com", crossOrigin: "anonymous" },
      {
        rel: "stylesheet",
        href: "https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap",
      },
      { rel: "stylesheet", href: appCss },
    ],
  }),
  shellComponent: RootShell,
  component: RootComponent,
  notFoundComponent: NotFoundComponent,
  errorComponent: ErrorComponent,
});

function RootShell({ children }: { children: ReactNode }) {
  return (
    <html lang="pt-BR">
      <head>
        <HeadContent />
      </head>
      <body>
        {children}
        <Scripts />
      </body>
    </html>
  );
}

function RootComponent() {
  const { queryClient } = Route.useRouteContext();
  return (
    <QueryClientProvider client={queryClient}>
      <StoreProvider>
        <AppContent />
      </StoreProvider>
    </QueryClientProvider>
  );
}

function AppContent() {
  const { user, loading, signOut } = useStore();
  const navigate = useNavigate();
  const location = useLocation();
  const isLoginPage = location.pathname === "/login";

  useEffect(() => {
    if (!loading && !user && !isLoginPage) {
      navigate({ to: "/login" });
    }
    if (!loading && user && isLoginPage) {
      navigate({ to: "/" });
    }
  }, [user, loading, isLoginPage, navigate]);

  // Página de login: layout standalone
  if (isLoginPage) {
    return <Outlet />;
  }

  // Carregando sessão ou dados
  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <p className="text-sm text-muted-foreground animate-pulse">Carregando…</p>
      </div>
    );
  }

  // Não autenticado: não renderiza nada (o useEffect redireciona)
  if (!user) return null;

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset className="paper-bg">
        <header className="sticky top-0 z-10 flex h-14 items-center gap-3 border-b border-border/70 bg-background/70 px-5 backdrop-blur-xl">
          <SidebarTrigger />
          <div className="h-4 w-px bg-border" />
          <span className="eyebrow">Caderno do professor</span>
          <div className="ml-auto flex items-center gap-3">
            <span className="hidden text-xs text-muted-foreground sm:block truncate max-w-[200px]">
              {user.email}
            </span>
            <div className="hidden h-3.5 w-px bg-border sm:block" />
            <div className="hidden items-center gap-2 text-xs text-muted-foreground sm:flex">
              <span className="inline-block h-1.5 w-1.5 rounded-full bg-gold" />
              <span className="font-mono uppercase tracking-wider">edição diária</span>
            </div>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-muted-foreground hover:text-foreground"
              onClick={signOut}
              title="Sair"
            >
              <LogOut className="h-3.5 w-3.5" />
            </Button>
          </div>
        </header>
        <main className="flex-1">
          <Outlet />
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}
