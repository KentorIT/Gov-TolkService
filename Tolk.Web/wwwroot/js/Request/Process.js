function validateControls() {
    if (checkTravelCost()) { $('#Accept').attr('disabled', false); }
    else { $('#Accept').attr('disabled', true); }
}

$(function () {
    checkRequirements();

    $('#ExpectedTravelCosts').change(function () { validateControls(); });

    $('#InterpreterId').change(function () {
        if ($(this).val() === "-1") {
            $('#new-interpreter').collapse('show');
        }
        else {
            $('#new-interpreter').collapse('hide');
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

    function checkRequirements() {
        $('#Accept').attr('disabled', false);

        $("input[id$='CanMeetRequirement']").each(function () {
            var isChecked = $(this).is(':checked');
            var isRequired = $('#' + $(this).attr('id').replace('CanMeetRequirement', 'IsRequired')).attr('value') === 'True';

            if (isRequired && !isChecked) {
                $('#Accept').attr('disabled', true);
            }
        });
    }

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

function checkTravelCost() {
    var travelCost = $('#ExpectedTravelCosts').val();

    if (travelCost = "" || isNaN(parseInt(travelCost)) || (parseInt(travelCost) < 0 || parseInt(travelCost) > 100000)) {
        triggerValidator("Resekostnad måste ha ett värde mellan 0 och 100 000 kr <br \><br \>", $('#ExpectedTravelCosts-error'));
        return false;
    }
    else {
        $('#ExpectedTravelCosts-error').hide();
        return true;
    }
}

function triggerValidator(message, validatorId) {
    validatorId.empty();
    validatorId.append(message);
    validatorId.show();
}

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