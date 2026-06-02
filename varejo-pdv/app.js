const STORAGE_KEY = "varejo-flow-v1";
const SESSION_KEY = "varejo-flow-session-v1";
const CLOUD_SYNC_DELAY = 800;

const planCatalog = {
  starter: {
    id: "starter",
    name: "Starter",
    price: 79,
    products: 150,
    users: 2,
    monthlySales: 800,
    features: ["pdv", "inventory", "cash", "reports", "browserPrint"],
  },
  pro: {
    id: "pro",
    name: "Pro",
    price: 149,
    products: 1500,
    users: 8,
    monthlySales: 6000,
    features: ["pdv", "inventory", "cash", "reports", "barcode", "browserPrint", "multiCompany"],
  },
  scale: {
    id: "scale",
    name: "Rede",
    price: 299,
    products: 10000,
    users: 30,
    monthlySales: 40000,
    features: ["pdv", "inventory", "cash", "reports", "barcode", "browserPrint", "multiCompany", "thermalPrinter", "audit"],
  },
};

const roles = {
  owner: {
    label: "Administrador",
    permissions: ["dashboard", "sell", "products", "inventory", "cash", "reports", "settings", "team", "billing"],
  },
  manager: {
    label: "Gerente",
    permissions: ["dashboard", "sell", "products", "inventory", "cash", "reports", "settings", "team"],
  },
  cashier: {
    label: "Operador de caixa",
    permissions: ["dashboard", "sell", "cash"],
  },
  stock: {
    label: "Estoquista",
    permissions: ["dashboard", "products", "inventory"],
  },
  accountant: {
    label: "Financeiro",
    permissions: ["dashboard", "cash", "reports"],
  },
};

const viewPermissions = {
  dashboard: "dashboard",
  pos: "sell",
  products: "products",
  inventory: "inventory",
  cash: "cash",
  reports: "reports",
  team: "team",
  billing: "billing",
  settings: "settings",
};

const categories = [
  "Mercearia",
  "Bebidas",
  "Padaria",
  "Açougue",
  "Hortifruti",
  "Limpeza",
  "Higiene",
  "Congelados",
  "Outros",
];

const units = ["un", "kg", "g", "l", "ml", "cx", "pct"];
const paymentMethods = ["Dinheiro", "Pix", "Débito", "Crédito", "Voucher"];

const cloud = {
  enabled: false,
  ready: false,
  auth: null,
  db: null,
  user: null,
  syncTimer: null,
  lastSyncAt: null,
  status: "Modo demonstração",
};

const seedProducts = [
  { sku: "MER-001", barcode: "7891000000011", name: "Arroz Tipo 1 5kg", category: "Mercearia", unit: "pct", cost: 18.9, price: 27.9, stock: 42, minStock: 12, supplier: "Distribuidora São Bento", active: true },
  { sku: "MER-002", barcode: "7891000000028", name: "Feijão Carioca 1kg", category: "Mercearia", unit: "pct", cost: 5.2, price: 8.49, stock: 36, minStock: 10, supplier: "Grãos Brasil", active: true },
  { sku: "BEB-001", barcode: "7891000000035", name: "Leite Integral 1l", category: "Bebidas", unit: "un", cost: 3.72, price: 5.99, stock: 24, minStock: 18, supplier: "Laticínios Vale", active: true },
  { sku: "BEB-002", barcode: "7891000000042", name: "Refrigerante Cola 2l", category: "Bebidas", unit: "un", cost: 5.9, price: 9.99, stock: 18, minStock: 8, supplier: "Rota Bebidas", active: true },
  { sku: "PAD-001", barcode: "2000000000016", name: "Pão Francês", category: "Padaria", unit: "kg", cost: 8.5, price: 15.9, stock: 16, minStock: 6, supplier: "Produção própria", active: true },
  { sku: "PAD-002", barcode: "2000000000023", name: "Bolo de Cenoura", category: "Padaria", unit: "un", cost: 10.8, price: 24.9, stock: 7, minStock: 3, supplier: "Produção própria", active: true },
  { sku: "ACO-001", barcode: "3000000000014", name: "Contra Filé", category: "Açougue", unit: "kg", cost: 31.9, price: 48.9, stock: 22.5, minStock: 8, supplier: "Frigorífico Central", active: true },
  { sku: "ACO-002", barcode: "3000000000021", name: "Frango Resfriado", category: "Açougue", unit: "kg", cost: 8.7, price: 13.99, stock: 9.2, minStock: 10, supplier: "Avícola Norte", active: true },
  { sku: "HOR-001", barcode: "4000000000012", name: "Tomate Italiano", category: "Hortifruti", unit: "kg", cost: 4.2, price: 7.99, stock: 14, minStock: 6, supplier: "Horti Serra", active: true },
  { sku: "LIM-001", barcode: "7891000000103", name: "Detergente Neutro 500ml", category: "Limpeza", unit: "un", cost: 1.55, price: 2.99, stock: 54, minStock: 20, supplier: "Casa Limpa", active: true },
];

const state = loadState();
const appSession = loadSession();

const ui = {
  view: "dashboard",
  productQuery: "",
  productCategory: "Todas",
  productStatus: "ativos",
  posQuery: "",
  movementQuery: "",
  movementType: "todos",
  reportMonth: monthKey(new Date()),
  teamQuery: "",
  cart: [],
};

const views = [
  { id: "dashboard", label: "Painel", icon: "layout-dashboard" },
  { id: "pos", label: "PDV", icon: "scan-line" },
  { id: "products", label: "Produtos", icon: "boxes" },
  { id: "inventory", label: "Estoque", icon: "clipboard-list" },
  { id: "cash", label: "Caixa", icon: "banknote" },
  { id: "reports", label: "Relatórios", icon: "chart-no-axes-combined" },
  { id: "team", label: "Equipe", icon: "users" },
  { id: "billing", label: "Planos", icon: "credit-card" },
  { id: "settings", label: "Configuração", icon: "settings" },
];

const viewTitles = {
  dashboard: ["Painel operacional", "Visão geral"],
  pos: ["Ponto de venda", "Venda rápida"],
  products: ["Produtos", "Cadastro e preços"],
  inventory: ["Estoque", "Entrada e saída"],
  cash: ["Caixa", "Movimento financeiro"],
  reports: ["Relatórios", "Vendas e margem"],
  team: ["Equipe e permissões", "Controle de acesso"],
  billing: ["Planos e assinatura", "Comercial"],
  settings: ["Configuração", "Loja e dados"],
};

function loadState() {
  try {
    const stored = JSON.parse(localStorage.getItem(STORAGE_KEY));
    if (stored && stored.products && stored.sales && stored.movements) {
      return ensureStateModel(stored);
    }
  } catch (error) {
    console.warn(error);
  }

  const now = new Date();
  const products = seedProducts.map((product, index) => ({
    id: uid("prd", index),
    createdAt: now.toISOString(),
    updatedAt: now.toISOString(),
    ...product,
  }));

  const movements = products.map((product, index) => ({
    id: uid("mov", index),
    productId: product.id,
    productName: product.name,
    type: "entrada",
    quantity: product.stock,
    unitCost: product.cost,
    totalCost: round(product.stock * product.cost),
    reason: "Estoque inicial",
    note: "",
    date: now.toISOString(),
    stockAfter: product.stock,
  }));

  return ensureStateModel({
    settings: {
      storeName: "Varejo Flow",
      storeDocument: "",
      storePhone: "",
      address: "",
      currency: "BRL",
      receiptWidth: "80mm",
      printerMode: "browser",
    },
    products,
    sales: [],
    movements,
    cash: {
      activeSessionId: null,
      sessions: [],
      transactions: [],
    },
  });
}

function ensureStateModel(nextState) {
  const companyId = nextState.account?.currentCompanyId || "company_demo";
  const now = new Date().toISOString();
  const companyName = nextState.settings?.storeName || "Varejo Flow";

  nextState.account = nextState.account || {};
  nextState.account.currentCompanyId = companyId;
  nextState.account.companies = Array.isArray(nextState.account.companies) && nextState.account.companies.length
    ? nextState.account.companies
    : [{
      id: companyId,
      name: companyName,
      document: nextState.settings?.storeDocument || "",
      phone: nextState.settings?.storePhone || "",
      address: nextState.settings?.address || "",
      planId: "pro",
      subscriptionStatus: "trial",
      trialEndsAt: addDays(now, 14),
      createdAt: now,
    }];
  nextState.account.users = Array.isArray(nextState.account.users) && nextState.account.users.length
    ? nextState.account.users
    : [{
      id: "user_demo_owner",
      companyId,
      name: "Administrador",
      email: "admin@varejoflow.com",
      role: "owner",
      status: "active",
      createdAt: now,
    }, {
      id: "user_demo_cashier",
      companyId,
      name: "Operador Caixa",
      email: "caixa@varejoflow.com",
      role: "cashier",
      status: "active",
      createdAt: now,
    }, {
      id: "user_demo_stock",
      companyId,
      name: "Estoquista",
      email: "estoque@varejoflow.com",
      role: "stock",
      status: "active",
      createdAt: now,
    }];
  nextState.account.audit = Array.isArray(nextState.account.audit) ? nextState.account.audit : [];

  nextState.settings = {
    receiptWidth: "80mm",
    printerMode: "browser",
    ...nextState.settings,
  };
  nextState.products = Array.isArray(nextState.products) ? nextState.products : [];
  nextState.sales = Array.isArray(nextState.sales) ? nextState.sales : [];
  nextState.movements = Array.isArray(nextState.movements) ? nextState.movements : [];
  nextState.cash = nextState.cash || {};
  nextState.cash.sessions = Array.isArray(nextState.cash.sessions) ? nextState.cash.sessions : [];
  nextState.cash.transactions = Array.isArray(nextState.cash.transactions) ? nextState.cash.transactions : [];
  nextState.cash.activeSessionByCompany = nextState.cash.activeSessionByCompany || {};
  if (nextState.cash.activeSessionId && !nextState.cash.activeSessionByCompany[companyId]) {
    nextState.cash.activeSessionByCompany[companyId] = nextState.cash.activeSessionId;
  }

  nextState.products.forEach((item) => item.companyId = item.companyId || companyId);
  nextState.sales.forEach((item) => item.companyId = item.companyId || companyId);
  nextState.movements.forEach((item) => item.companyId = item.companyId || companyId);
  nextState.cash.sessions.forEach((item) => item.companyId = item.companyId || companyId);
  nextState.cash.transactions.forEach((item) => item.companyId = item.companyId || companyId);

  return nextState;
}

function addDays(value, days) {
  const date = new Date(value);
  date.setDate(date.getDate() + days);
  return date.toISOString();
}

function loadSession() {
  try {
    return JSON.parse(localStorage.getItem(SESSION_KEY)) || {};
  } catch {
    return {};
  }
}

function saveSession() {
  localStorage.setItem(SESSION_KEY, JSON.stringify(appSession));
}

function clearSession() {
  Object.keys(appSession).forEach((key) => delete appSession[key]);
  localStorage.removeItem(SESSION_KEY);
}

function saveState() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  scheduleCloudSync();
}

function firebaseConfigAvailable() {
  const config = window.VAREJO_FIREBASE_CONFIG;
  return !!(config && config.apiKey && config.projectId && window.firebase);
}

async function initCloud() {
  if (!firebaseConfigAvailable()) {
    cloud.status = "Modo local/demo";
    return;
  }

  try {
    if (!window.firebase.apps.length) {
      window.firebase.initializeApp(window.VAREJO_FIREBASE_CONFIG);
    }
    cloud.auth = window.firebase.auth();
    cloud.db = window.firebase.firestore();
    cloud.enabled = true;
    cloud.ready = true;
    cloud.status = "Firebase conectado";
    cloud.auth.onAuthStateChanged((firebaseUser) => {
      cloud.user = firebaseUser;
      if (firebaseUser && !currentUser()) {
        const user = ensureFirebaseUser(firebaseUser);
        appSession.userId = user.id;
        state.account.currentCompanyId = user.companyId;
        saveSession();
        saveState();
        render();
      }
    });
  } catch (error) {
    console.warn(error);
    cloud.status = "Firebase indisponível";
  }
}

