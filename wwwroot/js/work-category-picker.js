/*
 * Worker Register page picker: Work Category -> Skills.
 *
 * Work Category no longer depends on an Industry/Business Category
 * selection -- the Worker Register form asks for Work Category directly,
 * loading ALL active work categories in one call. Skills still cascade
 * from Work Category, same as before.
 *
 * Markup contract:
 *   Work Category <select>: data-work-category-picker
 *   Skills chip container:  data-skills-picker  data-work-category-source="<id of Work Category select>"
 *                            (renders "SelectedSkillIds" checkboxes into itself)
 *
 * Work Categories load from  GET /api/workcategories/work-categories/all
 * Choosing a Work Category loads GET /api/workcategories/skills?workCategoryId=X
 */
(function () {
    function clearSelect(select, placeholderText) {
        select.innerHTML = '';
        var placeholder = document.createElement('option');
        placeholder.value = '';
        placeholder.textContent = placeholderText;
        select.appendChild(placeholder);
    }

    function initWorkCategorySelect(select) {
        var initialValue = select.getAttribute('data-initial-value') || '';

        select.disabled = false;
        clearSelect(select, 'Loading work categories...');

        fetch('/api/workcategories/work-categories/all')
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (workCategories) {
                clearSelect(select, 'Select Work Category');

                workCategories.forEach(function (wc) {
                    var opt = document.createElement('option');
                    opt.value = wc.workCategoryId;
                    opt.textContent = wc.categoryName;
                    if (initialValue && String(wc.workCategoryId) === String(initialValue)) {
                        opt.selected = true;
                    }
                    select.appendChild(opt);
                });

                // If restoring a previously-chosen value (e.g. validation round-trip), cascade forward.
                if (select.value) {
                    select.dispatchEvent(new Event('change'));
                }
            })
            .catch(function () {
                clearSelect(select, 'Could not load work categories');
            });
    }

    function initSkillsPicker(container) {
        var sourceId = container.getAttribute('data-work-category-source');
        var sourceSelect = sourceId ? document.getElementById(sourceId) : null;
        if (!sourceSelect) return;

        var initialSkillIds = (container.getAttribute('data-initial-skill-ids') || '')
            .split(',')
            .map(function (s) { return s.trim(); })
            .filter(function (s) { return s.length > 0; });

        function renderSkills() {
            var workCategoryId = sourceSelect.value;

            if (!workCategoryId) {
                container.innerHTML = '<p class="k-form__hint">Select a Work Category above to see relevant skills.</p>';
                return;
            }

            container.innerHTML = '<p class="k-form__hint">Loading skills...</p>';

            fetch('/api/workcategories/skills?workCategoryId=' + encodeURIComponent(workCategoryId))
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (skills) {
                    if (!skills.length) {
                        container.innerHTML = '<p class="k-form__hint">No skills are mapped to this Work Category yet.</p>';
                        return;
                    }

                    container.innerHTML = '';
                    skills.forEach(function (s) {
                        var label = document.createElement('label');
                        label.className = 'k-checkbox-chip';

                        var input = document.createElement('input');
                        input.type = 'checkbox';
                        input.name = 'SelectedSkillIds';
                        input.value = s.skillId;
                        if (initialSkillIds.indexOf(String(s.skillId)) !== -1) {
                            input.checked = true;
                        }

                        label.appendChild(input);
                        label.appendChild(document.createTextNode(s.skillName));
                        container.appendChild(label);
                    });

                    initialSkillIds = []; // only restore once

                    // Dynamically-added inputs aren't picked up by jQuery
                    // Unobtrusive Validation's initial DOM parse -- re-parse
                    // this container so client-side validation (if any is
                    // configured for SelectedSkillIds) actually runs.
                    if (window.jQuery && window.jQuery.validator && window.jQuery.validator.unobtrusive) {
                        window.jQuery.validator.unobtrusive.parse(container);
                    }
                })
                .catch(function () {
                    container.innerHTML = '<p class="k-form__hint">Could not load skills. Please try again.</p>';
                });
        }

        sourceSelect.addEventListener('change', renderSkills);
        renderSkills();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-work-category-picker]').forEach(initWorkCategorySelect);
        document.querySelectorAll('[data-skills-picker]').forEach(initSkillsPicker);
    });
})();
