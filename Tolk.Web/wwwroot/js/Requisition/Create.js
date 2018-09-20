$('#TimeWasteNormalTime').change(function () { checkWasteTime(); });
$('#TimeWasteIWHTime').change(function () { checkWasteTime(); });

function checkWasteTime() {

    var nT = $('#TimeWasteNormalTime').val();
    var iwhT = $('#TimeWasteIWHTime').val();
    if ((nT != "" && isNaN(parseInt(nT)) || (iwhT != "" && isNaN(parseInt(iwhT))))) {
        triggerValidator("Spilltid måste vara ett tal, ange antal minuter <br \><br \>");
    }
    else if (nT != "" && (parseInt(nT) < 30 || parseInt(nT) > 600)) {
        triggerValidator("Spilltidens värde måste vara mellan 30 och 600 minuter <br \><br \>");
    }
    else {
        nT = nT == "" ? 0 : nT;

        if (parseInt(iwhT) > parseInt(nT)) {
            triggerValidator("Spilltid för obekväm tid kan inte vara större än den totala spilltiden <br \><br \>");
        }
        else {
            $('#create').attr('disabled', false);
            $('#wasteTimeValidator').hide();
        }
    }
}

function triggerValidator(message) {
    $('#create').attr('disabled', true);
    $('#wasteTimeValidator').empty();
    $('#wasteTimeValidator').append(message);
    $('#wasteTimeValidator').show();
}