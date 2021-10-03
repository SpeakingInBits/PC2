window.onload = function () {
    window.scrollTo(0, yPos);
}

function getYpos() {
    let y = document.documentElement.scrollTop;
    let url = $(this).attr("href");
    url = url.replace("tempY", y);
    console.log(url);
    $(this).attr("href", url);
}