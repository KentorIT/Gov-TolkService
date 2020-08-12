"use strict";
$(function () {
    $("body").on("click", ".create-order", function () {
        $.ajax({
            url: "/Order/Create",
            type: 'GET',
            success: function (data) {
                alert(data.message);
            }
        });
    });
});
