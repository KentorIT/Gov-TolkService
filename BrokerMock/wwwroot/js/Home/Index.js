"use strict";
$(function () {
    $("body").on("click", ".get-lists", function () {
        $.ajax({
            url: "/Home/GetLists",
            type: 'GET'
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
    document.getElementById("messagesList").appendChild(li);
});

connection.on("OutgoingCall", function (message) {
    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var li = document.createElement("li");
    li.textContent = msg;
    document.getElementById("callsList").appendChild(li);
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
