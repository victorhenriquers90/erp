import fs from "fs";
import path from "path";

const clientDir = "dist/client";
const assetsDir = path.join(clientDir, "assets");

const files = fs.readdirSync(assetsDir);
const css = files.find((f) => f.startsWith("styles-") && f.endsWith(".css"));
const js = files.find((f) => f.startsWith("index-") && f.endsWith(".js"));

if (!js) {
  console.error("postbuild: não encontrou index-*.js em dist/client/assets/");
  process.exit(1);
}

// Replicates the minimal shell that TanStack Start's SSR would produce,
// so hydrateRoot(document, ...) finds a compatible DOM tree to attach to.
const html = `<!DOCTYPE html>
<html lang="pt-BR">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Regente — Caderno do professor</title>
    <meta name="description" content="Sistema para professores: lançamento de notas, frequência, comparativos e tutoria de alunos." />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap" />
    ${css ? `<link rel="stylesheet" href="/assets/${css}" />` : ""}
  </head>
  <body>
    <div id="app-root"></div>
    <script>
      window.addEventListener('error', function(e) {
        document.getElementById('app-root').innerHTML =
          '<div style="padding:2rem;font-family:monospace;color:red"><b>JS Error:</b><br>' +
          (e.message || e) + '<br>' + (e.filename || '') + ':' + (e.lineno || '') + '</div>';
      });
      window.addEventListener('unhandledrejection', function(e) {
        document.getElementById('app-root').innerHTML =
          '<div style="padding:2rem;font-family:monospace;color:red"><b>Unhandled Promise:</b><br>' +
          (e.reason || e) + '</div>';
      });
    </script>
    <script type="module" src="/assets/${js}"></script>
  </body>
</html>`;

fs.writeFileSync(path.join(clientDir, "index.html"), html);
console.log(`postbuild: gerou dist/client/index.html (js=${js}, css=${css ?? "n/a"})`);
