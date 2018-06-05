// Write your JavaScript code.
$(function () {
    $("body").on("click", ".btn", function () {
        $("#SetStatus").val($(this).data("set-status"));
    });
});
