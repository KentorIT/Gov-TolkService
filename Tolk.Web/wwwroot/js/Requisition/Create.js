$('#TimeWasteNormalTime').change(function () { checkWasteTime(); });
$('#TimeWasteIWHTime').change(function () { checkWasteTime(); });
$('#TravelCosts').change(function () { checkTravelCost(); });

function checkWasteTime() {
    var nT = $('#TimeWasteNormalTime').val();
    var iwhT = $('#TimeWasteIWHTime').val();
    if ((nT != "" && isNaN(parseInt(nT)) || (iwhT != "" && isNaN(parseInt(iwhT))))) {
        triggerValidator("Spilltid måste vara ett tal, ange antal minuter <br \><br \>", $('#wasteTimeValidator'));
    }
    else if (nT != "" && (parseInt(nT) < 30 || parseInt(nT) > 600)) {
        triggerValidator("Spilltidens värde måste vara mellan 30 och 600 minuter <br \><br \>", $('#wasteTimeValidator'));
    }
    else {
        nT = nT == "" ? 0 : nT;

        if (parseInt(iwhT) > parseInt(nT)) {
            triggerValidator("Spilltid för obekväm tid kan inte vara större än den totala spilltiden <br \><br \>", $('#wasteTimeValidator'));
        }
        else {
            $('#create').attr('disabled', false);
            $('#wasteTimeValidator').hide();
        }
    }
}

function checkTravelCost() {
    var travelCost = $('#TravelCosts').val();

    if (travelCost = "" || isNaN(parseInt(travelCost)) || (parseInt(travelCost) < 0 || parseInt(travelCost) > 100000)) {
        triggerValidator("Resekostnad måste ha ett värde mellan 0 och 100 000 kr <br \><br \>", $('#travelCostsValidator'));
    }
    else {
        $('#create').attr('disabled', false);
        $('#travelCostsValidator').hide();
    }
}

function triggerValidator(message, validatorId) {
    $('#create').attr('disabled', true);
    validatorId.empty();
    validatorId.append(message);
    validatorId.show();
}