// ═══════════════════════════════════════════════════════════════════════════
// LaviApp - Cloud Functions MINIMALISTA
// Version 2 - Simples, sem erros de sintaxe
// ═══════════════════════════════════════════════════════════════════════════

const { onRequest } = require("firebase-functions/v2/https");
const { onSchedule } = require("firebase-functions/v2/scheduler");
const { defineSecret } = require("firebase-functions/params");
const admin = require("firebase-admin");
const Stripe = require("stripe");
const crypto = require("crypto");

// Inicializar Firebase
admin.initializeApp();

// Definir secrets
const stripeSecretKey = defineSecret("STRIPE_SECRET_KEY");
const stripeWebhookSecret = defineSecret("STRIPE_WEBHOOK_SECRET");
const brevoApiKey = defineSecret("BREVO_API_KEY");

async function authenticateRequest(req) {
  const header = req.get("authorization") || req.headers.authorization || "";
  const match = String(header).match(/^Bearer\s+(.+)$/i);
  if (!match) {
    const error = new Error("Unauthorized");
    error.status = 401;
    throw error;
  }
  return admin.auth().verifyIdToken(match[1]);
}

async function requireFamilyAdmin(db, familyId, uid) {
  if (!familyId) {
    const error = new Error("familyId is required");
    error.status = 400;
    throw error;
  }

  const familyDoc = await db.collection("families").doc(String(familyId)).get();
  if (!familyDoc.exists) {
    const error = new Error("Family not found");
    error.status = 404;
    throw error;
  }

  const family = familyDoc.data() || {};
  if (family.adminUid !== uid) {
    const error = new Error("Only the family admin can perform this action");
    error.status = 403;
    throw error;
  }

  return { ref: familyDoc.ref, data: family };
}

async function getFamilyRefsForUser(db, userId) {
  const refs = new Map();

  const memberSnap = await db
    .collectionGroup("members")
    .where("uid", "==", userId)
    .get();

  memberSnap.docs.forEach((doc) => {
    const familyRef = doc.ref.parent.parent;
    if (familyRef) refs.set(familyRef.id, familyRef);
  });

  const ownedSnap = await db
    .collection("families")
    .where("adminUid", "==", userId)
    .get();

  ownedSnap.docs.forEach((doc) => refs.set(doc.id, doc.ref));
  return [...refs.values()];
}

// ═══════════════════════════════════════════════════════════════════════════
// 1. WEBHOOK STRIPE
// ═══════════════════════════════════════════════════════════════════════════

