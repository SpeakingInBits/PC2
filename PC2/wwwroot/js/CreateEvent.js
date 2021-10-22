window.onload = function () {
    instance = new dtsel.DTS('input[name="dateTimePicker"]', {
        showTime: false,
        showDate: true,
        dateFormat: "mm/dd/yyyy",
        direction: 'BOTTOM',
        paddingX: 5,
        paddingY: 5
    });

    let pc2Checkbox = document.getElementById("pc2");
    let countyCheckbox = document.getElementById("county");

    pc2Checkbox.onclick = uncheckBox;
    countyCheckbox.onclick = uncheckBox;

    let submitButton = document.getElementById("submit");
    submitButton.onclick = onSubmit;
}

function onSubmit() {
    let isValid = true;
    if (!validateDate()) {
        isValid = false;
    }
    if (!validateStartTime()) {
        isValid = false;
    }
    if (!validateEndTime()) {
        isValid = false;
    }
    if (!validateDescription()) {
        isValid = false;
    }
    if (!validateCheckboxes()) {
        isValid = false;
    }

    if (!isValid) {
        $("#create").submit(function (e) {
            e.preventDefault();
        });
    }
    else {
        let url = $("#submit").attr("formaction");
        let description = document.getElementById("description").value;
        url = url.replace("tempDescription", description);

        let date = document.getElementById("date").value;
        url = url.replace("tempDate", date);

        let start = document.getElementById("starting-time").value;
        url = url.replace("tempStart", start);

        let end = document.getElementById("ending-time").value;
        url = url.replace("tempEnd", end);

        $("#submit").attr("formaction", url);
        $("#create").unbind("submit").submit();
    }
}

/**
 * Validates whether or not one of the checkboxes is checked
 * */
function validateCheckboxes() {
    if ($("#pc2").prop("checked") || $("#county").prop("checked")) {
        document.getElementById("checkbox-error").innerHTML = "";
        return true;
    }
    else {
        document.getElementById("checkbox-error").innerHTML = "Must check one of the checkboxes";
        return false;
    }
}

/**
 * Validates whether or not a description has been added
 * */
function validateDescription() {
    if (document.getElementById("description").value.trim() == "") {
        document.getElementById("description-error").innerHTML = "Enter a description for the event";
        return false;
    }
    else {
        document.getElementById("description-error").innerHTML = "";
        
        return true;
    }
}

/**
 * Validates whether or not an end time has been filled in
 * */
function validateEndTime() {
    if (document.getElementById("ending-time").value.trim() == "") {
        document.getElementById("end-error").innerHTML = "Enter a valid end time. ie 01:30 PM";
        return false;
    }
    else {
        document.getElementById("end-error").innerHTML = "";
        return true;
    }
}

/**
 * Validates whether or not the starting time has been filled in
 * */
function validateStartTime() {
    if (document.getElementById("starting-time").value.trim() == "") {
        document.getElementById("start-error").innerHTML = "Enter a valid start time. ie 01:30 PM";
        return false;
    }
    else {
        document.getElementById("start-error").innerHTML = "";
        return true;
    }
}

/**
 * Validates that the date has been filled in and is in the proper format for parsing into DateTime
 * */
function validateDate() {
    let regexp = /^(0[1-9]|1[012])[- /.](0[1-9]|[12][0-9]|3[01])[- /.](19|20)\d\d/g;

    let inputDate = document.getElementById("date").value;;

    if (!regexp.test(inputDate)) {
        document.getElementById("error").innerHTML = "Enter a valid date. ie 07/01/2021";
        return false;
    }
    else {
        document.getElementById("error").innerHTML = "";
        return true;
    }
}

/**
 * Unchecks the opposite checkbox
 * */
function uncheckBox() {
    if (this == document.getElementById("pc2")) {
        $("#county").prop("checked", false);
    }
    else {
        $("#pc2").prop("checked", false);
    }
}