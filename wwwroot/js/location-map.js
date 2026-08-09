// ── Kaarigar Interactive Location Map Picker (Google Maps) ─────────────────
// Lets employers / workers click on a map or drag a pin to set their exact
// location, instead of relying only on the single-line "Use My Current
// Location" GPS button.
//
// Google Maps JS API loads asynchronously (via the `callback=` param on the
// <script> tag in _DashboardLayout.cshtml), so page code may call
// initKaarigarLocationMap() before `google.maps` exists yet. To handle that,
// this returns a lightweight controller immediately; if the real map isn't
// ready yet, calls are queued and replayed once Google Maps finishes loading
// (onKaarigarMapsReady fires the queue).
//
// Usage:
//   const picker = initKaarigarLocationMap({
//       mapId: 'locationMap',
//       latInputId: 'latitudeInput',
//       lngInputId: 'longitudeInput',
//       previewId: 'gpsCoordsPreview',      // optional
//       defaultLat: 22.3072,                // fallback center (Vadodara)
//       defaultLng: 73.1812,
//       onLocationChange: function (lat, lng) { ... } // optional
//   });
//
//   // later, e.g. after "Use My Current Location" succeeds:
//   picker.setLatLng(lat, lng);

window.__kMapInitQueue = window.__kMapInitQueue || [];

function initKaarigarLocationMap(config) {
    const controller = {
        _real: null,
        _pending: null,
        setLatLng: function (lat, lng, skipCallback) {
            if (this._real) {
                this._real.setLatLng(lat, lng, skipCallback);
            } else {
                this._pending = [lat, lng, skipCallback];
            }
        }
    };

    function tryInit() {
        if (window.google && window.google.maps) {
            controller._real = createGoogleLocationMap(config);
            if (controller._pending && controller._real) {
                controller._real.setLatLng.apply(null, controller._pending);
                controller._pending = null;
            }
        } else {
            window.__kMapInitQueue.push(tryInit);
        }
    }

    tryInit();

    // Fallback: if Google Maps still hasn't loaded after a while (missing/invalid
    // API key, network blocked, etc.), show a friendly message instead of a blank box.
    setTimeout(function () {
        if (!controller._real) {
            const mapEl = document.getElementById(config.mapId);
            if (mapEl && !mapEl.querySelector('.k-map-fallback')) {
                mapEl.innerHTML =
                    '<div class="k-map-fallback">Map could not be loaded. ' +
                    'You can still use the "Use My Current Location" button above, ' +
                    'or contact support if this keeps happening.</div>';
            }
        }
    }, 8000);

    return controller;
}

// Called by the Google Maps script tag once the API has finished loading.
function onKaarigarMapsReady() {
    const queue = window.__kMapInitQueue || [];
    window.__kMapInitQueue = [];
    queue.forEach(function (fn) { fn(); });
}

function createGoogleLocationMap(config) {
    const mapEl = document.getElementById(config.mapId);
    if (!mapEl) {
        return null;
    }

    const latInput = document.getElementById(config.latInputId);
    const lngInput = document.getElementById(config.lngInputId);
    const preview = config.previewId ? document.getElementById(config.previewId) : null;

    const existingLat = parseFloat(latInput && latInput.value);
    const existingLng = parseFloat(lngInput && lngInput.value);
    const hasExisting = !isNaN(existingLat) && !isNaN(existingLng) && (existingLat !== 0 || existingLng !== 0);

    const startLat = hasExisting ? existingLat : (config.defaultLat || 22.3072);
    const startLng = hasExisting ? existingLng : (config.defaultLng || 73.1812);
    const startZoom = hasExisting ? 15 : 12;

    const map = new google.maps.Map(mapEl, {
        center: { lat: startLat, lng: startLng },
        zoom: startZoom,
        streetViewControl: false,
        mapTypeControl: false,
        fullscreenControl: false,
        clickableIcons: false
    });

    const marker = new google.maps.Marker({
        position: { lat: startLat, lng: startLng },
        map: map,
        draggable: true
    });

    function updateFromLatLng(lat, lng) {
        if (latInput) latInput.value = lat;
        if (lngInput) lngInput.value = lng;

        if (preview) {
            preview.textContent = 'Location selected: ' + lat.toFixed(6) + ', ' + lng.toFixed(6);
            preview.style.display = 'block';
        }

        if (typeof config.onLocationChange === 'function') {
            config.onLocationChange(lat, lng);
        }
    }

    marker.addListener('dragend', function () {
        const pos = marker.getPosition();
        updateFromLatLng(pos.lat(), pos.lng());
    });

    map.addListener('click', function (e) {
        marker.setPosition(e.latLng);
        updateFromLatLng(e.latLng.lat(), e.latLng.lng());
    });

    return {
        map: map,
        marker: marker,
        setLatLng: function (lat, lng, skipCallback) {
            const pos = { lat: lat, lng: lng };
            map.panTo(pos);
            map.setZoom(15);
            marker.setPosition(pos);

            if (!skipCallback) {
                updateFromLatLng(lat, lng);
            } else {
                if (latInput) latInput.value = lat;
                if (lngInput) lngInput.value = lng;
                if (preview) {
                    preview.textContent = 'Location selected: ' + lat.toFixed(6) + ', ' + lng.toFixed(6);
                    preview.style.display = 'block';
                }
            }
        }
    };
}
