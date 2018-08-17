$(function () {
    var currentId = 0;
    $("body").on("click", ".deny-button", function () {
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
    $("body").on("click", ".cancel-button", function () {
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
            $("#denyMessageDialog").modal("hide");
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

