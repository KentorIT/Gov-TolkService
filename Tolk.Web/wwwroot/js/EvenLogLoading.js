$(function () {
    $("body").on("show.bs.collapse", ".collapse.load-dynamically:not('.loaded')", function () {
        var $url = tolkBaseUrl + $(this).data("dynamic-load-url");
        $(this).addClass("loaded");
        var $child = $(this).find(".table-responsive");
        $.ajax({
            url: $url,
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
    });
});