exports.stripeWebhook = onRequest(
  {
    secrets: [stripeSecretKey, stripeWebhookSecret],
    cors: { origin: ["https://www.laviapp.com.br"] }
  },
  async (req, res) => {
    try {
      if (req.method !== "POST") {
        return res.status(405).json({ error: "Method Not Allowed" });
      }

      const sig = req.headers["stripe-signature"];
      const rawBody = req.rawBody;

      if (!sig || !rawBody) {
        return res.status(400).json({ error: "Missing signature" });
      }

      const stripe = new Stripe(stripeSecretKey.value());
      const event = stripe.webhooks.constructEvent(
        rawBody,
        sig,
        stripeWebhookSecret.value()
      );

      console.log(`Webhook received: ${event.type}`);

      // Processar eventos
      if (event.type === "payment_intent.succeeded") {
        const paymentIntent = event.data.object;
        const { familyId, userId, plano } = paymentIntent.metadata || {};

        if (familyId && userId && plano) {
          const db = admin.firestore();
          await db.collection("families").doc(familyId).update({
            plan: plano,
            stripeCustomerId: paymentIntent.customer,
            paidAt: admin.firestore.FieldValue.serverTimestamp(),
            paymentStatus: "succeeded"
          });

          await db
            .collection("families")
            .doc(familyId)
            .collection("activityLogs")
            .add({
              type: "payment",
              status: "succeeded",
              plano: plano,
              amount: paymentIntent.amount,
              timestamp: admin.firestore.FieldValue.serverTimestamp(),
              userId: userId
            });

          console.log(`Payment succeeded for family ${familyId}`);
        }
      }

      if (event.type === "customer.subscription.created") {
        const subscription = event.data.object;
        const { familyId, userId, plano } = subscription.metadata || {};

        if (familyId && userId && plano) {
          const db = admin.firestore();
          await db.collection("subscriptions").doc(subscription.id).set({
            familyId: familyId,
            userId: userId,
            stripeCustomerId: subscription.customer,
            status: subscription.status,
            plano: plano,
            createdAt: new Date(subscription.created * 1000),
            currentPeriodEnd: new Date(subscription.current_period_end * 1000)
          });

          await db.collection("families").doc(familyId).update({
            stripeSubscriptionId: subscription.id,
            plan: plano
          });

          console.log(`Subscription created for family ${familyId}`);
        }
      }

      if (event.type === "customer.subscription.deleted") {
        const subscription = event.data.object;
        const familyId = subscription.metadata?.familyId;

        if (familyId) {
          const db = admin.firestore();
          const familyRef = db.collection("families").doc(familyId);
          const familySnap = await familyRef.get();
          const family = familySnap.exists ? familySnap.data() : {};
          const isLifetime =
            family.lifetime === true ||
            family.ownerPlan === true ||
            family.billingType === "owner" ||
            family.billingType === "lifetime";

          if (isLifetime) {
            await familyRef.set({
              plan: "familia",
              plano: "familia",
              planStatus: "ativa",
              stripeSubscriptionStatus: "canceled",
              stripeCanceledAt: admin.firestore.FieldValue.serverTimestamp()
            }, { merge: true });
          } else {
            await familyRef.update({
              plan: "free",
              plano: "free",
              planStatus: "cancelada",
              canceledAt: admin.firestore.FieldValue.serverTimestamp()
            });
          }

          console.log(`Subscription canceled for family ${familyId}`);
        }
      }

      res.json({ received: true });
    } catch (error) {
      console.error(`Webhook error: ${error.message}`);
      res.status(400).json({ error: error.message });
    }
  }
);

// ═══════════════════════════════════════════════════════════════════════════
// 2. DELETAR DADOS DO USUÁRIO (GDPR/LGPD)
// ═══════════════════════════════════════════════════════════════════════════

exports.deleteUserData = onRequest(
  { cors: { origin: ["https://www.laviapp.com.br"] } },
  async (req, res) => {
    try {
      const decoded = await authenticateRequest(req);
      const userId = decoded.uid;
      const db = admin.firestore();
      const auth = admin.auth();

      console.log(`Deleting data for user ${userId}`);

      const familyRefs = await getFamilyRefsForUser(db, userId);

      // Remover de famílias como membro
      for (const familyRef of familyRefs) {
        const familyDoc = await familyRef.get();
        const familyData = familyDoc.exists ? familyDoc.data() : {};
        const membros = Array.isArray(familyData.membros) ? familyData.membros : [];
        const remaining = membros.filter((m) => m && m.uid !== userId);

        await familyRef.collection("members").doc(userId).delete().catch(() => null);

        if (familyData.adminUid === userId && remaining.length > 0) {
          await familyRef.update({
            adminUid: remaining[0].uid,
            membros: remaining,
            updatedAt: admin.firestore.FieldValue.serverTimestamp()
          });
        } else {
          await familyRef.update({
            membros: remaining,
            updatedAt: admin.firestore.FieldValue.serverTimestamp()
          });
        }
      }

      // Deletar dados do usuário
      await db.collection("users").doc(userId).delete();

      // Deletar assinaturas
      const subscriptionsSnapshot = await db
        .collection("subscriptions")
        .where("userId", "==", userId)
        .get();

      for (const doc of subscriptionsSnapshot.docs) {
        await doc.ref.delete();
      }

      // Deletar do Firebase Auth
      await auth.deleteUser(userId);

      res.json({
        message: "Seus dados foram deletados permanentemente"
      });
    } catch (error) {
      console.error(`Delete error: ${error.message}`);
      res.status(error.status || 500).json({ error: error.message });
    }
  }
);

