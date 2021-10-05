window.onload = function () {
    let agencySubmit = document.getElementById("agency-submit");
    let agency = document.getElementById("agency");
    let categorySubmit = document.getElementById("category-submit");
    agencySubmit.onclick = setAgencyNameAndGetYPos;
    categorySubmit.onclick = setCategoryNameAndGetYPos;
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
    input = input.replace("&", "**");
    url = url.replace("agency-name", input);
    $(this).attr("formaction", url);
}

function setCategoryNameAndGetYPos() {
    let y = document.documentElement.scrollTop;
    let url = $(this).attr("formaction");
    url = url.replace("tempY", y);

    let input = $('[name="list-of-services"]').val();
    input = input.replace("&", "**");
    url = url.replace("agency-category", input);
    $(this).attr("formaction", url);
}