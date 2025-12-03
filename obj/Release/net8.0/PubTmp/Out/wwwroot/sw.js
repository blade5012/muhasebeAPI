const CACHE_NAME = 'muhasebe-api-cache-v3';
const urlsToCache = [
  '/',
  '/login.html',
  '/register.html',
  // '/resetpassword.html', // Dinamik token içerdiği için önbelleğe alınmayacak
  '/kategoriler.html',
  '/gelirgider.html',
  '/grafik.html',
  // '/forgotpassword.html', // Dinamik veri içerebilir, önbelleğe alınmayacak
  '/config.js',
  '/app.js',
  '/login.js',
  '/register.js',
  '/reset.js',
  '/forgot.js',
  '/logo.png',
  '/icons/favicon.ico',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/icons/apple-touch-icon-180.png',
  // Diğer statik dosyalarınızı buraya ekleyin (CSS, diğer JS dosyaları vb.)
  // Örneğin: 'css/style.css', 'js/main.js'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('Önbellek açıldı');
        return cache.addAll(urlsToCache);
      })
  );
});

self.addEventListener('fetch', event => {
  event.respondWith(
    caches.match(event.request)
      .then(response => {
        if (response) {
          return response; // Önbellekten döndür
        }
        return fetch(event.request); // Ağdan iste
      })
  );
});

self.addEventListener('activate', event => {
  const cacheWhitelist = [CACHE_NAME];
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => {
          if (cacheWhitelist.indexOf(cacheName) === -1) {
            return caches.delete(cacheName); // Eski önbellekleri sil
          }
        })
      );
    })
  );
});
