//Edit Defaultsettings (interpreterlocation must not be same)
$(function () {
    $("body").on("change", "#RankedInterpreterLocationFirst, #RankedInterpreterLocationSecond, #RankedInterpreterLocationThird", function (event) {
        checkInterpreterLocation();
    });
});

function checkInterpreterLocation() {
    var no1 = $("#RankedInterpreterLocationFirst").val();
    var no2 = $("#RankedInterpreterLocationSecond").val();
    var no3 = $("#RankedInterpreterLocationThird").val();
    var validator = $("#interpreterLocationValidator");
    if (hasValue(no1) && no1 === no2) {
        triggerValidator("Inställelsesätt i första och andra hand kan inte vara samma", validator);
    }
    else if (hasValue(no1) && no1 === no3) {
        triggerValidator("Inställelsesätt i första och tredje hand kan inte vara samma", validator);
    }
    else if (hasValue(no2) && no2 === no3) {
        triggerValidator("Inställelsesätt i andra och tredje hand kan inte vara samma", validator);
    }
    else if (!hasValue(no1) && (hasValue(no2) || hasValue(no3))) {
        triggerValidator("Välj inställelsesätt i första hand om du valt inställelsesätt i andra eller tredjehand", validator);
    }
    else if (!hasValue(no2) && hasValue(no3)) {
        triggerValidator("Välj inställelsesätt i andra hand om du valt inställelsesätt i tredjehand", validator);
    }
    else {
        validator.hide();
        $('.save-defaultsettings').attr('disabled', false);
    }
}

function hasValue(selectedValue) {
    return (selectedValue !== null && selectedValue.length > 0);
}

function triggerValidator(message, validatorId) {
    $('.save-defaultsettings').attr('disabled', true);
    validatorId.empty();
    validatorId.append(message);
    validatorId.show();
}