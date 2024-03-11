$(function () {
    var loadNotifications = function (brokerId) {
        var $child = $(".archivable-notifications");
        if (brokerId !== "") {
            var $url = tolkBaseUrl + "Notification/List/" + brokerId;
            $.ajax({
                url: $url,
                headers: { "RequestVerificationToken": getAntiForgeryToken() },
                type: 'POST',
                dataType: 'html',
                success: function (data) {
                    $child.html(data);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    $(this).removeClass("loaded");
                    $child.html("<p>Listan gick inte att ladda.</p>");
                }
            });
        } else {
            $child.html("<p>Välj en förmedling att visa notifieringar för.</p>");
        }

    }
    $("body").on("change", "#BrokerId", function () {
        loadNotifications($(this).val());
    });

    loadNotifications($("#BrokerId").val());

});
