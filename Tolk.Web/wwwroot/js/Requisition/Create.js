/* eslint-disable eqeqeq */
﻿$('#TimeWasteTotalTime').change(function () { validateControls(); });
$('#TimeWasteIWHTime').change(function () { validateControls(); });
$('#Outlay').change(function () { validateControls(); });
$('#CarCompensation').change(function () { validateControls(); });
$('#PerDiem').change(function () { validateControls(); });
$('#SessionEndedAt').change(function () { validateControls(); });
$('#SessionStartedAt').change(function () { validateControls(); });

function validateControls() {
    if (checkWasteTime() &&
        checkTravelCosts($('#Outlay').val(), "Kontrollera värdet för utlägg (finns ingen kostnad anges 0) <br \><br \>", $('#outlayValidator')) &&
        checkTravelCosts($('#CarCompensation').val(), "Kontrollera värdet för bilersättning (finns ingen kostnad anges 0) <br \><br \>", $('#carCompensationValidator')) &&
        checkTravelCosts($('#PerDiem').val(), "Kontrollera värdet för traktamente (ange 0 om det inte ska erhållas något traktamente) <br \><br \>", $('#perDiemValidator')) &&
        checkSessionEndedAt()) { $('#create').attr('disabled', false); }
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

function checkTravelCosts(cost, message, validatorId) {
    if (cost === "" || isNaN(cost) || (parseInt(cost) < 0 || parseInt(cost) > 100000)) {
        triggerValidator(message, validatorId);
        return false;
    }
    else {
        validatorId.hide();
        return true;
    }
}

function checkSessionEndedAt() {
    var validatorId = "#sessionEndedAtValidator";

    if (!checkSessionEndedAtBeforeNow()) {
        triggerValidator("Faktisk sluttid kan inte ske efter nutid", $(validatorId));
        return false;
    }
    else if (!checkSessionEndedAtAfterStart()) {
        triggerValidator("Faktisk sluttid kan inte ske före faktisk starttid", $(validatorId));
        return false;
    }
    else {
        $(validatorId).hide();
        return true;
    }
}

function checkSessionEndedAtAfterStart() {
    var date = new Date($("#SessionEndedAt_Date").val());
    var hour = $("#SessionEndedAt_Hour").val();
    var minute = $("#SessionEndedAt_Minute").val();
    var startdate = new Date($("#SessionStartedAt_Date").val());
    if (date !== "" && hour !== "" && minute !== "") {
        var starthour = $("#SessionStartedAt_Hour").val();
        var startminute = $("#SessionStartedAt_Minute").val();
        if (date.equalsDate(new Date(startdate))) {
            if (Number(starthour) > Number(hour)) {
                return false;
            } else if (Number(hour) === Number(starthour)) {
                return Number(startminute) < Number(minute);
            }
        }
        else if (date.before(startdate)) {
            return false;
        }
    }
    return true;
}

function checkSessionEndedAtBeforeNow() {
    var date = new Date($("#SessionEndedAt_Date").val());
    var hour = $("#SessionEndedAt_Hour").val();
    var minute = $("#SessionEndedAt_Minute").val();
    if (date !== "" && hour !== "" && minute !== "") {
        var now = new Date($("#now").val());
        if (date.equalsDate(now)) {
            var nowHour = now.getHours();
            if (nowHour < Number(hour)) {
                return false;
            } else if (nowHour === Number(hour)) {
                return now.getMinutes() > Number(minute);
            }
        }
        else if (date.after(now)) {
            return false;
        }
    }

    return true;
}

function triggerValidator(message, validatorId) {
    validatorId.empty();
    validatorId.append(message);
    validatorId.show();
}