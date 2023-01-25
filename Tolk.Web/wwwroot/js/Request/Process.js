$(function () {

    var checkRequirements = function checkRequirements() {
        $('.disable-at-required').attr('disabled', false);

        $("input[id$='CanMeetRequirement']").each(function () {
            var isChecked = $(this).is(':checked');
            var isRequired = $('#' + $(this).attr('id').replace('CanMeetRequirement', 'IsRequired')).attr('value') === 'True';

            if (isRequired && !isChecked) {
                $('.disable-at-required').attr('disabled', true);
            }
        });
    };

    checkRequirements();

    $("#InterpreterCompetenceLevel, #NewInterpreterOfficialInterpreterId").change(function () {
        validateInterpreter(".interpreter-panel", $('#InterpreterId').val(), $('#NewInterpreterOfficialInterpreterId').val(), $("#InterpreterCompetenceLevel").val(), $("#OrderId").val(), null, $("#InterpreterId option:selected").data('additional') === "Protected");
    });

    $(document).ready(function () {
        setInterpreter();
        setExpectedTravelcost();
        toggleFullAnswerPanel();
    });

    $('#InterpreterId').change(function () {
        setInterpreter();
    });

    $("body").on("click", "input[name=SetLatestAnswerTimeForCustomer]", function () {
        $("#SetLatestAnswerTimeForCustomerValidator").hide();
        if ($(this).val() === "Yes") {
            $("#latestAnswerTimeForCustomer").show();
        }
        else {
            $("#latestAnswerTimeForCustomer").hide();
        }
    });

    $("body").on("change", "#FullAnswer", function () {
        toggleFullAnswerPanel();
    });

    function toggleFullAnswerPanel() {
        if ($("#FullAnswer").is(":hidden") || $("#FullAnswer").is(":checked")) {
            $(".full-answer-panel").show();
            $("#Answer").show();
            $("#Accept").hide();
            $(".required-on-accept-panel").hide();
        }
        else {
            $(".full-answer-panel").hide();
            $("#Answer").hide();
            $("#Accept").show();
            $(".required-on-accept-panel").show();
       }
    }

    $("#Answer").closest("form").on("submit", function () {
        if (!validateSetLatestAnswerTimeForCustomer() || !validateLatestAnswerTimeWithinValidTimeSpan()) {
            return false;
        };
        $("#Answer").disableOnSubmit();
    });

    $("#Change").closest("form").on("submit", function () {
        if (!validateSetLatestAnswerTimeForCustomer() || !validateLatestAnswerTimeWithinValidTimeSpan()) {
            return false;
        };
        $("#Change").disableOnSubmit();
    });

    $("#Decline").closest("form").on("submit", function () {  
        if (!validateDeclineMessageExist()) {
            return false;
        }   
        $("#Decline").disableOnSubmit();
    });

    $("body").on("click", "#Accept", function () {
        var $form = $(this).closest("form");
        var $action = $form.prop("action");
        $form.prop("action", $action.replace("/Answer", "/Accept"));
        $form.submit();
        $("#Accept").disableOnSubmit();
    });

    function setInterpreter() {
        if ($("#InterpreterId option:selected").val() === "-1") {
            $('#new-interpreter').collapse('show');
        }
        else {
            $('#new-interpreter').collapse('hide');
            validateInterpreter(".interpreter-panel", $('#InterpreterId').val(), $('#NewInterpreterOfficialInterpreterId').val(), $("#InterpreterCompetenceLevel").val(), $("#OrderId").val(), null, $("#InterpreterId option:selected").data('additional') === "Protected");
        }
    }

    function setExpectedTravelcost() {
        if ($("#InterpreterLocation option:selected").val() === "OffSitePhone" || $("#InterpreterLocation option:selected").val() === "OffSiteVideo" || $("#InterpreterLocation option:selected").val() === "") {
            $('#set-expected-travel-costs').collapse('hide');
        }
        else if ($("#InterpreterLocation").val() === "OffSitePhone" || $("#InterpreterLocation").val() === "OffSiteVideo") {
            $('#set-expected-travel-costs').collapse('hide');
        }
        else {
            $('#set-expected-travel-costs').collapse('show');
        }
    }

    var validateSetLatestAnswerTimeForCustomer = function () {
        if (!$("#SetLatestAnswerTimeForCustomer").is(":visible") || $("[name=SetLatestAnswerTimeForCustomer]").filter(":checked").length > 0) {
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

    function validateDeclineMessageExist()
    {
        if ($("#DenyMessage").val().length > 0) {
            return true;
        }
        return false;
    }


    var setLatestAnswerDateTimeSpan = function () {
        if ($("#OrderViewModel_StartAt").val() !== undefined) {
            var startTime = new Date($('#OrderViewModel_StartAt').val().replace(" ", "T").replace(" ", ""));
            $("#LatestAnswerTimeForCustomer_Date").datepicker("setStartDate", new Date($("#now").val()).zeroTime());
            $("#LatestAnswerTimeForCustomer_Date").datepicker("setEndDate", startTime.zeroTime());
        }
    };

    setLatestAnswerDateTimeSpan();

    function checkLatestAnswerTime(latestAnswerTime) {
        var now = new Date($("#now").val());
        if (now - latestAnswerTime === 0 || now > latestAnswerTime) {
            return "Angiven sista svarstid har passerats";
        }

        var startTime = new Date($('#OrderViewModel_StartAt').val().replace(" ", "T").replace(" ", ""));
        if (startTime - latestAnswerTime === 0 || startTime < latestAnswerTime) {
            return "Sista svarstid ska vara innan uppdraget startar";
        }
        return "";
    }

    $('#InterpreterLocation').change(function () {
        setExpectedTravelcost();
    });

    $('.checkbox').change(function () {
        checkRequirements();
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
