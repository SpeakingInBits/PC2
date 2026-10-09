using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers;

[Authorize(Roles = IdentityHelper.AdminOrStaff)]
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _context;

    public CalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public async Task<IActionResult> Index()
    {
        List<CalendarEvent> calendarEvents = await CalendarEventDB.GetAllEvents(_context);

        await CalendarEventDB.DeletePastEvents(_context);

        // Convert to view models with sanitized descriptions
        List<CalendarEventViewModel> viewModels = calendarEvents.Select(e => new CalendarEventViewModel
        {
            Event = e,
            SanitizedDescription = TextLinkifier.Linkify(e.EventDescription),
            SeriesSchedule = e.EventSeries == null ? null : EventRecurrence.Describe(e.EventSeries)
        }).ToList();

        return View(viewModels);
    }

    /// <summary>
    /// Creates a calendar event and date
    /// </summary>
    /// <param name="date">Date to fill in on the form, if any</param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Create(DateTime? date)
    {
        CalendarCreateEventViewModel model = new();

        if (date.HasValue && date.Value.Date >= DateTime.Today)
        {
            model.DateOfEvent = date.Value.Date;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CalendarCreateEventViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        DateOnly date = DateOnly.FromDateTime(model.DateOfEvent);

        if (!model.Recurrence.Repeats)
        {
            CalendarEvent newEvent = new()
            {
                DateOfEvent = date,
                StartingTime = TimeOnly.Parse(model.StartingTime),
                EndingTime = TimeOnly.Parse(model.EndingTime),
                EventDescription = model.Description,
                PC2Event = model.IsPc2Event,
                CountyEvent = model.IsCountyEvent
            };

            await CalendarEventDB.AddEvent(_context, newEvent);

            return RedirectToAction("Index");
        }

        List<DateOnly> dates = model.Recurrence.GetSelectedDates(date, Today);

        if (dates.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Every date is unchecked. Check at least one date to create the event");
            return View(model);
        }

        EventSeries series = new()
        {
            StartingTime = TimeOnly.Parse(model.StartingTime),
            EndingTime = TimeOnly.Parse(model.EndingTime),
            EventDescription = model.Description,
            PC2Event = model.IsPc2Event,
            CountyEvent = model.IsCountyEvent
        };
        model.Recurrence.ApplyTo(series, date);

        if (series.IsOpenEnded)
        {
            series.GeneratedThrough = EventRecurrence.GetGenerationLimit(series, Today);
        }

        await EventSeriesDB.AddSeries(_context, series, dates);

        TempData["CalendarMessage"] = $"Created a repeating event with {dates.Count} {(dates.Count == 1 ? "date" : "dates")}.";
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Lists the dates a repeating event will be created on, for the preview on the create and edit series forms
    /// </summary>
    /// <param name="startDate">First date of the series</param>
    /// <param name="recurrence">How the event repeats</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PreviewDates(DateTime? startDate, [Bind(Prefix = "Recurrence")] RecurrenceInputModel recurrence)
    {
        if (!recurrence.Repeats)
        {
            return Json(new RecurrencePreview(null, [], []));
        }

        if (startDate == null)
        {
            return Json(new RecurrencePreview(null, [], ["Choose a date to see when the event repeats"]));
        }

        DateOnly start = DateOnly.FromDateTime(startDate.Value);

        if (start < Today)
        {
            return Json(new RecurrencePreview(null, [], ["The date must be today or in the future"]));
        }

        List<string> errors = recurrence.Validate(start, Today, nameof(CalendarCreateEventViewModel.Recurrence))
            .Select(r => r.ErrorMessage ?? string.Empty)
            .ToList();

        if (errors.Count > 0)
        {
            return Json(new RecurrencePreview(null, [], errors));
        }

        HashSet<DateOnly> excluded = recurrence.GetExcludedDates();
        List<RecurrencePreviewDate> dates = recurrence.GetAllDates(start, Today)
            .Select(d => new RecurrencePreviewDate(
                d.ToString("yyyy-MM-dd"),
                d.ToString("dddd, MMMM d"),
                d.ToString("MMMM yyyy"),
                excluded.Contains(d)))
            .ToList();

        EventSeries series = new() { EventDescription = string.Empty };
        recurrence.ApplyTo(series, start);

        return Json(new RecurrencePreview(EventRecurrence.Describe(series), dates, []));
    }

    /// <summary>
    /// Edits an event based on event id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        CalendarEvent? calendarEvent = await CalendarEventDB.GetEvent(_context, id);

        if (calendarEvent == null)
        {
            return NotFound();
        }

        CalendarCreateEventViewModel editEvent = new()
        {
            DateOfEvent = calendarEvent.DateOfEvent.ToDateTime(new TimeOnly()),
            Description = calendarEvent.EventDescription,
            EndingTime = calendarEvent.EndingTime.ToString("HH:mm"),
            EventId = calendarEvent.CalendarEventID,
            IsCountyEvent = calendarEvent.CountyEvent,
            StartingTime = calendarEvent.StartingTime.ToString("HH:mm"),
            IsPc2Event = calendarEvent.PC2Event
        };
        SetSeriesInfo(editEvent, calendarEvent);

        return View(editEvent);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CalendarCreateEventViewModel model)
    {
        // Update the existing event so fields not on the form, such as its series, are kept
        CalendarEvent? calendarEvent = await CalendarEventDB.GetEvent(_context, model.EventId);

        if (calendarEvent == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            SetSeriesInfo(model, calendarEvent);
            return View(model);
        }

        calendarEvent.CountyEvent = model.IsCountyEvent;
        calendarEvent.PC2Event = model.IsPc2Event;
        calendarEvent.StartingTime = TimeOnly.Parse(model.StartingTime);
        calendarEvent.EndingTime = TimeOnly.Parse(model.EndingTime);
        calendarEvent.EventDescription = model.Description;
        calendarEvent.DateOfEvent = DateOnly.FromDateTime(model.DateOfEvent);

        bool success = await CalendarEventDB.UpdateEvent(_context, calendarEvent);

        if (!success)
        {
            TempData["UpdateFailed"] = "An error occurred updating the event";
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Changes every upcoming date of a series
    /// </summary>
    /// <param name="id">Id of the series</param>
    [HttpGet]
    public async Task<IActionResult> EditSeries(int id)
    {
        EventSeries? series = await EventSeriesDB.GetSeries(_context, id);

        if (series == null)
        {
            return NotFound();
        }

        List<DateOnly> upcomingDates = GetUpcomingDates(series);
        DateOnly startDate = GetScheduleStartDate(series, upcomingDates);

        EditSeriesViewModel model = new()
        {
            SeriesId = series.EventSeriesID,
            StartingTime = series.StartingTime.ToString("HH:mm"),
            EndingTime = series.EndingTime.ToString("HH:mm"),
            Description = series.EventDescription,
            IsPc2Event = series.PC2Event,
            IsCountyEvent = series.CountyEvent,
            StartDate = startDate.ToDateTime(TimeOnly.MinValue),
            Recurrence = RecurrenceInputModel.FromSeries(series, upcomingDates, startDate, Today)
        };
        SetSeriesInfo(model, series, upcomingDates);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSeries(EditSeriesViewModel model)
    {
        EventSeries? series = await EventSeriesDB.GetSeries(_context, model.SeriesId);

        if (series == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            SetSeriesInfo(model, series, GetUpcomingDates(series));
            return View(model);
        }

        List<DateOnly> dates = new();
        DateOnly startDate = DateOnly.FromDateTime(model.StartDate);

        if (model.ChangeSchedule)
        {
            dates = model.Recurrence.GetSelectedDates(startDate, Today);

            if (dates.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Every date is unchecked. Check at least one date, or delete the series instead");
                SetSeriesInfo(model, series, GetUpcomingDates(series));
                return View(model);
            }
        }

        series.StartingTime = TimeOnly.Parse(model.StartingTime);
        series.EndingTime = TimeOnly.Parse(model.EndingTime);
        series.EventDescription = model.Description;
        series.PC2Event = model.IsPc2Event;
        series.CountyEvent = model.IsCountyEvent;

        if (model.ChangeSchedule)
        {
            model.Recurrence.ApplyTo(series, startDate);
            await EventSeriesDB.RescheduleSeries(_context, series, dates, Today);
        }
        else
        {
            await EventSeriesDB.UpdateSeriesDetails(_context, series, Today);
        }

        TempData["CalendarMessage"] = "The repeating event was updated.";
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Asks to confirm deleting a series
    /// </summary>
    /// <param name="id">Id of the series</param>
    [HttpGet]
    public async Task<IActionResult> DeleteSeries(int id)
    {
        EventSeries? series = await EventSeriesDB.GetSeries(_context, id);

        if (series == null)
        {
            return NotFound();
        }

        DeleteSeriesViewModel model = new()
        {
            SeriesId = series.EventSeriesID,
            Description = series.EventDescription,
            Schedule = EventRecurrence.Describe(series),
            StartingTime = series.StartingTime,
            EndingTime = series.EndingTime,
            UpcomingDates = GetUpcomingDates(series)
        };

        return View(model);
    }

    [HttpPost]
    [ActionName("DeleteSeries")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSeriesConfirmed(int seriesId)
    {
        await EventSeriesDB.DeleteSeries(_context, seriesId);
        TempData["CalendarMessage"] = "The repeating event and all of its dates were deleted.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(int id)
    {
        await CalendarEventDB.DeleteEvent(_context, id);
        TempData["EventDeleted"] = true;
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Gets the dates of a series that are today or later
    /// </summary>
    private static List<DateOnly> GetUpcomingDates(EventSeries series)
    {
        return series.Events
            .Where(e => e.DateOfEvent >= Today)
            .Select(e => e.DateOfEvent)
            .Distinct()
            .Order()
            .ToList();
    }

    /// <summary>
    /// Gets the date an edited schedule starts on: the next date of the series' pattern, so the
    /// pattern is kept even if the next date was moved individually
    /// </summary>
    private static DateOnly GetScheduleStartDate(EventSeries series, List<DateOnly> upcomingDates)
    {
        if (series.Frequency != RecurrenceFrequency.SpecificDates)
        {
            DateOnly from = series.StartDate > Today ? series.StartDate : Today;
            DateOnly limit = EventRecurrence.GetGenerationLimit(series, Today);
            foreach (DateOnly date in EventRecurrence.GetDates(series, from, limit))
            {
                return date;
            }
        }

        return upcomingDates.Count > 0 ? upcomingDates[0] : Today;
    }

    private static void SetSeriesInfo(CalendarCreateEventViewModel model, CalendarEvent calendarEvent)
    {
        if (calendarEvent.EventSeries != null)
        {
            model.SeriesId = calendarEvent.EventSeries.EventSeriesID;
            model.SeriesSchedule = EventRecurrence.Describe(calendarEvent.EventSeries);
        }
    }

    private static void SetSeriesInfo(EditSeriesViewModel model, EventSeries series, List<DateOnly> upcomingDates)
    {
        model.CurrentSchedule = EventRecurrence.Describe(series);
        model.UpcomingCount = upcomingDates.Count;
    }
}
