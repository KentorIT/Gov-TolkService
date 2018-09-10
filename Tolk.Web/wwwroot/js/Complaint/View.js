$(function () {
    var currentId = 0;
    $("body").on("click", ".decline-button", function () {
        event.preventDefault();
        $("#disputeMessageDialog").openDialog();
    });
    $("body").on("click", ".refute-button", function () {
        event.preventDefault();
        $("#refuteMessageDialog").openDialog();
    });
    $("body").on("click", ".accept-button", function () {
        event.preventDefault();
        $("#acceptDisputeMessageDialog").openDialog();
    });
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
