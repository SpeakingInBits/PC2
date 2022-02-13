window.onload = function () {
    let agencySubmit = document.getElementById("agency-submit");
    let agency = document.getElementsByClassName("right-agency");
    let agency2 = document.getElementsByClassName("left-agency");
    let categorySubmit = document.getElementById("category-submit");
    let citySubmit = document.getElementById("city-submit");
    agencySubmit.onclick = setAgencyNameAndGetYPos;
    categorySubmit.onclick = setCategoryNameAndGetYPos;
    citySubmit.onclick = setCityAndGetYPos;

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
    input = encodeURIComponent(input);
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

function setCityAndGetYPos() {
    let y = document.documentElement.scrollTop;
    let url = $(this).attr("formaction");
    url = url.replace("tempY", y);

    let input = $('[name="list-of-cities"]').val();
    url = url.replace("city-name", input);
    $(this).attr("formaction", url);
}