"use strict";
$(function () {
    $("body").on("click", ".get-lists", function () {
        $.ajax({
            url: "/Home/GetLists",
            type: 'GET'
        });
    });

    $("body").on("click", ".clear-list", function () {
        $("." + $(this).data("list")).empty();
    });

    $("body").on("click", ".create-order", function () {
        $.ajax({
            url: "/Order/Create?description=" + $(".order-description").val(),
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".api-response-list").append(li);
            }
        });
    });

    $("body").on("click", ".create-several-orders", function () {
        var $info = $(".number-of-orders").val().split(";");
        $.ajax({
            url: "/Order/CreateSeveralOrders?numberOfOrders=" + $info[0] + "&delay=" + $info[1],
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".api-response-list").append(li);
            }
        });
    });

    $("body").on("click", ".approve-order-answer", function () {
        var $info = $(".order-description").val().split(";");
        $.ajax({
            url: "/Order/ApproveAnswer?orderNumber=" + $info[0] + "&brokerKey=" + $info[1],
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".api-response-list").append(li);
            }
        });
    });

    $("body").on("click", ".deny-order-answer", function () {
        var $info = $(".order-description").val().split(";");
        $.ajax({
            url: "/Order/DenyAnswer?orderNumber=" + $info[0] + "&brokerKey=" + $info[1],
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".api-response-list").append(li);
            }
        });
    });

    $("body").on("click", ".confirm-no-answer", function () {
        $.ajax({
            url: "/Order/ConfirmNoAnswer?orderNumber=" + $(".order-description").val(),
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".api-response-list").append(li);
            }
        });
    });

    $("body").on("click", ".confirm-cancellation", function () {
        $.ajax({
            url: "/Order/ConfirmCancellation?orderNumber=" + $(".order-description").val(),
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".api-response-list").append(li);
            }
        });
    });

    $("body").on("click", ".cancel-order", function () {
        var $info = $(".order-description").val().split(";");
        $.ajax({
            url: "/Order/Cancel?orderNumber=" + $info[0] + "&Message=" + $info[1],
            type: 'GET',
            success: function (data) {
                var msg = data.message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                var li = document.createElement("li");
                li.textContent = msg;
                $(".api-response-list").append(li);
            }
        });
    });
});

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/webHooksHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("IncommingCall", function (message) {
    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var li = document.createElement("li");
    li.textContent = msg;
    $(".incoming-webhook-list").append(li);
});

connection.on("OutgoingCall", function (message) {
    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var li = document.createElement("li");
    li.textContent = msg;
    $(".api-call-list").append(li);
});

connection.onclose(reconnect);
startConnection();
function startConnection() {
    console.log('connecting...');
    connection.start()
        .catch(reconnect);
    console.log('connected!');
}

function reconnect() {
    console.log('reconnecting...');
    setTimeout(startConnection, 2000);
}
