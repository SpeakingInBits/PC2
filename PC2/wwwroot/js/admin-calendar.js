// Admin event calendar (Views/Calendar/Index.cshtml).
// Shows events in a month grid or list. Each upcoming day has a "+" link to add an event on that date,
// and choosing an event opens a panel with its edit, duplicate, and delete actions.
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    const calendarEl = document.getElementById('admin-calendar');
    const allEvents = JSON.parse(document.getElementById('calendar-events').textContent);
    const filters = Array.from(document.querySelectorAll('[data-calendar-filter]'));
    const modalEl = document.getElementById('event-actions');
    const deleteForm = document.getElementById('delete-event-form');
    const urls = calendarEl.dataset;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    let modalOpener = null;
    let selectedEvent = null;

    function toDateValue(date) {
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return date.getFullYear() + '-' + month + '-' + day;
    }

    function createUrl(date) {
        return urls.createUrl + '?date=' + toDateValue(date);
    }

    function isShown(event) {
        const type = event.extendedProps.isPc2Event ? 'pc2' : 'county';
        return filters.some(filter => filter.dataset.calendarFilter === type && filter.checked);
    }

    // The prev/next buttons are named by their title; hide their icon spans (role="img" with no name)
    function hideToolbarIcons() {
        calendarEl.querySelectorAll('.fc-toolbar .fc-icon').forEach(icon => icon.setAttribute('aria-hidden', 'true'));
    }

    function setLink(selector, href) {
        modalEl.querySelectorAll('[data-action="' + selector + '"]').forEach(link => { link.href = href; });
    }

    function openActions(event) {
        const props = event.extendedProps;
        const isSeries = props.seriesId !== null && props.seriesId !== undefined;
        selectedEvent = event;

        modalEl.querySelector('[data-event-date]').textContent = props.dateLabel;
        modalEl.querySelector('[data-event-time]').textContent = props.timeLabel;
        modalEl.querySelector('[data-event-type]').textContent = props.isPc2Event ? 'PC2 event' : 'County event';
        // Encoded and linkified by the server (TextLinkifier)
        modalEl.querySelector('[data-event-description]').innerHTML = props.descriptionHtml;
        modalEl.querySelector('[data-event-schedule]').textContent = props.seriesSchedule || '';
        modalEl.querySelector('[data-event-series]').hidden = !isSeries;
        modalEl.querySelector('[data-single-actions]').hidden = isSeries;
        modalEl.querySelector('[data-series-actions]').hidden = !isSeries;
        modalEl.querySelector('#event-actions-title').textContent = isSeries ? 'Repeating event' : 'Event';

        setLink('edit', urls.editUrl + '/' + event.id);
        setLink('duplicate', urls.createUrl + '?copyFrom=' + event.id);
        if (isSeries) {
            setLink('edit-series', urls.editSeriesUrl + '/' + props.seriesId);
            setLink('delete-series', urls.deleteSeriesUrl + '/' + props.seriesId);
        }

        modalOpener = document.activeElement;
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    modalEl.querySelectorAll('[data-action="delete"]').forEach(button => {
        button.addEventListener('click', () => {
            const isSeries = selectedEvent.extendedProps.seriesId !== null && selectedEvent.extendedProps.seriesId !== undefined;
            const message = isSeries
                ? 'Delete only ' + selectedEvent.extendedProps.dateLabel + ' from this repeating event?'
                : 'Are you sure you want to delete this event?';
            if (confirm(message)) {
                deleteForm.elements.id.value = selectedEvent.id;
                deleteForm.submit();
            }
        });
    });

    // Return focus to the event that opened the panel once it closes
    modalEl.addEventListener('hidden.bs.modal', () => {
        if (modalOpener && document.body.contains(modalOpener)) {
            modalOpener.focus();
        }
        modalOpener = null;
    });

    const isMobile = window.innerWidth < 768;
    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: isMobile ? 'listMonth' : 'dayGridMonth',
        headerToolbar: isMobile ? {
            left: 'prev,next',
            center: 'title',
            right: 'dayGridMonth,listMonth'
        } : {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,listMonth'
        },
        buttonText: {
            today: 'Today',
            month: 'Month',
            list: 'List'
        },
        // Grow with the events instead of scrolling inside a fixed height box
        height: 'auto',
        // Show up to 3 events a day, then "+ more". The row grows to fit them
        dayMaxEvents: 3,
        eventDisplay: 'block',
        displayEventEnd: true,
        // Events can be reached with Tab and opened with Enter or Space
        eventInteractive: true,
        noEventsText: 'No events this month',
        events: function (info, successCallback) {
            successCallback(allEvents.filter(isShown));
        },
        moreLinkDidMount: function (info) {
            // FullCalendar gives this link aria-expanded, which is only allowed on a button
            info.el.setAttribute('role', 'button');
        },
        datesSet: function () {
            // Runs after the toolbar re-renders
            setTimeout(hideToolbarIcons, 0);
        },
        eventClick: function (info) {
            info.jsEvent.preventDefault();
            openActions(info.event);
        },
        dateClick: function (info) {
            if (info.date >= today) {
                window.location.href = createUrl(info.date);
            }
        },
        dayCellDidMount: function (info) {
            // Keyboard and screen reader friendly way to add an event on a day
            const top = info.el.querySelector('.fc-daygrid-day-top');
            if (!top || info.isPast || info.isOther) {
                return;
            }
            const link = document.createElement('a');
            link.className = 'calendar-add-link';
            link.href = createUrl(info.date);
            link.innerHTML = '<span aria-hidden="true">+</span>';
            const label = document.createElement('span');
            label.className = 'visually-hidden';
            label.textContent = 'Add event on ' + info.date.toLocaleDateString(undefined,
                { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
            link.append(label);
            // Don't also trigger dateClick
            link.addEventListener('click', e => e.stopPropagation());
            // The day header is laid out right to left, so this puts the link on the left of the day number
            top.append(link);
        },
        eventDidMount: function (info) {
            const props = info.event.extendedProps;
            const title = info.el.querySelector('.fc-event-title, .fc-list-event-title a, .fc-list-event-title');
            if (!title) {
                return;
            }

            if (props.seriesId !== null && props.seriesId !== undefined) {
                const icon = document.createElement('i');
                icon.className = 'bi bi-arrow-repeat me-1';
                icon.setAttribute('aria-hidden', 'true');
                title.prepend(icon);
            }

            // Event type and repeating are otherwise shown only by colour and icon
            const hiddenText = document.createElement('span');
            hiddenText.className = 'visually-hidden';
            hiddenText.textContent = ' (' + (props.isPc2Event ? 'PC2 event' : 'County event')
                + (props.seriesId ? ', repeating' : '') + ')';
            title.append(hiddenText);
        }
    });

    filters.forEach(filter => filter.addEventListener('change', () => calendar.refetchEvents()));

    calendar.render();
    hideToolbarIcons();
});
