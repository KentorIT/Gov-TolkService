$('#TimeWasteNormalTime').change(function () { validateControls(); });
$('#TimeWasteIWHTime').change(function () { validateControls(); });
$('#TravelCosts').change(function () { validateControls(); });

function validateControls() {
    if (checkWasteTime() && checkTravelCost()) { $('#create').attr('disabled', false); }
    else { $('#create').attr('disabled', true); }
}

function checkWasteTime() {
    var nT = $('#TimeWasteNormalTime').val();
    var iwhT = $('#TimeWasteIWHTime').val();
    if ((nT != "" && isNaN(parseInt(nT)) || (iwhT != "" && isNaN(parseInt(iwhT))))) {
        triggerValidator("Spilltid måste vara ett tal, ange antal minuter <br \><br \>", $('#wasteTimeValidator'));
        return false;
    }
    else if (nT != "" && (parseInt(nT) < 31 || parseInt(nT) > 600)) {
        triggerValidator("Spilltidens värde måste vara mellan 31 och 600 minuter <br \><br \>", $('#wasteTimeValidator'));
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
        triggerValidator("Resekostnad måste ha ett värde mellan 0 och 100 000 kr <br \><br \>", $('#travelCostsValidator'));
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