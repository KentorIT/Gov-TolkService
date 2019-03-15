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
    var validateInterpreter = function () {
        /**
         * If we're going to re-activate validation of interpreters, the implementation exists in ValidateInterpreter.js.
         * To re-activate the validation, include InterpreterValidation.js in Process.cshtml before Process.js, and delete this function.
         */
    };

    checkRequirements();
    $("#InterpreterCompetenceLevel, #NewInterpreterOfficialInterpreterId").change(function () {
        validateInterpreter();
    });

    $('#InterpreterId').change(function () {
        if ($(this).val() === "-1") {
            $('#new-interpreter').collapse('show');
        }
        else {
            $('#new-interpreter').collapse('hide');
            validateInterpreter();
        }
    });

    $('#InterpreterLocation').change(function () {
        if ($(this).val() === "OffSitePhone" || $(this).val() === "OffSiteVideo") {
            $('#set-expected-travel-costs').collapse('hide');
        }
        else {
            $('#set-expected-travel-costs').collapse('show');
        }
    });

    $('.checkbox').change(function () {
        checkRequirements();
    });

    //handle cancellation by broker
    $("body").on("click", ".cancel-button", function (event) {
        event.preventDefault();
        $("#cancelMessageDialog").openDialog();
    });

    $("body").on("click", "#cancelMessageDialog .send-message", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        var $form = $(this).parents(".modal-content").find("form");
        if ($form.valid()) {
            $('.cancel-form [name="CancelMessage"]').val($form.find("#CancelMessage").val());
            $(".cancel-form").submit();
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


$.fn.extend({
    openDialog: function () {
        $(this).find("input:not(:checkbox,:hidden),select, textarea").val("");
        $(this).find("input:checkbox").prop("checked", false);
        var $form = $(this).find('form:first');
        $(this).bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
        $(this).modal({ backdrop: "static" });
    }
});