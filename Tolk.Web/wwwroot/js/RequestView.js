$(function () {
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

    $(window).on("beforeunload", function () {
        if (requestId > 0) {
            var $url = tolkBaseUrl + "Request/DeleteRequestView/" + requestId;
            var ua = window.navigator.userAgent;
            this.console.log(ua);
            if (ua.indexOf("MSIE ") > 0 || !!navigator.userAgent.match(/Trident.*rv\:11\./)) {
                $.ajax({
                    type: "POST",
                    url: $url,
                    data: { __RequestVerificationToken: getAntiForgeryToken() },
                    dataType: "json",
                    async: false
                });
            } else {
                navigator.sendBeacon($url, null);
            }
        }
    });
});