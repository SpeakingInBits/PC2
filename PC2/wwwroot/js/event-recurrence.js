// "Repeats" section of the calendar event forms (Views/Calendar/_RecurrenceFields.cshtml).
// Shows the fields for the chosen option, keeps the option labels in step with the chosen date,
// and loads a preview of the dates from the server so the admin can uncheck any they don't want.
(function () {
    'use strict';

    const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    const ORDINALS = ['first', 'second', 'third', 'fourth'];
    const PATTERN_SECTIONS = ['interval', 'end', 'preview'];

    // Sections of the form shown for each "Repeats" option
    const SECTIONS = {
        None: [],
        Weekly: ['interval', 'weekly', 'end', 'preview'],
        MonthlyByWeekday: PATTERN_SECTIONS,
        MonthlyByLastWeekday: PATTERN_SECTIONS,
        MonthlyByDate: PATTERN_SECTIONS,
        SpecificDates: ['specific', 'preview']
    };

    // Parses a yyyy-MM-dd value from a date input as a local date
    function parseDate(value) {
        const parts = (value || '').split('-').map(Number);
        if (parts.length !== 3 || parts.some(isNaN)) {
            return null;
        }
        return new Date(parts[0], parts[1] - 1, parts[2]);
    }

    function weekOfMonth(date) {
        return Math.floor((date.getDate() - 1) / 7) + 1;
    }

    function isLastWeekdayOfMonth(date) {
        const nextWeek = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 7);
        return nextWeek.getMonth() !== date.getMonth();
    }

    function initRecurrence(container) {
        const form = container.closest('form');
        const prefix = container.dataset.prefix;
        const startInput = document.getElementById(container.dataset.startInput);
        const repeatSelect = container.querySelector('[data-recurrence-repeat]');
        const intervalInput = form.elements[prefix + '.Interval'];
        const unit = container.querySelector('[data-recurrence-unit]');
        const dayBoxes = Array.from(container.querySelectorAll('[data-recurrence-day]'));
        const specificList = container.querySelector('[data-recurrence-specific-list]');
        const addDateButton = container.querySelector('[data-recurrence-add-date]');
        const summary = container.querySelector('[data-recurrence-summary]');
        const status = container.querySelector('[data-recurrence-status]');
        const errorList = container.querySelector('[data-recurrence-errors]');
        const datesContainer = container.querySelector('[data-recurrence-dates]');
        const excludedContainer = container.querySelector('[data-recurrence-excluded]');

        // On the edit series form, a checkbox shows or hides the whole section
        const toggle = form.querySelector('[data-recurrence-toggle]');
        const toggledArea = toggle ? document.getElementById(toggle.dataset.recurrenceToggle) : null;

        const originalLabels = new Map(Array.from(repeatSelect.options).map(o => [o.value, o.text]));
        let autoCheckedDay = null;
        let previewTimer = null;
        let previewRequest = 0;

        function isActive() {
            return (!toggle || toggle.checked) && repeatSelect.value !== 'None';
        }

        function getOption(value) {
            return Array.from(repeatSelect.options).find(o => o.value === value);
        }

        function setOptionAvailable(option, available) {
            if (option) {
                option.hidden = !available;
                option.disabled = !available;
            }
        }

        // Names the monthly options after the chosen date, e.g. "Monthly on the second Tuesday"
        function updateOptionLabels() {
            const date = parseDate(startInput.value);
            const byWeekday = getOption('MonthlyByWeekday');
            const byLastWeekday = getOption('MonthlyByLastWeekday');
            const byDate = getOption('MonthlyByDate');

            originalLabels.forEach((text, value) => { getOption(value).text = text; });
            setOptionAvailable(byWeekday, true);
            setOptionAvailable(byLastWeekday, true);

            if (!date) {
                return;
            }

            const dayName = DAY_NAMES[date.getDay()];
            const week = weekOfMonth(date);
            const isLast = isLastWeekdayOfMonth(date);

            if (byWeekday) {
                // A fifth weekday only exists in some months, so offer "last" instead
                byWeekday.text = week <= 4 ? 'Monthly on the ' + ORDINALS[week - 1] + ' ' + dayName : byWeekday.text;
                setOptionAvailable(byWeekday, week <= 4);
            }
            if (byLastWeekday) {
                byLastWeekday.text = 'Monthly on the last ' + dayName;
                setOptionAvailable(byLastWeekday, isLast);
            }
            if (byDate) {
                byDate.text = 'Monthly on day ' + date.getDate();
            }

            // Switch to the other monthly weekday option if the chosen one no longer applies
            if (repeatSelect.value === 'MonthlyByWeekday' && week > 4) {
                repeatSelect.value = 'MonthlyByLastWeekday';
            }
            else if (repeatSelect.value === 'MonthlyByLastWeekday' && !isLast) {
                repeatSelect.value = 'MonthlyByWeekday';
            }
        }

        // Checks the weekday of the chosen date when nothing else is checked,
        // and moves that check along if the date changes
        function updateWeeklyDays() {
            const date = parseDate(startInput.value);
            if (!date || repeatSelect.value !== 'Weekly') {
                return;
            }

            const checked = dayBoxes.filter(box => box.checked);
            const onlyAutoChecked = checked.length === 1 && checked[0] === autoCheckedDay;
            if (checked.length === 0 || onlyAutoChecked) {
                checked.forEach(box => { box.checked = false; });
                autoCheckedDay = dayBoxes.find(box => Number(box.dataset.recurrenceDay) === date.getDay());
                autoCheckedDay.checked = true;
            }
        }

        function updateUnit() {
            const plural = Number(intervalInput.value) !== 1;
            const word = repeatSelect.value === 'Weekly' ? 'week' : 'month';
            unit.textContent = plural ? word + 's' : word;
        }

        function updateSections() {
            const visible = SECTIONS[repeatSelect.value] || [];
            container.querySelectorAll('[data-recurrence-section]').forEach(section => {
                section.hidden = !visible.includes(section.dataset.recurrenceSection);
            });
            if (toggledArea) {
                toggledArea.hidden = !toggle.checked;
            }
        }

        function renumberSpecificDates() {
            specificList.querySelectorAll('li').forEach((item, index) => {
                const number = index + 1;
                item.querySelector('input').setAttribute('aria-label', 'Additional date ' + number);
                item.querySelector('.visually-hidden').textContent = ' additional date ' + number;
            });
        }

        function addSpecificDate() {
            const item = document.createElement('li');
            item.className = 'd-flex gap-2 mb-2';

            const input = document.createElement('input');
            input.type = 'date';
            input.name = prefix + '.SpecificDates';
            input.className = 'form-control recurrence-specific-date';

            const remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'btn btn-outline-danger btn-sm';
            remove.setAttribute('data-recurrence-remove-date', '');
            remove.append('Remove');
            const hiddenText = document.createElement('span');
            hiddenText.className = 'visually-hidden';
            remove.append(hiddenText);

            item.append(input, remove);
            specificList.append(item);
            renumberSpecificDates();
            input.focus();
        }

        // Writes a hidden field for each unchecked date so the server leaves it out
        function updateExcludedDates() {
            excludedContainer.replaceChildren();
            datesContainer.querySelectorAll('input[type="checkbox"]:not(:checked)').forEach(box => {
                const hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = prefix + '.ExcludedDates';
                hidden.value = box.value;
                excludedContainer.append(hidden);
            });
        }

        function updateStatus() {
            const boxes = datesContainer.querySelectorAll('input[type="checkbox"]');
            const checked = datesContainer.querySelectorAll('input[type="checkbox"]:checked').length;
            if (boxes.length === 0) {
                status.textContent = '';
                return;
            }

            let text = checked + ' of ' + boxes.length + (boxes.length === 1 ? ' date' : ' dates') + ' will be saved.';
            const neverEnds = form.elements[prefix + '.End'].value === 'Never' && repeatSelect.value !== 'SpecificDates';
            if (neverEnds) {
                text += ' More dates are added automatically as time goes on.';
            }
            status.textContent = text;
        }

        function clearPreview() {
            summary.textContent = '';
            status.textContent = '';
            errorList.replaceChildren();
            datesContainer.replaceChildren();
        }

        function renderPreview(preview) {
            clearPreview();
            summary.textContent = preview.summary || '';

            preview.errors.forEach(error => {
                const item = document.createElement('li');
                item.textContent = error;
                errorList.append(item);
            });

            // Group the dates under a heading for each month
            let monthGroup = null;
            let monthName = null;
            preview.dates.forEach(date => {
                if (date.month !== monthName) {
                    monthName = date.month;
                    monthGroup = document.createElement('fieldset');
                    monthGroup.className = 'recurrence-month';
                    const legend = document.createElement('legend');
                    legend.className = 'fs-6 fw-semibold mb-1';
                    legend.textContent = monthName;
                    monthGroup.append(legend);
                    datesContainer.append(monthGroup);
                }

                const id = prefix.replace(/\./g, '_') + '_Date_' + date.date;
                const wrapper = document.createElement('div');
                wrapper.className = 'form-check';
                const box = document.createElement('input');
                box.type = 'checkbox';
                box.className = 'form-check-input';
                box.id = id;
                box.value = date.date;
                box.checked = !date.excluded;
                box.setAttribute('data-recurrence-date', '');
                const label = document.createElement('label');
                label.className = 'form-check-label';
                label.htmlFor = id;
                label.textContent = date.label;
                wrapper.append(box, label);
                monthGroup.append(wrapper);
            });

            updateExcludedDates();
            updateStatus();
        }

        async function loadPreview() {
            if (!isActive()) {
                clearPreview();
                return;
            }

            const requestId = ++previewRequest;
            const data = new FormData(form);
            data.set('startDate', startInput.value);
            status.textContent = 'Loading dates…';

            try {
                const response = await fetch(container.dataset.previewUrl, { method: 'POST', body: data });
                if (!response.ok) {
                    throw new Error('Preview request failed with status ' + response.status);
                }
                const preview = await response.json();
                // Ignore responses to older requests that finish late
                if (requestId === previewRequest) {
                    renderPreview(preview);
                }
            }
            catch (error) {
                console.error(error);
                if (requestId === previewRequest) {
                    clearPreview();
                    status.textContent = 'The dates could not be loaded. They will still be saved when you submit the form.';
                }
            }
        }

        function schedulePreview() {
            clearTimeout(previewTimer);
            previewTimer = setTimeout(loadPreview, 300);
        }

        // initialLoad: true when the page first loads, so the form is shown as the server sent it
        function refresh(initialLoad) {
            updateOptionLabels();
            if (initialLoad !== true) {
                updateWeeklyDays();
            }
            updateUnit();
            updateSections();
            schedulePreview();
        }

        container.addEventListener('change', event => {
            if (event.target.hasAttribute('data-recurrence-date')) {
                updateExcludedDates();
                updateStatus();
                return;
            }
            if (event.target.hasAttribute('data-recurrence-end-for')) {
                // Typing an end date or count chooses that way of ending
                form.elements[prefix + '.End'].value = event.target.dataset.recurrenceEndFor;
            }
            refresh();
        });

        container.addEventListener('click', event => {
            const remove = event.target.closest('[data-recurrence-remove-date]');
            if (remove) {
                remove.closest('li').remove();
                renumberSpecificDates();
                addDateButton.focus();
                schedulePreview();
            }
        });

        addDateButton.addEventListener('click', addSpecificDate);
        startInput.addEventListener('change', refresh);
        if (toggle) {
            toggle.addEventListener('change', refresh);
        }

        refresh(true);
    }

    document.querySelectorAll('[data-recurrence]').forEach(initRecurrence);
})();
