window.onload = function () {
    if (yPos != 0) {
        window.scrollTo(0, yPos);
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

    let input = $(this).siblings("#agencyName").val();
    url = url.replace("agencyName", input);
    $(this).attr("formaction", url);
}