// ═══════════════════════════════════════════════════════════════════════════
// 3. EXPORTAR DADOS DO USUÁRIO
// ═══════════════════════════════════════════════════════════════════════════

exports.exportUserData = onRequest(
  { cors: { origin: ["https://www.laviapp.com.br"] } },
  async (req, res) => {
    try {
      const decoded = await authenticateRequest(req);
      const userId = decoded.uid;
      const db = admin.firestore();

      console.log(`Exporting data for user ${userId}`);

      // Dados do usuário
      const userData = await db.collection("users").doc(userId).get();

      const familyRefs = await getFamilyRefsForUser(db, userId);
      const families = [];
      for (const familyRef of familyRefs) {
        const familyDoc = await familyRef.get();
        if (!familyDoc.exists) continue;
        const members = await familyDoc.ref.collection("members").get();
        const expenses = await familyDoc.ref.collection("expenses").get();
        const incomes = await familyDoc.ref.collection("incomes").get();

        families.push({
          id: familyDoc.id,
          ...familyDoc.data(),
          members: members.docs.map((d) => ({ id: d.id, ...d.data() })),
          expenses: expenses.docs.map((d) => ({ id: d.id, ...d.data() })),
          incomes: incomes.docs.map((d) => ({ id: d.id, ...d.data() }))
        });
      }

      // Assinaturas
      const subscriptionsSnapshot = await db
        .collection("subscriptions")
        .where("userId", "==", userId)
        .get();

      const exportData = {
        exportDate: new Date().toISOString(),
        user: userData.exists ? userData.data() : null,
        families: families,
        subscriptions: subscriptionsSnapshot.docs.map((d) => ({
          id: d.id,
          ...d.data()
        }))
      };

      res.setHeader("Content-Type", "application/json");
      res.setHeader(
        "Content-Disposition",
        "attachment; filename=laviapp-dados.json"
      );

      res.send(JSON.stringify(exportData, null, 2));
    } catch (error) {
      console.error(`Export error: ${error.message}`);
      res.status(error.status || 500).json({ error: error.message });
    }
  }
);



// ═══════════════════════════════════════════════════════════════════════════
// 4. CHECKOUT STRIPE - Planos do LaviApp
// ═══════════════════════════════════════════════════════════════════════════

const CHECKOUT_ORIGINS = [
  "https://www.laviapp.com.br",
  "https://laviapp.com.br",
  "http://127.0.0.1:4173",
  "http://localhost:4173",
  "http://localhost:5000",
  "http://127.0.0.1:5000"
];

function applyCors(req, res) {
  const origin = req.headers.origin;
  if (CHECKOUT_ORIGINS.includes(origin)) {
    res.set("Access-Control-Allow-Origin", origin);
  } else {
    res.set("Access-Control-Allow-Origin", "https://www.laviapp.com.br");
  }
  res.set("Vary", "Origin");
  res.set("Access-Control-Allow-Methods", "POST, OPTIONS");
  res.set("Access-Control-Allow-Headers", "Content-Type, Authorization");
}

function maskEmailAddress(email) {
  const raw = String(email || "").trim();
  const [localPart, domainPart] = raw.split("@");
  if (!localPart || !domainPart) return raw || "email cadastrado";
  if (localPart.length <= 3) return `${localPart[0] || "*"}***@${domainPart}`;
  const startSize = localPart.length > 10 ? 6 : Math.min(3, localPart.length - 1);
  const endSize = localPart.length > 10 ? 6 : Math.min(2, Math.max(0, localPart.length - startSize));
  const start = localPart.slice(0, startSize);
  const end = endSize ? localPart.slice(-endSize) : "";
  const hidden = "*".repeat(Math.max(4, Math.min(8, localPart.length - start.length - end.length || 4)));
  return `${start}${hidden}${end}@${domainPart}`;
}