function scheduleCloudSync() {
  if (!cloud.enabled || !cloud.db || !currentUser()) return;
  clearTimeout(cloud.syncTimer);
  cloud.syncTimer = setTimeout(syncCompanySnapshot, CLOUD_SYNC_DELAY);
}

async function syncCompanySnapshot() {
  if (!cloud.enabled || !cloud.db || !currentUser()) return;
  try {
    const companyId = currentCompanyId();
    const company = currentCompany();
    await cloud.db.collection("companies").doc(companyId).set({
      ...company,
      updatedAt: new Date().toISOString(),
      updatedBy: currentUser()?.id || null,
    }, { merge: true });
    await cloud.db.collection("companies").doc(companyId).collection("snapshots").doc("main").set({
      products: productList(),
      sales: saleList(),
      movements: movementList(),
      cashSessions: cashSessionList(),
      cashTransactions: state.cash.transactions.filter((item) => item.companyId === companyId),
      users: companyUsers(),
      settings: state.settings,
      updatedAt: new Date().toISOString(),
    }, { merge: true });
    cloud.lastSyncAt = new Date().toISOString();
    cloud.status = "Sincronizado";
  } catch (error) {
    console.warn(error);
    cloud.status = "Falha ao sincronizar";
  }
}

async function loadCompanySnapshot(companyId) {
  if (!cloud.enabled || !cloud.db || !companyId) return;
  try {
    const snap = await cloud.db.collection("companies").doc(companyId).collection("snapshots").doc("main").get();
    if (!snap.exists) return;
    const data = snap.data() || {};
    state.products = state.products.filter((item) => item.companyId !== companyId).concat(data.products || []);
    state.sales = state.sales.filter((item) => item.companyId !== companyId).concat(data.sales || []);
    state.movements = state.movements.filter((item) => item.companyId !== companyId).concat(data.movements || []);
    state.cash.sessions = state.cash.sessions.filter((item) => item.companyId !== companyId).concat(data.cashSessions || []);
    state.cash.transactions = state.cash.transactions.filter((item) => item.companyId !== companyId).concat(data.cashTransactions || []);
    if (Array.isArray(data.users) && data.users.length) {
      state.account.users = state.account.users.filter((item) => item.companyId !== companyId).concat(data.users);
    }
    if (data.settings) Object.assign(state.settings, data.settings);
    cloud.status = "Dados carregados";
  } catch (error) {
    console.warn(error);
    cloud.status = "Falha ao carregar nuvem";
  }
}

function uid(prefix, salt = "") {
  return `${prefix}_${Date.now().toString(36)}_${salt}_${Math.random().toString(36).slice(2, 8)}`;
}

