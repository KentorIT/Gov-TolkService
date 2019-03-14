function validatorMessage(forName, message) {
    var validatorQuery = "[data-valmsg-for=\"" + forName + "\"]";
    $(validatorQuery).empty();
    $(validatorQuery).append(message);
    $(validatorQuery).show();
}

function validateLastAnswerBy() {
    if (!$("#LatestAnswerBy_Date").is(":visible")) {
        return true;
    }
    var date = new Date($("#LatestAnswerBy_Date").val());
    var hour = $("#LatestAnswerBy_Hour").val();
    var minute = $("#LatestAnswerBy_Minute").val();
    if (date !== "" && hour !== "" && minute !== "") {
        var now = new Date($("#now").val());
        if (date.equalsDate(now)) {
            var hours = now.getHours();
            if (hours > Number(hour)) {
                return false;
            } else if (hours === Number(hour)) {
                return !(now.getMinutes() > Number(minute));
            }
        }
    }

    return true;
}

function validateLastAnswerByAgainstStartTime() {
    if (!$("#LatestAnswerBy_Date").is(":visible")) {
        return true;
    }
    var date = new Date($("#LatestAnswerBy_Date").val());
    var hour = $("#LatestAnswerBy_Hour").val();
    var minute = $("#LatestAnswerBy_Minute").val();
    if (date !== "" && hour !== "" && minute !== "") {
        var startDateTime = new Date($("#TimeRange_StartDateTime").val());
        var latestAnswerByDateTime = new Date(date);
        latestAnswerByDateTime.setHours(Number(hour));
        latestAnswerByDateTime.setMinutes(Number(minute));
        return latestAnswerByDateTime <= startDateTime;
    }

    return true;
}

$(function () {
    $("body").on("click", ".deny-button", function (event) {
        event.preventDefault();
        $("#denyMessageDialog").openDialog();
    });
    $("body").on("click", "#denyMessageDialog .send-message", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        var $form = $(this).parents(".modal-content").find("form");
        if ($form.valid()) {
            $('.deny-form [name="DenyMessage"]').val($form.find("#DenyMessage").val());
            $(".deny-form").submit();
            $("#denyMessageDialog").modal("hide");
        }
    });
    //this is for requisition-tab
    $("body").on("click", ".btn-comment-req", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        var $form = $(this).parents(".modal-content").find("form");
        $("#commentRequisitionValidator").empty();
        if ($form.valid() && $form.find("#CustomerComment").val().length > 0) {
            $form.submit();
        }
        else {
            $("#commentRequisitionValidator").append("Ange kommentar");
        }
    });
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
            $('.cancel-form [name="AddReplacementOrder"]').val($form.find("#AddReplacementOrder").is(":checked"));
            $(".cancel-form").submit();
            $("#cancelMessageDialog").modal("hide");
        }
    });

    if ($('#TimeRange_StartDateTime').length > 0) {
        // Turn datetime string into UTC string for parsing
        var startVal = $('#TimeRange_StartDateTime').val().replace(" ", "T").replace(" ", "");
        $('#TimeRange_StartDateTime').val(startVal);
        var now = new Date($('#now').val()).zeroTime();
        var start = new Date(startVal).zeroTime();
        $("#LatestAnswerBy_Date").datepicker("setStartDate", now);
        $("#LatestAnswerBy_Date").datepicker("setEndDate", start);
    }

    $("body").on("click", "#updateLatestAnswerBy", function (event) {
        // Validate LatestAnswerBy time
        if (!validateLastAnswerBy()
            || !validateLastAnswerByAgainstStartTime()) {
            event.preventDefault();
            validatorMessage("LatestAnswerBy.Date", "Ogiltig tid, vänligen kontrollera sista svarstid.");
        }
    });
});
$.fn.extend({
    openDialog: function () {
        $(this).find("input:not(:checkbox),select, textarea").val("");
        $(this).find("input:checkbox").prop("checked", false);
        var $form = $(this).find('form:first');
        $(this).bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
        $(this).modal({ backdrop: "static" });
    }
});
