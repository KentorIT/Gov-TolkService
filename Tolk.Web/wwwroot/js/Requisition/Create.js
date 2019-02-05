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

function checkSessionEndedAt() {

    var validatorId = "#sessionEndedAtValidator";
    var validatorIdStart = "#sessionStartedAtValidator";
    $(validatorId).hide();
    $(validatorIdStart).hide();
    if (!checkSessionEndedAtBeforeNow()) {
        triggerValidator("Faktisk sluttid kan inte ske efter nutid", $(validatorId));
        return false;
    }
    else if (!checkSessionEndedAtAfterStart()) {
        triggerValidator("Faktisk sluttid kan inte ske före faktisk starttid", $(validatorId));
        return false;
    }
    //check mealbreaks
    var message = checkEachMealBreak(getSessionStartDate(), getSessionEndDate(), true);
    if (message !== "") {
        if (message.includes("slut")) {
            triggerValidator(message, $(validatorId));
        }
        else {
            triggerValidator(message, $(validatorIdStart));
            $(validatorId).hide();
        }
        return false;
    }
    return true;
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

$(function () {
    var currentId = 0;

    $("body").on("click", ".add-mealbreak-button", function () {
        var target = $($(this).data("target"));
        AddMealbreak(target);
    });

    function AddMealbreak(target) {
        //empty validators
        $('#mealBreakEndedAtAtValidator').empty();
        $('#mealBreakStartedAtAtValidator').empty();

        var sessionStart = $('#SessionStartedAt_Date').val();
        var sessionEnd = $('#SessionEndedAt_Date').val();
        var sessionStartHour = $('#SessionStartedAt_Hour').val();
        var sessionEndHour = $('#SessionEndedAt_Hour').val();

        var newSessionStartHour = sessionStartHour == 23 ? 0 : Number(sessionStartHour) + 1;
        var newSessionEndHour = sessionEndHour == 0 ? 23 : Number(sessionEndHour) - 1;

        target.find("#MealBreakStartAt_Minute").val(0).trigger("change");
        target.find("#MealBreakEndAt_Minute").val(0).trigger("change");

        //if assignment time just one day set calander and lock them
        if (sessionStart !== "" && sessionEnd === sessionStart) {
            target.find("#MealBreakStartAt_Date").val(sessionStart);
            target.find("#MealBreakEndAt_Date").val(sessionEnd);
            target.find("#MealBreakStartAt_Date").attr('disabled', true);
            target.find("#MealBreakEndAt_Date").attr('disabled', true);
            if (sessionEndHour - sessionStartHour > 1) {
                target.find("#MealBreakStartAt_Hour").val(newSessionStartHour).trigger("change");
                target.find("#MealBreakEndAt_Hour").val(newSessionEndHour).trigger("change");
            }
            else {
                target.find("#MealBreakStartAt_Hour").val(sessionStartHour).trigger("change");
                target.find("#MealBreakEndAt_Hour").val(sessionEndHour).trigger("change");
            }
        }
        else {
            target.find("#MealBreakStartAt_Date").val(sessionStart);
            target.find("#MealBreakEndAt_Date").val(sessionStart);
            target.find("#MealBreakStartAt_Hour").val(newSessionStartHour).trigger("change");
            target.find("#MealBreakEndAt_Hour").val(newSessionEndHour).trigger("change");
            target.find("#MealBreakStartAt_Date").attr('disabled', false);
            target.find("#MealBreakEndAt_Date").attr('disabled', false);
        }

        var $form = target.find('form:first');
        target.bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
    }

    $("body").on("click", ".remove-mealbreak-row", function () {
        var $tbody = $(this).closest("tbody");
        $(this).closest("tr").remove();
        //Reindex 0 to n
        var $rows = $tbody.find("tr");
        currentId = 0;
        if ($rows.length === 0) {
            $('.mealbreak-list').addClass("d-none");
        } else {
            $rows.each(function () {
                $(this).find("input").each(function () {
                    var $id = $(this).prop("id").match(/\d+/);
                    $(this).prop("id", $(this).prop("id").replace($id, currentId));
                    $(this).prop("name", $(this).prop("name").replace($id, currentId));
                });
                currentId++;
            });
        }
        validateControls();
    });

    $("body").on("click", ".save-mealbreak", function (event) {
        event.preventDefault();

        $('#mealBreakEndedAtAtValidator').empty();
        $('#mealBreakStartedAtAtValidator').empty();
        var modalContent = $(this).parents(".modal-content");

        var startSessionDateAndTime = getSessionStartDate();
        var endSessionDateAndTime = getSessionEndDate();

        var startMealDateAndTime = getDate($("#MealBreakStartAt_Date").val(), $("#MealBreakStartAt_Hour").val(), $("#MealBreakStartAt_Minute").val());
        var endMealDateAndTime = getDate($("#MealBreakEndAt_Date").val(), $("#MealBreakEndAt_Hour").val(), $("#MealBreakEndAt_Minute").val());

        if (startMealDateAndTime - endMealDateAndTime === 0) {
            triggerValidator("Sluttid kan inte vara samma som starttid", $('#mealBreakEndedAtAtValidator'));
            return;
        }
        else if (startMealDateAndTime > endMealDateAndTime) {
            triggerValidator("Sluttid kan inte vara före starttid", $('#mealBreakEndedAtAtValidator'));
            return;
        }
        if (startSessionDateAndTime - startMealDateAndTime === 0) {
            triggerValidator("Måltidspausen kan inte starta samtidigt som uppdraget startar", $('#mealBreakStartedAtAtValidator'));
            return;
        }
        if (endSessionDateAndTime - endMealDateAndTime === 0) {
            triggerValidator("Måltidspausen kan inte sluta samtidigt som uppdraget slutar", $('#mealBreakEndedAtAtValidator'));
            return;
        }

        if (startSessionDateAndTime > startMealDateAndTime) {
            triggerValidator("Måltidspausen kan inte starta innan uppdraget startar", $('#mealBreakStartedAtAtValidator'));
            return;
        }
        if (endSessionDateAndTime < endMealDateAndTime) {
            triggerValidator("Måltidspausen kan inte sluta efter uppdraget slutar", $('#mealBreakEndedAtAtValidator'));
            return;
        }
        var validationMessage = checkEachMealBreak(startMealDateAndTime, endMealDateAndTime, false);
        if (validationMessage !== "") {
            triggerValidator(validationMessage, $('#mealBreakEndedAtAtValidator'));
            return;
        }

        if (modalContent.find("form").valid()) {
            var $hidden = $("#baseMealBreaks").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentId));
                $(this).prop("name", $(this).prop("name").replace("0", currentId));
            });

            //check modal for input
            var totalStartAt = $("#MealBreakStartAt_Date").val() + ' ' + getTimeString($("#MealBreakStartAt_Hour").val()) + ':' + getTimeString($("#MealBreakStartAt_Minute").val());
            var totalEndAt = $("#MealBreakEndAt_Date").val() + ' ' + getTimeString($("#MealBreakEndAt_Hour").val()) + ':' + getTimeString($("#MealBreakEndAt_Minute").val());

            var startAtSelector = "#MealBreaks_" + currentId + "__StartAtTemp";
            $hidden.find(startAtSelector).val(totalStartAt);

            var endAtSelector = "#MealBreaks_" + currentId + "__EndAtTemp";
            $hidden.find(endAtSelector).val(totalEndAt);

            //Add the info to the cloned hidden fields, add a row to the table
            $('.mealbreak-table > tbody:last-child').append('<tr>' +
                '<td class="table-start-column">' + $hidden.html() + $hidden.find(startAtSelector).val() + '</td>' +
                '<td class="table-end-column">' + $hidden.find(endAtSelector).val() + '</td>' +
                '<td class="table-remove-column"><span class="remove-mealbreak-row bold">&times;</span></td>' +
                '</tr>');

            currentId++;

            //Make the table visible, if this is the first visible row.
            $('.mealbreak-list').removeClass("d-none");
            //Close dialog
            $("#addMealBreak").modal("hide");
        }
    });
    currentId = $(".mealbreak-table > tbody > tr").length;
});

