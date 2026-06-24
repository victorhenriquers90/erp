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

const html = `<!DOCTYPE html>
<html lang="pt-BR">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Regente</title>
    ${css ? `<link rel="stylesheet" crossorigin href="/assets/${css}" />` : ""}
  </head>
  <body>
    <script type="module" crossorigin src="/assets/${js}"></script>
  </body>
</html>`;

fs.writeFileSync(path.join(clientDir, "index.html"), html);
console.log(`postbuild: gerou dist/client/index.html (js=${js}, css=${css ?? "n/a"})`);
