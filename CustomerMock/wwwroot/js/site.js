"use strict";
$(function () {
    $("body").on("click", ".create-order", function () {
        $.ajax({
            url: "/Order/Create",
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".message-list").append(li);
            }
        });
    });
});
