/*
 * City / Area picker — used on Register (both Employer & Employee sections),
 * Employee Profile and Employer Profile.
 *
 * Markup contract:
 *   City <select>:  data-city-picker  [data-area-target="<id of paired area field>"]  [data-initial-value="<existing city name, for edit forms>"]
 *   Area <input|textarea>: data-area-picker  data-city-source="<id of paired city select>"
 *
 * The City <select> is populated from /api/locations/cities on page load.
 * Typing in the Area field queries /api/locations/areas?cityId=X&q=... and
 * shows a click-to-fill suggestion list, scoped to whichever city is
 * currently selected in the paired dropdown.
 */
(function () {
    var citiesCache = null;

    function getCities() {
        if (citiesCache) return Promise.resolve(citiesCache);
        return fetch('/api/locations/cities')
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (data) { citiesCache = data; return data; })
            .catch(function () { return []; });
    }

    function initCitySelect(select) {
        var initialValue = (select.getAttribute('data-initial-value') || '').trim().toLowerCase();

        getCities().then(function (cities) {
            var placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = 'Select City';
            select.appendChild(placeholder);

            cities.forEach(function (c) {
                var opt = document.createElement('option');
                opt.value = c.cityName;
                opt.textContent = c.cityName;
                opt.setAttribute('data-city-id', c.cityId);
                if (initialValue && c.cityName.trim().toLowerCase() === initialValue) {
                    opt.selected = true;
                }
                select.appendChild(opt);
            });
        });
    }

    function currentCityId(citySelect) {
        var opt = citySelect.options[citySelect.selectedIndex];
        return opt ? opt.getAttribute('data-city-id') : null;
    }

    function closeList(list) {
        list.innerHTML = '';
        list.style.display = 'none';
    }

    function initAreaField(field) {
        var citySourceId = field.getAttribute('data-city-source');
        var citySelect = citySourceId ? document.getElementById(citySourceId) : null;

        var wrap = document.createElement('div');
        wrap.className = 'k-autocomplete-wrap';
        field.parentNode.insertBefore(wrap, field);
        wrap.appendChild(field);

        var list = document.createElement('div');
        list.className = 'k-autocomplete-list';
        list.style.display = 'none';
        wrap.appendChild(list);

        var debounceHandle = null;

        function runSearch() {
            var query = field.value.trim();
            var cityId = citySelect ? currentCityId(citySelect) : null;

            if (!cityId) {
                closeList(list);
                return;
            }

            fetch('/api/locations/areas?cityId=' + encodeURIComponent(cityId) + '&q=' + encodeURIComponent(query))
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (areas) {
                    if (!areas.length) { closeList(list); return; }

                    list.innerHTML = '';
                    areas.forEach(function (a) {
                        var item = document.createElement('div');
                        item.className = 'k-autocomplete-item';
                        item.textContent = a.areaName;
                        item.addEventListener('mousedown', function (e) {
                            e.preventDefault(); // keep focus so blur doesn't fire first
                            field.value = a.areaName;
                            closeList(list);
                        });
                        list.appendChild(item);
                    });
                    list.style.display = 'block';
                })
                .catch(function () { closeList(list); });
        }

        field.addEventListener('input', function () {
            clearTimeout(debounceHandle);
            debounceHandle = setTimeout(runSearch, 250);
        });

        field.addEventListener('focus', function () {
            if (field.value.trim().length > 0) runSearch();
        });

        field.addEventListener('blur', function () {
            setTimeout(function () { closeList(list); }, 100);
        });

        if (citySelect) {
            citySelect.addEventListener('change', function () {
                field.value = '';
                closeList(list);
            });
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-city-picker]').forEach(initCitySelect);
        document.querySelectorAll('[data-area-picker]').forEach(initAreaField);
    });
})();
