window.onload = function () {
    let agencySubmit = document.getElementById("agency-submit");
    let agency = document.getElementsByClassName("right-agency");
    let agency2 = document.getElementsByClassName("left-agency");
    let categorySubmit = document.getElementById("category-submit");
    let zipSubmit = document.getElementById("zip-submit");
    agencySubmit.onclick = setAgencyNameAndGetYPos;
    categorySubmit.onclick = setCategoryNameAndGetYPos;
    zipSubmit.onclick = setZipcodeAndGetYPos;

    for (let i = 0; i < agency.length; i++) {
        agency[i].addEventListener('click', getYpos);
    }
    for (let i = 0; i < agency2.length; i++) {
        agency2[i].addEventListener('click', getYpos);
    }
    
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
    url = url.replace("agency-category", input);
    $(this).attr("formaction", url);
}

function setZipcodeAndGetYPos() {
    let y = document.documentElement.scrollTop;
    let url = $(this).attr("formaction");
    url = url.replace("tempY", y);

    let input = $('[name="list-of-zipcodes"]').val();
    url = url.replace("zip-code", input);
    $(this).attr("formaction", url);
}