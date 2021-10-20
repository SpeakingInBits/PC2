window.onload = function () {
    instance = new dtsel.DTS('input[name="dateTimePicker"]', {
        showTime: false,
        showDate: true,
        dateFormat: "mm/dd/yyyy",
        direction: 'BOTTOM',
        paddingX: 5,
        paddingY: 5
    });
}