function hashEmailVerificationToken(token) {
  return crypto.createHash("sha256").update(String(token || ""), "utf8").digest("hex");
}

async function createLaviAppEmailVerificationLink(db, { uid, email }) {
  const token = crypto.randomBytes(32).toString("base64url");
  const tokenHash = hashEmailVerificationToken(token);
  const now = Date.now();
  const expiresAt = admin.firestore.Timestamp.fromMillis(now + 24 * 60 * 60 * 1000);
  await db.collection("emailVerificationTokens").doc(tokenHash).set({
    uid,
    email: String(email || "").trim().toLowerCase(),
    used: false,
    createdAt: admin.firestore.FieldValue.serverTimestamp(),
    expiresAt
  });
  return `https://www.laviapp.com.br/app.html?v=1.2.12&confirmacao=laviapp&token=${encodeURIComponent(token)}`;
}

async function assertEmailVerificationRateLimit(db, uid) {
  const ref = db.collection("rateLimits").doc(`emailVerification_${uid}`);
  const snap = await ref.get();
  const now = Date.now();
  const windowMs = 10 * 60 * 1000;
  const minGapMs = 90 * 1000;
  const maxAttempts = 4;
  const data = snap.exists ? (snap.data() || {}) : {};
  const shouldEnforce = data.lastStatus === "sent";
  const firstAt = shouldEnforce ? Number(data.firstAt || 0) : 0;
  const lastAt = shouldEnforce ? Number(data.lastAt || 0) : 0;
  const attempts = shouldEnforce ? Number(data.attempts || 0) : 0;

  if (lastAt && now - lastAt < minGapMs) {
    const error = new Error("Aguarde alguns segundos antes de reenviar a confirmacao.");
    error.status = 429;
    throw error;
  }

  const nextAttempts = firstAt && now - firstAt < windowMs ? attempts + 1 : 1;
  if (nextAttempts > maxAttempts) {
    const error = new Error("Muitas tentativas de confirmacao. Aguarde alguns minutos antes de reenviar.");
    error.status = 429;
    throw error;
  }

  return {
    ref,
    uid,
    firstAt: firstAt && now - firstAt < windowMs ? firstAt : now,
    lastAt: now,
    attempts: nextAttempts,
    expiresAt: admin.firestore.Timestamp.fromMillis(now + windowMs)
  };
}

async function recordEmailVerificationRateLimit(rateLimit) {
  await rateLimit.ref.set({
    uid: rateLimit.uid,
    firstAt: rateLimit.firstAt,
    lastAt: rateLimit.lastAt,
    attempts: rateLimit.attempts,
    lastStatus: "sent",
    expiresAt: rateLimit.expiresAt,
    updatedAt: admin.firestore.FieldValue.serverTimestamp()
  }, { merge: true });
}

