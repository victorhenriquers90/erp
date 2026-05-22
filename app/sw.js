const CACHE_NAME = "laviapp-shell-v1.2.42";
const APP_SHELL = [
  "/",
  "/app.html?v=1.2.42",
  "/manifest.json?v=1.2.42",
  "/stripe-config.js",
  "/legal.css?v=1.0",
  "/termos.html",
  "/privacidade.html",
  "/suporte.html",
  "/icon-192.png",
  "/icon-192-1778584824271.png",
  "/icon-512-1778584824271.png"
];

self.addEventListener("install", event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(APP_SHELL))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(
        keys
          .filter(key => key.startsWith("laviapp-shell-") && key !== CACHE_NAME)
          .map(key => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("message", event => {
  if (event.data === "skipWaiting") self.skipWaiting();
});

self.addEventListener("fetch", event => {
  const req = event.request;
  if (req.method !== "GET") return;

  const url = new URL(req.url);
  if (url.origin !== self.location.origin) return;

  if (req.mode === "navigate") {
    event.respondWith(
      fetch(req)
        .then(resp => {
          const copy = resp.clone();
          caches.open(CACHE_NAME).then(cache => cache.put("/app.html?v=1.2.42", copy));
          return resp;
        })
        .catch(() => caches.match("/app.html?v=1.2.42"))
    );
    return;
  }

  event.respondWith(
    caches.match(req).then(cached => {
      if (cached) return cached;
      return fetch(req).then(resp => {
        if (resp && resp.ok) {
          const copy = resp.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(req, copy));
        }
        return resp;
      });
    })
  );
});
