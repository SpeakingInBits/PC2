let services = new Array();

window.onload = function () {
    let servicesButton = document.getElementById("category-submit");
    let submitButton = document.getElementById("submit");
    servicesButton.onclick = addService;
    submitButton.onclick = onSubmit;
}

function addService() {
    if ($('[name="list-of-categories"]').val().trim() != "") {
        services.push($('[name="list-of-categories"]').val());
        document.getElementById("category-success").innerHTML = $('[name="list-of-categories"]').val() + " added";
        document.getElementById("list-of-service").value = "";
    }
}

function onSubmit() {
    let url = $(this).attr("formaction");

    let newString = JSON.stringify(services);
    //newString = encodeURIComponent(newString);
    url = url.replace("tempServices", newString);
    $(this).attr("formaction", url);
}