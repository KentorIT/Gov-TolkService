$(function () {
    $("body").on("click", ".add-standard-business-document", function () {
        window.location.href = "/StandardBusinessDocument/CreateFromOrderNumber?orderNumber=" + $("#OrderNumber").val();
    });
});