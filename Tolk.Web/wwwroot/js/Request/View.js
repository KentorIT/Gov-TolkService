$(function () {

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

    //handle cancellation by broker
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
            $(".cancel-form").submit();
        }
        else {
            $("#cancelMessageDialog .send-message").enable();
        }
    });
});
