// 誕生日を知っている人にだけ管理のページを見せる。機械での総当たりは Turnstile で止める（[D-41]）。
// 誕生日と Turnstile の秘密鍵は git に入れず、Worker の secret（PASSWORD と TURNSTILE_SECRET）に置く。
// 設定がそろっていなければ誰も入れない。
const COOKIE = "admin";

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (birthday(env.PASSWORD).length !== 8 || !env.TURNSTILE_SECRET || !env.TURNSTILE_SITEKEY) {
      return new Response("まだ設定が済んでいません", { status: 503 });
    }
    const token = await digest(birthday(env.PASSWORD));

    if (readCookie(request) === token) {
      if (url.pathname === "/") return Response.redirect(new URL("/admin", url), 302);
      return env.ASSETS.fetch(request);
    }
    if (request.method !== "POST") return loginPage(env, "", 200);

    const form = await request.formData();
    if (!(await human(form.get("cf-turnstile-response"), env))) {
      return loginPage(env, "確認がうまくいきませんでした。少し待ってから、もう一度お試しください。", 403);
    }
    if ((await digest(birthday(form.get("birthday")))) !== token) {
      return loginPage(env, "生年月日が一致しませんでした。もう一度お試しください。", 401);
    }
    return new Response(null, {
      status: 303,
      headers: {
        Location: url.pathname,
        "Set-Cookie": `${COOKIE}=${token}; Path=/; Max-Age=2592000; HttpOnly; Secure; SameSite=Lax`,
      },
    });
  },
};

// 2000/1/1 や全角の数字も 20000101 にそろえる
function birthday(text) {
  const parts = String(text ?? "").normalize("NFKC").split(/\D+/).filter(Boolean);
  if (parts.length === 3) return parts[0] + parts[1].padStart(2, "0") + parts[2].padStart(2, "0");
  return parts.join("");
}

async function human(response, env) {
  if (!response) return false;
  const body = new FormData();
  body.append("secret", env.TURNSTILE_SECRET);
  body.append("response", response);
  const result = await fetch("https://challenges.cloudflare.com/turnstile/v0/siteverify", { method: "POST", body });
  return (await result.json()).success === true;
}

async function digest(text) {
  const bytes = await crypto.subtle.digest("SHA-256", new TextEncoder().encode("memorial-floor-admin:" + text));
  return [...new Uint8Array(bytes)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

function readCookie(request) {
  const header = request.headers.get("Cookie") ?? "";
  const hit = header.split(/;\s*/).find((part) => part.startsWith(COOKIE + "="));
  return hit ? hit.slice(COOKIE.length + 1) : null;
}

function loginPage(env, message, status) {
  const html = `<!doctype html>
<html lang="ja"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex"><title>Memorial Floor</title>
<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
<style>
:root { color-scheme: light dark; --bg: #f5f4f0; --card: #fff; --text: #222; --sub: #666; --line: #d8d6d0; --accent: #2f6f5e; --on-accent: #fff; --error: #b3261e; }
@media (prefers-color-scheme: dark) {
  :root { --bg: #161616; --card: #1f1f1f; --text: #eee; --sub: #aaa; --line: #3a3a3a; --accent: #7cc4ad; --on-accent: #10201b; --error: #ff8a80; }
}
body { background: var(--bg); color: var(--text); font-family: system-ui, sans-serif; line-height: 1.7; margin: 0; min-height: 100vh; display: grid; place-items: center; }
main { box-sizing: border-box; width: min(420px, 100% - 32px); margin: 16px 0; padding: 28px clamp(12px, 3.5vw, 24px); background: var(--card); border: 1px solid var(--line); border-radius: 12px; }
h1 { font-size: 1.5rem; margin: 0 0 12px; }
p { margin: 0 0 8px; }
form { display: grid; gap: 10px; margin-top: 20px; }
label small { color: var(--sub); }
input, button { font: inherit; padding: 10px 12px; border-radius: 8px; }
input { border: 1px solid var(--line); background: transparent; color: inherit; }
button { border: none; background: var(--accent); color: var(--on-accent); cursor: pointer; }
.error { color: var(--error); margin: 0; }
</style></head><body><main>
<h1>Memorial Floor</h1>
<p>個人で開発しているゲーム「Memorial Floor」の、企画・仕組み・設計判断をまとめたページです。</p>
<p>採用ご担当の方、知り合いの方は、ぜひご覧ください。</p>
<form method="post">
<label for="b">作者の生年月日を 8 桁で入れてください <small>（例: 20000101）</small></label>
<input id="b" name="birthday" type="text" inputmode="numeric" autocomplete="off" autofocus required>
<div class="cf-turnstile" data-sitekey="${env.TURNSTILE_SITEKEY}" data-language="ja" data-size="flexible"></div>
${message ? `<p class="error">${message}</p>` : ""}
<button>ページを見る</button>
</form></main></body></html>`;
  return new Response(html, { status, headers: { "Content-Type": "text/html; charset=utf-8" } });
}
