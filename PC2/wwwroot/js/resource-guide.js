window.onload = function () {
    let agencySubmit = document.getElementById("agency-submit");
    let agency = document.getElementById("agency");
    agencySubmit.onclick = setAgencyNameAndGetYPos;
    //agency.onclick = getYpos;
    agency.addEventListener('click', getYpos);
}

function getYpos() {
    let y = document.documentElement.scrollTop;
    let url = $(this).attr("href");
    url = url.replace("tempY", y);
    $(this).attr("href", url);
}

function setAgencyNameAndGetYPos() {
    let y = document.documentElement.scrollTop;
    let url = $(this).attr("formaction");
    url = url.replace("tempY", y);

    let input = $('[name="list-of-agencies"]').val();
    url = url.replace("agency-name", input);
    $(this).attr("formaction", url);
}