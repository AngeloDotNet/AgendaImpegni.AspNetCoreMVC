document.addEventListener("DOMContentLoaded", function () {
    const calendarEl = document.getElementById("calendar");
    const eventModalEl = document.getElementById("eventModal");
    const eventModal = new bootstrap.Modal(eventModalEl);

    const btnToday = document.getElementById("btnToday");
    const btnNewEvent = document.getElementById("btnNewEvent");
    const btnDeleteEvent = document.getElementById("btnDeleteEvent");
    const eventForm = document.getElementById("eventForm");

    const fields = {
        id: document.getElementById("eventId"),
        title: document.getElementById("eventTitle"),
        description: document.getElementById("eventDescription"),
        notes: document.getElementById("eventNotes"),
        location: document.getElementById("eventLocation"),
        start: document.getElementById("eventStart"),
        end: document.getElementById("eventEnd"),
        allDay: document.getElementById("eventAllDay"),
        color: document.getElementById("eventColor"),
        categoryId: document.getElementById("eventCategoryId"),
        recurrenceType: document.getElementById("eventRecurrenceType"),
        recurrenceInterval: document.getElementById("eventRecurrenceInterval"),
        recurrenceUntil: document.getElementById("eventRecurrenceUntil"),
        reminderType: document.getElementById("eventReminderType"),
        modalTitle: document.getElementById("eventModalTitle")
    };

    let calendar = new FullCalendar.Calendar(calendarEl, {
        locale: 'it',
        initialView: calendarInitialView,
        initialDate: calendarInitialDate,
        firstDay: 1,
        navLinks: true,
        editable: true,
        selectable: true,
        dayMaxEvents: true,
        height: "auto",
        nowIndicator: true,
        stickyHeaderDates: true,
        weekNumbers: true,
        fixedWeekCount: false,
        businessHours: {
            daysOfWeek: [1, 2, 3, 4, 5],
            startTime: '08:00',
            endTime: '18:00'
        },
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
        },
        buttonText: {
            today: 'Oggi',
            month: 'Mese',
            week: 'Settimana',
            day: 'Giorno',
            list: 'Lista'
        },
        events: function (fetchInfo, successCallback, failureCallback) {
            const params = new URLSearchParams({
                query: window.agendaFilters?.query || "",
                categoryId: window.agendaFilters?.categoryId || "",
                day: ""
            });

            fetch(`/api/calendar?${params.toString()}`)
                .then(r => r.json())
                .then(successCallback)
                .catch(failureCallback);
        },
        dateClick: function (info) {
            openCreateModal(info.dateStr, info.allDay);
        },
        eventClick: function (info) {
            openEditModal(info.event);
        },
        eventDrop: async function (info) {
            try {
                await saveMove(info.event);
            } catch {
                info.revert();
            }
        },
        eventResize: async function (info) {
            try {
                await saveMove(info.event);
            } catch {
                info.revert();
            }
        },
        eventDidMount: function (info) {
            const desc = info.event.extendedProps.description || "";
            const loc = info.event.extendedProps.location || "";
            const cat = info.event.extendedProps.categoryName || "";

            const tooltipParts = [info.event.title, cat, desc, loc ? `Luogo: ${loc}` : ""].filter(Boolean);
            if (tooltipParts.length) {
                info.el.setAttribute("title", tooltipParts.join("\n"));
            }
        }
    });

    calendar.render();
    loadCategories();

    btnToday.addEventListener("click", function () {
        calendar.today();
    });

    btnNewEvent.addEventListener("click", function () {
        openCreateModal(calendar.getDate().toISOString().slice(0, 10), false);
    });

    fields.allDay.addEventListener("change", toggleAllDay);
    eventForm.addEventListener("submit", async function (e) {
        e.preventDefault();
        await saveEvent();
    });

    btnDeleteEvent.addEventListener("click", async function () {
        const id = fields.id.value;
        if (!id) return;

        if (!confirm("Vuoi eliminare questo impegno?")) return;

        const resp = await fetch(`/api/calendar/${id}`, { method: 'DELETE' });
        if (resp.ok) {
            eventModal.hide();
            calendar.refetchEvents();
        } else {
            alert("Errore durante l'eliminazione.");
        }
    });

    window.applyAgendaFilters = function (query, categoryId) {
        window.agendaFilters = { query, categoryId };
        calendar.refetchEvents();
    };

    function openCreateModal(dateStr, allDay) {
        resetModal();
        fields.modalTitle.textContent = "Nuovo impegno";
        fields.allDay.checked = allDay;
        fields.start.value = buildDateValue(dateStr, allDay ? "00:00" : "09:00", allDay);
        fields.end.value = buildDateValue(dateStr, allDay ? "23:59" : "10:00", allDay);
        btnDeleteEvent.classList.add("d-none");
        toggleAllDay();
        eventModal.show();
    }

    function openEditModal(fcEvent) {
        resetModal();
        fields.modalTitle.textContent = "Modifica impegno";
        fields.id.value = fcEvent.id;
        fields.title.value = fcEvent.title || "";
        fields.description.value = fcEvent.extendedProps.description || "";
        fields.notes.value = fcEvent.extendedProps.notes || "";
        fields.location.value = fcEvent.extendedProps.location || "";
        fields.color.value = fcEvent.backgroundColor || "#0d6efd";
        fields.categoryId.value = fcEvent.extendedProps.categoryId || "";
        fields.recurrenceType.value = fcEvent.extendedProps.recurrenceType || "None";
        fields.reminderType.value = fcEvent.extendedProps.reminderType || "None";
        fields.allDay.checked = fcEvent.allDay;
        fields.start.value = toInput(fcEvent.start, fcEvent.allDay);
        fields.end.value = toInput(fcEvent.end || fcEvent.start, fcEvent.allDay);
        fields.recurrenceInterval.value = 1;
        btnDeleteEvent.classList.remove("d-none");
        toggleAllDay();
        eventModal.show();
    }

    function resetModal() {
        fields.id.value = "";
        fields.title.value = "";
        fields.description.value = "";
        fields.notes.value = "";
        fields.location.value = "";
        fields.color.value = "#0d6efd";
        fields.categoryId.value = "";
        fields.recurrenceType.value = "None";
        fields.recurrenceInterval.value = 1;
        fields.recurrenceUntil.value = "";
        fields.reminderType.value = "None";
        fields.allDay.checked = false;
    }

    function toggleAllDay() {
        if (fields.allDay.checked) {
            fields.start.type = "date";
            fields.end.type = "date";
            fields.start.value = fields.start.value.slice(0, 10);
            fields.end.value = fields.end.value.slice(0, 10);
        } else {
            if (fields.start.type === "date") {
                const s = fields.start.value;
                const e = fields.end.value;
                fields.start.type = "datetime-local";
                fields.end.type = "datetime-local";
                fields.start.value = s ? `${s}T09:00` : "";
                fields.end.value = e ? `${e}T10:00` : "";
            }
        }
    }

    async function saveEvent() {
        const payload = {
            id: fields.id.value || null,
            title: fields.title.value?.trim(),
            description: fields.description.value,
            notes: fields.notes.value,
            location: fields.location.value,
            start: parseDateValue(fields.start.value, fields.allDay.checked),
            end: parseDateValue(fields.end.value, fields.allDay.checked),
            allDay: fields.allDay.checked,
            color: fields.color.value,
            categoryId: fields.categoryId.value || null,
            recurrenceType: fields.recurrenceType.value,
            recurrenceInterval: parseInt(fields.recurrenceInterval.value || "1", 10),
            recurrenceUntil: fields.recurrenceUntil.value || null,
            reminderType: fields.reminderType.value
        };

        if (!payload.title) {
            alert("Il titolo è obbligatorio.");
            fields.title.focus();
            return;
        }

        if (!payload.start || !payload.end) {
            alert("Inserisci data di inizio e fine.");
            return;
        }

        if (new Date(payload.end) < new Date(payload.start)) {
            alert("La fine non può essere precedente all'inizio.");
            return;
        }

        const isEdit = !!payload.id;
        const url = isEdit ? `/api/calendar/${payload.id}` : '/api/calendar';
        const method = isEdit ? 'PUT' : 'POST';

        const resp = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (resp.ok) {
            eventModal.hide();
            calendar.refetchEvents();
        } else {
            const text = await resp.text();
            alert("Errore salvataggio: " + text);
        }
    }

    async function saveMove(fcEvent) {
        const payload = {
            id: fcEvent.id,
            start: fcEvent.start.toISOString(),
            end: (fcEvent.end || fcEvent.start).toISOString(),
            allDay: fcEvent.allDay
        };

        const resp = await fetch('/api/calendar/move', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!resp.ok) throw new Error("Move failed");
    }

    async function loadCategories() {
        const resp = await fetch('/api/calendar/categories');
        if (!resp.ok) return;

        const categories = await resp.json();
        fields.categoryId.innerHTML = `<option value="">Nessuna</option>` +
            categories.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
    }

    function toInput(date, allDay) {
        if (!date) return "";
        const d = new Date(date);
        if (allDay) {
            return d.toISOString().slice(0, 10);
        }
        const pad = n => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    }

    function buildDateValue(dateStr, time, allDay) {
        if (!dateStr) return "";
        return allDay ? dateStr.slice(0, 10) : `${dateStr}T${time}`;
    }

    function parseDateValue(value, allDay) {
        if (!value) return null;
        if (allDay) return new Date(value + "T00:00:00").toISOString();
        return new Date(value).toISOString();
    }
});