function buildVerificationEmailHtml({ displayName, maskedEmail, link }) {
  const safeName = String(displayName || "Tudo bem?").replace(/[<>&"]/g, "");
  return `
  <div style="margin:0;padding:0;background:#0f172a;font-family:Arial,Helvetica,sans-serif;color:#e2e8f0">
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#0f172a;padding:28px 14px">
      <tr>
        <td align="center">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:520px;background:#111827;border:1px solid #243042;border-radius:22px;overflow:hidden">
            <tr>
              <td style="padding:26px 24px 10px">
                <div style="font-size:24px;font-weight:800;color:#f8fafc;letter-spacing:-.04em">Lavi<span style="color:#2dd4bf">App</span></div>
                <div style="font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#64748b;margin-top:6px">Finanças familiares</div>
              </td>
            </tr>
            <tr>
              <td style="padding:12px 24px 26px">
                <h1 style="margin:0 0 10px;font-size:22px;line-height:1.25;color:#f8fafc">Confirme seu e-mail</h1>
                <p style="margin:0 0 14px;font-size:15px;line-height:1.55;color:#cbd5e1">Olá, ${safeName}. Recebemos um cadastro no LaviApp usando o e-mail <strong style="color:#f8fafc">${maskedEmail}</strong>.</p>
                <p style="margin:0 0 22px;font-size:14px;line-height:1.55;color:#94a3b8">Para proteger sua família e liberar o acesso inicial, confirme que este endereço pertence a você.</p>
                <a href="${link}" style="display:block;text-align:center;background:linear-gradient(135deg,#2dd4bf,#a7f3d0);color:#0f172a;text-decoration:none;font-weight:800;font-size:15px;padding:14px 18px;border-radius:14px">Confirmar meu e-mail</a>
                <p style="margin:20px 0 0;font-size:12px;line-height:1.5;color:#64748b">Se você não criou uma conta no LaviApp, pode ignorar esta mensagem.</p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </div>`;
}

async function sendBrevoEmail({ to, name, subject, htmlContent, textContent }) {
  const key = brevoApiKey.value();
  if (!key) {
    const error = new Error("BREVO_API_KEY nao configurada nas Functions.");
    error.status = 500;
    throw error;
  }

  const senderEmail = process.env.BREVO_FROM_EMAIL || "contato@laviapp.com.br";
  const senderName = process.env.BREVO_FROM_NAME || "LaviApp";
  const response = await fetch("https://api.brevo.com/v3/smtp/email", {
    method: "POST",
    headers: {
      "accept": "application/json",
      "api-key": key,
      "content-type": "application/json"
    },
    body: JSON.stringify({
      sender: { email: senderEmail, name: senderName },
      to: [{ email: to, name: name || to }],
      replyTo: { email: senderEmail, name: senderName },
      subject,
      htmlContent,
      textContent
    })
  });

  const text = await response.text();
  let data = {};
  try { data = text ? JSON.parse(text) : {}; } catch (e) { data = { raw: text }; }
  if (!response.ok) {
    const brevoMessage = data.message || data.error || data.raw || `Brevo retornou HTTP ${response.status}`;
    const error = new Error(brevoMessage);
    error.status = 502;
    error.brevoStatus = response.status;
    error.brevoResponse = data;
    if (/unrecognised IP address|unrecognized IP address|authorised_ips|authorized_ips/i.test(String(brevoMessage))) {
      error.code = "brevo/ip-not-authorized";
      error.publicMessage = "O envio oficial do LaviApp esta em configuracao de seguranca. Aguarde a liberacao e tente novamente em alguns minutos.";
    }
    throw error;
  }
  return data;
}

const LAVIAPP_PLANOS = {
  pro: {
    nome: "LaviApp Pro",
    amount: 990,
    mode: "subscription",
    recurring: { interval: "month" }
  },
  familia: {
    nome: "LaviApp Família",
    amount: 1990,
    mode: "subscription",
    recurring: { interval: "month", interval_count: 3 }
  },
  anual: {
    nome: "LaviApp Anual",
    amount: 15900,
    mode: "subscription",
    recurring: { interval: "year" }
  }
};

exports.sendVerificationEmailBrevo = onRequest(
  { secrets: [brevoApiKey], cors: false },
  async (req, res) => {
    applyCors(req, res);
    if (req.method === "OPTIONS") return res.status(204).send("");
    if (req.method !== "POST") return res.status(405).json({ error: "Method Not Allowed" });

    try {
      const decoded = await authenticateRequest(req);
      const uid = decoded.uid;
      const email = String(decoded.email || "").trim().toLowerCase();
      if (!email) return res.status(400).json({ error: "Usuario sem e-mail cadastrado." });

      const authUser = await admin.auth().getUser(uid);
      if (authUser.emailVerified) {
        return res.json({ ok: true, alreadyVerified: true, maskedEmail: maskEmailAddress(email) });
      }

      const db = admin.firestore();
      const rateLimit = await assertEmailVerificationRateLimit(db, uid);

      const link = await createLaviAppEmailVerificationLink(db, { uid, email });
      const maskedEmail = maskEmailAddress(email);
      const displayName = String(authUser.displayName || decoded.name || "").trim();
      const htmlContent = buildVerificationEmailHtml({ displayName, maskedEmail, link });
      const textContent = [
        "LaviApp - Confirme seu e-mail",
        "",
        `Recebemos um cadastro usando ${maskedEmail}.`,
        "Para liberar o acesso inicial, abra o link abaixo:",
        link,
        "",
        "Se voce nao criou uma conta no LaviApp, ignore esta mensagem."
      ].join("\n");

      const brevo = await sendBrevoEmail({
        to: email,
        name: displayName || email,
        subject: "Confirme seu e-mail no LaviApp",
        htmlContent,
        textContent
      });

      await recordEmailVerificationRateLimit(rateLimit);

      await db.collection("mailLogs").add({
        type: "emailVerification",
        provider: "brevo",
        uid,
        emailMasked: maskedEmail,
        messageId: brevo.messageId || "",
        createdAt: admin.firestore.FieldValue.serverTimestamp()
      });

      return res.json({ ok: true, provider: "brevo", maskedEmail });
    } catch (error) {
      console.error("sendVerificationEmailBrevo error:", error);
      return res.status(error.status || 500).json({
        error: error.publicMessage || error.message || "Erro ao enviar confirmacao por e-mail.",
        code: error.code || "",
        provider: "brevo"
      });
    }
  }
);

exports.confirmVerificationEmailLaviApp = onRequest(
  { cors: false },
  async (req, res) => {
    applyCors(req, res);
    if (req.method === "OPTIONS") return res.status(204).send("");
    if (req.method !== "POST") return res.status(405).json({ error: "Method Not Allowed" });

    try {
      const token = String((req.body && req.body.token) || "").trim();
      if (!token || token.length < 32) {
        return res.status(400).json({ error: "Link de confirmacao invalido.", code: "invalid-token" });
      }

      const db = admin.firestore();
      const ref = db.collection("emailVerificationTokens").doc(hashEmailVerificationToken(token));
      const snap = await ref.get();
      if (!snap.exists) {
        return res.status(400).json({ error: "Link de confirmacao invalido ou expirado.", code: "invalid-token" });
      }

      const data = snap.data() || {};
      const expiresAt = data.expiresAt && typeof data.expiresAt.toMillis === "function"
        ? data.expiresAt.toMillis()
        : Number(data.expiresAt || 0);
      if (data.used || !expiresAt || Date.now() > expiresAt) {
        return res.status(400).json({ error: "Este link de confirmacao expirou. Solicite um novo e-mail.", code: "expired-token" });
      }

      const uid = String(data.uid || "");
      const email = String(data.email || "").trim().toLowerCase();
      if (!uid || !email) {
        return res.status(400).json({ error: "Link de confirmacao invalido.", code: "invalid-token" });
      }

      const authUser = await admin.auth().getUser(uid);
      if (String(authUser.email || "").trim().toLowerCase() !== email) {
        return res.status(400).json({ error: "Este link nao corresponde ao e-mail atual da conta.", code: "email-changed" });
      }

      if (!authUser.emailVerified) {
        await admin.auth().updateUser(uid, { emailVerified: true });
      }

      await ref.set({
        used: true,
        usedAt: admin.firestore.FieldValue.serverTimestamp()
      }, { merge: true });

      await db.collection("users").doc(uid).set({
        emailVerified: true,
        emailVerifiedAt: admin.firestore.FieldValue.serverTimestamp()
      }, { merge: true });

      return res.json({ ok: true, maskedEmail: maskEmailAddress(email) });
    } catch (error) {
      console.error("confirmVerificationEmailLaviApp error:", error);
      return res.status(error.status || 500).json({
        error: error.message || "Nao foi possivel confirmar o e-mail.",
        code: error.code || "verification-error"
      });
    }
  }
);

exports.joinFamilyByCode = onRequest(
  { cors: false },
  async (req, res) => {
    applyCors(req, res);
    if (req.method === "OPTIONS") return res.status(204).send("");
    if (req.method !== "POST") return res.status(405).json({ error: "Method Not Allowed" });

    try {
      const decoded = await authenticateRequest(req);
      if (!decoded.email_verified) {
        return res.status(403).json({ error: "Confirme seu e-mail antes de entrar na familia." });
      }
      const uid = decoded.uid;
      const { codigo, nome } = req.body || {};
      const code = String(codigo || "").trim().toUpperCase();
      const displayName = String(nome || decoded.name || decoded.email || "Membro").trim().slice(0, 80);

      if (!/^[A-Z0-9]{4,12}$/.test(code)) {
        return res.status(400).json({ error: "Codigo de familia invalido." });
      }

      const db = admin.firestore();
      const snap = await db.collection("families").where("codigo", "==", code).limit(1).get();
      if (snap.empty) {
        return res.status(404).json({ error: "Codigo de familia nao encontrado." });
      }

      const familyDoc = snap.docs[0];
      const family = familyDoc.data() || {};
      const members = Array.isArray(family.membros) ? family.membros : [];
      const alreadyMember = members.some((m) => m && m.uid === uid);
      const maxMembers = Number(family.freeLimits && family.freeLimits.maxMembers);

      if (!alreadyMember && (family.plan === "free" || family.plano === "free") && maxMembers > 0 && members.length >= maxMembers) {
        return res.status(403).json({ error: "Limite de membros do plano Free atingido." });
      }

      const memberData = {
        uid,
        nome: displayName,
        email: decoded.email || "",
        role: alreadyMember
          ? ((members.find((m) => m && m.uid === uid) || {}).role || "member")
          : "member"
      };

      const nextMembers = alreadyMember
        ? members.map((m) => (m && m.uid === uid ? { ...m, ...memberData } : m))
        : [...members, memberData];

      await familyDoc.ref.update({
        membros: nextMembers,
        updatedAt: admin.firestore.FieldValue.serverTimestamp()
      });

      await familyDoc.ref.collection("members").doc(uid).set({
        ...memberData,
        createdAt: admin.firestore.FieldValue.serverTimestamp(),
        updatedAt: admin.firestore.FieldValue.serverTimestamp()
      }, { merge: true });

      await db.collection("users").doc(uid).set({
        nome: displayName,
        email: decoded.email || "",
        familyId: familyDoc.id,
        role: memberData.role,
        updatedAt: admin.firestore.FieldValue.serverTimestamp()
      }, { merge: true });

      return res.json({
        familyId: familyDoc.id,
        familyNome: family.nome || "Minha Familia",
        role: memberData.role
      });
    } catch (error) {
      console.error("joinFamilyByCode error:", error);
      return res.status(error.status || 500).json({ error: error.message || "Erro ao entrar na familia." });
    }
  }
);

exports.createCheckoutSession = onRequest(
  { secrets: [stripeSecretKey], cors: false },
  async (req, res) => {
    applyCors(req, res);
    if (req.method === "OPTIONS") return res.status(204).send("");
    if (req.method !== "POST") return res.status(405).json({ error: "Method Not Allowed" });

    try {
      const decoded = await authenticateRequest(req);
      const { plano, familyId, userId } = req.body || {};
      const planId = String(plano || "").toLowerCase();
      const plan = LAVIAPP_PLANOS[planId];

      if (!plan) return res.status(400).json({ error: "Plano inválido." });
      if (!familyId) return res.status(400).json({ error: "familyId é obrigatório." });
      if (userId && userId !== decoded.uid) return res.status(403).json({ error: "Usuário inválido para esta sessão." });

      const db = admin.firestore();
      await requireFamilyAdmin(db, familyId, decoded.uid);

      const stripe = new Stripe(stripeSecretKey.value());
      const origin = req.headers.origin && CHECKOUT_ORIGINS.includes(req.headers.origin)
        ? req.headers.origin
        : "https://www.laviapp.com.br";

      const metadata = { familyId, userId: decoded.uid, plano: planId };
      const lineItem = {
        quantity: 1,
        price_data: {
          currency: "brl",
          product_data: { name: plan.nome },
          unit_amount: plan.amount,
          recurring: plan.recurring
        }
      };

      const session = await stripe.checkout.sessions.create({
        mode: plan.mode,
        line_items: [lineItem],
        success_url: `${origin}/app.html?payment=success&plano=${planId}`,
        cancel_url: `${origin}/app.html?payment=cancel&plano=${planId}`,
        metadata,
        subscription_data: { metadata },
        allow_promotion_codes: true
      });

      return res.json({ url: session.url });
    } catch (error) {
      console.error("createCheckoutSession error:", error);
      return res.status(error.status || 500).json({ error: error.message || "Erro ao criar sessão de pagamento." });
    }
  }
);

exports.createPortalSession = onRequest(
  { secrets: [stripeSecretKey], cors: false },
  async (req, res) => {
    applyCors(req, res);
    if (req.method === "OPTIONS") return res.status(204).send("");
    if (req.method !== "POST") return res.status(405).json({ error: "Method Not Allowed" });

    try {
      const decoded = await authenticateRequest(req);
      const { familyId } = req.body || {};
      if (!familyId) return res.status(400).json({ error: "familyId é obrigatório." });

      const db = admin.firestore();
      const { data: family } = await requireFamilyAdmin(db, familyId, decoded.uid);
      const customerId = family && family.stripeCustomerId;
      if (!customerId) return res.status(400).json({ error: "Cliente Stripe não encontrado." });

      const stripe = new Stripe(stripeSecretKey.value());
      const origin = req.headers.origin && CHECKOUT_ORIGINS.includes(req.headers.origin)
        ? req.headers.origin
        : "https://www.laviapp.com.br";
      const portal = await stripe.billingPortal.sessions.create({
        customer: customerId,
        return_url: `${origin}/app.html?assinatura=portal`
      });
      return res.json({ url: portal.url });
    } catch (error) {
      console.error("createPortalSession error:", error);
      return res.status(error.status || 500).json({ error: error.message || "Erro ao abrir portal de assinatura." });
    }
  }
);

// ═══════════════════════════════════════════════════════════════════════════
// 5. CLEANUP DE RATE LIMITS (A cada 6 horas)
// ═══════════════════════════════════════════════════════════════════════════

exports.cleanupRateLimits = onSchedule(
  "every 6 hours",
  async (context) => {
    try {
      console.log("Cleaning up rate limits...");

      const db = admin.firestore();
      const now = Math.floor(Date.now() / 1000);
      const maxAge = 24 * 3600;

      const snapshot = await db
        .collection("rateLimits")
        .where("lastRequest", "<", now - maxAge)
        .get();

      let count = 0;
      for (const doc of snapshot.docs) {
        await doc.ref.delete();
        count++;
      }

      console.log(`Cleaned ${count} rate limit records`);
    } catch (error) {
      console.error(`Cleanup error: ${error.message}`);
    }
  }
);

// ═══════════════════════════════════════════════════════════════════════════
// FIM
// ═══════════════════════════════════════════════════════════════════════════
