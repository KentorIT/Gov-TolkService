$(function () {

    var checkRequirements = function () {
        var requirementNotFulfilled = true;
        $("input[id$='CanMeetRequirement']:visible").each(function () {
            var isChecked = $(this).is(':checked');
            var isRequired = $('#' + $(this).attr('id').replace('CanMeetRequirement', 'IsRequired')).attr('value') === 'True';

            if (isRequired && !isChecked) {
                $('#Answer').attr('disabled', true);
                $('#Accept').attr('disabled', true);
                requirementNotFulfilled = false;
                return false;
            }
        });
        return requirementNotFulfilled;
    };


    $("body").on("change", "#FullAnswer", function () {
        toggleFullAnswerPanel();
    });

    function toggleFullAnswerPanel() {
        if ($("#FullAnswer").is(":hidden") || $("#FullAnswer").is(":checked")) {
            $(".full-answer-panel").show();
            $("#Answer").show();
            $("#Accept").hide();
            $(".required-competence-on-accept-panel").hide();
        }
        else {
            $(".full-answer-panel").hide();
            $("#Answer").hide();
            $("#Accept").show();
            $(".required-competence-on-accept-panel").show();
        }
    }

    var checkSameInterpreter = function () {
        if ($("#InterpreterAnswerModel_InterpreterId").val() != "" && $("#InterpreterAnswerModel_InterpreterId").val() !== "-1"
            && $("#InterpreterAnswerModel_InterpreterId").val() === $("#ExtraInterpreterAnswerModel_InterpreterId").val()
            && $("#InterpreterAnswerModel_InterpreterId option:selected").data('additional') !== "Protected") {
            triggerValidator("Det går inte att tillsätta samma tolk, ändra på någon av de tillsatta tolkarna", $("#interpreterIdValidator"))
            return false;
        }
        else {
            $("#interpreterIdValidator").hide();
            return true;
        }
    };

    function clientValidate() {
        if (checkSameInterpreter() && checkRequirements()) {
            $('#Answer').attr('disabled', false);
            $('#Accept').attr('disabled', false);
        }
    }

    function triggerValidator(message, validatorId) {
        $('#Answer').attr('disabled', true);
        $('#Accept').attr('disabled', true);
       validatorId.empty();
        validatorId.append(message);
        validatorId.show();
    }

    var setInterpreter = function (panel) {
        if ($(panel + " select[id$='InterpreterId'] option:selected").val() === "-1") {
            $(panel + " .new-interpreter").collapse('show');
        } else {
            $(panel + " .new-interpreter").collapse('hide');
            validateInterpreter(panel, $(panel + " select[id$='InterpreterId']").val(), $(panel + " input[id$='NewInterpreterOfficialInterpreterId']").val(), $(panel + " select[id$='InterpreterCompetenceLevel']").val(), null, $("#OrderGroupId").val(), $(panel + " select[id$='InterpreterId'] option:selected").data('additional') === "Protected");
        }
    };

    var handlePartialDecline = function (panel) {
        if ($(panel + " select[id$='InterpreterId'] option:selected").val() === "-2") {
            $(panel + " .decline-message-panel").collapse('show');
            $(panel + " .interpreter-information-panel").collapse('hide');
        } else {
            $(panel + " .decline-message-panel").collapse('hide');
            $(panel + " .interpreter-information-panel").collapse('show');
        }
    };

    $('.interpreter-information-panel').on('shown.bs.collapse', function () {
        clientValidate();
    }).on('hidden.bs.collapse', function () {
        clientValidate();
    });
    var setExpectedTravelcost = function () {
        $(".expected-travel-costs-panel, #latestAnswerTimeForCustomer-panel").collapse($("#InterpreterLocation option:selected").val() === "OffSitePhone" || $("#InterpreterLocation option:selected").val() === "OffSiteVideo" || $("#InterpreterLocation option:selected").val() === "" ?
            'hide' : 'show');
    };

    $("body").on("click", "input[name=SetLatestAnswerTimeForCustomer]", function () {
        $("#SetLatestAnswerTimeForCustomerValidator").hide();
        if ($(this).val() === "Yes") {
            $("#latestAnswerTimeForCustomer").show();
        }
        else {
            $("#latestAnswerTimeForCustomer").hide();
        }
    });

    var validateSetLatestAnswerTimeForCustomer = function () {
        if (!$("input[name=SetLatestAnswerTimeForCustomer]").is(":visible") || $("input[name=SetLatestAnswerTimeForCustomer]").filter(":checked").length > 0) {
            return true;
        }
        else {
            $("#SetLatestAnswerTimeForCustomerValidator").show();
            return false;
        }
    };

    function validateLatestAnswerTimeWithinValidTimeSpan() {
        if (!$("#LatestAnswerTimeForCustomer_Date").is(":visible") || $("#LatestAnswerTimeForCustomer_Hour").val() == "" || $("#LatestAnswerTimeForCustomer_Minute").val() == "") {
            return true;
        }
        var latestAnswerTime = new Date($("#LatestAnswerTimeForCustomer_Date").val());
        latestAnswerTime.setHours($("#LatestAnswerTimeForCustomer_Hour").val());
        latestAnswerTime.setMinutes($("#LatestAnswerTimeForCustomer_Minute").val());
        var validatorId = $("#LatestAnswerTimeForCustomerValidator");
        var message = checkLatestAnswerTime(latestAnswerTime);
        if (message.length > 0) {
            validatorId.empty();
            validatorId.append(message);
            validatorId.show();
            return false;
        }
        else {
            validatorId.empty();
            validatorId.hide();
        }
        return true;
    };

    var setLatestAnswerDateTimeSpan = function () {
        if ($("#OccasionList_FirstStartDateTime").val() !== undefined) {
            var startTime = new Date($("#OccasionList_FirstStartDateTime").val().replace(" ", "T").replace(" ", ""));
            $("#LatestAnswerTimeForCustomer_Date").datepicker("setStartDate", new Date($("#now").val()).zeroTime());
            $("#LatestAnswerTimeForCustomer_Date").datepicker("setEndDate", startTime.zeroTime());
        }
    };

    function checkLatestAnswerTime(latestAnswerTime) {
        var now = new Date($("#now").val());
        if (now - latestAnswerTime === 0 || now > latestAnswerTime) {
            return "Angiven sista svarstid har passerats";
        }

        var startTime = new Date($("#OccasionList_FirstStartDateTime").val().replace(" ", "T").replace(" ", ""));
        if (startTime - latestAnswerTime === 0 || startTime < latestAnswerTime) {
            return "Sista svarstid ska vara innan första uppdraget startar";
        }
        return "";
    }

    $(document).ready(function () {

        checkRequirements();
        setExpectedTravelcost();
        setInterpreter();
        setLatestAnswerDateTimeSpan();
        toggleFullAnswerPanel();
    });

    $("#InterpreterAnswerModel_InterpreterCompetenceLevel, #InterpreterAnswerModel_NewInterpreterOfficialInterpreterId").change(function () {
        validateInterpreter(".interpreter-selection-panel", $('#InterpreterAnswerModel_InterpreterId').val(), $('#InterpreterAnswerModel_NewInterpreterOfficialInterpreterId').val(), $("#InterpreterAnswerModel_InterpreterCompetenceLevel").val(), null, $("#OrderGroupId").val(), $("#InterpreterAnswerModel_InterpreterId option:selected").data('additional') === "Protected");
    });

    $("#ExtraInterpreterAnswerModel_InterpreterCompetenceLevel, #ExtraInterpreterAnswerModel_NewInterpreterOfficialInterpreterId").change(function () {
        validateInterpreter(".extra-interpreter-selection-panel", $('#ExtraInterpreterAnswerModel_InterpreterId').val(), $('#ExtraInterpreterAnswerModel_NewInterpreterOfficialInterpreterId').val(), $("#ExtraInterpreterAnswerModel_InterpreterCompetenceLevel").val(), null, $("#OrderGroupId").val(), $("#ExtraInterpreterAnswerModel_InterpreterId option:selected").data('additional') === "Protected");
    });

    $('#InterpreterAnswerModel_InterpreterId').change(function () {
        clientValidate();
        setInterpreter(".interpreter-selection-panel");
    });

    $('#ExtraInterpreterAnswerModel_InterpreterId').change(function () {
        clientValidate();
        setInterpreter(".extra-interpreter-selection-panel");
        handlePartialDecline(".outer-extra-interpreter-panel");
    });

    $("body").on("click", "#Accept", function () {
        var $form = $(this).closest("form");
        var $action = $form.prop("action");
        $form.prop("action", $action.replace("/Answer", "/Accept"));
        $form.submit();
        $("#Accept").disableOnSubmit();
    });

    $("#Answer").closest("form").on("submit", function () {
        if (!validateSetLatestAnswerTimeForCustomer() || !validateLatestAnswerTimeWithinValidTimeSpan()) {
            return false;
        };
        $("#Answer").disableOnSubmit();
    });

    $('#InterpreterLocation').change(function () {
        setExpectedTravelcost();
    });

    $('.checkbox').change(function () {
        clientValidate();
    });

    //handle cancel/back to previous page
    $("body").on("click", "#cancel-go-back", function (event) {
        //if from link in email it can be first page (no history) or login page - then don't go back, go to start page 
        if (document.referrer === "" || document.referrer.indexOf("Login") > 0) {
            window.location.href = tolkBaseUrl + "Home/Index";
        }
        else {
            history.back();
        }
    });
});

$(window).on("beforeunload", function () {
    var requestGroupId = $("#RequestGroupId").val();
    if (requestGroupId > 0) {
        var $url = tolkBaseUrl + "RequestGroup/DeleteView/" + requestGroupId;
        $.ajax({
            type: "DELETE",
            url: $url,
            data: { __RequestVerificationToken: getAntiForgeryToken() },
            dataType: "json",
            async: false
        });
    }
});

$(document).ready(function () {
    var requestGroupId = $("#RequestGroupId").val();
    if (requestGroupId > 0) {
        var $url = tolkBaseUrl + "RequestGroup/AddView/" + requestGroupId;
        $.ajax({
            type: "POST",
            url: $url,
            data: { __RequestVerificationToken: getAntiForgeryToken() },
            dataType: "json"
        });
    }
});



