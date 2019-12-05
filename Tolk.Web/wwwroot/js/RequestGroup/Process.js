$(function () {

    var checkRequirements = function () {
        var requirementNotFulfilled = true;
        $("input[id$='CanMeetRequirement']:visible").each(function () {
            var isChecked = $(this).is(':checked');
            var isRequired = $('#' + $(this).attr('id').replace('CanMeetRequirement', 'IsRequired')).attr('value') === 'True';

            if (isRequired && !isChecked) {
                $('#Accept').attr('disabled', true);
                requirementNotFulfilled = false;
                return false;
            }
        });
        return requirementNotFulfilled;
    };

    var checkSameInterpreter = function () {
        //TODO ska få lägga till två skyddade
        if ($("#InterpreterAnswerModel_InterpreterId").val() != "" && $("#InterpreterAnswerModel_InterpreterId").val() !== "-1" && $("#InterpreterAnswerModel_InterpreterId").val() === $("#ExtraInterpreterAnswerModel_InterpreterId").val()) {
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
            $('#Accept').attr('disabled', false);
        }
    }

    function triggerValidator(message, validatorId) {
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
            validateInterpreter(panel, $(panel + " select[id$='InterpreterId']").val(), $(panel + " input[id$='NewInterpreterOfficialInterpreterId']").val(), $(panel + " select[id$='InterpreterCompetenceLevel']").val(), null, $("#OrderGroupId").val());
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
        $(".expected-travel-costs-panel").collapse($("#InterpreterLocation option:selected").val() === "OffSitePhone" || $("#InterpreterLocation option:selected").val() === "OffSiteVideo" || $("#InterpreterLocation option:selected").val() === "" ?
            'hide' : 'show');
    };

    checkRequirements();
    setExpectedTravelcost();
    setInterpreter();

    $("#InterpreterAnswerModel_InterpreterCompetenceLevel, #InterpreterAnswerModel_NewInterpreterOfficialInterpreterId").change(function () {
        validateInterpreter(".interpreter-selection-panel", $('#InterpreterAnswerModel_InterpreterId').val(), $('#InterpreterAnswerModel_NewInterpreterOfficialInterpreterId').val(), $("#InterpreterAnswerModel_InterpreterCompetenceLevel").val(), null, $("#OrderGroupId").val());
    });

    $("#ExtraInterpreterAnswerModel_InterpreterCompetenceLevel, #ExtraInterpreterAnswerModel_NewInterpreterOfficialInterpreterId").change(function () {
        validateInterpreter(".extra-interpreter-selection-panel", $('#ExtraInterpreterAnswerModel_InterpreterId').val(), $('#ExtraInterpreterAnswerModel_NewInterpreterOfficialInterpreterId').val(), $("#ExtraInterpreterAnswerModel_InterpreterCompetenceLevel").val(), null, $("#OrderGroupId").val());
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

    $("#Accept").closest("form").on("submit", function () { $("#Accept").disableOnSubmit(); });

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