function getSessionStartDate() {
    return getDate($('#SessionStartedAt_Date').val(), $('#SessionStartedAt_Hour').val(), $('#SessionStartedAt_Minute').val());
}

function getSessionEndDate() {
    return getDate($('#SessionEndedAt_Date').val(), $('#SessionEndedAt_Hour').val(), $('#SessionEndedAt_Minute').val());
}

function getDate(date, hour, min) {
    hour = hour.length === 1 ? "0" + hour : hour;
    min = min.length === 1 ? "0" + min : min;
    return new Date(date + "T" + hour + ":" + min + ":00");
}

function getTimeString(timeValue) {
    return timeValue.length === 1 ? "0" + timeValue : timeValue;
}

function checkEachMealBreak(start, end, checkAgainstSessionTime) {
    var message = "";
    var mbTbody = $("#mealBreak-tbody");
    var $rows = mbTbody.find("tr");
    if ($rows.length === 0) {
        return message;
    }
    else {
        $rows.each(function () {
            var tdStart = $(this).find(".table-start-column").text().trim();
            var tdEnd = $(this).find(".table-end-column").text().trim();
            var testStart = tdStart.substring(0, 10) + "T" + tdStart.substring(11, 16) + ":00";
            var testEnd = tdEnd.substring(0, 10) + "T" + tdEnd.substring(11, 16) + ":00";
            //check added mealbreak against previous mealbreaks
            if (!checkAgainstSessionTime) {
                if (new Date(testStart) - end === 0 || new Date(testEnd) - start === 0 || new Date(testStart) - start === 0 || new Date(testEnd) - end === 0) {
                    message = "Denna måltidspaus startar eller slutar precis samtidigt som en tidigare sparad måltidspaus startar eller slutar. Det måste vara mellanrum mellan måltidspauser.";
                    return;
                }
                else if ((new Date(testStart) > start && new Date(testStart) < end) || (new Date(testStart) < start && new Date(testEnd) > start)) {
                    message = "Denna måltidspaus överlappar med en tidigare sparad måltidspaus.";
                    return;
                }
            }
            //check against session start and end
            else {
                if (new Date(testStart) <= start) {
                    message = "Minst en måltidspaus börjar innan eller samtidigt som den faktiska starttiden för uppdraget. Ta bort måltidspausen eller ändra faktisk starttid.";
                    return;
                }
                else if (new Date(testEnd) >= end) {
                    message = "Minst en måltidspaus slutar efter  eller samtidigt som den faktiska sluttiden för uppdraget. Ta bort måltidspausen eller ändra faktisk sluttid.";
                    return;
                }
            }
        });
        return message;
    }
    return message;
}