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
        if ($("#InterpreterCompetenceLevel").val() === "" || $("#InterpreterCompetenceLevel").val() === "OtherInterpreter" ||
            $('#InterpreterId').val() === "") {
            $('.interpreter-information').addClass("d-none");
        } else {
            var $url = "";
            if ($('#InterpreterId').val() === "-1") {
                $url = tolkBaseUrl + "Verify/InterpreterByOfficialId?officialInterpreterId=" + encodeURIComponent($('#NewInterpreterOfficialInterpreterId').val()) + "&orderId=" + $("#OrderId").val() + "&competenceLevel=" + $("#InterpreterCompetenceLevel").val();
            } else {
                $url = tolkBaseUrl + "Verify/InterpreterByInternalId?id=" + $('#InterpreterId').val() + "&orderId=" + $("#OrderId").val() + "&competenceLevel=" + $("#InterpreterCompetenceLevel").val();
            }
            $.ajax({
                type: "GET",
                url: $url,
                dataType: "json",
                success: function (data) {
                    //system-action-info || warning-info-home
                    //glyphicon-ok || glyphicon-exclamation-sign
                    $('.interpreter-information').removeClass("d-none");
                    $('.interpreter-information > span.info-message').text(data.description);
                    if (data.value === 100) {
                        $('.interpreter-information').removeClass("warning-info-home").addClass("system-action-info")
                            .children("span.glyphicon").removeClass("glyphicon-exclamation-sign").addClass("glyphicon-ok");
                    }
                    else {
                        $('.interpreter-information').removeClass("system-action-info").addClass("warning-info-home")
                            .children("span.glyphicon").removeClass("glyphicon-ok").addClass("glyphicon-exclamation-sign");
                    }
                },
                error: function (t2) {
                    $('.interpreter-information').addClass("d-none");
                }
            });
        }
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