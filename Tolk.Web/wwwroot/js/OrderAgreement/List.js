﻿$(function () {
    $("body").on("click", ".add-order-agreement", function () {
        window.location.href = "/OrderAgreement/CreateFromOrderNumber?orderNumber=" + $("#OrderNumber").val();
    });
});