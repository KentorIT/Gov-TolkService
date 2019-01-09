$('#TimeWasteTotalTime').change(function () { validateControls(); });
$('#TimeWasteIWHTime').change(function () { validateControls(); });
$('#TravelCosts').change(function () { validateControls(); });

function validateControls() {
    if (checkWasteTime() && checkTravelCost()) { $('#create').attr('disabled', false); }
    else { $('#create').attr('disabled', true); }
}

function checkWasteTime() {
    var nT = $('#TimeWasteTotalTime').val();
    var iwhT = $('#TimeWasteIWHTime').val();
    if ((nT != "" && isNaN(parseInt(nT)) || (iwhT != "" && isNaN(parseInt(iwhT))))) {
        triggerValidator("Spilltid måste vara ett tal, ange antal minuter <br \><br \>", $('#wasteTimeValidator'));
        return false;
    }
    else if (nT != "" && (parseInt(nT) < 31 || parseInt(nT) > 600)) {
        triggerValidator("Kontrollera värden för spilltid (ska endast anges om det överstiger 30 min) <br \><br \>", $('#wasteTimeValidator'));
        return false;
    }
    else {
        nT = nT == "" ? 0 : nT;

        if (parseInt(iwhT) > parseInt(nT)) {
            triggerValidator("Spilltid för obekväm tid kan inte vara större än den totala spilltiden <br \><br \>", $('#wasteTimeValidator'));
            return false;
        }
        else {
            $('#wasteTimeValidator').hide();
            return true;
        }
    }
}

function checkTravelCost() {
    var travelCost = $('#TravelCosts').val();

    if (travelCost = "" || isNaN(parseInt(travelCost)) || (parseInt(travelCost) < 0 || parseInt(travelCost) > 100000)) {
        triggerValidator("Kontrollera värdet för resekostnad (finns ingen kostnad anges 0) <br \><br \>", $('#travelCostsValidator'));
        return false;
    }
    else {
        $('#travelCostsValidator').hide();
        return true;
    }
}

function triggerValidator(message, validatorId) {
    validatorId.empty();
    validatorId.append(message);
    validatorId.show();
}