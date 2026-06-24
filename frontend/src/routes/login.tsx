import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { supabase } from "@/lib/supabase";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/login")({
  head: () => ({ meta: [{ title: "Entrar — Regente" }] }),
  component: LoginPage,
});

function LoginPage() {
  const navigate = useNavigate();
  const [mode, setMode] = useState<"login" | "register">("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [registered, setRegistered] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      if (mode === "login") {
        const { error } = await supabase.auth.signInWithPassword({ email, password });
        if (error) { setError(error.message); return; }
        navigate({ to: "/" });
      } else {
        const { error } = await supabase.auth.signUp({ email, password });
        if (error) { setError(error.message); return; }
        setRegistered(true);
      }
    } finally {
      setLoading(false);
    }
  }

  if (registered) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background px-4">
        <div className="w-full max-w-sm space-y-4 text-center">
          <h1 className="text-xl font-semibold">Verifique seu e-mail</h1>
          <p className="text-sm text-muted-foreground">
            Enviamos um link de confirmação para <strong>{email}</strong>.
            Após confirmar, volte aqui para entrar.
          </p>
          <Button variant="outline" onClick={() => { setRegistered(false); setMode("login"); }}>
            Ir para login
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-sm space-y-6">
        <div className="text-center">
          <p className="eyebrow mb-1">Caderno do professor</p>
          <h1 className="text-2xl font-bold tracking-tight">Regente</h1>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="email">E-mail</Label>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="password">Senha</Label>
            <Input
              id="password"
              type="password"
              autoComplete={mode === "login" ? "current-password" : "new-password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
            />
          </div>

          {error && (
            <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </p>
          )}

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? "Aguarde…" : mode === "login" ? "Entrar" : "Criar conta"}
          </Button>
        </form>

        <p className="text-center text-sm text-muted-foreground">
          {mode === "login" ? "Não tem conta? " : "Já tem conta? "}
          <button
            type="button"
            className="underline hover:text-foreground"
            onClick={() => { setMode(mode === "login" ? "register" : "login"); setError(""); }}
          >
            {mode === "login" ? "Criar conta" : "Entrar"}
          </button>
        </p>
      </div>
    </div>
  );
}
