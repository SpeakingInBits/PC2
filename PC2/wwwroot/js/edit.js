let removedServices = new Array();
let addedServices = new Array();

window.onload = function () {
    let removeServices = document.getElementById("remove-category-submit");
    let addServices = document.getElementById("category-submit");
    let submit = document.getElementById("submit");
    removeServices.onclick = removeService;
    addServices.onclick = addService;
    submit.onclick = submitForm;
}

function removeService() {
    if ($('[name="list-of-existing-categories"]').val().trim() != "") {
        removedServices.push($('[name="list-of-existing-categories"]').val());
        document.getElementById("existing-category-success").innerHTML = $('[name="list-of-existing-categories"]').val() + " removed";
        document.getElementById("list-of-existing-service").value = "";
    }
}

function addService() {
    if ($('[name="list-of-categories"]').val().trim() != "") {
        addedServices.push($('[name="list-of-categories"]').val());
        document.getElementById("category-success").innerHTML = $('[name="list-of-categories"]').val() + " added";
        document.getElementById("list-of-service").value = "";
    }
}

function submitForm() {
    let url = $(this).attr("formaction");

    let newString = JSON.stringify(addedServices);
    newString = encodeURIComponent(newString);
    url = url.replace("tempAdded", newString);
    newString = JSON.stringify(removedServices);
    newString = encodeURIComponent(newString);
    url = url.replace("tempRemoved", newString);
    $(this).attr("formaction", url);
}