function currency(value) {
  return Number(value || 0).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function number(value, digits = 2) {
  return Number(value || 0).toLocaleString("pt-BR", {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });
}

function parseMoney(value) {
  const raw = String(value || "").trim();
  if (!raw) return 0;
  if (raw.includes(",")) {
    return Number(raw.replace(/\./g, "").replace(",", ".").replace(/[^\d.-]/g, "")) || 0;
  }
  return Number(raw.replace(/[^\d.-]/g, "")) || 0;
}

function round(value) {
  return Math.round((Number(value) || 0) * 100) / 100;
}

function monthKey(dateValue) {
  const date = dateValue instanceof Date ? dateValue : new Date(dateValue);
  return date.toISOString().slice(0, 7);
}

function todayKey() {
  return new Date().toISOString().slice(0, 10);
}

function dateTime(value) {
  return new Date(value).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
}

function dateOnly(value) {
  return new Date(value).toLocaleDateString("pt-BR");
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function currentCompanyId() {
  return state.account?.currentCompanyId || state.account?.companies?.[0]?.id || "company_demo";
}

function currentCompany() {
  return state.account.companies.find((company) => company.id === currentCompanyId()) || state.account.companies[0];
}

function currentPlan() {
  return planCatalog[currentCompany()?.planId] || planCatalog.starter;
}

function currentUser() {
  const user = state.account.users.find((item) => item.id === appSession.userId && item.companyId === currentCompanyId() && item.status !== "inactive");
  return user || null;
}

function companyUsers() {
  return state.account.users.filter((user) => user.companyId === currentCompanyId());
}

function accessibleCompanies() {
  const user = currentUser();
  if (!user) return [];
  return state.account.companies.filter((company) => state.account.users.some((item) => item.companyId === company.id && item.email === user.email && item.status !== "inactive"));
}

function can(permission) {
  const user = currentUser();
  if (!user) return false;
  return roles[user.role]?.permissions.includes(permission) || false;
}

function canView(viewId) {
  return can(viewPermissions[viewId] || "dashboard");
}

function hasFeature(feature) {
  return currentPlan().features.includes(feature);
}

function activeSessionId() {
  return state.cash.activeSessionByCompany?.[currentCompanyId()] || null;
}

function setActiveSessionId(value) {
  state.cash.activeSessionByCompany = state.cash.activeSessionByCompany || {};
  if (value) state.cash.activeSessionByCompany[currentCompanyId()] = value;
  else delete state.cash.activeSessionByCompany[currentCompanyId()];
  state.cash.activeSessionId = value || null;
}

function productList() {
  return state.products.filter((product) => product.companyId === currentCompanyId());
}

function saleList() {
  return state.sales.filter((sale) => sale.companyId === currentCompanyId());
}

function movementList() {
  return state.movements.filter((movement) => movement.companyId === currentCompanyId());
}

function cashSessionList() {
  return state.cash.sessions.filter((session) => session.companyId === currentCompanyId());
}

function audit(action, details = {}) {
  const user = currentUser();
  state.account.audit.unshift({
    id: uid("audit"),
    companyId: currentCompanyId(),
    userId: user?.id || "system",
    userName: user?.name || "Sistema",
    action,
    details,
    date: new Date().toISOString(),
  });
  state.account.audit = state.account.audit.slice(0, 300);
}

function productById(id) {
  return productList().find((product) => product.id === id);
}

function activeProducts() {
  return productList().filter((product) => product.active !== false);
}

function saleTotals(cart = ui.cart) {
  const subtotal = cart.reduce((sum, item) => sum + item.qty * item.price, 0);
  const discount = parseMoney(document.querySelector("#sale-discount")?.value || 0);
  const total = Math.max(0, subtotal - discount);
  const cost = cart.reduce((sum, item) => sum + item.qty * item.cost, 0);
  return { subtotal: round(subtotal), discount: round(discount), total: round(total), cost: round(cost), profit: round(total - cost) };
}

function salesInMonth(month) {
  return saleList().filter((sale) => monthKey(sale.date) === month);
}

function salesToday() {
  return saleList().filter((sale) => sale.date.slice(0, 10) === todayKey());
}

function lowStockProducts() {
  return productList()
    .filter((product) => product.active !== false && Number(product.stock) <= Number(product.minStock || 0))
    .sort((a, b) => Number(a.stock) - Number(b.stock));
}

function stockValue() {
  return productList().reduce((sum, product) => sum + Number(product.stock || 0) * Number(product.cost || 0), 0);
}

function currentSession() {
  return cashSessionList().find((session) => session.id === activeSessionId()) || null;
}

function metrics(month = monthKey(new Date())) {
  const monthSales = salesInMonth(month);
  const todaySales = salesToday();
  const revenue = monthSales.reduce((sum, sale) => sum + Number(sale.total || 0), 0);
  const profit = monthSales.reduce((sum, sale) => sum + Number(sale.profit || 0), 0);
  const todayRevenue = todaySales.reduce((sum, sale) => sum + Number(sale.total || 0), 0);
  const averageTicket = monthSales.length ? revenue / monthSales.length : 0;

  return {
    revenue: round(revenue),
    profit: round(profit),
    todayRevenue: round(todayRevenue),
    todayCount: todaySales.length,
    averageTicket: round(averageTicket),
    lowStock: lowStockProducts().length,
    stockValue: round(stockValue()),
  };
}

function renderLogin() {
  const firebaseReady = cloud.enabled;
  document.querySelector("#app").innerHTML = `
    <main class="auth-page">
      <section class="auth-panel">
        <div class="brand auth-brand">
          <div class="brand-mark"><i data-lucide="store"></i></div>
          <div>
            <div class="brand-name">Varejo Flow</div>
            <div class="brand-sub">Sistema vendável para varejo</div>
          </div>
        </div>

        <div class="auth-copy">
          <div class="eyebrow">Acesso seguro</div>
          <h1>Entre na sua empresa</h1>
          <p>Use Firebase Auth quando o projeto estiver configurado. Enquanto isso, os acessos de demonstração funcionam localmente.</p>
        </div>

        <form id="auth-form" class="auth-form">
          <div class="field">
            <label for="auth-email">E-mail</label>
            <input id="auth-email" name="email" type="email" value="admin@varejoflow.com" required />
          </div>
          <div class="field">
            <label for="auth-password">Senha</label>
            <input id="auth-password" name="password" type="password" value="123456" required />
          </div>
          <div class="field">
            <label for="auth-company">Empresa</label>
            <input id="auth-company" name="companyName" value="Varejo Flow" />
          </div>
          <div class="inline-actions">
            <button class="btn primary" type="submit" data-auth-mode="login"><i data-lucide="log-in"></i><span>Entrar</span></button>
            <button class="btn" type="submit" data-auth-mode="signup"><i data-lucide="user-plus"></i><span>Criar conta</span></button>
          </div>
        </form>

        <div class="demo-users">
          <button class="chip" data-action="demo-login" data-email="admin@varejoflow.com"><i data-lucide="shield-check"></i><span>Admin demo</span></button>
          <button class="chip" data-action="demo-login" data-email="caixa@varejoflow.com"><i data-lucide="scan-line"></i><span>Caixa demo</span></button>
          <button class="chip" data-action="demo-login" data-email="estoque@varejoflow.com"><i data-lucide="warehouse"></i><span>Estoque demo</span></button>
        </div>

        <div class="auth-status">
          <span class="pill ${firebaseReady ? "green" : "amber"}">${firebaseReady ? "Firebase ativo" : "Modo local/demo"}</span>
          <span class="muted">Configure firebase-config.js para login e banco em nuvem.</span>
        </div>
      </section>
    </main>
  `;

  refreshIcons();
}

function render() {
  if (!currentUser()) {
    renderLogin();
    return;
  }

  if (!canView(ui.view)) {
    ui.view = "dashboard";
  }

  const [title, eyebrow] = viewTitles[ui.view];
  const activeSession = currentSession();
  const m = metrics();
  const company = currentCompany();
  const user = currentUser();
  const plan = currentPlan();

  document.querySelector("#app").innerHTML = `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">
          <div class="brand-mark"><i data-lucide="store"></i></div>
          <div>
            <div class="brand-name">${escapeHtml(company?.name || state.settings.storeName || "Varejo Flow")}</div>
            <div class="brand-sub">PDV e estoque</div>
          </div>
        </div>

        <div class="company-switch">
          <label for="company-select">Empresa</label>
          <select id="company-select">
            ${accessibleCompanies().map((item) => `<option value="${item.id}" ${item.id === currentCompanyId() ? "selected" : ""}>${escapeHtml(item.name)}</option>`).join("")}
          </select>
          <button class="btn" data-action="open-company-modal"><i data-lucide="building-2"></i><span>Nova empresa</span></button>
        </div>

        <nav class="nav" aria-label="Navegação principal">
          ${views.filter((view) => canView(view.id)).map((view) => `
            <button class="nav-button ${ui.view === view.id ? "active" : ""}" data-view="${view.id}">
              <i data-lucide="${view.icon}"></i>
              <span>${view.label}</span>
            </button>
          `).join("")}
        </nav>

        <div class="sidebar-footer">
          <div class="mini-stat">
            Caixa
            <strong>${activeSession ? "Aberto" : "Fechado"}</strong>
          </div>
          <div class="mini-stat">
            Plano
            <strong>${escapeHtml(plan.name)}</strong>
          </div>
          <div class="mini-stat">
            Vendas hoje
            <strong>${currency(m.todayRevenue)}</strong>
          </div>
          <button class="btn ghost" data-action="logout"><i data-lucide="log-out"></i><span>Sair</span></button>
        </div>
      </aside>

      <main class="main">
        <header class="topbar">
          <div>
            <div class="eyebrow">${eyebrow}</div>
            <h1>${title}</h1>
            <div class="top-meta">
              <span class="pill green">${escapeHtml(roles[user.role]?.label || user.role)}</span>
              <span class="pill blue">${escapeHtml(plan.name)} · ${escapeHtml(company.subscriptionStatus || "trial")}</span>
              <span class="pill">${escapeHtml(cloud.status)}</span>
            </div>
          </div>
          <div class="top-actions">
            <button class="btn primary" data-action="go-pos" title="Nova venda" ${can("sell") ? "" : "disabled"}>
              <i data-lucide="shopping-cart"></i>
              <span>Nova venda</span>
            </button>
            <button class="btn" data-action="open-product-modal" title="Novo produto" ${can("products") ? "" : "disabled"}>
              <i data-lucide="package-plus"></i>
              <span>Novo produto</span>
            </button>
            <button class="icon-btn" data-action="logout" title="Sair">
              <i data-lucide="log-out"></i>
            </button>
          </div>
        </header>

        <section class="content" id="view-root">
          ${renderView()}
        </section>
      </main>
    </div>
  `;

  refreshIcons();
}

function renderView() {
  if (ui.view === "dashboard") return renderDashboard();
  if (ui.view === "pos") return renderPos();
  if (ui.view === "products") return renderProducts();
  if (ui.view === "inventory") return renderInventory();
  if (ui.view === "cash") return renderCash();
  if (ui.view === "reports") return renderReports();
  if (ui.view === "team") return renderTeam();
  if (ui.view === "billing") return renderBilling();
  return renderSettings();
}

function renderDashboard() {
  const m = metrics();
  const recentSales = state.sales.slice(0, 6);
  const lowStock = lowStockProducts().slice(0, 8);
  const byCategory = categoryRevenue(salesInMonth(monthKey(new Date())));

  return `
    <div class="kpi-grid">
      ${kpi("Faturamento mês", currency(m.revenue), "chart-no-axes-combined", `${saleList().length} vendas registradas`)}
      ${kpi("Lucro bruto", currency(m.profit), "badge-dollar-sign", `${m.revenue ? Math.round((m.profit / m.revenue) * 100) : 0}% de margem`)}
      ${kpi("Hoje", currency(m.todayRevenue), "calendar-days", `${m.todayCount} venda(s)`)}
      ${kpi("Ticket médio", currency(m.averageTicket), "receipt-text", "média do mês")}
      ${kpi("Estoque", currency(m.stockValue), "warehouse", `${m.lowStock} item(ns) em alerta`)}
    </div>

    <div class="split band">
      <div>
        <div class="section-head">
          <h2>Vendas recentes</h2>
          <button class="btn ghost" data-view="reports"><i data-lucide="arrow-right"></i><span>Relatórios</span></button>
        </div>
        ${recentSales.length ? salesTable(recentSales) : empty("Nenhuma venda registrada ainda.")}
      </div>

      <div class="tool-panel">
        <div class="section-head">
          <h2>Alertas de estoque</h2>
          <small>${lowStock.length} produto(s)</small>
        </div>
        ${lowStock.length ? lowStockTable(lowStock) : empty("Sem produtos abaixo do mínimo.")}
      </div>
    </div>

    <div class="band">
      <div class="section-head">
        <h2>Faturamento por categoria</h2>
        <small>${new Date().toLocaleDateString("pt-BR", { month: "long", year: "numeric" })}</small>
      </div>
      ${byCategory.length ? barList(byCategory, "category", "total") : empty("As categorias aparecem aqui após as vendas.")}
    </div>
  `;
}

function renderPos() {
  const products = filteredPosProducts();
  const totals = cartTotals();
  const barcodeEnabled = hasFeature("barcode");

  return `
    <div class="split">
      <div>
        <div class="filters">
          <div class="field">
            <label for="pos-search">Buscar produto ou código</label>
            <input id="pos-search" data-bind="posQuery" value="${escapeHtml(ui.posQuery)}" placeholder="${barcodeEnabled ? "Bipe, digite código, SKU ou nome" : "Nome, SKU ou código"}" autofocus />
          </div>
          <div class="field">
            <label for="pos-category">Categoria</label>
            <select id="pos-category" data-bind="productCategory">
              ${["Todas", ...categories].map((category) => `<option ${ui.productCategory === category ? "selected" : ""}>${category}</option>`).join("")}
            </select>
          </div>
          <button class="btn" data-action="clear-pos-filter" title="Limpar busca">
            <i data-lucide="x"></i>
            <span>Limpar</span>
          </button>
        </div>

        <div class="scanner-strip ${barcodeEnabled ? "enabled" : ""}">
          <i data-lucide="barcode"></i>
          <strong>${barcodeEnabled ? "Leitor de código ativo" : "Leitor de código no plano Pro"}</strong>
          <span>${barcodeEnabled ? "A maioria dos leitores USB funciona como teclado: bipe o produto e pressione Enter." : "A venda manual continua liberada neste plano."}</span>
        </div>

        <div class="product-grid">
          ${products.map((product) => productTile(product)).join("") || empty("Nenhum produto encontrado.")}
        </div>
      </div>

      <aside class="cart-panel">
        <div class="section-head">
          <h2>Carrinho</h2>
          <button class="icon-btn" data-action="clear-cart" title="Limpar carrinho"><i data-lucide="trash-2"></i></button>
        </div>

        <div class="cart-list">
          ${ui.cart.length ? ui.cart.map((item) => cartRow(item)).join("") : empty("Carrinho vazio.")}
        </div>

        <div class="form-grid">
          <div class="field">
            <label for="sale-payment">Pagamento</label>
            <select id="sale-payment">
              ${paymentMethods.map((method) => `<option>${method}</option>`).join("")}
            </select>
          </div>
          <div class="field">
            <label for="sale-discount">Desconto</label>
            <input id="sale-discount" inputmode="decimal" value="0,00" />
          </div>
          <div class="field full">
            <label for="sale-customer">Cliente</label>
            <input id="sale-customer" placeholder="Consumidor final" />
          </div>
        </div>

        <div class="summary">
          <div class="summary-row"><span>Subtotal</span><strong>${currency(totals.subtotal)}</strong></div>
          <div class="summary-row"><span>Itens</span><strong>${ui.cart.reduce((sum, item) => sum + Number(item.qty || 0), 0).toLocaleString("pt-BR")}</strong></div>
          <div class="summary-row total"><span>Total</span><span id="cart-total">${currency(totals.subtotal)}</span></div>
          <button class="btn primary" data-action="finalize-sale" ${ui.cart.length ? "" : "disabled"}>
            <i data-lucide="check-circle-2"></i>
            <span>Finalizar venda</span>
          </button>
        </div>
      </aside>
    </div>
  `;
}

function renderProducts() {
  const products = filteredProducts();

  return `
    <div class="filters">
      <div class="field">
        <label for="product-search">Buscar produto</label>
        <input id="product-search" data-bind="productQuery" value="${escapeHtml(ui.productQuery)}" placeholder="Nome, SKU, fornecedor ou código" />
      </div>
      <div class="field">
        <label for="product-category">Categoria</label>
        <select id="product-category" data-bind="productCategory">
          ${["Todas", ...categories].map((category) => `<option ${ui.productCategory === category ? "selected" : ""}>${category}</option>`).join("")}
        </select>
      </div>
      <div class="field">
        <label for="product-status">Status</label>
        <select id="product-status" data-bind="productStatus">
          <option value="ativos" ${ui.productStatus === "ativos" ? "selected" : ""}>Ativos</option>
          <option value="todos" ${ui.productStatus === "todos" ? "selected" : ""}>Todos</option>
          <option value="baixo" ${ui.productStatus === "baixo" ? "selected" : ""}>Baixo estoque</option>
          <option value="inativos" ${ui.productStatus === "inativos" ? "selected" : ""}>Inativos</option>
        </select>
      </div>
      <button class="btn primary" data-action="open-product-modal">
        <i data-lucide="package-plus"></i>
        <span>Novo produto</span>
      </button>
    </div>

    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Produto</th>
            <th>Categoria</th>
            <th>Fornecedor</th>
            <th class="num">Custo</th>
            <th class="num">Preço</th>
            <th class="num">Estoque</th>
            <th>Status</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          ${products.map(productRow).join("") || `<tr><td colspan="8">${empty("Nenhum produto encontrado.")}</td></tr>`}
        </tbody>
      </table>
    </div>
  `;
}

function renderInventory() {
  const movements = filteredMovements();

  return `
    <div class="split">
      <form class="tool-panel" id="movement-form">
        <div class="section-head">
          <h2>Registrar movimentação</h2>
          <span class="pill blue">Entrada e saída</span>
        </div>
        <div class="form-grid">
          <div class="field full">
            <label for="movement-product">Produto</label>
            <select id="movement-product" name="productId" required>
              ${activeProducts().map((product) => `<option value="${product.id}">${escapeHtml(product.name)} - ${number(product.stock)} ${product.unit}</option>`).join("")}
            </select>
          </div>
          <div class="field">
            <label for="movement-type">Tipo</label>
            <select id="movement-type" name="type" required>
              <option value="entrada">Entrada</option>
              <option value="saida">Saída</option>
              <option value="perda">Perda</option>
              <option value="ajuste">Ajuste para saldo</option>
            </select>
          </div>
          <div class="field">
            <label for="movement-quantity">Quantidade</label>
            <input id="movement-quantity" name="quantity" inputmode="decimal" placeholder="0,00" required />
          </div>
          <div class="field">
            <label for="movement-cost">Custo unitário</label>
            <input id="movement-cost" name="unitCost" inputmode="decimal" placeholder="0,00" />
          </div>
          <div class="field">
            <label for="movement-reason">Motivo</label>
            <input id="movement-reason" name="reason" placeholder="Compra, troca, quebra..." required />
          </div>
          <div class="field full">
            <label for="movement-note">Observação</label>
            <textarea id="movement-note" name="note"></textarea>
          </div>
        </div>
        <div class="inline-actions">
          <button class="btn primary" type="submit"><i data-lucide="save"></i><span>Salvar movimento</span></button>
        </div>
      </form>

      <div class="tool-panel">
        <div class="section-head">
          <h2>Baixo estoque</h2>
          <small>${lowStockProducts().length} produto(s)</small>
        </div>
        ${lowStockProducts().length ? lowStockTable(lowStockProducts().slice(0, 10)) : empty("Nenhum alerta agora.")}
      </div>
    </div>

    <div class="band">
      <div class="filters">
        <div class="field">
          <label for="movement-search">Buscar movimento</label>
          <input id="movement-search" data-bind="movementQuery" value="${escapeHtml(ui.movementQuery)}" placeholder="Produto, motivo ou observação" />
        </div>
        <div class="field">
          <label for="movement-filter">Tipo</label>
          <select id="movement-filter" data-bind="movementType">
            <option value="todos" ${ui.movementType === "todos" ? "selected" : ""}>Todos</option>
            <option value="entrada" ${ui.movementType === "entrada" ? "selected" : ""}>Entrada</option>
            <option value="saida" ${ui.movementType === "saida" ? "selected" : ""}>Saída</option>
            <option value="perda" ${ui.movementType === "perda" ? "selected" : ""}>Perda</option>
            <option value="ajuste" ${ui.movementType === "ajuste" ? "selected" : ""}>Ajuste</option>
            <option value="venda" ${ui.movementType === "venda" ? "selected" : ""}>Venda</option>
          </select>
        </div>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Data</th>
              <th>Produto</th>
              <th>Tipo</th>
              <th>Motivo</th>
              <th class="num">Quantidade</th>
              <th class="num">Custo</th>
              <th class="num">Saldo</th>
            </tr>
          </thead>
          <tbody>
            ${movements.map(movementRow).join("") || `<tr><td colspan="7">${empty("Nenhum movimento encontrado.")}</td></tr>`}
          </tbody>
        </table>
      </div>
    </div>
  `;
}

function renderCash() {
  const session = currentSession();
  const sessions = cashSessionList().slice(0, 8);
  const transactions = session ? state.cash.transactions.filter((item) => item.sessionId === session.id && item.companyId === currentCompanyId()) : [];
  const totals = cashTotals(session?.id);

  return `
    <div class="three-col">
      <div class="kpi">${kpiInner("Status", session ? "Aberto" : "Fechado", "banknote", session ? `desde ${dateTime(session.openedAt)}` : "sem sessão ativa")}</div>
      <div class="kpi">${kpiInner("Entradas", currency(totals.in), "arrow-down-left", "vendas e suprimentos")}</div>
      <div class="kpi">${kpiInner("Saídas", currency(totals.out), "arrow-up-right", "sangrias e retiradas")}</div>
    </div>

    <div class="split band">
      <div class="tool-panel">
        <div class="section-head">
          <h2>${session ? "Movimento do caixa" : "Abrir caixa"}</h2>
          ${session ? `<span class="pill green">Sessão ativa</span>` : `<span class="pill red">Fechado</span>`}
        </div>

        ${session ? `
          <form id="cash-movement-form" class="form-grid">
            <div class="field">
              <label for="cash-kind">Tipo</label>
              <select id="cash-kind" name="kind">
                <option value="suprimento">Suprimento</option>
                <option value="sangria">Sangria</option>
              </select>
            </div>
            <div class="field">
              <label for="cash-amount">Valor</label>
              <input id="cash-amount" name="amount" inputmode="decimal" required />
            </div>
            <div class="field full">
              <label for="cash-desc">Descrição</label>
              <input id="cash-desc" name="description" placeholder="Troco, retirada, ajuste..." required />
            </div>
            <div class="inline-actions full">
              <button class="btn primary" type="submit"><i data-lucide="plus"></i><span>Lançar</span></button>
              <button class="btn warning" type="button" data-action="open-close-cash"><i data-lucide="lock"></i><span>Fechar caixa</span></button>
            </div>
          </form>
        ` : `
          <form id="cash-open-form" class="form-grid">
            <div class="field">
              <label for="cash-initial">Valor inicial</label>
              <input id="cash-initial" name="initialAmount" inputmode="decimal" value="0,00" required />
            </div>
            <div class="field">
              <label for="cash-operator">Operador</label>
              <input id="cash-operator" name="operator" value="Operador" required />
            </div>
            <div class="inline-actions full">
              <button class="btn primary" type="submit"><i data-lucide="unlock"></i><span>Abrir caixa</span></button>
            </div>
          </form>
        `}
      </div>

      <div class="tool-panel">
        <div class="section-head">
          <h2>Resumo</h2>
          <small>${session ? dateOnly(session.openedAt) : "sem sessão"}</small>
        </div>
        <div class="summary">
          <div class="summary-row"><span>Inicial</span><strong>${currency(totals.initial)}</strong></div>
          <div class="summary-row"><span>Vendas</span><strong>${currency(totals.sales)}</strong></div>
          <div class="summary-row"><span>Suprimentos</span><strong>${currency(totals.supply)}</strong></div>
          <div class="summary-row"><span>Sangrias</span><strong>${currency(totals.withdraw)}</strong></div>
          <div class="summary-row total"><span>Esperado</span><span>${currency(totals.expected)}</span></div>
        </div>
      </div>
    </div>

    <div class="band">
      <div class="section-head">
        <h2>Transações do caixa</h2>
        <small>${transactions.length} lançamento(s)</small>
      </div>
      ${transactions.length ? cashTransactionTable(transactions) : empty("Nenhuma transação na sessão atual.")}
    </div>

    <div class="band">
      <div class="section-head">
        <h2>Sessões recentes</h2>
      </div>
      ${sessions.length ? cashSessionTable(sessions) : empty("Nenhuma sessão de caixa criada.")}
    </div>
  `;
}

function renderReports() {
  const sales = salesInMonth(ui.reportMonth);
  const byPayment = groupSales(sales, "paymentMethod");
  const byCategory = categoryRevenue(sales);
  const topProducts = topProductSales(sales);
  const totals = sales.reduce((acc, sale) => {
    acc.revenue += Number(sale.total || 0);
    acc.profit += Number(sale.profit || 0);
    acc.cost += Number(sale.cost || 0);
    return acc;
  }, { revenue: 0, profit: 0, cost: 0 });

  return `
    <div class="filters">
      <div class="field">
        <label for="report-month">Mês</label>
        <input id="report-month" type="month" data-bind="reportMonth" value="${ui.reportMonth}" />
      </div>
      <button class="btn" data-action="export-sales-csv"><i data-lucide="file-down"></i><span>Exportar CSV</span></button>
    </div>

    <div class="kpi-grid">
      ${kpi("Vendas", String(sales.length), "receipt", "no período")}
      ${kpi("Faturamento", currency(totals.revenue), "chart-column-increasing", "valor bruto")}
      ${kpi("Custo", currency(totals.cost), "package-search", "custo estimado")}
      ${kpi("Lucro bruto", currency(totals.profit), "badge-dollar-sign", `${totals.revenue ? Math.round((totals.profit / totals.revenue) * 100) : 0}% de margem`)}
      ${kpi("Ticket médio", currency(sales.length ? totals.revenue / sales.length : 0), "wallet-cards", "média por venda")}
    </div>

    <div class="three-col band">
      <div class="tool-panel">
        <div class="section-head"><h2>Por pagamento</h2></div>
        ${byPayment.length ? barList(byPayment, "label", "total") : empty("Sem vendas no período.")}
      </div>
      <div class="tool-panel">
        <div class="section-head"><h2>Por categoria</h2></div>
        ${byCategory.length ? barList(byCategory, "category", "total") : empty("Sem dados por categoria.")}
      </div>
      <div class="tool-panel">
        <div class="section-head"><h2>Mais vendidos</h2></div>
        ${topProducts.length ? barList(topProducts, "name", "quantity", "qty") : empty("Sem produtos vendidos.")}
      </div>
    </div>

    <div class="band">
      <div class="section-head">
        <h2>Vendas do período</h2>
        <small>${sales.length} venda(s)</small>
      </div>
      ${sales.length ? salesTable(sales) : empty("Nenhuma venda no mês selecionado.")}
    </div>
  `;
}

function renderTeam() {
  const users = companyUsers().filter((user) => {
    const query = ui.teamQuery.trim().toLowerCase();
    if (!query) return true;
    return [user.name, user.email, roles[user.role]?.label].some((value) => String(value || "").toLowerCase().includes(query));
  });
  const plan = currentPlan();
  const canAdd = companyUsers().filter((user) => user.status !== "inactive").length < plan.users;

  return `
    <div class="split">
      <form class="tool-panel" id="team-form">
        <div class="section-head">
          <h2>Novo funcionário</h2>
          <span class="pill ${canAdd ? "green" : "red"}">${companyUsers().length}/${plan.users} usuários</span>
        </div>
        <div class="form-grid">
          <div class="field">
            <label for="team-name">Nome</label>
            <input id="team-name" name="name" required />
          </div>
          <div class="field">
            <label for="team-email">E-mail</label>
            <input id="team-email" name="email" type="email" required />
          </div>
          <div class="field">
            <label for="team-role">Cargo</label>
            <select id="team-role" name="role">
              ${Object.entries(roles).filter(([key]) => key !== "owner").map(([key, role]) => `<option value="${key}">${role.label}</option>`).join("")}
            </select>
          </div>
          <div class="field">
            <label for="team-status">Status</label>
            <select id="team-status" name="status">
              <option value="active">Ativo</option>
              <option value="inactive">Inativo</option>
            </select>
          </div>
        </div>
        <div class="inline-actions">
          <button class="btn primary" type="submit" ${canAdd ? "" : "disabled"}><i data-lucide="user-plus"></i><span>Adicionar</span></button>
        </div>
      </form>

      <div class="tool-panel">
        <div class="section-head">
          <h2>Permissões por cargo</h2>
        </div>
        <div class="role-list">
          ${Object.entries(roles).map(([key, role]) => `
            <div class="role-card">
              <strong>${role.label}</strong>
              <span>${role.permissions.map(permissionLabel).join(", ")}</span>
            </div>
          `).join("")}
        </div>
      </div>
    </div>

    <div class="band">
      <div class="filters">
        <div class="field">
          <label for="team-search">Buscar equipe</label>
          <input id="team-search" data-bind="teamQuery" value="${escapeHtml(ui.teamQuery)}" placeholder="Nome, e-mail ou cargo" />
        </div>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Nome</th>
              <th>E-mail</th>
              <th>Cargo</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            ${users.map((user) => `
              <tr>
                <td class="strong">${escapeHtml(user.name)}</td>
                <td>${escapeHtml(user.email)}</td>
                <td><span class="pill blue">${escapeHtml(roles[user.role]?.label || user.role)}</span></td>
                <td><span class="pill ${user.status === "inactive" ? "red" : "green"}">${user.status === "inactive" ? "Inativo" : "Ativo"}</span></td>
                <td>
                  <div class="row-actions">
                    <button class="icon-btn" data-action="cycle-role" data-id="${user.id}" ${user.role === "owner" ? "disabled" : ""} title="Trocar cargo"><i data-lucide="repeat-2"></i></button>
                    <button class="icon-btn ${user.status === "inactive" ? "" : "danger"}" data-action="toggle-user" data-id="${user.id}" ${user.role === "owner" ? "disabled" : ""} title="Ativar/Inativar"><i data-lucide="${user.status === "inactive" ? "user-check" : "user-x"}"></i></button>
                  </div>
                </td>
              </tr>
            `).join("") || `<tr><td colspan="5">${empty("Nenhum funcionário encontrado.")}</td></tr>`}
          </tbody>
        </table>
      </div>
    </div>
  `;
}

function renderBilling() {
  const company = currentCompany();
  const plan = currentPlan();
  const usage = {
    products: productList().length,
    users: companyUsers().filter((user) => user.status !== "inactive").length,
    monthlySales: salesInMonth(monthKey(new Date())).length,
  };

  return `
    <div class="kpi-grid">
      ${kpi("Plano atual", escapeHtml(plan.name), "credit-card", company.subscriptionStatus === "active" ? "assinatura ativa" : `teste até ${dateOnly(company.trialEndsAt)}`)}
      ${kpi("Produtos", `${usage.products}/${plan.products}`, "boxes", "limite do plano")}
      ${kpi("Usuários", `${usage.users}/${plan.users}`, "users", "equipe ativa")}
      ${kpi("Vendas mês", `${usage.monthlySales}/${plan.monthlySales}`, "receipt", "franquia comercial")}
      ${kpi("Preço", currency(plan.price), "badge-dollar-sign", "valor mensal sugerido")}
    </div>

    <div class="plan-grid band">
      ${Object.values(planCatalog).map((item) => `
        <article class="plan-card ${item.id === plan.id ? "active" : ""}">
          <div>
            <div class="eyebrow">${item.id === plan.id ? "Plano atual" : "Plano"}</div>
            <h2>${item.name}</h2>
            <div class="plan-price">${currency(item.price)}<span>/mês</span></div>
          </div>
          <ul>
            <li>${item.products.toLocaleString("pt-BR")} produtos</li>
            <li>${item.users} usuários</li>
            <li>${item.monthlySales.toLocaleString("pt-BR")} vendas/mês</li>
            <li>${item.features.includes("barcode") ? "Leitor de código" : "PDV manual"}</li>
            <li>${item.features.includes("thermalPrinter") ? "Preparado para térmica dedicada" : "Impressão pelo navegador"}</li>
          </ul>
          <button class="btn ${item.id === plan.id ? "" : "primary"}" data-action="select-plan" data-id="${item.id}" ${item.id === plan.id ? "disabled" : ""}>
            <i data-lucide="check"></i>
            <span>${item.id === plan.id ? "Selecionado" : "Selecionar"}</span>
          </button>
        </article>
      `).join("")}
    </div>

    <div class="tool-panel">
      <div class="section-head">
        <h2>Cobrança</h2>
        <span class="pill amber">Gateway pendente</span>
      </div>
      <p class="muted">A base já guarda plano, status e limites. Para venda real, conecte Stripe, Mercado Pago ou Asaas por Firebase Functions e salve o retorno do webhook no status da empresa.</p>
      <div class="inline-actions">
        <button class="btn" data-action="open-checkout-placeholder"><i data-lucide="link"></i><span>Simular link de pagamento</span></button>
      </div>
    </div>
  `;
}

function permissionLabel(permission) {
  return {
    dashboard: "painel",
    sell: "vendas",
    products: "produtos",
    inventory: "estoque",
    cash: "caixa",
    reports: "relatórios",
    settings: "configuração",
    team: "equipe",
    billing: "planos",
  }[permission] || permission;
}

function renderSettings() {
  const company = currentCompany();
  return `
    <div class="split">
      <form class="tool-panel" id="settings-form">
        <div class="section-head">
          <h2>Dados da loja</h2>
        </div>
        <div class="form-grid">
          <div class="field">
            <label for="store-name">Nome</label>
            <input id="store-name" name="storeName" value="${escapeHtml(company?.name || state.settings.storeName)}" required />
          </div>
          <div class="field">
            <label for="store-document">CNPJ/CPF</label>
            <input id="store-document" name="storeDocument" value="${escapeHtml(company?.document || state.settings.storeDocument)}" />
          </div>
          <div class="field">
            <label for="store-phone">Telefone</label>
            <input id="store-phone" name="storePhone" value="${escapeHtml(company?.phone || state.settings.storePhone)}" />
          </div>
          <div class="field">
            <label for="store-address">Endereço</label>
            <input id="store-address" name="address" value="${escapeHtml(company?.address || state.settings.address)}" />
          </div>
          <div class="field">
            <label for="receipt-width">Cupom</label>
            <select id="receipt-width" name="receiptWidth">
              <option value="58mm" ${state.settings.receiptWidth === "58mm" ? "selected" : ""}>58mm</option>
              <option value="80mm" ${state.settings.receiptWidth !== "58mm" ? "selected" : ""}>80mm</option>
            </select>
          </div>
          <div class="field">
            <label for="printer-mode">Impressão</label>
            <select id="printer-mode" name="printerMode">
              <option value="browser" ${state.settings.printerMode === "browser" ? "selected" : ""}>Diálogo do navegador</option>
              <option value="thermal" ${state.settings.printerMode === "thermal" ? "selected" : ""}>Térmica dedicada</option>
            </select>
          </div>
        </div>
        <div class="inline-actions">
          <button class="btn primary" type="submit"><i data-lucide="save"></i><span>Salvar</span></button>
          <button class="btn" type="button" data-action="test-print"><i data-lucide="printer"></i><span>Teste impressão</span></button>
        </div>
      </form>

      <div class="tool-panel">
        <div class="section-head">
          <h2>Dados e nuvem</h2>
        </div>
        <div class="summary">
          <div class="summary-row"><span>Banco</span><strong>${cloud.enabled ? "Firebase Firestore" : "Local/demo"}</strong></div>
          <div class="summary-row"><span>Sincronização</span><strong>${cloud.lastSyncAt ? dateTime(cloud.lastSyncAt) : cloud.status}</strong></div>
        </div>
        <div class="inline-actions">
          <button class="btn" data-action="export-backup"><i data-lucide="download"></i><span>Backup</span></button>
          <label class="btn" title="Importar backup">
            <i data-lucide="upload"></i>
            <span>Importar</span>
            <input type="file" id="import-backup" accept="application/json" hidden />
          </label>
          <button class="btn danger" data-action="reset-demo"><i data-lucide="rotate-ccw"></i><span>Restaurar demo</span></button>
        </div>
      </div>
    </div>
  `;
}

function kpi(label, value, icon, note = "") {
  return `<div class="kpi">${kpiInner(label, value, icon, note)}</div>`;
}

function kpiInner(label, value, icon, note = "") {
  return `
    <div class="kpi-head">
      <span>${label}</span>
      <i data-lucide="${icon}"></i>
    </div>
    <div class="kpi-value">${value}</div>
    ${note ? `<div class="kpi-note">${note}</div>` : ""}
  `;
}

function empty(text) {
  return `<div class="empty">${text}</div>`;
}

function filteredProducts() {
  const query = ui.productQuery.trim().toLowerCase();
  return productList()
    .filter((product) => ui.productCategory === "Todas" || product.category === ui.productCategory)
    .filter((product) => {
      if (ui.productStatus === "ativos") return product.active !== false;
      if (ui.productStatus === "inativos") return product.active === false;
      if (ui.productStatus === "baixo") return product.active !== false && Number(product.stock) <= Number(product.minStock || 0);
      return true;
    })
    .filter((product) => {
      if (!query) return true;
      return [product.name, product.sku, product.barcode, product.supplier, product.category].some((value) => String(value || "").toLowerCase().includes(query));
    })
    .sort((a, b) => a.name.localeCompare(b.name, "pt-BR"));
}

function filteredPosProducts() {
  const query = ui.posQuery.trim().toLowerCase();
  return activeProducts()
    .filter((product) => ui.productCategory === "Todas" || product.category === ui.productCategory)
    .filter((product) => {
      if (!query) return true;
      return [product.name, product.sku, product.barcode].some((value) => String(value || "").toLowerCase().includes(query));
    })
    .sort((a, b) => a.name.localeCompare(b.name, "pt-BR"));
}

function filteredMovements() {
  const query = ui.movementQuery.trim().toLowerCase();
  return movementList()
    .filter((movement) => ui.movementType === "todos" || movement.type === ui.movementType)
    .filter((movement) => {
      if (!query) return true;
      return [movement.productName, movement.reason, movement.note].some((value) => String(value || "").toLowerCase().includes(query));
    })
    .slice()
    .sort((a, b) => new Date(b.date) - new Date(a.date));
}

function productTile(product) {
  const stockClass = product.stock <= product.minStock ? "red" : "green";
  return `
    <button class="product-tile" data-action="add-cart" data-id="${product.id}">
      <span class="product-top">
        <span>
          <span class="product-name">${escapeHtml(product.name)}</span>
          <span class="product-code">${escapeHtml(product.sku)} · ${escapeHtml(product.barcode)}</span>
        </span>
        <span class="pill ${stockClass}">${number(product.stock)} ${product.unit}</span>
      </span>
      <span class="muted">${escapeHtml(product.category)}</span>
      <span class="product-price">${currency(product.price)}</span>
    </button>
  `;
}

function productRow(product) {
  const margin = product.price ? Math.round(((product.price - product.cost) / product.price) * 100) : 0;
  const stockClass = product.active === false ? "red" : product.stock <= product.minStock ? "amber" : "green";
  const status = product.active === false ? "Inativo" : product.stock <= product.minStock ? "Baixo" : "Ativo";

  return `
    <tr>
      <td>
        <div class="strong">${escapeHtml(product.name)}</div>
        <div class="muted">${escapeHtml(product.sku)} · ${escapeHtml(product.barcode || "-")}</div>
      </td>
      <td>${escapeHtml(product.category)}</td>
      <td>${escapeHtml(product.supplier || "-")}</td>
      <td class="num">${currency(product.cost)}</td>
      <td class="num">
        <div class="strong">${currency(product.price)}</div>
        <div class="muted">${margin}% margem</div>
      </td>
      <td class="num">${number(product.stock)} ${product.unit}<br><span class="muted">mín. ${number(product.minStock)} ${product.unit}</span></td>
      <td><span class="pill ${stockClass}">${status}</span></td>
      <td>
        <div class="row-actions">
          <button class="icon-btn" data-action="open-product-modal" data-id="${product.id}" title="Editar"><i data-lucide="pencil"></i></button>
          <button class="icon-btn ${product.active === false ? "" : "danger"}" data-action="toggle-product" data-id="${product.id}" title="${product.active === false ? "Ativar" : "Inativar"}"><i data-lucide="${product.active === false ? "power" : "power-off"}"></i></button>
        </div>
      </td>
    </tr>
  `;
}

function cartRow(item) {
  return `
    <div class="cart-row">
      <div>
        <div class="strong">${escapeHtml(item.name)}</div>
        <div class="muted">${currency(item.price)} · ${escapeHtml(item.unit)}</div>
      </div>
      <div class="qty-control">
        <button data-action="cart-dec" data-id="${item.productId}" title="Diminuir">-</button>
        <input data-action="cart-qty" data-id="${item.productId}" value="${number(item.qty)}" inputmode="decimal" />
        <button data-action="cart-inc" data-id="${item.productId}" title="Aumentar">+</button>
      </div>
      <button class="icon-btn danger" data-action="cart-remove" data-id="${item.productId}" title="Remover"><i data-lucide="x"></i></button>
    </div>
  `;
}

function cartTotals() {
  const subtotal = ui.cart.reduce((sum, item) => sum + item.qty * item.price, 0);
  const cost = ui.cart.reduce((sum, item) => sum + item.qty * item.cost, 0);
  return { subtotal: round(subtotal), cost: round(cost), profit: round(subtotal - cost) };
}

function movementRow(movement) {
  const qtyClass = ["entrada", "ajuste"].includes(movement.type) && movement.quantity >= 0 ? "positive" : "negative";
  return `
    <tr>
      <td>${dateTime(movement.date)}</td>
      <td>${escapeHtml(movement.productName)}</td>
      <td><span class="pill ${movementPillClass(movement.type)}">${movementLabel(movement.type)}</span></td>
      <td>${escapeHtml(movement.reason || "-")}</td>
      <td class="num ${qtyClass}">${movement.quantity > 0 ? "+" : ""}${number(movement.quantity)}</td>
      <td class="num">${movement.unitCost ? currency(movement.unitCost) : "-"}</td>
      <td class="num">${number(movement.stockAfter)}</td>
    </tr>
  `;
}

function movementLabel(type) {
  return {
    entrada: "Entrada",
    saida: "Saída",
    perda: "Perda",
    ajuste: "Ajuste",
    venda: "Venda",
  }[type] || type;
}

function movementPillClass(type) {
  return {
    entrada: "green",
    saida: "amber",
    perda: "red",
    ajuste: "blue",
    venda: "blue",
  }[type] || "";
}

function salesTable(sales) {
  return `
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Venda</th>
            <th>Data</th>
            <th>Cliente</th>
            <th>Pagamento</th>
            <th class="num">Itens</th>
            <th class="num">Total</th>
            <th class="num">Lucro</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          ${sales.map((sale) => `
            <tr>
              <td class="strong">#${sale.number}</td>
              <td>${dateTime(sale.date)}</td>
              <td>${escapeHtml(sale.customer || "Consumidor final")}</td>
              <td><span class="pill blue">${escapeHtml(sale.paymentMethod)}</span></td>
              <td class="num">${sale.items.reduce((sum, item) => sum + Number(item.qty || 0), 0).toLocaleString("pt-BR")}</td>
              <td class="num strong">${currency(sale.total)}</td>
              <td class="num ${sale.profit >= 0 ? "positive" : "negative"}">${currency(sale.profit)}</td>
              <td><button class="icon-btn" data-action="print-receipt" data-id="${sale.id}" title="Comprovante"><i data-lucide="printer"></i></button></td>
            </tr>
          `).join("")}
        </tbody>
      </table>
    </div>
  `;
}

function lowStockTable(products) {
  return `
    <div class="table-wrap">
      <table class="compact-table">
        <thead>
          <tr>
            <th>Produto</th>
            <th class="num">Estoque</th>
            <th class="num">Mínimo</th>
          </tr>
        </thead>
        <tbody>
          ${products.map((product) => `
            <tr>
              <td>${escapeHtml(product.name)}</td>
              <td class="num negative">${number(product.stock)} ${product.unit}</td>
              <td class="num">${number(product.minStock)} ${product.unit}</td>
            </tr>
          `).join("")}
        </tbody>
      </table>
    </div>
  `;
}

function cashTransactionTable(transactions) {
  return `
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Data</th>
            <th>Tipo</th>
            <th>Descrição</th>
            <th>Pagamento</th>
            <th class="num">Valor</th>
          </tr>
        </thead>
        <tbody>
          ${transactions.slice().sort((a, b) => new Date(b.date) - new Date(a.date)).map((item) => `
            <tr>
              <td>${dateTime(item.date)}</td>
              <td><span class="pill ${item.kind === "sangria" ? "red" : "green"}">${cashKindLabel(item.kind)}</span></td>
              <td>${escapeHtml(item.description)}</td>
              <td>${escapeHtml(item.paymentMethod || "-")}</td>
              <td class="num ${item.amount >= 0 ? "positive" : "negative"}">${currency(item.amount)}</td>
            </tr>
          `).join("")}
        </tbody>
      </table>
    </div>
  `;
}

function cashSessionTable(sessions) {
  return `
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Abertura</th>
            <th>Operador</th>
            <th>Status</th>
            <th class="num">Inicial</th>
            <th class="num">Esperado</th>
            <th class="num">Informado</th>
          </tr>
        </thead>
        <tbody>
          ${sessions.map((session) => {
            const totals = cashTotals(session.id);
            return `
              <tr>
                <td>${dateTime(session.openedAt)}</td>
                <td>${escapeHtml(session.operator || "-")}</td>
                <td><span class="pill ${session.closedAt ? "blue" : "green"}">${session.closedAt ? "Fechado" : "Aberto"}</span></td>
                <td class="num">${currency(session.initialAmount)}</td>
                <td class="num">${currency(totals.expected)}</td>
                <td class="num">${session.closedAt ? currency(session.countedAmount) : "-"}</td>
              </tr>
            `;
          }).join("")}
        </tbody>
      </table>
    </div>
  `;
}

function cashKindLabel(kind) {
  return {
    sale: "Venda",
    suprimento: "Suprimento",
    sangria: "Sangria",
  }[kind] || kind;
}

function cashTotals(sessionId) {
  const session = state.cash.sessions.find((item) => item.id === sessionId);
  const transactions = sessionId ? state.cash.transactions.filter((item) => item.sessionId === sessionId) : [];
  const sales = transactions.filter((item) => item.kind === "sale").reduce((sum, item) => sum + Number(item.amount || 0), 0);
  const supply = transactions.filter((item) => item.kind === "suprimento").reduce((sum, item) => sum + Number(item.amount || 0), 0);
  const withdraw = transactions.filter((item) => item.kind === "sangria").reduce((sum, item) => sum + Math.abs(Number(item.amount || 0)), 0);
  const initial = Number(session?.initialAmount || 0);
  const expected = initial + sales + supply - withdraw;
  return {
    initial: round(initial),
    sales: round(sales),
    supply: round(supply),
    withdraw: round(withdraw),
    in: round(initial + sales + supply),
    out: round(withdraw),
    expected: round(expected),
  };
}

function barList(items, labelKey, valueKey, mode = "money") {
  const max = Math.max(...items.map((item) => Number(item[valueKey] || 0)), 1);
  return `
    <div class="bar-list">
      ${items.map((item) => {
        const value = Number(item[valueKey] || 0);
        const label = escapeHtml(item[labelKey] || "-");
        return `
          <div class="bar-row">
            <div class="strong">${label}</div>
            <div class="bar-track"><div class="bar-fill" style="width:${Math.max(4, (value / max) * 100)}%"></div></div>
            <div class="num">${mode === "money" ? currency(value) : number(value)}</div>
          </div>
        `;
      }).join("")}
    </div>
  `;
}

function groupSales(sales, key) {
  const map = new Map();
  sales.forEach((sale) => {
    const label = sale[key] || "-";
    map.set(label, (map.get(label) || 0) + Number(sale.total || 0));
  });
  return [...map.entries()].map(([label, total]) => ({ label, total: round(total) })).sort((a, b) => b.total - a.total);
}

function categoryRevenue(sales) {
  const map = new Map();
  sales.forEach((sale) => {
    sale.items.forEach((item) => {
      map.set(item.category, (map.get(item.category) || 0) + Number(item.total || 0));
    });
  });
  return [...map.entries()].map(([category, total]) => ({ category, total: round(total) })).sort((a, b) => b.total - a.total);
}

function topProductSales(sales) {
  const map = new Map();
  sales.forEach((sale) => {
    sale.items.forEach((item) => {
      const current = map.get(item.productId) || { name: item.name, quantity: 0 };
      current.quantity += Number(item.qty || 0);
      map.set(item.productId, current);
    });
  });
  return [...map.values()].sort((a, b) => b.quantity - a.quantity).slice(0, 8);
}

function openProductModal(id = "") {
  const product = productById(id) || {
    name: "",
    sku: nextSku(),
    barcode: "",
    category: "Mercearia",
    unit: "un",
    cost: 0,
    price: 0,
    stock: 0,
    minStock: 0,
    supplier: "",
    active: true,
  };

  showModal(`
    <div class="modal-head">
      <h2>${id ? "Editar produto" : "Novo produto"}</h2>
      <button class="icon-btn" data-action="close-modal" title="Fechar"><i data-lucide="x"></i></button>
    </div>
    <form id="product-form" data-id="${id}">
      <div class="modal-body">
        <div class="form-grid">
          <div class="field full">
            <label for="name">Nome</label>
            <input id="name" name="name" value="${escapeHtml(product.name)}" required />
          </div>
          <div class="field">
            <label for="sku">SKU</label>
            <input id="sku" name="sku" value="${escapeHtml(product.sku)}" required />
          </div>
          <div class="field">
            <label for="barcode">Código de barras</label>
            <input id="barcode" name="barcode" value="${escapeHtml(product.barcode)}" />
          </div>
          <div class="field">
            <label for="category">Categoria</label>
            <select id="category" name="category">
              ${categories.map((category) => `<option ${product.category === category ? "selected" : ""}>${category}</option>`).join("")}
            </select>
          </div>
          <div class="field">
            <label for="unit">Unidade</label>
            <select id="unit" name="unit">
              ${units.map((unit) => `<option ${product.unit === unit ? "selected" : ""}>${unit}</option>`).join("")}
            </select>
          </div>
          <div class="field">
            <label for="cost">Custo</label>
            <input id="cost" name="cost" inputmode="decimal" value="${number(product.cost)}" required />
          </div>
          <div class="field">
            <label for="price">Preço de venda</label>
            <input id="price" name="price" inputmode="decimal" value="${number(product.price)}" required />
          </div>
          <div class="field">
            <label for="stock">Estoque atual</label>
            <input id="stock" name="stock" inputmode="decimal" value="${number(product.stock)}" required />
          </div>
          <div class="field">
            <label for="minStock">Estoque mínimo</label>
            <input id="minStock" name="minStock" inputmode="decimal" value="${number(product.minStock)}" required />
          </div>
          <div class="field full">
            <label for="supplier">Fornecedor</label>
            <input id="supplier" name="supplier" value="${escapeHtml(product.supplier)}" />
          </div>
        </div>
      </div>
      <div class="modal-actions">
        <button class="btn" type="button" data-action="close-modal"><i data-lucide="x"></i><span>Cancelar</span></button>
        <button class="btn primary" type="submit"><i data-lucide="save"></i><span>Salvar</span></button>
      </div>
    </form>
  `);
}

function nextSku() {
  const n = state.products.length + 1;
  return `PRO-${String(n).padStart(3, "0")}`;
}

function showModal(html) {
  document.querySelector("#modal-root").innerHTML = `<div class="modal-backdrop"><div class="modal">${html}</div></div>`;
  refreshIcons();
}

function closeModal() {
  document.querySelector("#modal-root").innerHTML = "";
}

function addToCart(id) {
  const product = productById(id);
  if (!product || product.active === false) return;
  if (Number(product.stock) <= 0) {
    toast("Produto sem estoque.");
    return;
  }

  const existing = ui.cart.find((item) => item.productId === id);
  if (existing) {
    setCartQty(id, Number(existing.qty) + 1, false);
  } else {
    ui.cart.push({
      productId: product.id,
      name: product.name,
      category: product.category,
      unit: product.unit,
      qty: 1,
      price: Number(product.price || 0),
      cost: Number(product.cost || 0),
    });
  }
  render();
}

function setCartQty(id, qty, rerender = true) {
  const item = ui.cart.find((cartItem) => cartItem.productId === id);
  const product = productById(id);
  if (!item || !product) return;
  const nextQty = Math.max(0, round(qty));
  if (nextQty > Number(product.stock)) {
    toast(`Estoque disponível: ${number(product.stock)} ${product.unit}.`);
    item.qty = Number(product.stock);
  } else {
    item.qty = nextQty;
  }
  ui.cart = ui.cart.filter((cartItem) => cartItem.qty > 0);
  if (rerender) render();
}

function finalizeSale() {
  if (!can("sell")) {
    toast("Seu cargo não permite finalizar vendas.");
    return;
  }
  if (!ui.cart.length) return;
  const plan = currentPlan();
  const monthSales = salesInMonth(monthKey(new Date())).length;
  if (monthSales >= plan.monthlySales) {
    toast("Limite mensal de vendas do plano atingido.");
    return;
  }
  const paymentMethod = document.querySelector("#sale-payment")?.value || "Dinheiro";
  const customer = document.querySelector("#sale-customer")?.value.trim() || "Consumidor final";
  const totals = saleTotals();

  for (const item of ui.cart) {
    const product = productById(item.productId);
    if (!product || Number(product.stock) < Number(item.qty)) {
      toast(`Estoque insuficiente para ${item.name}.`);
      return;
    }
  }

  const sale = {
    id: uid("sale"),
    companyId: currentCompanyId(),
    number: String(saleList().length + 1).padStart(6, "0"),
    date: new Date().toISOString(),
    customer,
    paymentMethod,
    subtotal: totals.subtotal,
    discount: totals.discount,
    total: totals.total,
    cost: totals.cost,
    profit: totals.profit,
    sessionId: activeSessionId(),
    items: ui.cart.map((item) => ({ ...item, total: round(item.qty * item.price), costTotal: round(item.qty * item.cost) })),
  };

  state.sales.unshift(sale);

  sale.items.forEach((item) => {
    const product = productById(item.productId);
    product.stock = round(Number(product.stock) - Number(item.qty));
    product.updatedAt = new Date().toISOString();
    state.movements.unshift({
      id: uid("mov"),
      companyId: currentCompanyId(),
      productId: product.id,
      productName: product.name,
      type: "venda",
      quantity: -Number(item.qty),
      unitCost: Number(product.cost || 0),
      totalCost: round(Number(item.qty) * Number(product.cost || 0)),
      reason: `Venda #${sale.number}`,
      note: paymentMethod,
      date: sale.date,
      stockAfter: product.stock,
    });
  });

  if (activeSessionId()) {
    state.cash.transactions.unshift({
      id: uid("cash"),
      companyId: currentCompanyId(),
      sessionId: activeSessionId(),
      saleId: sale.id,
      kind: "sale",
      amount: totals.total,
      paymentMethod,
      description: `Venda #${sale.number}`,
      date: sale.date,
    });
  }

  ui.cart = [];
  audit("sale.created", { number: sale.number, total: totals.total });
  saveState();
  render();
  toast(`Venda #${sale.number} finalizada.`);
}

function saveProduct(form) {
  if (!can("products")) {
    toast("Seu cargo não permite alterar produtos.");
    return;
  }
  const data = Object.fromEntries(new FormData(form).entries());
  const id = form.dataset.id;
  const payload = {
    name: data.name.trim(),
    sku: data.sku.trim(),
    barcode: data.barcode.trim(),
    category: data.category,
    unit: data.unit,
    cost: round(parseMoney(data.cost)),
    price: round(parseMoney(data.price)),
    stock: round(parseMoney(data.stock)),
    minStock: round(parseMoney(data.minStock)),
    supplier: data.supplier.trim(),
    active: true,
    updatedAt: new Date().toISOString(),
  };

  if (!payload.name || !payload.sku) {
    toast("Preencha nome e SKU.");
    return;
  }

  if (id) {
    const product = productById(id);
    const previousStock = Number(product.stock || 0);
    Object.assign(product, payload);
    if (previousStock !== payload.stock) {
      state.movements.unshift({
        id: uid("mov"),
        companyId: currentCompanyId(),
        productId: product.id,
        productName: product.name,
        type: "ajuste",
        quantity: round(payload.stock - previousStock),
        unitCost: product.cost,
        totalCost: round(Math.abs(payload.stock - previousStock) * product.cost),
        reason: "Ajuste no cadastro",
        note: "",
        date: new Date().toISOString(),
        stockAfter: product.stock,
      });
    }
  } else {
    if (productList().length >= currentPlan().products) {
      toast("Limite de produtos do plano atingido.");
      return;
    }
    const product = { id: uid("prd"), companyId: currentCompanyId(), createdAt: new Date().toISOString(), ...payload };
    state.products.unshift(product);
    if (product.stock) {
      state.movements.unshift({
        id: uid("mov"),
        companyId: currentCompanyId(),
        productId: product.id,
        productName: product.name,
        type: "entrada",
        quantity: product.stock,
        unitCost: product.cost,
        totalCost: round(product.stock * product.cost),
        reason: "Estoque inicial",
        note: "",
        date: new Date().toISOString(),
        stockAfter: product.stock,
      });
    }
  }

  saveState();
  closeModal();
  render();
  toast("Produto salvo.");
}

function toggleProduct(id) {
  if (!can("products")) {
    toast("Seu cargo não permite alterar produtos.");
    return;
  }
  const product = productById(id);
  if (!product) return;
  product.active = product.active === false;
  product.updatedAt = new Date().toISOString();
  saveState();
  render();
  toast(product.active ? "Produto ativado." : "Produto inativado.");
}

function saveMovement(form) {
  if (!can("inventory")) {
    toast("Seu cargo não permite movimentar estoque.");
    return;
  }
  const data = Object.fromEntries(new FormData(form).entries());
  const product = productById(data.productId);
  if (!product) return;

  const type = data.type;
  const inputQty = round(parseMoney(data.quantity));
  if (inputQty < 0) {
    toast("Informe uma quantidade positiva.");
    return;
  }

  let delta = inputQty;
  if (type === "saida" || type === "perda") delta = -inputQty;
  if (type === "ajuste") delta = round(inputQty - Number(product.stock || 0));

  const nextStock = round(Number(product.stock || 0) + delta);
  if (nextStock < 0) {
    toast(`Estoque insuficiente. Saldo atual: ${number(product.stock)} ${product.unit}.`);
    return;
  }

  const unitCost = parseMoney(data.unitCost) || Number(product.cost || 0);
  if (type === "entrada" && unitCost > 0) product.cost = round(unitCost);
  product.stock = nextStock;
  product.updatedAt = new Date().toISOString();

  state.movements.unshift({
    id: uid("mov"),
    companyId: currentCompanyId(),
    productId: product.id,
    productName: product.name,
    type,
    quantity: delta,
    unitCost: round(unitCost),
    totalCost: round(Math.abs(delta) * unitCost),
    reason: data.reason.trim(),
    note: data.note.trim(),
    date: new Date().toISOString(),
    stockAfter: product.stock,
  });

  saveState();
  form.reset();
  render();
  toast("Movimentação registrada.");
}

function openCash(form) {
  if (!can("cash")) {
    toast("Seu cargo não permite operar caixa.");
    return;
  }
  const data = Object.fromEntries(new FormData(form).entries());
  const session = {
    id: uid("cash_session"),
    companyId: currentCompanyId(),
    openedAt: new Date().toISOString(),
    closedAt: null,
    initialAmount: round(parseMoney(data.initialAmount)),
    operator: data.operator.trim() || "Operador",
    countedAmount: null,
    note: "",
  };
  state.cash.sessions.unshift(session);
  setActiveSessionId(session.id);
  audit("cash.opened", { initialAmount: session.initialAmount });
  saveState();
  render();
  toast("Caixa aberto.");
}

function addCashMovement(form) {
  if (!can("cash")) {
    toast("Seu cargo não permite operar caixa.");
    return;
  }
  const session = currentSession();
  if (!session) return;
  const data = Object.fromEntries(new FormData(form).entries());
  const amount = round(parseMoney(data.amount));
  if (!amount) {
    toast("Informe o valor.");
    return;
  }
  const signedAmount = data.kind === "sangria" ? -Math.abs(amount) : Math.abs(amount);
  state.cash.transactions.unshift({
    id: uid("cash"),
    companyId: currentCompanyId(),
    sessionId: session.id,
    kind: data.kind,
    amount: signedAmount,
    paymentMethod: "Dinheiro",
    description: data.description.trim(),
    date: new Date().toISOString(),
  });
  saveState();
  form.reset();
  render();
  toast("Lançamento de caixa salvo.");
}

function openCloseCashModal() {
  const session = currentSession();
  if (!session) return;
  const totals = cashTotals(session.id);
  showModal(`
    <div class="modal-head">
      <h2>Fechar caixa</h2>
      <button class="icon-btn" data-action="close-modal" title="Fechar"><i data-lucide="x"></i></button>
    </div>
    <form id="cash-close-form">
      <div class="modal-body">
        <div class="summary">
          <div class="summary-row"><span>Valor esperado</span><strong>${currency(totals.expected)}</strong></div>
        </div>
        <div class="form-grid">
          <div class="field">
            <label for="countedAmount">Valor contado</label>
            <input id="countedAmount" name="countedAmount" inputmode="decimal" value="${number(totals.expected)}" required />
          </div>
          <div class="field full">
            <label for="note">Observação</label>
            <textarea id="note" name="note"></textarea>
          </div>
        </div>
      </div>
      <div class="modal-actions">
        <button class="btn" type="button" data-action="close-modal"><i data-lucide="x"></i><span>Cancelar</span></button>
        <button class="btn warning" type="submit"><i data-lucide="lock"></i><span>Fechar caixa</span></button>
      </div>
    </form>
  `);
}

function closeCash(form) {
  if (!can("cash")) {
    toast("Seu cargo não permite operar caixa.");
    return;
  }
  const session = currentSession();
  if (!session) return;
  const data = Object.fromEntries(new FormData(form).entries());
  session.closedAt = new Date().toISOString();
  session.countedAmount = round(parseMoney(data.countedAmount));
  session.note = data.note.trim();
  setActiveSessionId(null);
  audit("cash.closed", { countedAmount: session.countedAmount });
  saveState();
  closeModal();
  render();
  toast("Caixa fechado.");
}

function saveSettings(form) {
  if (!can("settings")) {
    toast("Seu cargo não permite alterar configurações.");
    return;
  }
  const data = Object.fromEntries(new FormData(form).entries());
  const company = currentCompany();
  if (company) {
    company.name = data.storeName;
    company.document = data.storeDocument;
    company.phone = data.storePhone;
    company.address = data.address;
  }
  Object.assign(state.settings, data);
  audit("settings.updated", { company: data.storeName });
  saveState();
  render();
  toast("Configuração salva.");
}

function exportBackup() {
  downloadFile(`backup-varejo-flow-${todayKey()}.json`, JSON.stringify(state, null, 2), "application/json");
}

function importBackup(file) {
  const reader = new FileReader();
  reader.onload = () => {
    try {
      const imported = JSON.parse(reader.result);
      if (!imported.products || !imported.sales || !imported.movements) throw new Error("Arquivo inválido");
      Object.keys(state).forEach((key) => delete state[key]);
      Object.assign(state, ensureStateModel(imported));
      saveState();
      render();
      toast("Backup importado.");
    } catch (error) {
      toast("Arquivo de backup inválido.");
    }
  };
  reader.readAsText(file);
}

function exportSalesCsv() {
  const sales = salesInMonth(ui.reportMonth);
  const rows = [
    ["venda", "data", "cliente", "pagamento", "subtotal", "desconto", "total", "custo", "lucro"],
    ...sales.map((sale) => [sale.number, sale.date, sale.customer, sale.paymentMethod, sale.subtotal, sale.discount, sale.total, sale.cost, sale.profit]),
  ];
  const csv = rows.map((row) => row.map((cell) => `"${String(cell ?? "").replaceAll('"', '""')}"`).join(";")).join("\n");
  downloadFile(`vendas-${ui.reportMonth}.csv`, csv, "text/csv;charset=utf-8");
}

function downloadFile(filename, content, type) {
  const blob = new Blob([content], { type });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}

function resetDemo() {
  if (!confirm("Restaurar dados de demonstração?")) return;
  localStorage.removeItem(STORAGE_KEY);
  const fresh = loadState();
  Object.keys(state).forEach((key) => delete state[key]);
  Object.assign(state, fresh);
  ui.cart = [];
  saveState();
  render();
  toast("Dados de demonstração restaurados.");
}

async function authSubmit(form, mode) {
  const data = Object.fromEntries(new FormData(form).entries());
  if (mode === "signup") {
    await signupUser(data);
    return;
  }
  await loginUser(data.email, data.password);
}

async function loginUser(email, password = "") {
  const normalizedEmail = String(email || "").trim().toLowerCase();
  if (!normalizedEmail) return;

  if (cloud.enabled && password) {
    try {
      const credential = await cloud.auth.signInWithEmailAndPassword(normalizedEmail, password);
      ensureFirebaseUser(credential.user, normalizedEmail);
    } catch (error) {
      toast("Não foi possível entrar no Firebase. Verifique e-mail e senha.");
      return;
    }
  }

  let user = state.account.users.find((item) => item.email.toLowerCase() === normalizedEmail && item.status !== "inactive");
  if (!user && !cloud.enabled) {
    user = state.account.users[0];
  }
  if (!user) {
    toast("Usuário não cadastrado nesta empresa.");
    return;
  }
  state.account.currentCompanyId = user.companyId;
  appSession.userId = user.id;
  user.lastLoginAt = new Date().toISOString();
  await loadCompanySnapshot(user.companyId);
  saveSession();
  saveState();
  render();
  toast(`Bem-vindo, ${user.name}.`);
}

async function signupUser(data) {
  const email = String(data.email || "").trim().toLowerCase();
  const password = String(data.password || "");
  const companyName = String(data.companyName || "Minha loja").trim();
  if (!email || password.length < 6) {
    toast("Informe e-mail e senha com pelo menos 6 caracteres.");
    return;
  }

  if (cloud.enabled) {
    try {
      await cloud.auth.createUserWithEmailAndPassword(email, password);
    } catch (error) {
      toast("Não foi possível criar a conta no Firebase.");
      return;
    }
  }

  const company = createCompany(companyName);
  const user = {
    id: uid("user"),
    companyId: company.id,
    name: "Administrador",
    email,
    role: "owner",
    status: "active",
    createdAt: new Date().toISOString(),
  };
  state.account.users.unshift(user);
  state.account.currentCompanyId = company.id;
  appSession.userId = user.id;
  saveSession();
  saveState();
  render();
  toast("Conta e empresa criadas.");
}

function ensureFirebaseUser(firebaseUser, fallbackEmail) {
  if (!firebaseUser) return null;
  const email = (firebaseUser.email || fallbackEmail || "").toLowerCase();
  let user = state.account.users.find((item) => item.email.toLowerCase() === email);
  if (!user) {
    user = {
      id: firebaseUser.uid,
      companyId: currentCompanyId(),
      name: firebaseUser.displayName || email.split("@")[0] || "Usuário",
      email,
      role: "owner",
      status: "active",
      createdAt: new Date().toISOString(),
    };
    state.account.users.unshift(user);
  }
  return user;
}

function demoLogin(email) {
  const user = state.account.users.find((item) => item.email.toLowerCase() === String(email).toLowerCase());
  if (!user) return;
  state.account.currentCompanyId = user.companyId;
  appSession.userId = user.id;
  saveSession();
  render();
}

function logout() {
  if (cloud.enabled) cloud.auth.signOut().catch(() => {});
  clearSession();
  render();
}

function createCompany(name) {
  const now = new Date().toISOString();
  const company = {
    id: uid("company"),
    name: name || "Nova empresa",
    document: "",
    phone: "",
    address: "",
    planId: "starter",
    subscriptionStatus: "trial",
    trialEndsAt: addDays(now, 14),
    createdAt: now,
  };
  state.account.companies.unshift(company);
  seedProducts.forEach((product, index) => {
    const item = {
      id: uid("prd", index),
      companyId: company.id,
      createdAt: now,
      updatedAt: now,
      ...product,
    };
    state.products.push(item);
    state.movements.push({
      id: uid("mov", index),
      companyId: company.id,
      productId: item.id,
      productName: item.name,
      type: "entrada",
      quantity: item.stock,
      unitCost: item.cost,
      totalCost: round(item.stock * item.cost),
      reason: "Estoque inicial",
      note: "",
      date: now,
      stockAfter: item.stock,
    });
  });
  return company;
}

function openCompanyModal() {
  if (!hasFeature("multiCompany") && state.account.companies.length >= 1) {
    toast("Multiempresa está disponível a partir do plano Pro.");
    return;
  }
  showModal(`
    <div class="modal-head">
      <h2>Nova empresa</h2>
      <button class="icon-btn" data-action="close-modal" title="Fechar"><i data-lucide="x"></i></button>
    </div>
    <form id="company-form">
      <div class="modal-body">
        <div class="field">
          <label for="company-name">Nome da empresa</label>
          <input id="company-name" name="name" required />
        </div>
      </div>
      <div class="modal-actions">
        <button class="btn" type="button" data-action="close-modal"><i data-lucide="x"></i><span>Cancelar</span></button>
        <button class="btn primary" type="submit"><i data-lucide="save"></i><span>Criar</span></button>
      </div>
    </form>
  `);
}

function saveCompany(form) {
  const data = Object.fromEntries(new FormData(form).entries());
  const previousUser = currentUser();
  const company = createCompany(data.name.trim());
  state.account.currentCompanyId = company.id;
  if (previousUser) {
    state.account.users.unshift({
      id: uid("user"),
      companyId: company.id,
      name: previousUser.name,
      email: previousUser.email,
      role: "owner",
      status: "active",
      createdAt: new Date().toISOString(),
    });
    appSession.userId = state.account.users[0].id;
    saveSession();
  }
  closeModal();
  saveState();
  render();
  toast("Empresa criada.");
}

function addTeamUser(form) {
  if (!can("team")) {
    toast("Seu cargo não permite gerenciar equipe.");
    return;
  }
  const activeUsers = companyUsers().filter((user) => user.status !== "inactive").length;
  if (activeUsers >= currentPlan().users) {
    toast("Limite de usuários do plano atingido.");
    return;
  }
  const data = Object.fromEntries(new FormData(form).entries());
  const email = data.email.trim().toLowerCase();
  if (state.account.users.some((user) => user.companyId === currentCompanyId() && user.email.toLowerCase() === email)) {
    toast("Este e-mail já está cadastrado nesta empresa.");
    return;
  }
  state.account.users.unshift({
    id: uid("user"),
    companyId: currentCompanyId(),
    name: data.name.trim(),
    email,
    role: data.role,
    status: data.status,
    createdAt: new Date().toISOString(),
  });
  audit("team.user_added", { email, role: data.role });
  saveState();
  form.reset();
  render();
  toast("Funcionário adicionado.");
}

function cycleUserRole(id) {
  const user = state.account.users.find((item) => item.id === id && item.companyId === currentCompanyId());
  if (!user || user.role === "owner") return;
  const sequence = ["cashier", "stock", "accountant", "manager"];
  const index = sequence.indexOf(user.role);
  user.role = sequence[(index + 1) % sequence.length];
  audit("team.role_changed", { email: user.email, role: user.role });
  saveState();
  render();
}

function toggleUser(id) {
  const user = state.account.users.find((item) => item.id === id && item.companyId === currentCompanyId());
  if (!user || user.role === "owner") return;
  user.status = user.status === "inactive" ? "active" : "inactive";
  audit("team.status_changed", { email: user.email, status: user.status });
  saveState();
  render();
}

function selectPlan(id) {
  const company = currentCompany();
  if (!company || !planCatalog[id]) return;
  company.planId = id;
  company.subscriptionStatus = "active";
  audit("billing.plan_selected", { planId: id });
  saveState();
  render();
  toast(`Plano ${planCatalog[id].name} selecionado.`);
}

function openCheckoutPlaceholder() {
  const company = currentCompany();
  const plan = currentPlan();
  showModal(`
    <div class="modal-head">
      <h2>Link de pagamento</h2>
      <button class="icon-btn" data-action="close-modal" title="Fechar"><i data-lucide="x"></i></button>
    </div>
    <div class="modal-body">
      <div class="summary">
        <div class="summary-row"><span>Empresa</span><strong>${escapeHtml(company.name)}</strong></div>
        <div class="summary-row"><span>Plano</span><strong>${escapeHtml(plan.name)}</strong></div>
        <div class="summary-row"><span>Valor</span><strong>${currency(plan.price)}</strong></div>
      </div>
      <p class="muted">Na produção, este botão chamará uma Firebase Function para criar checkout no Stripe, Mercado Pago ou Asaas.</p>
    </div>
    <div class="modal-actions">
      <button class="btn primary" data-action="close-modal"><i data-lucide="check"></i><span>Entendi</span></button>
    </div>
  `);
}

function handleBarcodeEntry(code) {
  const value = String(code || "").trim().toLowerCase();
  if (!value) return;
  if (!hasFeature("barcode")) {
    toast("Leitor de código disponível no plano Pro.");
    return;
  }
  const product = activeProducts().find((item) => String(item.barcode || "").toLowerCase() === value || String(item.sku || "").toLowerCase() === value);
  if (!product) {
    toast("Código não encontrado.");
    return;
  }
  addToCart(product.id);
  ui.posQuery = "";
}

function printReceipt(id) {
  const sale = state.sales.find((item) => item.id === id);
  if (!sale) return;
  renderReceipt(sale);
}

function testPrint() {
  renderReceipt({
    number: "TESTE",
    date: new Date().toISOString(),
    subtotal: 9.9,
    discount: 0,
    total: 9.9,
    paymentMethod: "Teste",
    items: [{ name: "Produto de teste", qty: 1, unit: "un", price: 9.9, total: 9.9 }],
  });
}

function renderReceipt(sale) {
  const company = currentCompany();
  const receipt = document.createElement("div");
  receipt.className = "receipt";
  receipt.style.width = state.settings.receiptWidth || "80mm";
  receipt.innerHTML = `
    <h2>${escapeHtml(company?.name || state.settings.storeName)}</h2>
    <div>${escapeHtml(company?.document || "")}</div>
    <div>Venda #${sale.number}</div>
    <div>${dateTime(sale.date)}</div>
    <hr>
    ${sale.items.map((item) => `
      <div>${escapeHtml(item.name)}</div>
      <div>${number(item.qty)} ${item.unit} x ${currency(item.price)} = ${currency(item.total)}</div>
    `).join("")}
    <hr>
    <div>Subtotal: ${currency(sale.subtotal)}</div>
    <div>Desconto: ${currency(sale.discount)}</div>
    <strong>Total: ${currency(sale.total)}</strong>
    <div>Pagamento: ${escapeHtml(sale.paymentMethod)}</div>
  `;
  document.body.append(receipt);
  window.print();
  receipt.remove();
}

function toast(message) {
  const wrap = document.querySelector("#toast");
  const item = document.createElement("div");
  item.className = "toast";
  item.textContent = message;
  wrap.append(item);
  setTimeout(() => item.remove(), 2600);
}

function refreshIcons() {
  if (window.lucide) {
    window.lucide.createIcons({ attrs: { width: 18, height: 18, "stroke-width": 2.1 } });
    return;
  }

  const fallback = {
    "layout-dashboard": "▦",
    "scan-line": "⌕",
    "clipboard-list": "☷",
    "chart-no-axes-combined": "▥",
    "package-plus": "+",
    "shopping-cart": "◱",
    "trash-2": "×",
    "check-circle-2": "✓",
    "calendar-days": "□",
    "receipt-text": "≡",
    "receipt": "≡",
    "warehouse": "▤",
    "badge-dollar-sign": "$",
    "chart-column-increasing": "▥",
    "package-search": "□",
    "wallet-cards": "▣",
    "arrow-down-left": "↙",
    "arrow-up-right": "↗",
    "arrow-right": "→",
    "banknote": "$",
    "store": "▣",
    "boxes": "▤",
    "settings": "⚙",
    "file-down": "↓",
    "download": "↓",
    "upload": "↑",
    "rotate-ccw": "↺",
    "printer": "⎙",
    "pencil": "✎",
    "power": "⏻",
    "power-off": "⏻",
    "save": "✓",
    "x": "×",
    "plus": "+",
    "lock": "▣",
    "unlock": "▢",
    "users": "▥",
    "credit-card": "▣",
    "building-2": "▥",
    "log-out": "↩",
    "log-in": "↪",
    "user-plus": "+",
    "shield-check": "✓",
    "barcode": "▥",
    "repeat-2": "↻",
    "user-check": "✓",
    "user-x": "×",
    "check": "✓",
    "link": "↗",
  };

  document.querySelectorAll("[data-lucide]").forEach((icon) => {
    icon.classList.add("icon-fallback");
    icon.textContent = fallback[icon.dataset.lucide] || "•";
  });
}

document.addEventListener("click", (event) => {
  const viewButton = event.target.closest("[data-view]");
  if (viewButton) {
    ui.view = viewButton.dataset.view;
    render();
    return;
  }

  const actionButton = event.target.closest("[data-action]");
  if (!actionButton) return;
  const action = actionButton.dataset.action;
  const id = actionButton.dataset.id;

  if (action === "go-pos") ui.view = "pos";
  if (action === "open-product-modal") openProductModal(id);
  if (action === "open-company-modal") openCompanyModal();
  if (action === "close-modal") closeModal();
  if (action === "toggle-product") toggleProduct(id);
  if (action === "add-cart") addToCart(id);
  if (action === "clear-cart") ui.cart = [];
  if (action === "clear-pos-filter") {
    ui.posQuery = "";
    ui.productCategory = "Todas";
  }
  if (action === "cart-inc") {
    const item = ui.cart.find((cartItem) => cartItem.productId === id);
    setCartQty(id, Number(item?.qty || 0) + 1, false);
  }
  if (action === "cart-dec") {
    const item = ui.cart.find((cartItem) => cartItem.productId === id);
    setCartQty(id, Number(item?.qty || 0) - 1, false);
  }
  if (action === "cart-remove") ui.cart = ui.cart.filter((item) => item.productId !== id);
  if (action === "finalize-sale") finalizeSale();
  if (action === "open-close-cash") openCloseCashModal();
  if (action === "export-backup") exportBackup();
  if (action === "reset-demo") resetDemo();
  if (action === "export-sales-csv") exportSalesCsv();
  if (action === "print-receipt") printReceipt(id);
  if (action === "test-print") testPrint();
  if (action === "demo-login") demoLogin(actionButton.dataset.email);
  if (action === "logout") logout();
  if (action === "cycle-role") cycleUserRole(id);
  if (action === "toggle-user") toggleUser(id);
  if (action === "select-plan") selectPlan(id);
  if (action === "open-checkout-placeholder") openCheckoutPlaceholder();

  if (!["open-product-modal", "open-company-modal", "close-modal", "toggle-product", "add-cart", "finalize-sale", "open-close-cash", "export-backup", "reset-demo", "export-sales-csv", "print-receipt", "test-print", "demo-login", "logout", "cycle-role", "toggle-user", "select-plan", "open-checkout-placeholder"].includes(action)) {
    render();
  }
});

document.addEventListener("input", (event) => {
  const target = event.target;
  if (target.matches("[data-bind]")) {
    ui[target.dataset.bind] = target.value;
    render();
    return;
  }
  if (target.dataset.action === "cart-qty") {
    setCartQty(target.dataset.id, parseMoney(target.value));
  }
  if (target.id === "sale-discount") {
    const totals = saleTotals();
    const totalEl = document.querySelector("#cart-total");
    if (totalEl) totalEl.textContent = currency(totals.total);
  }
});

document.addEventListener("change", (event) => {
  const target = event.target;
  if (target.id === "company-select") {
    const previousUser = currentUser();
    state.account.currentCompanyId = target.value;
    const user = state.account.users.find((item) => item.email === previousUser?.email && item.companyId === target.value) || companyUsers()[0];
    if (user) {
      appSession.userId = user.id;
      saveSession();
    }
    saveState();
    render();
    return;
  }
  if (target.matches("[data-bind]")) {
    ui[target.dataset.bind] = target.value;
    render();
  }
  if (target.id === "import-backup" && target.files?.[0]) {
    importBackup(target.files[0]);
  }
});

document.addEventListener("submit", (event) => {
  event.preventDefault();
  const form = event.target;
  if (form.id === "auth-form") authSubmit(form, event.submitter?.dataset.authMode || "login");
  if (form.id === "product-form") saveProduct(form);
  if (form.id === "company-form") saveCompany(form);
  if (form.id === "movement-form") saveMovement(form);
  if (form.id === "cash-open-form") openCash(form);
  if (form.id === "cash-movement-form") addCashMovement(form);
  if (form.id === "cash-close-form") closeCash(form);
  if (form.id === "settings-form") saveSettings(form);
  if (form.id === "team-form") addTeamUser(form);
});

document.addEventListener("keydown", (event) => {
  if (ui.view !== "pos") return;
  if (event.key === "Enter" && event.target?.id === "pos-search") {
    event.preventDefault();
    handleBarcodeEntry(event.target.value);
  }
});

async function bootstrap() {
  await initCloud();
  if (appSession.userId && !currentUser()) {
    clearSession();
  }
  saveState();
  render();
}

bootstrap();
