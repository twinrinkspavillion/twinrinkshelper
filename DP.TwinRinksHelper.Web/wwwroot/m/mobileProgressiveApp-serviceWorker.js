importScripts('https://storage.googleapis.com/workbox-cdn/releases/4.1.1/workbox-sw.js');

workbox.routing.registerRoute(
    /\.(?:png|jpg|css|js|json|html)$/,
    new workbox.strategies.StaleWhileRevalidate()
);

workbox.routing.registerRoute(
    new RegExp('/api/'),
    new workbox.strategies.StaleWhileRevalidate()
);