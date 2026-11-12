// Cloudflare は 1 つのファイルを 25 MiB までしか置けないので、書き出しの大きいファイルは site.py が分けて置く。
// 元の名前で求められたら、ここで順につないで返す。置いたファイルに当たる道は Worker を通らずに Cloudflare が返し、
// ここに来るのは当たらない道だけ。
export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);
    const index = await env.ASSETS.fetch(new URL("/parts.json", url));
    const entry = index.ok ? (await index.json())[url.pathname] : undefined;
    if (!entry) {
      return new Response("見つかりません", { status: 404, headers: { "Content-Type": "text/plain; charset=utf-8" } });
    }

    // 中身が変わっていなければ 304 を返し、ブラウザや Unity が持っている物を使わせる
    const headers = { "Content-Type": "application/octet-stream", "Cache-Control": "no-cache", ETag: `"${entry.hash}"` };
    if (request.headers.get("If-None-Match") === headers.ETag) return new Response(null, { status: 304, headers });

    // 長さを先に伝える。Unity は読み込みの進みを、長さと届いた量から出す
    const { readable, writable } = new FixedLengthStream(entry.size);
    ctx.waitUntil(join(env, url, entry.parts, writable));
    return new Response(readable, { headers });
  },
};

// 分けたファイルを順に流し込む。欠けていたら流れを壊し、途中までの中身を正しい物として渡さない
async function join(env, url, count, writable) {
  try {
    for (let i = 0; i < count; i++) {
      const part = await env.ASSETS.fetch(new URL(`${url.pathname}.${i}`, url));
      if (!part.ok) throw new Error(`${url.pathname}.${i} が無い（${part.status}）`);
      await part.body.pipeTo(writable, { preventClose: i < count - 1 });
    }
  } catch (error) {
    await writable.abort(error).catch(() => {});
  }
}
