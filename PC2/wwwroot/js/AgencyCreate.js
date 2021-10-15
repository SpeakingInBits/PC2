let services = new Array();

window.onload = function () {
    let servicesButton = document.getElementById("category-submit");
    servicesButton.onclick = addService;
}

function addService() {
    if ($('[name="list-of-categories"]').val().trim() != "") {
        services.push($('[name="list-of-categories"]').val());
        document.getElementById("category-success").innerHTML = $('[name="list-of-categories"]').val() + " added";
        document.getElementById("list-of-service").value = "";
    }
}