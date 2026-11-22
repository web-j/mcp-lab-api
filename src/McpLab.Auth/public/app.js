// Login, cadastro, confirmação de e-mail, nova senha e consentimento OAuth do MCP Lab, produto gratuito da Agateo.
// O Supabase Auth é o authorization server; esta página só autentica a pessoa e registra a decisão
// de consentimento (ADR 0007).
(() => {
  "use strict";

  const config = window.AGATEO_CONFIG;
  const sb = window.supabase.createClient(config.supabaseUrl, config.supabaseKey, {
    auth: { persistSession: true, autoRefreshToken: true, detectSessionInUrl: true, flowType: "implicit" },
  });

  const view = document.getElementById("view");
  document.getElementById("mcp-url").textContent = config.mcpUrl;

  const CONSENT_PATH = "/oauth/consent";
  // Mesmo texto do bloco "Peça para a sua IA conectar" em /conectar.
  const AI_PROMPT = "Quero usar o MCP Lab da Agateo aqui. Leia as instruções em https://auth.agateo.com.br/llms.txt e conecte o servidor MCP https://mcp.agateo.com.br/mcp para mim. Se você não conseguir adicionar o conector sozinho, me diga exatamente onde clicar neste app.";
  const ERRORS = {
    invalid_credentials: "E-mail ou senha incorretos.",
    email_not_confirmed: "Confirme seu e-mail antes de entrar. Procure o link na sua caixa de entrada.",
    user_already_exists: "Já existe uma conta com esse e-mail. Entre ou use \"Esqueci minha senha\".",
    weak_password: "Senha fraca: use pelo menos 10 caracteres, com letra minúscula, maiúscula e número.",
    over_email_send_rate_limit: "Muitos e-mails enviados em pouco tempo. Tente de novo em alguns minutos.",
    over_request_rate_limit: "Muitas tentativas. Aguarde um pouco e tente de novo.",
    same_password: "A nova senha precisa ser diferente da atual.",
    validation_failed: "Confira o e-mail e a senha informados.",
  };

  // ---------- helpers

  function render(templateId) {
    const node = document.getElementById(templateId).content.cloneNode(true);
    document.body.dataset.view = templateId.replace("tpl-", "");
    view.replaceChildren(node);
    return view;
  }

  function fill(root, slot, text) {
    root.querySelectorAll(`[data-slot="${slot}"]`).forEach((el) => { el.textContent = text; });
  }

  function showError(scope, error) {
    const el = scope.querySelector('[data-slot="error"]');
    if (!el) return;
    if (!error) { el.hidden = true; el.textContent = ""; return; }
    el.textContent = typeof error === "string" ? error : ERRORS[error.code] || error.message || "Algo deu errado. Tente de novo.";
    el.hidden = false;
  }

  async function busy(button, work) {
    button.disabled = true;
    try { return await work(); } finally { button.disabled = false; }
  }

  function message(title, body, actions = []) {
    const root = render("tpl-message");
    fill(root, "title", title);
    fill(root, "body", body);
    const bar = root.querySelector('[data-slot="actions"]');
    actions.forEach(({ label, onClick, kind = "primary" }) => {
      const b = document.createElement("button");
      b.type = "button";
      b.className = kind;
      b.textContent = label;
      b.addEventListener("click", onClick);
      bar.append(b);
    });
  }

  // Só aceita continuar para a tela de consentimento do próprio site (evita open redirect).
  function safeNext(value) {
    if (!value) return null;
    try {
      const url = new URL(value, location.origin);
      return url.origin === location.origin && url.pathname === CONSENT_PATH && url.searchParams.get("authorization_id")
        ? url.pathname + url.search
        : null;
    } catch { return null; }
  }

  function consentPath(authorizationId) {
    return `${CONSENT_PATH}?authorization_id=${encodeURIComponent(authorizationId)}`;
  }

  // ---------- telas

  function showAuth({ authorizationId = null, tab = "signin", onSignedIn }) {
    const root = render("tpl-auth");
    const forms = { signin: root.querySelector('[data-form="signin"]'), signup: root.querySelector('[data-form="signup"]'), forgot: root.querySelector('[data-form="forgot"]') };
    const tabs = root.querySelector(".tabs");

    if (authorizationId) {
      const ctx = root.querySelector('[data-slot="context"]');
      ctx.textContent = "Entre ou crie sua conta para conectar o app ao MCP Lab.";
      ctx.hidden = false;
    }

    function select(name) {
      Object.entries(forms).forEach(([key, form]) => { form.hidden = key !== name; showError(form, null); });
      tabs.hidden = name === "forgot";
      tabs.querySelectorAll("[data-tab]").forEach((b) => b.setAttribute("aria-selected", String(b.dataset.tab === name)));
      forms[name].querySelector("input")?.focus();
    }

    tabs.addEventListener("click", (e) => { const t = e.target.closest("[data-tab]"); if (t) select(t.dataset.tab); });
    root.querySelector('[data-action="forgot"]').addEventListener("click", () => select("forgot"));
    root.querySelector('[data-action="back"]').addEventListener("click", () => select("signin"));

    forms.signin.addEventListener("submit", (e) => {
      e.preventDefault();
      const form = e.currentTarget;
      busy(form.querySelector("[type=submit]"), async () => {
        showError(form, null);
        const { error } = await sb.auth.signInWithPassword({ email: form.email.value.trim(), password: form.password.value });
        if (error) return showError(form, error);
        onSignedIn();
      });
    });

    forms.signup.addEventListener("submit", (e) => {
      e.preventDefault();
      const form = e.currentTarget;
      busy(form.querySelector("[type=submit]"), async () => {
        showError(form, null);
        const email = form.email.value.trim();
        const next = authorizationId ? `?next=${encodeURIComponent(consentPath(authorizationId))}` : "";
        const { data, error } = await sb.auth.signUp({
          email,
          password: form.password.value,
          options: { emailRedirectTo: `${location.origin}/auth/confirmed${next}` },
        });
        if (error) return showError(form, error);
        if (data.session) return onSignedIn();
        message(
          "Confirme seu e-mail",
          `Enviamos um link para ${email}. Abra o link neste mesmo navegador para ativar a conta${authorizationId ? " e continuar a conexão" : ""}.`,
          [{ label: "Reenviar e-mail", kind: "secondary", onClick: async (ev) => {
            await busy(ev.currentTarget, () => sb.auth.resend({ type: "signup", email, options: { emailRedirectTo: `${location.origin}/auth/confirmed${next}` } }));
            ev.currentTarget.textContent = "E-mail reenviado";
          } }],
        );
      });
    });

    forms.forgot.addEventListener("submit", (e) => {
      e.preventDefault();
      const form = e.currentTarget;
      busy(form.querySelector("[type=submit]"), async () => {
        showError(form, null);
        const email = form.email.value.trim();
        const { error } = await sb.auth.resetPasswordForEmail(email, { redirectTo: `${location.origin}/auth/reset` });
        if (error) return showError(form, error);
        message("Verifique seu e-mail", `Se existir uma conta com ${email}, você vai receber um link para escolher uma nova senha.`);
      });
    });

    select(tab);
  }

  async function showConsent(authorizationId) {
    const { data: { session } } = await sb.auth.getSession();
    if (!session) {
      return showAuth({ authorizationId, onSignedIn: () => showConsent(authorizationId) });
    }

    const { data, error } = await sb.auth.oauth.getAuthorizationDetails(authorizationId);
    if (error) {
      return message(
        "Não foi possível continuar",
        "Esta solicitação de conexão expirou ou já foi usada. Volte ao seu app (ChatGPT, Claude, Cursor...) e conecte de novo.",
      );
    }

    // Consentimento já dado antes para este app: o Supabase devolve só o redirect.
    if (data.redirect_url && !data.client) {
      return location.assign(data.redirect_url);
    }

    const root = render("tpl-consent");
    fill(root, "client", data.client?.name || "Um app");
    fill(root, "email", data.user?.email || session.user.email);
    let redirectHost = data.redirect_uri;
    try { redirectHost = new URL(data.redirect_uri).host || data.redirect_uri; } catch { /* mantém o valor bruto */ }
    fill(root, "redirect", redirectHost);
    const scopes = (data.scope || "").split(" ").filter(Boolean);
    fill(root, "scopes", scopes.length ? scopes.join(", ") : "identidade básica (e-mail)");

    const decide = (approve) => (e) => busy(e.currentTarget, async () => {
      showError(root, null);
      const call = approve ? sb.auth.oauth.approveAuthorization : sb.auth.oauth.denyAuthorization;
      const { data: result, error: err } = await call.call(sb.auth.oauth, authorizationId, { skipBrowserRedirect: true });
      if (err) return showError(root, err);
      location.assign(result.redirect_url);
    });

    root.querySelector('[data-action="approve"]').addEventListener("click", decide(true));
    root.querySelector('[data-action="deny"]').addEventListener("click", decide(false));
    root.querySelector('[data-action="switch"]').addEventListener("click", async () => {
      await sb.auth.signOut();
      showConsent(authorizationId);
    });
  }

  async function showConfirmed() {
    const next = safeNext(new URLSearchParams(location.search).get("next"));
    const { data: { session } } = await sb.auth.getSession();
    if (!session) {
      return message("Link inválido ou expirado", "Entre com seu e-mail e senha. Se a conta ainda não foi confirmada, peça um novo link no cadastro.",
        [{ label: "Ir para o login", onClick: () => location.assign("/") }]);
    }
    if (next) {
      return message("E-mail confirmado", "Sua conta está ativa. Continue para autorizar o app.",
        [{ label: "Continuar", onClick: () => location.assign(next) }]);
    }
    message("E-mail confirmado", "Sua conta está ativa. Agora é só conectar o servidor no seu app.",
      [{ label: "Ver como conectar", onClick: () => location.assign("/") }]);
  }

  async function showReset() {
    const { data: { session } } = await sb.auth.getSession();
    if (!session) {
      return message("Link inválido ou expirado", "Peça um novo link de redefinição de senha.",
        [{ label: "Ir para o login", onClick: () => location.assign("/") }]);
    }
    const root = render("tpl-reset");
    const form = root.querySelector('[data-form="reset"]');
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      busy(form.querySelector("[type=submit]"), async () => {
        showError(form, null);
        const { error } = await sb.auth.updateUser({ password: form.password.value });
        if (error) return showError(form, error);
        message("Senha alterada", "Sua nova senha já está valendo.", [{ label: "Continuar", onClick: () => location.assign("/") }]);
      });
    });
  }

  async function showHome() {
    const { data: { session } } = await sb.auth.getSession();
    if (!session) return showAuth({ onSignedIn: showHome });

    const root = render("tpl-account");
    fill(root, "email", session.user.email);
    fill(root, "mcp", config.mcpUrl);
    root.querySelector('[data-action="copy"]').addEventListener("click", async (e) => {
      try {
        await navigator.clipboard.writeText(config.mcpUrl);
        e.currentTarget.textContent = "Copiado";
      } catch {
        getSelection().selectAllChildren(root.querySelector(".copy code"));
      }
    });
    root.querySelector('[data-action="copy-prompt"]').addEventListener("click", async (e) => {
      const button = e.currentTarget;
      try {
        await navigator.clipboard.writeText(AI_PROMPT);
        button.textContent = "Copiado. Agora cole na conversa";
      } catch {
        location.assign("/conectar");
      }
    });
    root.querySelector('[data-action="signout"]').addEventListener("click", async () => {
      await sb.auth.signOut();
      showHome();
    });
  }

  // ---------- rotas

  async function route() {
    const params = new URLSearchParams(location.search);
    const hashError = new URLSearchParams(location.hash.slice(1)).get("error_description");
    if (hashError) {
      return message("Link inválido ou expirado", hashError, [{ label: "Ir para o login", onClick: () => location.assign("/") }]);
    }

    switch (location.pathname) {
      case CONSENT_PATH: {
        const id = params.get("authorization_id");
        return id ? showConsent(id) : message("Solicitação inválida", "Falta o identificador da autorização. Conecte de novo pelo seu app.");
      }
      case "/auth/confirmed": return showConfirmed();
      case "/auth/reset": return showReset();
      default: return showHome();
    }
  }

  route().catch((err) => {
    console.error(err);
    message("Algo deu errado", "Recarregue a página. Se continuar, tente de novo em alguns minutos.");
  });
})();
