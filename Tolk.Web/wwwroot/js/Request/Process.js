$(function () {

    var checkRequirements = function checkRequirements() {
        $('#Accept').attr('disabled', false);

        $("input[id$='CanMeetRequirement']").each(function () {
            var isChecked = $(this).is(':checked');
            var isRequired = $('#' + $(this).attr('id').replace('CanMeetRequirement', 'IsRequired')).attr('value') === 'True';

            if (isRequired && !isChecked) {
                $('#Accept').attr('disabled', true);
            }
        });
    };

    checkRequirements();
    $("#InterpreterCompetenceLevel, #NewInterpreterOfficialInterpreterId").change(function () {
        validateInterpreter();
    });

    $(document).ready(function () {
        setInterpreter();
        setExpectedTravelcost();
    });

    $('#InterpreterId').change(function () {
        setInterpreter();
    });

    $("#Accept").closest("form").on("submit", function () { $("#Accept").disableOnSubmit(); });

    function setInterpreter() {
        if ($("#InterpreterId option:selected").val() === "-1") {
            $('#new-interpreter').collapse('show');
        }
        else {
            $('#new-interpreter').collapse('hide');
            validateInterpreter();
        }
    }

    function setExpectedTravelcost() {
        if ($("#InterpreterLocation option:selected").val() === "OffSitePhone" || $("#InterpreterLocation option:selected").val() === "OffSiteVideo" || $("#InterpreterLocation option:selected").val() === "") {
            $('#set-expected-travel-costs').collapse('hide');
        }
        else {
            $('#set-expected-travel-costs').collapse('show');
        }
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

$(window).on("beforeunload", function () {
    var requestId = $("#RequestId").val();
    if (requestId > 0) {
        var $url = tolkBaseUrl + "Request/DeleteRequestView?requestId=" + requestId;
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
    var requestId = $("#RequestId").val();
    if (requestId > 0) {
        var $url = tolkBaseUrl + "Request/AddRequestView?requestId=" + requestId;
        $.ajax({
            type: "POST",
            url: $url,
            data: { __RequestVerificationToken: getAntiForgeryToken() },
            dataType: "json"
        });
    }
});

