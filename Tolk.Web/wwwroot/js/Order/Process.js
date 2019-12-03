$(function () {

    $("body").on("click", ".deny-button", function (event) {
        event.preventDefault();
        $("#denyMessageDialog").openDialog();
    });
    $("body").on("click", "#denyMessageDialog .send-message", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        $("#denyMessageDialog .send-message").disable();
        var $form = $(this).parents(".modal-content").find("form");
        if ($form.valid()) {
            $('.deny-form [name="DenyMessage"]').val($form.find("#DenyMessage").val());
            $(".deny-form").submit();
            $("#denyMessageDialog").modal("hide");
        }
        else {
            $("#denyMessageDialog .send-message").enable();
        }
    });

    //this is for requisition-tab
    $("body").on("click", ".btn-comment-req", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        $(".btn-comment-req").disable();
        var $form = $(this).parents(".modal-content").find("form");
        $("#commentRequisitionValidator").empty();
        if ($form.valid() && $form.find("#CustomerComment").val().length > 0) {
            $form.submit();
        }
        else {
            $(".btn-comment-req").enable();
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
        $("#cancelMessageDialog .send-message").disable();
        var $form = $(this).parents(".modal-content").find("form");
        if ($form.valid()) {
            $('.cancel-form [name="CancelMessage"]').val($form.find("#CancelMessage").val());
            $('.cancel-form [name="AddReplacementOrder"]').val($form.find("#AddReplacementOrder").is(":checked"));
            $(".cancel-form").submit();
            $("#cancelMessageDialog").modal("hide");
        }
        else {
            $("#cancelMessageDialog .send-message").enable();
        }
    });
});
