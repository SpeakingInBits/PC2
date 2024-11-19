let calendarEl = document.getElementById("calendar");

let calendar = new FullCalendar.Calendar(calendarEl, {
    initialView: 'dayGridMonth',
    headerToolbar: {
        left: 'prev,next today',
        center: 'title',
        right: 'dayGridMonth,timeGridWeek,timeGridDay'
    },
    events: [
        {
            title: "Record a video for Marissa",
            start: "2024-11-18",
        },
    ],
    eventClick: function (info) {
        // Populate modal with event data
        document.getElementById('modalEventTitle').textContent = info.event.title;
        document.getElementById('modalEventDescription').textContent = info.event.extendedProps.description;
        document.getElementById('modalEventDate').textContent = info.event.start.toLocaleString();

        // Show the modal
        var myModal = new bootstrap.Modal(document.getElementById('eventModal'));
        myModal.show();
    },
});

calendar.render();
