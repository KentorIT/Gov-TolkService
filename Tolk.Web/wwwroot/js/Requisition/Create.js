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



$(function () {
    var currentId = 0;

    $("body").on("click", ".add-mealbreak-button", function () {
        var target = $($(this).data("target"));
        AddMealbreak(target);
    });

    function AddMealbreak(target) {
        //maybe we should empty dates/check date lock date etc 
        var sessionStart = $('#SessionStartedAt_Date').val();
        var sessionEnd = $('#SessionEndedAt_Date').val();
        var sessionStartTime = $('#SessionStartedAt_Hour').val();
        var sessionEndTime = $('#SessionEndedAt_Hour').val();
        //if assignment time just one day set calander and lock it (maybe we should not display them)
        if (sessionStart !== "" && sessionEnd === sessionStart) {
            target.find("#MealBreakStartAt_Date").val(sessionStart);
            target.find("#MealBreakEndAt_Date").val(sessionEnd);
            target.find("#MealBreakStartAt_Date").attr('disabled', true);
            target.find("#MealBreakEndAt_Date").attr('disabled', true);
        }
        else {
            target.find("#MealBreakStartAt_Date").val(sessionStart);
            target.find("#MealBreakEndAt_Date").val(sessionStart);
            target.find("#MealBreakStartAt_Date").attr('disabled', false);
            target.find("#MealBreakEndAt_Date").attr('disabled', false);
        }
        //todo check starthour and no of hours for assignment and add some hours?
        target.find("#MealBreakStartAt_Hour").val(sessionStartTime).trigger("change");
        target.find("#MealBreakEndAt_Hour").val(sessionEndTime).trigger("change");

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
    });

    $("body").on("click", ".save-mealbreak", function (event) {
        event.preventDefault();
        var modalContent = $(this).parents(".modal-content");

        //Before we start, validate the form!
        if (modalContent.find("form").valid()) {
            var $hidden = $("#baseMealBreaks").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentId));
                $(this).prop("name", $(this).prop("name").replace("0", currentId));
            });

            //check modal for input
            var startAtDate = modalContent.find("#MealBreakStartAt_Date").val();
            var startAtHour = modalContent.find("#MealBreakStartAt_Hour").val().length === 1 ? "0" + modalContent.find("#MealBreakStartAt_Hour").val() : modalContent.find("#MealBreakStartAt_Hour").val();
            var startAtMin = modalContent.find("#MealBreakStartAt_Minute").val().length === 1 ? "0" + modalContent.find("#MealBreakStartAt_Minute").val() : modalContent.find("#MealBreakStartAt_Minute").val();

            var endAtDate = modalContent.find("#MealBreakEndAt_Date").val();
            var endAtHour = modalContent.find("#MealBreakEndAt_Hour").val().length === 1 ? "0" + modalContent.find("#MealBreakEndAt_Hour").val() : modalContent.find("#MealBreakEndAt_Hour").val();
            var endAtMin = modalContent.find("#MealBreakEndAt_Minute").val().length === 1 ? "0" + modalContent.find("#MealBreakEndAt_Minute").val() :modalContent.find("#MealBreakEndAt_Minute").val();

            var totalStartAt = startAtDate + ' ' + startAtHour + ':' + startAtMin;
            var totalEndAt = endAtDate + ' ' + endAtHour + ':' + endAtMin;

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
});