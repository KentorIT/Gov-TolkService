$(function () {
    var currentId = 0;
    $("body").on("click", ".deny-button", function () {
        event.preventDefault();
        $("#denyMessageDialog").find("input:not(:checkbox),select, textarea").val("");
        $("#denyMessageDialog").find("input:checkbox").prop("checked", false);
        var $form = $("#denyMessageDialog").find('form:first');
        $("#denyMessageDialog").bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
        $("#denyMessageDialog").modal({ backdrop: "static" });
    });
    $("body").on("click", ".send-message", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        var $form = $(this).parents(".modal-content").find("form");
        if ($form.valid()) {
            $(".deny-form #DenyMessage").val($form.find("#DenyMessage").val());
            $(".deny-form").submit();
            $("#denyMessageDialog").modal("hide");
        }
    });
});
